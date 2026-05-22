namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Defines the input and output operations for the Topsy Turvy runtime.
/// </summary>
/// <remarks>
/// This abstraction allows the interpreter to be decoupled from the system console.
/// </remarks>
public interface ITopsyTurvyIO
{
    /// <summary>
    /// Reads a single line of text from the input source.
    /// </summary>
    /// <returns>The line of text read from the source.</returns>
    string ReadLine();

    /// <summary>
    /// Writes a line of text to the output source.
    /// </summary>
    /// <param name="message">The text message to write.</param>
    /// <param name="suppressNewline">If <c>true</c>, the trailing newline character is omitted.</param>
    void WriteLine(string message, bool suppressNewline = false);
}
