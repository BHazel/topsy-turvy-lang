using System.Collections.Generic;
using BWHazel.TopsyTurvy.Bindings;

namespace BWHazel.TopsyTurvy.Cli;

/// <summary>
/// The outcome of loading a set of external library assemblies.
/// </summary>
/// <param name="Catalogue">The merged catalogue, or <c>null</c> if loading failed.</param>
/// <param name="ErrorMessage">A clean, one-line error message, or <c>null</c> if loading succeeded.</param>
/// <param name="ExternalLibraryAssemblyPaths">The resolved absolute paths of the loaded external library assemblies, or empty if loading failed.</param>
public readonly record struct ExternalLibraryLoadResult(BindingCatalogue? Catalogue, string? ErrorMessage, IReadOnlyList<string> ExternalLibraryAssemblyPaths);
