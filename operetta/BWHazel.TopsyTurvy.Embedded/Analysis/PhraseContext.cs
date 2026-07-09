using System;

namespace BWHazel.TopsyTurvy.Embedded.Analysis;

/// <summary>
/// Extracts the phrase and last word preceding a cursor position, for completion filtering.
/// </summary>
/// <remarks>
/// This is a deliberate duplication of <c>CompletionHandler.GetPhraseContext</c> in
/// <c>BWHazel.TopsyTurvy.LanguageServer</c>: that project depends on OmniSharp, which the Embedded
/// project must not reference, so the same small piece of logic is reimplemented here rather than shared.
/// <b>Constraint:</b> if the phrase-parsing rule ever changes, both implementations must be updated
/// together — <c>CompletionHandler.GetPhraseContext</c> carries the same note pointing back here.
/// </remarks>
public static class PhraseContext
{
    /// <summary>
    /// Extracts the trimmed phrase on the current line up to the cursor and the last word within it.
    /// </summary>
    /// <param name="source">The full document source.</param>
    /// <param name="line">The 0-indexed line number.</param>
    /// <param name="character">The 0-indexed character offset.</param>
    /// <returns>The trimmed phrase before the cursor and the last space-delimited word within it.</returns>
    public static (string Phrase, string LastWord) Get(string source, int line, int character)
    {
        string[] lines = source.Split('\n');
        if (line < 0 || line >= lines.Length)
        {
            return (string.Empty, string.Empty);
        }

        string lineText = lines[line];
        int safeCharacter = Math.Min(Math.Max(character, 0), lineText.Length);
        string textBeforeCursor = lineText[..safeCharacter];

        string phrase = textBeforeCursor.TrimStart();
        int lastSpaceIndex = phrase.LastIndexOf(' ');
        string lastWord = lastSpaceIndex >= 0
            ? phrase[(lastSpaceIndex + 1)..]
            : phrase;

        return (phrase, lastWord);
    }
}
