using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Represents a declared symbol for display in the symbol panel.
/// </summary>
/// <param name="Name">The symbol identifier name.</param>
/// <param name="Kind">The symbol kind.</param>
/// <param name="TypeDisplay">The Topsy Turvy type keyword for variables.</param>
/// <param name="ParameterNames">The parameter names for function symbols.</param>
public record VisualSymbolEntry(
    string Name,
    VisualSymbolKind Kind,
    string? TypeDisplay,
    IReadOnlyList<string> ParameterNames);
