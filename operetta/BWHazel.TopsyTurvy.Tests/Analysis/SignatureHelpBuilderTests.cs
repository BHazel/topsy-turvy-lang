using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Analysis;

/// <summary>
/// Tests for the <see cref="SignatureHelpBuilder"/> class.
/// </summary>
public class SignatureHelpBuilderTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that the <see cref="SignatureHelpBuilder.Build"/> method returns <c>null</c> when there is no
    /// <c>SUMMON</c> keyword before the cursor.
    /// </summary>
    [Fact]
    public void Build_WithNoSummonOnLine_ReturnsNull()
    {
        SymbolTable table = this.BuildTable("HARK! \"Test\"\nFINALE.\n");

        SignatureHelpResult? result = SignatureHelpBuilder.Build("HARK! \"Test\"\n", 0, 5, table);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpBuilder.Build"/> method returns a candidate signature for an
    /// in-progress <c>SUMMON</c> call.
    /// </summary>
    [Fact]
    public void Build_WithCursorInsideSummonCall_ReturnsSignature()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM add UNDER THE TERMS OF lhs AS A PEER AND rhs AS A PEER TO FIND PEER
              AND SO I FIND SUM OF lhs AND rhs
            MY DUTY IS DISCHARGED.
            FINALE.
            """;
        SymbolTable table = this.BuildTable(source);
        string liveText = "SUMMON add WITH 1";

        SignatureHelpResult? result = SignatureHelpBuilder.Build(liveText, 0, liveText.Length, table);

        result.ShouldNotBeNull();
        result.Signatures.ShouldHaveSingleItem();
        result.Signatures[0].Label.ShouldStartWith("add(");
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpBuilder.Build"/> method includes an array-typed parameter element
    /// type in the signature label.
    /// </summary>
    [Fact]
    public void Build_WithArrayParameter_IncludesParameterElementTypeInLabel()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM firstElement UNDER THE TERMS OF nums AS A LITTLE LIST OF PEER TO FIND PEER
              AND SO I FIND VICTIM 1 ON nums
            MY DUTY IS DISCHARGED.
            FINALE.
            """;
        SymbolTable table = this.BuildTable(source);
        string liveText = "SUMMON firstElement WITH numbers";

        SignatureHelpResult? result = SignatureHelpBuilder.Build(liveText, 0, liveText.Length, table);

        result.ShouldNotBeNull();
        result.Signatures.ShouldHaveSingleItem();
        result.Signatures[0].Label.ShouldBe("firstElement(nums AS A LITTLE LIST OF PEER) TO FIND PEER");
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpBuilder.Build"/> method includes an array-typed return type
    /// element type in the signature label, distinct from the parameter element type, so a swap between
    /// the two would be caught.
    /// </summary>
    [Fact]
    public void Build_WithArrayReturnType_IncludesReturnElementTypeInLabel()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM makeArray UNDER THE TERMS OF seed AS A YARN TO FIND LITTLE LIST OF PEER
              PRAY WELCOME numbers AS A LITTLE LIST OF PEER BEING 1 AND 2 IF YOU PLEASE.
              AND SO I FIND numbers
            MY DUTY IS DISCHARGED.
            FINALE.
            """;
        SymbolTable table = this.BuildTable(source);
        string liveText = "SUMMON makeArray WITH \"x\"";

        SignatureHelpResult? result = SignatureHelpBuilder.Build(liveText, 0, liveText.Length, table);

        result.ShouldNotBeNull();
        result.Signatures.ShouldHaveSingleItem();
        result.Signatures[0].Label.ShouldBe("makeArray(seed AS A YARN) TO FIND LITTLE LIST OF PEER");
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpBuilder.Build"/> method includes each parameter type in an
    /// overload label, so two overloads differing only by parameter type render as distinct signatures.
    /// </summary>
    [Fact]
    public void Build_WithOverloadsDifferingOnlyByParameterType_ReturnsDistinctLabels()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "a whole number"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A YARN TO FIND YARN
              AND SO I FIND "a piece of text"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;
        SymbolTable table = this.BuildTable(source);
        string liveText = "SUMMON describe WITH 1";

        SignatureHelpResult? result = SignatureHelpBuilder.Build(liveText, 0, liveText.Length, table);

        result.ShouldNotBeNull();
        result.Signatures.Count.ShouldBe(2);
        result.Signatures.ShouldContain(signature => signature.Label.Contains("PEER"));
        result.Signatures.ShouldContain(signature => signature.Label.Contains("YARN"));
    }

    /// <summary>
    /// Tests that the <see cref="SignatureHelpBuilder.Build"/> method returns <c>null</c> when the argument list
    /// begins with <c>NOTHING</c>, a zero-argument call.
    /// </summary>
    [Fact]
    public void Build_WithNothingArgument_ReturnsNull()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
            MY DUTY IS DISCHARGED.
            FINALE.
            """;
        SymbolTable table = this.BuildTable(source);
        string liveText = "SUMMON greet WITH NOTHING";

        SignatureHelpResult? result = SignatureHelpBuilder.Build(liveText, 0, liveText.Length, table);

        result.ShouldBeNull();
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
