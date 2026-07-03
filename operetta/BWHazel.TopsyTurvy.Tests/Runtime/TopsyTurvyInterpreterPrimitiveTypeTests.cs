using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Tests for primitive types in the <see cref="Interpreter"/> class, including character literals, numeric types,
/// the <c>STANDING</c> modifier, arithmetic widening, in-place casts and YARN string indexing.
/// </summary>
public class TopsyTurvyInterpreterPrimitiveTypeTests : TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// Tests that a <c>STITCH</c> variable declared with a character literal prints the bare character.
    /// </summary>
    [Fact]
    public void Execute_CharLiteral_PrintsBareCharacter()
    {
        string source = """
            HARK! "Char"
            PRAY WELCOME letter AS A STITCH BEING 'G'
            BEHOLD letter
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("G");
    }

    /// <summary>
    /// Tests that the <c>~n</c> escape sequence in a character literal prints a newline character.
    /// </summary>
    [Fact]
    public void Execute_CharLiteralNewlineEscape_PrintsNewline()
    {
        string source = """
            HARK! "Char escape"
            PRAY WELCOME nl AS A STITCH BEING '~n'
            BEHOLD nl
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("\n");
    }

    /// <summary>
    /// Tests that a <c>CHANCELLOR</c> variable prints its value correctly.
    /// </summary>
    [Fact]
    public void Execute_ChancellorVariable_PrintsValue()
    {
        string source = """
            HARK! "Chancellor"
            PRAY WELCOME big AS A CHANCELLOR BEING 2000000000
            BEHOLD big
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("2000000000");
    }

    /// <summary>
    /// Tests that arithmetic on two <c>PIRATE</c> variables returns a <c>PIRATE</c> result.
    /// </summary>
    [Fact]
    public void Execute_PirateArithmetic_ReturnsPirateResult()
    {
        string source = """
            HARK! "Pirate arithmetic"
            PRAY WELCOME first AS A PIRATE BEING 10
            PRAY WELCOME second AS A PIRATE BEING 3
            BEHOLD SUM OF first AND second
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("13");
    }

    /// <summary>
    /// Tests that a <c>FOOT</c> (single-precision float) variable prints its value.
    /// </summary>
    [Fact]
    public void Execute_FootVariable_PrintsValue()
    {
        string source = """
            HARK! "Foot"
            PRAY WELCOME f AS A FOOT BEING 1.5
            BEHOLD f
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("1.5");
    }

    /// <summary>
    /// Tests that a <c>STANDING PEER</c> variable prints its value.
    /// </summary>
    [Fact]
    public void Execute_StandingPeerVariable_PrintsValue()
    {
        string source = """
            HARK! "Standing Peer"
            PRAY WELCOME u AS A STANDING PEER BEING 100
            BEHOLD u
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("100");
    }

    /// <summary>
    /// Tests that a <c>STANDING CHANCELLOR</c> variable prints its value.
    /// </summary>
    [Fact]
    public void Execute_StandingChancellorVariable_PrintsValue()
    {
        string source = """
            HARK! "Standing Chancellor"
            PRAY WELCOME u AS A STANDING CHANCELLOR BEING 100
            BEHOLD u
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("100");
    }

    /// <summary>
    /// Tests that adding a <c>PIRATE</c> and a <c>CHANCELLOR</c> widens the result to <c>CHANCELLOR</c>.
    /// </summary>
    [Fact]
    public void Execute_MixedIntegerArithmetic_WidensToLargerType()
    {
        string source = """
            HARK! "Widening"
            PRAY WELCOME small AS A PIRATE BEING 10
            PRAY WELCOME big AS A CHANCELLOR BEING 20
            BEHOLD SUM OF small AND big
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("30");
    }

    /// <summary>
    /// Tests that adding a <c>PEER</c> and a <c>FATHOM</c> promotes the result to <c>FATHOM</c>.
    /// </summary>
    [Fact]
    public void Execute_IntegerPlusFathom_PromotesToFloat()
    {
        string source = """
            HARK! "Float promotion"
            PRAY WELCOME whole AS A PEER BEING 2
            PRAY WELCOME frac AS A FATHOM BEING 1.5
            BEHOLD SUM OF whole AND frac
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("3.5");
    }

    /// <summary>
    /// Tests that <c>VICTIM n ON yarn</c> returns the 1-based character at position <c>n</c> as a <c>STITCH</c>.
    /// </summary>
    [Fact]
    public void Execute_VictimOnYarn_ReturnsCharacterAtPosition()
    {
        string source = """
            HARK! "YARN index"
            PRAY WELCOME word AS A YARN BEING "Ko-Ko"
            BEHOLD VICTIM 1 ON word
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("K");
    }

    /// <summary>
    /// Tests that <c>VICTIM n ON yarn</c> returns the correct character at an interior position.
    /// </summary>
    [Fact]
    public void Execute_VictimOnYarnInteriorIndex_ReturnsCorrectCharacter()
    {
        string source = """
            HARK! "YARN index interior"
            PRAY WELCOME word AS A YARN BEING "Pooh-Bah"
            BEHOLD VICTIM 5 ON word
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("-");
    }

    /// <summary>
    /// Tests that <c>RECKONING OF yarn</c> returns the number of characters in the string.
    /// </summary>
    [Fact]
    public void Execute_ReckoningOfYarn_ReturnsStringLength()
    {
        string source = """
            HARK! "YARN length"
            PRAY WELCOME title AS A YARN BEING "Mikado"
            BEHOLD RECKONING OF title
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("6");
    }

    /// <summary>
    /// Tests that <c>VICTIM n ON yarn</c> where <c>n</c> equals the string length returns the last character.
    /// </summary>
    [Fact]
    public void Execute_VictimOnYarnAtLastIndex_ReturnsLastCharacter()
    {
        string source = """
            HARK! "YARN index last"
            PRAY WELCOME word AS A YARN BEING "Ko-Ko"
            BEHOLD VICTIM 5 ON word
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("o");
    }

    /// <summary>
    /// Tests that <c>VICTIM</c> on a <c>YARN</c> with an out-of-range index produces a runtime error.
    /// </summary>
    [Fact]
    public void Execute_VictimOnYarnOutOfRange_ProducesRuntimeError()
    {
        string source = """
            HARK! "YARN index OOB"
            PRAY WELCOME word AS A YARN BEING "Ko-Ko"
            BEHOLD VICTIM 10 ON word
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> _) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that <c>VICTIM 0 ON yarn</c> produces a runtime error because the index is 1-based.
    /// </summary>
    [Fact]
    public void Execute_VictimOnYarnZeroIndex_ProducesRuntimeError()
    {
        string source = """
            HARK! "YARN index zero"
            PRAY WELCOME word AS A YARN BEING "Ko-Ko"
            BEHOLD VICTIM 0 ON word
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> _) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that <c>VICTIM</c> on a <c>YARN</c> with a non-integer index produces a runtime error.
    /// </summary>
    [Fact]
    public void Execute_VictimOnYarnNonIntegerIndex_ProducesRuntimeError()
    {
        string source = """
            HARK! "YARN index non-integer"
            PRAY WELCOME word AS A YARN BEING "Ko-Ko"
            BEHOLD VICTIM 1.5 ON word
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> _) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that <c>RECKONING OF</c> an empty <c>YARN</c> returns <c>0</c>.
    /// </summary>
    [Fact]
    public void Execute_ReckoningOfEmptyYarn_ReturnsZero()
    {
        string source = """
            HARK! "YARN length empty"
            PRAY WELCOME word AS A YARN BEING ""
            BEHOLD RECKONING OF word
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        interpreter.Execute(program);

        output.ShouldHaveSingleItem().ShouldBe("0");
    }

    /// <summary>
    /// Tests that assignment to a character position within a <c>YARN</c> produces a runtime error.
    /// </summary>
    [Fact]
    public void Execute_VictimAssignmentOnYarn_ProducesRuntimeError()
    {
        string source = """
            HARK! "YARN assign"
            PRAY WELCOME word AS A YARN BEING "Ko-Ko"
            VICTIM 1 ON word IS APPOINTED 'X'
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> _) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        diagnostics.HasErrors.ShouldBeTrue();
    }
}
