using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Tests for the <see cref="VisualSymbolScanner"/> class.
/// </summary>
public class VisualSymbolScannerTests
{
    private static readonly Point Origin = new(0, 0);

    /// <summary>
    /// Tests that an empty diagram still returns the hardcoded "THE PROPS" entry.
    /// </summary>
    [Fact]
    public void Scan_EmptyDiagram_ReturnsOnlyThePropsHardcodedEntry()
    {
        BlazorDiagram diagram = new();

        IReadOnlyList<VisualSymbolEntry> symbols = VisualSymbolScanner.Scan(diagram);

        symbols.Count.ShouldBe(1);
        symbols[0].Name.ShouldBe("THE PROPS");
        symbols[0].Kind.ShouldBe(VisualSymbolKind.Variable);
        symbols[0].TypeDisplay.ShouldBe("CONSERVATIVE LITTLE LIST OF YARN");
    }

    /// <summary>
    /// Tests that a scalar declaration node produces a variable entry with its mapped type keyword.
    /// </summary>
    [Fact]
    public void Scan_WithScalarDeclaration_AddsVariableEntryWithMappedTypeKeyword()
    {
        BlazorDiagram diagram = new();
        TopsyTurvyVisualNodeModel declaration = AddNode(diagram, "n1", "DeclarationNode");
        declaration.SymbolIdentifierNodeName = "count";
        declaration.NodeLiteralType = LiteralType.Integer;

        IReadOnlyList<VisualSymbolEntry> symbols = VisualSymbolScanner.Scan(diagram);

        VisualSymbolEntry entry = symbols.Single(s => s.Name == "count");
        entry.Kind.ShouldBe(VisualSymbolKind.Variable);
        entry.TypeDisplay.ShouldBe("PEER");
    }

    /// <summary>
    /// Tests that an array declaration node type display uses "LITTLE LIST OF" with the element type keyword
    /// when the element type is set, or a plain fallback when it is not.
    /// </summary>
    /// <param name="elementType">The element type of the array declaration node.</param>
    /// <param name="expectedTypeDisplay">The expected type display string.</param>
    [Theory]
    [InlineData(LiteralType.Integer, "LITTLE LIST OF PEER")]
    [InlineData(null, "LITTLE LIST")]
    public void Scan_WithArrayDeclaration_UsesLittleListOfElementTypeOrPlainFallback(LiteralType? elementType, string expectedTypeDisplay)
    {
        BlazorDiagram diagram = new();
        TopsyTurvyVisualNodeModel declaration = AddNode(diagram, "n1", "ArrayDeclarationNode");
        declaration.SymbolIdentifierNodeName = "items";
        declaration.ArrayElementLiteralType = elementType;

        IReadOnlyList<VisualSymbolEntry> symbols = VisualSymbolScanner.Scan(diagram);

        VisualSymbolEntry entry = symbols.Single(symbol => symbol.Name == "items");
        entry.TypeDisplay.ShouldBe(expectedTypeDisplay);
    }

    /// <summary>
    /// Tests that a declaration node missing a name is skipped.
    /// </summary>
    [Fact]
    public void Scan_WithDeclarationMissingName_IsSkipped()
    {
        BlazorDiagram diagram = new();
        AddNode(diagram, "n1", "DeclarationNode");

        IReadOnlyList<VisualSymbolEntry> symbols = VisualSymbolScanner.Scan(diagram);

        symbols.Count.ShouldBe(1);
        symbols[0].Name.ShouldBe("THE PROPS");
    }

    /// <summary>
    /// Tests that a function-body opener node parameter names are read from its Data Out port labels.
    /// </summary>
    [Fact]
    public void Scan_WithFunctionBodyOpener_UsesDataOutPortLabelsForParameterNames()
    {
        BlazorDiagram diagram = new();
        TopsyTurvyVisualNodeModel opener = AddNode(diagram, "n1", "FunctionBodyOpener", VisualNodeKind.Function);
        opener.SymbolIdentifierNodeName = "compute";
        opener.AddPort(new TopsyTurvyVisualPortModel(opener, PortAlignment.Right, "x : PEER", VisualPortRole.DataOut));
        opener.AddPort(new TopsyTurvyVisualPortModel(opener, PortAlignment.Right, "y : PEER", VisualPortRole.DataOut));

        IReadOnlyList<VisualSymbolEntry> symbols = VisualSymbolScanner.Scan(diagram);

        VisualSymbolEntry entry = symbols.Single(symbol => symbol.Name == "compute");
        entry.Kind.ShouldBe(VisualSymbolKind.Function);
        entry.ParameterNames.ShouldBe(["x", "y"]);
    }

