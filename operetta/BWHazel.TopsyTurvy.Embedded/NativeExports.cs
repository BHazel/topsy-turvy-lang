using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Embedded.Analysis;
using BWHazel.TopsyTurvy.Embedded.NativeInterop;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.Runtime;
using BWHazel.TopsyTurvy.TypeChecker;

namespace BWHazel.TopsyTurvy.Embedded;

/// <summary>
/// Provides the native export surface consumed across the C ABI boundary.
/// </summary>
/// <remarks>
/// <para>
/// Every export is <see cref="UnmanagedCallersOnlyAttribute"/>-annotated with an explicit <c>EntryPoint</c>
/// naming the exported C symbol (<c>topsyturvy_*</c>).  The managed method name itself follows ordinary .NET
/// <c>PascalCase</c> conventions since the two are independent once <c>EntryPoint</c> is specified.  Every export
/// is wrapped in a try/catch: a managed exception escaping such an export is undefined behaviour, so each
/// one returns a sentinel value (0 or a null pointer) on failure instead, stashing the exception message
/// into the session <see cref="NativeSession.LastError"/> for retrieval via <see cref="GetLastError"/>.
/// </para>
/// <para>
/// Every pointer this class returns to the caller must be released via <see cref="FreeBuffer"/> exactly once and
/// never accessed afterwards:
/// * <see cref="AnalyseSource"/>
/// * <see cref="FormatSource"/>
/// * <see cref="GetHover"/>
/// * <see cref="GetCompletions"/>
/// * <see cref="GetTokens"/>
/// * <see cref="GetLastError"/>
/// </para>
/// </remarks>
public static unsafe class NativeExports
{
    /// <summary>
    /// Returns the native export contract version.
    /// </summary>
    /// <returns>The contract version number, starting at <c>1</c>.</returns>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_api_version")]
    public static int ApiVersion() => 1;

