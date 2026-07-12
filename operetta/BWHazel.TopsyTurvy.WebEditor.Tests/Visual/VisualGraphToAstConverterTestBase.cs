using System.Linq;
using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Base class for <see cref="VisualGraphToAstConverter"/> tests.
/// </summary>
public abstract class VisualGraphToAstConverterTestBase
{
    /// <summary>
    /// A placeholder source span used when constructing AST nodes for test setup.
    /// </summary>
    protected static readonly SourceSpan PlaceholderSpan = new(new(0, 0), new(0, 0));

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
    /// Builds a diagram from the given programme using <see cref="VisualGraphBuilder"/>.
    /// </summary>
    /// <param name="program">The programme to build.</param>
    /// <returns>The built diagram.</returns>
    protected static BlazorDiagram Build(ProgramNode program) =>
        new VisualGraphBuilder().Build(program, isReadOnly: false);

    /// <summary>
    /// Converts the given diagram back into a <see cref="ProgramNode"/> using <see cref="VisualGraphToAstConverter"/>.
    /// </summary>
    /// <param name="diagram">The diagram to convert.</param>
    /// <returns>The reconstructed programme.</returns>
    protected static ProgramNode Convert(BlazorDiagram diagram) => new VisualGraphToAstConverter().Convert(diagram);

    /// <summary>
    /// Builds a diagram from the given programme and converts it back into a <see cref="ProgramNode"/>, exercising
    /// the full <see cref="VisualGraphBuilder"/> -&gt; <see cref="VisualGraphToAstConverter"/> round trip.
    /// </summary>
    /// <param name="program">The programme to round-trip.</param>
    /// <returns>The reconstructed programme.</returns>
    protected static ProgramNode RoundTrip(ProgramNode program) => Convert(Build(program));

    /// <summary>
    /// Builds a diagram from the given programme, detaches the original AST node from the node matching
    /// <paramref name="statementType"/>.
    /// </summary>
    /// <param name="program">The programme to round-trip.</param>
    /// <param name="statementType">The statement type of the node whose original AST link should be detached.</param>
    /// <returns>The reconstructed programme.</returns>
    protected static ProgramNode RoundTripAsFactory(ProgramNode program, string statementType)
    {
        BlazorDiagram diagram = Build(program);
        diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().First(node => node.StatementType == statementType).AstNode = null;
        return Convert(diagram);
    }

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
    /// Creates a <see cref="PrintNode"/> with the given integer literal value.
    /// </summary>
    /// <param name="value">The integer value for the print statement.</param>
    /// <returns>A <see cref="PrintNode"/> representing the print statement.</returns>
    protected static PrintNode PrintStatement(int value) => new()
    {
        Expression = IntegerLiteral(value),
        Span = PlaceholderSpan
    };

    /// <summary>
    /// Wraps the given expression in a <see cref="PrintNode"/> and round-trips it through
    /// <see cref="VisualGraphBuilder"/> and <see cref="VisualGraphToAstConverter"/>.
    /// </summary>
    /// <param name="expression">The expression to round-trip.</param>
    /// <returns>The reconstructed programme.</returns>
    protected static ProgramNode RoundTripExpression(Expression expression) =>
        RoundTrip(WrapInProgram(
            new PrintNode()
            {
                Expression = expression,
                Span = PlaceholderSpan
            }));

    /// <summary>
    /// Extracts the expression from a <see cref="ProgramNode"/> containing a single <see cref="PrintNode"/>.
    /// </summary>
    /// <param name="program">The programme containing the print node.</param>
    /// <returns>The extracted expression.</returns>
    protected static Expression ExtractExpression(ProgramNode program) =>
        program.Statements
            .OfType<PrintNode>()
            .Single().Expression;
}
