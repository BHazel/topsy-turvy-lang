using System;
using System.Collections.Generic;
using System.Text;
using Spectre.Console;

namespace BWHazel.TopsyTurvy.Cli.Repl;

/// <summary>
/// Reads interactive input from the console for the REPL, providing styled prompts,
/// single-character key processing, cursor navigation, backspace editing, syntax highlighting,
/// and input history navigation via the arrow keys.
/// </summary>
/// <remarks>
/// <para>
/// In single-line _Patter_ mode pressing <c>Enter</c> submits the input buffer immediately.  In multi-line mode
/// pressing <c>Enter</c> on a non-empty line appends a newline and shows the continuation prompt;
/// pressing <c>Enter</c> on a blank line flushes the accumulated input buffer for execution.
/// <c>Ctrl+C</c> and <c>Ctrl+D</c> on an empty buffer return the exit command whereas <c>Ctrl+L</c> returns the clear
/// screen command.
/// </para>
/// <para>
/// <b>Keeping the cursor in sync:</b> At the start of every iteration of the key-reading loop, the terminal
/// cursor column position, measured from the first character of the user input and not from the
/// left edge of the screen, exactly equals <c>cursorPosition</c>.  This holds because
/// <c>Console.ReadKey(intercept: true)</c> never echoes a character or moves the terminal cursor:
/// it captures the keystroke silently.  After <c>ReadKey</c> returns, the terminal cursor is
/// therefore exactly where the previous iteration left it.  Each key handler either updates
/// <c>cursorPosition</c> to match whatever it did to the terminal or issues an explicit redraw
/// that restores the match.  The benefit is any code that needs to move the cursor back to
/// the start of the buffer, e.g. before a full redraw, can emit exactly <c>cursorPosition</c>
/// backspaces with no extra book-keeping required.
/// </para>
/// <para>
/// In Aesthetic mode, syntax highlighting is applied after every buffer mutation via
/// <see cref="ReplHighlighter.Highlight"/>.  The right arrow and End use ANSI CSI cursor-forward
/// sequences (<c>ESC[C</c> / <c>ESC[nC</c>) rather than reprinting characters, so the existing
/// highlight colours are not disrupted.
/// </para>
/// <para>
/// When <c>isTiptoe</c> is <c>true</c>, all markup is replaced by plain-text prompts
/// and character-by-character echoing is used: no syntax highlighting is applied.
/// </para>
/// </remarks>
/// <param name="history">The shared input history list maintained by the REPL session.</param>
/// <param name="isTiptoe">When <c>true</c>, plain-text prompts and plain echoing are used instead of styled markup.</param>
public sealed class ReplInputReader(List<string> history, bool isTiptoe = false)
{
    private const string AestheticSingleLinePrompt = "[bold deepskyblue1] ♪ ❯[/] ";
    private const string AestheticMultiLinePrompt = "[bold mediumpurple1] ♫ ❯[/] ";
    private const string AestheticContinuationPrompt = "[dim grey] ··· ❯[/] ";
    private const string TiptoeSingleLinePrompt = "♪ ❯ ";
    private const string TiptoeMultiLinePrompt = "♫ ❯ ";
    private const string TiptoeContinuationPrompt = "··· ";

    private readonly List<string> history = history;
    private readonly bool isTiptoe = isTiptoe;

    /// <summary>
    /// Reads a complete input from the user, handling styled prompts, history navigation and
    /// multi-line accumulation according to the current mode.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <paramref name="isMultiLineMode"/> is <c>true</c>, <c>Enter</c> on a non-empty line adds it to the accumulation buffer
    /// and shows the multi-line prompt again; <c>Enter</c> on an empty line returns all accumulated lines joined with newlines.
    /// When <c>false</c>, <c>Enter</c> immediately returns the
    /// current line.
    /// </para>
    /// <para>
    /// The result of the method is either:
    /// * The user input, which may span multiple lines when in multi-line mode.
    /// * A control command string: <see cref="ReplConstants.Exit"/> on <c>Ctrl+C</c> or <c>Ctrl+D</c> or
    ///   <see cref="ReplConstants.Begone"/> on <c>Ctrl+L</c>.
    /// </para>
    /// </remarks>
    /// <param name="isMultiLineMode">A value indicating whether the REPL is in multi-line mode.</param>
    /// <returns>
    /// The user input or a control command.
    /// </returns>
    public string ReadInput(bool isMultiLineMode)
    {
        if (!isMultiLineMode)
        {
            return this.ReadSingleLine(ReplPromptKind.SingleLine);
        }

        List<string> accumulatedLines = [];
        while (true)
        {
            string line = this.ReadSingleLine(ReplPromptKind.MultiLine);
            if (line is ReplConstants.Exit or ReplConstants.Begone)
            {
                return line;
            }

            if (string.IsNullOrEmpty(line))
            {
                return string.Join("\n", accumulatedLines);
            }

            accumulatedLines.Add(line);
        }
    }

