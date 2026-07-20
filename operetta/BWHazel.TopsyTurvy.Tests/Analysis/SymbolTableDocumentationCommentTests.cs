using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Analysis;

/// <summary>
/// Tests for documentation comment extraction in the <see cref="SymbolTable"/> class.
/// </summary>
public class SymbolTableDocumentationCommentTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets <c>null</c> documentation when no preceding block comment exists.
    /// </summary>
    [Fact]
    public void Build_WithVariableAndNoBlockComment_SetsDocumentationNull()
    {
        string source = """
            HARK! "Test"
            PRAY WELCOME Numbers AS A PEER
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("Numbers", out SymbolInfo? info);

        info!.Documentation.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets <c>null</c> documentation when a plain block comment (no recognised tags) precedes the declaration.
    /// </summary>
    [Fact]
    public void Build_WithPlainBlockCommentBeforeVariable_SetsDocumentationNull()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              This is just a plain block comment with no documentation tags.
            END OF ASIDE.)
            PRAY WELCOME Numbers AS A PEER
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("Numbers", out SymbolInfo? info);

        info!.Documentation.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method extracts the summary from a LEGEND tag.
    /// </summary>
    [Fact]
    public void Build_WithLegendTag_ExtractsSummary()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              LEGEND: Holds the numbers.
            END OF ASIDE.)
            PRAY WELCOME Numbers AS A PEER
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("Numbers", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        info.Documentation!.Summary.ShouldBe("Holds the numbers.");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method extracts a multi-line LEGEND tag.
    /// </summary>
    [Fact]
    public void Build_WithMultiLineLegend_ExtractsSummaryAcrossLines()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              LEGEND: First line of summary.
              Continued on the second line.
            END OF ASIDE.)
            PRAY WELCOME Numbers AS A PEER
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("Numbers", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        info.Documentation!.Summary.ShouldNotBeNull();
        info.Documentation.Summary!.ShouldContain("First line of summary.");
        info.Documentation.Summary.ShouldContain("Continued on the second line.");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method extracts remarks from a RECITATIVE tag.
    /// </summary>
    [Fact]
    public void Build_WithRecitativeTag_ExtractsRemarks()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              LEGEND: A variable.
              RECITATIVE: Some additional remarks.
            END OF ASIDE.)
            PRAY WELCOME Numbers AS A PEER
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("Numbers", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        info.Documentation!.Remarks.ShouldBe("Some additional remarks.");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets IsDeprecated when a STATUTORY tag is present.
    /// </summary>
    [Fact]
    public void Build_WithStatutoryTag_SetsIsDeprecated()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              LEGEND: Old variable.
              STATUTORY: Use NewNumbers instead.
            END OF ASIDE.)
            PRAY WELCOME Numbers AS A PEER
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("Numbers", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        info.Documentation!.IsDeprecated.ShouldBeTrue();
        info.Documentation.DeprecationMessage.ShouldBe("Use NewNumbers instead.");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets IsDeprecated true even when no message follows the STATUTORY tag.
    /// </summary>
    [Fact]
    public void Build_WithStatutoryTagAndNoMessage_SetsIsDeprecatedWithNullMessage()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              STATUTORY:
            END OF ASIDE.)
            PRAY WELCOME Numbers AS A PEER
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("Numbers", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        info.Documentation!.IsDeprecated.ShouldBeTrue();
        info.Documentation.DeprecationMessage.ShouldBeNullOrEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets <c>null</c> documentation when a non-blank code line appears between the block comment and the declaration.
    /// </summary>
    [Fact]
    public void Build_WithCodeLineBetweenBlockCommentAndDeclaration_SetsDocumentationNull()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              LEGEND: Holds the numbers.
            END OF ASIDE.)
            ASIDE: This intervening line breaks the association.
            PRAY WELCOME Numbers AS A PEER
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("Numbers", out SymbolInfo? info);

        info!.Documentation.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method finds the documentation comment even when blank lines separate it from the declaration.
    /// </summary>
    [Fact]
    public void Build_WithBlankLinesBetweenBlockCommentAndDeclaration_ExtractsDocumentation()
    {
        string source = "HARK! \"Test\"\n(ASIDE, AT SOME LENGTH:\n  LEGEND: Holds the numbers.\nEND OF ASIDE.)\n\n\nPRAY WELCOME Numbers AS A PEER\nFINALE.\n";

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("Numbers", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        info.Documentation!.Summary.ShouldBe("Holds the numbers.");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method extracts ARTICLE tags as parameter descriptions for a function.
    /// </summary>
    [Fact]
    public void Build_WithArticleTags_ExtractsParameterDescriptions()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              LEGEND: Sums the numbers in a range.
              ARTICLE Start (PEER): The starting number.
              ARTICLE End (PEER): The ending number.
            END OF ASIDE.)
            IT IS MY DUTY TO PERFORM SumRange UNDER THE TERMS OF Start AS A PEER AND End AS A PEER TO FIND PEER
              AND SO I FIND 0
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("SumRange", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        info.Documentation!.Parameters.ShouldNotBeNull();
        info.Documentation.Parameters!.ContainsKey("Start").ShouldBeTrue();
        info.Documentation.Parameters["Start"].Type.ShouldBe("PEER");
        info.Documentation.Parameters["Start"].Description.ShouldBe("The starting number.");
        info.Documentation.Parameters!.ContainsKey("End").ShouldBeTrue();
        info.Documentation.Parameters["End"].Type.ShouldBe("PEER");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method extracts the CONSEQUENCE tag as the return value description.
    /// </summary>
    [Fact]
    public void Build_WithConsequenceTag_ExtractsReturnValue()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              LEGEND: Sums a range.
              CONSEQUENCE (PEER): The total sum.
            END OF ASIDE.)
            IT IS MY DUTY TO PERFORM SumRange UNDER THE TERMS OF Start AS A PEER AND End AS A PEER TO FIND PEER
              AND SO I FIND 0
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("SumRange", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        info.Documentation!.ReturnValue.ShouldNotBeNull();
        info.Documentation.ReturnValue!.Value.Type.ShouldBe("PEER");
        info.Documentation.ReturnValue.Value.Description.ShouldBe("The total sum.");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method extracts CURSES tags as exception descriptions.
    /// </summary>
    [Fact]
    public void Build_WithCursesTag_ExtractsExceptionDescriptions()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              LEGEND: Sums a range.
              CURSES SameValues (DECREE): Thrown if Start and End are the same.
            END OF ASIDE.)
            IT IS MY DUTY TO PERFORM SumRange UNDER THE TERMS OF Start AS A PEER AND End AS A PEER TO FIND PEER
              AND SO I FIND 0
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("SumRange", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        info.Documentation!.Exceptions.ShouldNotBeNull();
        info.Documentation.Exceptions!.Count.ShouldBe(1);
        info.Documentation.Exceptions[0].Name.ShouldBe("SameValues");
        info.Documentation.Exceptions[0].Type.ShouldBe("DECREE");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method extracts a CHORUS tag as a code example.
    /// </summary>
    [Fact]
    public void Build_WithChorusTag_ExtractsExample()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              LEGEND: A function.
              CHORUS:
              SUMMON SumRange WITH 1 AND 10 IF YOU PLEASE.
            END OF ASIDE.)
            IT IS MY DUTY TO PERFORM SumRange UNDER THE TERMS OF Start AS A PEER AND End AS A PEER TO FIND PEER
              AND SO I FIND 0
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("SumRange", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        info.Documentation!.Examples.ShouldNotBeNull();
        info.Documentation.Examples!.Count.ShouldBe(1);
        info.Documentation.Examples[0].ShouldContain("SUMMON SumRange");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method extracts ENSEMBLE tags as see-also references.
    /// </summary>
    [Fact]
    public void Build_WithEnsembleTag_ExtractsSeeAlso()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              LEGEND: A function.
              ENSEMBLE: SumRange
            END OF ASIDE.)
            IT IS MY DUTY TO PERFORM OtherFunc UNDER THE TERMS OF x AS A YARN
              AND SO I FIND x
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("OtherFunc", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        info.Documentation!.SeeAlso.ShouldNotBeNull();
        info.Documentation.SeeAlso!.Count.ShouldBe(1);
        info.Documentation.SeeAlso[0].ShouldBe("SumRange");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method extracts a full documentation comment with all tags.
    /// </summary>
    [Fact]
    public void Build_WithAllTags_ExtractsAllDocumentationFields()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              LEGEND: Sums the numbers in a range.
              RECITATIVE: Any additional remarks.
              ARTICLE Start (PEER): The starting number.
              ARTICLE End (PEER): The ending number.
              CONSEQUENCE (PEER): The total sum.
              CURSES SameValues (DECREE): Thrown if Start and End are the same.
              CHORUS:
              SUMMON SumRange WITH 1 AND 10 IF YOU PLEASE.
              ENSEMBLE: AnotherFunc
              STATUTORY: Use NewSumRange instead.
            END OF ASIDE.)
            IT IS MY DUTY TO PERFORM SumRange UNDER THE TERMS OF Start AS A PEER AND End AS A PEER TO FIND PEER
              AND SO I FIND 0
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("SumRange", out SymbolInfo? info);

        info!.Documentation.ShouldNotBeNull();
        DocumentationComment documentation = info.Documentation!;
        documentation.Summary.ShouldBe("Sums the numbers in a range.");
        documentation.Remarks.ShouldBe("Any additional remarks.");
        documentation.Parameters.ShouldNotBeNull();
        documentation.Parameters!.Count.ShouldBe(2);
        documentation.ReturnValue.ShouldNotBeNull();
        documentation.Exceptions.ShouldNotBeNull();
        documentation.Exceptions!.Count.ShouldBe(1);
        documentation.Examples.ShouldNotBeNull();
        documentation.Examples!.Count.ShouldBe(1);
        documentation.SeeAlso.ShouldNotBeNull();
        documentation.SeeAlso!.Count.ShouldBe(1);
        documentation.IsDeprecated.ShouldBeTrue();
        documentation.DeprecationMessage.ShouldBe("Use NewSumRange instead.");
    }

    /// <summary>
    /// Builds a symbol table from a source string.
    /// </summary>
    /// <param name="source">The Topsy Turvy source code to parse and analyse.</param>
    /// <returns>A <see cref="SymbolTable"/> built from the parsed program.</returns>
    private SymbolTable BuildTable(string source)
    {
        ParseResult result = this.parser.TryParse(source);
        return SymbolTable.Build(result.Program!, source);
    }
}
