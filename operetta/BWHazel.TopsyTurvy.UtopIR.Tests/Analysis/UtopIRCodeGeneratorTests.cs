using System.Collections.Generic;
using BWHazel.TopsyTurvy.UtopIR.Analysis;
using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.UtopIR.Tests.Analysis;

/// <summary>
/// Tests for the <see cref="UtopIRCodeGenerator"/> class.
/// </summary>
public class UtopIRCodeGeneratorTests
{
    private readonly UtopIRCodeGenerator generator = new();

    /// <summary>
    /// Tests that each <see cref="UtopIRType"/> value produces the correct keyword in the emitted <c>welcome</c> instruction.
    /// </summary>
    /// <param name="type">The <see cref="UtopIRType"/> to test.</param>
    /// <param name="expectedKeyword">The expected keyword in the emitted instruction.</param>
    [Theory]
    [InlineData(UtopIRType.Chancellor, "chancellor")]
    [InlineData(UtopIRType.Peer, "peer")]
    [InlineData(UtopIRType.Pirate, "pirate")]
    [InlineData(UtopIRType.SausageRoll, "sausageroll")]
    [InlineData(UtopIRType.StandingChancellor, "standingchancellor")]
    [InlineData(UtopIRType.StandingPeer, "standingpeer")]
    [InlineData(UtopIRType.StandingPirate, "standingpirate")]
    [InlineData(UtopIRType.StandingSausageRoll, "standingsausageroll")]
    [InlineData(UtopIRType.Fathom, "fathom")]
    [InlineData(UtopIRType.Foot, "foot")]
    [InlineData(UtopIRType.Decree, "decree")]
    [InlineData(UtopIRType.Stitch, "stitch")]
    [InlineData(UtopIRType.Yarn, "yarn")]
    public void Generate_WelcomeInstruction_EmitsCorrectTypeKeyword(UtopIRType type, string expectedKeyword)
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new UtopIRVariable("x"), type)
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe($"£x = welcome {expectedKeyword}");
    }

    /// <summary>
    /// Tests that the variable name is prefixed with <c>£</c> in the emitted instruction.
    /// </summary>
    [Fact]
    public void Generate_WelcomeInstruction_PrefixesVariableNameWithPound()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new UtopIRVariable("LovesickMaidens"), UtopIRType.Peer)
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£LovesickMaidens = welcome peer");
    }

    /// <summary>
    /// Tests that a <see cref="VariableOperand"/> in an <c>appoint</c> instruction is rendered with the <c>£</c> prefix.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithVariableOperand_EmitsCorrectLine()
    {
        UtopIRProgram program = new([
            new AppointInstruction(
                new UtopIRVariable("y"),
                new VariableOperand(new UtopIRVariable("x")))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£y = appoint £x");
    }

    /// <summary>
    /// Tests that an integer <see cref="LiteralOperand"/> is emitted as a bare integer token.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithIntegerLiteral_EmitsCorrectLine()
    {
        UtopIRProgram program = new([
            new AppointInstruction(
                new UtopIRVariable("n"),
                new LiteralOperand(42))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£n = appoint 42");
    }

    /// <summary>
    /// Tests that boolean <see cref="LiteralOperand"/> values are emitted as <c>verity</c> or <c>nay</c> tokens,
    /// matching the UtopIR <c>decree</c> literal convention.
    /// </summary>
    /// <param name="value">The boolean value to test.</param>
    /// <param name="expectedToken">The expected token in the emitted instruction.</param>
    [Theory]
    [InlineData(true, "verity")]
    [InlineData(false, "nay")]
    public void Generate_AppointInstruction_WithBooleanLiteral_EmitsLowercaseKeyword(bool value, string expectedToken)
    {
        UtopIRProgram program = new([
            new AppointInstruction(
                new UtopIRVariable("flag"),
                new LiteralOperand(value))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe($"£flag = appoint {expectedToken}");
    }

    /// <summary>
    /// Tests that a whole-number <c>float</c> literal is emitted with a <c>.0</c> suffix to distinguish it from an integer token.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithWholeNumberFloat_AppendsDotZero()
    {
        UtopIRProgram program = new([
            new AppointInstruction(
                new UtopIRVariable("f"),
                new LiteralOperand(3.0f))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£f = appoint 3.0");
    }

    /// <summary>
    /// Tests that a whole-number <c>double</c> literal is emitted with a <c>.0</c> suffix.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithWholeNumberDouble_AppendsDotZero()
    {
        UtopIRProgram program = new([
            new AppointInstruction(
                new UtopIRVariable("d"),
                new LiteralOperand(5.0))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£d = appoint 5.0");
    }

    /// <summary>
    /// Tests that a fractional <c>double</c> literal preserves its decimal point without appending extra digits.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithFractionalDouble_PreservesDecimalPoint()
    {
        UtopIRProgram program = new([
            new AppointInstruction(
                new UtopIRVariable("d"),
                new LiteralOperand(3.14))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£d = appoint 3.14");
    }

    /// <summary>
    /// Tests that a plain string literal is emitted enclosed in double quotes.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithStringLiteral_EmitsQuotedString()
    {
        UtopIRProgram program = new([
            new AppointInstruction(
                new UtopIRVariable("s"),
                new LiteralOperand("hello"))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£s = appoint \"hello\"");
    }

    /// <summary>
    /// Tests that double-quote characters inside a string literal are escaped as <c>~"</c>.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithStringContainingDoubleQuote_EscapesIt()
    {
        UtopIRProgram program = new([
            new AppointInstruction(
                new UtopIRVariable("s"),
                new LiteralOperand("say \"hello\""))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£s = appoint \"say ~\"hello~\"\"");
    }

    /// <summary>
    /// Tests that a tilde character inside a string literal is escaped as <c>~~</c>.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithStringContainingTilde_EscapesIt()
    {
        UtopIRProgram program = new([
            new AppointInstruction(
                new UtopIRVariable("s"),
                new LiteralOperand("a~b"))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£s = appoint \"a~~b\"");
    }

    /// <summary>
    /// Tests that a character literal is emitted enclosed in single quotes.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithCharLiteral_EmitsSingleQuoted()
    {
        UtopIRProgram program = new([
            new AppointInstruction(
                new UtopIRVariable("c"),
                new LiteralOperand('A'))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£c = appoint 'A'");
    }

    /// <summary>
    /// Tests that each <see cref="UtopIRArithmeticOperation"/> value produces the correct mnemonic in the emitted arithmetic instruction.
    /// </summary>
    /// <param name="operation">The <see cref="UtopIRArithmeticOperation"/> to test.</param>
    /// <param name="expectedMnemonic">The expected mnemonic in the emitted instruction.</param>
    [Theory]
    [InlineData(UtopIRArithmeticOperation.Sum, "sum")]
    [InlineData(UtopIRArithmeticOperation.Diff, "diff")]
    [InlineData(UtopIRArithmeticOperation.Prod, "prod")]
    [InlineData(UtopIRArithmeticOperation.Quot, "quot")]
    [InlineData(UtopIRArithmeticOperation.Rem, "rem")]
    [InlineData(UtopIRArithmeticOperation.Max, "max")]
    [InlineData(UtopIRArithmeticOperation.Min, "min")]
    [InlineData(UtopIRArithmeticOperation.SumFloat, "sum.f")]
    [InlineData(UtopIRArithmeticOperation.DiffFloat, "diff.f")]
    [InlineData(UtopIRArithmeticOperation.ProdFloat, "prod.f")]
    [InlineData(UtopIRArithmeticOperation.QuotFloat, "quot.f")]
    [InlineData(UtopIRArithmeticOperation.RemFloat, "rem.f")]
    [InlineData(UtopIRArithmeticOperation.MaxFloat, "max.f")]
    [InlineData(UtopIRArithmeticOperation.MinFloat, "min.f")]
    public void Generate_ArithmeticInstruction_EmitsCorrectMnemonic(UtopIRArithmeticOperation operation, string expectedMnemonic)
    {
        UtopIRProgram program = new([
            new ArithmeticInstruction(
                operation,
                new UtopIRVariable("result"),
                new VariableOperand(new UtopIRVariable("a")),
                new VariableOperand(new UtopIRVariable("b")))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe($"£result = {expectedMnemonic} £a, £b");
    }

    /// <summary>
    /// Tests that an arithmetic instruction with literal operands is formatted correctly, with operands separated by a comma and space.
    /// </summary>
    [Fact]
    public void Generate_ArithmeticInstruction_WithLiteralOperands_EmitsCorrectLine()
    {
        UtopIRProgram program = new([
            new ArithmeticInstruction(
                UtopIRArithmeticOperation.Sum,
                new UtopIRVariable("total"),
                new LiteralOperand(10),
                new LiteralOperand(20))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£total = sum 10, 20");
    }

    /// <summary>
    /// Tests that a floating-point arithmetic instruction with float literal operands is formatted with the <c>.f</c> mnemonic and decimal operands.
    /// </summary>
    [Fact]
    public void Generate_ArithmeticInstruction_WithFloatLiteralOperands_EmitsCorrectLine()
    {
        UtopIRProgram program = new([
            new ArithmeticInstruction(
                UtopIRArithmeticOperation.SumFloat,
                new UtopIRVariable("total"),
                new LiteralOperand(1.5),
                new LiteralOperand(2.5))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£total = sum.f 1.5, 2.5");
    }

    /// <summary>
    /// Tests that each <see cref="UtopIRBitwiseOperation"/> value produces the correct mnemonic in the emitted bitwise instruction.
    /// </summary>
    /// <param name="operation">The <see cref="UtopIRBitwiseOperation"/> to test.</param>
    /// <param name="expectedMnemonic">The expected mnemonic in the emitted instruction.</param>
    [Theory]
    [InlineData(UtopIRBitwiseOperation.Chord, "chord")]
    [InlineData(UtopIRBitwiseOperation.Harmony, "harmony")]
    [InlineData(UtopIRBitwiseOperation.Discord, "discord")]
    [InlineData(UtopIRBitwiseOperation.TransUp, "transup")]
    [InlineData(UtopIRBitwiseOperation.TransDown, "transdown")]
    public void Generate_BitwiseInstruction_EmitsCorrectMnemonic(UtopIRBitwiseOperation operation, string expectedMnemonic)
    {
        UtopIRProgram program = new([
            new BitwiseInstruction(
                operation,
                new UtopIRVariable("result"),
                new VariableOperand(new UtopIRVariable("a")),
                new VariableOperand(new UtopIRVariable("b")))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe($"£result = {expectedMnemonic} £a, £b");
    }

    /// <summary>
    /// Tests that an <c>inv</c> instruction is emitted with its single operand.
    /// </summary>
    [Fact]
    public void Generate_InvInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([
            new InvInstruction(
                new UtopIRVariable("result"),
                new VariableOperand(new UtopIRVariable("a")))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£result = inv £a");
    }

    /// <summary>
    /// Tests that a <c>were</c> instruction is emitted with its value operand and destination type.
    /// </summary>
    [Fact]
    public void Generate_WereInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([
            new WereInstruction(
                new UtopIRVariable("Lords"),
                new VariableOperand(new UtopIRVariable("LovesickMaidens")),
                UtopIRType.Chancellor)
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£Lords = were £LovesickMaidens, chancellor");
    }

    /// <summary>
    /// Tests that a <c>were</c> instruction with a literal operand is emitted correctly.
    /// </summary>
    [Fact]
    public void Generate_WereInstruction_WithLiteralOperand_EmitsCorrectLine()
    {
        UtopIRProgram program = new([
            new WereInstruction(new UtopIRVariable("x"), new LiteralOperand(10), UtopIRType.Chancellor)
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£x = were 10, chancellor");
    }

    /// <summary>
    /// Tests that a <c>prentice</c> instruction with a variable operand is emitted correctly.
    /// </summary>
    [Fact]
    public void Generate_PrenticeInstruction_WithVariableOperand_EmitsCorrectLine()
    {
        UtopIRProgram program = new([
            new PrenticeInstruction(new VariableOperand(new UtopIRVariable("x")))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("prentice £x");
    }

    /// <summary>
    /// Tests that a <c>prentice</c> instruction with a literal operand is emitted correctly.
    /// </summary>
    [Fact]
    public void Generate_PrenticeInstruction_WithLiteralOperand_EmitsCorrectLine()
    {
        UtopIRProgram program = new([
            new PrenticeInstruction(new LiteralOperand(99))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("prentice 99");
    }

    /// <summary>
    /// Tests that a <c>leave</c> instruction is emitted with the correct assignment target.
    /// </summary>
    [Fact]
    public void Generate_LeaveInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([
            new LeaveInstruction(new UtopIRVariable("result"))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("£result = leave");
    }

    /// <summary>
    /// Tests that a <c>find</c> instruction with a variable operand is emitted correctly.
    /// </summary>
    [Fact]
    public void Generate_FindInstruction_WithVariableOperand_EmitsCorrectLine()
    {
        UtopIRProgram program = new([
            new FindInstruction(new VariableOperand(new UtopIRVariable("exitCode")))
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("find £exitCode");
    }

    /// <summary>
    /// Tests that a <c>find</c> instruction with no operand emits the keyword alone, representing a void return.
    /// </summary>
    [Fact]
    public void Generate_FindInstruction_WithNoOperand_EmitsFindKeywordOnly()
    {
        UtopIRProgram program = new([
            new FindInstruction(null)
        ]);

        string result = this.generator.Generate(program);

        result.Trim().ShouldBe("find");
    }

    /// <summary>
    /// Tests that a complete programme with all instruction types is emitted in order, one instruction per line.
    /// </summary>
    [Fact]
    public void Generate_MultipleInstructions_EmitsEachOnSeparateLine()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new UtopIRVariable("a"), UtopIRType.Peer),
            new AppointInstruction(new UtopIRVariable("a"), new LiteralOperand(10)),
            new WelcomeInstruction(new UtopIRVariable("b"), UtopIRType.Peer),
            new AppointInstruction(new UtopIRVariable("b"), new LiteralOperand(20)),
            new ArithmeticInstruction(
                UtopIRArithmeticOperation.Sum,
                new UtopIRVariable("_sum_a_b"),
                new VariableOperand(new UtopIRVariable("a")),
                new VariableOperand(new UtopIRVariable("b"))),
            new FindInstruction(new VariableOperand(new UtopIRVariable("_sum_a_b")))
        ]);

        string result = this.generator.Generate(program);

        string[] lines = result.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(6);
        lines[0].Trim().ShouldBe("£a = welcome peer");
        lines[1].Trim().ShouldBe("£a = appoint 10");
        lines[2].Trim().ShouldBe("£b = welcome peer");
        lines[3].Trim().ShouldBe("£b = appoint 20");
        lines[4].Trim().ShouldBe("£_sum_a_b = sum £a, £b");
        lines[5].Trim().ShouldBe("find £_sum_a_b");
    }
    
    /// <summary>
    /// Tests that an empty programme produces an empty string with no extraneous whitespace.
    /// </summary>
    [Fact]
    public void Generate_EmptyProgramme_ReturnsEmptyString()
    {
        UtopIRProgram program = new(new List<UtopIRInstruction>());

        string result = this.generator.Generate(program);

        result.ShouldBe(string.Empty);
    }
}
