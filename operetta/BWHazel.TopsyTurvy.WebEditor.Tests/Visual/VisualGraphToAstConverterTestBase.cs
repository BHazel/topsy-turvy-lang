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
    /// Builds a diagram from the given programme and converts it back into a <see cref="ProgramNode"/>, exercising
    /// the full <see cref="VisualGraphBuilder"/> -&gt; <see cref="VisualGraphToAstConverter"/> round trip.
    /// </summary>
    /// <param name="program">The programme to round-trip.</param>
    /// <returns>The reconstructed programme.</returns>
    protected static ProgramNode RoundTrip(ProgramNode program)
    {
        VisualGraphBuilder builder = new();
        BlazorDiagram diagram = builder.Build(program, isReadOnly: false);

        VisualGraphToAstConverter converter = new();
        return converter.Convert(diagram);
    }
}
