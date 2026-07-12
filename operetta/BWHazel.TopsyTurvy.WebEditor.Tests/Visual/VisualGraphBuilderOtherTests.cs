using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Program, import, array declaration and print tests for the <see cref="VisualGraphBuilder"/> class.
/// </summary>
public class VisualGraphBuilderOtherTests : VisualGraphBuilderTestBase
{
    /// <summary>
    /// Tests that a programme with no statements links HARK! directly to FINALE.
    /// </summary>
    [Fact]
    public void Build_WithNoStatements_LinksHarkDirectlyToFinale()
    {
        BlazorDiagram diagram = Build(WrapInProgram());

        IsLinked(Port(Node(diagram, "HarkNode"), "Out", VisualPortRole.FlowOut)).ShouldBeTrue();
        diagram.Links.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a programme with both a title and a subtitle formats both, quoted, in the HARK! node subtitle.
    /// </summary>
    [Fact]
    public void Build_WithTitleAndSubtitle_FormatsSubtitleWithBothQuoted()
    {
        ProgramNode program = new()
        {
            Title = "The Mikado",
            Subtitle = "The Town of Titipu",
            Statements = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(program);

        Node(diagram, "HarkNode").Subtitle.ShouldBe("\"The Mikado\" or, \"The Town of Titipu\"");
    }

    /// <summary>
    /// Tests that a programme with only a title formats the HARK! node subtitle as just the quoted title.
    /// </summary>
    [Fact]
    public void Build_WithTitleOnly_SubtitleIsJustQuotedTitle()
    {
        ProgramNode program = new()
        {
            Title = "The Mikado",
            Statements = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(program);

        Node(diagram, "HarkNode").Subtitle.ShouldBe("\"The Mikado\"");
    }

    /// <summary>
    /// Tests that an import statement floats in the sidebar with no incoming Flow In link.
    /// </summary>
    [Fact]
    public void Build_WithImport_FloatsInSidebarWithNoIncomingFlowLink()
    {
        ImportNode import = new()
        {
            FilePath = "utils.topsy",
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(import));

        IsLinked(Port(Node(diagram, "ImportNode"), "In", VisualPortRole.FlowIn)).ShouldBeFalse();
    }

    /// <summary>
    /// Tests that an array declaration with no initial values but a declared size uses the size as the
    /// node literal value.
    /// </summary>
    [Fact]
    public void Build_WithArrayDeclarationNoInitialValuesButSize_UsesSizeAsLiteralValue()
    {
        ArrayDeclarationNode declaration = new()
        {
            Name = "items",
            ElementType = LiteralType.Integer,
            Size = 5,
            InitialValues = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(declaration));

        Node(diagram, "ArrayDeclarationNode").LiteralValue.ShouldBe("5");
    }

    /// <summary>
    /// Tests that an array declaration with initial values creates one "Victim N" Data In port per element.
    /// </summary>
    [Fact]
    public void Build_WithArrayDeclarationInitialValues_CreatesOneVictimPortPerElement()
    {
        ArrayDeclarationNode declaration = new()
        {
            Name = "items",
            ElementType = LiteralType.Integer,
            InitialValues = [IntegerLiteral(1), IntegerLiteral(2)],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(declaration));

        TopsyTurvyVisualNodeModel node = Node(diagram, "ArrayDeclarationNode");
        IsLinked(Port(node, "Victim 1", VisualPortRole.DataIn)).ShouldBeTrue();
        IsLinked(Port(node, "Victim 2", VisualPortRole.DataIn)).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a constant array declaration subtitle includes both "CONSERVATIVE " and "LITTLE LIST OF".
    /// </summary>
    [Fact]
    public void Build_WithConstantArrayDeclaration_SubtitleIncludesConservativeAndLittleListOf()
    {
        ArrayDeclarationNode declaration = new()
        {
            Name = "items",
            ElementType = LiteralType.Integer,
            IsConstant = true,
            InitialValues = [],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(declaration));

        Node(diagram, "ArrayDeclarationNode").Subtitle.ShouldBe("items : CONSERVATIVE LITTLE LIST OF PEER");
    }

    /// <summary>
    /// Tests that each declaration inside a PRINCIPALS block floats independently, with no incoming Flow In link.
    /// </summary>
    [Fact]
    public void Build_WithPrincipalBlockDeclarations_FloatsEachDeclarationIndependently()
    {
        DeclarationNode first = new()
        {
            Name = "a",
            Type = LiteralType.Integer,
            Span = PlaceholderSpan
        };

        DeclarationNode second = new()
        {
            Name = "b",
            Type = LiteralType.Integer,
            Span = PlaceholderSpan
        };

        PrincipalBlockNode principals = new()
        {
            Declarations = [first, second],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(principals));

        TopsyTurvyVisualNodeModel[] declarations = [.. Nodes(diagram, "DeclarationNode")];
        declarations.Length.ShouldBe(2);
        foreach (TopsyTurvyVisualNodeModel node in declarations)
        {
            IsLinked(Port(node, "In", VisualPortRole.FlowIn)).ShouldBeFalse();
        }
    }

    /// <summary>
    /// Tests that a print statement suppress-newline flag is mirrored onto the visual node.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Build_WithPrintNode_MirrorsSuppressNewlineFlag(bool suppressNewline)
    {
        PrintNode print = new()
        {
            Expression = IntegerLiteral(1),
            SuppressNewline = suppressNewline,
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(print));

        Node(diagram, "PrintNode").PrintSuppressNewline.ShouldBe(suppressNewline);
    }

    /// <summary>
    /// Tests that a bare expression statement wraps its expression with an "Expr" Data In port.
    /// </summary>
    [Fact]
    public void Build_WithExpressionStatement_WrapsBareExpressionWithExprPort()
    {
        ExpressionStatement statement = new()
        {
            Expression = IntegerLiteral(1),
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(statement));

        IsLinked(Port(Node(diagram, "ExpressionStatement"), "Expr", VisualPortRole.DataIn)).ShouldBeTrue();
    }
}
