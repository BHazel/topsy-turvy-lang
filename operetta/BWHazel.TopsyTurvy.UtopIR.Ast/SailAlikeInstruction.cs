namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Branches to the specified label if the given <c>decree</c> value is <c>verity</c>.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>sailalike</c> instruction with the format
/// <c>sailalike &lt;value&gt;, &lt;label&gt;</c>.  The value operand must be of type <c>decree</c>;
/// any other type is a compilation error.  There is no direct equivalent in Topsy Turvy.
/// </para>
/// <para>
/// For example, to branch to the label <c>IS_ALIKE</c> when <c>£Boolean</c> is <c>verity</c>:
/// </para>
/// <code>
/// new SailAlikeInstruction(
///     Value: new VariableOperand(new UtopIRVariable("Boolean")),
///     Label: new UtopIRLabel("IS_ALIKE"));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// sailalike £Boolean, !IS_ALIKE
/// </code>
/// </remarks>
/// <param name="Value">The <c>decree</c> value being evaluated.</param>
/// <param name="Label">The label to branch to.</param>
public sealed record SailAlikeInstruction(UtopIROperand Value, UtopIRLabel Label) : UtopIRInstruction;
