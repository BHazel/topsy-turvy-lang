using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Try-catch and throw tests for the <see cref="VisualGraphBuilder"/> class.
/// </summary>
public class VisualGraphBuilderExceptionTests : VisualGraphBuilderTestBase
{
    /// <summary>
    /// Tests that a try-catch subtitle shows the caught value name.
    /// </summary>
    [Fact]
    public void Build_WithTryCatch_SubtitleShowsCaughtValueName()
    {
        TryCatchNode tryCatch = new()
        {
            Operation = IntegerLiteral(1),
            SuccessBlock = [],
            ExceptionBlock = [],
            CaughtValueName = "err",
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(tryCatch));

        Node(diagram, "TryCatchOpener").Subtitle.ShouldBe("catch: err");
    }

    /// <summary>
    /// Tests that a try-catch creates Success and Error Branch Out ports, each with the correct header title.
    /// </summary>
    [Fact]
    public void Build_WithTryCatch_CreatesSuccessAndErrorBranchesWithHeaders()
    {
        TryCatchNode tryCatch = new()
        {
            Operation = IntegerLiteral(1),
            SuccessBlock = [],
            ExceptionBlock = [],
            CaughtValueName = "err",
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(tryCatch));

        TopsyTurvyVisualNodeModel opener = Node(diagram, "TryCatchOpener");
        Port(opener, "Success", VisualPortRole.BranchOut).ShouldNotBeNull();
        Port(opener, "Error", VisualPortRole.BranchOut).ShouldNotBeNull();
        Node(diagram, "TryCatchSuccessBranch").Title.ShouldBe("WITH GRATITUDE");
        Node(diagram, "TryCatchErrorBranch").Title.ShouldBe("MODIFIED RAPTURE,");
    }

    /// <summary>
    /// Tests that a throw statement wires its Value expression to a "Value" Data In port.
    /// </summary>
    [Fact]
    public void Build_WithThrow_WiresValuePort()
    {
        ThrowNode statement = new() { Value = IntegerLiteral(1), Span = PlaceholderSpan };

        BlazorDiagram diagram = Build(WrapInProgram(statement));

        IsLinked(Port(Node(diagram, "ThrowNode"), "Value", VisualPortRole.DataIn)).ShouldBeTrue();
    }
}
