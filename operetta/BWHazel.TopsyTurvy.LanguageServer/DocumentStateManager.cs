using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Manages the per-document parsed state for all documents open in the language server.
/// </summary>
/// <remarks>
/// This is the central registry of documents for the language server, where all handlers either read or write to.
/// </remarks>
public class DocumentStateManager
{
    private readonly Dictionary<string, DocumentState> states = new();
    private readonly Lock lockObject = new();

    /// <summary>
    /// Updates the state for a document following a parse attempt.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <param name="source">The current raw source text.</param>
    /// <param name="parseResult">The parse result from the most recent attempt.</param>
    /// <param name="externalFunctions">The catalogue of external functions to seed the symbol table with, or <c>null</c> to fall back to <see cref="BindingCatalogue.Default"/> via <see cref="ExternalFunctionRegistrar.Register"/>.</param>
    /// <remarks>
    /// At present this is only called by the <see cref="TextDocumentSyncHandler"/> handler whenever the document content changes:
    /// open, keystroke and save.  While the source is always updated, the symbol table is only rebuilt on a successful parse,
    /// preserving the last good table while the document contains syntax errors.  This ensures language server features, such as
    /// Hover and Go-to-Definition, continue to work even when the document is temporarily in an invalid state.
    /// </remarks>
    public void Update(DocumentUri uri, string source, ParseResult parseResult, BindingCatalogue? externalFunctions = null)
    {
        lock (this.lockObject)
        {
            string documentUriKey = uri.ToString();
            if (!this.states.TryGetValue(documentUriKey, out DocumentState? documentState))
            {
                documentState = new();
                this.states[documentUriKey] = documentState;
            }

            documentState.Source = source;
            if (parseResult.Success && parseResult.Program is not null)
            {
                SymbolTable symbolTable = SymbolTable.Build(parseResult.Program, source);
                documentState.ShadowedExternalFunctionNames = ExternalFunctionRegistrar.Register(symbolTable, externalFunctions);
                documentState.SymbolTable = symbolTable;
                documentState.ImportPaths = [.. parseResult.Program.Statements
                    .OfType<ImportNode>()
                    .Select(import => import.FilePath)];
                documentState.NamespacePath = parseResult.Program.Statements
                    .OfType<NamespaceDeclarationNode>()
                    .FirstOrDefault()?.Path ?? [];
            }
        }
    }

    /// <summary>
    /// Returns the current state for a document.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <remarks>
    /// <para>
    /// The main entry point for the majority of handlers as their first processing step to retrieve the current
    /// document source and symbol table.  If this method returns either <c>null</c>, where the document is not currently tracked
    /// by the language server, or the <see cref="DocumentState.SymbolTable"/> is <c>null</c>, where the document has not yet been
    /// successfully parsed, the handler should return an empty response immediately.
    /// </para>
    /// <para>
    /// Called by the following handlers as their first step:
    /// * <see cref="CodeLensHandler"/>: Retrieves the current document source to count symbol references for inline annotations.
    /// * <see cref="CompletionHandler"/>: Retrieves the current document symbol table to build the list of completion items.
    /// * <see cref="DefinitionHandler"/>: Retrieves the current document source and symbol table to locate a symbol definition.
    /// * <see cref="DocumentFormattingHandler"/>: Retrieves the current document source text to format.
    /// * <see cref="DocumentSymbolHandler"/>: Retrieves the current document symbol table to build the Outline panel.
    /// * <see cref="FoldingRangeHandler"/>: Retrieves the current document source to identify foldable regions.
    /// * <see cref="HoverHandler"/>: Retrieves the current document symbol table and source to find the symbol under the cursor.
    /// * <see cref="PrepareRenameHandler"/>: Retrieves the current document symbol table to validate a rename target.
    /// * <see cref="RenameHandler"/>: Retrieves the current document symbol table and source to find and rename a symbol.
    /// * <see cref="ReferencesHandler"/>: Retrieves the current document symbol table and source to find all references to a symbol.
    /// * <see cref="SemanticTokensHandler"/>: Retrieves the current document symbol table and source to apply syntax highlighting.
    /// * <see cref="SignatureHelpHandler"/>: Retrieves the current document symbol table to provide function signature information.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The <see cref="DocumentState"/> for the document, or <c>null</c> if the document is not tracked.
    /// </returns>
    public DocumentState? Get(DocumentUri uri)
    {
        lock (this.lockObject)
        {
            this.states.TryGetValue(uri.ToString(), out DocumentState? state);
            return state;
        }
    }

    /// <summary>
    /// Returns a snapshot of all currently tracked documents and their state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called by handlers that need to search across all open documents rather than just the current one.
    /// * <see cref="CodeLensHandler"/>: Counts references across all open documents to display a reference count for each symbol.
    /// * <see cref="ReferencesHandler"/>: Searches all open documents for references to a symbol.
    /// * <see cref="RenameHandler"/>: Applies a rename to all references to a symbol across all open documents.
    /// * <see cref="WorkspaceSymbolHandler"/>: Searches all open documents for symbols matching the query.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A list of <see cref="DocumentUri"/> and <see cref="DocumentState"/> pairs for every
    /// document currently open in the language server.
    /// </returns>
    public IReadOnlyList<(DocumentUri Uri, DocumentState State)> AllDocuments()
    {
        lock (this.lockObject)
        {
            return this.states
                .Select(state => (DocumentUri.From(state.Key), state.Value))
                .ToList();
        }
    }

