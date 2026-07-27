using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.TypeChecker;

namespace BWHazel.TopsyTurvy.Tests.TypeChecker;

/// <summary>
/// Array-typed function parameter and return-value tests for the <see cref="TopsyTurvyTypeChecker"/> class.
/// </summary>
public class TypeCheckerArrayFunctionTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds when an array-typed parameter is indexed inside the function body.
    /// </summary>
    [Fact]
    public void Check_WithArrayParameterIndexedInsideBody_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM firstElement UNDER THE TERMS OF nums AS A LITTLE LIST OF PEER TO FIND PEER
              AND SO I FIND VICTIM 1 ON nums
            MY DUTY IS DISCHARGED.

            PRAY WELCOME numbers AS A LITTLE LIST OF PEER BEING 1 AND 2 AND 3 IF YOU PLEASE.
            PRAY WELCOME result AS A PEER BEING SUMMON firstElement WITH numbers IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds when an array-typed parameter is passed
    /// as the argument to another function array-typed parameter.
    /// </summary>
    [Fact]
    public void Check_WithArrayParameterPassedToAnotherArrayParameter_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM firstElement UNDER THE TERMS OF nums AS A LITTLE LIST OF PEER TO FIND PEER
              AND SO I FIND VICTIM 1 ON nums
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM wrapper UNDER THE TERMS OF values AS A LITTLE LIST OF PEER TO FIND PEER
              AND SO I FIND SUMMON firstElement WITH values IF YOU PLEASE.
            MY DUTY IS DISCHARGED.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds for a function returning an array value.
    /// </summary>
    [Fact]
    public void Check_WithArrayReturnType_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM makeArray UNDER NO OBLIGATION TO FIND LITTLE LIST OF PEER
              PRAY WELCOME numbers AS A LITTLE LIST OF PEER BEING 1 AND 2 AND 3 IF YOU PLEASE.
              AND SO I FIND numbers
            MY DUTY IS DISCHARGED.

            SUMMON makeArray WITH NOTHING IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a parameter is declared
    /// <c>LITTLE LIST OF NAUGHT</c>, the same rejection an ordinary array variable declaration already gets.
    /// </summary>
    [Fact]
    public void Check_WithNaughtElementParameter_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM bad UNDER THE TERMS OF nums AS A LITTLE LIST OF NAUGHT
            MY DUTY IS DISCHARGED.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when the return type is
    /// declared <c>TO FIND LITTLE LIST OF NAUGHT</c>.
    /// </summary>
    [Fact]
    public void Check_WithNaughtElementReturnType_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM bad UNDER NO OBLIGATION TO FIND LITTLE LIST OF NAUGHT
              AND SO I FIND NAUGHT
            MY DUTY IS DISCHARGED.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method still succeeds for an ordinary scalar-typed
    /// function, confirming array-parameter support leaves existing declarations unaffected.
    /// </summary>
    [Fact]
    public void Check_WithScalarParametersAndReturnType_StillSucceeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM add UNDER THE TERMS OF alpha AS A PEER AND beta AS A PEER TO FIND PEER
              AND SO I FIND SUM OF alpha AND beta
            MY DUTY IS DISCHARGED.
            PRAY WELCOME result AS A PEER BEING SUMMON add WITH 1 AND 2 IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Runs the type checker on the given source code and returns the result.
    /// </summary>
    /// <param name="source">The source code to check.</param>
    /// <returns>The result of the type check.</returns>
    private TypeCheckResult Check(string source)
    {
        ProgramNode program = this.parser.Parse(source);
        TopsyTurvyTypeChecker typeChecker = new();
        return typeChecker.Check(program);
    }
}
