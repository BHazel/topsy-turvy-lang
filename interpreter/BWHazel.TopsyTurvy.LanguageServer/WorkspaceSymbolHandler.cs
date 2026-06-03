using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspSymbolKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind;
using TopsyTurvySymbolKind = BWHazel.TopsyTurvy.Analysis.SymbolKind;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles <c>workspace/symbol</c> requests.
/// </summary>
/// <remarks>
/// Returns symbols matching the query string from all documents currently open in the
/// language server.  An empty query returns all symbols across all open documents.
/// </remarks>
public class WorkspaceSymbolHandler : WorkspaceSymbolsHandlerBase
{
    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// Initialises a new instance of the <see cref="WorkspaceSymbolHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public WorkspaceSymbolHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override WorkspaceSymbolRegistrationOptions CreateRegistrationOptions(
        WorkspaceSymbolCapability capability, ClientCapabilities clientCapabilities) => new();

    /// <inheritdoc/>
    public override Task<Container<WorkspaceSymbol>?> Handle(
        WorkspaceSymbolParams request, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<(DocumentUri Uri, DocumentState State)> documents =
                this.documentStateManager.AllDocuments();

            List<WorkspaceSymbol> items = [];
            foreach ((DocumentUri uri, DocumentState state) in documents)
            {
                if (state.SymbolTable is null)
                {
                    continue;
                }

                foreach (SymbolInfo symbol in state.SymbolTable.AllSymbols())
                {
                    if (!string.IsNullOrEmpty(request.Query) &&
                        !symbol.Name.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    items.Add(new WorkspaceSymbol
                    {
                        Name = symbol.Name,
                        Kind = LspUtilities.MapSymbolKind(symbol.Kind),
                        Location = LspUtilities.BuildLocation(uri, symbol)
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
