using System;
using System.Text.RegularExpressions;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Pre-processor that strips single-line and block comments.
/// </summary>
/// <remarks>
/// This pre-processor removes comments from the source text while preserving line breaks to maintain accurate source mappings.
/// It supports two types of comments:
/// * Line comments: <c>ASIDE:</c>.
/// * Block comments: <c>(ASIDE, AT SOME LENGTH:</c> ... <c>END OF ASIDE.)</c>.
/// 
/// For example, the input:
/// <code>
/// PRAY WELCOME Ko-Ko AS A PEER
/// ASIDE: Ko-Ko is the Lord High Executioner.
/// PRAY WELCOME Titipu AS A YARN
/// (ASIDE, AT SOME LENGTH:
///     The town of Titipu is a small seaside village.
/// END OF ASIDE.)
/// PRAY WELCOME ExecutionsPerformed AS A DECREE
/// </code>
/// would be transformed to:
/// <code>
/// PRAY WELCOME Ko-Ko AS A PEER
/// 
/// PRAY WELCOME Titipu AS A YARN
/// 
/// 
/// 
/// PRAY WELCOME ExecutionsPerformed AS A DECREE
/// </code>
/// As newlines are preserved no source mappings are required in the <see cref="SourceMap"/>.
/// </remarks>
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
    /// <returns>A <see cref="PreProcessResult"/> with comment text removed and newlines preserved.</returns>
    public PreProcessResult Process(string input, SourceMap currentSourceMap)
    {
        string sourceWithoutBlocks = blockComment.Replace(input, match =>
        {
            string preservedSource = string.Empty;
            foreach (char character in match.Value)
            {
                if (character == '\n')
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
