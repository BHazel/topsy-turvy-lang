using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Displays the parameter list of a function call as the user types, highlighting the active parameter.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/signatureHelp</c>: The client requests signature help at a given position in a text document.
/// </para>
/// <para>
/// In Topsy Turvy, function calls take the form <c>SUMMON functionName WITH arg1 AND arg2 IF YOU PLEASE.</c>
/// The handler parses the text before the cursor to identify the function being called, the arguments typed so
/// far and which parameter is currently active.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class SignatureHelpHandler(DocumentStateManager documentStateManager)
    : SignatureHelpHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for signature help handling.
    /// </summary>
    /// <param name="capability">The signature help capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler is configured to trigger on a space character as function arguments in Topsy Turvy
    /// are space-separated.
    /// </remarks>
    /// <returns>The registration options for signature help handling.</returns>
    protected override SignatureHelpRegistrationOptions CreateRegistrationOptions(SignatureHelpCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId),
            TriggerCharacters = new(" ")
        };

    /// <summary>
    /// Handles the <c>textDocument/signatureHelp</c> request from the client when signature help is requested.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// * The document state is retrieved from the document state manager.  If <c>null</c> or the symbol table is <c>null</c> then <c>null</c> is returned so no signature pop-up is displayed.
    /// * The resolution is entirely delegated to <see cref="SignatureHelpBuilder.Build"/> so both hosts scan the same <c>SUMMON</c> call and resolve the same overload set the same way.
    /// * The resolved candidates are mapped into <see cref="SignatureInformation"/>/<see cref="ParameterInformation"/>, the LSP-specific shapes <see cref="SignatureHelpBuilder"/> has no reference to.
    /// </remarks>
    /// <returns>
    /// A task resolving to a <see cref="SignatureHelp"/> containing every declared overload of the function and the
    /// index of the active signature and parameter, or <c>null</c> if no in-progress function call is detected at
    /// the cursor position.
    /// </returns>
    public override Task<SignatureHelp?> Handle(SignatureHelpParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            SignatureHelpResult? result = SignatureHelpBuilder.Build(
                state.Source,
                request.Position.Line,
                request.Position.Character,
                state.SymbolTable);

            if (result is null)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            return Task.FromResult<SignatureHelp?>(new()
            {
                Signatures = new(result.Signatures.Select(BuildSignatureInformation)),
                ActiveSignature = result.ActiveSignature,
                ActiveParameter = result.ActiveParameter
            });
        }
        catch (Exception)
        {
            return Task.FromResult<SignatureHelp?>(null);
        }
    }

    /// <summary>
    /// Maps a shared <see cref="SignatureCandidate"/> into the LSP-specific <see cref="SignatureInformation"/> shape.
    /// </summary>
    /// <param name="candidate">The candidate to map.</param>
    /// <returns>The built <see cref="SignatureInformation"/>.</returns>
    private static SignatureInformation BuildSignatureInformation(SignatureCandidate candidate) =>
        new()
        {
            Label = candidate.Label,
            Parameters = new(candidate.ParameterLabels.Select(static parameterLabel => new ParameterInformation()
            {
                Label = parameterLabel
            }))
        };
}
