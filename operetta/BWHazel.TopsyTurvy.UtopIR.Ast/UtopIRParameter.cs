namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Represents a named function parameter in UtopIR.
/// </summary>
/// <remarks>
/// In UtopIR source text, a function parameter is written with a <c>%</c> prefix, e.g. <c>%Name</c>.
/// This record stores the name without the prefix and emitters and formatters are responsible for
/// adding <c>%</c> when producing textual output. Unlike a <see cref="UtopIRVariable"/>, a parameter is
/// never declared with <c>welcome</c>: it is bound directly from the argument passed by the caller and
/// can only appear within the body of the <see cref="UtopIRFunctionDefinition"/> that declares it.
/// </remarks>
/// <param name="Name">The name of the parameter, without the <c>%</c> prefix.</param>
public sealed record UtopIRParameter(string Name);
