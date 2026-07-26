using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using BWHazel.TopsyTurvy.Bindings;

namespace BWHazel.TopsyTurvy.Cli;

/// <summary>
/// Resolves a set of external library assembly paths, scans each for bindings, and merges the results with the
/// Standard Library catalogue.
/// </summary>
/// <remarks>
/// Every host that supports external libraries calls <see cref="Load"/> the same way, keeping the resolve, load,
/// scan and merge sequence in one place rather than duplicated per command.
/// </remarks>
public static class ExternalLibraryLoader
{
    /// <summary>
    /// Loads and merges the external library assemblies at the given paths with <see cref="BindingCatalogue.Default"/>.
    /// </summary>
    /// <param name="externalLibraryAssemblyPaths">The paths to the external library assemblies to load, or empty for none.</param>
    /// <param name="workingDirectory">The directory relative paths are resolved against, or <c>null</c> to use the current directory.</param>
    /// <returns>The load result.</returns>
    /// <remarks>
    /// Any failure, a missing file, a load failure, a malformed binding, or a name collision returns immediately
    /// with <see cref="ExternalLibraryLoadResult.Catalogue"/> set to <c>null</c>, rather than leaving a caller to
    /// reason about a partially merged catalogue.
    /// </remarks>
    [RequiresUnreferencedCode("Scans externally loaded assemblies reflectively via BindingScanner.ScanAssembly; those assemblies are only known at runtime, so the trimmer has no way to know in advance that they are used and may remove them as unused.  Never call this from a Native AOT host.")]
    public static ExternalLibraryLoadResult Load(IReadOnlyList<string> externalLibraryAssemblyPaths, string? workingDirectory = null)
    {
        if (externalLibraryAssemblyPaths.Count == 0)
        {
            return new(BindingCatalogue.Default, null, []);
        }

        string baseDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
        List<string> resolvedPaths = [];
        List<BindingCatalogue> externalLibraryCatalogues = [];

        foreach (string externalLibraryAssemblyPath in externalLibraryAssemblyPaths)
        {
            string resolvedPath = Path.IsPathRooted(externalLibraryAssemblyPath)
                ? externalLibraryAssemblyPath
                : Path.GetFullPath(Path.Combine(baseDirectory, externalLibraryAssemblyPath));

            if (!File.Exists(resolvedPath))
            {
                return new(null, $"External library not found: {resolvedPath}", []);
            }

            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(resolvedPath);
            }
            catch (Exception exception) when (exception is BadImageFormatException or FileLoadException or IOException)
            {
                return new(null, $"Failed to load external library '{resolvedPath}': {exception.Message}", []);
            }

            try
            {
                externalLibraryCatalogues.Add(BindingCatalogue.FromDescriptors(BindingScanner.ScanAssembly(assembly)));
            }
            catch (BindingCatalogueException exception)
            {
                return new(null, $"Failed to bind external library '{resolvedPath}': {exception.Message}", []);
            }

            resolvedPaths.Add(resolvedPath);
        }

        try
        {
            BindingCatalogue mergedCatalogue = BindingCatalogue.Merge([BindingCatalogue.Default, .. externalLibraryCatalogues]);
            return new(mergedCatalogue, null, resolvedPaths);
        }
        catch (BindingCatalogueException exception)
        {
            return new(null, exception.Message, []);
        }
    }
}
