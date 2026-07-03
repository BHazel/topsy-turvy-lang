using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Searches all open documents for symbols matching a query string.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>workspace/symbol</c>: The client requests symbols matching a query string across all open documents.
/// </para>
/// <para>
/// Powers the Go to Symbol in Workspace feature.  An empty query returns all symbols from all open documents.
/// A non-empty query filters by case-insensitive substring match against the symbol name, unlike completion
/// which uses a prefix match.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class WorkspaceSymbolHandler(DocumentStateManager documentStateManager)
    : WorkspaceSymbolsHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for workspace symbol handling.
    /// </summary>
    /// <param name="capability">The workspace symbol capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  No document selector is required as workspace symbols span all open documents rather than
    /// being scoped to a specific language or file.
    /// </remarks>
    /// <returns>The registration options for workspace symbol handling.</returns>
    protected override WorkspaceSymbolRegistrationOptions CreateRegistrationOptions(
        WorkspaceSymbolCapability capability, ClientCapabilities clientCapabilities) => new();

    /// <summary>
    /// Handles the <c>workspace/symbol</c> request from the client when a workspace symbol search is performed.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// * All open documents are retrieved from the document state manager.  Documents without a symbol table are skipped.
    /// * For each document, all symbols are visited.  If the query is non-empty, symbols whose name does not contain the query string (case-insensitive substring match) are skipped.
    /// * A <see cref="WorkspaceSymbol"/> is built for each matching symbol using <see cref="LspUtilities.MapSymbolKind"/> and <see cref="LspUtilities.GetSymbolDefinitionLocation"/> for the icon and location respectively.
    /// </remarks>
    /// <returns>
    /// A task resolving to a container of <see cref="WorkspaceSymbol"/> items matching the query across all open
    /// documents, or an empty container if no matches are found or an error occurs.
    /// </returns>
    public override Task<Container<WorkspaceSymbol>?> Handle(
        WorkspaceSymbolParams request, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<(DocumentUri Uri, DocumentState State)> documents = this.documentStateManager.AllDocuments();
            List<WorkspaceSymbol> items = [];
            foreach ((DocumentUri uri, DocumentState state) in documents)
            {
                if (state.SymbolTable is null)
                {
                    continue;
                }

                foreach (SymbolInfo symbol in state.SymbolTable.AllSymbols())
                {
                    if (!string.IsNullOrEmpty(request.Query) && !symbol.Name.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    items.Add(new()
                    {
                        Name = symbol.Name,
                        Kind = LspUtilities.MapSymbolKind(symbol.Kind),
                        Location = LspUtilities.GetSymbolDefinitionLocation(uri, symbol)
                    });
                }
            }

            return Task.FromResult<Container<WorkspaceSymbol>?>(new(items));
        }
        catch (Exception)
        {
            return Task.FromResult<Container<WorkspaceSymbol>?>(new());
        }
    }
}
