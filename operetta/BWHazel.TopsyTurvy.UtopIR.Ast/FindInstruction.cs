namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Returns a value to the calling scope, terminating the current function or programme.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>find</c> instruction with the format <c>find [&lt;value&gt;]</c>.
/// When <see cref="Value"/> is <c>null</c>, the instruction performs a void return (no value).  In
/// a top-level programme context this results in an exit code of <c>0</c>.
/// </para>
/// <para>
/// For example, the Topsy Turvy top-level statement:
/// </para>
/// <code>
/// AND SO I FIND PeerResult
/// </code>
/// <para>
/// produces the following <see cref="FindInstruction"/>:
/// </para>
/// <code>
/// new FindInstruction(Value: new VariableOperand(new UtopIRVariable("PeerResult")));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// find PeerResult
/// </code>
/// <para>
/// A void return is represented in the AST as:
/// </para>
/// <code>
/// new FindInstruction(Value: null);
/// </code>
/// <para>
/// and rendered in UtopIR source as:
/// </para>
/// <code>
/// find
/// </code>
/// </remarks>
/// <param name="Value">The operand to return, <c>null</c> for a void return.
/// </param>
public sealed record FindInstruction(UtopIROperand? Value) : UtopIRInstruction;
