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

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles text document synchronisation events and publishes parse diagnostics to the client.
/// </summary>
public class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
{
    private const string LanguageId = "topsy-turvy";
    private readonly ILanguageServerFacade languageServer;
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Initialises a new instance of the <see cref="TextDocumentSyncHandler"/> class.
    /// </summary>
    /// <param name="languageServer">The language server facade used to send notifications to the client.</param>
    public TextDocumentSyncHandler(ILanguageServerFacade languageServer)
    {
        this.languageServer = languageServer;
    }

    /// <inheritdoc/>
    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) =>
        new(uri, LanguageId);

    /// <inheritdoc/>
    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId),
            Change = TextDocumentSyncKind.Full,
            Save = new SaveOptions { IncludeText = true }
        };

    /// <inheritdoc/>
    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        this.PublishDiagnostics(request.TextDocument.Uri, request.TextDocument.Text);
        return Unit.Task;
    }

    /// <inheritdoc/>
    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        string text = request.ContentChanges.LastOrDefault()?.Text ?? string.Empty;
        this.PublishDiagnostics(request.TextDocument.Uri, text);
        return Unit.Task;
    }

    /// <inheritdoc/>
    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        if (request.Text is not null)
        {
            this.PublishDiagnostics(request.TextDocument.Uri, request.Text);
        }

        return Unit.Task;
    }

    /// <inheritdoc/>
    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        this.languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = request.TextDocument.Uri,
            Diagnostics = new Container<Diagnostic>()
        });

        return Unit.Task;
    }

    private void PublishDiagnostics(DocumentUri uri, string text)
    {
        ParseResult result = this.parser.TryParse(text);
        this.languageServer.Window.ShowMessage(new ShowMessageParams
        {
            Type = MessageType.Info,
            Message = $"Topsy Turvy: parsed, errors={result.Diagnostics.Count}"
        });
        IEnumerable<Diagnostic> lspDiagnostics = result.Diagnostics.Select(
            (AstDiagnostic diagnostic) => new Diagnostic
            {
                Range = new Range(
                    new Position(diagnostic.Span.Start.Line - 1, diagnostic.Span.Start.Column - 1),
                    new Position(diagnostic.Span.End.Line - 1, diagnostic.Span.End.Column - 1)),
                Severity = diagnostic.Severity switch
                {
                    AstDiagnosticSeverity.Error   => DiagnosticSeverity.Error,
                    AstDiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
                    _                             => DiagnosticSeverity.Information
                },
                Message = diagnostic.Message,
                Source  = LanguageId
            });

        this.languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri         = uri,
            Diagnostics = new Container<Diagnostic>(lspDiagnostics)
        });
    }
}
