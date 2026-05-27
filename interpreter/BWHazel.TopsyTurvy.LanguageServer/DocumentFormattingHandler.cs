using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles <c>textDocument/formatting</c> requests.
/// </summary>
/// <remarks>
/// Applies two transforms to a Topsy Turvy source file:
/// <list type="number">
///   <item>Keyword casing: All language keywords are normalised to their canonical case.</item>
///   <item>Indentation: Every line is re-indented to 2-space libretto style.</item>
/// </list>
/// The result is returned as a single <see cref="TextEdit"/> replacing the entire document.
/// </remarks>
public class DocumentFormattingHandler : DocumentFormattingHandlerBase
{
    private const string LanguageId = "topsy-turvy";
    private const int IndentWidth = 2;

    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// Keywords that trigger a depth increase after the line is written.
    /// </summary>
    private static readonly string[] PostIncrease1Keywords =
    [
        "PRINCIPALS",
        "IT IS MY DUTY TO PERFORM",
        "BY A LEGAL FICTION",
        "SHOULD IT TRANSPIRE THAT",
        "WITH THE GREATEST RESPECT,",
        "QUITE SO.",
    ];

    /// <summary>
    /// Keywords that trigger a depth increase of 2 after the line is written.
    /// </summary>
    private static readonly string[] PostIncrease2Keywords =
    [
        "IN WHICH CAPACITY?",
    ];

    /// <summary>
    /// Keywords that trigger a depth decrease before the line is written.
    /// </summary>
    private static readonly string[] PreDecrease1Keywords =
    [
        "THE CURTAIN RISES.",
        "MY DUTY IS DISCHARGED.",
        "THE TERM EXPIRES.",
        "THAT CONCLUDES THE MATTER.",
    ];

    /// <summary>
    /// Keywords that trigger a depth decrease of 2 before the line is written.
    /// </summary>
    private static readonly string[] PreDecrease2Keywords =
    [
        "SO MUCH FOR THAT.",
        "NOTHING COULD BE MORE SATISFACTORY.",
    ];

    /// <summary>
    /// Keywords that trigger a depth decrease before the line is written, and a depth increase after.
    /// </summary>
    private static readonly string[] MidBlockKeywords =
    [
        "OR, IF NOT,",
        "OTHERWISE,",
        "WHEN ACTING AS",
        "FAILING ALL OF THE ABOVE,",
        "WITH GRATITUDE",
        "MODIFIED RAPTURE",
    ];

    /// <summary>
    /// All language keywords in their canonical case for normalisation purposes.
    /// </summary>
    private static readonly string[] CanonicalKeywords =
    [
        "NOTHING COULD BE MORE SATISFACTORY.",
        "MY DUTY IS PREMATURELY DISCHARGED.",
        "WITH THE GREATEST RESPECT,",
        "SHOULD IT TRANSPIRE THAT",
        "IT IS MY DUTY TO PERFORM",
        "FAILING ALL OF THE ABOVE,",
        "(ASIDE, AT SOME LENGTH:",
        "THAT CONCLUDES THE MATTER.",
        "BY A LEGAL FICTION",
        "IN WHICH CAPACITY?",
        "UNDER THE TERMS OF",
        "A HIDEOUS CURSE ON",
        "MY DUTY IS DISCHARGED.",
        "THE CURTAIN RISES.",
        "SO MUCH FOR THAT.",
        "THE TERM EXPIRES.",
        "IS HENCEFORTH A",
        "PRAY WELCOME",
        "WITHOUT CEREMONY",
        "MODIFIED RAPTURE",
        "PRAY ADMIT",
        "PRAY TELL",
        "OR, IF NOT,",
        "AND SO I FIND",
        "IS APPOINTED",
        "UNDER NO OBLIGATION",
        "WITH GRATITUDE",
        "WHEN ACTING AS",
        "DIFFERENCE OF",
        "AS IT WERE",
        "LOWER DEGREE",
        "PRE-ADAMITE",
        "PRODUCT OF",
        "QUOTIENT OF",
        "REMAINDER OF",
        "HARDLY EVER",
        "WHEN ACTING AS",
        "QUITE SO.",
        "OTHERWISE,",
        "ASCENDING",
        "DESCENDING",
        "WOVEN OF",
        "BEHOLD",
        "SUMMON",
        "WHILST",
        "FINALE.",
        "DECREE",
        "LARGER OF",
        "SMALLER OF",
        "HARK!",
        "UNTIL",
        "BEING",
        "ALL OF",
        "ANY OF",
        "BOTH",
        "EITHER",
        "ALIKE",
        "UNLIKE",
        "SUM OF",
        "NAUGHT",
        "VERITY",
        "FATHOM",
        "PEER",
        "YARN",
        "NAY",
        "WITH",
        "ONCE MORE.",
        "THAT WILL DO.",
        "PRINCIPALS",
        "AS A",
        "JUST SO",
        "or,",
        "END OF ASIDE.)",
    ];

