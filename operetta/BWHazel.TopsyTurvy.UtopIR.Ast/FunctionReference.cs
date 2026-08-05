namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Represents a named function to call in UtopIR.
/// </summary>
/// <remarks>
/// <para>
/// In UtopIR source text, a function reference is written with a <c>&amp;</c> prefix, e.g.
/// <c>&amp;Function</c>.  This record stores the name without the prefix and emitters and
/// formatters are responsible for adding <c>&amp;</c> when producing textual output.  A function
/// reference names the target of a <see cref="SummonInstruction"/> or
/// <see cref="SummonFindInstruction"/>, not a virtual register or a branch target.
/// </para>
/// <para>
/// For a function declared in a <c>TOWN</c> namespace, <see cref="Name"/> is the fully-qualified
/// path, with each namespace segment joined by <see cref="UtopIRKeywords.NamespaceDelimiter"/>, e.g.
/// <c>Aesthetic*Writing*ReadPoem</c> for a function <c>ReadPoem</c> declared under
/// <c>TOWN Aesthetic WITH DISTRICT Writing</c>. This is a plain <see cref="string"/>, not a segmented
/// path type: the namespace delimiter is just another character valid in the name, so no separate
/// parsing or matching is needed anywhere a <see cref="FunctionReference"/> is already handled.
/// </para>
/// </remarks>
/// <param name="Name">The name of the function, without the <c>&amp;</c> prefix, including any namespace path segments joined by <see cref="UtopIRKeywords.NamespaceDelimiter"/>.</param>
public sealed record FunctionReference(string Name);
