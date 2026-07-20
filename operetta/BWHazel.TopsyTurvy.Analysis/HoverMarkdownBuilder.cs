using System.Collections.Generic;
using System.Linq;
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
            SymbolKind.Variable when symbolInfo.IsConstant =>
                $"**(constant)** `{symbolInfo.Name}` : {symbolInfo.TypeDisplayName}",
            SymbolKind.Variable =>
                $"**(variable)** `{symbolInfo.Name}` : {symbolInfo.TypeDisplayName}",
            SymbolKind.Function =>
                BuildFunctionSignature(symbolInfo),
            SymbolKind.Parameter when symbolInfo.TypeDisplayName is not null =>
                $"**(parameter)** `{symbolInfo.Name}` : {symbolInfo.TypeDisplayName}",
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

    /// <summary>
    /// Builds a Markdown hover string for a namespace path.
    /// </summary>
    /// <param name="namespacePath">The namespace path segments.</param>
    /// <param name="functions">The function symbols declared in the namespace, if any.</param>
    /// <returns>A Markdown string suitable for display in a hover tooltip.</returns>
    public static string BuildNamespaceHover(IReadOnlyList<string> namespacePath, IEnumerable<SymbolInfo> functions)
    {
        string pathDisplay = string.Join('*', namespacePath);
        StringBuilder builder = new($"**(namespace)** `{pathDisplay}`");

        List<SymbolInfo> functionList = [.. functions.OrderBy(function => function.Name)];
        if (functionList.Count > 0)
        {
            builder.Append("\n\n---\n\n");
            builder.AppendLine("**Functions:**");
            builder.AppendLine();
            foreach (SymbolInfo function in functionList)
            {
                builder.AppendLine($"- `{function.Name}`");
            }
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// Builds the signature string for a function symbol, including typed parameters and return type.
    /// </summary>
    /// <param name="symbolInfo">The function symbol.</param>
    /// <returns>A Markdown signature string.</returns>
    private static string BuildFunctionSignature(SymbolInfo symbolInfo)
    {
        string parameterList = symbolInfo.TypedParameters is { Count: > 0 }
            ? string.Join(", ", symbolInfo.TypedParameters.Select(static parameter =>
                $"{parameter.Name} AS A {LiteralTypeToKeyword(parameter.Type)}"))
            : string.Empty;

        string returnPart = symbolInfo.DeclaredType.HasValue
            ? $" TO FIND {LiteralTypeToKeyword(symbolInfo.DeclaredType.Value)}"
            : string.Empty;

        return $"**(function)** `{symbolInfo.Name}`({parameterList}){returnPart}";
    }

    /// <summary>
    /// Gets the display name for a <see cref="LiteralType"/> as a keyword string.
    /// </summary>
    /// <param name="type">The literal type.</param>
    /// <returns>The keyword string for the literal type.</returns>
    private static string LiteralTypeToKeyword(LiteralType type) =>
        LiteralTypeNames.ToDisplayName(type);
}
