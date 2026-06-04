using System.Collections.Generic;
using System.Threading;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Exception and error handling tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterExceptionTests : TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly executes a try-catch block and handles exceptions.
    /// </summary>
    [Fact]
    public void Execute_WithCaughtException_ExecutesExceptionBlock()
    {
        string source = """
            HARK! "Exception"
            PRINCIPALS
              PRAY WELCOME result AS A YARN BEING "none"
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM risky UNDER NO OBLIGATION
              A HIDEOUS CURSE ON "catastrophe"
            MY DUTY IS DISCHARGED.
            WITH THE GREATEST RESPECT, SUMMON risky WITH NOTHING IF YOU PLEASE.
              WITH GRATITUDE
                result IS APPOINTED "success"
              MODIFIED RAPTURE
                result IS APPOINTED JUST SO
            THAT CONCLUDES THE MATTER.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("catastrophe");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method executes the success block when no exception is thrown in a try-catch.
    /// </summary>
    [Fact]
    public void Execute_WithTryCatchAndNoException_ExecutesSuccessBlock()
    {
        string source = """
            HARK! "Try Success"
            PRINCIPALS
              PRAY WELCOME result AS A YARN BEING "none"
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM safe UNDER NO OBLIGATION
              AND SO I FIND "ok"
            MY DUTY IS DISCHARGED.
            WITH THE GREATEST RESPECT, SUMMON safe WITH NOTHING IF YOU PLEASE.
              WITH GRATITUDE
                result IS APPOINTED JUST SO
              MODIFIED RAPTURE
                result IS APPOINTED "error"
            THAT CONCLUDES THE MATTER.
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
    /// Tests that the <see cref="Interpreter.Execute"/> method returns an error diagnostic when an unhandled exception is thrown.
    /// </summary>
    [Fact]
    public void Execute_WithUncaughtThrow_ReturnsErrorDiagnostic()
    {
        string source = """
            HARK! "Uncaught Exception"
            PRINCIPALS
            THE CURTAIN RISES.
            A HIDEOUS CURSE ON "something went wrong"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns an error diagnostic when the cancellation token is cancelled.
    /// </summary>
    [Fact]
    public void Execute_WithCancelledToken_ReturnsTimeoutDiagnostic()
    {
        string source = """
            HARK! "Infinite"
            PRINCIPALS
              PRAY WELCOME x AS A PEER BEING 0
            THE CURTAIN RISES.
            BY A LEGAL FICTION
              x IS APPOINTED SUM OF x AND 1
            THE TERM EXPIRES.
            FINALE.
            """;

        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program, cancellationTokenSource.Token);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns an error diagnostic when the max duration elapses.
    /// </summary>
    [Fact]
    public void Execute_WithElapsedMaxDuration_ReturnsTimeoutDiagnostic()
    {
        string source = """
            HARK! "Infinite"
            PRINCIPALS
              PRAY WELCOME x AS A PEER BEING 0
            THE CURTAIN RISES.
            BY A LEGAL FICTION
              x IS APPOINTED SUM OF x AND 1
            THE TERM EXPIRES.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();
        
        DiagnosticCollection diagnostics = interpreter.Execute(program, timeout: System.TimeSpan.FromMilliseconds(1));

        diagnostics.HasErrors.ShouldBeTrue();
    }
}
