using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Function and import tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterFunctionTests : TopsyTurvyInterpreterTestBase
{
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
    /// Tests that the <see cref="Interpreter.Execute"/> method returns null from a function that returns without a value.
    /// </summary>
    [Fact]
    public void Execute_WithEarlyReturnNoValue_ReturnsNullToJustSo()
    {
        string source = """
            HARK! "Early Return"
            PRINCIPALS
              PRAY WELCOME result AS A YARN
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM earlyExit UNDER NO OBLIGATION
              MY DUTY IS PREMATURELY DISCHARGED.
              AND SO I FIND "never"
            MY DUTY IS DISCHARGED.
            SUMMON earlyExit WITH NOTHING IF YOU PLEASE.
            result IS APPOINTED JUST SO
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("NAUGHT", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns an error diagnostic when a function is called with the wrong number of arguments.
    /// </summary>
    [Fact]
    public void Execute_WithWrongArgumentCount_ReturnsErrorDiagnostic()
    {
        string source = """
            HARK! "Wrong Args"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name
              BEHOLD name
            MY DUTY IS DISCHARGED.
            SUMMON greet WITH 1 AND 2 IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.True(diagnostics.HasErrors);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns an error diagnostic when an undefined function is called.
    /// </summary>
    [Fact]
    public void Execute_WithUndefinedFunction_ReturnsErrorDiagnostic()
    {
        string source = """
            HARK! "Undefined Function"
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON noSuchFunction WITH NOTHING IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.True(diagnostics.HasErrors);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method resolves imports via the file resolver delegate when supplied.
    /// </summary>
    [Fact]
    public void Execute_WithFileResolverImport_LoadsFunctionFromResolver()
    {
        string importedSource = """
            HARK! "Utils"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name
              BEHOLD WOVEN OF "Hello, " AND name AND "!" IF YOU PLEASE.
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string source = """
            HARK! "Import Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY ADMIT "utils.topsy"
            SUMMON greet WITH "World" IF YOU PLEASE.
            FINALE.
            """;

        Dictionary<string, string> fileSystem = new()
        {
            ["utils.topsy"] = importedSource
        };

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program, fileResolver: name => fileSystem.GetValueOrDefault(name));

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("Hello, World!", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns an error diagnostic when the file resolver cannot resolve an import.
    /// </summary>
    [Fact]
    public void Execute_WithUnresolvableImport_ReturnsErrorDiagnostic()
    {
        string source = """
            HARK! "Import Missing"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY ADMIT "missing.topsy"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program, fileResolver: _ => null);

        Assert.True(diagnostics.HasErrors);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns an error diagnostic when a syntax error is found in an imported file.
    /// </summary>
    [Fact]
    public void Execute_WithSyntaxErrorInImport_ReturnsErrorDiagnostic()
    {
        string source = """
            HARK! "Import Syntax Error"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY ADMIT "broken.topsy"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();
        
        DiagnosticCollection diagnostics = interpreter.Execute(
            program,
            fileResolver: _ => "THIS IS NOT VALID TOPSY TURVY SYNTAX AT ALL");

        Assert.True(diagnostics.HasErrors);
    }
}
