using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents the PRINCIPALS section where global variables are declared.
/// </summary>
public class PrincipalBlockNode : TopsyTurvyStatement
{
    /// <summary>
    /// Gets or initialises the list of variable declarations in the block.
    /// </summary>
    public required IReadOnlyList<DeclarationNode> Declarations { get; init; }
}