    /// <summary>
    /// Returns the other open documents connected to the given document by a <c>PRAY ADMIT</c> import, in either
    /// direction, one hop.
    /// </summary>
    /// <param name="currentUri">The URI of the document to find import-connected documents for.</param>
    /// <remarks>
    /// Used by <see cref="ReferencesHandler"/> and <see cref="CodeLensHandler"/> so a same-named symbol in an
    /// unrelated file is not reported as a reference.  Does not model <c>PRAY RECOGNISE</c>/FQN visibility.
    /// </remarks>
    /// <returns>Every other open document connected to <paramref name="currentUri"/> by an import, in either direction.</returns>
    public IReadOnlyList<(DocumentUri Uri, DocumentState State)> GetImportConnectedDocuments(DocumentUri currentUri)
    {
        string currentUriKey = currentUri.ToString();
        DocumentState? currentState = this.Get(currentUri);
        if (currentState is null)
        {
            return [];
        }

        HashSet<string> currentImportUriKeys = [.. currentState.ImportPaths
            .Select(importPath => ResolveImportUriKey(importPath, currentUri))];

        List<(DocumentUri, DocumentState)> connected = [];
        foreach ((DocumentUri otherUri, DocumentState otherState) in this.AllDocuments())
        {
            string otherUriKey = otherUri.ToString();
            if (otherUriKey == currentUriKey)
            {
                continue;
            }

            bool currentImportsOther = currentImportUriKeys.Contains(otherUriKey);
            bool otherImportsCurrent = otherState.ImportPaths
                .Any(importPath => ResolveImportUriKey(importPath, otherUri) == currentUriKey);

            if (currentImportsOther || otherImportsCurrent)
            {
                connected.Add((otherUri, otherState));
            }
        }

        return connected;
    }

    /// <summary>
    /// Resolves a <c>PRAY ADMIT</c> import path to the URI key of the document it refers to.
    /// </summary>
    /// <param name="importPath">The raw import path string, relative or absolute.</param>
    /// <param name="importingUri">The URI of the document declaring the import; a relative path is resolved against its directory.</param>
    /// <returns>The URI of the resolved document.</returns>
    private static string ResolveImportUriKey(string importPath, DocumentUri importingUri)
    {
        string? importingDirectory = Path.GetDirectoryName(DocumentUri.GetFileSystemPath(importingUri));
        string resolvedPath = importingDirectory is not null && !Path.IsPathRooted(importPath)
            ? Path.GetFullPath(Path.Combine(importingDirectory, importPath))
            : importPath;

        return DocumentUri.FromFileSystemPath(resolvedPath).ToString();
    }

    /// <summary>
    /// Returns the distinct namespace paths declared across all open documents.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="CompletionHandler"/> and <see cref="HoverHandler"/> for namespace completion and hover.
    /// </remarks>
    /// <returns>Every distinct, non-empty <see cref="DocumentState.NamespacePath"/> across all open documents.</returns>
    public IReadOnlyList<IReadOnlyList<string>> GetKnownNamespacePaths()
    {
        Dictionary<string, IReadOnlyList<string>> distinctPaths = new(StringComparer.OrdinalIgnoreCase);
        foreach ((_, DocumentState state) in this.AllDocuments())
        {
            if (state.NamespacePath.Count == 0)
            {
                continue;
            }

            string key = string.Join('.', state.NamespacePath);
            distinctPaths.TryAdd(key, state.NamespacePath);
        }

        return [.. distinctPaths.Values];
    }

    /// <summary>
    /// Returns the function symbols declared in every open document whose own namespace path exactly matches
    /// <paramref name="namespacePath"/> (segment-by-segment, case-insensitive).
    /// </summary>
    /// <param name="namespacePath">The namespace path to match against.</param>
    /// <remarks>
    /// Used by <see cref="CompletionHandler"/> to scope function-name completion after the <c>WITH DUTY</c> segment
    /// of a fully-qualified <c>SUMMON</c> target.
    /// </remarks>
    /// <returns>Function <see cref="SymbolInfo"/> records from every open document whose namespace path matches.</returns>
    public IEnumerable<SymbolInfo> GetFunctionsInNamespace(IReadOnlyList<string> namespacePath)
    {
        foreach ((_, DocumentState state) in this.AllDocuments())
        {
            if (state.SymbolTable is null || !NamespacePathsEqual(state.NamespacePath, namespacePath))
            {
                continue;
            }

            foreach (SymbolInfo symbol in state.SymbolTable.AllSymbols())
            {
                if (symbol.Kind == SymbolKind.Function)
                {
                    yield return symbol;
                }
            }
        }
    }

