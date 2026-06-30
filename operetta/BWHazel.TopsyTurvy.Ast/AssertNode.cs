namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an assert statement.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>THE LAW IS &lt;condition&gt; THAT &lt;error-message&gt;</c>
/// assert statement in Topsy Turvy.  It evaluates the <see cref="Condition"/> and, if falsy,
/// throws a <c>TopsyTurvyThrowException</c> with <see cref="ErrorMessage"/> as the payload,
/// the same exception mechanism as <c>A HIDEOUS CURSE ON</c>.  If the condition is truthy no
/// effect occurs.
/// </para>
/// <para>
/// This is a runtime invariant check rather than a test-framework assertion.  The error message
/// is evaluated lazily as it is only evaluated when the condition is falsy.
/// </para>
/// <para>
/// For example, the following Topsy Turvy code asserts that a score is in range:
/// </para>
/// <code>
/// THE LAW IS BOTH PRE-ADAMITE score AND 0 AND LOWER DEGREE score AND 101 THAT "Score must be between 1 and 100"
/// </code>
/// <para>
/// would be represented in the AST as an <see cref="AssertNode"/> with:
/// </para>
/// <code>
/// new AssertNode()
/// {
///     Condition = new PrefixExpressionNode()
///     {
///         Operator = Operator.Both,
///         Arguments =
///         [
///             new PrefixExpressionNode() { Operator = Operator.PreAdamite, /* ... */ },
///             new PrefixExpressionNode() { Operator = Operator.LowerDegree, /* ... */ }
///         ],
///         Span = new() { /* ... */ }
///     },
///     ErrorMessage = new LiteralNode()
///     {
///         Value = "Score must be between 1 and 100",
///         Type = LiteralType.String,
///         Span = new() { /* ... */ }
///     },
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class AssertNode : Statement
{
    /// <summary>
    /// Gets or initialises the condition that must hold for execution to continue without error.
    /// </summary>
    public required Expression Condition { get; init; }

    /// <summary>
    /// Gets or initialises the error message expression evaluated and thrown when the condition is falsy.
    /// </summary>
    public required Expression ErrorMessage { get; init; }
}
