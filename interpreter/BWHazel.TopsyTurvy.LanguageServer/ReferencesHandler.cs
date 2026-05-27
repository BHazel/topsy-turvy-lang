using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles <c>textDocument/references</c> requests.
/// </summary>
/// <remarks>
/// Returns every whole-word, case-insensitive occurrence of the symbol under the cursor,
/// skipping occurrences inside comments and string literals.  When the request context
/// specifies that the declaration should be excluded, the occurrence on the definition
/// line is omitted from the result.
/// </remarks>
public class ReferencesHandler : ReferencesHandlerBase
{
    private const string LanguageId = "topsy-turvy";
    private readonly DocumentStateManager documentStateManager;

    private static readonly Regex BlockCommentPattern =
        new(@"\(ASIDE, AT SOME LENGTH:.*?END OF ASIDE\.\)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex StringLiteralPattern =
        new(@"""(?:[^""\\]|\\.)*""", RegexOptions.Singleline);

    private static readonly Regex LineCommentPattern =
        new(@"ASIDE:.*", RegexOptions.IgnoreCase);

    /// <summary>
    /// Initialises a new instance of the <see cref="ReferencesHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public ReferencesHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override ReferenceRegistrationOptions CreateRegistrationOptions(
        ReferenceCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId)
        };

    /// <inheritdoc/>
    public override Task<LocationContainer?> Handle(
        ReferenceParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<LocationContainer?>(null);
            }

            string? word = SymbolTable.ExtractWordAt(
                state.Source,
                request.Position.Line,
                request.Position.Character);

            if (word is null)
            {
                return Task.FromResult<LocationContainer?>(null);
            }

            if (!state.SymbolTable.TryGetSymbol(word, out SymbolInfo? info) || info is null)
            {
                return Task.FromResult<LocationContainer?>(null);
            }

            if (info.Name.Contains(' '))
            {
                return Task.FromResult<LocationContainer?>(null);
            }

            bool includeDeclaration = request.Context?.IncludeDeclaration ?? true;
            int definitionLineIndex = info.DefinitionLine > 0
                ? info.DefinitionLine - 1
                : -1;

            string source = state.Source;
            string[] lines = source.Split('\n');
            int[] lineOffsets = BuildLineOffsets(lines);
            List<(int Start, int End)> skipRanges = FindSkipRanges(source);
            List<Location> locations = [];

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string lineText = lines[lineIndex];
                int searchFrom = 0;
                int foundAt;

                while ((foundAt = lineText.IndexOf(word, searchFrom, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    searchFrom = foundAt + 1;

                    if (foundAt > 0 && IsIdentifierChar(lineText[foundAt - 1]))
                    {
                        continue;
                    }

                    int endChar = foundAt + word.Length;
                    if (endChar < lineText.Length && IsIdentifierChar(lineText[endChar]))
                    {
                        continue;
                    }

                    int absoluteOffset = lineOffsets[lineIndex] + foundAt;
                    if (IsInSkipRange(absoluteOffset, skipRanges))
                    {
                        continue;
                    }

                    if (!includeDeclaration && lineIndex == definitionLineIndex)
                    {
                        continue;
                    }

                    locations.Add(new Location
                    {
                        Uri = request.TextDocument.Uri,
                        Range = new LspRange(
                            new Position(lineIndex, foundAt),
                            new Position(lineIndex, endChar))
                    });
                }
            }

            return Task.FromResult<LocationContainer?>(new LocationContainer(locations));
        }
        catch (Exception)
        {
            return Task.FromResult<LocationContainer?>(null);
        }
    }

    /// <summary>
    /// Builds an array of the absolute character offset at which each line begins.
    /// </summary>
    /// <param name="lines">The source lines.</param>
    /// <returns>An array of absolute start offsets, one per line.</returns>
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
    /// Returns source ranges that must be excluded from reference scanning.
    /// </summary>
    /// <param name="source">The full document source text.</param>
    /// <remarks>
    /// This includes block comments, string literals, and line comments.
    /// </remarks>
    /// <returns>A list of absolute offset pairs of start and end positions to skip.</returns>
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
    /// Determines whether an absolute character offset falls within any skip range.
    /// </summary>
    /// <param name="absoluteOffset">The offset to test.</param>
    /// <param name="ranges">The ranges to test against.</param>
    /// <returns><c>true</c> if the offset is inside a skip range, otherwise <c>false</c>.</returns>
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
    /// Determines if a character is valid inside a Topsy Turvy identifier.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns><c>true</c> if the character is a valid identifier character, otherwise <c>false</c>.</returns>
    private static bool IsIdentifierChar(char character) =>
        char.IsLetterOrDigit(character) || character == '-' || character == '_';
}
