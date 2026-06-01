using System;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles <c>textDocument/formatting</c> requests.
/// </summary>
/// <remarks>
/// Applies two transforms to a Topsy Turvy source file:
/// <list type="number">
///   <item>Keyword casing: All language keywords are normalised to their canonical case.</item>
///   <item>Indentation: Every line is re-indented to 2-space libretto style.</item>
/// </list>
/// The result is returned as a single <see cref="TextEdit"/> replacing the entire document.
/// Formatting logic is delegated to <see cref="SourceFormatter"/>.
/// </remarks>
public class DocumentFormattingHandler : DocumentFormattingHandlerBase
{
    private const string LanguageId = "topsy-turvy";

    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// Initialises a new instance of the <see cref="DocumentFormattingHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public DocumentFormattingHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override DocumentFormattingRegistrationOptions CreateRegistrationOptions(
        DocumentFormattingCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId)
        };

    /// <inheritdoc/>
    public override Task<TextEditContainer?> Handle(
        DocumentFormattingParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state is null)
            {
                return Task.FromResult<TextEditContainer?>(null);
            }

            string formatted = SourceFormatter.FormatSource(state.Source);

            string[] originalLines = state.Source.Split('\n');
            string lastOriginalLine = originalLines.Length > 0
                ? originalLines[^1].TrimEnd('\r')
                : string.Empty;

            LspRange fullDocumentRange = new(
                new Position(0, 0),
                new Position(originalLines.Length - 1, lastOriginalLine.Length));

            TextEdit edit = new()
            {
                Range = fullDocumentRange,
                NewText = formatted
            };

            return Task.FromResult<TextEditContainer?>(new TextEditContainer(edit));
        }
        catch (Exception)
        {
            return Task.FromResult<TextEditContainer?>(null);
        }
    }
}
