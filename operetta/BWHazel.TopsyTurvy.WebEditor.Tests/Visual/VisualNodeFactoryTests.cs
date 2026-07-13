using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Tests for the <see cref="VisualNodeFactory"/> class.
/// </summary>
public class VisualNodeFactoryTests
{
    private static readonly Point Origin = new(0, 0);

    /// <summary>
    /// Tests that <see cref="VisualNodeFactory.CreateStatement"/> creates a single node with the correct
    /// Data In port labels for each simple, non-block, statement type.
    /// </summary>
    /// <param name="statementType">The type of statement to create.</param>
    /// <param name="expectedTitle">The expected title of the created node.</param>
    /// <param name="expectedDataInLabels">The expected labels of the Data In ports of the created node.</param>
    [Theory]
    [InlineData("DeclarationNode", "PRAY WELCOME", new[] { "Value" })]
    [InlineData("ArrayDeclarationNode", "PRAY WELCOME", new string[0])]
    [InlineData("AssignmentNode", "IS APPOINTED", new[] { "Variable", "Value" })]
    [InlineData("ArrayElementAssignmentNode", "VICTIM IS APPOINTED", new[] { "Variable", "Victim", "Value" })]
    [InlineData("PrintNode", "BEHOLD", new[] { "Expr" })]
    [InlineData("InputNode", "PRAY TELL", new[] { "Variable" })]
    [InlineData("ProgrammeReturnNode", "AND SO I FIND", new[] { "Value" })]
    [InlineData("ReturnNode", "AND SO I FIND", new[] { "Value" })]
    [InlineData("ThrowNode", "A HIDEOUS CURSE ON", new[] { "Value" })]
    [InlineData("ImportNode", "PRAY ADMIT", new string[0])]
    [InlineData("NamespaceDeclarationNode", "TOWN", new string[0])]
    [InlineData("RecogniseNode", "PRAY RECOGNISE", new string[0])]
    [InlineData("AssertNode", "THE LAW IS", new[] { "Cond", "Msg" })]
    [InlineData("ExpressionStatement", "EXPRESSION", new[] { "Expr" })]
    public void CreateStatement_SimpleTypes_CreatesSingleNodeWithCorrectTitleAndDataInPorts(
        string statementType, string expectedTitle, string[] expectedDataInLabels)
    {
        BlazorDiagram diagram = new();

        IReadOnlyList<TopsyTurvyVisualNodeModel> created = CreateStatement(statementType, diagram);

        created.Count.ShouldBe(1);
        TopsyTurvyVisualNodeModel node = created[0];
        node.Title.ShouldBe(expectedTitle);
        node.StatementType.ShouldBe(statementType);
        node.Ports.OfType<TopsyTurvyVisualPortModel>().ShouldContain(port => port.Role == VisualPortRole.FlowIn && port.Label == "In");
        node.Ports.OfType<TopsyTurvyVisualPortModel>().ShouldContain(port => port.Role == VisualPortRole.FlowOut && port.Label == "Out");
        DataInPorts(node).Select(port => port.Label).ShouldBe(expectedDataInLabels);
    }

    /// <summary>
    /// Tests that break and continue nodes omit the Flow Out port, since they never flow onward.
    /// </summary>
    [Theory]
    [InlineData("BreakNode", "THAT WILL DO.")]
    [InlineData("ContinueNode", "ONCE MORE.")]
    public void CreateStatement_BreakAndContinue_OmitFlowOutPort(string statementType, string expectedTitle)
    {
        BlazorDiagram diagram = new();

        IReadOnlyList<TopsyTurvyVisualNodeModel> created = CreateStatement(statementType, diagram);

        created.Count.ShouldBe(1);
        created[0].Title.ShouldBe(expectedTitle);
        created[0].Ports.OfType<TopsyTurvyVisualPortModel>().ShouldContain(port => port.Role == VisualPortRole.FlowIn);
        created[0].Ports.OfType<TopsyTurvyVisualPortModel>().ShouldNotContain(port => port.Role == VisualPortRole.FlowOut);
    }