    /// <summary>
    /// Tests that when a function-body opener node has no Data Out port labels, parameter names fall back to
    /// reading the linked TERM node names from its "Param N" Data In ports.
    /// </summary>
    [Fact]
    public void Scan_WithFunctionBodyOpenerLackingDataOutPorts_FallsBackToLinkedTermNodeNames()
    {
        BlazorDiagram diagram = new();
        TopsyTurvyVisualNodeModel opener = AddNode(diagram, "n1", "FunctionBodyOpener", VisualNodeKind.Function);
        opener.SymbolIdentifierNodeName = "compute";
        TopsyTurvyVisualPortModel paramPort = new(opener, PortAlignment.Left, "Param 1", VisualPortRole.DataIn);
        opener.AddPort(paramPort);
        TopsyTurvyVisualNodeModel term = AddNode(diagram, "n2", "ParameterNode", VisualNodeKind.Parameter);
        term.SymbolIdentifierNodeName = "x";
        TopsyTurvyVisualPortModel termOutPort = new(term, PortAlignment.Right, "Out", VisualPortRole.DataOut);
        term.AddPort(termOutPort);
        diagram.Links.Add(new LinkModel(termOutPort, paramPort));

        IReadOnlyList<VisualSymbolEntry> symbols = VisualSymbolScanner.Scan(diagram);

        VisualSymbolEntry entry = symbols.Single(symbol => symbol.Name == "compute");
        entry.ParameterNames.ShouldBe(["x"]);
    }

    /// <summary>
    /// Tests that a parameter node attributes its owner function name from the linked TERM node, via
    /// the function-body opener "Param N" port wiring.
    /// </summary>
    [Fact]
    public void Scan_WithParameterNode_AttributesOwnerFunctionNameFromLinkedTermNode()
    {
        BlazorDiagram diagram = new();
        TopsyTurvyVisualNodeModel opener = AddNode(diagram, "n1", "FunctionBodyOpener", VisualNodeKind.Function);
        opener.SymbolIdentifierNodeName = "compute";
        TopsyTurvyVisualPortModel paramPort = new(opener, PortAlignment.Left, "Param 1", VisualPortRole.DataIn);
        opener.AddPort(paramPort);
        TopsyTurvyVisualNodeModel term = AddNode(diagram, "n2", "ParameterNode", VisualNodeKind.Parameter);
        term.SymbolIdentifierNodeName = "x";
        term.NodeLiteralType = LiteralType.Integer;
        TopsyTurvyVisualPortModel termOutPort = new(term, PortAlignment.Right, "Out", VisualPortRole.DataOut);
        term.AddPort(termOutPort);
        diagram.Links.Add(new LinkModel(termOutPort, paramPort));

        IReadOnlyList<VisualSymbolEntry> symbols = VisualSymbolScanner.Scan(diagram);

        VisualSymbolEntry entry = symbols.Single(s => s.Kind == VisualSymbolKind.Parameter && s.Name == "x");
        entry.TypeDisplay.ShouldBe("PEER");
        entry.OwnerName.ShouldBe("compute");
    }

    /// <summary>
    /// Tests that a try-catch opener node caught value name produces a variable entry.
    /// </summary>
    [Fact]
    public void Scan_WithTryCatchOpener_AddsCaughtValueAsVariableEntry()
    {
        BlazorDiagram diagram = new();
        TopsyTurvyVisualNodeModel opener = AddNode(diagram, "n1", "TryCatchOpener", VisualNodeKind.ErrorHandling);
        opener.SymbolIdentifierNodeName = "error";

        IReadOnlyList<VisualSymbolEntry> symbols = VisualSymbolScanner.Scan(diagram);

        VisualSymbolEntry entry = symbols.Single(s => s.Name == "error");
        entry.Kind.ShouldBe(VisualSymbolKind.Variable);
    }

    /// <summary>
    /// Adds a node to the diagram.
    /// </summary>
    /// <param name="diagram">The diagram to add the node to.</param>
    /// <param name="id">The ID of the node.</param>
    /// <param name="statementType">The statement type of the node.</param>
    /// <param name="kind">The kind of the node.</param>
    /// <returns>The added node.</returns>
    private static TopsyTurvyVisualNodeModel AddNode(BlazorDiagram diagram, string id, string statementType, VisualNodeKind kind = VisualNodeKind.Other)
    {
        TopsyTurvyVisualNodeModel node = new(id, Origin, statementType, null, kind)
        {
            StatementType = statementType
        };

        diagram.Nodes.Add(node);
        return node;
    }
}
