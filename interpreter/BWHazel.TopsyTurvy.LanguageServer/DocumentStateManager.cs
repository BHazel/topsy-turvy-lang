using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OmniSharp.Extensions.LanguageServer.Protocol;
using BWHazel.TopsyTurvy.Parser;

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
}
