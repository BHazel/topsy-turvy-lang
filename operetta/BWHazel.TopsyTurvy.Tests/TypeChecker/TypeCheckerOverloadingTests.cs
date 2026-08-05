using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.TypeChecker;

namespace BWHazel.TopsyTurvy.Tests.TypeChecker;

/// <summary>
/// Function overloading tests for the <see cref="TopsyTurvyTypeChecker"/> class.
/// </summary>
public class TypeCheckerOverloadingTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that two functions sharing a name but differing in arity both resolve correctly by argument count.
    /// </summary>
    [Fact]
    public void Check_WithOverloadsDifferingByArity_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Overload By Arity"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "one argument"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF first AS A PEER AND second AS A PEER TO FIND YARN
              AND SO I FIND "two arguments"
            MY DUTY IS DISCHARGED.

            PRAY WELCOME first AS A YARN BEING SUMMON describe WITH 1 IF YOU PLEASE.
            PRAY WELCOME second AS A YARN BEING SUMMON describe WITH 1 AND 2 IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that two functions sharing a name but differing in a single parameter type both resolve correctly
    /// by argument type.
    /// </summary>
    [Fact]
    public void Check_WithOverloadsDifferingByParameterType_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Overload By Type"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "a whole number"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A YARN TO FIND YARN
              AND SO I FIND "a piece of text"
            MY DUTY IS DISCHARGED.

            PRAY WELCOME first AS A YARN BEING SUMMON describe WITH 42 IF YOU PLEASE.
            PRAY WELCOME second AS A YARN BEING SUMMON describe WITH "Ko-Ko" IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a call matching two overloads with an equal widening distance is reported as ambiguous.
    /// </summary>
    [Fact]
    public void Check_WithAmbiguousCall_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Ambiguous Overload"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM combine UNDER THE TERMS OF first AS A CHANCELLOR AND second AS A PEER TO FIND PEER
              AND SO I FIND 1
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM combine UNDER THE TERMS OF first AS A PEER AND second AS A CHANCELLOR TO FIND PEER
              AND SO I FIND 2
            MY DUTY IS DISCHARGED.

            PRAY WELCOME result AS A PEER BEING SUMMON combine WITH 1 AND 2 IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that a call matching no overload by argument type is reported as an error.
    /// </summary>
    [Fact]
    public void Check_WithNoMatchingOverload_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "No Matching Overload"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "a whole number"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A YARN TO FIND YARN
              AND SO I FIND "a piece of text"
            MY DUTY IS DISCHARGED.

            PRAY WELCOME result AS A YARN BEING SUMMON describe WITH VERITY IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that two functions sharing both a name and an identical parameter type list are reported as a
    /// duplicate declaration, rather than the second silently overwriting the first.
    /// </summary>
    [Fact]
    public void Check_WithDuplicateDeclaration_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Duplicate Declaration"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "first"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF other AS A PEER TO FIND YARN
              AND SO I FIND "second"
            MY DUTY IS DISCHARGED.

            PRAY WELCOME result AS A YARN BEING SUMMON describe WITH 1 IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
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
