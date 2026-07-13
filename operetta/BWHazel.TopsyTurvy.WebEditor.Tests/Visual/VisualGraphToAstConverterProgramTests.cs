using System.Linq;
using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Program-level, declaration-block, import, print and expression-statement round-trip tests for the
/// <see cref="VisualGraphToAstConverter"/> class.
/// </summary>
public class VisualGraphToAstConverterProgramTests : VisualGraphToAstConverterTestBase
{
    /// <summary>
    /// Tests that a programme title and subtitle round-trip, both with and without a subtitle.
    /// </summary>
    [Theory]
    [InlineData("The Mikado", "The Town of Titipu")]
    [InlineData("The Mikado", null)]
    public void RoundTrip_ProgramTitleAndSubtitle_ArePreserved(string title, string? subtitle)
    {
        ProgramNode program = new() { Title = title, Subtitle = subtitle, Statements = [], Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTrip(program);

        reconstructed.Title.ShouldBe(title);
        reconstructed.Subtitle.ShouldBe(subtitle);
    }

    /// <summary>
    /// Tests that PRINCIPALS block declarations round-trip in their original order.
    /// </summary>
    [Fact]
    public void RoundTrip_PrincipalBlockDeclarations_CollectsAllFloatingDeclarationsInOrder()
    {
        DeclarationNode first = new() { Name = "a", Type = LiteralType.Integer, Span = PlaceholderSpan };
        DeclarationNode second = new() { Name = "b", Type = LiteralType.Integer, Span = PlaceholderSpan };
        PrincipalBlockNode principals = new() { Declarations = [first, second], Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(principals));

        PrincipalBlockNode result = reconstructed.Statements.OfType<PrincipalBlockNode>().Single();
        result.Declarations.Count.ShouldBe(2);
        ((DeclarationNode)result.Declarations[0]).Name.ShouldBe("a");
        ((DeclarationNode)result.Declarations[1]).Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that an import statement round-trips with its file path preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_Import_PreservesFilePath()
    {
        ImportNode import = new() { FilePath = "utils.topsy", Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(import));

        reconstructed.Statements.OfType<ImportNode>().Single().FilePath.ShouldBe("utils.topsy");
    }

    /// <summary>
    /// Tests that a print statement round-trips with its expression and suppress-newline flag preserved.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RoundTrip_Print_PreservesExpressionAndSuppressNewlineFlag(bool suppressNewline)
    {
        PrintNode print = new()
        {
            Expression = new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan },
            SuppressNewline = suppressNewline,
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(print));

        PrintNode result = reconstructed.Statements.OfType<PrintNode>().Single();
        result.SuppressNewline.ShouldBe(suppressNewline);
        ((LiteralNode)result.Expression).Value.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a bare expression statement round-trips with its expression preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_ExpressionStatement_PreservesBareExpression()
    {
        ExpressionStatement statement = new() { Expression = new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan }, Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(statement));

        ((LiteralNode)reconstructed.Statements.OfType<ExpressionStatement>().Single().Expression).Value.ShouldBe(1);
    }

    /// <summary>
    /// Tests that editing the HARK! node title and subtitle directly in the visual layer, as the editor
    /// UI does, overrides the original programme title and subtitle on conversion.
    /// </summary>
    [Fact]
    public void Convert_WithHarkNodeMetadataEditedInVisualLayer_OverridesOriginalProgramTitleAndSubtitle()
    {
        ProgramNode program = new() { Title = "Original Title", Subtitle = "Original Subtitle", Statements = [], Span = PlaceholderSpan };
        BlazorDiagram diagram = Build(program);

        TopsyTurvyVisualNodeModel harkNode = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(node => node.Title == "HARK!");
        harkNode.SymbolIdentifierNodeName = "Edited Title";
        harkNode.LiteralValue = "Edited Subtitle";

        ProgramNode reconstructed = Convert(diagram);

        reconstructed.Title.ShouldBe("Edited Title");
        reconstructed.Subtitle.ShouldBe("Edited Subtitle");
    }
}
