using System;
using System.Threading;
using System.Threading.Tasks;
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
                return Task.FromResult<Hover?>(null);
            }

            return Task.FromResult<Hover?>(new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = BuildHoverMarkdown(info)
                })
            });
        }
        catch (Exception)
        {
            return Task.FromResult<Hover?>(null);
        }
    }

    private static string BuildHoverMarkdown(SymbolInfo info) => info.Kind switch
    {
        SymbolKind.Variable when info.Name.Equals("JUST SO", StringComparison.OrdinalIgnoreCase) =>
            "**implicit variable** `JUST SO` — receives the result of the last expression",
        SymbolKind.Variable =>
            $"**(variable)** `{info.Name}` : {info.TypeDisplayName}",
        SymbolKind.Function =>
            $"**(function)** `{info.Name}`({string.Join(", ", info.Parameters ?? Array.Empty<string>())})",
        SymbolKind.Parameter =>
            $"**(parameter)** `{info.Name}`",
        _ =>
            $"`{info.Name}`"
    };
}
