using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.Tests.Runtime;
using BWHazel.TopsyTurvy.TypeChecker;

namespace BWHazel.TopsyTurvy.Tests.TypeChecker;

/// <summary>
/// External function seeding tests for the <see cref="TopsyTurvyTypeChecker"/> class.
/// </summary>
/// <remarks>
/// These tests resolve <c>SUMMON</c> targets against <see cref="TestExternalFunctionBindingClass"/>, a test-only
/// catalogue shared with <c>Tests/Runtime</c>, rather than the real Standard Library.
/// </remarks>
public class TypeCheckerExternalFunctionTests
{
    private readonly TopsyTurvyParser parser = new();
    private readonly BindingCatalogue catalogue = BindingCatalogue.Create(typeof(TestExternalFunctionBindingClass));

    /// <summary>
    /// Tests that a call to an external function type-checks successfully, with no source-level function declaration.
    /// </summary>
    [Fact]
    public void Check_WithExternalFunctionCall_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "External Call"
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON TestWrite WITH "Hello" IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a wrong argument count against an external function reports a type error.
    /// </summary>
    [Fact]
    public void Check_WithWrongArgumentCountForExternalFunction_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Wrong External Args"
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON TestWrite WITH NOTHING IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that an argument type mismatch against an external function reports a type error.
    /// </summary>
    [Fact]
    public void Check_WithArgumentTypeMismatchForExternalFunction_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "External Type Mismatch"
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON TestWrite WITH 5 IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that a void external function used as a declaration initial value type-checks the same way a void
    /// Topsy Turvy function does: silently, with no diagnostic.
    /// </summary>
    [Fact]
    public void Check_WithVoidExternalFunctionAsDeclarationValue_SucceedsSilently()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Void As Value"
            PRINCIPALS
              PRAY WELCOME result AS A YARN BEING SUMMON TestWrite WITH "Hello" IF YOU PLEASE.
            THE CURTAIN RISES.
            BEHOLD result
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a user-defined global function shadows an external function of the same name, checking the exact
    /// source the interpreter-level shadowing test in <c>Tests/Runtime</c> also checks, so the two layers agree.
    /// </summary>
    [Fact]
    public void Check_WithUserFunctionShadowingExternalFunction_Succeeds()
    {
        TypeCheckResult result = this.Check(ExternalFunctionTestSources.Shadowing);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that <see cref="BindingCatalogue.Empty"/> disables the external function fallback, so a call to an
    /// otherwise-available external function is reported as undefined.
    /// </summary>
    [Fact]
    public void Check_WithEmptyCatalogueSummoningExternalFunction_ReportsError()
    {
        TypeCheckResult result = this.Check(
            """
            HARK! "Empty Catalogue"
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON TestWrite WITH "Hello" IF YOU PLEASE.
            FINALE.
            """,
            BindingCatalogue.Empty);

        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Runs the type checker on the given source against the test catalogue and returns the result.
    /// </summary>
    /// <param name="source">The source code to check.</param>
    /// <returns>The result of the type check.</returns>
    private TypeCheckResult Check(string source) => this.Check(source, this.catalogue);

    /// <summary>
    /// Runs the type checker on the given source against a given catalogue and returns the result.
    /// </summary>
    /// <param name="source">The source code to check.</param>
    /// <param name="externalFunctions">The catalogue of external functions to check against.</param>
    /// <returns>The result of the type check.</returns>
    private TypeCheckResult Check(string source, BindingCatalogue externalFunctions)
    {
        ProgramNode program = this.parser.Parse(source);
        TopsyTurvyTypeChecker typeChecker = new();
        return typeChecker.Check(program, externalFunctions: externalFunctions);
    }
}
