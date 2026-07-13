using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a function definition.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>IT IS MY DUTY TO PERFORM</c> ... <c>MY DUTY IS DISCHARGED.</c>
/// function definition block in Topsy Turvy.  Every function definition consists of a
/// <see cref="Name"/> for the function, a list of typed <see cref="Parameters"/>, an optional
/// <see cref="ReturnType"/>, and a <see cref="Body"/> of <see cref="Statement"/>s to execute
/// when the function is called.
/// </para>
/// <para>
/// Each parameter carries its name, declared <see cref="LiteralType"/>, and source location as a
/// single <see cref="TypedParameter"/>.  The <see cref="ReturnType"/> is <c>null</c> for void
/// functions (those declared without <c>TO FIND</c>).  When non-null it holds the
/// <see cref="LiteralType"/> that every <see cref="ReturnNode"/> in the body must evaluate to.
/// </para>
/// <para>
/// For example, the following function sums the number of Conservative and Liberal lords:
/// </para>
/// <code>
/// IT IS MY DUTY TO PERFORM TotalLords UNDER THE TERMS OF Conservatives AS A PEER AND Liberals AS A PEER TO FIND PEER
///     AND SO I FIND SUM OF Conservatives AND Liberals
/// MY DUTY IS DISCHARGED.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="FunctionDefinitionNode"/> with:
/// * The <see cref="Name"/> property set to <c>TotalLords</c>.
/// * The <see cref="Parameters"/> property set to a list of two <see cref="TypedParameter"/> records: <c>Conservatives AS A PEER</c> and <c>Liberals AS A PEER</c>.
/// * The <see cref="ReturnType"/> property set to <see cref="LiteralType.Integer"/>.
/// * The <see cref="Body"/> property set to a list containing a single <see cref="ReturnNode"/>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new FunctionDefinitionNode()
/// {
///     Name = "TotalLords",
///     Parameters =
///     [
///         new TypedParameter("Conservatives", LiteralType.Integer, new() { /* ... */ }),
///         new TypedParameter("Liberals", LiteralType.Integer, new() { /* ... */ })
///     ],
///     ReturnType = LiteralType.Integer,
///     Body =
///     [
///         new ReturnNode()
///         {
///             Value = new PrefixExpressionNode()
///             {
///                 Operator = Operator.Sum,
///                 Arguments =
///                 [
///                     new IdentifierNode() { Name = "Conservatives", Span = new() { /* ... */ } },
///                     new IdentifierNode() { Name = "Liberals", Span = new() { /* ... */ } }
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
    /// Gets or initialises the source span of the <see cref="Name"/> identifier itself.
    /// </summary>
    /// <remarks>
    /// <see cref="Node.Span"/> covers the whole <c>IT IS MY DUTY TO PERFORM</c> ... <c>MY DUTY IS DISCHARGED.</c>
    /// statement, starting at that keyword, not the name; use <c>NameSpan</c> when only the position of the
    /// identifier itself is needed, e.g. in <c>SymbolTable</c>.
    /// </remarks>
    public required SourceSpan NameSpan { get; init; }

    /// <summary>
    /// Gets or initialises the list of typed parameters.
    /// </summary>
    public required IReadOnlyList<TypedParameter> Parameters { get; init; }

    /// <summary>
    /// Gets or initialises the declared return type of the function, or <c>null</c> if the function is void.
    /// </summary>
    public LiteralType? ReturnType { get; init; }

    /// <summary>
    /// Gets or initialises the block of statements in the function body.
    /// </summary>
    /// <remarks>
    /// Return statements are represented by <see cref="ReturnNode"/>.
    /// </remarks>
    public required IReadOnlyList<Statement> Body { get; init; }
}
