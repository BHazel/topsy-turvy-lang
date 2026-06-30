using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a switch statement.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>IN WHICH CAPACITY?</c> ... <c>NOTHING COULD BE MORE SATISFACTORY.</c> switch block
/// in Topsy Turvy.  Two forms are supported:
/// * In the **inline form**, the expression to switch on follows immediately after <c>IN WHICH CAPACITY?</c> and is stored in <see cref="Expression"/>.
/// * In the **two-line form**, the expression is evaluated on the preceding line depositing its result in <c>JUST SO</c>, and <c>IN WHICH CAPACITY?</c> appears alone.  In this case <see cref="Expression"/> is <c>null</c>.
/// Every switch consists of a list of <see cref="Cases"/>, <c>WHEN ACTING AS</c>, and an optional <see cref="DefaultBlock"/>,
/// <c>FAILING ALL OF THE ABOVE,</c>.  Cases fall through unless broken with <c>THAT WILL DO.</c>, represented by a <see cref="BreakNode"/>.
/// </para>
/// <para>
/// For example, the following switch in Topsy Turvy which selects a response based on Pooh-Bah's current office:
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
/// would be represented in the AST as a <see cref="SwitchNode"/> with:
/// * The <see cref="Expression"/> property set to an <see cref="IdentifierNode"/> with <see cref="IdentifierNode.Name"/> set to <c>office</c>.
/// * The <see cref="Cases"/> property set to a list of two <see cref="SwitchCase"/>s, one for <c>Private Secretary</c> and one for <c>Chancellor of the Exchequer</c>.
/// * The <see cref="DefaultBlock"/> property set to a list containing a <see cref="PrintNode"/> for the <c>BEHOLD</c> statement.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new SwitchNode()
/// {
///     Expression = new IdentifierNode()
///     {
///         Name = "office",
///         Span = new() { /* ... */ }
///     },
///     Cases =
///     [
///         new SwitchCase("Private Secretary", [ /* ... */ ]),
///         new SwitchCase("Chancellor of the Exchequer",  [ /* ... */ ])
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
public class SwitchNode : Statement
{
    /// <summary>
    /// Gets or initialises the expression to switch on.
    /// </summary>
    public required Expression Expression { get; init; }

    /// <summary>
    /// Gets or initialises the list of cases.
    /// </summary>
    public required IReadOnlyList<SwitchCase> Cases { get; init; }

    /// <summary>
    /// Gets or initialises the default block to execute if no case matches.
    /// </summary>
    public IReadOnlyList<Statement> DefaultBlock { get; init; } = new List<Statement>().AsReadOnly();
}
