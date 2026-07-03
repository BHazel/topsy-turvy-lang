using System;
using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Analysis;

/// <summary>
/// Tests for the <see cref="SymbolTable"/> class.
/// </summary>
public class SymbolTableTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method collects a variable declared with PRAY WELCOME.
    /// </summary>
    [Fact]
    public void Build_WithVariableDeclaration_CollectsVariableByName()
    {
        SymbolTable table = this.BuildTable("HARK! \"Test\"\nPRAY WELCOME myVar AS A PEER BEING 0\nFINALE.");

        table.TryGetSymbol("myVar", out _).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets the correct kind for a collected variable.
    /// </summary>
    [Fact]
    public void Build_WithVariableDeclaration_SetsVariableKind()
    {
        SymbolTable table = this.BuildTable("HARK! \"Test\"\nPRAY WELCOME myVar AS A PEER BEING 0\nFINALE.");
        table.TryGetSymbol("myVar", out SymbolInfo? info);

        info!.Kind.ShouldBe(SymbolKind.Variable);
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets the correct type display name for variable declarations.
    /// </summary>
    [Theory]
    [InlineData("PEER", "PEER")]
    [InlineData("FATHOM", "FATHOM")]
    [InlineData("YARN", "YARN")]
    [InlineData("DECREE", "DECREE")]
    [InlineData("NAUGHT", "NAUGHT")]
    public void Build_WithVariableDeclaration_SetsTypeDisplayName(string typeKeyword, string expectedDisplay)
    {
        string source = $"HARK! \"Test\"\nPRAY WELCOME x AS A {typeKeyword}\nFINALE.";

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("x", out SymbolInfo? info);

        info!.TypeDisplayName.ShouldBe(expectedDisplay);
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets a positive definition line and column for a variable declaration.
    /// </summary>
    [Fact]
    public void Build_WithVariableDeclaration_SetsPositiveDefinitionLine()
    {
        SymbolTable table = this.BuildTable("HARK! \"Test\"\nPRAY WELCOME myVar AS A PEER BEING 0\nFINALE.");
        table.TryGetSymbol("myVar", out SymbolInfo? info);

        (info!.DefinitionLine > 0).ShouldBeTrue();
        (info.DefinitionColumn > 0).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets the correct 1-indexed line and column for a variable declaration.
    /// </summary>
    [Fact]
    public void Build_WithVariableDeclaration_SetsCorrectDefinitionLineAndColumn()
    {
        SymbolTable table = this.BuildTable("HARK! \"Test\"\nPRAY WELCOME myVar AS A PEER BEING 0\nFINALE.");
        table.TryGetSymbol("myVar", out SymbolInfo? info);

        info!.DefinitionLine.ShouldBe(2);
        info.DefinitionColumn.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method collects all variables declared in a PRINCIPALS block.
    /// </summary>
    [Fact]
    public void Build_WithPrincipalsBlock_CollectsAllVariables()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
              PRAY WELCOME alpha AS A PEER BEING 1
              PRAY WELCOME beta AS A YARN BEING "hi"
            THE CURTAIN RISES.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);

        table.TryGetSymbol("alpha", out _).ShouldBeTrue();
        table.TryGetSymbol("beta", out _).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method collects a function definition by name.
    /// </summary>
    [Fact]
    public void Build_WithFunctionDefinition_CollectsFunctionByName()
    {
        string source = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);

        table.TryGetSymbol("greet", out _).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets the correct kind for a collected function.
    /// </summary>
    [Fact]
    public void Build_WithFunctionDefinition_SetsFunctionKind()
    {
        string source = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("greet", out SymbolInfo? info);

        info!.Kind.ShouldBe(SymbolKind.Function);
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method stores the typed parameters on a function <see cref="SymbolInfo"/>.
    /// </summary>
    [Fact]
    public void Build_WithFunctionDefinition_SetsTypedParameters()
    {
        string source = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF salutation AS A YARN AND recipient AS A YARN
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("greet", out SymbolInfo? info);

        info!.TypedParameters.ShouldNotBeNull();
        info.TypedParameters!.Count.ShouldBe(2);
        info.TypedParameters.ShouldContain(static parameter => parameter.Name == "salutation");
        info.TypedParameters.ShouldContain(static parameter => parameter.Name == "recipient");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets the correct 1-indexed line and column for a function definition.
    /// </summary>
    [Fact]
    public void Build_WithFunctionDefinition_SetsCorrectDefinitionLineAndColumn()
    {
        string source = "HARK! \"Test\"\nIT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION\nMY DUTY IS DISCHARGED.\nFINALE.";

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("greet", out SymbolInfo? info);

        info!.DefinitionLine.ShouldBe(2);
        info.DefinitionColumn.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method collects function parameters as <see cref="SymbolKind.Parameter"/> symbols.
    /// </summary>
    [Fact]
    public void Build_WithFunctionParameters_CollectsParameterSymbols()
    {
        string source = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF salutation AS A YARN AND recipient AS A YARN
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);

        table.TryGetSymbol("salutation", out _).ShouldBeTrue();
        table.TryGetSymbol("recipient", out _).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets the correct kind for function parameters.
    /// </summary>
    [Fact]
    public void Build_WithFunctionParameters_SetsParameterKind()
    {
        string source = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("name", out SymbolInfo? info);

        info!.Kind.ShouldBe(SymbolKind.Parameter);
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method collects a variable declared inside a conditional block.
    /// </summary>
    [Fact]
    public void Build_WithVariableInsideConditional_CollectsVariable()
    {
        string source = """
            HARK! "Test"
            SHOULD IT TRANSPIRE THAT VERITY
            QUITE SO.
              PRAY WELCOME inner AS A PEER BEING 1
            SO MUCH FOR THAT.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);

        table.TryGetSymbol("inner", out _).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method collects a variable declared inside the else branch of a conditional.
    /// </summary>
    [Fact]
    public void Build_WithVariableInsideElseBranch_CollectsVariable()
    {
        string source = """
            HARK! "Test"
            SHOULD IT TRANSPIRE THAT NAY
            QUITE SO.
              BEHOLD "true"
            OTHERWISE,
              PRAY WELCOME elseVar AS A PEER BEING 99
            SO MUCH FOR THAT.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);

        table.TryGetSymbol("elseVar", out _).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method collects a variable declared inside a loop body.
    /// </summary>
    [Fact]
    public void Build_WithVariableInsideLoop_CollectsVariable()
    {
        string source = """
            HARK! "Test"
            BY A LEGAL FICTION
              PRAY WELCOME counter AS A PEER BEING 0
            THAT WILL DO.
            THE TERM EXPIRES.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);

        table.TryGetSymbol("counter", out _).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.TryGetSymbol"/> method returns <c>true</c> for an existing symbol and populates the output for a known symbol.
    /// </summary>
    [Fact]
    public void TryGetSymbol_WithExistingSymbol_ReturnsTrueAndPopulatesInfo()
    {
        SymbolTable table = this.BuildTable("HARK! \"Test\"\nPRAY WELCOME x AS A PEER BEING 0\nFINALE.");

        bool found = table.TryGetSymbol("x", out SymbolInfo? info);

        found.ShouldBeTrue();
        info.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.TryGetSymbol"/> method returns <c>false</c> for an unknown symbol.
    /// </summary>
    [Fact]
    public void TryGetSymbol_WithUnknownSymbol_ReturnsFalse()
    {
        SymbolTable table = this.BuildTable("HARK! \"Test\" FINALE.");

        bool found = table.TryGetSymbol("doesNotExist", out _);

        found.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.TryGetSymbol"/> method is case-insensitive when looking up symbol names.
    /// </summary>
    /// <param name="symbolName">The symbol name.</param>
    [Theory]
    [InlineData("myvar")]
    [InlineData("MYVAR")]
    [InlineData("MyVar")]
    public void TryGetSymbol_WithSameSymbolInDifferentCasings_ReturnsTrueForAllCasings(string symbolName)
    {
        SymbolTable table = this.BuildTable("HARK! \"Test\"\nPRAY WELCOME myVar AS A PEER BEING 0\nFINALE.");

        table.TryGetSymbol(symbolName, out _).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.AllSymbols"/> method returns all declared symbols.
    /// </summary>
    [Fact]
    public void AllSymbols_WithDeclaredSymbols_ContainsAllDeclaredSymbols()
    {
        string source = """
            HARK! "Test"
            PRAY WELCOME alpha AS A PEER BEING 1
            IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);
        List<string> names = [.. table.AllSymbols().Select(symbol => symbol.Name)];

        names.ShouldContain("alpha");
        names.ShouldContain("greet");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.ExtractWordAt"/> method returns the word when the cursor is at the start of an identifier.
    /// </summary>
    [Fact]
    public void ExtractWordAt_WithCursorOnWordStart_ReturnsWord()
    {
        string? word = SymbolTable.ExtractWordAt("greet world", 0, 0);

        word.ShouldBe("greet");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.ExtractWordAt"/> method returns the word when the cursor is in the middle of an identifier.
    /// </summary>
    [Fact]
    public void ExtractWordAt_WithCursorOnWordMiddle_ReturnsWord()
    {
        string? word = SymbolTable.ExtractWordAt("greet world", 0, 3);

        word.ShouldBe("greet");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.ExtractWordAt"/> method returns the full hyphenated identifier when the cursor is on a hyphen.
    /// </summary>
    [Fact]
    public void ExtractWordAt_WithCursorOnHyphen_ReturnsFullHyphenatedWord()
    {
        string? word = SymbolTable.ExtractWordAt("Ko-Ko sings", 0, 2);

        word.ShouldBe("Ko-Ko");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.ExtractWordAt"/> method returns <c>null</c> when the cursor is on whitespace.
    /// </summary>
    [Fact]
    public void ExtractWordAt_WithCursorOnWhitespace_ReturnsNull()
    {
        string? word = SymbolTable.ExtractWordAt("greet world", 0, 5);

        word.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.ExtractWordAt"/> method returns <c>null</c> when the token at the cursor starts with a digit.
    /// </summary>
    [Fact]
    public void ExtractWordAt_WithTokenStartingWithDigit_ReturnsNull()
    {
        string? word = SymbolTable.ExtractWordAt("42 greet", 0, 0);

        word.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.ExtractWordAt"/> method returns <c>null</c> for a negative line number.
    /// </summary>
    [Fact]
    public void ExtractWordAt_WithNegativeLineNumber_ReturnsNull()
    {
        string? word = SymbolTable.ExtractWordAt("greet", -1, 0);

        word.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.ExtractWordAt"/> method returns <c>null</c> when the line index exceeds the number of lines in the source.
    /// </summary>
    [Fact]
    public void ExtractWordAt_WithLineOutOfBounds_ReturnsNull()
    {
        string? word = SymbolTable.ExtractWordAt("greet", 99, 0);

        word.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.ExtractWordAt"/> method returns <c>null</c> when the column index exceeds the length of the line.
    /// </summary>
    [Fact]
    public void ExtractWordAt_WithColumnOutOfBounds_ReturnsNull()
    {
        string? word = SymbolTable.ExtractWordAt("greet", 0, 99);

        word.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method collects an array variable declared with PRAY WELCOME.
    /// </summary>
    [Fact]
    public void Build_WithArrayDeclaration_CollectsArrayVariableByName()
    {
        SymbolTable table = this.BuildTable("""
            HARK! "Test"
            PRAY WELCOME miscreants AS A LITTLE LIST OF YARN BEING "a" AND "b" IF YOU PLEASE.
            FINALE.
            """);

        table.TryGetSymbol("miscreants", out _).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets the correct type display name for an array declaration.
    /// </summary>
    [Fact]
    public void Build_WithArrayDeclaration_SetsTypeDisplayNameWithElementType()
    {
        SymbolTable table = this.BuildTable("""
            HARK! "Test"
            PRAY WELCOME miscreants AS A LITTLE LIST OF YARN BEING "a" IF YOU PLEASE.
            FINALE.
            """);
        table.TryGetSymbol("miscreants", out SymbolInfo? info);

        info!.TypeDisplayName.ShouldBe("LITTLE LIST OF YARN");
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method sets <see cref="SymbolInfo.IsConstant"/> to <c>true</c> for a CONSERVATIVE array.
    /// </summary>
    [Fact]
    public void Build_WithConservativeArrayDeclaration_SetsIsConstantTrue()
    {
        SymbolTable table = this.BuildTable("""
            HARK! "Test"
            PRAY WELCOME fixed AS A CONSERVATIVE LITTLE LIST OF PEER BEING 1 IF YOU PLEASE.
            FINALE.
            """);
        table.TryGetSymbol("fixed", out SymbolInfo? info);

        info!.IsConstant.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="SymbolTable.Build"/> method collects array declarations declared inside a PRINCIPALS block.
    /// </summary>
    [Fact]
    public void Build_WithArrayDeclarationInPrincipalsBlock_CollectsArrayVariable()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
              PRAY WELCOME names AS A LITTLE LIST OF YARN BEING "a" AND "b" IF YOU PLEASE.
              PRAY WELCOME count AS A PEER BEING 0
            THE CURTAIN RISES.
            FINALE.
            """;

        SymbolTable table = this.BuildTable(source);

        table.TryGetSymbol("names", out _).ShouldBeTrue();
        table.TryGetSymbol("count", out _).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that function parameter symbols carry a <see cref="SymbolInfo.DefinitionColumn"/> that is strictly greater than
    /// the function own definition column, confirming parameters are positioned at their own token locations.
    /// </summary>
    [Fact]
    public void Build_WithFunctionParameters_ParameterColumnsAreGreaterThanFunctionColumn()
    {
        // Function declaration starts at column 1; parameters alpha and beta appear later on the same line.
        string source = "HARK! \"Test\"\nIT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF alpha AS A PEER AND beta AS A PEER\nMY DUTY IS DISCHARGED.\nFINALE.";

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("greet", out SymbolInfo? greetInfo);
        table.TryGetSymbol("alpha", out SymbolInfo? alphaInfo);
        table.TryGetSymbol("beta", out SymbolInfo? betaInfo);

        greetInfo!.DefinitionColumn.ShouldBe(1);
        (alphaInfo!.DefinitionColumn > greetInfo.DefinitionColumn).ShouldBeTrue();
        (betaInfo!.DefinitionColumn > alphaInfo.DefinitionColumn).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that function parameter symbols carry precise 1-indexed definition columns matching each parameter token position.
    /// </summary>
    [Fact]
    public void Build_WithFunctionParameters_SetsCorrectDefinitionColumnsForEachParameter()
    {
        // In "IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF alpha AS A PEER AND beta AS A PEER":
        // alpha starts at column 51, beta starts at column 71 (1-indexed).
        string source = "HARK! \"Test\"\nIT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF alpha AS A PEER AND beta AS A PEER\nMY DUTY IS DISCHARGED.\nFINALE.";

        SymbolTable table = this.BuildTable(source);
        table.TryGetSymbol("alpha", out SymbolInfo? alphaInfo);
        table.TryGetSymbol("beta", out SymbolInfo? betaInfo);

        alphaInfo!.DefinitionLine.ShouldBe(2);
        alphaInfo.DefinitionColumn.ShouldBe(51);
        betaInfo!.DefinitionLine.ShouldBe(2);
        betaInfo.DefinitionColumn.ShouldBe(71);
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
