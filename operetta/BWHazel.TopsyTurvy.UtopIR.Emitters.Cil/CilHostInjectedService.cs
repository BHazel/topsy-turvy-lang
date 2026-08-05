using System;
using System.Reflection;

namespace BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

/// <summary>
/// Maps a host-injected service type to the constructor the emitted programme uses to satisfy it.
/// </summary>
/// <remarks>
/// One instance is created per distinct <see cref="ServiceType"/> the first time an emitted programme
/// calls an external function that needs it, and reused for every subsequent call.
/// </remarks>
/// <param name="ServiceType">The CLR type of the host-injected service, matching an entry in some <see cref="CilExternalFunction.HostInjectedParameterTypes"/>.</param>
/// <param name="ServiceConstructor">The constructor the emitter calls to create the service instance.</param>
public sealed record CilHostInjectedService(Type ServiceType, ConstructorInfo ServiceConstructor);
