using System;
using System.Collections.Generic;
using BWHazel.TopsyTurvy.Runtime;
using Spectre.Console;

namespace BWHazel.TopsyTurvy.Cli.Repl;

/// <summary>
/// Provides input and output for the Cadenza REPL that buffers normal output for panel display whilst passing
/// interactive prompts directly to the console.
/// </summary>
/// <remarks>
/// In rich interactive mode, <see cref="ReadLine"/> displays a styled prompt before reading, distinguishing
/// <c>PRAY TELL</c> input from the primary REPL entry prompts.  In `tiptoe` or non-interactive mode, no prompt
/// is shown and <see cref="Console.ReadLine"/> is called directly.
/// </remarks>
/// <param name="isTiptoe">When <c>true</c>, plain-text I/O is used and no styled prompts are shown.</param>
/// <param name="isInteractive">When <c>true</c>, the session is attached to an interactive terminal; when <c>false</c> no prompts are shown.</param>
public sealed class ReplIO(bool isTiptoe = false, bool isInteractive = true) : ITopsyTurvyIO
{
    private const string PrayTellPromptRich = "[bold gold1] ♩ ❯[/] ";

    private readonly List<string> outputBuffer = [];
    private readonly bool isTiptoe = isTiptoe;
    private readonly bool isInteractive = isInteractive;

    /// <summary>
    /// Reads a line of input from the console.
    /// </summary>
    /// <remarks>
    /// In rich interactive mode a styled prompt is shown before reading, distinguishing
    /// <c>PRAY TELL</c> input from the regular REPL entry prompts.
    /// </remarks>
    /// <returns>The line of text entered by the user, or an empty string on end-of-stream.</returns>
    public string ReadLine()
    {
        if (!this.isTiptoe && this.isInteractive)
        {
            AnsiConsole.Markup(PrayTellPromptRich);
        }

        return Console.ReadLine() ?? string.Empty;
    }

    /// <summary>
    /// Buffers a line of programme output for later panel display.
    /// </summary>
    /// <param name="message">The message to output.</param>
    /// <param name="suppressNewline">
    /// When <c>true</c> the message is an interactive inline prompt, e.g. <c>PRAY TELL</c>, and is
    /// written directly to the console rather than buffered, so the user can type a response immediately.
    /// </param>
    public void WriteLine(string message, bool suppressNewline = false)
    {
        if (suppressNewline)
        {
            Console.Write(message);
        }
        else
        {
            this.outputBuffer.Add(message);
        }
    }

    /// <summary>
    /// Returns all buffered output lines and clears the buffer.
    /// </summary>
    /// <returns>The lines written since the last flush, in order.</returns>
    public IReadOnlyList<string> FlushOutput()
    {
        List<string> lines = [.. this.outputBuffer];
        this.outputBuffer.Clear();
        return lines;
    }
}
