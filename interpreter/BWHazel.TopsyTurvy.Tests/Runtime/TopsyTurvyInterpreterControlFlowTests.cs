using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Control flow tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterControlFlowTests : TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly executes an inline conditional expression.
    /// </summary>
    [Fact]
    public void Execute_WithInlineConditional_ExecutesCorrectBranch()
    {
        string source = """
            HARK! "Conditional"
            PRINCIPALS
              PRAY WELCOME x AS A PEER BEING 5
              PRAY WELCOME result AS A YARN
            THE CURTAIN RISES.
            SHOULD IT TRANSPIRE THAT PRE-ADAMITE x AND 3
              QUITE SO.
                result IS APPOINTED "big"
              OTHERWISE,
                result IS APPOINTED "small"
            SO MUCH FOR THAT.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("big");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method evaluates the else-if branch when the first condition is false.
    /// </summary>
    [Fact]
    public void Execute_WithElseIfBranch_ExecutesFirstTrueBranch()
    {
        string source = """
            HARK! "Else-If"
            PRINCIPALS
              PRAY WELCOME x AS A PEER BEING 2
              PRAY WELCOME result AS A YARN
            THE CURTAIN RISES.
            SHOULD IT TRANSPIRE THAT ALIKE x AND 1
              QUITE SO.
                result IS APPOINTED "one"
              OR, IF NOT, ALIKE x AND 2
                result IS APPOINTED "two"
              OTHERWISE,
                result IS APPOINTED "other"
            SO MUCH FOR THAT.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("two");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly executes a WHILST loop.
    /// </summary>
    [Fact]
    public void Execute_WithWhilstLoop_IteratesCorrectly()
    {
        string source = """
            HARK! "Whilst Loop"
            PRINCIPALS
              PRAY WELCOME idx AS A PEER BEING 1
              PRAY WELCOME total AS A PEER BEING 0
            THE CURTAIN RISES.
            BY A LEGAL FICTION WHILST LOWER DEGREE idx AND 4
              total IS APPOINTED SUM OF total AND idx
              idx IS APPOINTED SUM OF idx AND 1
            THE TERM EXPIRES.
            BEHOLD total
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("6");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly executes an ascending loop.
    /// </summary>
    [Fact]
    public void Execute_WithAscendingLoop_CountsCorrectly()
    {
        string source = """
            HARK! "Ascending Loop"
            PRINCIPALS
              PRAY WELCOME idx AS A PEER BEING 0
              PRAY WELCOME count AS A PEER BEING 0
            THE CURTAIN RISES.
            BY A LEGAL FICTION ASCENDING idx UNTIL ALIKE idx AND 5
              count IS APPOINTED SUM OF count AND 1
            THE TERM EXPIRES.
            BEHOLD count
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("5");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly executes a descending loop.
    /// </summary>
    [Fact]
    public void Execute_WithDescendingLoop_CountsDownCorrectly()
    {
        string source = """
            HARK! "Descending Loop"
            PRINCIPALS
              PRAY WELCOME loopIdx AS A PEER BEING 3
              PRAY WELCOME total AS A PEER BEING 0
            THE CURTAIN RISES.
            BY A LEGAL FICTION DESCENDING loopIdx UNTIL ALIKE loopIdx AND 0
              total IS APPOINTED SUM OF total AND loopIdx
            THE TERM EXPIRES.
            BEHOLD total
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("6");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method retains an ascending loop variable pre-loop value rather than resetting it to 0.
    /// </summary>
    [Fact]
    public void Execute_WithAscendingLoop_RetainsPreLoopStartValue()
    {
        string source = """
            HARK! "Ascending Loop Retains Start"
            PRINCIPALS
              PRAY WELCOME idx AS A PEER BEING 5
            THE CURTAIN RISES.
            BY A LEGAL FICTION ASCENDING idx UNTIL ALIKE idx AND 8
              THAT WILL DO.
            THE TERM EXPIRES.
            BEHOLD idx
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("5");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method steps an ascending loop by the evaluated BY expression.
    /// </summary>
    [Fact]
    public void Execute_WithAscendingLoopByStep_CountsByStep()
    {
        string source = """
            HARK! "Ascending Loop By Step"
            PRINCIPALS
              PRAY WELCOME idx AS A PEER BEING 5
            THE CURTAIN RISES.
            BY A LEGAL FICTION ASCENDING idx BY 3 UNTIL PRE-ADAMITE idx AND 20
              BEHOLD idx
            THE TERM EXPIRES.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output.ShouldBe(["5", "8", "11", "14", "17", "20"]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method steps a descending loop by the evaluated BY expression.
    /// </summary>
    [Fact]
    public void Execute_WithDescendingLoopByStep_CountsDownByStep()
    {
        string source = """
            HARK! "Descending Loop By Step"
            PRINCIPALS
              PRAY WELCOME idx AS A PEER BEING 10
            THE CURTAIN RISES.
            BY A LEGAL FICTION DESCENDING idx BY 2 UNTIL LOWER DEGREE idx AND 0
              BEHOLD idx
            THE TERM EXPIRES.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output.ShouldBe(["10", "8", "6", "4", "2", "0"]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method breaks out of an infinite loop when a break is encountered.
    /// </summary>
    [Fact]
    public void Execute_WithInfiniteLoopAndBreak_BreaksAtCorrectIteration()
    {
        string source = """
            HARK! "Infinite Loop"
            PRINCIPALS
              PRAY WELCOME count AS A PEER BEING 0
            THE CURTAIN RISES.
            BY A LEGAL FICTION
              count IS APPOINTED SUM OF count AND 1
              SHOULD IT TRANSPIRE THAT ALIKE count AND 3
                QUITE SO.
                  THAT WILL DO.
              SO MUCH FOR THAT.
            THE TERM EXPIRES.
            BEHOLD count
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("3");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method skips the rest of a loop body when a continue is encountered.
    /// </summary>
    [Fact]
    public void Execute_WithContinueInLoop_SkipsRemainingBodyForThatIteration()
    {
        string source = """
            HARK! "Continue"
            PRINCIPALS
              PRAY WELCOME idx AS A PEER BEING 0
              PRAY WELCOME count AS A PEER BEING 0
            THE CURTAIN RISES.
            BY A LEGAL FICTION ASCENDING idx UNTIL ALIKE idx AND 5
              SHOULD IT TRANSPIRE THAT ALIKE idx AND 2
                QUITE SO.
                  ONCE MORE.
              SO MUCH FOR THAT.
              count IS APPOINTED SUM OF count AND 1
            THE TERM EXPIRES.
            BEHOLD count
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("4");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly executes a switch statement and breaks after a matching case.
    /// </summary>
    [Fact]
    public void Execute_WithSwitch_MatchesCaseAndBreaks()
    {
        string source = """
            HARK! "Switch"
            PRINCIPALS
              PRAY WELCOME x AS A PEER BEING 2
              PRAY WELCOME result AS A YARN BEING "none"
            THE CURTAIN RISES.
            IN WHICH CAPACITY? x
              WHEN ACTING AS 1
                result IS APPOINTED "one"
                THAT WILL DO.
              WHEN ACTING AS 2
                result IS APPOINTED "two"
                THAT WILL DO.
              WHEN ACTING AS 3
                result IS APPOINTED "three"
                THAT WILL DO.
            NOTHING COULD BE MORE SATISFACTORY.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("two");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method executes the default case when no switch case matches.
    /// </summary>
    [Fact]
    public void Execute_WithSwitchAndDefaultCase_ExecutesDefaultWhenNoMatch()
    {
        string source = """
            HARK! "Switch Default"
            PRINCIPALS
              PRAY WELCOME x AS A PEER BEING 99
              PRAY WELCOME result AS A YARN BEING "none"
            THE CURTAIN RISES.
            IN WHICH CAPACITY? x
              WHEN ACTING AS 1
                result IS APPOINTED "one"
                THAT WILL DO.
              FAILING ALL OF THE ABOVE,
                result IS APPOINTED "other"
            NOTHING COULD BE MORE SATISFACTORY.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("other");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method falls through to subsequent cases when no break is present.
    /// </summary>
    [Fact]
    public void Execute_WithSwitchFallThrough_ExecutesSubsequentCaseBlocks()
    {
        string source = """
            HARK! "Switch Fall-Through"
            PRINCIPALS
              PRAY WELCOME x AS A PEER BEING 1
              PRAY WELCOME result AS A YARN BEING ""
            THE CURTAIN RISES.
            IN WHICH CAPACITY? x
              WHEN ACTING AS 1
                result IS APPOINTED WOVEN OF result AND "a" IF YOU PLEASE.
              WHEN ACTING AS 2
                result IS APPOINTED WOVEN OF result AND "b" IF YOU PLEASE.
              WHEN ACTING AS 3
                result IS APPOINTED WOVEN OF result AND "c" IF YOU PLEASE.
                THAT WILL DO.
            NOTHING COULD BE MORE SATISFACTORY.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();
        
        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("abc");
    }
}
