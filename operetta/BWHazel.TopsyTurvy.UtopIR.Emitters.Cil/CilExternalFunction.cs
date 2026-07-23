using System;
using System.Collections.Generic;
using System.Reflection;

namespace BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

/// <summary>
/// Describes one externally implemented function callable from UtopIR via <c>summon</c> or <c>summon.find</c>.
/// </summary>
/// <param name="Name">The UtopIR-level function name, exactly as it appears after the <c>&amp;</c> sigil in a <c>summon</c> or <c>summon.find</c> instruction.</param>
/// <param name="Method">The CLR method to call.</param>
/// <param name="ParameterTypes">The CLR types of the language-level parameters, in declaration order, excluding any trailing host-injected parameter.</param>
/// <param name="ReturnType">The CLR return type, or <c>null</c> for a void function.</param>
/// <param name="HostInjectedParameterTypes">The CLR types of the trailing host-injected parameters, supplied by the emitter itself rather than by a preceding <c>prentice</c>.</param>
public sealed record CilExternalFunction(
    string Name,
    MethodInfo Method,
    IReadOnlyList<Type> ParameterTypes,
    Type? ReturnType,
    IReadOnlyList<Type> HostInjectedParameterTypes);
