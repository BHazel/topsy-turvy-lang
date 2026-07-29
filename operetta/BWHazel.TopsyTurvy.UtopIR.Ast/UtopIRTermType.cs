namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// A type valid in a <c>term</c>/<c>finds</c> signature position: a <c>duty</c> parameter, a <c>duty</c>
/// return type, or a <c>summon</c>/<c>summon.find</c> signature operand.
/// </summary>
/// <remarks>
/// <para>
/// This is a distinct type from the bare <see cref="UtopIRType"/> used by <c>welcome</c> and
/// <c>were</c>: the array form <c>list.&lt;type&gt;</c> is valid only in a signature position.
/// </para>
/// <para>
/// <see cref="ElementType"/> is non-<c>null</c> only when <see cref="Type"/> is
/// <see cref="UtopIRType.Array"/>, in which case the term type renders as <c>list.&lt;ElementType&gt;</c>,
/// e.g. <c>list.peer</c>. For every other <see cref="Type"/>, <see cref="ElementType"/> is <c>null</c> and
/// the term type renders as the plain type keyword.
/// </para>
/// <para>
/// For example, the following function parameter in Topsy Turvy:
/// </para>
/// <code>
/// IT IS MY DUTY TO PERFORM Sum UNDER THE TERMS OF Numbers AS A LITTLE LIST OF PEER TO FIND PEER
/// </code>
/// <para>
/// would be represented in the AST as:
/// </para>
/// <code>
/// new UtopIRFunctionParameter(
///     Type: new UtopIRTermType(UtopIRType.Array, UtopIRType.Peer),
///     Name: "Numbers");
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// term list.peer %Numbers
/// </code>
/// </remarks>
/// <param name="Type">The underlying type, or <see cref="UtopIRType.Array"/> when this is an array term type.</param>
/// <param name="ElementType">The array element type, when <paramref name="Type"/> is <see cref="UtopIRType.Array"/>; otherwise <c>null</c>.</param>
public sealed record UtopIRTermType(UtopIRType Type, UtopIRType? ElementType = null);
