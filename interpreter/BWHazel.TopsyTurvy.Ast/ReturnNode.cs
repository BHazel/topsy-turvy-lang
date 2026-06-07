namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a return statement inside a function body.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to two statements in Topsy Turvy:
/// * <c>AND SO I FIND</c> which returns a value from a function, setting <see cref="Value"/> to the evaluated expression.
/// * <c>MY DUTY IS PREMATURELY DISCHARGED.</c> which exits a function early with no return value, setting <see cref="Value"/> to <c>null</c>.
/// The function closing keyword, <c>MY DUTY IS DISCHARGED.</c> is not part of the <see cref="ReturnNode"/> and is handled by the parser
/// as the end of a <see cref="FunctionDefinitionNode"/> block.
/// </para>
/// <para>
/// For example, in the following function the return statement of the sum of the number of Conservative and Liberal lords:
/// </para>
/// <code>
/// IT IS MY DUTY TO PERFORM TotalLords UNDER THE TERMS OF Conservatives AND Liberals
///     AND SO I FIND SUM OF Conservatives AND Liberals
/// MY DUTY IS DISCHARGED.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="ReturnNode"/> with:
/// * The <see cref="Value"/> property set to a <see cref="PrefixExpressionNode"/> using the <see cref="Operator"/>.<c>Sum</c> operator, with:
///     * <see cref="PrefixExpressionNode.Arguments"/> containing an <see cref="IdentifierNode"/> for <c>Conservatives</c> and an <see cref="IdentifierNode"/> for <c>Liberals</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new FunctionDefinitionNode()
/// {
///     Name = "TotalLords",
///     Parameters = ["Conservatives", "Liberals"],
///     Body =
///     [
///         new ReturnNode()
///         {
///             Value = new PrefixExpressionNode()
///             {
///                 Operator = Operator.Sum,
///                 Arguments =
///                 [
///                     new IdentifierNode()
///                     {
///                         Name = "Conservatives",
///                         Span = new() { /* ... */ }
///                     },
///                     new IdentifierNode()
///                     {
///                         Name = "Liberals",
///                         Span = new() { /* ... */ }
///                     }
///                 ],
///                 Span = new() { /* ... */ }
///             },
///             Span = new() { /* ... */ }
///         }
///     ],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// An early exit with no return value, <c>MY DUTY IS PREMATURELY DISCHARGED.</c>, produces a <see cref="ReturnNode"/>
/// with <see cref="Value"/> set to <c>null</c>.
/// </para>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class ReturnNode : Statement
{
    /// <summary>
    /// Gets or initialises the expression whose value is returned, or <c>null</c> for a
    /// function without a return value.
    /// </summary>
    public Expression? Value { get; init; }
}
