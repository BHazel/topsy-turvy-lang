using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// The root node of a Topsy Turvy program.
/// </summary>
public class TopsyTurvyProgramNode : TopsyTurvyNode
{
    /// <summary>
    /// Gets or initialises the title of the program.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets or initialises the optional subtitle explaining the situation.
    /// </summary>
    public string? Subtitle { get; init; }

    /// <summary>
    /// Gets or initialises the list of statements constituting the body of the program.
    /// </summary>
    public required IReadOnlyList<TopsyTurvyStatement> Statements { get; init; }
}