    /// <summary>
    /// Reads a single continuation line with the continuation prompt, used when the session
    /// detects an open block construct.
    /// </summary>
    /// <returns>The user input or a control command.</returns>
    public string ReadContinuationLine()
    {
        return this.ReadSingleLine(ReplPromptKind.Continuation);
    }

    /// <summary>
    /// Displays a prompt then processes keystrokes one at a time until the user submits the
    /// line or issues a control command.
    /// </summary>
    /// <remarks>
    /// The method maintains two pieces of mutable state: the input buffer, which is the text being
    /// constructed, and the cursor position, the insertion point, 0-based from the first
    /// character of the input.  A third piece of state, the history index, tracks which
    /// history entry is currently displayed: -1 means the user has not navigated into history.
    /// Each key press is dispatched to a dedicated private method that handles one category of
    /// key and returns <c>true</c> if it consumed the key, allowing the loop to continue.
    /// </remarks>
    /// <param name="promptKind">The kind of prompt to display.</param>
    /// <returns>The user input or a control command.</returns>
    private string ReadSingleLine(ReplPromptKind promptKind)
    {
        this.ShowPrompt(promptKind);

        StringBuilder inputBuffer = new();
        int cursorPosition = 0;
        int historyIndex = -1;
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return inputBuffer.ToString();
            }

            string? controlKeyCommand = GetControlKeyCommand(key, inputBuffer);
            if (controlKeyCommand is not null)
            {
                Console.WriteLine();
                return controlKeyCommand;
            }

            if (HandleBackspace(key, inputBuffer, ref cursorPosition, this.isTiptoe))
            {
                continue;
            }

            if (HandleCursorMovement(key, inputBuffer, ref cursorPosition, this.isTiptoe))
            {
                continue;
            }

