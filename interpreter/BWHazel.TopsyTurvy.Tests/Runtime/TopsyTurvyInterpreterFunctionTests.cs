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
            IT IS MY DUTY TO PERFORM fib UNDER THE TERMS OF n AS A PEER TO FIND PEER
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

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("13");
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
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
              BEHOLD name
            MY DUTY IS DISCHARGED.
            SUMMON greet WITH 1 AND 2 IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
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

        diagnostics.HasErrors.ShouldBeTrue();
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
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
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

        DiagnosticCollection diagnostics = interpreter.Execute(program, options: new(null, null, name => fileSystem.GetValueOrDefault(name)));

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("Hello, World!");
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

        DiagnosticCollection diagnostics = interpreter.Execute(program, options: new(null, null, _ => null));

        diagnostics.HasErrors.ShouldBeTrue();
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
            options: new(null, null, _ => "THIS IS NOT VALID TOPSY TURVY SYNTAX AT ALL"));

        diagnostics.HasErrors.ShouldBeTrue();
    }
}
