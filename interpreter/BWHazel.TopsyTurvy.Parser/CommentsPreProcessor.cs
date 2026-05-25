using System;
using System.Text.RegularExpressions;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Pre-processor that strips single-line and block comments.
/// </summary>
public class CommentsPreProcessor : ITopsyTurvyPreProcessor
{
    private static readonly Regex blockComment =
        new(@"\(ASIDE, AT SOME LENGTH:.*?END OF ASIDE\.\)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex lineComment =
        new(@"ASIDE:.*", RegexOptions.IgnoreCase);

    /// <summary>
    /// Strips all comments from the input while preserving line breaks.
    /// </summary>
    /// <param name="input">The source text to transform.</param>
    /// <param name="currentSourceMap">The source map to extend.</param>
    /// <returns>A <see cref="PreProcessResult"/> with comments replaced by whitespace.</returns>
    public PreProcessResult Process(string input, SourceMap currentSourceMap)
    {
        string sourceWithoutBlocks = blockComment.Replace(input, match =>
        {
            string preservedSource = string.Empty;
            foreach (char c in match.Value)
            {
                if (c == '\n')
                {
                    preservedSource += '\n';
                }
            }

            return preservedSource;
        });

        string sourceWithoutLines = lineComment.Replace(sourceWithoutBlocks, string.Empty);
        return new(sourceWithoutLines, currentSourceMap);
    }
}
