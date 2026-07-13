using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a namespace declaration.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>TOWN</c> namespace declaration in Topsy Turvy.  It declares the
/// namespace all functions in the whole file belong to: once a file contains a <see cref="NamespaceDeclarationNode"/>,
/// every <see cref="FunctionDefinitionNode"/> in that file belongs to the declared namespace instead
/// of the global scope, regardless of the position of the declaration relative to those functions.  A file
/// may contain at most one <see cref="NamespaceDeclarationNode"/> and it is valid only as a top-level
/// statement, never nested inside a function, loop, conditional, or other block body.
/// </para>
/// <para>
/// Every namespace declaration consists of an ordered <see cref="Path"/> of one or more segments.
/// Sub-namespaces are written either with the long-hand <c>WITH DISTRICT</c> phrase or the short-hand
/// <c>*</c> character.  Both forms produce the same <see cref="Path"/> shape.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy declaring a nested namespace:
/// </para>
/// <code>
/// TOWN Accounts WITH DISTRICT Payroll
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="NamespaceDeclarationNode"/> with:
/// * The <see cref="Path"/> property set to <c>["Accounts", "Payroll"]</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new NamespaceDeclarationNode()
/// {
///     Path = ["Accounts", "Payroll"],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class NamespaceDeclarationNode : Statement
{
    /// <summary>
    /// Gets or initialises the ordered namespace path segments.
    /// </summary>
    public required IReadOnlyList<string> Path { get; init; }
}
