using System;
using System.Collections.Generic;
using System.Linq;

namespace BWHazel.TopsyTurvy.Cli.Debug;

/// <summary>
/// Parses a single debug console input line into a <see cref="DebugConsoleCommand"/>.
/// </summary>
internal static class DebugConsoleCommandParser
{
    /// <summary>
    /// Parses one input line.
    /// </summary>
    /// <param name="input">The raw line typed into the debug console.</param>
    /// <returns>The parsed command; <see cref="DebugConsoleCommandKind.Unknown"/> if the first word matches none of the known commands.</returns>
    internal static DebugConsoleCommand Parse(string input)
    {
        string trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return new(DebugConsoleCommandKind.Unknown, null);
        }

        int spaceIndex = trimmed.IndexOf(' ');
        string word = spaceIndex < 0
            ? trimmed
            : trimmed[..spaceIndex];

        string? argument = spaceIndex < 0
            ? null
            : trimmed[(spaceIndex + 1)..].Trim();

        DebugConsoleCommandKind kind = word switch
        {
            _ when IsWord(word, DebugConsoleConstants.Mark) => DebugConsoleCommandKind.Mark,
            _ when IsWord(word, DebugConsoleConstants.Unmark) => DebugConsoleCommandKind.Unmark,
            _ when IsWord(word, DebugConsoleConstants.Proceed) => DebugConsoleCommandKind.Proceed,
            _ when IsWord(word, DebugConsoleConstants.Step) => DebugConsoleCommandKind.Step,
            _ when IsWord(word, DebugConsoleConstants.Enter) => DebugConsoleCommandKind.Enter,
            _ when IsWord(word, DebugConsoleConstants.Exit) => DebugConsoleCommandKind.Exit,
            _ when IsWord(word, DebugConsoleConstants.Pause) => DebugConsoleCommandKind.Pause,
            _ when IsWord(word, DebugConsoleConstants.Troupe) => DebugConsoleCommandKind.Troupe,
            _ when IsWord(word, DebugConsoleConstants.Cue) => DebugConsoleCommandKind.Cue,
            _ when IsWord(word, DebugConsoleConstants.Words) => DebugConsoleCommandKind.Words,
            _ when IsWord(word, DebugConsoleConstants.Armoury) => DebugConsoleCommandKind.Armoury,
            _ when IsWord(word, DebugConsoleConstants.Behold) => DebugConsoleCommandKind.Behold,
            _ when IsWord(word, DebugConsoleConstants.Scene) => DebugConsoleCommandKind.Scene,
            _ when IsWord(word, DebugConsoleConstants.Entracte) => DebugConsoleCommandKind.Entracte,
            _ when IsWord(word, DebugConsoleConstants.Finale) => DebugConsoleCommandKind.Finale,
            _ => DebugConsoleCommandKind.Unknown
        };

        return new(kind, kind == DebugConsoleCommandKind.Unknown ? trimmed : argument);
    }

    /// <summary>
    /// Determines whether a word matches any alias in the word list of a command, case-insensitively.
    /// </summary>
    /// <param name="word">The word to check.</param>
    /// <param name="aliases">The primary word and aliases of the command.</param>
    /// <returns><c>true</c> if <paramref name="word"/> matches, otherwise <c>false</c>.</returns>
    private static bool IsWord(string word, IReadOnlyList<string> aliases) =>
        aliases.Any(alias => string.Equals(alias, word, StringComparison.OrdinalIgnoreCase));
}
