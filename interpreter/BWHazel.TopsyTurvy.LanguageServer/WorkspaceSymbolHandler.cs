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
                        Kind = MapSymbolKind(symbol.Kind),
                        Location = BuildLocation(uri, symbol)
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

    /// <summary>
    /// Maps a Topsy Turvy <see cref="SymbolKind"/> to the corresponding LSP <see cref="LspSymbolKind"/>.
    /// </summary>
    /// <param name="kind">The Topsy Turvy symbol kind.</param>
    /// <returns>The LSP symbol kind.</returns>
    private static LspSymbolKind MapSymbolKind(TopsyTurvySymbolKind kind) => kind switch
    {
        TopsyTurvySymbolKind.Function  => LspSymbolKind.Function,
        TopsyTurvySymbolKind.Parameter => LspSymbolKind.TypeParameter,
        _                      => LspSymbolKind.Variable
    };

    /// <summary>
    /// Builds an LSP <see cref="Location"/> for the given symbol.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <param name="symbol">The symbol whose definition location is required.</param>
    /// <returns>
    /// A <see cref="Location"/> covering the symbol name token on its definition line,
    /// or a zero-point location when the definition position is not known.
    /// </returns>
    private static Location BuildLocation(DocumentUri uri, SymbolInfo symbol)
    {
        LspRange range;
        if (symbol.DefinitionLine != 0)
        {
            int startLine = symbol.DefinitionLine - 1;
            int startChar = symbol.DefinitionColumn - 1;
            int endChar = startChar + symbol.Name.Length;
            range = new LspRange(
                new Position(startLine, startChar),
                new Position(startLine, endChar));
        }
        else
        {
            range = new(new Position(0, 0), new Position(0, 0));
        }

        return new Location
        {
            Uri = uri,
            Range = range
        };
    }
}
