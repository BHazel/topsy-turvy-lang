using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Represents a named symbol found in a Topsy Turvy source file.
/// </summary>
public class SymbolInfo
{
    /// <summary>
    /// Gets or initialises the name of the symbol.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initialises the kind of symbol.
    /// </summary>
    public required SymbolKind Kind { get; init; }

    /// <summary>
    /// Gets or initialises the display name of the declared type.
    /// </summary>
    /// <remarks>
    /// Only populated for <see cref="SymbolKind.Variable"/>.
    /// </remarks>
    public string? TypeDisplayName { get; init; }

    /// <summary>
    /// Gets or initialises the list of parameter names.
    /// </summary>
    /// <remarks>
    /// Only populated for <see cref="SymbolKind.Function"/>.
    /// </remarks>
    public IReadOnlyList<string>? Parameters { get; init; }

    /// <summary>
    /// Gets or initialises the 1-indexed line number of the symbol definition in the original source.
    /// </summary>
    /// <remarks>
    /// Zero indicates the position could not be determined.
    /// </remarks>
    public int DefinitionLine { get; init; }

    /// <summary>
    /// Gets or initialises the 1-indexed column number of the symbol definition in the original source.
    /// </summary>
    /// <remarks>
    /// Zero indicates the position could not be determined.
    /// </remarks>
    public int DefinitionColumn { get; init; }
}
