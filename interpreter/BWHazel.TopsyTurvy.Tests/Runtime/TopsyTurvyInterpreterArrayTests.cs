using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Array operation tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterArrayTests : TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method populates the list and prints the elements when an array declaration includes initial values.
    /// </summary>
    [Fact]
    public void Execute_ArrayDeclaration_WithInitialValues_PopulatesList()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME miscreants AS A LITTLE LIST OF YARN BEING "Pooh-Bah" AND "Ko-Ko" IF YOU PLEASE.
            BEHOLD miscreants
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output.ShouldHaveSingleItem();
        output[0].ShouldBe("[Pooh-Bah, Ko-Ko]");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method creates an empty array when an array declaration has no BEING clause.
    /// </summary>
    [Fact]
    public void Execute_ArrayDeclaration_WithoutBeingClause_CreatesEmptyArray()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME scores AS A LITTLE LIST OF PEER
            BEHOLD scores
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("[]");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns the correct element at the given 1-based index for a VICTIM n ON array expression.
    /// </summary>
    [Fact]
    public void Execute_ArrayIndex_ValidIndex_ReturnsCorrectElement()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME lords AS A LITTLE LIST OF YARN BEING "Mountararat" AND "Tolloller" IF YOU PLEASE.
            BEHOLD VICTIM 2 ON lords
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("Tolloller");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces a runtime error when a VICTIM expression references an out-of-range index.
    /// </summary>
    [Fact]
    public void Execute_ArrayIndex_OutOfRange_ProducesRuntimeError()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME tiny AS A LITTLE LIST OF PEER BEING 1 IF YOU PLEASE.
            BEHOLD VICTIM 5 ON tiny
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method updates the array element at the given index for a VICTIM...IS APPOINTED statement.
    /// </summary>
    [Fact]
    public void Execute_ArrayAssignment_ValidIndex_UpdatesElement()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME peers AS A LITTLE LIST OF YARN BEING "Strephon" AND "Phyllis" IF YOU PLEASE.
            VICTIM 1 ON peers IS APPOINTED "Iolanthe"
            BEHOLD VICTIM 1 ON peers
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("Iolanthe");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces a runtime error when assigning to an element of a CONSERVATIVE array.
    /// </summary>
    [Fact]
    public void Execute_ArrayAssignment_ConservativeArray_ProducesRuntimeError()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME fixed AS A CONSERVATIVE LITTLE LIST OF PEER BEING 1 AND 2 IF YOU PLEASE.
            VICTIM 1 ON fixed IS APPOINTED 99
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method evaluates an empty array as falsy and a non-empty array as truthy.
    /// </summary>
    [Fact]
    public void Execute_ArrayTruthiness_EmptyArrayIsFalsy_NonEmptyArrayIsTruthy()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME empty AS A LITTLE LIST OF PEER
            PRAY WELCOME full AS A LITTLE LIST OF PEER BEING 1 IF YOU PLEASE.
            SHOULD IT TRANSPIRE THAT empty
            QUITE SO.
              BEHOLD "empty is truthy"
            OTHERWISE,
              BEHOLD "empty is falsy"
            SO MUCH FOR THAT.
            SHOULD IT TRANSPIRE THAT full
            QUITE SO.
              BEHOLD "full is truthy"
            OTHERWISE,
              BEHOLD "full is falsy"
            SO MUCH FOR THAT.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output.Count.ShouldBe(2);
        output[0].ShouldBe("empty is falsy");
        output[1].ShouldBe("full is truthy");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method pre-allocates the correct number of NAUGHT elements for a sized array declaration.
    /// </summary>
    [Fact]
    public void Execute_ArrayDeclaration_WithSize_PreAllocatesNaughtElements()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME arr AS A LITTLE LIST OF 3 YARN
            BEHOLD VICTIM 1 ON arr
            BEHOLD VICTIM 2 ON arr
            BEHOLD VICTIM 3 ON arr
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output.Count.ShouldBe(3);
        output[0].ShouldBe("NAUGHT");
        output[1].ShouldBe("NAUGHT");
        output[2].ShouldBe("NAUGHT");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method supports element assignment on a sized array without a BEING clause.
    /// </summary>
    [Fact]
    public void Execute_ArrayDeclaration_WithSize_ElementAssignmentWorks()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME miscreants AS A LITTLE LIST OF 3 YARN
            VICTIM 1 ON miscreants IS APPOINTED "Miscreant1"
            VICTIM 2 ON miscreants IS APPOINTED "Miscreant2"
            VICTIM 3 ON miscreants IS APPOINTED "Miscreant3"
            BEHOLD VICTIM 1 ON miscreants
            BEHOLD VICTIM 2 ON miscreants
            BEHOLD VICTIM 3 ON miscreants
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output.Count.ShouldBe(3);
        output[0].ShouldBe("Miscreant1");
        output[1].ShouldBe("Miscreant2");
        output[2].ShouldBe("Miscreant3");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces a runtime error when both a size and a BEING clause are provided on the same array declaration.
    /// </summary>
    [Fact]
    public void Execute_ArrayDeclaration_WithSizeAndBeingClause_ProducesRuntimeError()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME arr AS A LITTLE LIST OF 3 PEER BEING 1 AND 2 AND 3 IF YOU PLEASE.
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> _) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces a runtime error when a negative size is used in an array declaration.
    /// </summary>
    [Fact]
    public void Execute_ArrayDeclaration_WithNegativeSize_ProducesRuntimeError()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME arr AS A LITTLE LIST OF -3 YARN
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> _) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method implements reference semantics, so that two variables pointing to the same array share the same underlying storage.
    /// </summary>
    [Fact]
    public void Execute_ArrayAssignment_ReferenceSemantics_BothVariablesReflectChange()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME original AS A LITTLE LIST OF PEER BEING 10 AND 20 IF YOU PLEASE.
            PRAY WELCOME alias AS A LITTLE LIST OF PEER
            alias IS APPOINTED original
            VICTIM 1 ON alias IS APPOINTED 99
            BEHOLD VICTIM 1 ON original
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("99");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method copies the element value when assigning an array element to a scalar variable, so that subsequent element mutations do not affect the variable.
    /// </summary>
    [Fact]
    public void Execute_ArrayIndex_ScalarAssignment_CopiesValueNotReference()
    {
        string source = """
            HARK! "Arrays"
            PRAY WELCOME arr AS A LITTLE LIST OF PEER BEING 10 AND 20 IF YOU PLEASE.
            PRAY WELCOME x AS A PEER
            x IS APPOINTED VICTIM 1 ON arr
            VICTIM 1 ON arr IS APPOINTED 99
            BEHOLD x
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("10");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method declares THE PROPS as an empty array when no programme arguments are provided.
    /// </summary>
    [Fact]
    public void Execute_TheProps_WithNoArguments_IsEmptyArray()
    {
        string source = """
            HARK! "THE PROPS"
            BEHOLD THE PROPS
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("[]");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method populates THE PROPS with the provided programme arguments as YARN elements.
    /// </summary>
    [Fact]
    public void Execute_TheProps_WithArguments_ContainsPassedStrings()
    {
        string source = """
            HARK! "THE PROPS"
            BEHOLD THE PROPS
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(
            program,
            options: new(null, null, null, ["Ko-Ko", "Pooh-Bah"]));

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("[Ko-Ko, Pooh-Bah]");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns the correct 1-based element from THE PROPS.
    /// </summary>
    [Fact]
    public void Execute_TheProps_ElementAccess_ReturnsCorrect1BasedElement()
    {
        string source = """
            HARK! "THE PROPS"
            BEHOLD VICTIM 2 ON THE PROPS
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(
            program,
            options: new(null, null, null, ["Ko-Ko", "Pooh-Bah"]));

        diagnostics.HasErrors.ShouldBeFalse();
        output[0].ShouldBe("Pooh-Bah");
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method produces a runtime error when element reassignment is attempted on THE PROPS.
    /// </summary>
    [Fact]
    public void Execute_TheProps_ElementReassignment_ProducesRuntimeError()
    {
        string source = """
            HARK! "THE PROPS"
            VICTIM 1 ON THE PROPS IS APPOINTED "changed"
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> _) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(
            program,
            options: new(null, null, null, ["Ko-Ko"]));

        diagnostics.HasErrors.ShouldBeTrue();
    }
}
