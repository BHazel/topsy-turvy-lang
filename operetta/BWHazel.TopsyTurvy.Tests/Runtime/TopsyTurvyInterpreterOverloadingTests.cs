using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Runtime;
using BWHazel.TopsyTurvy.Tests.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Function overloading tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterOverloadingTests : TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// Tests that two Topsy Turvy functions sharing a name but differing in arity each execute their own body.
    /// </summary>
    [Fact]
    public void Execute_WithOverloadsDifferingByArity_CallsCorrectOverload()
    {
        string source = """
            HARK! "Overload By Arity"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "one argument"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF first AS A PEER AND second AS A PEER TO FIND YARN
              AND SO I FIND "two arguments"
            MY DUTY IS DISCHARGED.

            BEHOLD SUMMON describe WITH 1 IF YOU PLEASE.
            BEHOLD SUMMON describe WITH 1 AND 2 IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("one argument");
        output[1].ShouldBe("two arguments");
    }

    /// <summary>
    /// Tests that two Topsy Turvy functions sharing a name but differing in a single parameter type each
    /// execute their own body.
    /// </summary>
    [Fact]
    public void Execute_WithOverloadsDifferingByParameterType_CallsCorrectOverload()
    {
        string source = """
            HARK! "Overload By Type"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "a whole number"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A YARN TO FIND YARN
              AND SO I FIND "a piece of text"
            MY DUTY IS DISCHARGED.

            BEHOLD SUMMON describe WITH 42 IF YOU PLEASE.
            BEHOLD SUMMON describe WITH "Ko-Ko" IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("a whole number");
        output[1].ShouldBe("a piece of text");
    }

    /// <summary>
    /// Tests that a call matching two overloads with an equal widening distance reports an error diagnostic
    /// rather than silently picking one.
    /// </summary>
    [Fact]
    public void Execute_WithAmbiguousCall_ReturnsErrorDiagnostic()
    {
        string source = """
            HARK! "Ambiguous Overload"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM combine UNDER THE TERMS OF first AS A CHANCELLOR AND second AS A PEER TO FIND PEER
              AND SO I FIND 1
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM combine UNDER THE TERMS OF first AS A PEER AND second AS A CHANCELLOR TO FIND PEER
              AND SO I FIND 2
            MY DUTY IS DISCHARGED.

            BEHOLD SUMMON combine WITH 1 AND 2 IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, _) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a call matching no overload by argument type reports an error diagnostic.
    /// </summary>
    [Fact]
    public void Execute_WithNoMatchingOverload_ReturnsErrorDiagnostic()
    {
        string source = """
            HARK! "No Matching Overload"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "a whole number"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A YARN TO FIND YARN
              AND SO I FIND "a piece of text"
            MY DUTY IS DISCHARGED.

            BEHOLD SUMMON describe WITH VERITY IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, _) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that two catalogue-bound external functions sharing a name but differing in parameter type each
    /// resolve and invoke correctly, mirroring the same overload resolution as a Topsy Turvy function.
    /// </summary>
    [Fact]
    public void Execute_WithExternalFunctionOverloads_CallsCorrectOverload()
    {
        string source = """
            HARK! "External Overload"
            PRINCIPALS
            THE CURTAIN RISES.
            BEHOLD SUMMON Describe WITH 5 IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        BindingCatalogue catalogue = BindingCatalogue.Create(typeof(TestOverloadBindingClass));
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter(catalogue);

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("5");
    }
}
