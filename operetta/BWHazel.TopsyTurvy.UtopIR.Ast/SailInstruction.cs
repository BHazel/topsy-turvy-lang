namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Branches unconditionally to the specified label.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>sail</c> instruction with the format <c>sail &lt;label&gt;</c>.
/// There is no direct equivalent in Topsy Turvy.
/// </para>
/// <para>
/// For example, to branch unconditionally to the label <c>LOGIC</c>:
/// </para>
/// <code>
/// new SailInstruction(Label: new UtopIRLabel("LOGIC"));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// sail !LOGIC
/// </code>
/// </remarks>
/// <param name="Label">The label to branch to.</param>
public sealed record SailInstruction(UtopIRLabel Label) : UtopIRInstruction;
