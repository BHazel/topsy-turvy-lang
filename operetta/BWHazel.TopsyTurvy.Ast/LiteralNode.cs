namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a literal value in the AST.
/// </summary>
/// <remarks>
/// <para>
/// This represents a literal value in Topsy Turvy and is produced wherever a literal value appears directly in Topsy Turvy.
/// Every literal consists of a <see cref="Value"/> holding the parsed value and a <see cref="Type"/> identifying the
/// <see cref="LiteralType"/>.  Please see the <see cref="LiteralType"/> enumeration for a description of the different literal
/// types and their corresponding Topsy Turvy keywords.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy prints a string literal:
/// </para>
/// <code>
/// BEHOLD "I am the very model of a modern Major-General!"
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="PrintNode"/> with its <see cref="PrintNode.Expression"/> set to a
/// <see cref="LiteralNode"/> with:
/// * The <see cref="Type"/> property set to <see cref="LiteralType"/>.<c>String</c>.
/// * The <see cref="Value"/> property set to <c>I am the very model of a modern Major-General!</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new PrintNode()
/// {
///     Expression = new LiteralNode()
///     {
///         Type = LiteralType.String,
///         Value = "I am the very model of a modern Major-General!",
///         Span = new() { /* ... */ }
///     },
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// For a boolean literal the Topsy Turvy keywords <c>VERITY</c> and <c>NAY</c> map to <see cref="LiteralType"/>.<c>Boolean</c>
/// with <see cref="Value"/> set to <c>true</c> or <c>false</c> respectively.  Convenience constants for these are provided by
/// <see cref="Keywords"/><c>.Literals.Verity</c> and <see cref="Keywords"/><c>.Literals.Nay</c>.
/// </para>
/// <para>
/// The null literal <c>NAUGHT</c> maps to <see cref="LiteralType"/>.<c>Null</c> with <see cref="Value"/> set to <c>null</c>.  As
/// with the boolean literals, a convenience constant for this is provided by <see cref="Keywords"/><c>.Literals.Naught</c>.
/// </para>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class LiteralNode : Expression
{
    /// <summary>
    /// Gets or initialises the actual value of the literal.
    /// </summary>
    public required object? Value { get; init; }

    /// <summary>
    /// Gets or initialises the type of the literal.
    /// </summary>
    public required LiteralType Type { get; init; }
}
