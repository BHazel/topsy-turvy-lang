using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using BWHazel.TopsyTurvy.Parser;

using AstDiagnostic = BWHazel.TopsyTurvy.Ast.Diagnostic;
using AstDiagnosticSeverity = BWHazel.TopsyTurvy.Ast.DiagnosticSeverity;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles text document synchronisation events and publishes parse diagnostics to the client.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP requests:
/// * <c>textDocument/didChange</c>: The content of a document changes.
/// * <c>textDocument/didClose</c>: A document is closed.
/// * <c>textDocument/didOpen</c>: A document is opened.
/// * <c>textDocument/didSave</c>: A document is saved.
/// </para>
/// <para>
/// This handler is responsible for synchronising text documents between clients, such as a code editor, and the language server.  It
/// is the first stage for a text document, being registered with the language server.  The handler methods are notifications,
/// which is why they return <see cref="Unit"/> as the editor does not wait for a meaningful response.
/// </para>
/// </remarks>
/// <param name="languageServer">The language server facade used to send notifications to the client.</param>
/// <param name="documentStateManager">The manager used to cache per-document symbol state.</param>
public class TextDocumentSyncHandler(ILanguageServerFacade languageServer, DocumentStateManager documentStateManager)
    : TextDocumentSyncHandlerBase
{
    private readonly ILanguageServerFacade languageServer = languageServer;
    private readonly DocumentStateManager documentStateManager = documentStateManager;
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Gets the attributes of a document.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <remarks>
    /// This is called by an editor when opening a document to determine the language ID of the document, therefore to determine
    /// which handler to route text document events to.
    /// </remarks>
    /// <returns>The document attributes, including the document URI and the Topsy Turvy language identifier.</returns>
    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) =>
        new(uri, LanguageServerConstants.LanguageId);

    /// <summary>
    /// Creates the registration options for text document synchronisation.
    /// </summary>
    /// <param name="capability">The text synchronization capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler is configured to instruct the editor to send the full text of a document on change
    /// and to send the full text of a document on save.
    /// </remarks>
    /// <returns>The registration options for text document synchronisation.</returns>
    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId),
            Change = TextDocumentSyncKind.Full,
            Save = new SaveOptions()
            {
                IncludeText = true
            }
        };

    /// <summary>
    /// Handles the <c>textDocument/didOpen</c> request from the client when a text document is opened.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A completed task. This is an LSP notification; no response value is sent to the client.</returns>
    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        this.PublishDiagnostics(request.TextDocument.Uri, request.TextDocument.Text);
        return Unit.Task;
    }

    /// <summary>
    /// Handles the <c>textDocument/didChange</c> request from the client when a text document is changed.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A completed task. This is an LSP notification; no response value is sent to the client.</returns>
    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        string text = request.ContentChanges.LastOrDefault()?.Text ?? string.Empty;
        this.PublishDiagnostics(request.TextDocument.Uri, text);
        return Unit.Task;
    }

    /// <summary>
    /// Handles the <c>textDocument/didSave</c> request from the client when a text document is saved.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A completed task. This is an LSP notification; no response value is sent to the client.</returns>
    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        if (request.Text is not null)
        {
            this.PublishDiagnostics(request.TextDocument.Uri, request.Text);
        }

        return Unit.Task;
    }

    /// <summary>
    /// Handles the <c>textDocument/didClose</c> request from the client when a text document is closed.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A completed task. This is an LSP notification; no response value is sent to the client.</returns>
    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        this.documentStateManager.Remove(request.TextDocument.Uri);
        this.languageServer.TextDocument.PublishDiagnostics(new()
        {
            Uri = request.TextDocument.Uri,
            Diagnostics = new()
        });

        return Unit.Task;
    }

    /// <summary>
    /// Publishes diagnostics for a text document to the client.
    /// </summary>
    /// <param name="uri">The URI of the text document.</param>
    /// <param name="text">The text of the document.</param>
    /// <remarks>
    /// In the event an exception is thrown while publishing diagnostics, the exception is caught and an error message is sent to
    /// the client, preventing the language server from crashing and allowing the user to continue working with the editor.
    /// </remarks>
    private void PublishDiagnostics(DocumentUri uri, string text)
    {
        try
        {
            ParseResult result = this.parser.TryParse(text);
            this.documentStateManager.Update(uri, text, result);
            List<Diagnostic> lspDiagnostics = [.. result.Diagnostics.Select(
                (AstDiagnostic diagnostic) => new Diagnostic()
                {
                    Range = new LspRange(
                        new(diagnostic.Span.Start.Line - 1, diagnostic.Span.Start.Column - 1),
                        new(diagnostic.Span.End.Line - 1, diagnostic.Span.End.Column - 1)),
                    Severity = diagnostic.Severity switch
                    {
                        AstDiagnosticSeverity.Error => DiagnosticSeverity.Error,
                        AstDiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
                        _ => DiagnosticSeverity.Information
                    },
                    Message = diagnostic.Message,
                    Source = LanguageServerConstants.LanguageId
                })];

            this.languageServer.TextDocument.PublishDiagnostics(new()
            {
                Uri = uri,
                Diagnostics = new(lspDiagnostics)
            });
        }
        catch (Exception ex)
        {
            this.languageServer.Window.ShowMessage(new()
            {
                Type = MessageType.Error,
                Message = $"Topsy Turvy LSP error: {ex.Message}"
            });
        }
    }
}
