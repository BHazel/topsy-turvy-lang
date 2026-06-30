using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Ternary expression tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterTernaryTests : TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// Tests that a ternary expression with a truthy condition returns the true-value.
    /// </summary>
    [Fact]
    public void Execute_TernaryExpression_TruthyCondition_ReturnsTrueValue()
    {
        string source = """
            HARK! "Ternary true branch"
            BEHOLD "yes" SHOULD IT TRANSPIRE THAT VERITY OTHERWISE, "no"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("yes");
    }

    /// <summary>
    /// Tests that a ternary expression with a falsy condition returns the false-value.
    /// </summary>
    [Fact]
    public void Execute_TernaryExpression_FalsyCondition_ReturnsFalseValue()
    {
        string source = """
            HARK! "Ternary false branch"
            BEHOLD "yes" SHOULD IT TRANSPIRE THAT NAY OTHERWISE, "no"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("no");
    }

    /// <summary>
    /// Tests that a ternary expression evaluates the condition expression correctly when it is a comparison.
    /// </summary>
    [Fact]
    public void Execute_TernaryExpression_WithComparisonCondition_SelectsCorrectBranch()
    {
        string source = """
            HARK! "Ternary comparison condition"
            PRINCIPALS
              PRAY WELCOME score AS A PEER BEING 95
            THE CURTAIN RISES.
            BEHOLD "High" SHOULD IT TRANSPIRE THAT PRE-ADAMITE score AND 90 OTHERWISE, "Low"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("High");
    }

    /// <summary>
    /// Tests that the true value is not evaluated when the condition is falsy.
    /// </summary>
    [Fact]
    public void Execute_TernaryExpression_FalsyCondition_DoesNotEvaluateTrueValue()
    {
        string source = """
            HARK! "Ternary short circuit"
            PRINCIPALS
              PRAY WELCOME score AS A PEER BEING 50
            THE CURTAIN RISES.
            BEHOLD "High" SHOULD IT TRANSPIRE THAT PRE-ADAMITE score AND 90 OTHERWISE, "Low"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("Low");
    }

    /// <summary>
    /// Tests that a right-chained ternary returns the correct value when the outer condition is truthy.
    /// </summary>
    [Fact]
    public void Execute_TernaryExpression_RightChained_OuterTruthy_ReturnsOuterTrueValue()
    {
        string source = """
            HARK! "Right-chained ternary outer true"
            PRINCIPALS
              PRAY WELCOME score AS A PEER BEING 95
            THE CURTAIN RISES.
            BEHOLD "A" SHOULD IT TRANSPIRE THAT PRE-ADAMITE score AND 90 OTHERWISE, "B" SHOULD IT TRANSPIRE THAT PRE-ADAMITE score AND 70 OTHERWISE, "C"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("A");
    }

    /// <summary>
    /// Tests that a right-chained ternary falls through to the inner branch when the outer condition is falsy.
    /// </summary>
    [Fact]
    public void Execute_TernaryExpression_RightChained_OuterFalsy_InnerTruthy_ReturnsInnerTrueValue()
    {
        string source = """
            HARK! "Right-chained ternary inner true"
            PRINCIPALS
              PRAY WELCOME score AS A PEER BEING 75
            THE CURTAIN RISES.
            BEHOLD "A" SHOULD IT TRANSPIRE THAT PRE-ADAMITE score AND 90 OTHERWISE, "B" SHOULD IT TRANSPIRE THAT PRE-ADAMITE score AND 70 OTHERWISE, "C"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("B");
    }

    /// <summary>
    /// Tests that a right-chained ternary returns the final false value when all conditions are falsy.
    /// </summary>
    [Fact]
    public void Execute_TernaryExpression_RightChained_AllFalsy_ReturnsFinalFalseValue()
    {
        string source = """
            HARK! "Right-chained ternary all false"
            PRINCIPALS
              PRAY WELCOME score AS A PEER BEING 50
            THE CURTAIN RISES.
            BEHOLD "A" SHOULD IT TRANSPIRE THAT PRE-ADAMITE score AND 90 OTHERWISE, "B" SHOULD IT TRANSPIRE THAT PRE-ADAMITE score AND 70 OTHERWISE, "C"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("C");
    }

    /// <summary>
    /// Tests that a ternary expression in an assignment stores the result in the target variable.
    /// </summary>
    [Fact]
    public void Execute_TernaryExpression_InAssignment_StoresResultInVariable()
    {
        string source = """
            HARK! "Ternary in assignment"
            PRINCIPALS
              PRAY WELCOME flag AS A DECREE BEING VERITY
              PRAY WELCOME label AS A YARN
            THE CURTAIN RISES.
            label IS APPOINTED "on" SHOULD IT TRANSPIRE THAT flag OTHERWISE, "off"
            BEHOLD label
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("on");
    }
}
