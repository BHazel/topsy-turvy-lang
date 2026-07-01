using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// The root node of a UtopIR programme.
/// </summary>
/// <remarks>
/// A <see cref="UtopIRProgram"/> is the result of transforming a Topsy Turvy AST or parsing a
/// UtopIR source file.  The <see cref="Instructions"/> list is executed top-to-bottom.
/// </remarks>
/// <param name="Instructions">The ordered list of instructions that make up the programme body.</param>
public sealed record UtopIRProgram(IReadOnlyList<UtopIRInstruction> Instructions);
