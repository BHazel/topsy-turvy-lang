using System;
using System.Collections.Generic;
using System.Text;
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
    public static string Build(SymbolInfo symbolInfo)
    {
        string signature = symbolInfo.Kind switch
        {
            SymbolKind.Variable when symbolInfo.Name.Equals(Keywords.SpecialNames.JustSo, StringComparison.OrdinalIgnoreCase) =>
                "**implicit variable** `JUST SO`: receives the result of the last expression",
            SymbolKind.Variable when symbolInfo.IsConstant =>
                $"**(constant)** `{symbolInfo.Name}` : {symbolInfo.TypeDisplayName}",
            SymbolKind.Variable =>
                $"**(variable)** `{symbolInfo.Name}` : {symbolInfo.TypeDisplayName}",
            SymbolKind.Function =>
                $"**(function)** `{symbolInfo.Name}`({string.Join(", ", symbolInfo.Parameters ?? Array.Empty<string>())})",
            SymbolKind.Parameter =>
                $"**(parameter)** `{symbolInfo.Name}`",
            _ =>
                $"`{symbolInfo.Name}`"
        };

        DocumentationComment? documentation = symbolInfo.Documentation;
        if (documentation is null)
        {
            return signature;
        }

        StringBuilder builder = new(signature);
        builder.Append("\n\n---\n\n");

        if (documentation.IsDeprecated)
        {
            string deprecationLine = string.IsNullOrWhiteSpace(documentation.DeprecationMessage)
                ? "~~Deprecated~~"
                : $"~~Deprecated~~ : {documentation.DeprecationMessage}";
            builder.AppendLine(deprecationLine);
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(documentation.Summary))
        {
            builder.AppendLine(documentation.Summary);
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(documentation.Remarks))
        {
            builder.AppendLine("**Remarks:**");
            builder.AppendLine(documentation.Remarks);
            builder.AppendLine();
        }

        if (documentation.Parameters is { Count: > 0 })
        {
            builder.AppendLine("**Parameters:**");
            builder.AppendLine();
            foreach (KeyValuePair<string, (string Type, string Description)> parameter in documentation.Parameters)
            {
                builder.AppendLine($"- `{parameter.Key}` (`{parameter.Value.Type}`): {parameter.Value.Description}");
            }

            builder.AppendLine();
        }

        if (documentation.ReturnValue.HasValue)
        {
            (string returnType, string returnDescription) = documentation.ReturnValue.Value;
            builder.AppendLine($"**Returns:** (`{returnType}`) {returnDescription}");
            builder.AppendLine();
        }

        if (documentation.Exceptions is { Count: > 0 })
        {
            builder.AppendLine("**Throws:**");
            builder.AppendLine();
            foreach ((string name, string type, string description) in documentation.Exceptions)
            {
                builder.AppendLine($"- `{name}` (`{type}`): {description}");
            }

            builder.AppendLine();
        }

        if (documentation.Examples is { Count: > 0 })
        {
            foreach (string example in documentation.Examples)
            {
                builder.AppendLine("**Example:**");
                builder.AppendLine();
                builder.AppendLine("```topsy");
                builder.AppendLine(example.TrimEnd());
                builder.AppendLine("```");
                builder.AppendLine();
            }
        }

        if (documentation.SeeAlso is { Count: > 0 })
        {
            builder.Append("**See also:** ");
            builder.AppendLine(string.Join(", ", documentation.SeeAlso));
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}
