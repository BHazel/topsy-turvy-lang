using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a try-catch block.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>WITH THE GREATEST RESPECT,</c> ... <c>THAT CONCLUDES THE MATTER.</c> try-catch block
/// in Topsy Turvy.  It wraps a potentially-failing expression, in <see cref="Operation"/>.  If the operation succeeds,
/// the <see cref="SuccessBlock"/>, <c>WITH GRATITUDE</c>, is executed.  However, if a <see cref="ThrowNode"/>, <c>A HIDEOUS CURSE ON</c>,
/// is raised within the operation the <see cref="ExceptionBlock"/>, <c>MODIFIED RAPTURE</c>, is executed instead.
/// The cursed value is always auto-declared as <see cref="CaughtValueName"/> with type <see cref="LiteralType.String"/>
/// and is scoped to the exception block.
/// </para>
/// <para>
/// For example, the following try-catch block in Topsy Turvy guards a division:
/// </para>
/// <code>
/// WITH THE GREATEST RESPECT, SUMMON RaffleVerdict WITH "Solicitor" IF YOU PLEASE.
///     WITH GRATITUDE
///         BEHOLD "A Blessing!"
///     MODIFIED RAPTURE, Grievance
///         BEHOLD WOVEN OF "A Hideous Curse: " AND Grievance IF YOU PLEASE.
/// THAT CONCLUDES THE MATTER.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="TryCatchNode"/> with:
/// * The <see cref="Operation"/> property set to a <see cref="PrefixExpressionNode"/> for the <c>RaffleVerdict</c> function call.
/// * The <see cref="SuccessBlock"/> property set to a list containing a <see cref="PrintNode"/> for the <c>WITH GRATITUDE</c> branch.
/// * The <see cref="ExceptionBlock"/> property set to a list containing a <see cref="PrintNode"/> for the <c>MODIFIED RAPTURE</c> branch.
/// * The <see cref="CaughtValueName"/> property set to <c>Grievance</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new TryCatchNode()
/// {
///     Operation = new PrefixExpressionNode()
///     {
///         FunctionName = new IdentifierNode() { Name = "RaffleVerdict", Span = new() { /* ... */ } },
///         Arguments = [ new LiteralNode() { Value = "Solicitor", Type = LiteralType.String, Span = new() { /* ... */ } } ],
///         Span = new() { /* ... */ }
///     },
///     SuccessBlock =
///     [
///         new PrintNode() { Expression = new LiteralNode() { Value = "A Blessing!", Type = LiteralType.String, Span = new() { /* ... */ } }, Span = new() { /* ... */ } }
///     ],
///     ExceptionBlock =
///     [
///         new PrintNode() { Expression = new PrefixExpressionNode() { /* WOVEN OF ... */ Span = new() { /* ... */ } }, Span = new() { /* ... */ } }
///     ],
///     CaughtValueName = "Grievance",
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class TryCatchNode : Statement
{
    /// <summary>
    /// Gets or initialises the operation wrapped in the try block.
    /// </summary>
    public required Expression Operation { get; init; }

    /// <summary>
    /// Gets or initialises the block of statements to execute on success.
    /// </summary>
    public required IReadOnlyList<Statement> SuccessBlock { get; init; }

    /// <summary>
    /// Gets or initialises the block of statements to execute on exception.
    /// </summary>
    public required IReadOnlyList<Statement> ExceptionBlock { get; init; }

    /// <summary>
    /// Gets or initialises the name of the variable to bind the caught value to on entering the exception block.
    /// </summary>
    public required string CaughtValueName { get; init; }
}
