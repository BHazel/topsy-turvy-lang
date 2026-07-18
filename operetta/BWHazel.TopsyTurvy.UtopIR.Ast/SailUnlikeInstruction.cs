namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Branches to the specified label if the given <c>decree</c> value is <c>nay</c>.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>sailunlike</c> instruction with the format
/// <c>sailunlike &lt;value&gt;, &lt;label&gt;</c>.  The value operand must be of type <c>decree</c>;
/// any other type is a compilation error.  There is no direct equivalent in Topsy Turvy.
/// </para>
/// <para>
/// For example, to branch to the label <c>IS_UNLIKE</c> when <c>£Boolean</c> is <c>nay</c>:
/// </para>
/// <code>
/// new SailUnlikeInstruction(
///     Value: new VariableOperand(new UtopIRVariable("Boolean")),
///     Label: new UtopIRLabel("IS_UNLIKE"));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// sailunlike £Boolean, !IS_UNLIKE
/// </code>
/// </remarks>
/// <param name="Value">The <c>decree</c> value being evaluated.</param>
/// <param name="Label">The label to branch to.</param>
public sealed record SailUnlikeInstruction(UtopIROperand Value, UtopIRLabel Label) : UtopIRInstruction;
