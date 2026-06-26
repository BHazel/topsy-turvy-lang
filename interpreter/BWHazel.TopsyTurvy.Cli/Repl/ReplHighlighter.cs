using System;
using System.Collections.Generic;
using System.Text;
using Spectre.Console;

namespace BWHazel.TopsyTurvy.Cli.Repl;

/// <summary>
/// Produces markup for a single line of Topsy Turvy source text, applying
/// token-category colours consistent with the rest of the CLI colour palette.
/// </summary>
/// <remarks>
/// <para>
/// The tokeniser is a greedy left-to-right scanner. At each position it tries, in order:
/// * Comments
/// * String Literals
/// * Character Literals
/// * Numeric Literals
/// * Keywords (Longest Match First)
/// * Falls back to emitting the character without any colour markup.
/// </para>
/// <para>
/// The result is a markup string suitable for passing directly to <c>AnsiConsole.Markup</c>.
/// </para>
/// </remarks>
public static class ReplHighlighter
{
    private static readonly (string Pattern, string Colour)[] Keywords;

    /// <summary>
    /// Initialises the static keyword table, sorted longest-first for greedy longest-match.
    /// </summary>
    static ReplHighlighter()
    {
        List<(string Pattern, string Colour)> entries =
        [
            ("HARK!",                                   "bold green3"),
            ("FINALE.",                                 "bold green3"),
            ("PRINCIPALS",                              "bold green3"),
            ("THE CURTAIN RISES.",                      "bold green3"),
            ("SHOULD IT TRANSPIRE THAT",                "deepskyblue1"),
            ("QUITE SO.",                               "deepskyblue1"),
            ("OR, IF NOT,",                             "deepskyblue1"),
            ("OTHERWISE,",                              "deepskyblue1"),
            ("SO MUCH FOR THAT.",                       "deepskyblue1"),
            ("IN WHICH CAPACITY?",                      "deepskyblue1"),
            ("WHEN ACTING AS",                          "deepskyblue1"),
            ("FAILING ALL OF THE ABOVE,",               "deepskyblue1"),
            ("NOTHING COULD BE MORE SATISFACTORY.",     "deepskyblue1"),
            ("BY A LEGAL FICTION",                      "deepskyblue1"),
            ("KNOWN AS",                                "deepskyblue1"),
            ("ASCENDING",                               "deepskyblue1"),
            ("DESCENDING",                              "deepskyblue1"),
            ("WHILST",                                  "deepskyblue1"),
            ("UNTIL",                                   "deepskyblue1"),
            ("THAT WILL DO.",                           "deepskyblue1"),
            ("ONCE MORE.",                              "deepskyblue1"),
            ("THE TERM EXPIRES.",                       "deepskyblue1"),
            ("WITH THE GREATEST RESPECT,",              "deepskyblue1"),
            ("MODIFIED RAPTURE",                        "deepskyblue1"),
            ("ALTERED CIRCUMSTANCES",                   "deepskyblue1"),
            ("UNDER ANY CIRCUMSTANCES",                 "deepskyblue1"),
            ("THAT CONCLUDES THE MATTER.",              "deepskyblue1"),
            ("YEOMAN",                                  "deepskyblue1"),
            ("UNDER ORDERS.",                           "deepskyblue1"),
            ("THE LAW IS",                              "deepskyblue1"),
            ("THAT",                                    "deepskyblue1"),
            ("A HIDEOUS CURSE ON",                      "deepskyblue1"),
            ("PRAY WELCOME",                            "mediumpurple1"),
            ("IS APPOINTED",                            "mediumpurple1"),
            ("AS IT WERE",                              "mediumpurple1"),
            ("AS A",                                    "mediumpurple1"),
            ("BEING",                                   "mediumpurple1"),
            ("CONSERVATIVE",                            "mediumpurple1"),
            ("LIBERAL",                                 "mediumpurple1"),
            ("LITTLE LIST OF",                          "cyan"),
            ("STANDING",                               "mediumpurple1"),
            ("PEER",                                    "cyan"),
            ("CHANCELLOR",                              "cyan"),
            ("PIRATE",                                  "cyan"),
            ("SAUSAGE-ROLL",                            "cyan"),
            ("FATHOM",                                  "cyan"),
            ("FOOT",                                    "cyan"),
            ("YARN",                                    "cyan"),
            ("STITCH",                                  "cyan"),
            ("DECREE",                                  "cyan"),
            ("SUM OF",                                  "orange1"),
            ("DIFFERENCE OF",                           "orange1"),
            ("PRODUCT OF",                              "orange1"),
            ("QUOTIENT OF",                             "orange1"),
            ("REMAINDER OF",                            "orange1"),
            ("LARGER OF",                               "orange1"),
            ("SMALLER OF",                              "orange1"),
            ("PRE-ADAMITE",                             "orange1"),
            ("LOWER DEGREE",                            "orange1"),
            ("HARDLY EVER",                             "orange1"),
            ("ALIKE",                                   "orange1"),
            ("UNLIKE",                                  "orange1"),
            ("BOTH",                                    "orange1"),
            ("EITHER",                                  "orange1"),
            ("ALL OF",                                  "orange1"),
            ("ANY OF",                                  "orange1"),
            ("WOVEN OF",                                "orange1"),
            ("TRANSPOSITION DOWN",                      "orange1"),
            ("TRANSPOSITION UP",                        "orange1"),
            ("INVERSION OF",                            "orange1"),
            ("HARMONY OF",                              "orange1"),
            ("DISCORD OF",                              "orange1"),
            ("CHORD OF",                                "orange1"),
            ("RECKONING OF",                            "orange1"),
            ("IF YOU PLEASE.",                          "orange1"),
            ("VICTIM",                                  "orange1"),
            ("AND",                                     "orange1"),
            ("ON",                                      "orange1"),
            ("WITHOUT CEREMONY",                        "lightgreen_1"),
            ("BEHOLD",                                  "lightgreen_1"),
            ("PRAY TELL",                               "lightgreen_1"),
            ("PRAY ADMIT",                              "lightgreen_1"),
            ("IT IS MY DUTY TO PERFORM",                "lightgreen_1"),
            ("MY DUTY IS PREMATURELY DISCHARGED.",      "lightgreen_1"),
            ("MY DUTY IS DISCHARGED.",                  "lightgreen_1"),
            ("UNDER THE TERMS OF",                      "lightgreen_1"),
            ("UNDER NO OBLIGATION",                     "lightgreen_1"),
            ("TO FIND",                                 "lightgreen_1"),
            ("AND SO I FIND",                           "lightgreen_1"),
            ("SUMMON",                                  "lightgreen_1"),
            ("WITH",                                    "lightgreen_1"),
            ("NOTHING",                                 "lightgreen_1"),
            ("VERITY",                                  "gold1"),
            ("NAY",                                     "gold1"),
            ("NAUGHT",                                  "grey"),
            ("THE PROPS",                               "bold cyan"),
        ];

        // Sort longest pattern first for greedy longest-match.
        entries.Sort((a, b) => b.Pattern.Length.CompareTo(a.Pattern.Length));
        Keywords = [.. entries];
    }

