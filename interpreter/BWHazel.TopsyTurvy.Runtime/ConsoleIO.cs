using System;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Provides console-based input and output for Topsy Turvy programs.
/// </summary>
public sealed class ConsoleIO : ITopsyTurvyIO
{
    /// <summary>
    /// Reads a line of text from standard input.
    /// </summary>
    /// <returns>The line entered by the user, or an empty string for no input.</returns>
    public string ReadLine() => Console.ReadLine() ?? string.Empty;

    /// <summary>
    /// Writes a message to standard output.
    /// </summary>
    /// <param name="message">The text to write.</param>
    /// <param name="suppressNewline">If <c>true</c>, the trailing newline is omitted.</param>
    public void WriteLine(string message, bool suppressNewline = false)
    {
        if (suppressNewline)
        {
            Console.Write(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }
}
