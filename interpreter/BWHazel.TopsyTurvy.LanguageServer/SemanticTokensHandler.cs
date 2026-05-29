using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

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

    private static readonly Regex BlockCommentPattern =
        new(@"\(ASIDE, AT SOME LENGTH:.*?END OF ASIDE\.\)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex StringLiteralPattern =
        new(@"""(?:[^""\\]|\\.)*""", RegexOptions.Singleline);

    private static readonly Regex LineCommentPattern =
        new(@"ASIDE:.*", RegexOptions.IgnoreCase);

    private static readonly SemanticTokensLegend Legend = new()
    {
        TokenTypes  = new Container<SemanticTokenType>("variable", "parameter", "function"),
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
            Legend           = Legend,
            Full             = true,
            Range            = false
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
            int[] lineOffsets = BuildLineOffsets(lines);
            List<(int Start, int End)> commentRanges = FindSkipRanges(source);
            List<(int Line, int Char, int Length, string TokenType)> tokens = [];

            IEnumerable<SymbolInfo> allSymbols = state.SymbolTable.AllSymbols()
                .Concat(this.GetImportedFunctionSymbols(identifier.TextDocument.Uri));

            foreach (SymbolInfo symbol in allSymbols)
            {
                if (symbol.Name.Contains(' '))
                {
                    continue;
                }

                string tokenType = symbol.Kind switch
                {
                    SymbolKind.Function  => "function",
                    SymbolKind.Parameter => "parameter",
                    _                    => "variable"
                };

                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    string lineText = lines[lineIndex];
                    int searchFrom = 0;
                    int foundAt;

                    while ((foundAt = lineText.IndexOf(
                        symbol.Name, searchFrom, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        searchFrom = foundAt + 1;
                        if (foundAt > 0 && IsIdentifierChar(lineText[foundAt - 1]))
                        {
                            continue;
                        }

                        int endChar = foundAt + symbol.Name.Length;
                        if (endChar < lineText.Length && IsIdentifierChar(lineText[endChar]))
                        {
                            continue;
                        }

                        int absoluteOffset = lineOffsets[lineIndex] + foundAt;
                        if (IsInSkipRange(absoluteOffset, commentRanges))
                        {
                            continue;
                        }

                        tokens.Add((lineIndex, foundAt, symbol.Name.Length, tokenType));
                    }
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

    /// <summary>
    /// Returns function symbols declared in all open documents other than the given document.
    /// </summary>
    /// <param name="currentUri">The URI of the document being tokenised which is excluded.</param>
    /// <returns>Function <see cref="SymbolInfo"/> records from every other open document.</returns>
    private IEnumerable<SymbolInfo> GetImportedFunctionSymbols(DocumentUri currentUri)
    {
        string currentKey = currentUri.ToString();
        foreach ((DocumentUri otherUri, DocumentState otherState) in
            this.documentStateManager.AllDocuments())
        {
            if (otherUri.ToString() == currentKey || otherState.SymbolTable is null)
            {
                continue;
            }

            foreach (SymbolInfo symbol in otherState.SymbolTable.AllSymbols())
            {
                if (symbol.Kind == SymbolKind.Function)
                {
                    yield return symbol;
                }
            }
        }
    }

    /// <summary>
    /// Builds an array of absolute offsets for the start of each line.
    /// </summary>
    /// <param name="lines">The lines of the source code.</param>
    /// <remarks>
    /// Allows conversion between absolute offsets and line/character positions.
    /// </remarks>
    /// <returns>An array of absolute offsets for the start of each line.</returns>
    private static int[] BuildLineOffsets(string[] lines)
    {
        int[] offsets = new int[lines.Length];
        int current = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            offsets[i] = current;
            current += lines[i].Length + 1;
        }

        return offsets;
    }

    /// <summary>
    /// Builds a list of source ranges that should be excluded from semantic highlighting.
    /// </summary>
    /// <param name="source">The source code to scan.</param>
    /// <remarks>
    /// Examples include block comments, string literals, and line comments.
    /// </remarks>
    /// <returns>A list of source ranges to exclude from semantic highlighting.</returns>
    private static List<(int Start, int End)> FindSkipRanges(string source)
    {
        List<(int Start, int End)> ranges = [];

        foreach (Match match in BlockCommentPattern.Matches(source))
        {
            ranges.Add((match.Index, match.Index + match.Length));
        }

        foreach (Match match in StringLiteralPattern.Matches(source))
        {
            if (!IsInSkipRange(match.Index, ranges))
            {
                ranges.Add((match.Index, match.Index + match.Length));
            }
        }

        foreach (Match match in LineCommentPattern.Matches(source))
        {
            if (!IsInSkipRange(match.Index, ranges))
            {
                ranges.Add((match.Index, match.Index + match.Length));
            }
        }

        return ranges;
    }

    /// <summary>
    /// Determines whether a given absolute offset falls within any of the specified ranges.
    /// </summary>
    /// <param name="absoluteOffset">The absolute offset to check.</param>
    /// <param name="ranges">The list of ranges to check against.</param>
    /// <returns><c>true</c> if the offset is within any range, otherwise, <c>false</c>.</returns>
    private static bool IsInSkipRange(int absoluteOffset, List<(int Start, int End)> ranges)
    {
        foreach ((int start, int end) in ranges)
        {
            if (absoluteOffset >= start && absoluteOffset < end)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a character is valid within an identifier.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns><c>true</c> if the character is valid within an identifier, otherwise, <c>false</c>.</returns>
    private static bool IsIdentifierChar(char c) =>
        char.IsLetterOrDigit(c) || c == '-' || c == '_';
}
