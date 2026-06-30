using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// The root node of a Topsy Turvy programme.
/// </summary>
/// <remarks>
/// <para>
/// This is the root of the AST for an entire Topsy Turvy programme.  It corresponds to the outermost
/// <c>HARK!</c> ... <c>FINALE.</c> structure.  Every programme has a <see cref="Title"/> and an optional
/// <see cref="Subtitle"/>, both of which are comments and are not evaluated.  The <see cref="Statements"/> list
/// contains all top-level statements in execution order, including any <see cref="PrincipalBlockNode"/> and
/// <see cref="FunctionDefinitionNode"/> declarations.
/// </para>
/// <para>
/// For example, the following minimal Topsy Turvy programme:
/// </para>
/// <code>
/// HARK! "The Mikado"
///   or, "The Town of Titipu"
/// BEHOLD "The Lord High Executioner!"
/// FINALE.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="ProgramNode"/> with:
/// * The <see cref="Title"/> property set to <c>The Mikado</c>.
/// * The <see cref="Subtitle"/> property set to <c>The Town of Titipu</c>.
/// * The <see cref="Statements"/> property set to a list containing a single <see cref="PrintNode"/> for the <c>BEHOLD</c> statement.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new ProgramNode()
/// {
///     Title = "The Mikado",
///     Subtitle = "The Town of Titipu",
///     Statements =
///     [
///         new PrintNode()
///         {
///             Expression = new LiteralNode()
///             {
///                 Type = LiteralType.String,
///                 Value = "The Lord High Executioner!",
///                 Span = new() { /* ... */ }
///             },
///             Span = new() { /* ... */ }
///         }
///     ],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class ProgramNode : Node
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
    public required IReadOnlyList<Statement> Statements { get; init; }
}
