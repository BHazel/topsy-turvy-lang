using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Switch tests for the <see cref="VisualGraphBuilder"/> class.
/// </summary>
public class VisualGraphBuilderSwitchTests : VisualGraphBuilderTestBase
{
    /// <summary>
    /// Tests that a case Branch Out port is labelled with the case literal string representation.
    /// </summary>
    [Fact]
    public void Build_WithSwitchCase_PortLabelUsesCaseLiteralToString()
    {
        SwitchNode switchNode = new()
        {
            Expression = Identifier("office"),
            Cases = [new SwitchCase("Private Secretary", [])],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(switchNode));

        Port(Node(diagram, "SwitchOpener"), "Private Secretary", VisualPortRole.BranchOut).ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that a case header node carries its literal type and value, so the Visual Editor case
    /// literal editing controls have somewhere to read the loaded value from.
    /// </summary>
    /// <param name="literal">The literal value to test.</param>
    /// <param name="expectedType">The expected literal type.</param>
    /// <param name="expectedValue">The expected literal value string representation.</param>
    [Theory]
    [InlineData("Private Secretary", LiteralType.String, "Private Secretary")]
    [InlineData(true, LiteralType.Boolean, "True")]
    [InlineData(1, LiteralType.Integer, "1")]
    public void Build_WithSwitchCase_HeaderCarriesLiteralTypeAndValue(object literal, LiteralType expectedType, string expectedValue)
    {
        SwitchNode switchNode = new()
        {
            Expression = Identifier("office"),
            Cases = [new SwitchCase(literal, [])],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(switchNode));

        TopsyTurvyVisualNodeModel header = Node(diagram, "SwitchCaseBranch");
        header.NodeLiteralType.ShouldBe(expectedType);
        header.LiteralValue.ShouldBe(expectedValue);
    }

    /// <summary>
    /// Tests that a case with a <c>null</c> literal falls back to a "case N" port label (lower case)
    /// and a "Case N" header subtitle (upper case).
    /// </summary>
    [Fact]
    public void Build_WithSwitchCaseHavingNullLiteral_PortLabelAndHeaderSubtitleUseDifferentFallbackCasing()
    {
        SwitchNode switchNode = new()
        {
            Expression = Identifier("office"),
            Cases = [new SwitchCase(null, [])],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(switchNode));

        Port(Node(diagram, "SwitchOpener"), "case 1", VisualPortRole.BranchOut).ShouldNotBeNull();
        Node(diagram, "SwitchCaseBranch").Subtitle.ShouldBe("Case 1");
    }

    /// <summary>
    /// Tests that a non-empty default block creates a "Default" Branch Out port with a
    /// "FAILING ALL OF THE ABOVE," header.
    /// </summary>
    [Fact]
    public void Build_WithSwitchDefaultBlock_CreatesDefaultBranchPortWithHeader()
    {
        SwitchNode switchNode = new()
        {
            Expression = Identifier("office"),
            Cases = [],
            DefaultBlock = [new PrintNode { Expression = IntegerLiteral(1), Span = PlaceholderSpan }],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(switchNode));

        Port(Node(diagram, "SwitchOpener"), "Default", VisualPortRole.BranchOut).ShouldNotBeNull();
        Node(diagram, "SwitchDefaultBranch").Title.ShouldBe("FAILING ALL OF THE ABOVE,");
    }

    /// <summary>
    /// Tests that an empty default block does not create a "Default" Branch Out port.
    /// </summary>
    [Fact]
    public void Build_WithEmptyDefaultBlock_CreatesNoDefaultBranchPort()
    {
        SwitchNode switchNode = new()
        {
            Expression = Identifier("office"),
            Cases = [],
            DefaultBlock = [],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(switchNode));

        Port(Node(diagram, "SwitchOpener"), "Default", VisualPortRole.BranchOut).ShouldBeNull();
    }
}
