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
/// Handles <c>textDocument/signatureHelp</c> requests.
/// </summary>
/// <remarks>
/// Displays the parameter list for a function call as the developer types,
/// highlighting the active parameter.
/// </remarks>
public class SignatureHelpHandler : SignatureHelpHandlerBase
{
    private const string LanguageId = "topsy-turvy";
    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// Initialises a new instance of the <see cref="SignatureHelpHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public SignatureHelpHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override SignatureHelpRegistrationOptions CreateRegistrationOptions(
        SignatureHelpCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector  = TextDocumentSelector.ForLanguage(LanguageId),
            TriggerCharacters = new Container<string>(" ")
        };

    /// <inheritdoc/>
    public override Task<SignatureHelp?> Handle(
        SignatureHelpParams request, CancellationToken cancellationToken)
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
            int safeChar = Math.Min(request.Position.Character, lineText.Length);
            string beforeCursor = lineText[..safeChar];

            int summonIndex = beforeCursor.LastIndexOf("SUMMON", StringComparison.OrdinalIgnoreCase);
            if (summonIndex < 0)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            string afterSummon = beforeCursor[(summonIndex + "SUMMON".Length)..].TrimStart();

            if (afterSummon.IndexOf("IF YOU PLEASE.", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            string[] tokens = afterSummon.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
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

            int withIndex = afterSummon.IndexOf(" WITH", StringComparison.OrdinalIgnoreCase);
            if (withIndex < 0)
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            string afterWith = afterSummon[(withIndex + " WITH".Length)..];
            if (afterWith.TrimStart().StartsWith("NOTHING", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<SignatureHelp?>(null);
            }

            int activeParamIndex = CountAndTokens(afterWith);
            int paramCount = info.Parameters?.Count ?? 0;
            if (paramCount > 0)
            {
                activeParamIndex = Math.Min(activeParamIndex, paramCount - 1);
            }

            string label = $"{info.Name}({string.Join(", ", info.Parameters ?? Array.Empty<string>())})";
            List<ParameterInformation> paramInfos = (info.Parameters ?? Array.Empty<string>())
                .Select(static p => new ParameterInformation { Label = p })
                .ToList();

            return Task.FromResult<SignatureHelp?>(new SignatureHelp
            {
                Signatures = new Container<SignatureInformation>(
                    new SignatureInformation
                    {
                        Label      = label,
                        Parameters = new Container<ParameterInformation>(paramInfos)
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
    /// Counts whole-word joining tokens (case-insensitive) in <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>The number of joining tokens found.</returns>
    private static int CountAndTokens(string text)
    {
        int count = 0;
        foreach (string token in text.Split(
            new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Equals("AND", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }
}
