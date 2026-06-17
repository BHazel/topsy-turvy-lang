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
    /// <param name="symbolInfo">The symbol to describe.</param>
    /// <returns>A Markdown string suitable for display in a hover tooltip.</returns>
    public static string Build(SymbolInfo symbolInfo) => symbolInfo.Kind switch
    {
        SymbolKind.Variable when symbolInfo.Name.Equals(Keywords.SpecialNames.JustSo, StringComparison.OrdinalIgnoreCase) =>
            "**implicit variable** `JUST SO` — receives the result of the last expression",
        SymbolKind.Variable =>
            $"**(variable)** `{symbolInfo.Name}` : {symbolInfo.TypeDisplayName}",
        SymbolKind.Function =>
            $"**(function)** `{symbolInfo.Name}`({string.Join(", ", symbolInfo.Parameters ?? Array.Empty<string>())})",
        SymbolKind.Parameter =>
            $"**(parameter)** `{symbolInfo.Name}`",
        _ =>
            $"`{symbolInfo.Name}`"
    };
}
