using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Parses the content of a Topsy Turvy documentation comment block into a <see cref="DocumentationComment"/>.
/// </summary>
public static class DocumentationCommentParser
{
    private static readonly Regex articlePattern =
        new(@"^ARTICLE\s+(\S+)\s+\(([^)]+)\)\s*:(.*)", RegexOptions.IgnoreCase);

    private static readonly Regex consequencePattern =
        new(@"^CONSEQUENCE\s+\(([^)]+)\)\s*:(.*)", RegexOptions.IgnoreCase);

    private static readonly Regex cursesPattern =
        new(@"^CURSES\s+(\S+)\s+\(([^)]+)\)\s*:(.*)", RegexOptions.IgnoreCase);

    private static readonly Regex statutoryPattern =
        new(@"^STATUTORY\s*:(.*)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Parses the content between the <c>(ASIDE, AT SOME LENGTH:</c> and <c>END OF ASIDE.)</c> markers.
    /// </summary>
    /// <param name="blockContent">The text between the documentation comment delimiters, excluding the markers themselves.</param>
    /// <remarks>
    /// <para>
    /// The parser works line by line through a block comment to extract all the documentation tags and content.  For each line:
    /// * If the line starts with a recognised tag:
    ///     * The parser flushes the current section, if any, committing its content into the appropriate field of the <see cref="DocumentationCommentParseState"/> being built.
    ///     * It then starts a new section for the recognised tag in <see cref="DocumentationCommentParseState"/>.
    ///     * For tags carrying additional data, such as <c>ARTICLE</c> the data is extracted using regular expressions and stored in the parse state.
    ///     * A new <see cref="StringBuilder"/> is created to accumulate the content for the new section.
    /// * If the new line does not start with a recognised tag:
    ///     * The parser appends the line to the current section <see cref="StringBuilder"/>, adding a newline if the accumulator already has content.
    /// * At the end of the block, the parser flushes any remaining section content into the parse state and returns a <see cref="DocumentationComment"/> populated with the recognised tags and their content.
    /// </para>
    /// <para>
    /// When a section is flushed:
    /// * The content accumulated in the <see cref="StringBuilder"/> is trimmed and stored in the appropriate field of the <see cref="DocumentationCommentParseState"/>.
    /// * Where additional data was extracted, such as for <c>ARTICLE</c>, they are also stored in the parse state.
    /// * The current section tracking fields are reset to prepare for the next tag.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A <see cref="DocumentationComment"/> populated from the recognised tags, or <c>null</c> when no recognised
    /// tags are found and the block should be treated as a plain comment.
    /// </returns>
    public static DocumentationComment? Parse(string blockContent)
    {
        DocumentationCommentParseState parseState = new();
        string[] rawLines = blockContent.Split('\n');

        foreach (string rawLine in rawLines)
        {
            string trimmedLine = rawLine.Trim();
            if (trimmedLine.StartsWith("LEGEND:", StringComparison.OrdinalIgnoreCase))
            {
                FlushCurrentSection(parseState);
                parseState.AnyTagFound = true;
                parseState.CurrentSection = "LEGEND";
                parseState.CurrentAccumulator = new(trimmedLine["LEGEND:".Length..].Trim());
                continue;
            }

            if (trimmedLine.StartsWith("RECITATIVE:", StringComparison.OrdinalIgnoreCase))
            {
                FlushCurrentSection(parseState);
                parseState.AnyTagFound = true;
                parseState.CurrentSection = "RECITATIVE";
                parseState.CurrentAccumulator = new(trimmedLine["RECITATIVE:".Length..].Trim());
                continue;
            }

            Match articleMatch = articlePattern.Match(trimmedLine);
            if (articleMatch.Success)
            {
                FlushCurrentSection(parseState);
                parseState.AnyTagFound = true;
                parseState.CurrentSection = "ARTICLE";
                parseState.CurrentArticleName = articleMatch.Groups[1].Value;
                parseState.CurrentArticleType = articleMatch.Groups[2].Value;
                parseState.CurrentAccumulator = new(articleMatch.Groups[3].Value.Trim());
                continue;
            }

            Match consequenceMatch = consequencePattern.Match(trimmedLine);
            if (consequenceMatch.Success)
            {
                FlushCurrentSection(parseState);
                parseState.AnyTagFound = true;
                parseState.CurrentSection = "CONSEQUENCE";
                parseState.CurrentArticleType = consequenceMatch.Groups[1].Value;
                parseState.CurrentAccumulator = new(consequenceMatch.Groups[2].Value.Trim());
                continue;
            }

            Match cursesMatch = cursesPattern.Match(trimmedLine);
            if (cursesMatch.Success)
            {
                FlushCurrentSection(parseState);
                parseState.AnyTagFound = true;
                parseState.CurrentSection = "CURSES";
                parseState.CurrentCursesName = cursesMatch.Groups[1].Value;
                parseState.CurrentCursesType = cursesMatch.Groups[2].Value;
                parseState.CurrentAccumulator = new(cursesMatch.Groups[3].Value.Trim());
                continue;
            }

            if (trimmedLine.StartsWith("CHORUS:", StringComparison.OrdinalIgnoreCase))
            {
                FlushCurrentSection(parseState);
                parseState.AnyTagFound = true;
                parseState.CurrentSection = "CHORUS";
                parseState.CurrentAccumulator = new(trimmedLine["CHORUS:".Length..].Trim());
                continue;
            }

            if (trimmedLine.StartsWith("ENSEMBLE:", StringComparison.OrdinalIgnoreCase))
            {
                FlushCurrentSection(parseState);
                parseState.AnyTagFound = true;
                parseState.CurrentSection = "ENSEMBLE";
                parseState.CurrentAccumulator = new(trimmedLine["ENSEMBLE:".Length..].Trim());
                continue;
            }

            Match statutoryMatch = statutoryPattern.Match(trimmedLine);
            if (statutoryMatch.Success)
            {
                FlushCurrentSection(parseState);
                parseState.AnyTagFound = true;
                parseState.CurrentSection = "STATUTORY";
                parseState.CurrentAccumulator = new StringBuilder(statutoryMatch.Groups[1].Value.Trim());
                continue;
            }

            if (parseState.CurrentAccumulator is not null)
            {
                if (parseState.CurrentAccumulator.Length > 0)
                {
                    parseState.CurrentAccumulator.Append('\n');
                }

                parseState.CurrentAccumulator.Append(trimmedLine);
            }
        }

        FlushCurrentSection(parseState);

        if (!parseState.AnyTagFound)
        {
            return null;
        }

        return new()
        {
            Summary = parseState.Summary,
            Remarks = parseState.RemarksBuilder?.ToString(),
            Parameters = parseState.Parameters,
            ReturnValue = parseState.ReturnValue,
            Exceptions = parseState.Exceptions,
            Examples = parseState.Examples,
            SeeAlso = parseState.SeeAlso,
            IsDeprecated = parseState.IsDeprecated,
            DeprecationMessage = parseState.DeprecationMessage
        };
    }

    /// <summary>
    /// Writes the content accumulated in the provided <see cref="DocumentationCommentParseState"/> into the appropriate output
    /// field, then resets the current section tracking fields ready for the next tag.
    /// </summary>
    private static void FlushCurrentSection(DocumentationCommentParseState parseState)
    {
        if (parseState.CurrentSection is null || parseState.CurrentAccumulator is null)
        {
            return;
        }

        string accumulatedSectionContent = parseState.CurrentAccumulator.ToString().Trim();
        if (parseState.CurrentSection == "LEGEND")
        {
            parseState.Summary = accumulatedSectionContent;
        }
        else if (parseState.CurrentSection == "RECITATIVE")
        {
            parseState.RemarksBuilder = new(accumulatedSectionContent);
        }
        else if (parseState.CurrentSection == "ARTICLE" && parseState.CurrentArticleName is not null && parseState.CurrentArticleType is not null)
        {
            parseState.Parameters ??= new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
            parseState.Parameters[parseState.CurrentArticleName] = (parseState.CurrentArticleType, accumulatedSectionContent);
        }
        else if (parseState.CurrentSection == "CONSEQUENCE")
        {
            parseState.ReturnValue = (parseState.CurrentArticleType ?? string.Empty, accumulatedSectionContent);
        }
        else if (parseState.CurrentSection == "CURSES" && parseState.CurrentCursesName is not null && parseState.CurrentCursesType is not null)
        {
            parseState.Exceptions ??= [];
            parseState.Exceptions.Add((parseState.CurrentCursesName, parseState.CurrentCursesType, accumulatedSectionContent));
        }
        else if (parseState.CurrentSection == "CHORUS")
        {
            parseState.Examples ??= [];
            parseState.Examples.Add(accumulatedSectionContent);
        }
        else if (parseState.CurrentSection == "ENSEMBLE")
        {
            parseState.SeeAlso ??= [];
            if (!string.IsNullOrWhiteSpace(accumulatedSectionContent))
            {
                parseState.SeeAlso.Add(accumulatedSectionContent);
            }
        }
        else if (parseState.CurrentSection == "STATUTORY")
        {
            parseState.IsDeprecated = true;
            parseState.DeprecationMessage = string.IsNullOrWhiteSpace(accumulatedSectionContent)
                ? null
                : accumulatedSectionContent;
        }

        parseState.CurrentSection = null;
        parseState.CurrentAccumulator = null;
        parseState.CurrentArticleName = null;
        parseState.CurrentArticleType = null;
        parseState.CurrentCursesName = null;
        parseState.CurrentCursesType = null;
    }
}
