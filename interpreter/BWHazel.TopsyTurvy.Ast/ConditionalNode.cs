using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a conditional statement.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>SHOULD IT TRANSPIRE THAT</c> ... <c>SO MUCH FOR THAT.</c> conditional block and associated 
/// <c>QUITE SO.</c> true branch, <c>OR, IF NOT,</c> else-if branch(es) and <c>OTHERWISE,</c> else branch in Topsy Turvy.
/// Every conditional block has a <see cref="Condition"/>  to evaluate as an <see cref="Expression"/> and a <see cref="TrueBlock"/>
/// block of <see cref="Statement"/>s to execute if the condition is true.  Optionally, a conditional block can have any number
/// of <see cref="ElseIfs"/> else-if branches and an <see cref="ElseBlock"/> block of <see cref="Statement"/> to execute if all
/// conditions were false.  The optional branches are always initialised but will be an empty list if not present.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to evaluate a variable <c>PoemSubject</c> is equal to a specific Yarn (string) literal:
/// </para>
/// <code>
/// SHOULD IT TRANSPIRE THAT ALIKE PoemSubject AND "Hollow"
///     QUITE SO.
///         BEHOLD "Bunthorne!"
///     OR, IF NOT, ALIKE PoemSubject AND "Magnet"
///         BEHOLD "Grosvenor!"
///     OTHERWISE,
///         BEHOLD "Not Aesthetic!"
/// SO MUCH FOR THAT.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="ConditionalNode"/> with:
/// * The <see cref="Condition"/> property set to a <see cref="PrefixExpressionNode"/>, as the condition uses the <see cref="Operator"/><c>.Alike</c> operator, with:
///     * The <see cref="PrefixExpressionNode.Operator"/> property set to <see cref="Operator"/><c>.Alike</c>.
///     * The <see cref="PrefixExpressionNode.Arguments"/> property set to a list containing:
///         * An <see cref="IdentifierNode"/> with <see cref="IdentifierNode.Name"/> set to <c>PoemSubject</c>, the variable being compared.
///         * A <see cref="LiteralNode"/> with <see cref="LiteralNode.Value"/> set to <c>Hollow</c>, the literal value being compared to, and its <see cref="LiteralNode.Type"/> set to <see cref="LiteralType"/><c>.String</c>.
/// * The <see cref="TrueBlock"/> property set to a list containing a single <see cref="PrintNode"/> for the <c>BEHOLD</c> statement.
/// * The <see cref="ElseIfs"/> property set to a list containing a single <see cref="ElseIfBranch"/> with:
///     * The <see cref="ElseIfBranch.Condition"/> property set in a very similar way to the main <see cref="Condition"/> property, but with the literal value set to <c>Magnet</c> instead.
///     * The <see cref="ElseIfBranch.Block"/> property set to a list containing a single <see cref="PrintNode"/> for the <c>BEHOLD</c> statement.
/// * The <see cref="ElseBlock"/> property set to a list containing a single <see cref="PrintNode"/> for the <c>BEHOLD</c> statement.
/// </para>
/// <para>
/// and when parsed would be represented in the AST as:
/// </para>
/// <code>
/// new ConditionalNode()
/// {
///     Condition = new PrefixExpressionNode()
///     {
///         Operator = Operator.Alike,
///         Arguments =
///         [
///             new IdentifierNode()
///             {
///                 Name = "PoemSubject",
///                 Span = new() { /* ... */ }
///             },
///             new LiteralNode()
///             {
///                 Value = "Hollow",
///                 Type = LiteralType.String,
///                 Span = new() { /* ... */ }
///             }
///         ],
///         Span = new() { /* ... */ }
///     },
///     TrueBlock =
///     [
///         new PrintNode() { /* ... */ },
///     ],
///     ElseIfs =
///     [
///         new ElseIfBranch()
///         {
///             Condition = new PrefixExpressionNode() { /* ... */ },
///             Block =
///             [
///                 new PrintNode() { /* ... */ }
///             ],
///         },
///     ],
///     ElseBlock =
///     [
///         new PrintNode() { /* ... */ }
///     ],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class ConditionalNode : Statement
{
    /// <summary>
    /// Gets or initialises the condition to evaluate.
    /// </summary>
    public Expression? Condition { get; init; }

    /// <summary>
    /// Gets or initialises the block of statements to execute if the condition is true.
    /// </summary>
    public required IReadOnlyList<Statement> TrueBlock { get; init; }

    /// <summary>
    /// Gets or initialises the list of else-if branches.
    /// </summary>
    public IReadOnlyList<ElseIfBranch> ElseIfs { get; init; } = new List<ElseIfBranch>().AsReadOnly();

    /// <summary>
    /// Gets or initialises the block to execute if all conditions were false.
    /// </summary>
    public IReadOnlyList<Statement> ElseBlock { get; init; } = new List<Statement>().AsReadOnly();
}
