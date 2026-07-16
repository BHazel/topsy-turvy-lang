using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Pointer operation tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterPointerTests : TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method declares a pointer as NAUGHT when no BEING clause is present.
    /// </summary>
    [Fact]
    public void Execute_PointerDeclaration_WithoutInitialValue_DefaultsToNaught()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER
            BEHOLD NumberPointer
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("NAUGHT");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method displays a synthetic hexadecimal address for an assigned pointer.
    /// </summary>
    [Fact]
    public void Execute_PointerDisplay_RendersSyntheticHexAddress()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Number AS A PEER BEING 42
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Number
            BEHOLD NumberPointer
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldMatch("^0x[0-9A-F]{8}$");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method resolves a dereference of a pointer to a scalar variable to that variable current value.
    /// </summary>
    [Fact]
    public void Execute_DereferenceRead_PointerToScalar_ReturnsVariableValue()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Number AS A PEER BEING 42
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Number
            BEHOLD VIEW FROM NumberPointer
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("42");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method decays a pointer to an array to the first element on address-of.
    /// </summary>
    [Fact]
    public void Execute_AddressOfArray_DecaysToFirstElement()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Values AS A LITTLE LIST OF PEER BEING 10 AND 20 AND 30 IF YOU PLEASE.
            PRAY WELCOME ValuesPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Values
            BEHOLD VIEW FROM ValuesPointer
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("10");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method decays a pointer to a YARN to the first character on address-of.
    /// </summary>
    [Fact]
    public void Execute_AddressOfString_DecaysToFirstCharacter()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Word AS A YARN BEING "Ruddigore"
            PRAY WELCOME WordPointer AS A GALLERY PICTURE OF STITCH BEING GALLERY PICTURE TO Word
            BEHOLD VIEW FROM WordPointer
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("R");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method writes through a pointer to a scalar variable and updates the aliased variable.
    /// </summary>
    [Fact]
    public void Execute_DereferenceWrite_PointerToScalar_UpdatesVariable()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Number AS A PEER BEING 42
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Number
            VIEW FROM NumberPointer IS APPOINTED 99
            BEHOLD Number
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("99");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method writes through a pointer to an array element and updates the aliased element.
    /// </summary>
    [Fact]
    public void Execute_DereferenceWrite_PointerToArrayElement_UpdatesElement()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Values AS A LITTLE LIST OF PEER BEING 10 AND 20 AND 30 IF YOU PLEASE.
            PRAY WELCOME ValuesPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Values
            VIEW FROM ValuesPointer IS APPOINTED 99
            BEHOLD VICTIM 1 ON Values
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("99");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces a runtime error when writing through a pointer to a YARN character, since string characters cannot be reassigned individually.
    /// </summary>
    [Fact]
    public void Execute_DereferenceWrite_PointerToStringElement_ProducesRuntimeError()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Word AS A YARN BEING "Ruddigore"
            PRAY WELCOME WordPointer AS A GALLERY PICTURE OF STITCH BEING GALLERY PICTURE TO Word
            VIEW FROM WordPointer IS APPOINTED 'X'
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces a runtime error when dereferencing an unassigned pointer.
    /// </summary>
    [Fact]
    public void Execute_DereferenceRead_NaughtPointer_ProducesRuntimeError()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER
            BEHOLD VIEW FROM NumberPointer
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces a runtime error when writing through an unassigned pointer.
    /// </summary>
    [Fact]
    public void Execute_DereferenceWrite_NaughtPointer_ProducesRuntimeError()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER
            VIEW FROM NumberPointer IS APPOINTED 5
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method moves a pointer to an array element forward by a given offset for a SUM OF expression.
    /// </summary>
    [Fact]
    public void Execute_PointerArithmetic_SumOf_MovesToNextElement()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Values AS A LITTLE LIST OF PEER BEING 10 AND 20 AND 30 IF YOU PLEASE.
            PRAY WELCOME ValuesPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Values
            ValuesPointer IS APPOINTED SUM OF ValuesPointer AND 2
            BEHOLD VIEW FROM ValuesPointer
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("30");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method moves a pointer to an array element backward by a given offset for a DIFFERENCE OF expression.
    /// </summary>
    [Fact]
    public void Execute_PointerArithmetic_DifferenceOf_MovesToPreviousElement()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Values AS A LITTLE LIST OF PEER BEING 10 AND 20 AND 30 IF YOU PLEASE.
            PRAY WELCOME ValuesPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Values
            ValuesPointer IS APPOINTED SUM OF ValuesPointer AND 2
            ValuesPointer IS APPOINTED DIFFERENCE OF ValuesPointer AND 1
            BEHOLD VIEW FROM ValuesPointer
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("20");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces a runtime error when pointer arithmetic moves past the end of the target array.
    /// </summary>
    [Fact]
    public void Execute_PointerArithmetic_ForwardOutOfBounds_ProducesRuntimeError()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Values AS A LITTLE LIST OF PEER BEING 10 AND 20 IF YOU PLEASE.
            PRAY WELCOME ValuesPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Values
            ValuesPointer IS APPOINTED SUM OF ValuesPointer AND 5
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces a runtime error when pointer arithmetic moves before the start of the target array.
    /// </summary>
    [Fact]
    public void Execute_PointerArithmetic_BackwardOutOfBounds_ProducesRuntimeError()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Values AS A LITTLE LIST OF PEER BEING 10 AND 20 IF YOU PLEASE.
            PRAY WELCOME ValuesPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Values
            ValuesPointer IS APPOINTED DIFFERENCE OF ValuesPointer AND 1
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces a runtime error when pointer arithmetic is attempted on a pointer to a single scalar variable.
    /// </summary>
    [Fact]
    public void Execute_PointerArithmetic_OnScalarPointer_ProducesRuntimeError()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Number AS A PEER BEING 42
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Number
            NumberPointer IS APPOINTED SUM OF NumberPointer AND 1
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method re-points a pointer variable at a new target when reassigned with a new address-of expression.
    /// </summary>
    [Fact]
    public void Execute_PointerReassignment_PointsAtNewTarget()
    {
        string source = """
            HARK! "Pointers"
            PRAY WELCOME Number AS A PEER BEING 42
            PRAY WELCOME Number2 AS A PEER BEING 78
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Number
            NumberPointer IS APPOINTED GALLERY PICTURE TO Number2
            BEHOLD VIEW FROM NumberPointer
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("78");
    }
}
