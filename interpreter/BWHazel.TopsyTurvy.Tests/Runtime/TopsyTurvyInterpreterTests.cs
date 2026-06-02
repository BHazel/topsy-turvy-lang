using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Core execution tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterTests : TopsyTurvyInterpreterTestBase
{
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
            result IS APPOINTED QUOTIENT OF result AND 2
            BEHOLD result
            result IS APPOINTED DIFFERENCE OF result AND 2
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(4, output.Count);
        Assert.Equal("42", output[0]);
        Assert.Equal("44", output[1]);
        Assert.Equal("22", output[2]);
        Assert.Equal("20", output[3]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method reads a value from the input stream into the target variable.
    /// </summary>
    [Fact]
    public void Execute_WithInputStatement_AssignsReadLineToVariable()
    {
        string source = """
            HARK! "Input"
            PRINCIPALS
              PRAY WELCOME name AS A YARN
            THE CURTAIN RISES.
            PRAY TELL name
            BEHOLD name
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter("Ko-Ko");

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("Ko-Ko", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method casts a variable in-place and updates its stored value.
    /// </summary>
    [Fact]
    public void Execute_WithInPlaceCast_ConvertsVariableType()
    {
        string source = """
            HARK! "In-Place Cast"
            PRINCIPALS
              PRAY WELCOME n AS A PEER BEING 42
            THE CURTAIN RISES.
            n IS HENCEFORTH A YARN
            BEHOLD n
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("42", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method stores the cast result in the JUST SO register.
    /// </summary>
    [Fact]
    public void Execute_WithExpressionCast_StoresResultInJustSo()
    {
        string source = """
            HARK! "Expression Cast"
            PRINCIPALS
              PRAY WELCOME n AS A PEER BEING 5
            THE CURTAIN RISES.
            AS IT WERE n AS A YARN
            BEHOLD JUST SO
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("5", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method interpolates a variable reference inside a printed string.
    /// </summary>
    [Fact]
    public void Execute_WithStringInterpolation_ReplacesPlaceholderWithVariableValue()
    {
        string source = """
            HARK! "String Interpolation"
            PRINCIPALS
              PRAY WELCOME name AS A YARN BEING "World"
            THE CURTAIN RISES.
            BEHOLD "Hello, {name}!"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("Hello, World!", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method stores the result of an expression statement in JUST SO.
    /// </summary>
    [Fact]
    public void Execute_WithExpressionStatement_StoresResultInJustSo()
    {
        string source = """
            HARK! "Expression Statement"
            PRINCIPALS
            THE CURTAIN RISES.
            SUM OF 3 AND 4
            BEHOLD JUST SO
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();
        
        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("7", output[0]);
    }
}
