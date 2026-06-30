namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Defines the input and output operations for Topsy Turvy programmes.
/// </summary>
/// <remarks>
/// I/O operations enable Topsy Turvy programmes to interact with the "outside world" via input and output operations.  Each
/// different type of I/O source, such as the console, must implement the <see cref="ITopsyTurvyIO"/> interface which defines
/// the methods for reading and writing text from and to a source.
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
