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

        diagnostics.HasErrors.ShouldBeFalse();
        output.ShouldHaveSingleItem();
        output[0].ShouldBe("Hello, World!");
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

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("Hello, World!");
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

        diagnostics.HasErrors.ShouldBeFalse();
        output.Count.ShouldBe(4);
        output[0].ShouldBe("42");
        output[1].ShouldBe("44");
        output[2].ShouldBe("22");
        output[3].ShouldBe("20");
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

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("Ko-Ko");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method assigns the cast result directly to a variable via <c>IS APPOINTED AS IT WERE</c>.
    /// </summary>
    [Fact]
    public void Execute_ExpressionCast_InAssignment_AssignsResult()
    {
        string source = """
            HARK! "Cast in assignment"
            PRINCIPALS
              PRAY WELCOME n AS A PEER BEING 7
              PRAY WELCOME s AS A YARN
            THE CURTAIN RISES.
            s IS APPOINTED AS IT WERE n AS A YARN
            BEHOLD s
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("7");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method initialises a variable using a cast expression in a <c>BEING</c> clause.
    /// </summary>
    [Fact]
    public void Execute_ExpressionCast_InDeclaration_InitialisesVariable()
    {
        string source = """
            HARK! "Cast in declaration"
            PRINCIPALS
              PRAY WELCOME n AS A PEER BEING 3
              PRAY WELCOME s AS A YARN BEING AS IT WERE n AS A YARN
            THE CURTAIN RISES.
            BEHOLD s
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("3");
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

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("Hello, World!");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns a runtime error diagnostic when IS APPOINTED targets a CONSERVATIVE variable.
    /// </summary>
    [Fact]
    public void Execute_AssignToConstant_ReturnsRuntimeErrorDiagnostic()
    {
        string source = """
            HARK! "Constant Assignment"
            PRAY WELCOME x AS A CONSERVATIVE PEER BEING 10
            x IS APPOINTED 20
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> _) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns a runtime error diagnostic when PRAY TELL targets a CONSERVATIVE variable.
    /// </summary>
    [Fact]
    public void Execute_InputToConstant_ReturnsRuntimeErrorDiagnostic()
    {
        string source = """
            HARK! "Constant Input"
            PRAY WELCOME x AS A CONSERVATIVE YARN
            PRAY TELL x
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> _) = this.CreateInterpreter("some input");

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method allows assignment to a LIBERAL variable.
    /// </summary>
    [Fact]
    public void Execute_LiberalDeclaration_AllowsAssignment()
    {
        string source = """
            HARK! "Liberal Variable"
            PRAY WELCOME x AS A LIBERAL PEER BEING 10
            x IS APPOINTED 99
            BEHOLD x
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("99");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method allows assignment to a variable declared without a modifier.
    /// </summary>
    [Fact]
    public void Execute_NoModifierDeclaration_AllowsAssignment()
    {
        string source = """
            HARK! "Mutable Default"
            PRAY WELCOME x AS A PEER BEING 10
            x IS APPOINTED 99
            BEHOLD x
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("99");
    }

    /// <summary>
    /// Tests that a variable declared in one <see cref="Interpreter.Execute"/> call is readable in a subsequent
    /// call when the same <see cref="TopsyTurvyEnvironment"/> is supplied as <c>sessionEnvironment</c>.
    /// </summary>
    [Fact]
    public void Execute_WithSessionEnvironment_PersistsVariableAcrossCalls()
    {
        string declareSource = """
            HARK! "Turn 1"
            PRAY WELCOME x AS A PEER BEING 42
            FINALE.
            """;

        string readSource = """
            HARK! "Turn 2"
            BEHOLD x
            FINALE.
            """;

        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();
        TopsyTurvyEnvironment session = TopsyTurvyEnvironment.CreateGlobal();
        session.Declare(Keywords.SpecialNames.TheProps, TopsyTurvyValue.Array([]), isConstant: true);

        interpreter.Execute(this.parser.Parse(declareSource), sessionEnvironment: session);
        DiagnosticCollection diagnostics = interpreter.Execute(this.parser.Parse(readSource), sessionEnvironment: session);

        diagnostics.HasErrors.ShouldBeFalse();
        output.ShouldHaveSingleItem();
        output[0].ShouldBe("42");
    }

    /// <summary>
    /// Tests that a function defined in one <see cref="Interpreter.Execute"/> call is callable in a subsequent
    /// call when the same <see cref="Interpreter"/> instance and <see cref="TopsyTurvyEnvironment"/> are reused.
    /// </summary>
    [Fact]
    public void Execute_WithSessionEnvironment_PreservesFunctionDefinitionAcrossCalls()
    {
        string defineSource = """
            HARK! "Turn 1"
            IT IS MY DUTY TO PERFORM Double UNDER THE TERMS OF n AS A PEER TO FIND PEER
                AND SO I FIND SUM OF n AND n
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string callSource = """
            HARK! "Turn 2"
            BEHOLD SUMMON Double WITH 21 IF YOU PLEASE.
            FINALE.
            """;

        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();
        TopsyTurvyEnvironment session = TopsyTurvyEnvironment.CreateGlobal();
        session.Declare(Keywords.SpecialNames.TheProps, TopsyTurvyValue.Array([]), isConstant: true);

        interpreter.Execute(this.parser.Parse(defineSource), sessionEnvironment: session);
        DiagnosticCollection diagnostics = interpreter.Execute(this.parser.Parse(callSource), sessionEnvironment: session);

        diagnostics.HasErrors.ShouldBeFalse();
        output.ShouldHaveSingleItem();
        output[0].ShouldBe("42");
    }
}
