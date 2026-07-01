namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// An operand whose value is a compile-time constant literal.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Value"/> is typed as <see cref="object"/> because a literal operand may hold any
/// primitive value supported by UtopIR:
/// * <c>chancellor</c>: <see cref="long"/>
/// * <c>peer</c>: <see cref="int"/> 
/// * <c>pirate</c>: <see cref="short"/>
/// * <c>sausageroll</c>: <see cref="byte"/>
/// * <c>fathom</c>: <see cref="double"/>
/// * <c>foot</c>: <see cref="float"/>
/// * <c>yarn</c>: <see cref="string"/>
/// * <c>stitch</c>: <see cref="char"/>
/// * <c>decree</c>: <see cref="bool"/>
/// For integer types, <c>standing</c> prefixes are used to indicate unsigned types, e.g. <c>standingpeer</c>
/// corresponds to <see cref="uint"/>.
/// </para>
/// <para>
/// Emitters and code generators are expected to inspect the runtime type and produce the appropriate output.
/// </para>
/// <para>
/// For example, for the UtopIR source:
/// </para>
/// <code>
/// £Lords = appoint 42
/// </code>
/// <para>
/// would produce an <see cref="AppointInstruction"/> whose <see cref="AppointInstruction.Value"/> is a
/// <see cref="LiteralOperand"/> with <see cref="Value"/> set to the <see cref="int"/> <c>42</c>.
/// </para>
/// </remarks>
/// <param name="Value">The literal constant value.</param>
public sealed record LiteralOperand(object Value) : UtopIROperand;
