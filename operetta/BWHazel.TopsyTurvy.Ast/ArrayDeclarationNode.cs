using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an array variable declaration.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>PRAY WELCOME ... AS A [CONSERVATIVE|LIBERAL] LITTLE LIST OF [size] type [BEING ... IF YOU PLEASE.]</c>
/// array declaration statement in Topsy Turvy.  Every array declaration consists of the <see cref="Name"/> of the variable being
/// declared, the declared <see cref="ElementType"/> of the array elements, an optional <see cref="Size"/> for pre-allocation,
/// an optional <see cref="IsConstant"/> flag set by the <c>CONSERVATIVE</c> or <c>LIBERAL</c> mutability modifier and an optional
/// list of initial values in <see cref="InitialValues"/>.
/// </para>
/// <para>
/// When no initial values are provided and no <see cref="Size"/> is set, the array is initialised as an empty list; it is not set
/// to <c>NAUGHT</c>.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to declare an array variable <c>miscreants</c> with three initial string values:
/// </para>
/// <code>
/// PRAY WELCOME miscreants AS A LITTLE LIST OF YARN BEING "Pooh-Bah" AND "Ko-Ko" AND "Pish-Tush" IF YOU PLEASE.
/// </code>
/// <para>
/// would be represented in the AST as an <see cref="ArrayDeclarationNode"/> with:
/// * The <see cref="Name"/> property set to <c>miscreants</c>.
/// * The <see cref="ElementType"/> property set to <see cref="LiteralType"/><c>.String</c>.
/// * The <see cref="IsConstant"/> property set to <c>false</c>.
/// * The <see cref="InitialValues"/> property containing three <see cref="LiteralNode"/>s for each string value.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new ArrayDeclarationNode()
/// {
///     Name = "miscreants",
///     ElementType = LiteralType.String,
///     Size = null,
///     IsConstant = false,
///     InitialValues =
///     [
///         new LiteralNode() { Type = LiteralType.String, Value = "Pooh-Bah",  Span = new() { /* ... */ } },
///         new LiteralNode() { Type = LiteralType.String, Value = "Ko-Ko",     Span = new() { /* ... */ } },
///         new LiteralNode() { Type = LiteralType.String, Value = "Pish-Tush", Span = new() { /* ... */ } }
///     ],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Alternatively, the following code pre-allocates a sized array of 3 <c>NAUGHT</c> elements:
/// </para>
/// <code>
/// PRAY WELCOME miscreants AS A LITTLE LIST OF 3 YARN
/// </code>
/// <para>
/// would be represented as an <see cref="ArrayDeclarationNode"/> with <see cref="Size"/> set to <c>3</c>:
/// </para>
/// <code>
/// new ArrayDeclarationNode()
/// {
///     Name = "miscreants",
///     ElementType = LiteralType.String,
///     Size = 3,
///     IsConstant = false,
///     InitialValues = [],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class ArrayDeclarationNode : Statement
{
    /// <summary>
    /// Gets or initialises the name of the array variable being declared.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initialises the source span of the <see cref="Name"/> identifier itself.
    /// </summary>
    /// <remarks>
    /// <see cref="Node.Span"/> covers the whole <c>PRAY WELCOME</c> ... statement, starting at that keyword, not
    /// the name; use <c>NameSpan</c> when only the position of the identifier itself is needed, e.g. in <c>SymbolTable</c>.
    /// </remarks>
    public required SourceSpan NameSpan { get; init; }

    /// <summary>
    /// Gets or initialises the element type of the array.
    /// </summary>
    public required LiteralType ElementType { get; init; }

    /// <summary>
    /// Gets or initialises the optional pre-allocation size of the array.
    /// </summary>
    /// <remarks>
    /// When set, the array is pre-allocated with this many <c>NAUGHT</c> elements at runtime.  This allows
    /// element assignment via <c>VICTIM n ON arr IS APPOINTED val</c> without first populating the array
    /// with a <c>BEING</c> clause.  This is mutually exclusive with a non-empty <see cref="InitialValues"/> list as
    /// providing both is a runtime error.  A value of <c>0</c> produces an empty array, equivalent to omitting
    /// the size entirely.  A negative value is a runtime error.
    /// </remarks>
    public int? Size { get; init; }

    /// <summary>
    /// Gets or initialises a value indicating whether this array declaration is a constant.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the array is declared with the <c>CONSERVATIVE</c> modifier.  Neither the variable itself nor
    /// any of its elements may be mutated at runtime.  When <c>false</c>, the array is mutable.
    /// </remarks>
    public bool IsConstant { get; init; }

    /// <summary>
    /// Gets or initialises the list of initial value expressions.
    /// </summary>
    /// <remarks>
    /// An empty list indicates the array was declared without a <c>BEING</c> clause and will be initialised as an empty
    /// array at runtime.  The list is never <c>null</c>.
    /// </remarks>
    public required IReadOnlyList<Expression> InitialValues { get; init; }
}
