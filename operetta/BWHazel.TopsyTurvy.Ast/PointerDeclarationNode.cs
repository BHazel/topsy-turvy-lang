namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a pointer variable declaration.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>PRAY WELCOME &lt;name&gt; AS A [CONSERVATIVE|LIBERAL] GALLERY PICTURE OF &lt;type&gt; [BEING ...]</c>
/// pointer declaration statement in Topsy Turvy.  Every pointer declaration consists of the <see cref="Name"/> of the
/// variable being declared, the declared <see cref="PointeeType"/> of the value the pointer refers to, an optional
/// <see cref="IsConstant"/> flag set by the <c>CONSERVATIVE</c> or <c>LIBERAL</c> mutability modifier and an optional
/// <see cref="InitialValue"/>.
/// </para>
/// <para>
/// When no initial value is provided, the pointer is initialised to <c>NAUGHT</c> (an unassigned pointer).  Dereferencing
/// a <c>NAUGHT</c> pointer is a runtime error.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to declare a pointer variable <c>NumberPointer</c> that refers to a
/// <c>PEER</c> value:
/// </para>
/// <code>
/// PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="PointerDeclarationNode"/> with:
/// * The <see cref="Name"/> property set to <c>NumberPointer</c>.
/// * The <see cref="PointeeType"/> property set to <see cref="LiteralType"/><c>.Integer</c>.
/// * The <see cref="IsConstant"/> property set to <c>false</c>.
/// * The <see cref="InitialValue"/> property set to <c>null</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new PointerDeclarationNode()
/// {
///     Name = "NumberPointer",
///     PointeeType = LiteralType.Integer,
///     IsConstant = false,
///     InitialValue = null,
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class PointerDeclarationNode : Statement
{
    /// <summary>
    /// Gets or initialises the name of the pointer variable being declared.
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
    /// Gets or initialises the declared type of the value this pointer refers to.
    /// </summary>
    public required LiteralType PointeeType { get; init; }

    /// <summary>
    /// Gets or initialises a value indicating whether this pointer declaration is a constant.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the pointer is declared with the <c>CONSERVATIVE</c> modifier and may not be re-pointed at a
    /// different variable.  When <c>false</c> (the default), the pointer is mutable and may be reassigned freely.
    /// </remarks>
    public bool IsConstant { get; init; }

    /// <summary>
    /// Gets or initialises the optional initial value.
    /// </summary>
    /// <remarks>
    /// When present, this is expected to be an <see cref="AddressOfExpressionNode"/> or a <c>NAUGHT</c>
    /// <see cref="LiteralNode"/>; any other expression is a type-checker error.
    /// </remarks>
    public Expression? InitialValue { get; init; }
}
