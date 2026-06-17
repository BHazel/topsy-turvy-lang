namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an in-place type cast that mutates the variable directly.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>IS HENCEFORTH A</c> type casting statement in Topsy Turvy.  It converts a variable from one
/// type to another by mutating it in place, replacing the original value.  Every in-place cast consists of a variable to cast,
/// the <see cref="Target"/>, and the destination type, <see cref="NewType"/>.  This contrasts with
/// <see cref="ExpressionCastNode"/>, which returns a cast value as an expression without modifying the original variable.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to re-cast the variable <c>Ko-Ko</c> from a Fathom (float) to a Peer (integer):
/// </para>
/// <code>
/// Ko-Ko IS HENCEFORTH A PEER
/// </code>
/// <para>
/// would be represented in the AST as an <see cref="InPlaceCastNode"/> with:
/// * The <see cref="Target"/> property set to <c>Ko-Ko</c>.
/// * The <see cref="NewType"/> property set to <see cref="LiteralType"/>.<c>Integer</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new InPlaceCastNode()
/// {
///     Target = "Ko-Ko",
///     NewType = LiteralType.Integer,
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class InPlaceCastNode : Statement
{
    /// <summary>
    /// Gets or initialises the name of the variable to cast.
    /// </summary>
    public required string Target { get; init; }

    /// <summary>
    /// Gets or initialises the destination type.
    /// </summary>
    public required LiteralType NewType { get; init; }
}
