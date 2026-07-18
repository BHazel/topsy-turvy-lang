namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Marks a position in the code as a named branch target.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to a UtopIR label declaration with the format <c>!&lt;name&gt;</c>.  A label is a
/// flat marker in the programme instruction list: it has no operands and introduces no block or
/// scope.  Instructions following it run straight through to the next label or unconditional
/// branch, exactly as if the label were not there.
/// </para>
/// <para>
/// Indentation of the instructions following a label is a documentation convention only, with no
/// grammar significance.
/// </para>
/// <para>
/// For example, the UtopIR source:
/// </para>
/// <code>
/// !LOGIC
///   find 1
/// </code>
/// <para>
/// is represented in the AST as:
/// </para>
/// <code>
/// new LabelInstruction(Name: new UtopIRLabel("LOGIC"));
/// new FindInstruction(Value: new LiteralOperand(1));
/// </code>
/// </remarks>
/// <param name="Name">The name of the label being declared.</param>
public sealed record LabelInstruction(UtopIRLabel Name) : UtopIRInstruction;
