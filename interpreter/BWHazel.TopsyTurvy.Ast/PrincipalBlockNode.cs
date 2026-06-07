using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents the <c>PRINCIPALS</c> variable declaration block.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>PRINCIPALS</c> ... <c>THE CURTAIN RISES.</c> block in Topsy Turvy.  It may appear anywhere
/// before first use but by convention is placed at the top of the programme.  Every principal block contains a list of
/// <see cref="Declarations"/>, each of which is a <see cref="DeclarationNode"/>.  Variables may also be declared inline
/// outside any <c>PRINCIPALS</c> block which produce free-standing <see cref="DeclarationNode"/> statements rather than a
/// <see cref="PrincipalBlockNode"/>.
/// </para>
/// <para>
/// For example, the following <c>PRINCIPALS</c> block in Topsy Turvy:
/// </para>
/// <code>
/// PRINCIPALS
///     PRAY WELCOME Defendant AS A YARN BEING "Edwin"
///     PRAY WELCOME JurySize AS A PEER BEING 12
/// THE CURTAIN RISES.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="PrincipalBlockNode"/> with:
/// * The <see cref="Declarations"/> property set to a list containing two <see cref="DeclarationNode"/>s: one for
///   <c>Defendant</c> and one for <c>JurySize</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new PrincipalBlockNode()
/// {
///     Declarations =
///     [
///         new DeclarationNode()
///         {
///             Name = "Defendant",
///             Type = LiteralType.String,
///             InitialValue = new LiteralNode()
///             {
///                 Type = LiteralType.String,
///                 Value = "Edwin",
///                 Span = new() { /* ... */ }
///             },
///             Span = new() { /* ... */ }
///         },
///         new DeclarationNode()
///         {
///             Name = "JurySize",
///             Type = LiteralType.Integer,
///             InitialValue = new LiteralNode()
///             {
///                 Type = LiteralType.Integer,
///                 Value = 12,
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
public class PrincipalBlockNode : Statement
{
    /// <summary>
    /// Gets or initialises the list of variable declarations in the block.
    /// </summary>
    public required IReadOnlyList<DeclarationNode> Declarations { get; init; }
}
