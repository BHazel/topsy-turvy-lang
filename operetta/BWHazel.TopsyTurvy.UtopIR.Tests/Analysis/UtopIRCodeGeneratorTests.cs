using System.Collections.Generic;
using BWHazel.TopsyTurvy.UtopIR.Analysis;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Parser;
using Superpower.Model;

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
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new WelcomeInstruction(new UtopIRVariable("x"), type)
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe($"£x = welcome {expectedKeyword}");
    }

    /// <summary>
    /// Tests that the variable name is prefixed with <c>£</c> in the emitted instruction.
    /// </summary>
    [Fact]
    public void Generate_WelcomeInstruction_PrefixesVariableNameWithPound()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new WelcomeInstruction(new UtopIRVariable("LovesickMaidens"), UtopIRType.Peer)
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£LovesickMaidens = welcome peer");
    }

    /// <summary>
    /// Tests that a <see cref="VariableOperand"/> in an <c>appoint</c> instruction is rendered with the <c>£</c> prefix.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithVariableOperand_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointInstruction(
                new UtopIRVariable("y"),
                new VariableOperand(new UtopIRVariable("x")))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£y = appoint £x");
    }

    /// <summary>
    /// Tests that an integer <see cref="LiteralOperand"/> is emitted as a bare integer token.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithIntegerLiteral_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointInstruction(
                new UtopIRVariable("n"),
                new LiteralOperand(42))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£n = appoint 42");
    }

    /// <summary>
    /// Tests that the <see cref="NaughtLiteral"/> singleton is emitted as the <c>naught</c> keyword.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithNaughtLiteral_EmitsNaughtKeyword()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointInstruction(
                new UtopIRVariable("NumberPointer"),
                new LiteralOperand(NaughtLiteral.Instance))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£NumberPointer = appoint naught");
    }

    /// <summary>
    /// Tests that a generated <c>naught</c> appoint re-parses to an equal <see cref="AppointInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithNaughtLiteral_RoundTripsThroughParser()
    {
        AppointInstruction original = new(new UtopIRVariable("NumberPointer"), new LiteralOperand(NaughtLiteral.Instance));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
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
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointInstruction(
                new UtopIRVariable("flag"),
                new LiteralOperand(value))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe($"£flag = appoint {expectedToken}");
    }

    /// <summary>
    /// Tests that a whole-number <c>float</c> literal is emitted with a <c>.0</c> suffix to distinguish it from an integer token.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithWholeNumberFloat_AppendsDotZero()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointInstruction(
                new UtopIRVariable("f"),
                new LiteralOperand(3.0f))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£f = appoint 3.0");
    }

    /// <summary>
    /// Tests that a whole-number <c>double</c> literal is emitted with a <c>.0</c> suffix.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithWholeNumberDouble_AppendsDotZero()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointInstruction(
                new UtopIRVariable("d"),
                new LiteralOperand(5.0))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£d = appoint 5.0");
    }

    /// <summary>
    /// Tests that a fractional <c>double</c> literal preserves its decimal point without appending extra digits.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithFractionalDouble_PreservesDecimalPoint()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointInstruction(
                new UtopIRVariable("d"),
                new LiteralOperand(3.14))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£d = appoint 3.14");
    }

    /// <summary>
    /// Tests that a plain string literal is emitted enclosed in double quotes.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithStringLiteral_EmitsQuotedString()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointInstruction(
                new UtopIRVariable("s"),
                new LiteralOperand("hello"))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£s = appoint \"hello\"");
    }

    /// <summary>
    /// Tests that double-quote characters inside a string literal are escaped as <c>~"</c>.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithStringContainingDoubleQuote_EscapesIt()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointInstruction(
                new UtopIRVariable("s"),
                new LiteralOperand("say \"hello\""))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£s = appoint \"say ~\"hello~\"\"");
    }

    /// <summary>
    /// Tests that a tilde character inside a string literal is escaped as <c>~~</c>.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithStringContainingTilde_EscapesIt()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointInstruction(
                new UtopIRVariable("s"),
                new LiteralOperand("a~b"))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£s = appoint \"a~~b\"");
    }

    /// <summary>
    /// Tests that a character literal is emitted enclosed in single quotes.
    /// </summary>
    [Fact]
    public void Generate_AppointInstruction_WithCharLiteral_EmitsSingleQuoted()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointInstruction(
                new UtopIRVariable("c"),
                new LiteralOperand('A'))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

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
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new ArithmeticInstruction(
                operation,
                new UtopIRVariable("result"),
                new VariableOperand(new UtopIRVariable("a")),
                new VariableOperand(new UtopIRVariable("b")))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe($"£result = {expectedMnemonic} £a, £b");
    }

    /// <summary>
    /// Tests that an arithmetic instruction with literal operands is formatted correctly, with operands separated by a comma and space.
    /// </summary>
    [Fact]
    public void Generate_ArithmeticInstruction_WithLiteralOperands_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new ArithmeticInstruction(
                UtopIRArithmeticOperation.Sum,
                new UtopIRVariable("total"),
                new LiteralOperand(10),
                new LiteralOperand(20))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£total = sum 10, 20");
    }

    /// <summary>
    /// Tests that a floating-point arithmetic instruction with float literal operands is formatted with the <c>.f</c> mnemonic and decimal operands.
    /// </summary>
    [Fact]
    public void Generate_ArithmeticInstruction_WithFloatLiteralOperands_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new ArithmeticInstruction(
                UtopIRArithmeticOperation.SumFloat,
                new UtopIRVariable("total"),
                new LiteralOperand(1.5),
                new LiteralOperand(2.5))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

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
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new BitwiseInstruction(
                operation,
                new UtopIRVariable("result"),
                new VariableOperand(new UtopIRVariable("a")),
                new VariableOperand(new UtopIRVariable("b")))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe($"£result = {expectedMnemonic} £a, £b");
    }

    /// <summary>
    /// Tests that an <c>inv</c> instruction is emitted with its single operand.
    /// </summary>
    [Fact]
    public void Generate_InvInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new InvInstruction(
                new UtopIRVariable("result"),
                new VariableOperand(new UtopIRVariable("a")))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£result = inv £a");
    }

    /// <summary>
    /// Tests that each <see cref="UtopIRComparisonOperation"/> value produces the correct mnemonic in the emitted comparison instruction.
    /// </summary>
    /// <param name="operation">The <see cref="UtopIRComparisonOperation"/> to test.</param>
    /// <param name="expectedMnemonic">The expected mnemonic in the emitted instruction.</param>
    [Theory]
    [InlineData(UtopIRComparisonOperation.Alike, "alike")]
    [InlineData(UtopIRComparisonOperation.Unlike, "unlike")]
    [InlineData(UtopIRComparisonOperation.PreAdam, "preadam")]
    [InlineData(UtopIRComparisonOperation.LowerDeg, "lowerdeg")]
    [InlineData(UtopIRComparisonOperation.AlikeFloat, "alike.f")]
    [InlineData(UtopIRComparisonOperation.UnlikeFloat, "unlike.f")]
    [InlineData(UtopIRComparisonOperation.PreAdamFloat, "preadam.f")]
    [InlineData(UtopIRComparisonOperation.LowerDegFloat, "lowerdeg.f")]
    public void Generate_ComparisonInstruction_EmitsCorrectMnemonic(UtopIRComparisonOperation operation, string expectedMnemonic)
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new ComparisonInstruction(
                operation,
                new UtopIRVariable("result"),
                new VariableOperand(new UtopIRVariable("a")),
                new VariableOperand(new UtopIRVariable("b")))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe($"£result = {expectedMnemonic} £a, £b");
    }

    /// <summary>
    /// Tests that each <see cref="UtopIRLogicalOperation"/> value produces the correct mnemonic in the emitted logical instruction.
    /// </summary>
    /// <param name="operation">The <see cref="UtopIRLogicalOperation"/> to test.</param>
    /// <param name="expectedMnemonic">The expected mnemonic in the emitted instruction.</param>
    [Theory]
    [InlineData(UtopIRLogicalOperation.Both, "both")]
    [InlineData(UtopIRLogicalOperation.Either, "either")]
    public void Generate_LogicalInstruction_EmitsCorrectMnemonic(UtopIRLogicalOperation operation, string expectedMnemonic)
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new LogicalInstruction(
                operation,
                new UtopIRVariable("result"),
                new LiteralOperand(true),
                new LiteralOperand(false))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe($"£result = {expectedMnemonic} verity, nay");
    }

    /// <summary>
    /// Tests that a <c>hardly</c> instruction is emitted with its single operand.
    /// </summary>
    [Fact]
    public void Generate_HardlyInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new HardlyInstruction(
                new UtopIRVariable("result"),
                new VariableOperand(new UtopIRVariable("a")))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£result = hardly £a");
    }

    /// <summary>
    /// Tests that a <c>victim.yarn</c> instruction is emitted with its yarn and index operands.
    /// </summary>
    [Fact]
    public void Generate_VictimYarnInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new VictimYarnInstruction(
                new UtopIRVariable("PoemSubjectLetter4"),
                new VariableOperand(new UtopIRVariable("PoemSubject")),
                new LiteralOperand(4))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£PoemSubjectLetter4 = victim.yarn £PoemSubject, 4");
    }

    /// <summary>
    /// Tests that a <c>welcome.list</c> instruction is emitted with its element type and size.
    /// </summary>
    [Fact]
    public void Generate_WelcomeListInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new WelcomeListInstruction(new UtopIRVariable("Numbers"), UtopIRType.Peer, new LiteralOperand(3))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£Numbers = welcome.list peer, 3");
    }

    /// <summary>
    /// Tests that an <c>appoint.victim</c> instruction is emitted with its array, index and value operands.
    /// </summary>
    [Fact]
    public void Generate_AppointVictimInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new AppointVictimInstruction(new UtopIRVariable("Numbers"), new LiteralOperand(1), new LiteralOperand(10))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("appoint.victim £Numbers, 1, 10");
    }

    /// <summary>
    /// Tests that a <c>victim.list</c> instruction is emitted with its array and index operands.
    /// </summary>
    [Fact]
    public void Generate_VictimListInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new VictimListInstruction(new UtopIRVariable("NumbersElement2"), new UtopIRVariable("Numbers"), new LiteralOperand(2))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£NumbersElement2 = victim.list £Numbers, 2");
    }

    /// <summary>
    /// Tests that a <c>welcome.gallerypic</c> instruction is emitted with its pointee type.
    /// </summary>
    [Fact]
    public void Generate_WelcomeGallerypicInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new WelcomeGallerypicInstruction(new UtopIRVariable("NumberPointer"), UtopIRType.Peer)
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£NumberPointer = welcome.gallerypic peer");
    }

    /// <summary>
    /// Tests that a <c>pictureto</c> instruction is emitted with its pointee variable.
    /// </summary>
    [Fact]
    public void Generate_PicturetoInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new PicturetoInstruction(new UtopIRVariable("NumberPointer"), new UtopIRVariable("Number"))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£NumberPointer = pictureto £Number");
    }

    /// <summary>
    /// Tests that a <c>viewfrom</c> instruction is emitted with its pointer.
    /// </summary>
    [Fact]
    public void Generate_ViewfromInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new ViewfromInstruction(new UtopIRVariable("NumberValue"), new UtopIRVariable("NumberPointer"))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£NumberValue = viewfrom £NumberPointer");
    }

    /// <summary>
    /// Tests that a <c>viewto</c> instruction is emitted with its pointer and value operand.
    /// </summary>
    [Fact]
    public void Generate_ViewtoInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new ViewtoInstruction(new UtopIRVariable("NumberPointer"), new LiteralOperand(23))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("viewto £NumberPointer, 23");
    }

    /// <summary>
    /// Tests that each pointer arithmetic mnemonic is emitted with its pointer and offset operands.
    /// </summary>
    /// <param name="operation">The pointer arithmetic operation.</param>
    /// <param name="expectedMnemonic">The expected UtopIR mnemonic.</param>
    [Theory]
    [InlineData(UtopIRPointerArithmeticOperation.Sum, "sum.g")]
    [InlineData(UtopIRPointerArithmeticOperation.Diff, "diff.g")]
    public void Generate_PointerArithmeticInstruction_EmitsCorrectLine(UtopIRPointerArithmeticOperation operation, string expectedMnemonic)
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new PointerArithmeticInstruction(operation, new UtopIRVariable("result"), new UtopIRVariable("NumbersPointer"), new LiteralOperand(2))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe($"£result = {expectedMnemonic} £NumbersPointer, 2");
    }

    /// <summary>
    /// Tests that a <c>sail</c> instruction is emitted with its label, prefixed with <c>!</c>.
    /// </summary>
    [Fact]
    public void Generate_SailInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new SailInstruction(new UtopIRLabel("LOGIC"))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("sail !LOGIC");
    }

    /// <summary>
    /// Tests that a <c>sailalike</c> instruction is emitted with its value operand and label, in that order.
    /// </summary>
    [Fact]
    public void Generate_SailAlikeInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new SailAlikeInstruction(new VariableOperand(new UtopIRVariable("Boolean")), new UtopIRLabel("IS_ALIKE"))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("sailalike £Boolean, !IS_ALIKE");
    }

    /// <summary>
    /// Tests that a <c>sailunlike</c> instruction is emitted with its value operand and label, in that order.
    /// </summary>
    [Fact]
    public void Generate_SailUnlikeInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new SailUnlikeInstruction(new VariableOperand(new UtopIRVariable("Boolean")), new UtopIRLabel("IS_UNLIKE"))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("sailunlike £Boolean, !IS_UNLIKE");
    }

    /// <summary>
    /// Tests that a label declaration is emitted as a bare <c>!name</c> line with no leading indentation, matching the flat-marker semantics of <see cref="LabelInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_LabelInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new LabelInstruction(new UtopIRLabel("LOGIC"))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("!LOGIC");
    }

    /// <summary>
    /// Tests that a <c>were</c> instruction is emitted with its value operand and destination type.
    /// </summary>
    [Fact]
    public void Generate_WereInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new WereInstruction(
                new UtopIRVariable("Lords"),
                new VariableOperand(new UtopIRVariable("LovesickMaidens")),
                UtopIRType.Chancellor)
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£Lords = were £LovesickMaidens, chancellor");
    }

    /// <summary>
    /// Tests that a <c>were</c> instruction with a literal operand is emitted correctly.
    /// </summary>
    [Fact]
    public void Generate_WereInstruction_WithLiteralOperand_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new WereInstruction(new UtopIRVariable("x"), new LiteralOperand(10), UtopIRType.Chancellor)
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£x = were 10, chancellor");
    }

    /// <summary>
    /// Tests that a <c>prentice</c> instruction with a variable operand is emitted correctly.
    /// </summary>
    [Fact]
    public void Generate_PrenticeInstruction_WithVariableOperand_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new PrenticeInstruction(new VariableOperand(new UtopIRVariable("x")))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("prentice £x");
    }

    /// <summary>
    /// Tests that a <c>prentice</c> instruction with a literal operand is emitted correctly.
    /// </summary>
    [Fact]
    public void Generate_PrenticeInstruction_WithLiteralOperand_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new PrenticeInstruction(new LiteralOperand(99))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("prentice 99");
    }

    /// <summary>
    /// Tests that a standalone <c>summon</c> instruction is emitted with the <c>&amp;</c>-prefixed function name.
    /// </summary>
    [Fact]
    public void Generate_SummonInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new SummonInstruction(new FunctionReference("PreviewBehold"), [])
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("summon &PreviewBehold");
    }

    /// <summary>
    /// Tests that a <c>summon.find</c> instruction is emitted with the assignment target and the
    /// <c>&amp;</c>-prefixed function name.
    /// </summary>
    [Fact]
    public void Generate_SummonFindInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new SummonFindInstruction(new UtopIRVariable("input"), new FunctionReference("PreviewPrayTell"), [])
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£input = summon.find &PreviewPrayTell");
    }

    /// <summary>
    /// Tests that a <c>leave</c> instruction is emitted with the correct assignment target.
    /// </summary>
    [Fact]
    public void Generate_LeaveInstruction_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new LeaveInstruction(new UtopIRVariable("result"))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£result = leave");
    }

    /// <summary>
    /// Tests that a <c>find</c> instruction with a variable operand is emitted correctly.
    /// </summary>
    [Fact]
    public void Generate_FindInstruction_WithVariableOperand_EmitsCorrectLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new FindInstruction(new VariableOperand(new UtopIRVariable("exitCode")))
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("find £exitCode");
    }

    /// <summary>
    /// Tests that a <c>find</c> instruction with no operand emits the keyword alone, representing a void return.
    /// </summary>
    [Fact]
    public void Generate_FindInstruction_WithNoOperand_EmitsFindKeywordOnly()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
            new FindInstruction(null)
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("find");
    }

    /// <summary>
    /// Tests that a complete programme with all instruction types is emitted in order, one instruction per line.
    /// </summary>
    [Fact]
    public void Generate_MultipleInstructions_EmitsEachOnSeparateLine()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [
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
        ])]);

        string result = ExtractBody(this.generator.Generate(program));

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
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), new List<UtopIRInstruction>())]);

        string result = ExtractBody(this.generator.Generate(program));

        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// Tests that a generated <c>alike</c> comparison instruction re-parses to an equal <see cref="ComparisonInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_ComparisonInstruction_RoundTripsThroughParser()
    {
        ComparisonInstruction original = new(
            UtopIRComparisonOperation.Alike,
            new UtopIRVariable("result"),
            new VariableOperand(new UtopIRVariable("a")),
            new VariableOperand(new UtopIRVariable("b")));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated <c>hardly</c> instruction re-parses to an equal <see cref="HardlyInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_HardlyInstruction_RoundTripsThroughParser()
    {
        HardlyInstruction original = new(
            new UtopIRVariable("result"),
            new VariableOperand(new UtopIRVariable("a")));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated <c>victim.yarn</c> instruction re-parses to an equal <see cref="VictimYarnInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_VictimYarnInstruction_RoundTripsThroughParser()
    {
        VictimYarnInstruction original = new(
            new UtopIRVariable("PoemSubjectLetter4"),
            new VariableOperand(new UtopIRVariable("PoemSubject")),
            new LiteralOperand(4));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated <c>welcome.list</c> instruction re-parses to an equal <see cref="WelcomeListInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_WelcomeListInstruction_RoundTripsThroughParser()
    {
        WelcomeListInstruction original = new(new UtopIRVariable("Numbers"), UtopIRType.Peer, new LiteralOperand(3));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated <c>appoint.victim</c> instruction re-parses to an equal <see cref="AppointVictimInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_AppointVictimInstruction_RoundTripsThroughParser()
    {
        AppointVictimInstruction original = new(new UtopIRVariable("Numbers"), new LiteralOperand(1), new LiteralOperand(10));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated <c>victim.list</c> instruction re-parses to an equal <see cref="VictimListInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_VictimListInstruction_RoundTripsThroughParser()
    {
        VictimListInstruction original = new(new UtopIRVariable("NumbersElement2"), new UtopIRVariable("Numbers"), new LiteralOperand(2));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated <c>welcome.gallerypic</c> instruction re-parses to an equal <see cref="WelcomeGallerypicInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_WelcomeGallerypicInstruction_RoundTripsThroughParser()
    {
        WelcomeGallerypicInstruction original = new(new UtopIRVariable("NumberPointer"), UtopIRType.Peer);
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated <c>pictureto</c> instruction re-parses to an equal <see cref="PicturetoInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_PicturetoInstruction_RoundTripsThroughParser()
    {
        PicturetoInstruction original = new(new UtopIRVariable("NumberPointer"), new UtopIRVariable("Number"));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated <c>viewfrom</c> instruction re-parses to an equal <see cref="ViewfromInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_ViewfromInstruction_RoundTripsThroughParser()
    {
        ViewfromInstruction original = new(new UtopIRVariable("NumberValue"), new UtopIRVariable("NumberPointer"));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated <c>viewto</c> instruction re-parses to an equal <see cref="ViewtoInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_ViewtoInstruction_RoundTripsThroughParser()
    {
        ViewtoInstruction original = new(new UtopIRVariable("NumberPointer"), new LiteralOperand(23));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated pointer arithmetic instruction re-parses to an equal <see cref="PointerArithmeticInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_PointerArithmeticInstruction_RoundTripsThroughParser()
    {
        PointerArithmeticInstruction original = new(UtopIRPointerArithmeticOperation.Sum, new UtopIRVariable("result"), new UtopIRVariable("NumbersPointer"), new LiteralOperand(2));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated <c>sailalike</c> instruction re-parses to an equal <see cref="SailAlikeInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_SailAlikeInstruction_RoundTripsThroughParser()
    {
        SailAlikeInstruction original = new(new VariableOperand(new UtopIRVariable("Boolean")), new UtopIRLabel("IS_ALIKE"));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated bare label declaration re-parses to an equal <see cref="LabelInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_LabelInstruction_RoundTripsThroughParser()
    {
        LabelInstruction original = new(new UtopIRLabel("LOGIC"));
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated standalone <c>summon</c> instruction re-parses to an equal <see cref="SummonInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_SummonInstruction_RoundTripsThroughParser()
    {
        SummonInstruction original = new(new FunctionReference("PreviewBehold"), []);
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that a generated <c>summon.find</c> instruction re-parses to an equal <see cref="SummonFindInstruction"/>.
    /// </summary>
    [Fact]
    public void Generate_SummonFindInstruction_RoundTripsThroughParser()
    {
        SummonFindInstruction original = new(new UtopIRVariable("input"), new FunctionReference("PreviewPrayTell"), []);
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [original])]);

        string generated = ExtractBody(this.generator.Generate(program)).Trim();
        Result<UtopIRInstruction> parsed = InstructionParser.Instruction(new(generated));

        parsed.HasValue.ShouldBeTrue();
        parsed.Value.ShouldBe(original);
    }

    /// <summary>
    /// Tests that <see cref="UtopIRCodeGenerator.Generate"/> renders a <c>duty</c> header with
    /// parameters and a return type, and the matching <c>discharged</c> closer.
    /// </summary>
    [Fact]
    public void Generate_FunctionDefinition_WithParametersAndReturnType_EmitsDutyHeaderAndDischarged()
    {
        UtopIRFunctionDefinition add = new(
            "Add",
            [
                new UtopIRFunctionParameter(new UtopIRTermType(UtopIRType.Peer), "Num1"),
                new UtopIRFunctionParameter(new UtopIRTermType(UtopIRType.Peer), "Num2")
            ],
            new UtopIRTermType(UtopIRType.Peer),
            [
                new ArithmeticInstruction(UtopIRArithmeticOperation.Sum, new UtopIRVariable("_sum_Num1_Num2"), new ParameterOperand(new UtopIRParameter("Num1")), new ParameterOperand(new UtopIRParameter("Num2"))),
                new FindInstruction(new VariableOperand(new UtopIRVariable("_sum_Num1_Num2")))
            ]);
        UtopIRProgram program = new([add]);

        string result = this.generator.Generate(program);

        result.ShouldContain("duty &Add, term peer %Num1, term peer %Num2, finds peer");
        result.ShouldContain("£_sum_Num1_Num2 = sum %Num1, %Num2");
        result.ShouldContain("discharged");
    }

    /// <summary>
    /// Tests that <see cref="UtopIRCodeGenerator.Generate"/> renders a void, parameterless <c>duty</c>
    /// header with no trailing <c>term</c>/<c>finds</c> clause.
    /// </summary>
    [Fact]
    public void Generate_FunctionDefinition_WithNoParametersOrReturnType_EmitsBareDutyHeader()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("DoNothing", [], null, [])]);

        string result = this.generator.Generate(program);

        result.ShouldContain("duty &DoNothing");
        result.ShouldNotContain("term");
        result.ShouldNotContain("finds");
    }

    /// <summary>
    /// Tests that <see cref="UtopIRCodeGenerator.Generate"/> renders an array-typed parameter using the
    /// <c>list.&lt;type&gt;</c> form.
    /// </summary>
    [Fact]
    public void Generate_FunctionDefinition_WithArrayParameter_EmitsListDotTypeForm()
    {
        UtopIRFunctionDefinition function = new(
            "FirstElement",
            [new UtopIRFunctionParameter(new UtopIRTermType(UtopIRType.Array, UtopIRType.Peer), "Numbers")],
            new UtopIRTermType(UtopIRType.Peer),
            [new FindInstruction(new LiteralOperand(0))]);
        UtopIRProgram program = new([function]);

        string result = this.generator.Generate(program);

        result.ShouldContain("term list.peer %Numbers");
    }

    /// <summary>
    /// Tests that <see cref="UtopIRCodeGenerator.Generate"/> renders the <c>summon</c>/<c>summon.find</c>
    /// trailing signature clause, including the array <c>list.&lt;type&gt;</c> form.
    /// </summary>
    [Fact]
    public void Generate_SummonFindInstruction_WithArrayParameterType_EmitsListDotTypeSignature()
    {
        SummonFindInstruction summonFind = new(
            new UtopIRVariable("first"),
            new FunctionReference("FirstElement"),
            [new UtopIRTermType(UtopIRType.Array, UtopIRType.Peer)]);
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Opera", [], new UtopIRTermType(UtopIRType.Peer), [summonFind])]);

        string result = ExtractBody(this.generator.Generate(program));

        result.Trim().ShouldBe("£first = summon.find &FirstElement, term list.peer");
    }

    /// <summary>
    /// Tests that <see cref="UtopIRCodeGenerator.Generate"/> renders a namespace-qualified function
    /// name unchanged, with the <c>*</c> delimiter preserved.
    /// </summary>
    [Fact]
    public void Generate_FunctionDefinition_WithNamespaceQualifiedName_PreservesDelimiter()
    {
        UtopIRProgram program = new([new UtopIRFunctionDefinition("Aesthetic*Writing*Greet", [], null, [])]);

        string result = this.generator.Generate(program);

        result.ShouldContain("duty &Aesthetic*Writing*Greet");
    }

    /// <summary>
    /// Tests that a generated multi-function programme, including a <c>list.&lt;type&gt;</c> parameter
    /// and a namespace-qualified name, re-parses to a structurally-equal <see cref="UtopIRProgram"/>.
    /// </summary>
    [Fact]
    public void Generate_MultiFunctionProgramme_RoundTripsThroughParser()
    {
        UtopIRProgram original = new([
            new UtopIRFunctionDefinition(
                "Aesthetic*Writing*FirstElement",
                [new UtopIRFunctionParameter(new UtopIRTermType(UtopIRType.Array, UtopIRType.Peer), "Numbers")],
                new UtopIRTermType(UtopIRType.Peer),
                [new FindInstruction(new LiteralOperand(0))]),
            new UtopIRFunctionDefinition(
                "Opera",
                [],
                new UtopIRTermType(UtopIRType.Peer),
                [new FindInstruction(new LiteralOperand(0))])
        ]);

        string generated = this.generator.Generate(original);
        UtopIRParseResult result = new UtopIRParser().TryParse(generated);

        result.Success.ShouldBeTrue();
        result.Program!.Functions.ShouldBe(original.Functions);
    }

    /// <summary>
    /// Extracts and de-indents the body lines of a generated <c>duty &amp;Opera ... discharged</c> block,
    /// so existing single-instruction assertions can keep asserting on just the instruction text itself.
    /// </summary>
    /// <param name="generated">The full generated UtopIR source, including the <c>duty</c>/<c>discharged</c> wrapper.</param>
    /// <returns>The function body, with the fixed 4-space indent removed from each line.</returns>
    private static string ExtractBody(string generated)
    {
        string[] lines = generated.Replace("\r\n", "\n").Split('\n');
        List<string> body = [];
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].TrimStart() == "discharged")
            {
                break;
            }

            body.Add(lines[i].Length >= 4
                ? lines[i][4..]
                : lines[i]);
        }

        return string.Join('\n', body);
    }
}
