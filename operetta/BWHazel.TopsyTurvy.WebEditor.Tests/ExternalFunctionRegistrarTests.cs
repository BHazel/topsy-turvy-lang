using System;
using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.WebEditor;

namespace BWHazel.TopsyTurvy.WebEditor.Tests;

/// <summary>
/// Tests for the <see cref="ExternalFunctionRegistrar"/> class.
/// </summary>
public class ExternalFunctionRegistrarTests
{
    private const string Source =
        """
        HARK! "Test"
        FINALE.
        """;

    /// <summary>
    /// Tests that <see cref="ExternalFunctionRegistrar.Register"/> throws when given a non-empty external catalogue,
    /// refusing third-party assembly loading in the browser sandbox.
    /// </summary>
    [Fact]
    public void Register_WithNonEmptyExternalCatalogue_Throws()
    {
        SymbolTable symbolTable = CreateSymbolTable();

        Should.Throw<InvalidOperationException>(() => ExternalFunctionRegistrar.Register(symbolTable, BindingCatalogue.Default));
    }

    /// <summary>
    /// Tests that <see cref="ExternalFunctionRegistrar.Register"/> succeeds when given no explicit catalogue, falling
    /// back to the Standard Library as before.
    /// </summary>
    [Fact]
    public void Register_WithNoExplicitCatalogue_Succeeds()
    {
        SymbolTable symbolTable = CreateSymbolTable();

        Should.NotThrow(() => ExternalFunctionRegistrar.Register(symbolTable));
    }

    /// <summary>
    /// Tests that <see cref="ExternalFunctionRegistrar.Register"/> succeeds when given an explicitly empty catalogue.
    /// </summary>
    [Fact]
    public void Register_WithEmptyCatalogue_Succeeds()
    {
        SymbolTable symbolTable = CreateSymbolTable();

        Should.NotThrow(() => ExternalFunctionRegistrar.Register(symbolTable, BindingCatalogue.Empty));
    }

    /// <summary>
    /// Builds a symbol table from a minimal, valid Topsy Turvy programme for use as test input.
    /// </summary>
    /// <returns>The built symbol table.</returns>
    private static SymbolTable CreateSymbolTable()
    {
        TopsyTurvyParser parser = new();
        ParseResult parseResult = parser.TryParse(Source);
        return SymbolTable.Build(parseResult.Program!, Source);
    }
}
