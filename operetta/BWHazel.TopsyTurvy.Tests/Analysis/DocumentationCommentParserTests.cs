using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Ast;
using Shouldly;

namespace BWHazel.TopsyTurvy.Tests.Analysis;

/// <summary>
/// Tests for the <see cref="DocumentationCommentParser"/> class.
/// </summary>
public class DocumentationCommentParserTests
{
    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> returns <c>null</c> when the block contains no recognised tags.
    /// </summary>
    [Fact]
    public void Parse_WithNoRecognisedTags_ReturnsNull()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse("  This is just a plain comment.\n");

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> returns <c>null</c> for an empty block.
    /// </summary>
    [Fact]
    public void Parse_WithEmptyContent_ReturnsNull()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(string.Empty);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts the summary from a LEGEND tag.
    /// </summary>
    [Fact]
    public void Parse_WithLegendTag_ExtractsSummary()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse("  LEGEND: Holds the numbers.\n");

        result.ShouldNotBeNull();
        result.Summary.ShouldBe("Holds the numbers.");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts multi-line content from a LEGEND tag.
    /// </summary>
    [Fact]
    public void Parse_WithMultilineLegendTag_ExtractsFullSummary()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  LEGEND: First line.\n" +
            "  Second line.\n");

        result.ShouldNotBeNull();
        result.Summary.ShouldBe("First line.\nSecond line.");
    }

    /// <summary>
    /// Tests that when LEGEND appears more than once, the last occurrence is used.
    /// </summary>
    [Fact]
    public void Parse_WithLegendTagTwice_LastOneWins()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  LEGEND: First summary.\n" +
            "  LEGEND: Second summary.\n");

        result.ShouldNotBeNull();
        result.Summary.ShouldBe("Second summary.");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts remarks from a RECITATIVE tag.
    /// </summary>
    [Fact]
    public void Parse_WithRecitativeTag_ExtractsRemarks()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse("  RECITATIVE: Some additional remarks.\n");

        result.ShouldNotBeNull();
        result.Remarks.ShouldBe("Some additional remarks.");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts multi-line content from a RECITATIVE tag.
    /// </summary>
    [Fact]
    public void Parse_WithMultilineRecitativeTag_ExtractsFullRemarks()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  RECITATIVE: First line of remarks.\n" +
            "  Second line of remarks.\n");

        result.ShouldNotBeNull();
        result.Remarks.ShouldBe("First line of remarks.\nSecond line of remarks.");
    }

    /// <summary>
    /// Tests that when RECITATIVE appears more than once, the last occurrence is used.
    /// </summary>
    [Fact]
    public void Parse_WithRecitativeTagTwice_LastOneWins()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  RECITATIVE: First remarks.\n" +
            "  RECITATIVE: Second remarks.\n");

        result.ShouldNotBeNull();
        result.Remarks.ShouldBe("Second remarks.");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts a single parameter from an ARTICLE tag.
    /// </summary>
    [Fact]
    public void Parse_WithArticleTag_ExtractsParameter()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse("  ARTICLE Start (PEER): The starting number.\n");

        result.ShouldNotBeNull();
        result.Parameters.ShouldNotBeNull();
        result.Parameters.ShouldContainKey("Start");
        result.Parameters["Start"].Type.ShouldBe("PEER");
        result.Parameters["Start"].Description.ShouldBe("The starting number.");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts all parameters when multiple ARTICLE tags are present.
    /// </summary>
    [Fact]
    public void Parse_WithMultipleArticleTags_ExtractsAllParameters()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  ARTICLE Start (PEER): The starting number.\n" +
            "  ARTICLE End (PEER): The ending number.\n");

        result.ShouldNotBeNull();
        result.Parameters.ShouldNotBeNull();
        result.Parameters.Count.ShouldBe(2);
        result.Parameters.ShouldContainKey("Start");
        result.Parameters.ShouldContainKey("End");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts the return value from a CONSEQUENCE tag.
    /// </summary>
    [Fact]
    public void Parse_WithConsequenceTag_ExtractsReturnValue()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse("  CONSEQUENCE (PEER): The total sum.\n");

        result.ShouldNotBeNull();
        result.ReturnValue.ShouldNotBeNull();
        result.ReturnValue.Value.Type.ShouldBe("PEER");
        result.ReturnValue.Value.Description.ShouldBe("The total sum.");
    }

    /// <summary>
    /// Tests that when CONSEQUENCE appears more than once, the last occurrence is used.
    /// </summary>
    [Fact]
    public void Parse_WithConsequenceTagTwice_LastOneWins()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  CONSEQUENCE (PEER): First description.\n" +
            "  CONSEQUENCE (YARN): Second description.\n");

        result.ShouldNotBeNull();
        result.ReturnValue.ShouldNotBeNull();
        result.ReturnValue.Value.Type.ShouldBe("YARN");
        result.ReturnValue.Value.Description.ShouldBe("Second description.");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts a thrown value from a CURSES tag.
    /// </summary>
    [Fact]
    public void Parse_WithCursesTag_ExtractsException()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse("  CURSES SameValues (DECREE): Thrown if Start and End are equal.\n");

        result.ShouldNotBeNull();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count.ShouldBe(1);
        result.Exceptions[0].Name.ShouldBe("SameValues");
        result.Exceptions[0].Type.ShouldBe("DECREE");
        result.Exceptions[0].Description.ShouldBe("Thrown if Start and End are equal.");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts all thrown values when multiple CURSES tags are present.
    /// </summary>
    [Fact]
    public void Parse_WithMultipleCursesTags_ExtractsAllExceptions()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  CURSES TooLow (PEER): Value is below minimum.\n" +
            "  CURSES TooHigh (PEER): Value is above maximum.\n");

        result.ShouldNotBeNull();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count.ShouldBe(2);
        result.Exceptions[0].Name.ShouldBe("TooLow");
        result.Exceptions[1].Name.ShouldBe("TooHigh");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts a code example from a CHORUS tag.
    /// </summary>
    [Fact]
    public void Parse_WithChorusTag_ExtractsExample()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  CHORUS:\n" +
            "  SUMMON SumRange WITH 1 AND 10 IF YOU PLEASE.\n");

        result.ShouldNotBeNull();
        result.Examples.ShouldNotBeNull();
        result.Examples.Count.ShouldBe(1);
        result.Examples[0].ShouldContain("SUMMON SumRange");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts all examples when multiple CHORUS tags are present.
    /// </summary>
    [Fact]
    public void Parse_WithMultipleChorusTags_ExtractsAllExamples()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  CHORUS:\n" +
            "  SUMMON SumRange WITH 1 AND 10 IF YOU PLEASE.\n" +
            "  CHORUS:\n" +
            "  SUMMON SumRange WITH 5 AND 20 IF YOU PLEASE.\n");

        result.ShouldNotBeNull();
        result.Examples.ShouldNotBeNull();
        result.Examples.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts a see-also reference from an ENSEMBLE tag.
    /// </summary>
    [Fact]
    public void Parse_WithEnsembleTag_ExtractsSeeAlso()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse("  ENSEMBLE: Numbers\n");

        result.ShouldNotBeNull();
        result.SeeAlso.ShouldNotBeNull();
        result.SeeAlso.Count.ShouldBe(1);
        result.SeeAlso[0].ShouldBe("Numbers");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> extracts all see-also references when multiple ENSEMBLE tags are present.
    /// </summary>
    [Fact]
    public void Parse_WithMultipleEnsembleTags_ExtractsAllSeeAlsoItems()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  ENSEMBLE: Numbers\n" +
            "  ENSEMBLE: SumRange\n");

        result.ShouldNotBeNull();
        result.SeeAlso.ShouldNotBeNull();
        result.SeeAlso.Count.ShouldBe(2);
        result.SeeAlso.ShouldContain("Numbers");
        result.SeeAlso.ShouldContain("SumRange");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> sets <see cref="DocumentationComment.IsDeprecated"/> and the deprecation message from a STATUTORY tag.
    /// </summary>
    [Fact]
    public void Parse_WithStatutoryTagAndMessage_SetsIsDeprecatedAndMessage()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse("  STATUTORY: Use SumRange instead.\n");

        result.ShouldNotBeNull();
        result.IsDeprecated.ShouldBeTrue();
        result.DeprecationMessage.ShouldBe("Use SumRange instead.");
    }

    /// <summary>
    /// Tests that <see cref="DocumentationCommentParser.Parse"/> sets <see cref="DocumentationComment.IsDeprecated"/> with no message when STATUTORY has no text.
    /// </summary>
    [Fact]
    public void Parse_WithStatutoryTagAndNoMessage_SetsIsDeprecatedOnly()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse("  STATUTORY:\n");

        result.ShouldNotBeNull();
        result.IsDeprecated.ShouldBeTrue();
        result.DeprecationMessage.ShouldBeNull();
    }

    /// <summary>
    /// Tests that when STATUTORY appears more than once, the last message wins.
    /// </summary>
    [Fact]
    public void Parse_WithStatutoryTagTwice_LastMessageWins()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  STATUTORY: First message.\n" +
            "  STATUTORY: Second message.\n");

        result.ShouldNotBeNull();
        result.IsDeprecated.ShouldBeTrue();
        result.DeprecationMessage.ShouldBe("Second message.");
    }

    /// <summary>
    /// Tests that tags are matched case-insensitively.
    /// </summary>
    [Fact]
    public void Parse_WithLowercaseTags_ParsesSuccessfully()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  legend: A summary.\n" +
            "  recitative: Some remarks.\n");

        result.ShouldNotBeNull();
        result.Summary.ShouldBe("A summary.");
        result.Remarks.ShouldBe("Some remarks.");
    }

    /// <summary>
    /// Tests that all recognised tags are extracted correctly when all appear in one block.
    /// </summary>
    [Fact]
    public void Parse_WithAllTags_PopulatesAllFields()
    {
        DocumentationComment? result = DocumentationCommentParser.Parse(
            "  LEGEND: Sums a range.\n" +
            "  RECITATIVE: Additional remarks.\n" +
            "  ARTICLE Start (PEER): The starting number.\n" +
            "  ARTICLE End (PEER): The ending number.\n" +
            "  CONSEQUENCE (PEER): The total sum.\n" +
            "  CURSES SameValues (DECREE): Thrown if Start and End are equal.\n" +
            "  CHORUS:\n" +
            "  SUMMON SumRange WITH 1 AND 10 IF YOU PLEASE.\n" +
            "  ENSEMBLE: Numbers\n" +
            "  STATUTORY: Use ProductRange instead.\n");

        result.ShouldNotBeNull();
        result.Summary.ShouldBe("Sums a range.");
        result.Remarks.ShouldBe("Additional remarks.");
        result.Parameters.ShouldNotBeNull();
        result.Parameters.Count.ShouldBe(2);
        result.ReturnValue.ShouldNotBeNull();
        result.ReturnValue.Value.Type.ShouldBe("PEER");
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions.Count.ShouldBe(1);
        result.Examples.ShouldNotBeNull();
        result.Examples.Count.ShouldBe(1);
        result.SeeAlso.ShouldNotBeNull();
        result.SeeAlso.Count.ShouldBe(1);
        result.IsDeprecated.ShouldBeTrue();
        result.DeprecationMessage.ShouldBe("Use ProductRange instead.");
    }
}
