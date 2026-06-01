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
    private const string LanguageId = "topsy-turvy";
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
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId)
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
                (definitionUri, info) = this.FindSymbolInOtherDocuments(request.TextDocument.Uri, word);
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

    /// <summary>
    /// Searches all open documents other than the current one for a symbol with the given name.
    /// </summary>
    /// <param name="currentUri">The URI of the requesting document which is excluded.</param>
    /// <param name="name">The symbol name to find.</param>
    /// <returns>
    /// The URI of the document containing the symbol and its <see cref="SymbolInfo"/>,
    /// or the current URI and <c>null</c> if not found.
    /// </returns>
    private (DocumentUri Uri, SymbolInfo? Info) FindSymbolInOtherDocuments(DocumentUri currentUri, string name)
    {
        string currentKey = currentUri.ToString();
        foreach ((DocumentUri otherUri, DocumentState otherState) in this.documentStateManager.AllDocuments())
        {
            if (otherUri.ToString() == currentKey || otherState.SymbolTable is null)
            {
                continue;
            }

            if (otherState.SymbolTable.TryGetSymbol(name, out SymbolInfo? info) && info is not null)
            {
                return (otherUri, info);
            }
        }

        return (currentUri, null);
    }
}
