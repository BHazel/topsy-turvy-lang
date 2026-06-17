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
/// is raised within the operation the <see cref="ExceptionBlock"/>, <c>MODIFIED RAPTURE</c>, is executed instead with the
/// cursed value in the <c>JUST SO</c> implicit variable on entry.
/// </para>
/// <para>
/// For example, the following try-catch block in Topsy Turvy guards a division:
/// </para>
/// <code>
/// WITH THE GREATEST RESPECT, SUMMON RaffleVerdict WITH "Solicitor" IF YOU PLEASE.
///     WITH GRATITUDE
///         BEHOLD "A Blessing!"
///     MODIFIED RAPTURE
///         BEHOLD WOVEN OF "A Hideous Curse: " AND JUST SO IF YOU PLEASE.
/// THAT CONCLUDES THE MATTER.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="TryCatchNode"/> with:
/// * The <see cref="Operation"/> property set to a <see cref="PrefixExpressionNode"/> using the <see cref="Operator"/>.<c>Summon</c> operator for the <c>RaffleVerdict</c> function call.
/// * The <see cref="SuccessBlock"/> property set to a list containing a <see cref="PrintNode"/> for the <c>WITH GRATITUDE</c> branch.
/// * The <see cref="ExceptionBlock"/> property set to a list containing a <see cref="PrintNode"/> for the <c>MODIFIED RAPTURE</c> branch.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new TryCatchNode()
/// {
///     Operation = new PrefixExpressionNode() { /* ... */ },
///     SuccessBlock =
///     [
///         new PrintNode() { /* ... */ }
///     ],
///     ExceptionBlock =
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
}
