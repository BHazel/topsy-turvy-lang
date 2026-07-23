namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Represents a named function to call in UtopIR.
/// </summary>
/// <remarks>
/// In UtopIR source text, a function reference is written with a <c>&amp;</c> prefix, e.g.
/// <c>&amp;Function</c>.  This record stores the name without the prefix and emitters and
/// formatters are responsible for adding <c>&amp;</c> when producing textual output.  A function
/// reference names the target of a <see cref="SummonInstruction"/> or
/// <see cref="SummonFindInstruction"/>, not a virtual register or a branch target.
/// </remarks>
/// <param name="Name">The name of the function, without the <c>&amp;</c> prefix.</param>
public sealed record FunctionReference(string Name);
