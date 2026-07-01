namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Represents a named virtual register (variable) in UtopIR.
/// </summary>
/// <remarks>
/// /// In UtopIR source text, virtual registers are written with a <c>£</c> prefix, e.g. <c>£Lords</c>.
/// This record stores the name without the prefix and emitters and formatters are responsible for
/// adding <c>£</c> when producing textual output.  Variables beginning with an underscore, e.g.
/// <c>_sum_a_b</c>, are temporary registers.
/// </remarks>
/// <param name="Name">The name of the virtual register, without the <c>£</c> prefix.</param>
public sealed record UtopIRVariable(string Name);
