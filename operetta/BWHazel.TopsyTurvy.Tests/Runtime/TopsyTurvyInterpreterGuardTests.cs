using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Guard clause tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterGuardTests : TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// Tests that a guard clause with a truthy condition does not execute the else block.
    /// </summary>
    [Fact]
    public void Execute_GuardClause_TruthyCondition_FallsThrough()
    {
        string source = """
            HARK! "Guard true falls through"
            PRINCIPALS
              PRAY WELCOME result AS A YARN BEING "initial"
            THE CURTAIN RISES.
            YEOMAN VERITY
              OTHERWISE,
                result IS APPOINTED "changed"
            UNDER ORDERS.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("initial");
    }

    /// <summary>
    /// Tests that a guard clause with a falsy condition executes the else block.
    /// </summary>
    [Fact]
    public void Execute_GuardClause_FalsyCondition_ExecutesElseBlock()
    {
        string source = """
            HARK! "Guard false executes body"
            PRINCIPALS
              PRAY WELCOME result AS A YARN BEING "initial"
            THE CURTAIN RISES.
            YEOMAN NAY
              OTHERWISE,
                result IS APPOINTED "changed"
            UNDER ORDERS.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("changed");
    }

    /// <summary>
    /// Tests that a guard clause with a comparison condition evaluates the condition correctly.
    /// </summary>
    [Fact]
    public void Execute_GuardClause_WithComparisonCondition_EvaluatesCorrectly()
    {
        string source = """
            HARK! "Guard with comparison"
            PRINCIPALS
              PRAY WELCOME score AS A PEER BEING 5
              PRAY WELCOME result AS A YARN BEING "ok"
            THE CURTAIN RISES.
            YEOMAN PRE-ADAMITE score AND 0
              OTHERWISE,
                result IS APPOINTED "negative"
            UNDER ORDERS.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("ok");
    }

    /// <summary>
    /// Tests that a guard clause with a falsy comparison condition executes the else block.
    /// </summary>
    [Fact]
    public void Execute_GuardClause_FalsyComparisonCondition_ExecutesElseBlock()
    {
        string source = """
            HARK! "Guard with falsy comparison"
            PRINCIPALS
              PRAY WELCOME score AS A PEER BEING -5
              PRAY WELCOME result AS A YARN BEING "ok"
            THE CURTAIN RISES.
            YEOMAN PRE-ADAMITE score AND 0
              OTHERWISE,
                result IS APPOINTED "negative"
            UNDER ORDERS.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("negative");
    }

    /// <summary>
    /// Tests that a guard clause with a throw in the else block propagates the exception.
    /// </summary>
    [Fact]
    public void Execute_GuardClause_WithThrowInElseBlock_ThrowsException()
    {
        string source = """
            HARK! "Guard with throw"
            YEOMAN NAY
              OTHERWISE,
                A HIDEOUS CURSE ON "Guard triggered"
            UNDER ORDERS.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that execution continues past the guard when the condition is truthy.
    /// </summary>
    [Fact]
    public void Execute_GuardClause_TruthyCondition_ExecutionContinuesPastGuard()
    {
        string source = """
            HARK! "Guard falls through and continues"
            YEOMAN VERITY
              OTHERWISE,
                BEHOLD "should not print"
            UNDER ORDERS.
            BEHOLD "after guard"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output.ShouldHaveSingleItem().ShouldBe("after guard");
    }
}
