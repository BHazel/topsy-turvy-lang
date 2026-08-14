using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// <see cref="IExecutionObserver"/> hook-firing tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterObserverTests : TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method fires the observer's function-enter callback before
    /// the callee's body statements, and function-exit after them.
    /// </summary>
    [Fact]
    public void Execute_WithFunctionCall_FiresEnterBeforeBodyStatementsAndExitAfter()
    {
        string source = """
            HARK! "ObserverTest"
            IT IS MY DUTY TO PERFORM Greet UNDER NO OBLIGATION
              BEHOLD "hi"
            MY DUTY IS DISCHARGED.
            SUMMON Greet WITH NOTHING IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        TestExecutionObserver observer = new();
        Interpreter interpreter = new(new TestIO([], new()), observer: observer);

        interpreter.Execute(program);

        int enterIndex = observer.Events.FindIndex(theEvent => theEvent.Kind == "Enter" && theEvent.Detail == "Greet:False");
        int bodyStatementIndex = observer.Events.FindIndex(enterIndex + 1, theEvent => theEvent.Kind == "Statement" && theEvent.Detail == nameof(PrintNode));
        int exitIndex = observer.Events.FindIndex(theEvent => theEvent.Kind == "Exit" && theEvent.Detail == "Greet");

        enterIndex.ShouldBeGreaterThanOrEqualTo(0);
        bodyStatementIndex.ShouldBeGreaterThan(enterIndex);
        exitIndex.ShouldBeGreaterThan(bodyStatementIndex);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method still fires the observer's function-exit callback
    /// when a function returns early from deep inside a conditional.
    /// </summary>
    [Fact]
    public void Execute_WithEarlyReturnInsideConditional_StillFiresFunctionExit()
    {
        string source = """
            HARK! "EarlyReturn"
            IT IS MY DUTY TO PERFORM Choose UNDER THE TERMS OF Flag AS A DECREE TO FIND PEER
              SHOULD IT TRANSPIRE THAT Flag
                QUITE SO.
                  AND SO I FIND 1
              SO MUCH FOR THAT.
              AND SO I FIND 2
            MY DUTY IS DISCHARGED.
            BEHOLD SUMMON Choose WITH VERITY IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        TestExecutionObserver observer = new();
        Interpreter interpreter = new(new TestIO([], new()), observer: observer);

        interpreter.Execute(program);

        observer.Events.ShouldContain(theEvent => theEvent.Kind == "Exit" && theEvent.Detail == "Choose");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method fires the function-enter callback with
    /// <c>isImportedFunction</c> set to <c>true</c> for a function imported via <c>PRAY ADMIT</c>.
    /// </summary>
    [Fact]
    public void Execute_WithImportedFunctionCall_FiresEnterWithIsImportedFunctionTrue()
    {
        string importedSource = """
            HARK! "Utils"
            IT IS MY DUTY TO PERFORM Greet UNDER NO OBLIGATION
              BEHOLD "hi"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string source = """
            HARK! "Main"
            PRAY ADMIT "utils.topsy"
            SUMMON Greet WITH NOTHING IF YOU PLEASE.
            FINALE.
            """;

        Dictionary<string, string> fileSystem = new()
        {
            ["utils.topsy"] = importedSource
        };

        ProgramNode program = this.parser.Parse(source);
        TestExecutionObserver observer = new();
        Interpreter interpreter = new(new TestIO([], new()), observer: observer);

        interpreter.Execute(program, options: new(null, null, name => fileSystem.GetValueOrDefault(name)));

        observer.Events.ShouldContain(theEvent => theEvent.Kind == "Enter" && theEvent.Detail == "Greet:True");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method fires the function-enter callback with
    /// <c>isImportedFunction</c> set to <c>false</c> for a function declared in the main file, even when it
    /// carries a namespace.
    /// </summary>
    [Fact]
    public void Execute_WithNamespacedLocalFunctionCall_FiresEnterWithIsImportedFunctionFalse()
    {
        string source = """
            HARK! "LocalNamespace"
            TOWN Mathematical
            IT IS MY DUTY TO PERFORM Add UNDER THE TERMS OF Num1 AS A PEER AND Num2 AS A PEER TO FIND PEER
              AND SO I FIND SUM OF Num1 AND Num2
            MY DUTY IS DISCHARGED.
            BEHOLD SUMMON Add WITH 2 AND 3 IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        TestExecutionObserver observer = new();
        Interpreter interpreter = new(new TestIO([], new()), observer: observer);

        interpreter.Execute(program);

        observer.Events.ShouldContain(theEvent => theEvent.Kind == "Enter" && theEvent.Detail == "Add:False");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method fires the pre-statement callback, in order, for a
    /// declaration, an assignment, a conditional and the print statement nested inside it.
    /// </summary>
    [Fact]
    public void Execute_WithSeveralStatementKinds_FiresBeforeStatementInOrder()
    {
        string source = """
            HARK! "StatementKinds"
            PRAY WELCOME x AS A PEER BEING 1
            x IS APPOINTED 2
            SHOULD IT TRANSPIRE THAT VERITY
              QUITE SO.
                BEHOLD x
            SO MUCH FOR THAT.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        TestExecutionObserver observer = new();
        Interpreter interpreter = new(new TestIO([], new()), observer: observer);

        interpreter.Execute(program);

        List<string> statementKinds = [.. observer.Events
            .Where(theEvent => theEvent.Kind == "Statement")
            .Select(theEvent => theEvent.Detail)];

        statementKinds.ShouldBe([nameof(DeclarationNode), nameof(AssignmentNode), nameof(ConditionalNode), nameof(PrintNode)]);
    }
}
