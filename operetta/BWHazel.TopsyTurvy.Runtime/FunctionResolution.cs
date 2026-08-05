using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Carries the outcome of resolving a <c>SUMMON</c> target to a namespace tier: exactly one of
/// <see cref="UserFunctions"/> or <see cref="ExternalFunctions"/> is set, and it may hold more than one
/// overload sharing that name at that tier.
/// </summary>
/// <param name="UserFunctions">The Topsy Turvy overload set matched at the winning tier, or <c>null</c> when an external overload set matched instead.</param>
/// <param name="ExternalFunctions">The external overload set matched at the winning tier, or <c>null</c> when a Topsy Turvy overload set matched instead.</param>
/// <remarks>
/// Resolving a name to this overload set is only half of resolving a call: the caller still has to pick the
/// specific overload within it, by argument type, since two Topsy Turvy functions or two external functions may
/// legitimately share a name at the same tier.
/// </remarks>
internal readonly record struct FunctionResolution(IReadOnlyList<FunctionDefinitionNode>? UserFunctions, IReadOnlyList<BoundFunctionDescriptor>? ExternalFunctions);