    private static readonly (Regex Pattern, string Replacement)[] KeywordPatterns =
        BuildKeywordPatterns();

    private static readonly Regex StringLiteralPattern =
        new(@"""(?:[^""\\]|\\.)*""", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex LineCommentPattern =
        new(@"(?i)ASIDE:.*", RegexOptions.Compiled);

    /// <summary>
    /// Initialises a new instance of the <see cref="DocumentFormattingHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public DocumentFormattingHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override DocumentFormattingRegistrationOptions CreateRegistrationOptions(
        DocumentFormattingCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId)
        };

    /// <inheritdoc/>
    public override Task<TextEditContainer?> Handle(
        DocumentFormattingParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state is null)
            {
                return Task.FromResult<TextEditContainer?>(null);
            }

            string formatted = FormatSource(state.Source);

            string[] originalLines = state.Source.Split('\n');
            string lastOriginalLine = originalLines.Length > 0
                ? originalLines[^1].TrimEnd('\r')
                : string.Empty;

            LspRange fullDocumentRange = new(
                new Position(0, 0),
                new Position(originalLines.Length - 1, lastOriginalLine.Length));

            TextEdit edit = new()
            {
                Range   = fullDocumentRange,
                NewText = formatted
            };

            return Task.FromResult<TextEditContainer?>(new TextEditContainer(edit));
        }
        catch (Exception)
        {
            return Task.FromResult<TextEditContainer?>(null);
        }
    }

    /// <summary>
    /// Formats the given Topsy Turvy source text by applying keyword normalisation
    /// and libretto-style indentation.
    /// </summary>
    /// <param name="source">The raw source text.</param>
    /// <returns>The formatted source text.</returns>
    private static string FormatSource(string source)
    {
        string[] lines = source.Split('\n');
        StringBuilder output = new();
        int depth = 0;
        bool inBlockComment = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string rawLine = lines[i].TrimEnd('\r');
            string trimmed = rawLine.Trim();
            bool appendNewline = i < lines.Length - 1;

            if (HandleBlockCommentLine(rawLine, trimmed, ref inBlockComment, output, appendNewline))
            {
                continue;
            }

            if (HandleBlankLine(trimmed, output, appendNewline))
            {
                continue;
            }

            HandleContentLine(trimmed, ref depth, output, appendNewline);
        }

        return output.ToString();
    }

    /// <summary>
    /// Handles a line that is inside a block comment, or that opens a block comment.
    /// </summary>
    /// <param name="rawLine">The raw untrimmed source line.</param>
    /// <param name="trimmed">The trimmed source line.</param>
    /// <param name="inBlockComment">The current block comment state, updated by this method.</param>
    /// <param name="output">The output builder to append to.</param>
    /// <param name="appendNewline">A value indicating whether a new line should be appended after the line.</param>
    /// <remarks>
    /// Block comment lines are written verbatim without indentation or keyword normalisation.
    /// </remarks>
    /// <returns><c>true</c> if the line was consumed as a block comment line, otherwise <c>false</c>.</returns>
    private static bool HandleBlockCommentLine(
        string rawLine,
        string trimmed,
        ref bool inBlockComment,
        StringBuilder output,
        bool appendNewline)
    {
        if (inBlockComment)
        {
            output.Append(rawLine);
            if (appendNewline)
            {
                output.Append('\n');
            }

            if (trimmed.EndsWith("END OF ASIDE.)", StringComparison.OrdinalIgnoreCase))
            {
                inBlockComment = false;
            }

            return true;
        }

        if (trimmed.StartsWith("(ASIDE, AT SOME LENGTH:", StringComparison.OrdinalIgnoreCase))
        {
            inBlockComment = !trimmed.EndsWith("END OF ASIDE.)", StringComparison.OrdinalIgnoreCase);
            output.Append(rawLine);
            if (appendNewline)
            {
                output.Append('\n');
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles a blank line by appending a newline to the output if required.
    /// </summary>
    /// <param name="trimmed">The trimmed source line.</param>
    /// <param name="output">The output builder to append to.</param>
    /// <param name="appendNewline">A value indicating whether a new line should be appended.</param>
    /// <returns><c>true</c> if the line was blank and consumed, otherwise <c>false</c>.</returns>
    private static bool HandleBlankLine(string trimmed, StringBuilder output, bool appendNewline)
    {
        if (trimmed.Length == 0)
        {
            if (appendNewline)
            {
                output.Append('\n');
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles a regular content line.
    /// </summary>
    /// <param name="trimmed">The trimmed source line.</param>
    /// <param name="depth">The current indentation depth; updated by this method.</param>
    /// <param name="output">The output builder to append to.</param>
    /// <param name="appendNewline">A value indicating whether a new line should be appended after the line.</param>
    /// <remarks>
    /// Adjusts the indentation depth, normalises keywords, applies indentation, and appends the result to the output.
    /// </remarks>
    private static void HandleContentLine(
        string trimmed, ref int depth, StringBuilder output, bool appendNewline)
    {
        DepthAction action = ClassifyLine(trimmed);

        switch (action)
        {
            case DepthAction.PreDecrease1:
            case DepthAction.MidBlock:
                depth = Math.Max(0, depth - 1);
                break;

            case DepthAction.PreDecrease2:
                depth = Math.Max(0, depth - 2);
                break;
        }

        string normalisedLine = NormaliseKeywords(trimmed);
        string indentedLine   = ApplyIndentation(normalisedLine, trimmed, depth);

        output.Append(indentedLine);
        if (appendNewline)
        {
            output.Append('\n');
        }

        switch (action)
        {
            case DepthAction.PostIncrease1:
            case DepthAction.MidBlock:
                depth++;
                break;

            case DepthAction.PostIncrease2:
                depth += 2;
                break;
        }
    }

    /// <summary>
    /// Applies libretto-style indentation to a normalised line.
    /// </summary>
    /// <remarks>
    /// Subtitle lines (starting with <c>or,</c>) are always indented by 2 spaces regardless
    /// of the current depth, matching the libretto convention used in the example files.
    /// </remarks>
    /// <param name="normalisedLine">The keyword-normalised line content.</param>
    /// <param name="trimmed">The original trimmed line, used to detect subtitle lines.</param>
    /// <param name="depth">The current indentation depth.</param>
    /// <returns>The indented line.</returns>
    private static string ApplyIndentation(string normalisedLine, string trimmed, int depth)
    {
        if (trimmed.StartsWith("or,", StringComparison.OrdinalIgnoreCase))
        {
            return "  " + normalisedLine;
        }

        return new string(' ', depth * IndentWidth) + normalisedLine;
    }

    /// <summary>
    /// Classifies a trimmed line by the depth action it requires.
    /// </summary>
    /// <param name="trimmedLine">The trimmed source line.</param>
    /// <returns>The <see cref="DepthAction"/> for the line.</returns>
    private static DepthAction ClassifyLine(string trimmedLine)
    {
        foreach (string keyword in PostIncrease1Keywords)
        {
            if (LineStartsWith(trimmedLine, keyword))
            {
                return DepthAction.PostIncrease1;
            }
        }

        foreach (string keyword in PostIncrease2Keywords)
        {
            if (LineStartsWith(trimmedLine, keyword))
            {
                return DepthAction.PostIncrease2;
            }
        }

        foreach (string keyword in PreDecrease1Keywords)
        {
            if (LineStartsWith(trimmedLine, keyword))
            {
                return DepthAction.PreDecrease1;
            }
        }

        foreach (string keyword in PreDecrease2Keywords)
        {
            if (LineStartsWith(trimmedLine, keyword))
            {
                return DepthAction.PreDecrease2;
            }
        }

        foreach (string keyword in MidBlockKeywords)
        {
            if (LineStartsWith(trimmedLine, keyword))
            {
                return DepthAction.MidBlock;
            }
        }

        return DepthAction.None;
    }

    /// <summary>
    /// Normalises all language keywords on a single trimmed line to their canonical case.
    /// </summary>
    /// <param name="trimmedLine">The trimmed source line.</param>
    /// <remarks>
    /// Occurrences inside string literals and line comments are left unchanged.
    /// </remarks>
    /// <returns>The line with keywords in canonical case.</returns>
    private static string NormaliseKeywords(string trimmedLine)
    {
        List<(int Start, int End)> skipRanges = BuildLineSkipRanges(trimmedLine);
        List<(int Start, int End, string Replacement)> replacements = [];

        foreach ((Regex pattern, string replacement) in KeywordPatterns)
        {
            foreach (Match match in pattern.Matches(trimmedLine))
            {
                if (IsInSkipRange(match.Index, skipRanges))
                {
                    continue;
                }

                bool overlaps = replacements.Any(r =>
                    r.Start < match.Index + match.Length && r.End > match.Index);

                if (!overlaps)
                {
                    replacements.Add((match.Index, match.Index + match.Length, replacement));
                }
            }
        }

        if (replacements.Count == 0)
        {
            return trimmedLine;
        }

        replacements.Sort(static (a, b) => b.Start.CompareTo(a.Start));

        StringBuilder sb = new(trimmedLine);
        foreach ((int start, int end, string rep) in replacements)
        {
            sb.Remove(start, end - start);
            sb.Insert(start, rep);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds skip ranges for a single line.
    /// </summary>
    /// <param name="line">The source line.</param>
    /// <remarks>
    /// This includes string literals and single-line comments.
    /// </remarks>
    /// <returns>A list of start and end positions for character offset pairs to skip.</returns>
    private static List<(int Start, int End)> BuildLineSkipRanges(string line)
    {
        List<(int Start, int End)> ranges = [];

        foreach (Match m in StringLiteralPattern.Matches(line))
        {
            ranges.Add((m.Index, m.Index + m.Length));
        }

        Match lineComment = LineCommentPattern.Match(line);
        if (lineComment.Success && !IsInSkipRange(lineComment.Index, ranges))
        {
            ranges.Add((lineComment.Index, line.Length));
        }

        return ranges;
    }

    /// <summary>
    /// Determines whether an offset falls within any skip range.
    /// </summary>
    /// <param name="offset">The character offset to test.</param>
    /// <param name="ranges">The ranges to test against.</param>
    /// <returns><c>true</c> if the offset is within a skip range, otherwise <c>false</c>.</returns>
    private static bool IsInSkipRange(int offset, List<(int Start, int End)> ranges)
    {
        foreach ((int start, int end) in ranges)
        {
            if (offset >= start && offset < end)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a trimmed line starts with a keyword
    /// with a non-identifier character, or end of string, following it.
    /// </summary>
    /// <param name="trimmedLine">The trimmed source line.</param>
    /// <param name="keyword">The keyword to match.</param>
    /// <returns><c>true</c> if the line starts with the keyword, otherwise <c>false</c>.</returns>
    private static bool LineStartsWith(string trimmedLine, string keyword)
    {
        if (!trimmedLine.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (trimmedLine.Length == keyword.Length)
        {
            return true;
        }

        char next = trimmedLine[keyword.Length];
        return !char.IsLetterOrDigit(next) && next != '_' && next != '-';
    }

    /// <summary>
    /// Builds the compiled keyword regular expression patterns, sorted longest-first.
    /// </summary>
    /// <returns>An array of pattern and replacement pairs.</returns>
    private static (Regex Pattern, string Replacement)[] BuildKeywordPatterns()
    {
        return CanonicalKeywords
            .OrderByDescending(static k => k.Length)
            .Select(static k =>
            {
                string escaped = Regex.Escape(k).Replace(@"\ ", @"\s+");
                Regex pattern = new(@"(?i)\b" + escaped, RegexOptions.Compiled);
                return (pattern, k);
            })
            .ToArray();
    }
}
