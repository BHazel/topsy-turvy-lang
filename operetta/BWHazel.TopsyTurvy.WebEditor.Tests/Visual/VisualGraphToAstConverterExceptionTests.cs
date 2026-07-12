using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Try-catch and throw round-trip tests for the <see cref="VisualGraphToAstConverter"/> class.
/// </summary>
public class VisualGraphToAstConverterExceptionTests : VisualGraphToAstConverterTestBase
{
    /// <summary>
    /// Tests that a try-catch round-trips with its operation, caught value name and both blocks preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_TryCatch_PreservesOperationCaughtNameAndBothBlocks()
    {
        TryCatchNode tryCatch = new()
        {
            Operation = new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan },
            SuccessBlock = [new PrintNode { Expression = new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan }, Span = PlaceholderSpan }],
            ExceptionBlock = [new PrintNode { Expression = new LiteralNode { Value = 2, Type = LiteralType.Integer, Span = PlaceholderSpan }, Span = PlaceholderSpan }],
            CaughtValueName = "err",
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(tryCatch));

        TryCatchNode result = reconstructed.Statements.OfType<TryCatchNode>().Single();
        result.CaughtValueName.ShouldBe("err");
        result.SuccessBlock.Count.ShouldBe(1);
        result.ExceptionBlock.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a throw statement round-trips with its value preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_Throw_PreservesValue()
    {
        ThrowNode statement = new() { Value = new LiteralNode { Value = "oops", Type = LiteralType.String, Span = PlaceholderSpan }, Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(statement));

        ((LiteralNode)reconstructed.Statements.OfType<ThrowNode>().Single().Value).Value.ShouldBe("oops");
    }

    /// <summary>
    /// Tests that a try-catch node with no original AST link is reconstructed via the factory path.
    /// </summary>
    [Fact]
    public void Factory_TryCatchOpener_ReconstructsViaTryCatchFactory()
    {
        TryCatchNode tryCatch = new()
        {
            Operation = new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan },
            SuccessBlock = [new PrintNode { Expression = new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan }, Span = PlaceholderSpan }],
            ExceptionBlock = [],
            CaughtValueName = "err",
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTripAsFactory(WrapInProgram(tryCatch), "TryCatchOpener");

        reconstructed.Statements.OfType<TryCatchNode>().Single().SuccessBlock.Count.ShouldBe(1);
    }
}
