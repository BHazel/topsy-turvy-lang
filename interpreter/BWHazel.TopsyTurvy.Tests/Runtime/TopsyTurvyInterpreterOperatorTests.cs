using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Operator tests for the <see cref="Interpreter"/> class.
/// </summary>
public class TopsyTurvyInterpreterOperatorTests : TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly computes the difference of two integers.
    /// </summary>
    [Fact]
    public void Execute_WithDifferenceOf_ComputesSubtraction()
    {
        string source = """
            HARK! "Subtraction"
            PRINCIPALS
              PRAY WELCOME result AS A PEER
            THE CURTAIN RISES.
            result IS APPOINTED DIFFERENCE OF 10 AND 3
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("7", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly computes integer division.
    /// </summary>
    [Fact]
    public void Execute_WithQuotientOfIntegers_ComputesIntegerDivision()
    {
        string source = """
            HARK! "Division"
            PRINCIPALS
              PRAY WELCOME result AS A PEER
            THE CURTAIN RISES.
            result IS APPOINTED QUOTIENT OF 10 AND 3
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("3", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns an error diagnostic when dividing an integer by zero.
    /// </summary>
    [Fact]
    public void Execute_WithIntegerDivisionByZero_ReturnsErrorDiagnostic()
    {
        string source = """
            HARK! "Division by Zero"
            PRINCIPALS
              PRAY WELCOME result AS A PEER
            THE CURTAIN RISES.
            result IS APPOINTED QUOTIENT OF 5 AND 0
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.True(diagnostics.HasErrors);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly computes the remainder of two integers.
    /// </summary>
    [Fact]
    public void Execute_WithRemainderOf_ComputesModulo()
    {
        string source = """
            HARK! "Remainder"
            PRINCIPALS
              PRAY WELCOME result AS A PEER
            THE CURTAIN RISES.
            result IS APPOINTED REMAINDER OF 10 AND 3
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("1", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns an error diagnostic when taking the remainder with a zero divisor.
    /// </summary>
    [Fact]
    public void Execute_WithRemainderByZero_ReturnsErrorDiagnostic()
    {
        string source = """
            HARK! "Remainder by Zero"
            PRINCIPALS
              PRAY WELCOME result AS A PEER
            THE CURTAIN RISES.
            result IS APPOINTED REMAINDER OF 5 AND 0
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.True(diagnostics.HasErrors);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly selects the larger of two values.
    /// </summary>
    [Fact]
    public void Execute_WithLargerOf_ReturnsMaximumValue()
    {
        string source = """
            HARK! "Larger Of"
            PRINCIPALS
              PRAY WELCOME result AS A PEER
            THE CURTAIN RISES.
            result IS APPOINTED LARGER OF 3 AND 7
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("7", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method correctly selects the smaller of two values.
    /// </summary>
    [Fact]
    public void Execute_WithSmallerOf_ReturnsMinimumValue()
    {
        string source = """
            HARK! "Smaller Of"
            PRINCIPALS
              PRAY WELCOME result AS A PEER
            THE CURTAIN RISES.
            result IS APPOINTED SMALLER OF 3 AND 7
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("3", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns true when two equal values are compared with ALIKE.
    /// </summary>
    [Fact]
    public void Execute_WithAlikeOnEqualValues_ReturnsVERITY()
    {
        string source = """
            HARK! "Alike"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED ALIKE 5 AND 5
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("VERITY", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns false when two unequal values are compared with ALIKE.
    /// </summary>
    [Fact]
    public void Execute_WithAlikeOnUnequalValues_ReturnsNAY()
    {
        string source = """
            HARK! "Alike Unequal"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED ALIKE 5 AND 6
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("NAY", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns true when two unequal values are compared with UNLIKE.
    /// </summary>
    [Fact]
    public void Execute_WithUnlikeOnUnequalValues_ReturnsVERITY()
    {
        string source = """
            HARK! "Unlike"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED UNLIKE 5 AND 6
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("VERITY", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method performs arithmetic on floats and returns a float result.
    /// </summary>
    [Fact]
    public void Execute_WithFloatArithmetic_ProducesFloatResult()
    {
        string source = """
            HARK! "Float Arithmetic"
            PRINCIPALS
              PRAY WELCOME result AS A FATHOM
            THE CURTAIN RISES.
            result IS APPOINTED QUOTIENT OF 1.0 AND 4.0
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(0.25.ToString(), output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method promotes an integer to float when mixed operands are used.
    /// </summary>
    [Fact]
    public void Execute_WithMixedIntegerAndFloat_PromotesToFloat()
    {
        string source = """
            HARK! "Mixed Arithmetic"
            PRINCIPALS
              PRAY WELCOME result AS A FATHOM
            THE CURTAIN RISES.
            result IS APPOINTED SUM OF 1 AND 0.5
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal(1.5.ToString(), output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method inverts a true boolean with HARDLY EVER.
    /// </summary>
    [Fact]
    public void Execute_WithHardlyEverOnTrue_ReturnsFalse()
    {
        string source = """
            HARK! "Hardly Ever"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED HARDLY EVER VERITY
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("NAY", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method short-circuits BOTH when the first operand is false.
    /// </summary>
    [Fact]
    public void Execute_WithBothAndFalseFirstOperand_ReturnsFalseWithoutEvaluatingSecond()
    {
        string source = """
            HARK! "Both Short-Circuit"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED BOTH NAY AND VERITY
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("NAY", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns true from BOTH when both operands are true.
    /// </summary>
    [Fact]
    public void Execute_WithBothAndTrueOperands_ReturnsTrue()
    {
        string source = """
            HARK! "Both True"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED BOTH VERITY AND VERITY
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("VERITY", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method short-circuits EITHER when the first operand is true.
    /// </summary>
    [Fact]
    public void Execute_WithEitherAndTrueFirstOperand_ReturnsTrueWithoutEvaluatingSecond()
    {
        string source = """
            HARK! "Either Short-Circuit"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED EITHER VERITY AND NAY
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("VERITY", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns false from EITHER when both operands are false.
    /// </summary>
    [Fact]
    public void Execute_WithEitherAndFalseOperands_ReturnsFalse()
    {
        string source = """
            HARK! "Either False"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED EITHER NAY AND NAY
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("NAY", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns false from ALL OF when any operand is false.
    /// </summary>
    [Fact]
    public void Execute_WithAllOfContainingFalseOperand_ReturnsFalse()
    {
        string source = """
            HARK! "All Of"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED ALL OF VERITY AND NAY AND VERITY IF YOU PLEASE.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("NAY", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns true from ALL OF when all operands are true.
    /// </summary>
    [Fact]
    public void Execute_WithAllOfAllTrueOperands_ReturnsTrue()
    {
        string source = """
            HARK! "All Of True"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED ALL OF VERITY AND VERITY AND VERITY IF YOU PLEASE.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("VERITY", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns true from ANY OF when at least one operand is true.
    /// </summary>
    [Fact]
    public void Execute_WithAnyOfContainingTrueOperand_ReturnsTrue()
    {
        string source = """
            HARK! "Any Of"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED ANY OF NAY AND VERITY AND NAY IF YOU PLEASE.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();

        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("VERITY", output[0]);
    }

    /// <summary>
    /// Tests that the <see cref="Interpreter.Execute"/> method returns false from ANY OF when all operands are false.
    /// </summary>
    [Fact]
    public void Execute_WithAnyOfAllFalseOperands_ReturnsFalse()
    {
        string source = """
            HARK! "Any Of False"
            PRINCIPALS
              PRAY WELCOME result AS A DECREE
            THE CURTAIN RISES.
            result IS APPOINTED ANY OF NAY AND NAY IF YOU PLEASE.
            BEHOLD result
            FINALE.
            """;

        ProgramNode program = this.parser.Parse(source);
        (Interpreter interpreter, List<string> output) = this.CreateInterpreter();
        
        DiagnosticCollection diagnostics = interpreter.Execute(program);

        Assert.False(diagnostics.HasErrors);
        Assert.Equal("NAY", output[0]);
    }
}
