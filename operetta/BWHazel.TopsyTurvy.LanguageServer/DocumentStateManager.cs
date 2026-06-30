using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BWHazel.TopsyTurvy.Analysis;
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
    /// <remarks>
    /// At present this is only called by the <see cref="TextDocumentSyncHandler"/> handler whenever the document content changes:
    /// open, keystroke and save.  While the source is always updated, the symbol table is only rebuilt on a successful parse,
    /// preserving the last good table while the document contains syntax errors.  This ensures language server features, such as
    /// Hover and Go-to-Definition, continue to work even when the document is temporarily in an invalid state.
    /// </remarks>
    public void Update(DocumentUri uri, string source, ParseResult parseResult)
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
                documentState.SymbolTable = SymbolTable.Build(parseResult.Program, source);
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
