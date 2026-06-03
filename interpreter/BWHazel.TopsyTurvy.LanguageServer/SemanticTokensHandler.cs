using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using TopsyTurvySymbolKind = BWHazel.TopsyTurvy.Analysis.SymbolKind;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles <c>textDocument/semanticTokens/full</c> requests.
/// </summary>
/// <remarks>
/// Tags each declared identifier in the document with its semantic kind so
/// that editor themes can colour variables, parameters, and functions differently.
/// </remarks>
public class SemanticTokensHandler : SemanticTokensHandlerBase
{
    private const string LanguageId = "topsy-turvy";
    private readonly DocumentStateManager documentStateManager;

    private static readonly SemanticTokensLegend Legend = new()
    {
        TokenTypes = new Container<SemanticTokenType>("variable", "parameter", "function"),
        TokenModifiers = new Container<SemanticTokenModifier>()
    };

    /// <summary>
    /// Initialises a new instance of the <see cref="SemanticTokensHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public SemanticTokensHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId),
            Legend = Legend,
            Full = true,
            Range = false
        };

    /// <inheritdoc/>
    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(
        ITextDocumentIdentifierParams @params, CancellationToken cancellationToken) =>
        Task.FromResult(new SemanticTokensDocument(Legend));

    /// <inheritdoc/>
    protected override Task Tokenize(
        SemanticTokensBuilder builder,
        ITextDocumentIdentifierParams identifier,
        CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(identifier.TextDocument.Uri);
            if (state?.SymbolTable is null || string.IsNullOrEmpty(state.Source))
            {
                return Task.CompletedTask;
            }

            string source = state.Source;
            string[] lines = source.Split('\n');
            List<(int Line, int Char, int Length, string TokenType)> tokens = [];

            IEnumerable<SymbolInfo> allSymbols = state.SymbolTable.AllSymbols()
                .Concat(this.documentStateManager.GetImportedFunctionSymbols(identifier.TextDocument.Uri));

            foreach (SymbolInfo symbol in allSymbols)
            {
                if (symbol.Name.Contains(' '))
                {
                    continue;
                }

                string tokenType = symbol.Kind switch
                {
                    TopsyTurvySymbolKind.Function => "function",
                    TopsyTurvySymbolKind.Parameter => "parameter",
                    _ => "variable"
                };

                foreach ((int lineIndex, int foundAt) in SourceAnalyser.FindWordOccurrences(lines, symbol.Name))
                {
                    tokens.Add((lineIndex, foundAt, symbol.Name.Length, tokenType));
                }
            }

            foreach ((int line, int character, int length, string tokenType) in
                tokens.OrderBy(t => t.Line).ThenBy(t => t.Char))
            {
                builder.Push(line, character, length, tokenType, Array.Empty<string>());
            }
        }
        catch (Exception)
        {
        }

        return Task.CompletedTask;
    }

}
