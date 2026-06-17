using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a single case within a switch statement.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to a <c>WHEN ACTING AS</c> case label within an <c>IN WHICH CAPACITY?</c>
/// switch block in Topsy Turvy.  It is always part of a <see cref="SwitchNode"/> and is never used
/// standalone.  Every case consists of a <see cref="Literal"/> value to match against and a <see cref="Block"/> of
/// <see cref="Statement"/>s to execute when it matches.  Cases fall through to subsequent cases unless a
/// <see cref="BreakNode"/>, <c>THAT WILL DO.</c>, is present in the block.  The default case, <c>FAILING ALL OF
/// THE ABOVE,</c>, is not a <see cref="SwitchCase"/> and is stored separately as
/// <see cref="SwitchNode.DefaultBlock"/> on the <see cref="SwitchNode"/>.
/// </para>
/// <para>
/// For example, in the following switch block the cases:
/// </para>
/// <code>
/// IN WHICH CAPACITY? office
///     WHEN ACTING AS "Private Secretary"
///         BEHOLD "Don't stint yourself, do it well."
///         THAT WILL DO.
///     WHEN ACTING AS "Chancellor of the Exchequer"
///         BEHOLD "Due economy is observed."
///         THAT WILL DO.
///     FAILING ALL OF THE ABOVE,
///         BEHOLD "No money, no grovel!"
/// NOTHING COULD BE MORE SATISFACTORY.
/// </code>
/// <para>
/// would be represented in the AST as <see cref="SwitchCase"/>s with:
/// * The <see cref="Literal"/> property set to <c>Private Secretary</c> and <c>Chancellor of the Exchequer</c> respectively.
/// * The <see cref="Block"/> property set to a list containing a <see cref="PrintNode"/> and a <see cref="BreakNode"/> for each case.
/// </para>
/// <para>
/// and when parsed would be represented in the AST as:
/// </para>
/// <code>
/// new SwitchNode()
/// {
///     Expression = new IdentifierNode() { /* ... */ },
///     Cases =
///     [
///         new SwitchCase(
///             "Private Secretary",
///             [
///                 new PrintNode() { /* ... */ },
///                 new BreakNode() { /* ... */ } ]),
///         new SwitchCase(
///             "Chancellor of the Exchequer",
///             [
///                 new PrintNode() { /* ... */ },
///                 new BreakNode() { /* ... */ } ])
///     ],
///     DefaultBlock =
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
/// <param name="Literal">The literal value to match against for this case.</param>
/// <param name="Block">The block of statements to execute if the case matches.  A <see cref="BreakNode"/> anywhere in the block prevents fall-through to subsequent cases.</param>
public record SwitchCase(object? Literal, IReadOnlyList<Statement> Block);
