using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Embedded;
using BWHazel.TopsyTurvy.Embedded.Analysis;
using BWHazel.TopsyTurvy.Embedded.NativeInterop;

namespace BWHazel.TopsyTurvy.Tests.Embedded;

/// <summary>
/// Tests for the <see cref="NativeExports"/> class.
/// </summary>
/// <remarks>
/// Every export is <see cref="System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute"/>-annotated, so
/// the C# compiler forbids calling them directly even from in-process test code.  A function
/// pointer must be obtained via <c>&amp;NativeExports.Method</c> and invoked through that, exactly as a
/// native caller would resolve the exported symbol.  All pointer marshalling is isolated in the private
/// static helper methods below, so the <c>[Fact]</c> test bodies themselves only ever deal in plain
/// managed types.
/// </remarks>
public class NativeExportsTests
{
    /// <summary>
    /// Tests that <see cref="NativeExports.ApiVersion"/> returns the frozen contract version.
    /// </summary>
    [Fact]
    public void ApiVersion_WhenCalled_ReturnsOne()
    {
        int version = ApiVersion();

        version.ShouldBe(1);
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.CreateSession"/> followed by <see cref="NativeExports.DestroySession"/> does not throw.
    /// </summary>
    [Fact]
    public void CreateSession_ThenDestroySession_DoesNotThrow()
    {
        nint session = CreateSession();

        session.ShouldNotBe(0);
        Should.NotThrow(() => DestroySession(session));
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.DestroySession"/> given an invalid or already-destroyed handle does not throw.
    /// </summary>
    [Fact]
    public void DestroySession_WithInvalidOrDoubleDestroyedHandle_DoesNotThrow()
    {
        nint session = CreateSession();
        DestroySession(session);

        Should.NotThrow(() => DestroySession(session));
        Should.NotThrow(() => DestroySession(0));
        Should.NotThrow(() => DestroySession(12345));
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.AnalyseSource"/> reports success with no diagnostics for a valid programme.
    /// </summary>
    [Fact]
    public void AnalyseSource_WithValidProgram_ReturnsSuccessWithNoDiagnostics()
    {
        nint session = CreateSession();

        try
        {
            AnalysisResult result = Analyse(session, "HARK! \"Test\"\n\nFINALE.\n");

            result.Success.ShouldBeTrue();
            result.Diagnostics.ShouldBeEmpty();
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.AnalyseSource"/> reports a diagnostic with a positive line and column for an unterminated string literal.
    /// </summary>
    [Fact]
    public void AnalyseSource_WithUnterminatedStringLiteral_ReturnsDiagnosticWithLineAndColumn()
    {
        nint session = CreateSession();

        try
        {
            AnalysisResult result = Analyse(session, "HARK! \"Test\"\n\nBEHOLD \"unterminated\n\nFINALE.\n");

            result.Success.ShouldBeFalse();
            result.Diagnostics.ShouldNotBeEmpty();
            result.Diagnostics[0].Severity.ShouldBe("Error");
            result.Diagnostics[0].StartLine.ShouldBeGreaterThan(0);
            result.Diagnostics[0].StartColumn.ShouldBeGreaterThan(0);
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.AnalyseSource"/> tracks a surrogate-pair emoji in a <c>YARN</c> literal as
    /// two UTF-16 code units, reporting the same diagnostic position as an equal-length ASCII stand-in.
    /// </summary>
    [Fact]
    public void AnalyseSource_WithSurrogatePairInYarnLiteral_TracksColumnAsTwoUtf16CodeUnits()
    {
        nint asciiSession = CreateSession();
        nint emojiSession = CreateSession();

        try
        {
            AnalysisResult asciiResult = Analyse(asciiSession, "HARK! \"Test\"\n\nBEHOLD \"xx unterminated\n\nFINALE.\n");
            AnalysisResult emojiResult = Analyse(emojiSession, "HARK! \"Test\"\n\nBEHOLD \"\U0001F3AD unterminated\n\nFINALE.\n");

            emojiResult.Success.ShouldBeFalse();
            emojiResult.Diagnostics.Count.ShouldBe(asciiResult.Diagnostics.Count);
            emojiResult.Diagnostics[0].StartLine.ShouldBe(asciiResult.Diagnostics[0].StartLine);
            emojiResult.Diagnostics[0].StartColumn.ShouldBe(asciiResult.Diagnostics[0].StartColumn);
            emojiResult.Diagnostics[0].EndLine.ShouldBe(asciiResult.Diagnostics[0].EndLine);
            emojiResult.Diagnostics[0].EndColumn.ShouldBe(asciiResult.Diagnostics[0].EndColumn);
        }
        finally
        {
            DestroySession(asciiSession);
            DestroySession(emojiSession);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.GetHover"/> returns Markdown content for a symbol at the requested position.
    /// </summary>
    [Fact]
    public void GetHover_WithSymbolAtPosition_ReturnsMarkdownContent()
    {
        nint session = CreateSession();

        try
        {
            HoverResult result = Hover(session, "HARK! \"Test\"\n\nPRAY WELCOME AllLords AS A PEER BEING 5\n\nFINALE.\n", 2, 15);

            result.Found.ShouldBeTrue();
            result.MarkdownContent.ShouldNotBeNull();
            result.MarkdownContent.ShouldContain("AllLords");
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.GetCompletions"/> offers keyword candidates at the start of an empty document.
    /// </summary>
    [Fact]
    public void GetCompletions_WithEmptyPhraseAtStartOfDocument_ReturnsKeywordCandidates()
    {
        nint session = CreateSession();

        try
        {
            CompletionResult result = Complete(session, "HARK! \"Test\"\n\nFINALE.\n", 0, 0);

            result.Items.ShouldContain(item => item.Label == "HARK!" && item.Kind == "Keyword");
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.GetCompletions"/> filters keyword items to a multi-word namespace keyword when its own first words are typed as the whole phrase.
    /// </summary>
    [Fact]
    public void GetCompletions_WithPartialRecogniseKeywordTyped_FiltersKeywordListToMatches()
    {
        nint session = CreateSession();

        try
        {
            CompletionResult result = Complete(session, "HARK! \"Test\"\nTHE CURTAIN RISES.\nPRAY REC\nFINALE.\n", 2, 8);

            result.Items.ShouldContain(item => item.Label == "PRAY RECOGNISE");
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.GetCompletions"/> filters keyword items to <c>WITH DISTRICT</c> and not the shorter, pre-existing bare <c>WITH</c> keyword.
    /// </summary>
    [Fact]
    public void GetCompletions_WithPartialWithDistrictKeywordTyped_FiltersKeywordListToMatches()
    {
        nint session = CreateSession();

        try
        {
            CompletionResult result = Complete(session, "HARK! \"Test\"\nTHE CURTAIN RISES.\nWITH DI\nFINALE.\n", 2, 7);

            result.Items.ShouldContain(item => item.Label == "WITH DISTRICT");
            result.Items.ShouldNotContain(item => item.Label == "WITH");
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.FormatSource"/> normalises keyword casing to the canonical uppercase form.
    /// </summary>
    [Fact]
    public void FormatSource_WithLowercaseKeywords_ReturnsCanonicalCase()
    {
        nint session = CreateSession();

        try
        {
            string formatted = Format(session, "hark! \"Test\"\n\nfinale.\n");

            formatted.ShouldContain("HARK!");
            formatted.ShouldContain("FINALE.");
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.GetTokens"/> reports a single "comment" token spanning multiple lines for a block comment, including one left unterminated at end-of-source.
    /// </summary>
    [Fact]
    public void GetTokens_WithBlockComment_ReturnsSingleTokenSpanningMultipleLines()
    {
        nint session = CreateSession();

        try
        {
            TokenResult terminated = Tokens(session, "HARK! \"Test\"\n\n(ASIDE, AT SOME LENGTH:\nspans several\nlines\nEND OF ASIDE.)\n\nFINALE.\n");
            TokenInfo terminatedComment = terminated.Tokens.Where(token => token.Category == "comment").ShouldHaveSingleItem();
            terminatedComment.StartLine.ShouldNotBe(terminatedComment.EndLine);

            TokenResult unterminated = Tokens(session, "HARK! \"Test\"\n\n(ASIDE, AT SOME LENGTH:\nnever closed\n");
            TokenInfo unterminatedComment = unterminated.Tokens.Where(token => token.Category == "comment").ShouldHaveSingleItem();
            unterminatedComment.StartLine.ShouldNotBe(unterminatedComment.EndLine);
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.GetTokens"/> categorises a string literal, a keyword phrase, a
    /// type keyword, <c>THE PROPS</c> and a plain identifier correctly.
    /// </summary>
    [Fact]
    public void GetTokens_WithMixedSource_CategorisesEachTokenKind()
    {
        nint session = CreateSession();

        try
        {
            const string source =
                "HARK! \"Test\"\n\nPRINCIPALS\nPRAY WELCOME peerVariable AS A PEER BEING 1\nTHE CURTAIN RISES.\n\nBEHOLD THE PROPS\n\nFINALE.\n";
            TokenResult result = Tokens(session, source);

            result.Tokens.ShouldContain(token => token.Category == "string");
            result.Tokens.ShouldContain(token => token.Category == "keyword");
            result.Tokens.ShouldContain(token => token.Category == "type");
            result.Tokens.ShouldContain(token => token.Category == "variable");
            result.Tokens.ShouldContain(token => token.Category == "identifier");
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.ExecuteProgramme"/> invokes the registered output callback for a <c>BEHOLD</c> statement.
    /// </summary>
    [Fact]
    public void ExecuteProgramme_WithBeholdStatement_InvokesOutputCallback()
    {
        TestCallbackCapture.Reset();
        nint session = CreateSession();

        try
        {
            int status = Execute(session, "HARK! \"Test\"\n\nBEHOLD \"Hello\"\n\nFINALE.\n");

            status.ShouldBe(0);
            TestCallbackCapture.OutputLines.ShouldContain("Hello");
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.ExecuteProgramme"/> declares <c>THE PROPS</c> from the arguments passed in <c>args_json_utf8</c>.
    /// </summary>
    [Fact]
    public void ExecuteProgramme_WithArguments_DeclaresThePropsFromArguments()
    {
        TestCallbackCapture.Reset();
        nint session = CreateSession();

        try
        {
            int status = Execute(session, "HARK! \"Test\"\n\nBEHOLD VICTIM 1 ON THE PROPS\n\nFINALE.\n", ["first-argument"]);

            status.ShouldBe(0);
            TestCallbackCapture.OutputLines.ShouldContain("first-argument");
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.ExecuteProgramme"/> can run the same session declarations twice without an "already declared" runtime error.
    /// </summary>
    [Fact]
    public void ExecuteProgramme_CalledTwiceOnSameSession_DoesNotThrowAlreadyDeclared()
    {
        TestCallbackCapture.Reset();
        nint session = CreateSession();
        string source = "HARK! \"Test\"\n\nPRINCIPALS\nPRAY WELCOME count AS A PEER BEING 0\nTHE CURTAIN RISES.\n\nBEHOLD count\n\nFINALE.\n";

        try
        {
            int firstStatus = Execute(session, source);
            int secondStatus = Execute(session, source);

            firstStatus.ShouldBe(0);
            secondStatus.ShouldBe(0);
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.CancelExecution"/> stops a running infinite loop and that
    /// <see cref="NativeExports.GetLastError"/> then reports the cancellation.
    /// </summary>
    [Fact]
    public async Task CancelExecution_WithRunningInfiniteLoop_StopsExecutionAndRecordsError()
    {
        TestCallbackCapture.Reset();
        nint session = CreateSession();
        string source = "HARK! \"Test\"\n\nBY A LEGAL FICTION\n  BEHOLD \"loop\"\nTHE TERM EXPIRES.\n\nFINALE.\n";

        try
        {
            Task<int> executeTask = Task.Run(() => Execute(session, source));

            await Task.Delay(50);
            Cancel(session);

            int status = await executeTask;
            status.ShouldBe(4);

            string? errorMessage = LastError(session);
            errorMessage.ShouldNotBeNullOrEmpty();
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Tests that <see cref="NativeExports.GetLastError"/> returns no message for a session with no recorded error.
    /// </summary>
    [Fact]
    public void GetLastError_WithNoRecordedError_ReturnsNull()
    {
        nint session = CreateSession();

        try
        {
            string? errorMessage = LastError(session);

            errorMessage.ShouldBeNull();
        }
        finally
        {
            DestroySession(session);
        }
    }

    /// <summary>
    /// Creates a session registered with <see cref="TestCallbackCapture"/> callback targets.
    /// </summary>
    /// <returns>The created session handle.</returns>
    private static unsafe nint CreateSession()
    {
        delegate* unmanaged<delegate* unmanaged<nint, byte*, byte, void>, delegate* unmanaged<nint, byte*, byte*>, nint, nint> createSession = &NativeExports.CreateSession;
        return createSession(&TestCallbackCapture.OnOutputLine, &TestCallbackCapture.OnResolveImport, 0);
    }

    /// <summary>
    /// Destroys a session via <see cref="NativeExports.DestroySession"/>.
    /// </summary>
    /// <param name="session">The session handle to destroy.</param>
    private static unsafe void DestroySession(nint session)
    {
        delegate* unmanaged<nint, void> destroySession = &NativeExports.DestroySession;
        destroySession(session);
    }

    /// <summary>
    /// Calls <see cref="NativeExports.AnalyseSource"/> and deserialises its result.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="source">The Topsy Turvy source text.</param>
    /// <returns>The deserialised <see cref="AnalysisResult"/>.</returns>
    private static unsafe AnalysisResult Analyse(nint session, string source)
    {
        delegate* unmanaged<nint, byte*, byte*> analyseSource = &NativeExports.AnalyseSource;
        delegate* unmanaged<byte*, void> freeBuffer = &NativeExports.FreeBuffer;

        nint sourcePointer = Marshal.StringToCoTaskMemUTF8(source);
        try
        {
            byte* resultPointer = analyseSource(session, (byte*)sourcePointer);
            string analysisResultJson = Marshal.PtrToStringUTF8((nint)resultPointer) ?? "{}";
            freeBuffer(resultPointer);
            return JsonSerializer.Deserialize(analysisResultJson, EmbeddedJsonContext.Default.AnalysisResult)!;
        }
        finally
        {
            Marshal.FreeCoTaskMem(sourcePointer);
        }
    }

    /// <summary>
    /// Calls <see cref="NativeExports.GetHover"/> and deserialises its result.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="source">The Topsy Turvy source text.</param>
    /// <param name="line">The 0-indexed line number.</param>
    /// <param name="column">The 0-indexed column number.</param>
    /// <returns>The deserialised <see cref="HoverResult"/>.</returns>
    private static unsafe HoverResult Hover(nint session, string source, int line, int column)
    {
        delegate* unmanaged<nint, byte*, int, int, byte*> getHover = &NativeExports.GetHover;
        delegate* unmanaged<byte*, void> freeBuffer = &NativeExports.FreeBuffer;

        nint sourcePointer = Marshal.StringToCoTaskMemUTF8(source);
        try
        {
            byte* resultPointer = getHover(session, (byte*)sourcePointer, line, column);
            string hoverResultJson = Marshal.PtrToStringUTF8((nint)resultPointer) ?? "{}";
            freeBuffer(resultPointer);
            return JsonSerializer.Deserialize(hoverResultJson, EmbeddedJsonContext.Default.HoverResult)!;
        }
        finally
        {
            Marshal.FreeCoTaskMem(sourcePointer);
        }
    }

    /// <summary>
    /// Calls <see cref="NativeExports.GetCompletions"/> and deserialises its result.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="source">The Topsy Turvy source text.</param>
    /// <param name="line">The 0-indexed line number.</param>
    /// <param name="column">The 0-indexed column number.</param>
    /// <returns>The deserialised <see cref="CompletionResult"/>.</returns>
    private static unsafe CompletionResult Complete(nint session, string source, int line, int column)
    {
        delegate* unmanaged<nint, byte*, int, int, byte*> getCompletions = &NativeExports.GetCompletions;
        delegate* unmanaged<byte*, void> freeBuffer = &NativeExports.FreeBuffer;

        nint sourcePointer = Marshal.StringToCoTaskMemUTF8(source);
        try
        {
            byte* resultPointer = getCompletions(session, (byte*)sourcePointer, line, column);
            string completionResultJson = Marshal.PtrToStringUTF8((nint)resultPointer) ?? "{}";
            freeBuffer(resultPointer);
            return JsonSerializer.Deserialize(completionResultJson, EmbeddedJsonContext.Default.CompletionResult)!;
        }
        finally
        {
            Marshal.FreeCoTaskMem(sourcePointer);
        }
    }

    /// <summary>
    /// Calls <see cref="NativeExports.GetTokens"/>.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="source">The Topsy Turvy source text.</param>
    /// <returns>The decoded <see cref="TokenResult"/>.</returns>
    private static unsafe TokenResult Tokens(nint session, string source)
    {
        delegate* unmanaged<nint, byte*, byte*> getTokens = &NativeExports.GetTokens;
        delegate* unmanaged<byte*, void> freeBuffer = &NativeExports.FreeBuffer;

        nint sourcePointer = Marshal.StringToCoTaskMemUTF8(source);
        try
        {
            byte* resultPointer = getTokens(session, (byte*)sourcePointer);
            string tokenResultJson = Marshal.PtrToStringUTF8((nint)resultPointer) ?? "{}";
            freeBuffer(resultPointer);
            return JsonSerializer.Deserialize(tokenResultJson, EmbeddedJsonContext.Default.TokenResult)!;
        }
        finally
        {
            Marshal.FreeCoTaskMem(sourcePointer);
        }
    }

    /// <summary>
    /// Calls <see cref="NativeExports.FormatSource"/>.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="source">The Topsy Turvy source text.</param>
    /// <returns>The formatted source text.</returns>
    private static unsafe string Format(nint session, string source)
    {
        delegate* unmanaged<nint, byte*, byte*> formatSource = &NativeExports.FormatSource;
        delegate* unmanaged<byte*, void> freeBuffer = &NativeExports.FreeBuffer;

        nint sourcePointer = Marshal.StringToCoTaskMemUTF8(source);
        try
        {
            byte* resultPointer = formatSource(session, (byte*)sourcePointer);
            string formattedSource = Marshal.PtrToStringUTF8((nint)resultPointer) ?? string.Empty;
            freeBuffer(resultPointer);
            return formattedSource;
        }
        finally
        {
            Marshal.FreeCoTaskMem(sourcePointer);
        }
    }

    /// <summary>
    /// Calls <see cref="NativeExports.ExecuteProgramme"/> with no arguments or pre-seeded input.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="source">The Topsy Turvy source text.</param>
    /// <returns>The execution status code.</returns>
    private static unsafe int Execute(nint session, string source) => Execute(session, source, arguments: null);

    /// <summary>
    /// Calls <see cref="NativeExports.ExecuteProgramme"/> with a JSON-encoded <c>THE PROPS</c> argument array.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <param name="source">The Topsy Turvy source text.</param>
    /// <param name="arguments">The arguments exposed as <c>THE PROPS</c>, or <c>null</c> for none.</param>
    /// <returns>The execution status code.</returns>
    private static unsafe int Execute(nint session, string source, string[]? arguments)
    {
        delegate* unmanaged<nint, byte*, byte*, byte*, int> executeProgramme = &NativeExports.ExecuteProgramme;

        nint sourcePointer = Marshal.StringToCoTaskMemUTF8(source);
        nint argumentsPointer = arguments is null
            ? 0
            : Marshal.StringToCoTaskMemUTF8(JsonSerializer.Serialize(arguments));
        try
        {
            return executeProgramme(session, (byte*)sourcePointer, (byte*)argumentsPointer, null);
        }
        finally
        {
            Marshal.FreeCoTaskMem(sourcePointer);
            if (argumentsPointer != 0)
            {
                Marshal.FreeCoTaskMem(argumentsPointer);
            }
        }
    }

    /// <summary>
    /// Calls <see cref="NativeExports.CancelExecution"/>.
    /// </summary>
    /// <param name="session">The session handle.</param>
    private static unsafe void Cancel(nint session)
    {
        delegate* unmanaged<nint, void> cancelExecution = &NativeExports.CancelExecution;
        cancelExecution(session);
    }

    /// <summary>
    /// Calls <see cref="NativeExports.GetLastError"/>.
    /// </summary>
    /// <param name="session">The session handle.</param>
    /// <returns>The last recorded error message, or <c>null</c> if none was recorded.</returns>
    private static unsafe string? LastError(nint session)
    {
        delegate* unmanaged<nint, byte*> getLastError = &NativeExports.GetLastError;
        delegate* unmanaged<byte*, void> freeBuffer = &NativeExports.FreeBuffer;

        byte* errorPointer = getLastError(session);
        if (errorPointer is null)
        {
            return null;
        }

        string message = Marshal.PtrToStringUTF8((nint)errorPointer) ?? string.Empty;
        freeBuffer(errorPointer);
        return message;
    }

    /// <summary>
    /// Calls <see cref="NativeExports.ApiVersion"/>.
    /// </summary>
    /// <returns>The native export contract version.</returns>
    private static unsafe int ApiVersion()
    {
        delegate* unmanaged<int> apiVersion = &NativeExports.ApiVersion;
        return apiVersion();
    }
}
