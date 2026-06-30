using System;
using System.Text;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Pre-processor that handles the Victorian Flourish (<c>~</c>) line-continuation character.
/// </summary>
/// <remarks>
/// This pre-processor transforms original source text by scanning for lines that end with the <c>~</c> line continuation character,
/// removes the continuation character and joins the line with its successor.  For example, the input:
/// <code>
/// I am the very model~
/// of a modern major general.
/// </code>
/// would be transformed to:
/// <code>
/// I am the very model of a modern major general.
/// </code>
/// It should be noted that source mappings are always added for the start of each line in the original source, therefore the
/// <see cref="SourceMapping.OriginalColumn"/> is always 1.
/// </remarks>
public class VictorianFlourishPreProcessor : ITopsyTurvyPreProcessor
{
    /// <summary>
    /// Scans each line of input text for a trailing <c>~</c> and merges the line with its successor.
    /// </summary>
    /// <param name="input">The source text to transform.</param>
    /// <param name="currentSourceMap">The source map to extend.</param>
    /// <returns>A <see cref="PreProcessResult"/> with continuation markers resolved.</returns>
    public PreProcessResult Process(string input, SourceMap currentSourceMap)
    {
        string[] lines = input.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        StringBuilder output = new();
        int currentLine = 1;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            currentSourceMap.AddMapping(output.Length, currentLine, 1);
            if (line.EndsWith('~'))
            {
                output.Append(line.AsSpan(0, line.Length - 1));
            }
            else
            {
                output.AppendLine(line);
            }

            currentLine++;
        }

        return new(output.ToString(), currentSourceMap);
    }
}
