using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for namespace declaration and namespace recognition forms parsed by the <see cref="TopsyTurvyParser"/> class.
/// </summary>
public class TopsyTurvyParserNamespaceTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that a single-segment TOWN declaration produces a <see cref="NamespaceDeclarationNode"/> with a one-element path.
    /// </summary>
    [Fact]
    public void Parse_NamespaceDeclaration_SingleSegment_SetsPath()
    {
        NamespaceDeclarationNode node = this.ParseFirstStatement<NamespaceDeclarationNode>("TOWN Mathematical");

        node.Path.ShouldBe(["Mathematical"]);
    }

    /// <summary>
    /// Tests that a long-form WITH DISTRICT chain produces a <see cref="NamespaceDeclarationNode"/> with each segment in order.
    /// </summary>
    [Fact]
    public void Parse_NamespaceDeclaration_LongFormDistrictChain_SetsPath()
    {
        NamespaceDeclarationNode node = this.ParseFirstStatement<NamespaceDeclarationNode>("TOWN Accounts WITH DISTRICT Payroll");

        node.Path.ShouldBe(["Accounts", "Payroll"]);
    }

    /// <summary>
    /// Tests that a short-form <c>*</c>-joined chain produces the same <see cref="NamespaceDeclarationNode"/> path as the long-form.
    /// </summary>
    [Fact]
    public void Parse_NamespaceDeclaration_ShortFormStarChain_SetsPath()
    {
        NamespaceDeclarationNode node = this.ParseFirstStatement<NamespaceDeclarationNode>("TOWN Accounts*Payroll");

        node.Path.ShouldBe(["Accounts", "Payroll"]);
    }

    /// <summary>
    /// Tests that a three-level long-form WITH DISTRICT chain produces a <see cref="NamespaceDeclarationNode"/> with all three segments in order.
    /// </summary>
    [Fact]
    public void Parse_NamespaceDeclaration_ThreeLevelDistrictChain_SetsPath()
    {
        NamespaceDeclarationNode node = this.ParseFirstStatement<NamespaceDeclarationNode>("TOWN A WITH DISTRICT B WITH DISTRICT C");

        node.Path.ShouldBe(["A", "B", "C"]);
    }

    /// <summary>
    /// Tests that a single-segment PRAY RECOGNISE directive produces a <see cref="RecogniseNode"/> with a one-element path.
    /// </summary>
    [Fact]
    public void Parse_RecogniseStatement_SingleSegment_SetsPath()
    {
        RecogniseNode node = this.ParseFirstStatement<RecogniseNode>("PRAY RECOGNISE Mathematical");

        node.Path.ShouldBe(["Mathematical"]);
    }

    /// <summary>
    /// Tests that a long-form WITH DISTRICT chain in a PRAY RECOGNISE directive produces a <see cref="RecogniseNode"/> with each segment in order.
    /// </summary>
    [Fact]
    public void Parse_RecogniseStatement_LongFormDistrictChain_SetsPath()
    {
        RecogniseNode node = this.ParseFirstStatement<RecogniseNode>("PRAY RECOGNISE Accounts WITH DISTRICT Payroll");

        node.Path.ShouldBe(["Accounts", "Payroll"]);
    }

    /// <summary>
    /// Tests that a short-form <c>*</c>-joined chain in a PRAY RECOGNISE directive produces the same <see cref="RecogniseNode"/> path as the long-form.
    /// </summary>
    [Fact]
    public void Parse_RecogniseStatement_ShortFormStarChain_SetsPath()
    {
        RecogniseNode node = this.ParseFirstStatement<RecogniseNode>("PRAY RECOGNISE Accounts*Payroll");

        node.Path.ShouldBe(["Accounts", "Payroll"]);
    }

    /// <summary>
    /// Tests that a <see cref="NamespaceDeclarationNode"/> carries a real source span starting at the TOWN keyword.
    /// </summary>
    [Fact]
    public void Parse_NamespaceDeclarationNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nTOWN Mathematical\nFINALE.");

        NamespaceDeclarationNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<NamespaceDeclarationNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <see cref="RecogniseNode"/> carries a real source span starting at the PRAY RECOGNISE keyword.
    /// </summary>
    [Fact]
    public void Parse_RecogniseNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nPRAY RECOGNISE Mathematical\nFINALE.");

        RecogniseNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<RecogniseNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a TOWN declaration nested inside a function body fails to parse, since namespace declarations are valid only as a top-level statement.
    /// </summary>
    [Fact]
    public void Parse_NamespaceDeclarationInsideFunctionBody_ReturnsDiagnostic()
    {
        string source = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM Foo UNDER NO OBLIGATION
              TOWN NotAllowedHere
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        ParseResult result = this.parser.TryParse(source);

        result.Diagnostics.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Tests that a PRAY RECOGNISE directive nested inside a function body fails to parse, since it is valid only as a top-level statement.
    /// </summary>
    [Fact]
    public void Parse_RecogniseStatementInsideFunctionBody_ReturnsDiagnostic()
    {
        string source = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM Foo UNDER NO OBLIGATION
              PRAY RECOGNISE NotAllowedHere
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        ParseResult result = this.parser.TryParse(source);

        result.Diagnostics.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Parses a single statement of a specific type from a source string.
    /// </summary>
    /// <typeparam name="T">The type of statement to parse.</typeparam>
    /// <param name="statementSource">The source string containing the statement.</param>
    /// <remarks>
    /// Wraps the statement in a minimal program structure to allow parsing, and asserts that exactly one statement of the expected type is produced.
    /// </remarks>
    /// <returns>The parsed statement of the specified type.</returns>
    private T ParseFirstStatement<T>(string statementSource) where T : Statement
    {
        ProgramNode program = this.parser.Parse($"HARK! \"T\" {statementSource} FINALE.");
        Statement statement = program.Statements.ShouldHaveSingleItem();
        return statement.ShouldBeOfType<T>();
    }
}