    /// <summary>
    /// Creates a new native session, capturing the registered callbacks from the caller.
    /// </summary>
    /// <param name="outputLine">The callback invoked once per output line written by a running programme.</param>
    /// <param name="resolveImport">The callback invoked to resolve a <c>PRAY ADMIT</c> import filename to source text.</param>
    /// <param name="context">
    /// A caller-supplied "userdata" pointer, for example one pointing at Swift-side state the caller wants
    /// to associate with this session. This library never dereferences or interprets the value: it stores
    /// it and passes it back, unchanged, as the first argument to every invocation of <paramref name="outputLine"/>
    /// and <paramref name="resolveImport"/>, so the caller can recover which session or object a callback
    /// invocation belongs to, since a C function pointer cannot itself capture that context.
    /// </param>
    /// <returns>An opaque session handle, or <c>0</c> if an exception was thrown.</returns>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_session_create")]
    public static nint CreateSession(
        delegate* unmanaged<nint, byte*, byte, void> outputLine,
        delegate* unmanaged<nint, byte*, byte*> resolveImport,
        nint context)
    {
        try
        {
            NativeCallbacks callbacks = new(outputLine, resolveImport, context);
            NativeSession session = new(callbacks);

            GCHandle handle = GCHandle.Alloc(session);
            nint handleValue = GCHandle.ToIntPtr(handle);

            SessionRegistry.Register(handleValue);
            return handleValue;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Destroys a native session created by <see cref="CreateSession"/>.
    /// </summary>
    /// <param name="session">The session handle to destroy.</param>
    /// <remarks>
    /// An invalid or already-destroyed handle is silently ignored rather than crashing the host process.
    /// <see cref="SessionRegistry"/> is consulted first, since <see cref="GCHandle"/> APIs are unsafe to call
    /// on a handle value this method has not itself vouched for.
    /// </remarks>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_session_destroy")]
    public static void DestroySession(nint session)
    {
        if (!SessionRegistry.TryUnregister(session))
        {
            return;
        }

        try
        {
            GCHandle.FromIntPtr(session).Free();
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Parses and type-checks the given Topsy Turvy source, returning every diagnostic produced.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="sourceUtf8">A null-terminated, UTF-8 encoded pointer to the Topsy Turvy source text.</param>
    /// <returns>
    /// A null-terminated, UTF-8 encoded pointer to a JSON-serialised <see cref="AnalysisResult"/>, which the
    /// caller must release via <see cref="FreeBuffer"/>, or a null pointer if the session handle is invalid
    /// or an exception was thrown.
    /// </returns>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_analyse")]
    public static byte* AnalyseSource(nint session, byte* sourceUtf8)
    {
        if (!TryGetSession(session, out NativeSession? nativeSession) || nativeSession is null)
        {
            return null;
        }

        try
        {
            string source = Marshal.PtrToStringUTF8((nint)sourceUtf8) ?? string.Empty;
            ParseResult parseResult = new TopsyTurvyParser().TryParse(source);

            List<DiagnosticInfo> diagnostics = [.. parseResult.Diagnostics.Select(ToDiagnosticInfo)];
            bool success = parseResult.Success;

            if (parseResult.Program is not null)
            {
                TypeCheckResult typeCheckResult = new TopsyTurvyTypeChecker()
                    .Check(parseResult.Program, NativeImportResolver.Create(nativeSession.Callbacks));

                diagnostics.AddRange(typeCheckResult.Diagnostics.Select(ToDiagnosticInfo));
                success = success && typeCheckResult.Success;
            }

            AnalysisResult result = new(success, diagnostics);
            string analysisResultJson = JsonSerializer.Serialize(result, EmbeddedJsonContext.Default.AnalysisResult);
            return AllocateUtf8String(analysisResultJson);
        }
        catch (Exception ex)
        {
            nativeSession.LastError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Builds Markdown hover content for the symbol at the given position.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="sourceUtf8">A null-terminated, UTF-8 encoded pointer to the Topsy Turvy source text.</param>
    /// <param name="line">The 0-indexed line number, matching the LSP convention used elsewhere in this toolchain.</param>
    /// <param name="column">The 0-indexed column number, matching the LSP convention used elsewhere in this toolchain.</param>
    /// <returns>
    /// A null-terminated, UTF-8 encoded pointer to a JSON-serialised <see cref="HoverResult"/>, which the
    /// caller must release via <see cref="FreeBuffer"/>, or a null pointer if the session handle is invalid
    /// or an exception was thrown.
    /// </returns>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_hover")]
    public static byte* GetHover(nint session, byte* sourceUtf8, int line, int column)
    {
        if (!TryGetSession(session, out NativeSession? nativeSession) || nativeSession is null)
        {
            return null;
        }

        try
        {
            string source = Marshal.PtrToStringUTF8((nint)sourceUtf8) ?? string.Empty;
            ParseResult parseResult = new TopsyTurvyParser().TryParse(source);

            HoverResult result = new(false, null);
            if (parseResult.Program is not null)
            {
                SymbolTable symbolTable = SymbolTable.Build(parseResult.Program, source);
                string? word = SymbolTable.ExtractWordAt(source, line, column);
                if (word is not null && symbolTable.TryGetSymbol(word, out SymbolInfo? symbolInfo) && symbolInfo is not null)
                {
                    result = new(true, HoverMarkdownBuilder.Build(symbolInfo));
                }
            }

            string hoverResultJson = JsonSerializer.Serialize(result, EmbeddedJsonContext.Default.HoverResult);
            return AllocateUtf8String(hoverResultJson);
        }
        catch (Exception ex)
        {
            nativeSession.LastError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Builds keyword and symbol completion candidates for the given position.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="sourceUtf8">A null-terminated, UTF-8 encoded pointer to the Topsy Turvy source text.</param>
    /// <param name="line">The 0-indexed line number, matching the LSP convention used elsewhere in this toolchain.</param>
    /// <param name="column">The 0-indexed column number, matching the LSP convention used elsewhere in this toolchain.</param>
    /// <remarks>Keywords are always offered, even when the source fails to parse.</remarks>
    /// <returns>
    /// A null-terminated, UTF-8 encoded pointer to a JSON-serialised <see cref="CompletionResult"/>, which the
    /// caller must release via <see cref="FreeBuffer"/>, or a null pointer if the session handle is invalid
    /// or an exception was thrown.
    /// </returns>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_complete")]
    public static byte* GetCompletions(nint session, byte* sourceUtf8, int line, int column)
    {
        if (!TryGetSession(session, out NativeSession? nativeSession) || nativeSession is null)
        {
            return null;
        }

        try
        {
            string source = Marshal.PtrToStringUTF8((nint)sourceUtf8) ?? string.Empty;
            ParseResult parseResult = new TopsyTurvyParser().TryParse(source);

            (string phrase, string lastWord) = PhraseContext.Get(source, line, column);

            bool isKeywordContext = phrase.Length == 0
                || KeywordData.Keywords.Any(keywordInfo => keywordInfo.Keyword.StartsWith(phrase, StringComparison.OrdinalIgnoreCase));
            
            int insertOffset = isKeywordContext
                ? phrase.Length - lastWord.Length
                : 0;

            List<CompletionItemInfo> items = [];
            if (parseResult.Program is not null)
            {
                SymbolTable symbolTable = SymbolTable.Build(parseResult.Program, source);
                IEnumerable<CompletionItemInfo> symbolItems = symbolTable.AllSymbols()
                    // Namespace symbols use a synthesised "*"-joined display name, e.g. "Accounts*Payroll", that a
                    // user would never type as a single completion target, so they are excluded here.
                    .Where(symbol => symbol.Kind != SymbolKind.Namespace
                        && (lastWord.Length == 0 || symbol.Name.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase)))
                    .Select(BuildSymbolItem);
                items.AddRange(symbolItems);
            }

            IEnumerable<CompletionItemInfo> keywordItems = KeywordData.Keywords
                .Where(keywordInfo => !isKeywordContext
                    || phrase.Length == 0
                    || keywordInfo.Keyword.StartsWith(phrase, StringComparison.OrdinalIgnoreCase))
                .Select(keywordInfo => BuildKeywordItem(keywordInfo, insertOffset));
            items.AddRange(keywordItems);

            CompletionResult result = new(items);
            string completionResultJson = JsonSerializer.Serialize(result, EmbeddedJsonContext.Default.CompletionResult);
            return AllocateUtf8String(completionResultJson);
        }
        catch (Exception ex)
        {
            nativeSession.LastError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Formats the given Topsy Turvy source with canonical keyword casing and libretto indentation.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="sourceUtf8">A null-terminated, UTF-8 encoded pointer to the Topsy Turvy source text.</param>
    /// <returns>
    /// A null-terminated, UTF-8 encoded pointer to the formatted source text (not JSON), which the caller
    /// must release via <see cref="FreeBuffer"/>, or a null pointer if the session handle is invalid or an
    /// exception was thrown.
    /// </returns>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_format")]
    public static byte* FormatSource(nint session, byte* sourceUtf8)
    {
        if (!TryGetSession(session, out NativeSession? nativeSession) || nativeSession is null)
        {
            return null;
        }

        try
        {
            string source = Marshal.PtrToStringUTF8((nint)sourceUtf8) ?? string.Empty;
            string formattedSource = SourceFormatter.FormatSource(source);
            return AllocateUtf8String(formattedSource);
        }
        catch (Exception ex)
        {
            nativeSession.LastError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Scans the given Topsy Turvy source into categorised token spans for editor syntax highlighting.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="sourceUtf8">A null-terminated, UTF-8 encoded pointer to the Topsy Turvy source text.</param>
    /// <returns>
    /// A null-terminated, UTF-8 encoded pointer to a JSON-serialised <see cref="TokenResult"/>, which the
    /// caller must release via <see cref="FreeBuffer"/>, or a null pointer if the session handle is invalid
    /// or an exception was thrown.
    /// </returns>
    /// <remarks>
    /// This is a lexical scan via <see cref="SourceTokeniser"/>, not a parse: it succeeds even when the
    /// source does not currently form valid syntax, since the editor calls it continuously while the user
    /// types.
    /// </remarks>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_tokens")]
    public static byte* GetTokens(nint session, byte* sourceUtf8)
    {
        if (!TryGetSession(session, out NativeSession? nativeSession) || nativeSession is null)
        {
            return null;
        }

        try
        {
            string source = Marshal.PtrToStringUTF8((nint)sourceUtf8) ?? string.Empty;
            List<TokenInfo> tokens = [.. SourceTokeniser.Tokenise(source).Select(ToTokenInfo)];

            TokenResult result = new(tokens);
            string tokenResultJson = JsonSerializer.Serialize(result, EmbeddedJsonContext.Default.TokenResult);
            return AllocateUtf8String(tokenResultJson);
        }
        catch (Exception ex)
        {
            nativeSession.LastError = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Parses, type-checks and executes the given Topsy Turvy source through the interpreter.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="sourceUtf8">A null-terminated, UTF-8 encoded pointer to the Topsy Turvy source text.</param>
    /// <param name="argsJsonUtf8">A null-terminated, UTF-8 encoded pointer to a JSON <c>string[]</c> exposed as <c>THE PROPS</c>, or a null pointer for none.</param>
    /// <param name="stdinUtf8">A null-terminated, UTF-8 encoded, newline-delimited pointer pre-seeding <c>PRAY TELL</c> input, or a null pointer for none.</param>
    /// <returns>
    /// <c>0</c> on success, <c>1</c> if parsing failed, <c>2</c> if type-checking failed, <c>3</c> if an
    /// exception was thrown, or the interpreter recorded a runtime error diagnostic or <c>4</c> if execution
    /// was cancelled via <see cref="CancelExecution"/>.
    /// </returns>
    /// <remarks>
    /// <see cref="Interpreter.Execute"/> does not throw for a cancellation or a runtime error: it catches
    /// both internally and reports them as diagnostics in the returned <see cref="DiagnosticCollection"/>
    /// instead.  This export inspects that collection and the session <see cref="CancellationTokenSource"/>
    /// after execution completes, rather than relying on an exception ever escaping <see cref="Interpreter.Execute"/>.
    /// </remarks>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_execute")]
    public static int ExecuteProgramme(nint session, byte* sourceUtf8, byte* argsJsonUtf8, byte* stdinUtf8)
    {
        if (!TryGetSession(session, out NativeSession? nativeSession) || nativeSession is null)
        {
            return 3;
        }

        try
        {
            string source = Marshal.PtrToStringUTF8((nint)sourceUtf8) ?? string.Empty;
            ParseResult parseResult = new TopsyTurvyParser().TryParse(source);
            if (!parseResult.Success || parseResult.Program is null)
            {
                return 1;
            }

            Func<string, string?> importResolver = NativeImportResolver.Create(nativeSession.Callbacks);
            TypeCheckResult typeCheckResult = new TopsyTurvyTypeChecker().Check(parseResult.Program, importResolver);
            if (!typeCheckResult.Success)
            {
                return 2;
            }

            string[] commandLineArguments = ParseArgs(argsJsonUtf8);
            IEnumerable<string> stdinLines = ParseStdin(stdinUtf8);

            BufferedNativeIO io = new(stdinLines, nativeSession.Callbacks);
            Interpreter interpreter = new(io);

            CancellationTokenSource cancellationTokenSource = new();
            nativeSession.CancellationTokenSource = cancellationTokenSource;

            InterpreterExecutionOptions options = new(
                ExecutionTimeout: null,
                SourceFilePath: null,
                SourceFileResolver: importResolver,
                CommandLineArguments: commandLineArguments);

            DiagnosticCollection runtimeDiagnostics = interpreter.Execute(parseResult.Program, cancellationTokenSource.Token, options);
            if (cancellationTokenSource.IsCancellationRequested)
            {
                nativeSession.LastError = "Execution was cancelled.";
                return 4;
            }

            if (runtimeDiagnostics.HasErrors)
            {
                nativeSession.LastError = string.Join(
                    "; ",
                    runtimeDiagnostics.Diagnostics
                        .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                        .Select(diagnostic => diagnostic.Message));
            
                return 3;
            }

            return 0;
        }
        catch (Exception ex)
        {
            nativeSession.LastError = ex.Message;
            return 3;
        }
    }

    /// <summary>
    /// Cooperatively cancels the session currently running execution.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <remarks>A no-op if the handle is invalid or no execution has started yet.</remarks>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_cancel")]
    public static void CancelExecution(nint session)
    {
        try
        {
            if (TryGetSession(session, out NativeSession? nativeSession) && nativeSession is not null)
            {
                nativeSession.CancellationTokenSource?.Cancel();
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Returns the message of the most recently caught exception for the given session.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <returns>
    /// A null-terminated, UTF-8 encoded pointer to the error message, which the caller must release via
    /// <see cref="FreeBuffer"/>, or a null pointer if the handle is invalid or no error has occurred.
    /// </returns>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_last_error")]
    public static byte* GetLastError(nint session)
    {
        try
        {
            if (!TryGetSession(session, out NativeSession? nativeSession) || nativeSession?.LastError is null)
            {
                return null;
            }

            return AllocateUtf8String(nativeSession.LastError);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Releases a buffer previously returned by another method.
    /// </summary>
    /// <param name="pointer">The pointer to release, or a null pointer, which is a no-op.</param>
    [UnmanagedCallersOnly(EntryPoint = "topsyturvy_free")]
    public static void FreeBuffer(byte* pointer)
    {
        if (pointer is not null)
        {
            Marshal.FreeHGlobal((nint)pointer);
        }
    }

    /// <summary>
    /// Resolves a session handle to its <see cref="NativeSession"/>, treating any failure as an invalid handle.
    /// </summary>
    /// <param name="handle">The session handle to resolve.</param>
    /// <param name="session">The resolved session, or <c>null</c> if resolution failed.</param>
    /// <returns><c>true</c> if the handle resolved to a live session, otherwise <c>false</c>.</returns>
    /// <remarks>
    /// <see cref="SessionRegistry"/> is consulted first, since <see cref="GCHandle"/> APIs are unsafe to call
    /// on a handle value that was not itself produced by <see cref="CreateSession"/>.
    /// </remarks>
    private static bool TryGetSession(nint handle, out NativeSession? session)
    {
        if (!SessionRegistry.IsActive(handle))
        {
            session = null;
            return false;
        }

        try
        {
            session = GCHandle.FromIntPtr(handle).Target as NativeSession;
            return session is not null;
        }
        catch (Exception)
        {
            session = null;
            return false;
        }
    }

    /// <summary>
    /// Maps a <see cref="Diagnostic"/> to its native-ready representation.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to map.</param>
    /// <returns>The equivalent <see cref="DiagnosticInfo"/>.</returns>
    private static DiagnosticInfo ToDiagnosticInfo(Diagnostic diagnostic) =>
        new(
            diagnostic.Message,
            diagnostic.Severity.ToString(),
            diagnostic.Span.Start.Line,
            diagnostic.Span.Start.Column,
            diagnostic.Span.End.Line,
            diagnostic.Span.End.Column);

    /// <summary>
    /// Maps a <see cref="SourceToken"/> to its native-ready representation.
    /// </summary>
    /// <param name="token">The token to map.</param>
    /// <returns>The equivalent <see cref="TokenInfo"/>.</returns>
    private static TokenInfo ToTokenInfo(SourceToken token) =>
        new(
            token.Category,
            token.Span.Start.Line,
            token.Span.Start.Column,
            token.Span.End.Line,
            token.Span.End.Column);

    /// <summary>
    /// Builds a completion candidate from a symbol.
    /// </summary>
    /// <param name="symbol">The symbol information.</param>
    /// <returns>The equivalent <see cref="CompletionItemInfo"/>.</returns>
    private static CompletionItemInfo BuildSymbolItem(SymbolInfo symbol) =>
        new(
            symbol.Name,
            symbol.Kind.ToString(),
            symbol.Kind switch
            {
                SymbolKind.Variable => symbol.TypeDisplayName,
                SymbolKind.Function => $"({string.Join(", ", symbol.TypedParameters?.Select(static parameter => parameter.Name) ?? [])})",
                SymbolKind.Parameter => "parameter",
                _ => null
            },
            symbol.Name);

    /// <summary>
    /// Builds a completion candidate from a keyword entry.
    /// </summary>
    /// <param name="keywordInfo">The keyword and its short description.</param>
    /// <param name="insertOffset">The offset into the keyword string from which to build the insert text.</param>
    /// <returns>The equivalent <see cref="CompletionItemInfo"/>.</returns>
    private static CompletionItemInfo BuildKeywordItem((string Keyword, string Detail) keywordInfo, int insertOffset) =>
        new(keywordInfo.Keyword, "Keyword", keywordInfo.Detail, keywordInfo.Keyword[insertOffset..]);

    /// <summary>
    /// Deserialises the <c>THE PROPS</c> command-line argument list from its JSON native-ready representation.
    /// </summary>
    /// <param name="commandLineArgumentsJsonUtf8">A null-terminated, UTF-8 encoded pointer to a JSON <c>string[]</c>, or a null pointer.</param>
    /// <returns>The deserialised arguments, or an empty array if <paramref name="commandLineArgumentsJsonUtf8"/> is null or empty.</returns>
    private static string[] ParseArgs(byte* commandLineArgumentsJsonUtf8)
    {
        if (commandLineArgumentsJsonUtf8 is null)
        {
            return [];
        }

        string commandLineArgumentsJson = Marshal.PtrToStringUTF8((nint)commandLineArgumentsJsonUtf8) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(commandLineArgumentsJson))
        {
            return [];
        }

        return JsonSerializer.Deserialize(commandLineArgumentsJson, EmbeddedJsonContext.Default.StringArray) ?? [];
    }

    /// <summary>
    /// Splits the pre-seeded <c>PRAY TELL</c> input buffer into individual lines.
    /// </summary>
    /// <param name="stdinUtf8">A null-terminated, UTF-8 encoded, newline-delimited pointer, or a null pointer.</param>
    /// <returns>The input lines, in order, or an empty sequence if <paramref name="stdinUtf8"/> is null or empty.</returns>
    private static IEnumerable<string> ParseStdin(byte* stdinUtf8)
    {
        if (stdinUtf8 is null)
        {
            return [];
        }

        string stdinText = Marshal.PtrToStringUTF8((nint)stdinUtf8) ?? string.Empty;
        return stdinText.Length == 0
            ? []
            : stdinText.Split('\n');
    }

    /// <summary>
    /// Copies a managed string to a null-terminated, UTF-8 encoded unmanaged buffer.
    /// </summary>
    /// <param name="value">The string to copy.</param>
    /// <returns>A pointer to the newly allocated buffer, owned by the caller until released via <see cref="FreeBuffer"/>.</returns>
    private static byte* AllocateUtf8String(string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        nint buffer = Marshal.AllocHGlobal(byteCount + 1);
        Span<byte> destination = new((void*)buffer, byteCount + 1);
        Encoding.UTF8.GetBytes(value, destination);
        destination[byteCount] = 0;
        return (byte*)buffer;
    }
}
