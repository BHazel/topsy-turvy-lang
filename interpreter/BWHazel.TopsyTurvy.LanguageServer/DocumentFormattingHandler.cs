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
/// Formats a document according to language standards.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/formatting</c>: The client requests the formatted version of a text document.
/// </para>
/// <para>
/// Formatting logic is delegated to <see cref="SourceFormatter"/> and applies two transforms:
/// * Keyword normalisation to standard case with the majority of keywords in uppercase.
/// * Indentation based on the depth of the current block.
/// The result is returned as a single <see cref="TextEdit"/> replacing the entire document.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class DocumentFormattingHandler(DocumentStateManager documentStateManager)
    : DocumentFormattingHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for document formatting handling.
    /// </summary>
    /// <param name="capability">The document formatting capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler has no additional configuration beyond registering for Topsy Turvy documents.
    /// </remarks>
    /// <returns>The registration options for document formatting handling.</returns>
    protected override DocumentFormattingRegistrationOptions CreateRegistrationOptions(DocumentFormattingCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId)
        };

    /// <summary>
    /// Handles the <c>textDocument/formatting</c> request from the client when document formatting is requested.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// <para>
    /// Unlike most handlers, this does not check whether the symbol table is available: formatting is a purely
    /// textual operation and works even when the document has syntax errors.
    /// </para>
    /// * The current document source is retrieved from the document state manager.  If it is <c>null</c> then <c>null</c> is returned so the editor takes no action.
    /// * The source is formatted by <see cref="SourceFormatter.FormatSource"/>.
    /// * A <see cref="TextEdit"/> is built covering the entire document from position (0, 0) to the end of the last line, with the formatted source as its replacement text.
    /// </remarks>
    /// <returns>
    /// A task resolving to a <see cref="TextEditContainer"/> containing a single edit that replaces the entire
    /// document with the formatted source, or <c>null</c> if the document state is unavailable.
    /// </returns>
    public override Task<TextEditContainer?> Handle(DocumentFormattingParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state is null)
            {
                return Task.FromResult<TextEditContainer?>(null);
            }

            string formattedSource = SourceFormatter.FormatSource(state.Source);
            string[] originalLines = state.Source.Split('\n');
            string lastOriginalLine = originalLines.Length > 0
                ? originalLines[^1].TrimEnd('\r')
                : string.Empty;

            LspRange fullDocumentRange = new(new(0, 0), new(originalLines.Length - 1, lastOriginalLine.Length));
            TextEdit textEdit = new()
            {
                Range = fullDocumentRange,
                NewText = formattedSource
            };

            return Task.FromResult<TextEditContainer?>(new(textEdit));
        }
        catch (Exception)
        {
            return Task.FromResult<TextEditContainer?>(null);
        }
    }
}
