using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// External function dispatch tests for the <see cref="Interpreter"/> class.
/// </summary>
/// <remarks>
/// These tests resolve <c>SUMMON</c> targets against <see cref="TestExternalFunctionBindingClass"/>, a test-only
/// catalogue, rather than the real Standard Library.
/// </remarks>
public class TopsyTurvyInterpreterExternalFunctionTests : TopsyTurvyInterpreterTestBase
{
    private readonly BindingCatalogue catalogue = BindingCatalogue.Create(typeof(TestExternalFunctionBindingClass));

    /// <summary>
    /// Tests that <c>SUMMON</c> of an external function writes to the output, with no source-level function declaration.
    /// </summary>
    [Fact]
    public void Execute_WithSummonExternalFunction_WritesToOutput()
    {
        string source = """
            HARK! "External Write"
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON TestWrite WITH "Hello" IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter(this.catalogue);

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output.ShouldContain("Hello");
    }

    /// <summary>
    /// Tests that <c>SUMMON</c> of an external function returns the next queued input line, with no source-level
    /// function declaration.
    /// </summary>
    [Fact]
    public void Execute_WithSummonExternalFunction_ReturnsQueuedInput()
    {
        string source = """
            HARK! "External Read"
            PRINCIPALS
              PRAY WELCOME result AS A YARN BEING SUMMON TestRead WITH NOTHING IF YOU PLEASE.
            THE CURTAIN RISES.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter(this.catalogue, "A Most Ingenious Paradox!");

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output.ShouldContain("A Most Ingenious Paradox!");
    }

    /// <summary>
    /// Tests that a user-defined global function shadows an external function of the same name.
    /// </summary>
    [Fact]
    public void Execute_WithUserFunctionShadowingExternalFunction_UserFunctionWins()
    {
        string source = """
            HARK! "Shadowing"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM TestWrite UNDER THE TERMS OF text AS A YARN
              BEHOLD "shadowed"
            MY DUTY IS DISCHARGED.
            SUMMON TestWrite WITH "Hello" IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter(this.catalogue);

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output.ShouldContain("shadowed");
        output.ShouldNotContain("Hello");
    }

    /// <summary>
    /// Tests that <see cref="BindingCatalogue.Empty"/> disables the external function fallback, so a <c>SUMMON</c> of an
    /// otherwise-available external function is reported as undefined.
    /// </summary>
    [Fact]
    public void Execute_WithEmptyCatalogueSummoningExternalFunction_ReturnsErrorDiagnostic()
    {
        string source = """
            HARK! "Empty Catalogue"
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON TestWrite WITH "Hello" IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter(BindingCatalogue.Empty);

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
        output.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a wrong argument count against an external function returns an error diagnostic.
    /// </summary>
    [Fact]
    public void Execute_WithWrongArgumentCountForExternalFunction_ReturnsErrorDiagnostic()
    {
        string source = """
            HARK! "Wrong External Args"
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON TestWrite WITH NOTHING IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter(this.catalogue);

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that an interpreter constructed with no explicit catalogue falls back to <see cref="BindingCatalogue.Default"/>,
    /// so the real Standard Library is reachable with the constructor overload every existing call site already uses.
    /// </summary>
    [Fact]
    public void Execute_WithNoExplicitCatalogue_ResolvesAgainstBindingCatalogueDefault()
    {
        string source = """
            HARK! "Default Catalogue"
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON PreviewBehold WITH "Hello" AND VERITY IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output.ShouldContain("Hello");
    }
}