    /// <summary>
    /// Converts a single line of Topsy Turvy source text into a markup string
    /// with syntax-category colours applied.
    /// </summary>
    /// <param name="sourceInput">The raw single logical line of source text to highlight.</param>
    /// <returns>The source with markup applied.</returns>
    public static string Highlight(string sourceInput)
    {
        if (string.IsNullOrEmpty(sourceInput))
        {
            return string.Empty;
        }

        StringBuilder output = new(sourceInput.Length * 2);
        int position = 0;
        int length = sourceInput.Length;

        while (position < length)
        {
            // 1. Comment: `ASIDE:` covers the rest of the line.
            if (StartsWithIgnoreCase(sourceInput, position, "ASIDE:"))
            {
                string commentText = sourceInput[position..];
                output.Append("[dim]");
                output.Append(Markup.Escape(commentText));
                output.Append("[/]");
                break;
            }

            // 2. String Literal: Consume from " to the next unescaped ".
            if (sourceInput[position] == '"')
            {
                int stringLiteralEndPosition = position + 1;
                while (stringLiteralEndPosition < length)
                {
                    if (sourceInput[stringLiteralEndPosition] == '~' && stringLiteralEndPosition + 1 < length)
                    {
                        // Skip escape sequence.
                        stringLiteralEndPosition += 2;
                        continue;
                    }

                    if (sourceInput[stringLiteralEndPosition] == '"')
                    {
                        stringLiteralEndPosition++;
                        break;
                    }

                    stringLiteralEndPosition++;
                }

                output.Append("[sandybrown]");
                output.Append(Markup.Escape(sourceInput[position..stringLiteralEndPosition]));
                output.Append("[/]");
                position = stringLiteralEndPosition;
                continue;
            }

            // 3. Character Literal: Consume from ' to the next unescaped '.
            if (sourceInput[position] == '\'')
            {
                int charLiteralEndPosition = position + 1;
                while (charLiteralEndPosition < length)
                {
                    if (sourceInput[charLiteralEndPosition] == '~' && charLiteralEndPosition + 1 < length)
                    {
                        // Skip escape sequence.
                        charLiteralEndPosition += 2;
                        continue;
                    }

                    if (sourceInput[charLiteralEndPosition] == '\'')
                    {
                        charLiteralEndPosition++;
                        break;
                    }

                    charLiteralEndPosition++;
                }

                output.Append("[sandybrown]");
                output.Append(Markup.Escape(sourceInput[position..charLiteralEndPosition]));
                output.Append("[/]");
                position = charLiteralEndPosition;
                continue;
            }

            // 5. Numeric Literal: Digit, or '-' followed immediately by a digit.
            if (char.IsDigit(sourceInput[position]) || (sourceInput[position] == '-' && position + 1 < length && char.IsDigit(sourceInput[position + 1])))
            {
                int numericLiteralEndPosition = position;
                if (sourceInput[numericLiteralEndPosition] == '-')
                {
                    numericLiteralEndPosition++;
                }

                while (numericLiteralEndPosition < length && (char.IsDigit(sourceInput[numericLiteralEndPosition]) || sourceInput[numericLiteralEndPosition] == '.'))
                {
                    numericLiteralEndPosition++;
                }

                output.Append("[cornsilk1]");
                output.Append(Markup.Escape(sourceInput[position..numericLiteralEndPosition]));
                output.Append("[/]");
                position = numericLiteralEndPosition;
                continue;
            }

            // 6. Keyword: Try each entry in the table, longest first.
            bool isKeywordMatched = false;
            foreach ((string pattern, string colour) in Keywords)
            {
                if (!StartsWithIgnoreCase(sourceInput, position, pattern))
                {
                    continue;
                }

                int afterPatternPosition = position + pattern.Length;

                // Require a word boundary after non-punctuation-terminated keywords.
                if (!IsWordBoundary(sourceInput, afterPatternPosition, pattern))
                {
                    continue;
                }

                output.Append($"[{colour}]");
                output.Append(Markup.Escape(sourceInput[position..afterPatternPosition]));
                output.Append("[/]");
                position = afterPatternPosition;
                isKeywordMatched = true;
                break;
            }

            if (isKeywordMatched)
            {
                continue;
            }

            // 7. Plain Character: Identifier characters, whitespace or punctuation.
            output.Append(Markup.Escape(sourceInput[position].ToString()));
            position++;
        }

        return output.ToString();
    }

