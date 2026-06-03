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
                LspSymbolKind lspKind = LspUtilities.MapSymbolKind(symbol.Kind);
                Location location = LspUtilities.BuildLocation(request.TextDocument.Uri, symbol);

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

}
