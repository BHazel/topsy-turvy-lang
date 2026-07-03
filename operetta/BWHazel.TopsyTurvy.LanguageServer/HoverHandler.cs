using System;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Provides hover information for symbols.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/hover</c>: The client requests hover information for a symbol at a given position in a text document.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class HoverHandler(DocumentStateManager documentStateManager)
    : HoverHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for hover handling.
    /// </summary>
    /// <param name="capability">The hover capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler has no additional configuration beyond registering for Topsy Turvy documents.
    /// </remarks>
    /// <returns>The registration options for hover handling.</returns>
    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId)
        };

    /// <summary>
    /// Handles the <c>textDocument/hover</c> request from the client when hover information is requested.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// * The document state is retrieved from the document state manager.  If <c>null</c> or the symbol table is <c>null</c>, <c>null</c> is returned so no hover pop-up is displayed.
    /// * The word at the cursor position is extracted from the source using <see cref="SymbolTable.ExtractWordAt"/>.  If no word is found, <c>null</c> is returned.
    /// * The word is looked up in the current document symbol table.  If not found, other open documents are searched via <see cref="DocumentStateManager.FindSymbolInOtherDocuments"/>.  If still not found, <c>null</c> is returned.
    /// * A Markdown hover card is built using <see cref="HoverMarkdownBuilder.Build"/> and returned to the client.
    /// </remarks>
    /// <returns>
    /// A task resolving to a <see cref="Hover"/> containing a Markdown card for the symbol under the cursor,
    /// or <c>null</c> if no symbol is found at that position.
    /// </returns>
    public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<Hover?>(null);
            }

            string? word = SymbolTable.ExtractWordAt(
                state.Source,
                request.Position.Line,
                request.Position.Character);

            if (word is null)
            {
                return Task.FromResult<Hover?>(null);
            }

            if (!state.SymbolTable.TryGetSymbol(word, out SymbolInfo? symbolInfo) || symbolInfo is null)
            {
                symbolInfo = this.documentStateManager.FindSymbolInOtherDocuments(word, request.TextDocument.Uri);
                if (symbolInfo is null)
                {
                    return Task.FromResult<Hover?>(null);
                }
            }

            return Task.FromResult<Hover?>(
                new()
                {
                    Contents = new(new MarkupContent()
                    {
                        Kind = MarkupKind.Markdown,
                        Value = HoverMarkdownBuilder.Build(symbolInfo)
                    })
                });
        }
        catch (Exception)
        {
            return Task.FromResult<Hover?>(null);
        }
    }
}
