namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an ternary (inline conditional) expression.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>&lt;true-value&gt; SHOULD IT TRANSPIRE THAT &lt;condition&gt; OTHERWISE, &lt;false-value&gt;</c>
/// ternary expression in Topsy Turvy.  It evaluates the <see cref="Condition"/> and if truthy it returns <see cref="TrueValue"/>,
/// otherwise it returns <see cref="FalseValue"/>.  As an <see cref="Expression"/>, a ternary can appear anywhere a value is
/// expected.  When used as a standalone expression statement the result is deposited in the implicit
/// <c>JUST SO</c> variable.
/// </para>
/// <para>
/// For example, the following Topsy Turvy code selects a label based on a numeric variable:
/// </para>
/// <code>
/// label IS APPOINTED "High" SHOULD IT TRANSPIRE THAT PRE-ADAMITE score AND 90 OTHERWISE, "Low"
/// </code>
/// <para>
/// would be represented in the AST as an <see cref="AssignmentNode"/> whose value is a <see cref="TernaryExpressionNode"/>:
/// </para>
/// <code>
/// new TernaryExpressionNode()
/// {
///     TrueValue = new LiteralNode()
///     {
///         Value = "High",
///         Type = LiteralType.String,
///         Span = new() { /* ... */ }
///     },
///     Condition = new PrefixExpressionNode()
///     {
///         Operator = Operator.PreAdamite,
///         Arguments =
///         [
///             new IdentifierNode() { Name = "score", Span = new() { /* ... */ } },
///             new LiteralNode() { Value = 90, Type = LiteralType.Integer, Span = new() { /* ... */ } }
///         ],
///         Span = new() { /* ... */ }
///     },
///     FalseValue = new LiteralNode()
///     {
///         Value = "Low",
///         Type = LiteralType.String,
///         Span = new() { /* ... */ }
///     },
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// It should be noted that the <see cref="TrueValue"/> and <see cref="Condition"/> are restricted to non-ternary expressions.  Only
/// the <see cref="FalseValue"/> may itself be a ternary expression, enabling right-chaining, for example:
/// </para>
/// <code>
/// TrueValue1 SHOULD IT TRANSPIRE THAT Condition1 OTHERWISE, TrueValue2 SHOULD IT TRANSPIRE THAT Condition2 OTHERWISE, FalseValue2
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class TernaryExpressionNode : Expression
{
    /// <summary>
    /// Gets or initialises the value returned when the condition is truthy.
    /// </summary>
    /// <remarks>
    /// Restricted to a non-ternary expression to avoid left-recursion ambiguity.
    /// </remarks>
    public required Expression TrueValue { get; init; }

    /// <summary>
    /// Gets or initialises the condition that determines which branch is taken.
    /// </summary>
    /// <remarks>
    /// Restricted to a non-ternary expression to prevent <c>OTHERWISE,</c> being consumed ambiguously.
    /// </remarks>
    public required Expression Condition { get; init; }

    /// <summary>
    /// Gets or initialises the value returned when the condition is falsy.
    /// </summary>
    /// <remarks>
    /// May itself be a <see cref="TernaryExpressionNode"/> to allow right-chained ternary expressions.
    /// </remarks>
    public required Expression FalseValue { get; init; }
}
