using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.StandardLibrary;

/// <summary>
/// Provides the Standard Library functions in the global namespace.
/// </summary>
/// <remarks>
/// All functions are pure managed code.  Input and output flow exclusively through the
/// <see cref="ITopsyTurvyIO"/> instance supplied by the host per invocation.  The library
/// holds no static IO state and never touches the console directly.
/// </remarks>
public static class Global
{
    /// <summary>
    /// Prints text to the standard output of the current input/output source.
    /// </summary>
    /// <remarks>The library counterpart of the <c>BEHOLD</c> statement.</remarks>
    /// <param name="text">The text to print to standard output.</param>
    /// <param name="withCeremony">A value indicating whether a trailing newline is applied.</param>
    /// <param name="io">The host-supplied input/output implementation.</param>
    [TopsyTurvyFunction(Name = "PreviewBehold", IsPreview = true, KeywordAnalogue = "BEHOLD")]
    public static void PreviewBehold(
        [TopsyTurvyParameter("Text")] string text,
        [TopsyTurvyParameter("WithCeremony")] bool withCeremony,
        ITopsyTurvyIO io) =>
        io.WriteLine(text, suppressNewline: !withCeremony);

    /// <summary>
    /// Reads a single line of text from the standard input of the current input/output source.
    /// </summary>
    /// <remarks>The library counterpart of the <c>PRAY TELL</c> statement.</remarks>
    /// <param name="io">The host-supplied input/output implementation.</param>
    /// <returns>The line read from standard input, or an empty string if no input is available.</returns>
    [TopsyTurvyFunction(Name = "PreviewPrayTell", IsPreview = true, KeywordAnalogue = "PRAY TELL")]
    public static string PreviewPrayTell(ITopsyTurvyIO io) => io.ReadLine();
}
