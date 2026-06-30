using System;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Navigates to the definition, using Go-to-Definition, of the symbol under the cursor.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/definition</c>: The client requests the definition location of the symbol at a given position in a text document.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class DefinitionHandler(DocumentStateManager documentStateManager)
    : DefinitionHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for definition handling.
    /// </summary>
    /// <param name="capability">The definition capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler has no additional configuration beyond registering for Topsy Turvy documents.
    /// </remarks>
    /// <returns>The registration options for definition handling.</returns>
    protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId)
        };

    /// <summary>
    /// Handles the <c>textDocument/definition</c> request from the client when a Go-to-Definition action is triggered.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// * The document state is retrieved from the document state manager.  If <c>null</c> or the symbol table is <c>null</c>, an empty result is returned so the editor takes no action.
    /// * The word at the cursor position is extracted from the source using <see cref="SymbolTable.ExtractWordAt"/>.  If no word is found, an empty result is returned.
    /// * The word is looked up in the current document symbol table.  If not found, other open documents are searched via <see cref="DocumentStateManager.FindSymbolWithUriInOtherDocuments"/>, which also returns the URI of the document where the symbol was found.
    /// * If the symbol has no known definition position (<see cref="SymbolInfo.DefinitionLine"/> is <c>0</c>), an empty result is returned.
    /// * An LSP <see cref="Location"/> is built covering the symbol name on its definition line, converting from 1-based Topsy Turvy co-ordinates to 0-based LSP co-ordinates, and returned to the client.
    /// </remarks>
    /// <returns>
    /// A task resolving to a collection containing the definition <see cref="Location"/> of the symbol,
    /// or an empty collection if no definition can be found.
    /// </returns>
    public override Task<LocationOrLocationLinks?> Handle(
        DefinitionParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
            }

            string? word = SymbolTable.ExtractWordAt(
                state.Source,
                request.Position.Line,
                request.Position.Character);

            if (word is null)
            {
                return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
            }

            DocumentUri definitionUri = request.TextDocument.Uri;
            if (!state.SymbolTable.TryGetSymbol(word, out SymbolInfo? symbolInfo) || symbolInfo is null)
            {
                (definitionUri, symbolInfo) = this.documentStateManager.FindSymbolWithUriInOtherDocuments(word, request.TextDocument.Uri);
            }

            if (symbolInfo is null || symbolInfo.DefinitionLine == 0)
            {
                return Task.FromResult<LocationOrLocationLinks?>(new());
            }

            int startLine = symbolInfo.DefinitionLine - 1;
            int startCharacter = symbolInfo.DefinitionColumn - 1;
            int endCharacter = startCharacter + word.Length;

            LocationOrLocationLink location = new(new Location()
            {
                Uri = definitionUri,
                Range = new(new(startLine, startCharacter), new(startLine, endCharacter))
            });

            return Task.FromResult<LocationOrLocationLinks?>(new(location));
        }
        catch (Exception)
        {
            return Task.FromResult<LocationOrLocationLinks?>(new());
        }
    }
}
