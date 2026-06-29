using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Scans a live visual diagram and extracts declared symbols for display in the symbol panel.
/// </summary>
internal static class VisualSymbolScanner
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
        List<VisualSymbolEntry> symbols = [];
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

                // Parameters are stored as DataOut port labels in "name : type" format.
                List<string> parameterNames = [.. visualNode.Ports
                    .OfType<TopsyTurvyVisualPortModel>()
                    .Where(port => port.Role == VisualPortRole.DataOut && port.Label is not null)
                    .Select(port => port.Label!.Split(':')[0].Trim())];

                symbols.Add(new VisualSymbolEntry(functionName, VisualSymbolKind.Function, null, parameterNames));
            }
        }

        return symbols;
    }
}