    /// <summary>
    /// Determines whether the substring of a provided source, starting at a specified position and having the same length
    /// as the provided pattern, is equal to the pattern in a case-insensitive manner.
    /// </summary>
    /// <param name="source">The source code to search.</param>
    /// <param name="position">The position in the source to start searching for the pattern.</param>
    /// <param name="pattern">The pattern to search for.</param>
    /// <returns><c>true</c> if the source code contains the pattern at the position, otherwise <c>false</c>.</returns>
    private static bool StartsWithIgnoreCase(string source, int position, string pattern)
    {
        if (position + pattern.Length > source.Length)
        {
            return false;
        }

        return source.AsSpan(position, pattern.Length).Equals(pattern.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the character at the specified position in the source is a valid word boundary following a keyword match.
    /// </summary>
    /// <remarks>
    /// Keywords that end with punctuation (., !, ?, ,) are self-delimiting: all others require a non-letter at the boundary,
    /// or end-of-input.
    /// </remarks>
    /// <param name="source">The source code to search.</param>
    /// <param name="position">The position in the source to check for a word boundary.</param>
    /// <param name="pattern">The keyword pattern that was matched.</param>
    /// <returns><c>true</c> if the character at the position is a valid word boundary following the pattern, otherwise <c>false</c>.</returns>
    private static bool IsWordBoundary(string source, int position, string pattern)
    {
        char last = pattern[^1];
        if (last is '.' or '!' or '?' or ',')
        {
            // Punctuation-terminated keyword is self-delimiting.
            return true;
        }

        if (position >= source.Length)
        {
            return true;
        }

        return !char.IsLetter(source[position]);
    }
}
