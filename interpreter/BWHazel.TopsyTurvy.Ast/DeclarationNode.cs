namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a variable declaration.
/// </summary>
public class DeclarationNode : Statement
{
    /// <summary>
    /// Gets or initialises the name of the variable being declared.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initialises the type of the variable.
    /// </summary>
    public required LiteralType Type { get; init; }

    /// <summary>
    /// Gets or initialises the optional initial value.
    /// </summary>
    public Expression? InitialValue { get; init; }
}
