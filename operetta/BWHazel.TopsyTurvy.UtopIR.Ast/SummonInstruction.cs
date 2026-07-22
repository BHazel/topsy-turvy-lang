namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Calls a function, ignoring any return value.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>summon</c> instruction with the format <c>summon &lt;function&gt;</c>.
/// Both void and returning functions can be called this way, however, a return value is always ignored.
/// Each argument is pushed onto the stack with a <see cref="PrenticeInstruction"/>, in the order
/// declared by the function, before the <see cref="SummonInstruction"/> itself.
/// </para>
/// <para>
/// For example, the Topsy Turvy call:
/// </para>
/// <code>
/// SUMMON ReadPoem WITH Name IF YOU PLEASE.
/// </code>
/// <para>
/// lowers to a <see cref="PrenticeInstruction"/> followed by a <see cref="SummonInstruction"/>,
/// represented in the AST as:
/// </para>
/// <code>
/// new PrenticeInstruction(Operand: new VariableOperand(new UtopIRVariable("Name")));
/// new SummonInstruction(Function: new FunctionReference("ReadPoem"));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// prentice £Name
/// summon &amp;ReadPoem
/// </code>
/// </remarks>
/// <param name="Function">The function to call.</param>
public sealed record SummonInstruction(FunctionReference Function) : UtopIRInstruction;
