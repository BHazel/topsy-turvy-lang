namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Pushes a value onto the current stack frame.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>prentice</c> instruction with the format <c>prentice &lt;value&gt;</c>.
/// There is no direct equivalent in Topsy Turvy.
/// </para>
/// <para>
/// For example, to push the literal value <c>42</c> and then the virtual register <c>£LovesickMaidens</c>:
/// </para>
/// <code>
/// new PrenticeInstruction(Value: new LiteralOperand(42));
/// new PrenticeInstruction(Value: new VariableOperand(new UtopIRVariable("LovesickMaidens")));
/// </code>
/// <para>
/// would be rendered in UtopIR source as:
/// </para>
/// <code>
/// prentice 42
/// prentice £LovesickMaidens
/// </code>
/// </remarks>
/// <param name="Value">The operand to push onto the stack.</param>
public sealed record PrenticeInstruction(UtopIROperand Value) : UtopIRInstruction;
