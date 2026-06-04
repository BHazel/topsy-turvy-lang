using System;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Builds Markdown hover text for Topsy Turvy symbols.
/// </summary>
public static class HoverMarkdownBuilder
{
    /// <summary>
    /// Builds a Markdown hover string for the given symbol.
    /// </summary>
    /// <param name="info">The symbol to describe.</param>
    /// <returns>A Markdown string suitable for display in a hover tooltip.</returns>
    public static string Build(SymbolInfo info) => info.Kind switch
    {
        SymbolKind.Variable when info.Name.Equals(Keywords.SpecialNames.JustSo, StringComparison.OrdinalIgnoreCase) =>
            "**implicit variable** `JUST SO` — receives the result of the last expression",
        SymbolKind.Variable =>
            $"**(variable)** `{info.Name}` : {info.TypeDisplayName}",
        SymbolKind.Function =>
            $"**(function)** `{info.Name}`({string.Join(", ", info.Parameters ?? Array.Empty<string>())})",
        SymbolKind.Parameter =>
            $"**(parameter)** `{info.Name}`",
        _ =>
            $"`{info.Name}`"
    };
}
