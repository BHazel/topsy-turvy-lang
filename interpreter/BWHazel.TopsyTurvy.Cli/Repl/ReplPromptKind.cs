namespace BWHazel.TopsyTurvy.Cli.Repl;

/// <summary>
/// Defines constants for the REPL prompt styles.
/// </summary>
internal enum ReplPromptKind
{
    /// <summary>The primary single-line entry prompt.</summary>
    SingleLine,

    /// <summary>The multi-line mode prompt shown on each line until a blank line is submitted.</summary>
    MultiLine,

    /// <summary>The block-continuation prompt shown when an open construct is detected.</summary>
    Continuation,
}
