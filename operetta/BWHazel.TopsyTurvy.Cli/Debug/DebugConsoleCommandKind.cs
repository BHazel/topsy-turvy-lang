namespace BWHazel.TopsyTurvy.Cli.Debug;

/// <summary>
/// Defines constants for each command the <c>director</c> debug console accepts.
/// </summary>
internal enum DebugConsoleCommandKind
{
    /// <summary>The input did not match any known command word.</summary>
    Unknown,

    /// <summary>Sets a breakpoint (<c>mark &lt;line&gt;</c>).</summary>
    Mark,

    /// <summary>Removes a breakpoint (<c>unmark &lt;line&gt;</c>).</summary>
    Unmark,

    /// <summary>Resumes execution (<c>proceed</c>).</summary>
    Proceed,

    /// <summary>Steps over one statement (<c>step</c>).</summary>
    Step,

    /// <summary>Steps into a call (<c>enter</c>).</summary>
    Enter,

    /// <summary>Steps out of the current frame (<c>exit</c>).</summary>
    Exit,

    /// <summary>Requests an on-demand pause (<c>pause</c>).</summary>
    Pause,

    /// <summary>Prints the call stack (<c>troupe</c>).</summary>
    Troupe,

    /// <summary>Prints the current line of the selected frame (<c>cue</c>).</summary>
    Cue,

    /// <summary>Prints the source text of the current line of the selected frame (<c>words</c>).</summary>
    Words,

    /// <summary>Prints in-scope variables (<c>armoury</c>).</summary>
    Armoury,

    /// <summary>Evaluates an expression (<c>behold &lt;expr&gt;</c>).</summary>
    Behold,

    /// <summary>Selects a stack frame (<c>scene &lt;id&gt;</c>).</summary>
    Scene,

    /// <summary>Prints the command list (<c>entracte</c>).</summary>
    Entracte,

    /// <summary>Ends the session (<c>finale</c>).</summary>
    Finale
}
