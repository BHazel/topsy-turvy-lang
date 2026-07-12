using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models.Base;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Scans a live visual diagram and extracts declared symbols for display in the symbol panel.
/// </summary>
public static class VisualSymbolScanner
{
    /// <summary>
    /// Scans all nodes in the diagram and returns a list of declared symbol entries.
    /// </summary>
    /// <param name="diagram">The diagram to scan.</param>
    /// <returns>A list of <see cref="VisualSymbolEntry"/> instances extracted from the diagram nodes.</returns>
    /// <remarks>
    /// Reads from the model properties set by <see cref="VisualGraphBuilder"/> and <see cref="VisualNodeFactory"/>:
    /// declaration nodes contribute Variable entries and function-body opener nodes contribute Function entries.
    /// The scanner does not invoke the parser: it works entirely from diagram node state.
    /// </remarks>
    public static IReadOnlyList<VisualSymbolEntry> Scan(BlazorDiagram diagram)
    {
        Dictionary<string, string> termDeclaringFunctions = [];
        foreach (TopsyTurvyVisualNodeModel opener in diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .Where(visualNode => visualNode.StatementType == "FunctionBodyOpener"))
        {
            string functionName = opener.SymbolIdentifierNodeName ?? string.Empty;
            foreach (TopsyTurvyVisualPortModel parameterPort in opener.Ports
                .OfType<TopsyTurvyVisualPortModel>()
                .Where(port => port.Role == VisualPortRole.DataIn && port.Label?.StartsWith("Param ") == true))
            {
                BaseLinkModel? link = parameterPort.Links.FirstOrDefault();
                string? termId = (link?.Source as SinglePortAnchor)?.Port?.Parent?.Id;
                if (termId is not null && !string.IsNullOrEmpty(functionName))
                {
                    termDeclaringFunctions[termId] = functionName;
                }
            }
        }

        List<VisualSymbolEntry> symbols =
        [
            new("THE PROPS", VisualSymbolKind.Variable, "CONSERVATIVE LITTLE LIST OF YARN", []),
        ];

        foreach (TopsyTurvyVisualNodeModel visualNode in diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>())
        {
            if (visualNode.StatementType is "DeclarationNode" or "ArrayDeclarationNode")
            {
                string variableName = visualNode.SymbolIdentifierNodeName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(variableName))
                {
                    continue;
                }

                string? typeDisplay = null;
                if (visualNode.StatementType == "ArrayDeclarationNode")
                {
                    if (visualNode.ArrayElementLiteralType.HasValue && VisualTypeMaps.TypeToKeyword.TryGetValue(visualNode.ArrayElementLiteralType.Value, out string? elementKeyword))
                    {
                        typeDisplay = $"LITTLE LIST OF {elementKeyword}";
                    }
                    else
                    {
                        typeDisplay = "LITTLE LIST";
                    }
                }
                else if (visualNode.NodeLiteralType.HasValue && VisualTypeMaps.TypeToKeyword.TryGetValue(visualNode.NodeLiteralType.Value, out string? keyword))
                {
                    typeDisplay = keyword;
                }

                symbols.Add(new VisualSymbolEntry(variableName, VisualSymbolKind.Variable, typeDisplay, []));
            }
            else if (visualNode.StatementType == "FunctionBodyOpener")
            {
                string functionName = visualNode.SymbolIdentifierNodeName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(functionName))
                {
                    continue;
                }

                List<string> parameterNames = [.. visualNode.Ports
                    .OfType<TopsyTurvyVisualPortModel>()
                    .Where(port => port.Role == VisualPortRole.DataOut && port.Label is not null)
                    .Select(port => port.Label!.Split(':')[0].Trim())];

                if (parameterNames.Count == 0)
                {
                    parameterNames = [.. visualNode.Ports
                        .OfType<TopsyTurvyVisualPortModel>()
                        .Where(port => port.Role == VisualPortRole.DataIn && port.Label?.StartsWith("Param ") == true)
                        .OrderBy(port => port.Label)
                        .Select(port =>
                        {
                            BaseLinkModel? link = port.Links.FirstOrDefault();
                            TopsyTurvyVisualNodeModel? termNode = (link?.Source as SinglePortAnchor)?.Port?.Parent as TopsyTurvyVisualNodeModel;
                            return termNode?.SymbolIdentifierNodeName ?? string.Empty;
                        })
                        .Where(name => !string.IsNullOrWhiteSpace(name))];
                }

                symbols.Add(new VisualSymbolEntry(functionName, VisualSymbolKind.Function, null, parameterNames));
            }
            else if (visualNode.StatementType == "ParameterNode")
            {
                string paramName = visualNode.SymbolIdentifierNodeName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(paramName))
                {
                    continue;
                }

                string? typeDisplay = visualNode.NodeLiteralType.HasValue && VisualTypeMaps.TypeToKeyword.TryGetValue(visualNode.NodeLiteralType.Value, out string? keyword)
                    ? keyword
                    : null;

                termDeclaringFunctions.TryGetValue(visualNode.Id, out string? ownerName);
                symbols.Add(new VisualSymbolEntry(paramName, VisualSymbolKind.Parameter, typeDisplay, [], ownerName));
            }
            else if (visualNode.StatementType == "TryCatchOpener")
            {
                string caughtName = visualNode.SymbolIdentifierNodeName ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(caughtName))
                {
                    symbols.Add(new VisualSymbolEntry(caughtName, VisualSymbolKind.Variable, null, []));
                }
            }
        }

        return symbols;
    }
}
