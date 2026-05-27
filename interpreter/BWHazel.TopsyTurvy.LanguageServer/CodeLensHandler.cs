using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles <c>textDocument/codeLens</c> requests.
/// </summary>
/// <remarks>
/// Emits an inline reference-count annotation above each function and variable declaration.
/// Clicking the lens opens the Find All References panel at the declaration position.
/// Reference counts exclude the declaration line itself, so <c>0 references</c> indicates an
/// unused symbol.  Parameters are excluded as their scope is local to the enclosing function.
/// Symbols with unknown definition positions are also excluded.
/// </remarks>
public class CodeLensHandler : CodeLensHandlerBase
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
    /// Initialises a new instance of the <see cref="CodeLensHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public CodeLensHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override CodeLensRegistrationOptions CreateRegistrationOptions(
        CodeLensCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId),
            ResolveProvider  = false
        };

    /// <inheritdoc/>
    /// <remarks>
    /// Resolve is not used (<c>ResolveProvider = false</c>); lenses are returned fully populated
    /// from <see cref="Handle(CodeLensParams, CancellationToken)"/>.
    /// </remarks>
    public override Task<CodeLens> Handle(CodeLens request, CancellationToken cancellationToken) =>
        Task.FromResult(request);

    /// <inheritdoc/>
    public override Task<CodeLensContainer?> Handle(
        CodeLensParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<CodeLensContainer?>(new CodeLensContainer());
            }

            string source = state.Source;
            string[] lines = source.Split('\n');
            int[] lineOffsets    = BuildLineOffsets(lines);
            List<(int Start, int End)> skipRanges = FindSkipRanges(source);
            List<CodeLens> lenses = [];

            foreach (SymbolInfo symbol in state.SymbolTable.AllSymbols())
            {
                if (symbol.Kind == SymbolKind.Parameter || symbol.DefinitionLine == 0)
                {
                    continue;
                }

                int lspLine = symbol.DefinitionLine - 1;
                int lspChar = symbol.DefinitionColumn - 1;

                int refCount = CountReferences(lines, lineOffsets, skipRanges, symbol.Name, lspLine);

                string title = refCount == 1 ? "1 reference" : $"{refCount} references";

                lenses.Add(new CodeLens
                {
                    Range   = new LspRange(
                        new Position(lspLine, lspChar),
                        new Position(lspLine, lspChar + symbol.Name.Length)),
                    Command = new Command
                    {
                        Title     = title,
                        Name      = "topsy-turvy.showReferences",
                        Arguments = new JArray(
                            JValue.CreateString(request.TextDocument.Uri.ToString()),
                            new JValue(lspLine),
                            new JValue(lspChar))
                    }
                });
            }

            return Task.FromResult<CodeLensContainer?>(new CodeLensContainer(lenses));
        }
        catch (Exception)
        {
            return Task.FromResult<CodeLensContainer?>(new CodeLensContainer());
        }
    }

    /// <summary>
    /// Counts whole-word, case-insensitive occurrences of the symbol name in the
    /// source, excluding the declaration line and any occurrences inside comments or string literals.
    /// </summary>
    /// <remarks>
    /// Excluding the declaration means a count of zero indicates a genuinely unused symbol,
    /// which is immediately visible as <c>0 references</c> in the annotation.
    /// </remarks>
    /// <param name="lines">The source lines.</param>
    /// <param name="lineOffsets">The absolute character offset at which each line begins.</param>
    /// <param name="skipRanges">The absolute offset ranges to exclude from scanning.</param>
    /// <param name="symbolName">The symbol name to count.</param>
    /// <param name="definitionLineIndex">The 0-indexed line on which the symbol is declared; excluded from the count.</param>
    /// <returns>The number of references found outside the declaration line.</returns>
    private static int CountReferences(
        string[] lines,
        int[] lineOffsets,
        List<(int Start, int End)> skipRanges,
        string symbolName,
        int definitionLineIndex)
    {
        int count = 0;
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (lineIndex == definitionLineIndex)
            {
                continue;
            }

            string lineText  = lines[lineIndex];
            int    searchFrom = 0;
            int    foundAt;

            while ((foundAt = lineText.IndexOf(symbolName, searchFrom, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                searchFrom = foundAt + 1;
                if (foundAt > 0 && IsIdentifierChar(lineText[foundAt - 1]))
                {
                    continue;
                }

                int endChar = foundAt + symbolName.Length;
                if (endChar < lineText.Length && IsIdentifierChar(lineText[endChar]))
                {
                    continue;
                }

                int absoluteOffset = lineOffsets[lineIndex] + foundAt;
                if (IsInSkipRange(absoluteOffset, skipRanges))
                {
                    continue;
                }

                count++;
            }
        }

        return count;
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
            current   += lines[i].Length + 1;
        }

        return offsets;
    }

    /// <summary>
    /// Returns source ranges that must be excluded from reference scanning.
    /// </summary>
    /// <param name="source">The full document source text.</param>
    /// <remarks>
    /// This includes block comments, string literals and line comments.
    /// </remarks>
    /// <returns>A list of absolute character offset pairs representing ranges to skip.</returns>
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
    /// <returns><c>true</c> if the offset is inside a skip range; otherwise <c>false</c>.</returns>
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
    /// Determines whether a character is valid inside a Topsy Turvy identifier.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns><c>true</c> if the character is a valid identifier character, otherwise <c>false</c>.</returns>
    private static bool IsIdentifierChar(char character) =>
        char.IsLetterOrDigit(character) || character == '-' || character == '_';
}
