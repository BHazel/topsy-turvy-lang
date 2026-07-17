namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Represents a named branch target (label) in UtopIR.
/// </summary>
/// <remarks>
/// In UtopIR source text, labels are written with a <c>!</c> prefix, e.g. <c>!LOGIC</c>.  This
/// record stores the name without the prefix and emitters and formatters are responsible for
/// adding <c>!</c> when producing textual output.  A label is a flat marker instruction in the
/// programme instruction list: it introduces no block or scope, only a position that
/// <c>sail</c>/<c>sailalike</c>/<c>sailunlike</c> can branch to.
/// </remarks>
/// <param name="Name">The name of the label, without the <c>!</c> prefix.</param>
public sealed record UtopIRLabel(string Name);
