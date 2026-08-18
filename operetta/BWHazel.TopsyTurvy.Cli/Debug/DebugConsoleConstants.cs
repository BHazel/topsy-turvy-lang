using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Cli.Debug;

/// <summary>
/// Defines the commands and every alias for each <c>director</c> debug console command.
/// </summary>
/// <remarks>
/// The primary command is always the first element in the lists.
/// </remarks>
internal static class DebugConsoleConstants
{
    /// <summary>All accepted words for setting a breakpoint (<c>mark &lt;line&gt;</c>).</summary>
    internal static readonly IReadOnlyList<string> Mark = ["mark", "break", "b"];

    /// <summary>All accepted words for removing a breakpoint (<c>unmark &lt;line&gt;</c>).</summary>
    internal static readonly IReadOnlyList<string> Unmark = ["unmark", "clear"];

    /// <summary>All accepted words for resuming execution (<c>proceed</c>).</summary>
    internal static readonly IReadOnlyList<string> Proceed = ["proceed", "continue", "c"];

    /// <summary>All accepted words for stepping over one statement (<c>step</c>).</summary>
    internal static readonly IReadOnlyList<string> Step = ["step", "s"];

    /// <summary>All accepted words for stepping into a call (<c>enter</c>).</summary>
    internal static readonly IReadOnlyList<string> Enter = ["enter", "stepin", "si"];

    /// <summary>All accepted words for stepping out of the current frame (<c>exit</c>).</summary>
    internal static readonly IReadOnlyList<string> Exit = ["exit", "stepout", "so"];

    /// <summary>The word for an on-demand pause request (<c>pause</c>).</summary>
    internal static readonly IReadOnlyList<string> Pause = ["pause"];

    /// <summary>All accepted words for printing the call stack (<c>troupe</c>).</summary>
    internal static readonly IReadOnlyList<string> Troupe = ["troupe", "stack"];

    /// <summary>All accepted words for printing the current line of the selected frame (<c>cue</c>).</summary>
    internal static readonly IReadOnlyList<string> Cue = ["cue", "line", "l"];

    /// <summary>All accepted words for printing the source text of the current line (<c>words</c>).</summary>
    internal static readonly IReadOnlyList<string> Words = ["words", "code"];

    /// <summary>All accepted words for printing in-scope variables (<c>armoury</c>).</summary>
    internal static readonly IReadOnlyList<string> Armoury = ["armoury", "vars", "v"];

    /// <summary>All accepted words for evaluating an expression (<c>behold &lt;expr&gt;</c>).</summary>
    internal static readonly IReadOnlyList<string> Behold = ["behold", "print", "p"];

    /// <summary>All accepted words for selecting a stack frame (<c>scene &lt;id&gt;</c>).</summary>
    internal static readonly IReadOnlyList<string> Scene = ["scene", "frame", "f"];

    /// <summary>All accepted words for printing the command list (<c>entracte</c>).</summary>
    internal static readonly IReadOnlyList<string> Entracte = ["entracte", "help"];

    /// <summary>All accepted words for ending the session (<c>finale</c>).</summary>
    internal static readonly IReadOnlyList<string> Finale = ["finale", "quit", "q"];
}
