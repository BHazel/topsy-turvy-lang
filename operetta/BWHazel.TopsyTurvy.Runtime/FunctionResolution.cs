using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Carries the outcome of resolving a <c>SUMMON</c> target: exactly one of <see cref="UserFunction"/>
/// or <see cref="ExternalFunction"/> is set.
/// </summary>
/// <param name="UserFunction">The matched Topsy Turvy function, or <c>null</c> when an external function matched instead.</param>
/// <param name="ExternalFunction">The matched external function, or <c>null</c> when a Topsy Turvy function matched instead.</param>
internal readonly record struct FunctionResolution(FunctionDefinitionNode? UserFunction, BoundFunctionDescriptor? ExternalFunction);