using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using TopsyTurvySymbolKind = BWHazel.TopsyTurvy.Analysis.SymbolKind;

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
    /// * The text before the cursor on the current line is scanned for a <c>SUMMON</c> keyword.  If not found, or the call is already complete (the closing <c>IF YOU PLEASE.</c> is present), <c>null</c> is returned.
    /// * The function name is extracted as the first token following <c>SUMMON</c> and looked up in the symbol table.  If not found or not a function, <c>null</c> is returned.
    /// * If no <c> WITH</c> has been typed yet, or the argument list begins with <c>NOTHING</c> (a zero-argument call), <c>null</c> is returned.
    /// * The active parameter index is determined by counting <c>AND</c> separators in the argument text using <see cref="CountAndTokens"/>, clamped to the last parameter index.
    /// * A <see cref="SignatureHelp"/> is built with the function label and parameter list and returned to the client.
    /// </remarks>
    /// <returns>
    /// A task resolving to a <see cref="SignatureHelp"/> containing the function signature and the index of the
    /// active parameter, or <c>null</c> if no in-progress function call is detected at the cursor position.
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

            string[] lines = state.Source.Split('\n');
            int lineIndex = request.Position.Line;
            if (lineIndex >= lines.Length)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            string lineText = lines[lineIndex];
            int safeCharacter = Math.Min(request.Position.Character, lineText.Length);
            string textBeforeCursor = lineText[..safeCharacter];

            int summonIndex = textBeforeCursor.LastIndexOf("SUMMON", StringComparison.OrdinalIgnoreCase);
            if (summonIndex < 0)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            string afterSummonText = textBeforeCursor[(summonIndex + "SUMMON".Length)..].TrimStart();
            if (afterSummonText.IndexOf("IF YOU PLEASE.", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            string[] tokens = afterSummonText.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            string functionName = tokens[0];
            if (!state.SymbolTable.TryGetSymbol(functionName, out SymbolInfo? info) || info is null
                || info.Kind != TopsyTurvySymbolKind.Function)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            int withIndex = afterSummonText.IndexOf(" WITH", StringComparison.OrdinalIgnoreCase);
            if (withIndex < 0)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            string afterWithText = afterSummonText[(withIndex + " WITH".Length)..];
            if (afterWithText.TrimStart().StartsWith("NOTHING", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            int activeParamIndex = CountAndTokens(afterWithText);
            int paramCount = info.Parameters?.Count ?? 0;
            if (paramCount > 0)
            {
                activeParamIndex = Math.Min(activeParamIndex, paramCount - 1);
            }

            string label = $"{info.Name}({string.Join(", ", info.Parameters ?? Array.Empty<string>())})";
            List<ParameterInformation> parameterInfoEntries = [.. (info.Parameters ?? Array.Empty<string>())
                .Select(static parameter => new ParameterInformation()
                    {
                        Label = parameter
                    })];

            return Task.FromResult<SignatureHelp?>(new()
            {
                Signatures = new(
                    new SignatureInformation()
                    {
                        Label = label,
                        Parameters = new(parameterInfoEntries)
                    }),
                ActiveSignature = 0,
                ActiveParameter = activeParamIndex
            });
        }
        catch (Exception)
        {
            return Task.FromResult<SignatureHelp?>(null);
        }
    }

    /// <summary>
    /// Counts whole-word joining tokens (case-insensitive) in the specified text.
    /// </summary>
    /// <remarks>
    /// Each <c>AND</c> token separates one function argument from the next so the count corresponds to the
    /// zero-based index of the parameter currently being typed.
    /// </remarks>
    /// <param name="text">The text to scan.</param>
    /// <returns>The number of <c>AND</c> tokens found.</returns>
    private static int CountAndTokens(string text)
    {
        int count = 0;
        foreach (string token in text.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Equals("AND", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }
}