            if (this.HandleHistoryNavigation(key, inputBuffer, ref cursorPosition, ref historyIndex))
            {
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                InsertCharacter(key.KeyChar, inputBuffer, ref cursorPosition, this.isTiptoe);
            }
        }
    }

    /// <summary>
    /// Gets a control key command string if the key is a recognised control shortcut.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="inputBuffer">The current input buffer, used to check whether it is empty for Ctrl+D.</param>
    /// <returns>
    /// A control key command string if the key is a recognised control shortcut; otherwise <c>null</c>.
    /// </returns>
    private static string? GetControlKeyCommand(ConsoleKeyInfo key, StringBuilder inputBuffer)
    {
        if (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            return ReplConstants.Exit;
        }

        if (key.Key == ConsoleKey.D && key.Modifiers.HasFlag(ConsoleModifiers.Control) && inputBuffer.Length == 0)
        {
            return ReplConstants.Exit;
        }

        if (key.Key == ConsoleKey.L && key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            return ReplConstants.Begone;
        }

        return null;
    }

    /// <summary>
    /// Handles the Backspace key.
    /// </summary>
    /// <remarks>
    /// This removes the character immediately to the left of the cursor then redraws the line (highlighted in Aesthetic mode, or
    /// plain in Tiptoe mode).  On a successful deletion the current cursor position is decremented by one.
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="inputBuffer">The current input buffer.</param>
    /// <param name="cursorPosition">The current cursor position.</param>
    /// <param name="isTiptoe">When <c>true</c>, uses plain character echoing instead of a full redraw.</param>
    /// <returns><c>true</c> if the key was Backspace, otherwise <c>false</c>.</returns>
    private static bool HandleBackspace(ConsoleKeyInfo key, StringBuilder inputBuffer, ref int cursorPosition, bool isTiptoe)
    {
        if (key.Key != ConsoleKey.Backspace)
        {
            return false;
        }

        if (cursorPosition > 0)
        {
            int terminalPositionBeforeDeletion = cursorPosition;
            int previousLength = inputBuffer.Length;
            inputBuffer.Remove(cursorPosition - 1, 1);
            cursorPosition--;

            if (!isTiptoe)
            {
                RedrawHighlighted(
                    inputBuffer,
                    previousLength,
                    fromTerminalPosition: terminalPositionBeforeDeletion,
                    targetPosition: cursorPosition);
            }
            else
            {
                Console.Write("\b");
                string textAfterCursor = inputBuffer.ToString()[cursorPosition..];
                Console.Write(textAfterCursor + " ");
                for (int i = 0; i <= textAfterCursor.Length; i++)
                {
                    Console.Write("\b");
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Handles cursor movement keys (Left and Right Arrows, Home, End) by updating the terminal cursor position
    /// and current cursor position to match.
    /// </summary>
    /// <remarks>
    /// In Aesthetic mode the right-arrow and End keys emit ANSI CSI cursor-forward sequences rather than
    /// reprinting characters, so the existing syntax-highlight colours are preserved.  The input buffer is used
    /// to determine valid movement ranges.  The current cursor position is updated in place.
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="inputBuffer">The current input buffer used to determine valid movement ranges.</param>
    /// <param name="cursorPosition">The current cursor position, updated in place.</param>
    /// <param name="isTiptoe">When <c>true</c>, reprints the character rather than using ANSI sequences.</param>
    /// <returns><c>true</c> if the key was a cursor-movement key, otherwise <c>false</c>.</returns>
    private static bool HandleCursorMovement(
        ConsoleKeyInfo key,
        StringBuilder inputBuffer,
        ref int cursorPosition,
        bool isTiptoe)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow when cursorPosition > 0:
                cursorPosition--;
                Console.Write("\b");
                return true;
            case ConsoleKey.RightArrow when cursorPosition < inputBuffer.Length:
                if (!isTiptoe)
                {
                    // ANSI CSI cursor-forward 1.
                    Console.Write("[C");
                }
                else
                {
                    Console.Write(inputBuffer[cursorPosition]);
                }

                cursorPosition++;
                return true;
            case ConsoleKey.Home when cursorPosition > 0:
                for (int i = 0; i < cursorPosition; i++)
                {
                    Console.Write("\b");
                }

                cursorPosition = 0;
                return true;
            case ConsoleKey.End when cursorPosition < inputBuffer.Length:
                if (!isTiptoe)
                {
                    // ANSI CSI cursor-forward N, where N is the number of characters to move forward.
                    int remainingCharacterCount = inputBuffer.Length - cursorPosition;
                    Console.Write($"[{remainingCharacterCount}C");
                }
                else
                {
                    Console.Write(inputBuffer.ToString()[cursorPosition..]);
                }

                cursorPosition = inputBuffer.Length;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Handles history navigation keys (Up and Down Arrows) by replacing the current input buffer content
    /// with the selected history entry and redrawing the line.
    /// </summary>
    /// <remarks>
    /// The history index tracks which history entry is shown.  -1 means the user is not in the history.  It is incremented on
    /// the Up Arrow, to older entries, and decremented on the Down Arrow, to newer entries.
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="inputBuffer">The current input buffer, replaced in place with the history entry.</param>
    /// <param name="cursorPosition">The current cursor position, updated to the end of the new content.</param>
    /// <param name="historyIndex">The current history index, updated based on the navigation.</param>
    /// <returns><c>true</c> if the key was Up Arrow or Down Arrow, otherwise <c>false</c>.</returns>
    private bool HandleHistoryNavigation(
        ConsoleKeyInfo key,
        StringBuilder inputBuffer,
        ref int cursorPosition,
        ref int historyIndex)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (this.history.Count > 0 && historyIndex < this.history.Count - 1)
                {
                    historyIndex++;
                    cursorPosition = RedrawLine(
                        inputBuffer,
                        this.history[this.history.Count - 1 - historyIndex],
                        cursorPosition,
                        shouldHighlight: !this.isTiptoe);
                }

                return true;
            case ConsoleKey.DownArrow:
                if (historyIndex > 0)
                {
                    historyIndex--;
                    cursorPosition = RedrawLine(
                        inputBuffer,
                        this.history[this.history.Count - 1 - historyIndex],
                        cursorPosition,
                        shouldHighlight: !this.isTiptoe);
                }
                else if (historyIndex == 0)
                {
                    historyIndex = -1;
                    cursorPosition = RedrawLine(inputBuffer, string.Empty, cursorPosition, shouldHighlight: false);
                }

                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Inserts a printable character at the current cursor position and redraws the line,
    /// shifting any existing characters to the right.
    /// </summary>
    /// <param name="character">The character to insert.</param>
    /// <param name="inputBuffer">The current input buffer.</param>
    /// <param name="cursorPosition">The insertion point, incremented by one after the insert.</param>
    /// <param name="isTiptoe">When <c>true</c>, echoes the character and reprints the shifted suffix instead of a full redraw.</param>
    private static void InsertCharacter(
        char character,
        StringBuilder inputBuffer,
        ref int cursorPosition,
        bool isTiptoe)
    {
        int terminalPositionBeforeInsert = cursorPosition;
        int previousLength = inputBuffer.Length;
        inputBuffer.Insert(cursorPosition, character);
        cursorPosition++;

        if (!isTiptoe)
        {
            RedrawHighlighted(
                inputBuffer,
                previousLength,
                fromTerminalPosition: terminalPositionBeforeInsert,
                targetPosition: cursorPosition);
        }
        else
        {
            Console.Write(character);
            string textAfterCursor = inputBuffer.ToString()[cursorPosition..];
            if (textAfterCursor.Length > 0)
            {
                Console.Write(textAfterCursor);
                for (int i = 0; i < textAfterCursor.Length; i++)
                {
                    Console.Write("\b");
                }
            }
        }
    }

    /// <summary>
    /// Displays the prompt appropriate for the given prompt kind.
    /// </summary>
    /// <param name="promptKind">The kind of prompt to display.</param>
    private void ShowPrompt(ReplPromptKind promptKind)
    {
        if (this.isTiptoe)
        {
            string tiptoePrompt = promptKind switch
            {
                ReplPromptKind.MultiLine => TiptoeMultiLinePrompt,
                ReplPromptKind.Continuation => TiptoeContinuationPrompt,
                _ => TiptoeSingleLinePrompt
            };

            Console.Write(tiptoePrompt);
        }
        else
        {
            string aestheticPrompt = promptKind switch
            {
                ReplPromptKind.MultiLine => AestheticMultiLinePrompt,
                ReplPromptKind.Continuation => AestheticContinuationPrompt,
                _ => AestheticSingleLinePrompt
            };

            AnsiConsole.Markup(aestheticPrompt);
        }
    }

    /// <summary>
    /// Replaces the input buffer content on the terminal with specified new content, optionally
    /// with syntax highlighting, and moves the cursor to the end of the new content.
    /// </summary>
    /// <param name="inputBuffer">The input buffer to update in place.</param>
    /// <param name="newContent">The replacement text to display.</param>
    /// <param name="currentCursorPosition">The terminal cursor column before the redraw begins, used to backspace to the start.</param>
    /// <param name="shouldHighlight">When <c>true</c>, renders the new content via the <see cref="ReplHighlighter"/>.</param>
    /// <returns>The new cursor position, always equal to the length of the new content.</returns>
    private static int RedrawLine(
        StringBuilder inputBuffer,
        string newContent,
        int currentCursorPosition,
        bool shouldHighlight)
    {
        int previousLength = inputBuffer.Length;
        inputBuffer.Clear();
        inputBuffer.Append(newContent);

        if (shouldHighlight)
        {
            RedrawHighlighted(
                inputBuffer,
                previousLength,
                fromTerminalPosition: currentCursorPosition,
                targetPosition: newContent.Length);
        }
        else
        {
            for (int i = 0; i < currentCursorPosition; i++)
            {
                Console.Write("\b");
            }

            int eraseLength = Math.Max(previousLength, newContent.Length);
            for (int i = 0; i < eraseLength; i++)
            {
                Console.Write(" ");
            }

            for (int i = 0; i < eraseLength; i++)
            {
                Console.Write("\b");
            }

            Console.Write(newContent);
        }

        return newContent.Length;
    }

    /// <summary>
    /// Erases the current input buffer from the terminal, redraws it with syntax highlighting
    /// and repositions the terminal cursor to the target position.
    /// </summary>
    /// <remarks>
    /// The erase step writes the maximum of either the previous buffer length or the new buffer length to ensure
    /// no stale characters from the old content remain visible after a shrinking mutation.
    /// </remarks>
    /// <param name="inputBuffer">The buffer containing the new content to render.</param>
    /// <param name="previousLength">The visual length of the buffer content before the mutation that triggered this redraw, used to calculate how many characters to erase.</param>
    /// <param name="fromTerminalPosition">The terminal cursor column, equal to the current cursor position, captured at the start of the key-reading iteration (before any buffer mutation).</param>
    /// <param name="targetPosition">The terminal cursor column to land on after the redraw.</param>
    private static void RedrawHighlighted(
        StringBuilder inputBuffer,
        int previousLength,
        int fromTerminalPosition,
        int targetPosition)
    {
        for (int i = 0; i < fromTerminalPosition; i++)
        {
            Console.Write("\b");
        }

        int eraseLength = Math.Max(previousLength, inputBuffer.Length);
        for (int i = 0; i < eraseLength; i++)
        {
            Console.Write(" ");
        }

        for (int i = 0; i < eraseLength; i++)
        {
            Console.Write("\b");
        }

        if (inputBuffer.Length > 0)
        {
            AnsiConsole.Markup(ReplHighlighter.Highlight(inputBuffer.ToString()));
        }

        for (int i = 0; i < inputBuffer.Length - targetPosition; i++)
        {
            Console.Write("\b");
        }
    }
}
