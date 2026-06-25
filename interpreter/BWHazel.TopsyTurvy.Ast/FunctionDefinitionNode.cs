using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a function definition.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>IT IS MY DUTY TO PERFORM</c> ... <c>MY DUTY IS DISCHARGED.</c> function definition block in Topsy Turvy.
/// Every function definition consists of a <see cref="Name"/> for the function, a list of <see cref="Parameters"/>, and a
/// <see cref="Body"/> of <see cref="Statement"/>s to execute when the function is called. Functions optionally return a value via a
/// <see cref="ReturnNode"/> within the body. If no <see cref="ReturnNode"/> is present, the function returns no value.
/// </para>
/// <para>
/// For example, the following function sums the number of Conservative and Liberal lords:
/// </para>
/// <code>
/// IT IS MY DUTY TO PERFORM TotalLords UNDER THE TERMS OF Conservatives AND Liberals
///     AND SO I FIND SUM OF Conservatives AND Liberals
/// MY DUTY IS DISCHARGED.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="FunctionDefinitionNode"/> with:
/// * The <see cref="Name"/> property set to <c>TotalLords</c>.
/// * The <see cref="Parameters"/> property set to a list containing two strings: <c>Conservatives</c> and <c>Liberals</c>.
/// * The <see cref="Body"/> property set to a list containing a single <see cref="ReturnNode"/> for the <c>AND SO I FIND</c> statement.
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
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class FunctionDefinitionNode : Statement
{
    /// <summary>
    /// Gets or initialises the name of the function.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initialises the list of parameters.
    /// </summary>
    public required IReadOnlyList<string> Parameters { get; init; }

    /// <summary>
    /// Gets or initialises the source spans of each parameter in <see cref="Parameters"/>, in the same order.
    /// </summary>
    /// <remarks>
    /// Each entry covers the identifier token of the corresponding parameter as it appeared in the original source
    /// text.  The list has the same length as <see cref="Parameters"/> and is populated by the parser at the same
    /// time as the parameter names.
    /// </remarks>
    public required IReadOnlyList<SourceSpan> ParameterSpans { get; init; }

    /// <summary>
    /// Gets or initialises the block of statements in the function body.
    /// </summary>
    /// <remarks>
    /// Return statements are represented by <see cref="ReturnNode"/>.
    /// </remarks>
    public required IReadOnlyList<Statement> Body { get; init; }
}
