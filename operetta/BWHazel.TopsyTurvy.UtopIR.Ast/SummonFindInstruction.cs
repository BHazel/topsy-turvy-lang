namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Calls a returning function and stores its result in a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>summon.find</c> instruction with the format
/// <c>£&lt;name&gt; = summon.find &lt;function&gt;</c>.  Only a returning function can be called
/// this way.  Each argument is pushed onto the stack with a <see cref="PrenticeInstruction"/>, in
/// the order declared by the function, before the <see cref="SummonFindInstruction"/> itself.
/// </para>
/// <para>
/// For example, the Topsy Turvy call:
/// </para>
/// <code>
/// Poem IS APPOINTED SUMMON GetPoem WITH Name IF YOU PLEASE.
/// </code>
/// <para>
/// lowers to a <see cref="PrenticeInstruction"/> followed by a <see cref="SummonFindInstruction"/>,
/// represented in the AST as:
/// </para>
/// <code>
/// new PrenticeInstruction(Operand: new VariableOperand(new UtopIRVariable("Name")));
/// new SummonFindInstruction(
///     Target: new UtopIRVariable("Poem"),
///     Function: new FunctionReference("GetPoem"));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// prentice £Name
/// £Poem = summon.find &amp;GetPoem
/// </code>
/// </remarks>
/// <param name="Target">The virtual register that will hold the returned value.</param>
/// <param name="Function">The function to call.</param>
public sealed record SummonFindInstruction(UtopIRVariable Target, FunctionReference Function) : UtopIRInstruction;
