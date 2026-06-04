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
public class DocumentStateManager
{
    private readonly Dictionary<string, DocumentState> states = new();
    private readonly Lock lockObject = new();

    /// <summary>
    /// Updates the state for a document following a parse attempt.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <param name="source">The current raw source text.</param>
    /// <param name="result">The parse result from the most recent attempt.</param>
    /// <remarks>
    /// The source is always updated.  The symbol table is only rebuilt on a successful parse,
    /// preserving the last good table while the document contains syntax errors.
    /// </remarks>
    public void Update(DocumentUri uri, string source, ParseResult result)
    {
        lock (this.lockObject)
        {
            string key = uri.ToString();
            if (!this.states.TryGetValue(key, out DocumentState? state))
            {
                state = new DocumentState();
                this.states[key] = state;
            }

            state.Source = source;
            if (result.Success && result.Program is not null)
            {
                state.SymbolTable = SymbolTable.Build(result.Program, source);
            }
        }
    }

    /// <summary>
    /// Returns the current state for a document.
    /// </summary>
    /// <param name="uri">The document URI.</param>
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
    /// <returns>The first matching <see cref="SymbolInfo"/>, or <c>null</c> if not found.</returns>
    public SymbolInfo? FindSymbolInOtherDocuments(string symbolName, DocumentUri currentUri)
    {
        string currentKey = currentUri.ToString();
        foreach ((DocumentUri otherUri, DocumentState otherState) in this.AllDocuments())
        {
            if (otherUri.ToString() == currentKey || otherState.SymbolTable is null)
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
    /// <returns>
    /// The URI of the document containing the symbol and its <see cref="SymbolInfo"/>,
    /// or the current URI and <c>null</c> if not found.
    /// </returns>
    public (DocumentUri Uri, SymbolInfo? Info) FindSymbolWithUriInOtherDocuments(string symbolName, DocumentUri currentUri)
    {
        string currentKey = currentUri.ToString();
        foreach ((DocumentUri otherUri, DocumentState otherState) in this.AllDocuments())
        {
            if (otherUri.ToString() == currentKey || otherState.SymbolTable is null)
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
    /// <returns>Function <see cref="SymbolInfo"/> records from every other open document.</returns>
    public IEnumerable<SymbolInfo> GetImportedFunctionSymbols(DocumentUri currentUri)
    {
        string currentKey = currentUri.ToString();
        foreach ((DocumentUri otherUri, DocumentState otherState) in this.AllDocuments())
        {
            if (otherUri.ToString() == currentKey || otherState.SymbolTable is null)
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
