using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.Runtime;
using Xunit;

namespace BWHazel.TopsyTurvy.Tests;

/// <summary>
/// Tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method prints the expected greeting for a Hello World program.
    /// </summary>
    [Fact]
    public void Execute_WithHelloWorld_PrintsGreeting()
    {
        string source = """
            HARK! "Hello World"
            PRINCIPALS
              PRAY WELCOME greeting AS A YARN BEING "Hello, World!"
            THE CURTAIN RISES.
            BEHOLD greeting
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();
        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Single(output);
        Assert.Equal("Hello, World!", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method concatenates strings when using WOVEN OF.
    /// </summary>
    [Fact]
    public void Execute_WithWovenOf_ConcatenatesStrings()
    {
        string source = """
            HARK! "Concatenation"
            PRINCIPALS
              PRAY WELCOME result AS A YARN
            THE CURTAIN RISES.
            result IS APPOINTED WOVEN OF "Hello" AND ", " AND "World!" IF YOU PLEASE.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();
        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("Hello, World!", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces correct arithmetic results.
    /// </summary>
    [Fact]
    public void Execute_WithArithmetic_ProducesCorrectResults()
    {
        string source = """
            HARK! "Arithmetic"
            PRINCIPALS
              PRAY WELCOME x AS A PEER BEING 6
              PRAY WELCOME y AS A PEER BEING 7
              PRAY WELCOME result AS A PEER
            THE CURTAIN RISES.
            result IS APPOINTED PRODUCT OF x AND y
            BEHOLD result
            result IS APPOINTED SUM OF result AND 2
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();
        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(2, output.Count);
        Assert.Equal("42", output[0]);
        Assert.Equal("44", output[1]);
    }

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

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("big", output[0]);
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

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("6", output[0]);
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

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("5", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly executes a recursive function.
    /// </summary>
    [Fact]
    public void Execute_WithRecursiveFunction_ReturnsCorrectValue()
    {
        string source = """
            HARK! "Fibonacci"
            PRINCIPALS
              PRAY WELCOME result AS A PEER BEING 0
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM fib UNDER THE TERMS OF n
              SHOULD IT TRANSPIRE THAT LOWER DEGREE n AND 2
                QUITE SO.
                  AND SO I FIND 1
              SO MUCH FOR THAT.
              AND SO I FIND SUM OF SUMMON fib WITH DIFFERENCE OF n AND 1 IF YOU PLEASE. AND SUMMON fib WITH DIFFERENCE OF n AND 2 IF YOU PLEASE.
            MY DUTY IS DISCHARGED.
            result IS APPOINTED SUMMON fib WITH 6 IF YOU PLEASE.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();
        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("13", output[0]);
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

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("two", output[0]);
    }

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

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("catastrophe", output[0]);
    }

    /// <summary>
    /// Creates an <see cref="Interpreter"/> instance and associated output.
    /// </summary>
    /// <param name="inputLines">The input lines to provide to the interpreter.</param>
    /// <returns>A tuple containing the <see cref="Interpreter"/> instance and the list of output lines.</returns>
    private (Interpreter Interpreter, List<string> Output) CreateInterpreter(params string[] inputLines)
    {
        List<string> output = [];
        Queue<string> input = new(inputLines);
        TestIO io = new(output, input);
        return (new(io), output);
    }
}
