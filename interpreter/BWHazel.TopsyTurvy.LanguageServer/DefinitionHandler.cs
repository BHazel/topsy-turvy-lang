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
/// Handles <c>textDocument/definition</c> requests.
/// </summary>
/// <remarks>
/// This navigates to the declaration or definition of the symbol under the cursor.
/// </remarks>
public class DefinitionHandler : DefinitionHandlerBase
{
    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// Initialises a new instance of the <see cref="DefinitionHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public DefinitionHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override DefinitionRegistrationOptions CreateRegistrationOptions(
        DefinitionCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId)
        };

    /// <inheritdoc/>
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
            if (!state.SymbolTable.TryGetSymbol(word, out SymbolInfo? info) || info is null)
            {
                (definitionUri, info) = this.documentStateManager.FindSymbolWithUriInOtherDocuments(word, request.TextDocument.Uri);
            }

            if (info is null || info.DefinitionLine == 0)
            {
                return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
            }

            int startLine = info.DefinitionLine - 1;
            int startChar = info.DefinitionColumn - 1;
            int endChar = startChar + word.Length;

            LocationOrLocationLink location = new(new Location
            {
                Uri = definitionUri,
                Range = new LspRange(
                    new Position(startLine, startChar),
                    new Position(startLine, endChar))
            });

            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks(location));
        }
        catch (Exception)
        {
            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
        }
    }

}
