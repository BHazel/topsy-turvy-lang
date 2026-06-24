using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a guard clause statement.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>YEOMAN &lt;condition&gt; OTHERWISE, &lt;block&gt; UNDER ORDERS.</c>
/// guard clause in Topsy Turvy.  It evaluates the <see cref="Condition"/> and, if falsy, executes
/// the statements in <see cref="ElseBlock"/>.  If truthy, execution falls through with no effect.
/// </para>
/// <para>
/// A guard clause is a compact alternative to a one-sided conditional where the body describes the
/// failure case.  It is conventional to place guard clauses at the beginning of a function or block to
/// assert preconditions before proceeding.
/// </para>
/// <para>
/// For example, the following Topsy Turvy code aborts early when <c>score</c> is negative:
/// </para>
/// <code>
/// YEOMAN PRE-ADAMITE score AND 0
///   OTHERWISE,
///     A HIDEOUS CURSE ON "Score must be positive"
/// UNDER ORDERS.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="GuardNode"/> with:
/// </para>
/// <code>
/// new GuardNode()
/// {
///     Condition = new PrefixExpressionNode()
///     {
///         Operator = Operator.PreAdamite,
///         Arguments =
///         [
///             new IdentifierNode() { Name = "score", /* ... */ },
///             new LiteralNode() { Value = 0, /* ... */ }
///         ],
///         Span = new() { /* ... */ }
///     },
///     ElseBlock =
///     [
///         new ThrowNode()
///         {
///             Value = new LiteralNode()
///             {
///                 Value = "Score must be positive", /* ... */ },
///                 Span = new() { /* ... */ }
///             }
///         }
///     ],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class GuardNode : Statement
{
    /// <summary>
    /// Gets or initialises the condition that must hold for execution to fall through.
    /// </summary>
    /// <remarks>
    /// When the condition is truthy the <see cref="ElseBlock"/> is skipped; when falsy the block is executed.
    /// </remarks>
    public required Expression Condition { get; init; }

    /// <summary>
    /// Gets or initialises the statements executed when the condition is falsy.
    /// </summary>
    public required IReadOnlyList<Statement> ElseBlock { get; init; }
}
