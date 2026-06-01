using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspSymbolKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind;
using TopsyTurvySymbolKind = BWHazel.TopsyTurvy.Analysis.SymbolKind;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles <c>textDocument/documentSymbol</c> requests.
/// </summary>
/// <remarks>
/// Returns a flat list of all declared variables, functions, and parameters for the
/// document.
/// </remarks>
public class DocumentSymbolHandler : DocumentSymbolHandlerBase
{
    private const string LanguageId = "topsy-turvy";
    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// Initialises a new instance of the <see cref="DocumentSymbolHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public DocumentSymbolHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(
        DocumentSymbolCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId)
        };

    /// <inheritdoc/>
    public override Task<SymbolInformationOrDocumentSymbolContainer?> Handle(
        DocumentSymbolParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(
                    new SymbolInformationOrDocumentSymbolContainer());
            }

            List<SymbolInformationOrDocumentSymbol> items = [];
            foreach (SymbolInfo symbol in state.SymbolTable.AllSymbols())
            {
                LspSymbolKind lspKind = MapSymbolKind(symbol.Kind);
                Location location = BuildLocation(request.TextDocument.Uri, symbol);

                SymbolInformation symbolInformation = new()
                {
                    Name = symbol.Name,
                    Kind = lspKind,
                    Location = location
                };

                items.Add(new SymbolInformationOrDocumentSymbol(symbolInformation));
            }

            return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(
                new SymbolInformationOrDocumentSymbolContainer(items));
        }
        catch (Exception)
        {
            return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(
                new SymbolInformationOrDocumentSymbolContainer());
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
            range = new LspRange(new Position(0, 0), new Position(0, 0));
        }

        return new Location
        {
            Uri = uri,
            Range = range
        };
    }
}
