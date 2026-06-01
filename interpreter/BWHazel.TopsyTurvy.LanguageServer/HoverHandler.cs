using System;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles <c>textDocument/hover</c> requests.
/// </summary>
/// <remarks>
/// Returns type or signature information for the symbol under the cursor.
/// </remarks>
public class HoverHandler : HoverHandlerBase
{
    private const string LanguageId = "topsy-turvy";
    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// Initialises a new instance of the <see cref="HoverHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public HoverHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override HoverRegistrationOptions CreateRegistrationOptions(
        HoverCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId)
        };

    /// <inheritdoc/>
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

            if (!state.SymbolTable.TryGetSymbol(word, out SymbolInfo? info) || info is null)
            {
                info = this.FindSymbolInOtherDocuments(request.TextDocument.Uri, word);
                if (info is null)
                {
                    return Task.FromResult<Hover?>(null);
                }
            }

            return Task.FromResult<Hover?>(new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = HoverMarkdownBuilder.Build(info)
                })
            });
        }
        catch (Exception)
        {
            return Task.FromResult<Hover?>(null);
        }
    }

    /// <summary>
    /// Searches all open documents other than the current one for a symbol with the given name.
    /// </summary>
    /// <param name="currentUri">The URI of the document being hovered, which is excluded.</param>
    /// <param name="name">The symbol name to find.</param>
    /// <returns>The first matching <see cref="SymbolInfo"/>, or <c>null</c> if not found.</returns>
    private SymbolInfo? FindSymbolInOtherDocuments(DocumentUri currentUri, string name)
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
                return info;
            }
        }

        return null;
    }

}
