namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Selects the <c>stitch</c> character at a 1-based index of a <c>yarn</c> value and assigns it to a
/// virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>victim.yarn</c> instruction with the format
/// <c>£&lt;name&gt; = victim.yarn &lt;yarn&gt;, &lt;index&gt;</c>. <see cref="YarnString"/> must be of the
/// <c>yarn</c> (string) type, <see cref="Target"/> must be of the <c>stitch</c> (character) type and <see cref="Index"/> must
/// be of an integer type; using any other types is a compilation error.
/// </para>
/// <para>
/// For example, the Topsy Turvy declaration:
/// </para>
/// <code>
/// PoemSubjectLetter4 IS APPOINTED VICTIM 4 ON PoemSubject
/// </code>
/// <para>
/// produces the following <see cref="VictimYarnInstruction"/>:
/// </para>
/// <code>
/// new VictimYarnInstruction(
///     Target: new UtopIRVariable("PoemSubjectLetter4"),
///     YarnString: new VariableOperand(new UtopIRVariable("PoemSubject")),
///     Index: new LiteralOperand(4));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £PoemSubjectLetter4 = victim.yarn £PoemSubject, 4
/// </code>
/// </remarks>
/// <param name="Target">The virtual register that will hold the selected <c>stitch</c> character.</param>
/// <param name="YarnString">The <c>yarn</c> string value being selected from, either literal or variable.</param>
/// <param name="Index">The 1-based integer index of the character in the <c>yarn</c> string, either a literal or variable.</param>
public sealed record VictimYarnInstruction(UtopIRVariable Target, UtopIROperand YarnString, UtopIROperand Index) : UtopIRInstruction;
