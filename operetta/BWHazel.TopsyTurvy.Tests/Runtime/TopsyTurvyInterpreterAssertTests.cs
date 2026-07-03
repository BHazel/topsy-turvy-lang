using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Assert statement tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterAssertTests : TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// Tests that an assert statement with a truthy condition has no effect.
    /// </summary>
    [Fact]
    public void Execute_AssertStatement_TruthyCondition_HasNoEffect()
    {
        string source = """
            HARK! "Assert truthy no effect"
            THE LAW IS VERITY THAT "Should not throw"
            BEHOLD "passed"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("passed");
    }

    /// <summary>
    /// Tests that an assert statement with a falsy condition throws a runtime error.
    /// </summary>
    [Fact]
    public void Execute_AssertStatement_FalsyCondition_ThrowsError()
    {
        string source = """
            HARK! "Assert falsy throws"
            THE LAW IS NAY THAT "Assertion failed"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that an assert statement with a falsy comparison condition throws a runtime error.
    /// </summary>
    [Fact]
    public void Execute_AssertStatement_FalsyComparisonCondition_ThrowsError()
    {
        string source = """
            HARK! "Assert comparison throws"
            PRINCIPALS
              PRAY WELCOME score AS A PEER BEING -1
            THE CURTAIN RISES.
            THE LAW IS PRE-ADAMITE score AND 0 THAT "Score must be positive"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that an assert exception can be caught by a try-catch block.
    /// </summary>
    [Fact]
    public void Execute_AssertStatement_ExceptionCaughtByTryCatch()
    {
        string source = """
            HARK! "Assert caught by try-catch"
            PRINCIPALS
              PRAY WELCOME result AS A YARN
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM fail_assert UNDER NO OBLIGATION
              THE LAW IS NAY THAT "Assert fired"
            MY DUTY IS DISCHARGED.
            WITH THE GREATEST RESPECT, SUMMON fail_assert WITH NOTHING IF YOU PLEASE.
              WITH GRATITUDE
                result IS APPOINTED "no error"
              MODIFIED RAPTURE, ErrMsg
                result IS APPOINTED ErrMsg
            THAT CONCLUDES THE MATTER.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("Assert fired");
    }
}
