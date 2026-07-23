using System;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.StandardLibrary.IO;

/// <summary>
/// Provides console-based input and output for Topsy Turvy programmes.
/// </summary>
/// <remarks>
/// This is a basic wrapper around the <see cref="Console"/> input and output operations.
/// </remarks>
public sealed class ConsoleIO : ITopsyTurvyIO
{
    /// <summary>
    /// Reads a line of text from standard input.
    /// </summary>
    /// <returns>The line entered by the user, or an empty string if the end of the input stream is reached.</returns>
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