    /// <summary>
    /// Tests that a conditional block creates the opener, both branch headers, and the closer,
    /// with paired opener and closer IDs and the branch ports pre-linked to their header nodes.
    /// </summary>
    [Fact]
    public void CreateStatement_ConditionalOpener_CreatesFourNodesWithHeadersPreLinked()
    {
        BlazorDiagram diagram = new();

        IReadOnlyList<TopsyTurvyVisualNodeModel> created = CreateStatement("ConditionalOpener", diagram);

        created.Count.ShouldBe(4);
        TopsyTurvyVisualNodeModel opener = created[0];
        opener.StatementType.ShouldBe("ConditionalOpener");
        opener.PairedCloserId.ShouldNotBeNull();

        TopsyTurvyVisualNodeModel closer = created[^1];
        closer.PairedOpenerId.ShouldBe(opener.Id);
        opener.PairedCloserId.ShouldBe(closer.Id);

        diagram.Links.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that a try-catch block creates the opener, both branch headers, and the closer,
    /// with paired opener and closer IDs and the branch ports pre-linked to their header nodes.
    /// </summary>
    [Fact]
    public void CreateStatement_TryCatchOpener_CreatesFourNodesWithHeadersPreLinked()
    {
        BlazorDiagram diagram = new();

        IReadOnlyList<TopsyTurvyVisualNodeModel> created = CreateStatement("TryCatchOpener", diagram);

        created.Count.ShouldBe(4);
        TopsyTurvyVisualNodeModel opener = created[0];
        opener.StatementType.ShouldBe("TryCatchOpener");
        opener.PairedCloserId.ShouldBe(created[^1].Id);
        diagram.Links.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that a loop block creates only the opener and closer, with no automatic flow link
    /// between them.
    /// </summary>
    [Fact]
    public void CreateStatement_LoopOpener_CreatesOpenerAndCloserOnlyWithNoAutoFlowLink()
    {
        BlazorDiagram diagram = new();

        IReadOnlyList<TopsyTurvyVisualNodeModel> created = CreateStatement("LoopOpener", diagram);

        created.Count.ShouldBe(2);
        created[0].StatementType.ShouldBe("LoopOpener");
        created[0].PairedCloserId.ShouldBe(created[1].Id);
        diagram.Links.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a guard block creates only the opener and closer, with the opener Success Flow Out
    /// port pre-linked directly to the closer Flow In port.
    /// </summary>
    [Fact]
    public void CreateStatement_GuardOpener_PreLinksSuccessFlowOutDirectlyToCloser()
    {
        BlazorDiagram diagram = new();

        IReadOnlyList<TopsyTurvyVisualNodeModel> created = CreateStatement("GuardOpener", diagram);

        created.Count.ShouldBe(2);
        created[0].StatementType.ShouldBe("GuardOpener");
        diagram.Links.Count.ShouldBe(1);
        created[0].Ports.OfType<TopsyTurvyVisualPortModel>().ShouldContain(port => port.Role == VisualPortRole.BranchOut && port.Label == "Else");
    }

    /// <summary>
    /// Tests that a switch block creates the opener, a pre-linked case header, a pre-linked default
    /// header, and the closer, matching the pattern established by conditional and try-catch blocks.
    /// </summary>
    [Fact]
    public void CreateStatement_SwitchOpener_CreatesOpenerCaseHeaderDefaultHeaderAndCloser()
    {
        BlazorDiagram diagram = new();

        IReadOnlyList<TopsyTurvyVisualNodeModel> created = CreateStatement("SwitchOpener", diagram);

        created.Count.ShouldBe(4);
        created[0].StatementType.ShouldBe("SwitchOpener");
        created[0].Ports.OfType<TopsyTurvyVisualPortModel>().Count(port => port.Role == VisualPortRole.BranchOut).ShouldBe(2);

        TopsyTurvyVisualNodeModel caseHeader = created.Single(node => node.StatementType == "SwitchCaseBranch");
        TopsyTurvyVisualNodeModel defaultHeader = created.Single(node => node.StatementType == "SwitchDefaultBranch");
        caseHeader.Ports.OfType<TopsyTurvyVisualPortModel>().ShouldContain(port => port.Role == VisualPortRole.FlowIn && port.Links.Count > 0);
        defaultHeader.Ports.OfType<TopsyTurvyVisualPortModel>().ShouldContain(port => port.Role == VisualPortRole.FlowIn && port.Links.Count > 0);
        created[3].StatementType.ShouldBe("SwitchCloser");
    }

    /// <summary>
    /// Tests that a switch block case header carries a real default literal type and value, not just a
    /// cosmetic "case 1" subtitle, so a freshly-dropped switch exports as <c>WHEN ACTING AS 1</c> rather
    /// than matching <c>NAUGHT</c>.
    /// </summary>
    [Fact]
    public void CreateStatement_SwitchOpener_CaseHeaderHasDefaultLiteralTypeAndValue()
    {
        BlazorDiagram diagram = new();

        IReadOnlyList<TopsyTurvyVisualNodeModel> created = CreateStatement("SwitchOpener", diagram);

        TopsyTurvyVisualNodeModel caseHeader = created.Single(node => node.StatementType == "SwitchCaseBranch");
        caseHeader.NodeLiteralType.ShouldBe(LiteralType.Integer);
        caseHeader.LiteralValue.ShouldBe("1");
    }

    /// <summary>
    /// Tests that a function body block opener has no Flow In port and its closer has no Flow Out port,
    /// since the body subgraph is entered and exited only via its own separate wiring.
    /// </summary>
    [Fact]
    public void CreateStatement_FunctionBodyOpener_OpenerHasNoFlowInAndCloserHasNoFlowOut()
    {
        BlazorDiagram diagram = new();

        IReadOnlyList<TopsyTurvyVisualNodeModel> created = CreateStatement("FunctionBodyOpener", diagram);

        created.Count.ShouldBe(2);
        created[0].Ports.OfType<TopsyTurvyVisualPortModel>().ShouldNotContain(port => port.Role == VisualPortRole.FlowIn);
        created[1].Ports.OfType<TopsyTurvyVisualPortModel>().ShouldNotContain(port => port.Role == VisualPortRole.FlowOut);
    }

    /// <summary>
    /// Tests that an unknown statement type returns an empty list.
    /// </summary>
    [Fact]
    public void CreateStatement_UnknownType_ReturnsEmptyList()
    {
        BlazorDiagram diagram = new();

        IReadOnlyList<TopsyTurvyVisualNodeModel> created = CreateStatement("NotARealStatement", diagram);

        created.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="VisualNodeFactory.CreateExpression"/> adds a node to the diagram with a
    /// Data Out port for each simple expression type.
    /// </summary>
    /// <param name="statementType">The type of expression to create.</param>
    /// <param name="expectedTitle">The expected title of the created node.</param>
    [Theory]
    [InlineData("IdentifierNode", "name")]
    [InlineData("OperatorNode", "SUM OF")]
    [InlineData("ArithmeticNode", "SUM OF")]
    [InlineData("BitwiseNode", "CHORD OF")]
    [InlineData("LogicalNode", "BOTH")]
    [InlineData("VariadicNode", "ALL OF")]
    [InlineData("WovenNode", "WOVEN OF")]
    [InlineData("TernaryNode", "SHOULD IT TRANSPIRE THAT")]
    [InlineData("ArrayIndexNode", "VICTIM")]
    [InlineData("ArrayLengthNode", "RECKONING OF")]
    [InlineData("FunctionReferenceNode", "function")]
    public void CreateExpression_SimpleTypes_AddsNodeToDiagramWithDataOutPort(string statementType, string expectedTitle)
    {
        BlazorDiagram diagram = new();
        int counter = 0;

        TopsyTurvyVisualNodeModel? node = VisualNodeFactory.CreateExpression(statementType, Origin, diagram, ref counter);

        node.ShouldNotBeNull();
        node.Title.ShouldBe(expectedTitle);
        diagram.Nodes.ShouldContain(node);
        node.Ports.OfType<TopsyTurvyVisualPortModel>().ShouldContain(port => port.Role == VisualPortRole.DataOut);
    }

    /// <summary>
    /// Tests that a fresh literal node defaults to the string literal type.
    /// </summary>
    [Fact]
    public void CreateExpression_LiteralNode_DefaultsToStringLiteralType()
    {
        BlazorDiagram diagram = new();
        int counter = 0;

        TopsyTurvyVisualNodeModel? node = VisualNodeFactory.CreateExpression("LiteralNode", Origin, diagram, ref counter);

        node.ShouldNotBeNull();
        node.NodeLiteralType.ShouldBe(LiteralType.String);
    }

    /// <summary>
    /// Tests that a fresh expression-cast node defaults to the integer literal type with an arrow subtitle.
    /// </summary>
    [Fact]
    public void CreateExpression_ExpressionCastNode_DefaultsToIntegerLiteralTypeWithArrowSubtitle()
    {
        BlazorDiagram diagram = new();
        int counter = 0;

        TopsyTurvyVisualNodeModel? node = VisualNodeFactory.CreateExpression("ExpressionCastNode", Origin, diagram, ref counter);

        node.ShouldNotBeNull();
        node.NodeLiteralType.ShouldBe(LiteralType.Integer);
        node.Subtitle.ShouldBe("→ PEER");
    }

    /// <summary>
    /// Tests that a fresh SUMMON expression node has both Flow ports and Data ports, since it can be
    /// used either as a statement or as an expression.
    /// </summary>
    [Fact]
    public void CreateExpression_SummonNode_HasBothFlowAndDataPorts()
    {
        BlazorDiagram diagram = new();
        int counter = 0;

        TopsyTurvyVisualNodeModel? node = VisualNodeFactory.CreateExpression("SummonNode", Origin, diagram, ref counter);

        node.ShouldNotBeNull();
        node.Ports.OfType<TopsyTurvyVisualPortModel>().ShouldContain(port => port.Role == VisualPortRole.FlowIn);
        node.Ports.OfType<TopsyTurvyVisualPortModel>().ShouldContain(port => port.Role == VisualPortRole.FlowOut);
        node.Ports.OfType<TopsyTurvyVisualPortModel>().ShouldContain(port => port.Role == VisualPortRole.DataIn && port.Label == "Function");
        node.Ports.OfType<TopsyTurvyVisualPortModel>().ShouldContain(port => port.Role == VisualPortRole.DataOut);
    }

    /// <summary>
    /// Tests that an unknown expression type returns <c>null</c> without mutating the diagram.
    /// </summary>
    [Fact]
    public void CreateExpression_UnknownType_ReturnsNullAndDoesNotMutateDiagram()
    {
        BlazorDiagram diagram = new();
        int counter = 0;

        TopsyTurvyVisualNodeModel? node = VisualNodeFactory.CreateExpression("NotARealExpression", Origin, diagram, ref counter);

        node.ShouldBeNull();
        diagram.Nodes.ShouldBeEmpty();
    }

    /// <summary>
    /// Creates a statement of the given type in the given diagram, returning the created nodes.
    /// </summary>
    /// <param name="statementType">The type of statement to create.</param>
    /// <param name="diagram">The diagram to which the statement will be added.</param>
    /// <returns>The created statement nodes.</returns>
    private static IReadOnlyList<TopsyTurvyVisualNodeModel> CreateStatement(string statementType, BlazorDiagram diagram)
    {
        int counter = 0;
        return VisualNodeFactory.CreateStatement(statementType, Origin, diagram, ref counter);
    }

    /// <summary>
    /// Returns the Data In ports of the given node.
    /// </summary>
    /// <param name="node">The node whose Data In ports are to be returned.</param>
    /// <returns>The Data In ports of the given node.</returns>
    private static IReadOnlyList<TopsyTurvyVisualPortModel> DataInPorts(TopsyTurvyVisualNodeModel node) =>
        [.. node.Ports.OfType<TopsyTurvyVisualPortModel>().Where(port => port.Role == VisualPortRole.DataIn)];
}