    /// <summary>
    /// Compares two namespace paths segment-by-segment, case-insensitively.
    /// </summary>
    /// <param name="first">The first namespace path.</param>
    /// <param name="second">The second namespace path.</param>
    /// <returns><c>true</c> if both paths have the same number of segments and each segment matches case-insensitively, otherwise <c>false</c>.</returns>
    private static bool NamespacePathsEqual(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (int i = 0; i < first.Count; i++)
        {
            if (!string.Equals(first[i], second[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Removes the state for a document when it is closed.
    /// </summary>
    /// <remarks>
    /// At present this is only called by the <see cref="TextDocumentSyncHandler"/> handler whenever the document is closed,
    /// removing the document from the registry to avoid stale state.
    /// </remarks>
    /// <param name="uri">The document URI.</param>
    public void Remove(DocumentUri uri)
    {
        lock (this.lockObject)
        {
            this.states.Remove(uri.ToString());
        }
    }

    /// <summary>
    /// Searches all open documents other than the current one for a symbol with the given name.
    /// </summary>
    /// <param name="symbolName">The symbol name to find.</param>
    /// <param name="currentUri">The URI of the current document, which is excluded from the search.</param>
    /// <remarks>
    /// <para>
    /// Called by handlers which search the current file then try other open files requiring only the
    /// <see cref="SymbolInfo"/>:
    /// * <see cref="HoverHandler"/>: Displays symbol information when hovering over a symbol in the current document.
    /// * <see cref="ReferencesHandler"/>: Confirms a symbol exists before scanning all open documents for references to it.
    /// * <see cref="RenameHandler"/>: Renames a symbol in the current document and all other open documents.
    /// </para>
    /// </remarks>
    /// <returns>The first matching <see cref="SymbolInfo"/>, or <c>null</c> if not found.</returns>
    public SymbolInfo? FindSymbolInOtherDocuments(string symbolName, DocumentUri currentUri)
    {
        string currentDocumentUriKey = currentUri.ToString();
        foreach ((DocumentUri otherUri, DocumentState otherState) in this.AllDocuments())
        {
            if (otherUri.ToString() == currentDocumentUriKey || otherState.SymbolTable is null)
            {
                continue;
            }

            if (otherState.SymbolTable.TryGetSymbol(symbolName, out SymbolInfo? info) && info is not null)
            {
                return info;
            }
        }

        return null;
    }

    /// <summary>
    /// Searches all open documents other than the current one for a symbol with the given name
    /// and returns both the symbol info and the URI of the document where it was found.
    /// </summary>
    /// <param name="symbolName">The symbol name to find.</param>
    /// <param name="currentUri">The URI of the current document, which is excluded from the search.</param>
    /// <remarks>
    /// Similar to <see cref="FindSymbolInOtherDocuments"/> but also returns the document URI where the symbol was found:
    /// * <see cref="DefinitionHandler"/>: Required by Go-to-Definition to open the correct document when the symbol is defined in another file.
    /// </remarks>
    /// <returns>
    /// The URI of the document containing the symbol and its <see cref="SymbolInfo"/>,
    /// or the current URI and <c>null</c> if not found.
    /// </returns>
    public (DocumentUri Uri, SymbolInfo? Info) FindSymbolWithUriInOtherDocuments(string symbolName, DocumentUri currentUri)
    {
        string currentDocumentUriKey = currentUri.ToString();
        foreach ((DocumentUri otherUri, DocumentState otherState) in this.AllDocuments())
        {
            if (otherUri.ToString() == currentDocumentUriKey || otherState.SymbolTable is null)
            {
                continue;
            }

            if (otherState.SymbolTable.TryGetSymbol(symbolName, out SymbolInfo? info) && info is not null)
            {
                return (otherUri, info);
            }
        }

        return (currentUri, null);
    }

    /// <summary>
    /// Returns function symbols declared in all open documents other than the given document.
    /// </summary>
    /// <param name="currentUri">The URI of the current document, which is excluded.</param>
    /// <remarks>
    /// <para>
    /// Called by handlers which need to be aware of functions declared in other open documents:
    /// * <see cref="CompletionHandler"/>: Concatenates function names from all open documents to provide a list of available functions for auto-completion.
    /// * <see cref="SemanticTokensHandler"/>: Similar to <see cref="CompletionHandler"/>, but enables syntax highlighting.
    /// </para>
    /// </remarks>
    /// <returns>Function <see cref="SymbolInfo"/> records from every other open document.</returns>
    public IEnumerable<SymbolInfo> GetImportedFunctionSymbols(DocumentUri currentUri)
    {
        string currentDocumentUriKey = currentUri.ToString();
        foreach ((DocumentUri otherUri, DocumentState otherState) in this.AllDocuments())
        {
            if (otherUri.ToString() == currentDocumentUriKey || otherState.SymbolTable is null)
            {
                continue;
            }

            foreach (SymbolInfo symbol in otherState.SymbolTable.AllSymbols())
            {
                if (symbol.Kind == SymbolKind.Function)
                {
                    yield return symbol;
                }
            }
        }
    }
}
