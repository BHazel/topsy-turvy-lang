using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Base class for <see cref="VisualGraphBuilder"/> tests.
/// </summary>
public abstract class VisualGraphBuilderTestBase
{
    /// <summary>
    /// A placeholder source span used when constructing AST nodes for test setup.
    /// </summary>
    protected static readonly SourceSpan PlaceholderSpan = new(new(0, 0), new(0, 0));

    /// <summary>
    /// Builds a diagram from the given programme using <see cref="VisualGraphBuilder"/>.
    /// </summary>
    /// <param name="program">The programme to build.</param>
    /// <returns>The built diagram.</returns>
    protected static BlazorDiagram Build(ProgramNode program) =>
        new VisualGraphBuilder().Build(program, isReadOnly: false);

    /// <summary>
    /// Wraps the given statements in a minimal <see cref="ProgramNode"/> for test setup.
    /// </summary>
    /// <param name="statements">The statements to include.</param>
    /// <returns>A <see cref="ProgramNode"/> containing the statements.</returns>
    protected static ProgramNode WrapInProgram(params Statement[] statements) => new()
    {
        Title = "Test",
        Statements = statements,
        Span = PlaceholderSpan,
    };

    /// <summary>
    /// Finds the single node with the given statement type in the diagram.
    /// </summary>
    /// <param name="diagram">The diagram to search.</param>
    /// <param name="statementType">The statement type to match.</param>
    /// <returns>The matching node.</returns>
    protected static TopsyTurvyVisualNodeModel Node(BlazorDiagram diagram, string statementType) =>
        diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .Single(node => node.StatementType == statementType);

    /// <summary>
    /// Finds all nodes with the given statement type in the diagram.
    /// </summary>
    /// <param name="diagram">The diagram to search.</param>
    /// <param name="statementType">The statement type to match.</param>
    /// <returns>The matching nodes.</returns>
    protected static IEnumerable<TopsyTurvyVisualNodeModel> Nodes(BlazorDiagram diagram, string statementType) =>
        diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .Where(node => node.StatementType == statementType);

    /// <summary>
    /// Finds a port with the given label and role on the given node.
    /// </summary>
    /// <param name="node">The node to search.</param>
    /// <param name="label">The port label to match.</param>
    /// <param name="role">The port role to match.</param>
    /// <returns>The matching port, or <c>null</c> if none is found.</returns>
    protected static TopsyTurvyVisualPortModel? Port(TopsyTurvyVisualNodeModel node, string label, VisualPortRole role) =>
        node.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .SingleOrDefault(port => port.Label == label && port.Role == role);

    /// <summary>
    /// Tests whether the given port has at least one link attached to it.
    /// </summary>
    /// <param name="port">The port to check.</param>
    /// <returns><c>true</c> if the port has at least one link, otherwise <c>false</c>.</returns>
    protected static bool IsLinked(TopsyTurvyVisualPortModel? port) => port is not null && port.Links.Count > 0;

    /// <summary>
    /// Creates an integer literal expression.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A <see cref="LiteralNode"/> representing the integer literal.</returns>
    protected static LiteralNode IntegerLiteral(int value) => new()
    {
        Value = value,
        Type = LiteralType.Integer,
        Span = PlaceholderSpan
    };

    /// <summary>
    /// Creates an identifier expression referencing the given name.
    /// </summary>
    /// <param name="name">The identifier name.</param>
    /// <returns>An <see cref="IdentifierNode"/> referencing the name.</returns>
    protected static IdentifierNode Identifier(string name) => new()
    {
        Name = name,
        Span = PlaceholderSpan
    };
}
