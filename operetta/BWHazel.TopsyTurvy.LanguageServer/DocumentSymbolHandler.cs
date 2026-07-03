using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspSymbolKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Populates the Outline panel with all declared symbols.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/documentSymbol</c>: The client requests all symbols in a text document.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class DocumentSymbolHandler(DocumentStateManager documentStateManager)
    : DocumentSymbolHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for document symbol handling.
    /// </summary>
    /// <param name="capability">The document symbol capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler has no additional configuration beyond registering for Topsy Turvy documents.
    /// </remarks>
    /// <returns>The registration options for document symbol handling.</returns>
    protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId)
        };

    /// <summary>
    /// Handles the <c>textDocument/documentSymbol</c> request from the client when a document symbol request is made.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// * First, the document state is retrieved from the document state manager.  If <c>null</c> or the symbol table is <c>null</c>,
    ///   an empty list is returned so nothing is displayed in the Outline panel.
    /// * All symbols in the <see cref="SymbolTable"/> are visited and added to the list of symbols to be returned to the client.
    /// * Conversions from internal to LSP types are performed.
    /// * LSP symbol information is built and returned to the client.
    /// </remarks>
    /// <returns>
    /// A task resolving to a container of <see cref="SymbolInformation"/> items, one per declared symbol,
    /// or an empty container if the document has not been successfully parsed.
    /// </returns>
    public override Task<SymbolInformationOrDocumentSymbolContainer?> Handle(DocumentSymbolParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(new());
            }

            List<SymbolInformationOrDocumentSymbol> outlineItems = [];
            foreach (SymbolInfo symbol in state.SymbolTable.AllSymbols())
            {
                LspSymbolKind lspKind = LspUtilities.MapSymbolKind(symbol.Kind);
                Location location = LspUtilities.GetSymbolDefinitionLocation(request.TextDocument.Uri, symbol);

                SymbolInformation symbolInformation = new()
                {
                    Name = symbol.Name,
                    Kind = lspKind,
                    Location = location
                };

                outlineItems.Add(new(symbolInformation));
            }

            return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(new(outlineItems));
        }
        catch (Exception)
        {
            return Task.FromResult<SymbolInformationOrDocumentSymbolContainer?>(new());
        }
    }
}
