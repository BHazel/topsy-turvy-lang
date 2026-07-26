using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;
using BWHazel.TopsyTurvy.StandardLibrary.IO;
using BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Projects a <see cref="BindingCatalogue"/> into the reflection-only shapes <see cref="CilEmitter"/> needs to
/// compile <c>summon</c> and <c>summon.find</c> calls.
/// </summary>
public static class ExternalFunctionCilProjection
{
    /// <summary>
    /// Gets the host-injected services every compiled programme that made at least one external call needs to run standalone.
    /// </summary>
    /// <remarks>
    /// The one host-injected service currently supported: <see cref="ITopsyTurvyIO"/>, satisfied by
    /// <see cref="ConsoleIO"/> in an emitted, standalone programme.
    /// </remarks>
    public static IReadOnlyList<CilHostInjectedService> HostInjectedServices { get; } =
    [
        new CilHostInjectedService(
            typeof(ITopsyTurvyIO),
            typeof(ConsoleIO).GetConstructor(Type.EmptyTypes)
                ?? throw new InvalidOperationException($"{nameof(ConsoleIO)} has no public parameterless constructor."))
    ];

    /// <summary>
    /// Get the on-disk paths of every assembly a compiled programme that made at least one external call
    /// needs alongside it to run standalone.
    /// </summary>
    /// <remarks>
    /// Two assemblies, not one: the <see cref="ConsoleIO"/> (implements the functions) and
    /// <see cref="ITopsyTurvyIO"/> (the host-injected parameter type every signature mentions) host assemblies.
    /// Paths are built from <see cref="AppContext.BaseDirectory"/> rather than <see cref="Assembly.Location"/>,
    /// since the latter is always empty for a single-file-published app.
    /// </remarks>
    public static IReadOnlyList<string> DeploymentAssemblyPaths { get; } =
    [
        Path.Combine(AppContext.BaseDirectory, $"{typeof(ConsoleIO).Assembly.GetName().Name}.dll"),
        Path.Combine(AppContext.BaseDirectory, $"{typeof(ITopsyTurvyIO).Assembly.GetName().Name}.dll"),
    ];

    /// <summary>
    /// Gets the on-disk paths of every assembly a compiled programme needs alongside it to run standalone, including
    /// any admitted external library assemblies.
    /// </summary>
    /// <param name="externalLibraryAssemblyPaths">The resolved paths of admitted external library assemblies, or <c>null</c> for none.</param>
    /// <returns><see cref="DeploymentAssemblyPaths"/> with <paramref name="externalLibraryAssemblyPaths"/> appended.</returns>
    public static IReadOnlyList<string> GetDeploymentAssemblyPaths(IReadOnlyList<string>? externalLibraryAssemblyPaths) =>
        externalLibraryAssemblyPaths is null || externalLibraryAssemblyPaths.Count == 0
            ? DeploymentAssemblyPaths
            : [.. DeploymentAssemblyPaths, .. externalLibraryAssemblyPaths];

    /// <summary>
    /// Projects every function in <paramref name="catalogue"/> into a <see cref="CilExternalFunction"/>.
    /// </summary>
    /// <param name="catalogue">The catalogue to project.</param>
    /// <returns>One <see cref="CilExternalFunction"/> per function in <paramref name="catalogue"/>.</returns>
    public static IReadOnlyList<CilExternalFunction> ProjectExternalFunctions(BindingCatalogue catalogue) =>
        [.. catalogue.Functions.Select(ProjectExternalFunction)];

    /// <summary>
    /// Projects one <see cref="BoundFunctionDescriptor"/> into a <see cref="CilExternalFunction"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="BoundParameter.ClrType"/> already excludes host-injected parameters,
    /// so the visible parameter types are read straight from <see cref="BoundFunctionDescriptor.Parameters"/>.
    /// The trailing host-injected parameter types have no equivalent in that list and are instead read
    /// directly off the real CLR signature, following the same <c>visibleParameterCount</c> split
    /// <see cref="XmlDocumentationMapper"/> already uses.
    /// </remarks>
    /// <param name="descriptor">The bound function to project.</param>
    /// <returns>The projected <see cref="CilExternalFunction"/>.</returns>
    private static CilExternalFunction ProjectExternalFunction(BoundFunctionDescriptor descriptor)
    {
        ParameterInfo[] clrParameters = descriptor.Method.GetParameters();
        int visibleParameterCount = clrParameters.Length - descriptor.HostInjectedParameterCount;
        Type[] hostInjectedParameterTypes = [.. clrParameters.Skip(visibleParameterCount).Select(parameter => parameter.ParameterType)];
        Type[] parameterTypes = [.. descriptor.Parameters.Select(parameter => parameter.ClrType)];
        Type? returnType = descriptor.Method.ReturnType == typeof(void) ? null : descriptor.Method.ReturnType;

        return new(descriptor.Name, descriptor.Method, parameterTypes, returnType, hostInjectedParameterTypes);
    }
}
