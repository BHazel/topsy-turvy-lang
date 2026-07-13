using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Function definition, parameter and return round-trip tests for the <see cref="VisualGraphToAstConverter"/> class.
/// </summary>
public class VisualGraphToAstConverterFunctionTests : VisualGraphToAstConverterTestBase
{
    /// <summary>
    /// Tests that a function definition round-trips with its name, parameters, return type and body preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_FunctionDefinition_PreservesNameParametersReturnTypeAndBody()
    {
        FunctionDefinitionNode function = new()
        {
            Name = "compute",
            NameSpan = PlaceholderSpan,
            Parameters = [new TypedParameter("x", LiteralType.Integer, PlaceholderSpan)],
            ReturnType = LiteralType.Integer,
            Body = [new PrintNode { Expression = new IdentifierNode { Name = "x", Span = PlaceholderSpan }, Span = PlaceholderSpan }],
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(function));

        FunctionDefinitionNode result = reconstructed.Statements.OfType<FunctionDefinitionNode>().Single();
        result.Name.ShouldBe("compute");
        result.Parameters.Count.ShouldBe(1);
        result.Parameters[0].Name.ShouldBe("x");
        result.ReturnType.ShouldBe(LiteralType.Integer);
        result.Body.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a function with no return type stays without one after round-tripping.
    /// </summary>
    [Fact]
    public void RoundTrip_FunctionWithNoReturnType_StaysNull()
    {
        FunctionDefinitionNode function = new() { Name = "f", NameSpan = PlaceholderSpan, Parameters = [], Body = [], Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(function));

        reconstructed.Statements.OfType<FunctionDefinitionNode>().Single().ReturnType.ShouldBeNull();
    }

    /// <summary>
    /// Tests that a top-level programme return round-trips with its value preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_ProgrammeReturn_PreservesValue()
    {
        ProgrammeReturnNode statement = new() { Value = new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan }, Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(statement));

        ((LiteralNode)reconstructed.Statements.OfType<ProgrammeReturnNode>().Single().Value).Value.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a function return statement round-trips correctly both with and without a value.
    /// </summary>
    /// <param name="hasValue">A value indicating whether the return statement has a value.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RoundTrip_Return_WithAndWithoutValue(bool hasValue)
    {
        FunctionDefinitionNode function = new()
        {
            Name = "f",
            NameSpan = PlaceholderSpan,
            Parameters = [],
            Body = [new ReturnNode
            {
                Value = hasValue ? new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan } : null,
                Span = PlaceholderSpan,
            }],
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(function));

        ReturnNode result = reconstructed.Statements.OfType<FunctionDefinitionNode>().Single().Body.OfType<ReturnNode>().Single();
        if (hasValue)
        {
            result.Value.ShouldNotBeNull();
        }
        else
        {
            result.Value.ShouldBeNull();
        }
    }

    /// <summary>
    /// Tests that a function body opener node with no original AST link is reconstructed via the factory path.
    /// </summary>
    [Fact]
    public void Factory_FunctionBodyOpener_ReconstructsViaFunctionBodyFactory()
    {
        FunctionDefinitionNode function = new()
        {
            Name = "compute",
            NameSpan = PlaceholderSpan,
            Parameters = [new TypedParameter("x", LiteralType.Integer, PlaceholderSpan)],
            Body = [new PrintNode { Expression = new IdentifierNode { Name = "x", Span = PlaceholderSpan }, Span = PlaceholderSpan }],
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTripAsFactory(WrapInProgram(function), "FunctionBodyOpener");

        FunctionDefinitionNode result = reconstructed.Statements.OfType<FunctionDefinitionNode>().Single();
        result.Name.ShouldBe("compute");
        result.Parameters.Count.ShouldBe(1);
        result.Body.Count.ShouldBe(1);
    }
}
