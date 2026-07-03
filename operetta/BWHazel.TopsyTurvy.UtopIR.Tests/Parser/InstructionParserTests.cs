using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Parser;

namespace BWHazel.TopsyTurvy.UtopIR.Tests.Parser;

/// <summary>
/// Tests for the <see cref="InstructionParser"/> instruction-form combinators.
/// </summary>
public class InstructionParserTests
{
    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>welcome</c> assignment.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithWelcome_ReturnsWelcomeInstruction()
    {
        var result = InstructionParser.AssignmentInstruction(new("£LovesickMaidens = welcome peer"));

        result.HasValue.ShouldBeTrue();
        WelcomeInstruction welcome = result.Value.ShouldBeOfType<WelcomeInstruction>();
        welcome.Target.Name.ShouldBe("LovesickMaidens");
        welcome.Type.ShouldBe(UtopIRType.Peer);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses an <c>appoint</c> assignment with a variable operand.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithAppointVariable_ReturnsAppointInstruction()
    {
        var result = InstructionParser.AssignmentInstruction(new("£x = appoint £y"));

        result.HasValue.ShouldBeTrue();
        AppointInstruction appoint = result.Value.ShouldBeOfType<AppointInstruction>();
        appoint.Target.Name.ShouldBe("x");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("y");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses an <c>appoint</c> assignment with a literal operand.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithAppointLiteral_ReturnsAppointInstruction()
    {
        var result = InstructionParser.AssignmentInstruction(new("£x = appoint 42"));

        result.HasValue.ShouldBeTrue();
        AppointInstruction appoint = result.Value.ShouldBeOfType<AppointInstruction>();
        appoint.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(42);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a negative integer literal.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithNegativeLiteral_ReturnsNegativeValue()
    {
        var result = InstructionParser.AssignmentInstruction(new("£x = appoint -7"));

        result.HasValue.ShouldBeTrue();
        AppointInstruction appoint = result.Value.ShouldBeOfType<AppointInstruction>();
        appoint.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(-7);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>were</c> cast.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithWere_ReturnsWereInstruction()
    {
        var result = InstructionParser.AssignmentInstruction(new("£Lords = were £LovesickMaidens, chancellor"));

        result.HasValue.ShouldBeTrue();
        WereInstruction were = result.Value.ShouldBeOfType<WereInstruction>();
        were.Target.Name.ShouldBe("Lords");
        were.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("LovesickMaidens");
        were.Type.ShouldBe(UtopIRType.Chancellor);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>leave</c> assignment.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithLeave_ReturnsLeaveInstruction()
    {
        var result = InstructionParser.AssignmentInstruction(new("£result = leave"));

        result.HasValue.ShouldBeTrue();
        LeaveInstruction leave = result.Value.ShouldBeOfType<LeaveInstruction>();
        leave.Target.Name.ShouldBe("result");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses each arithmetic mnemonic to the correct operation.
    /// </summary>
    /// <param name="mnemonic">The UtopIR arithmetic mnemonic.</param>
    /// <param name="expectedOperation">The expected <see cref="UtopIRArithmeticOperation"/>.</param>
    [Theory]
    [InlineData("sum", UtopIRArithmeticOperation.Sum)]
    [InlineData("diff", UtopIRArithmeticOperation.Diff)]
    [InlineData("prod", UtopIRArithmeticOperation.Prod)]
    [InlineData("quot", UtopIRArithmeticOperation.Quot)]
    [InlineData("rem", UtopIRArithmeticOperation.Rem)]
    [InlineData("max", UtopIRArithmeticOperation.Max)]
    [InlineData("min", UtopIRArithmeticOperation.Min)]
    public void AssignmentInstruction_WithArithmeticMnemonic_ReturnsCorrectOperation(string mnemonic, UtopIRArithmeticOperation expectedOperation)
    {
        var result = InstructionParser.AssignmentInstruction(new($"£r = {mnemonic} £a, £b"));

        result.HasValue.ShouldBeTrue();
        ArithmeticInstruction arithmetic = result.Value.ShouldBeOfType<ArithmeticInstruction>();
        arithmetic.Operation.ShouldBe(expectedOperation);
        arithmetic.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        arithmetic.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.Prentice"/> parses a <c>prentice</c> instruction.
    /// </summary>
    [Fact]
    public void Prentice_WithLiteral_ReturnsPrenticeInstruction()
    {
        var result = InstructionParser.Prentice(new("prentice 99"));

        result.HasValue.ShouldBeTrue();
        PrenticeInstruction prentice = result.Value.ShouldBeOfType<PrenticeInstruction>();
        prentice.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(99);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.Find"/> parses a valued <c>find</c> instruction.
    /// </summary>
    [Fact]
    public void Find_WithOperand_ReturnsFindInstructionWithValue()
    {
        var result = InstructionParser.Find(new("find £exitCode"));

        result.HasValue.ShouldBeTrue();
        FindInstruction find = result.Value.ShouldBeOfType<FindInstruction>();
        find.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("exitCode");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.Find"/> parses a void <c>find</c> instruction with no operand.
    /// </summary>
    [Fact]
    public void Find_WithNoOperand_ReturnsFindInstructionWithNullValue()
    {
        var result = InstructionParser.Find(new("find"));

        result.HasValue.ShouldBeTrue();
        FindInstruction find = result.Value.ShouldBeOfType<FindInstruction>();
        find.Value.ShouldBeNull();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.Instruction"/> fails on an unrecognised assignment right-hand side.
    /// </summary>
    [Fact]
    public void Instruction_WithUnknownRhsKeyword_Fails()
    {
        var result = InstructionParser.Instruction(new("£x = bogus 5"));

        result.HasValue.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.InstructionSequence"/> parses multiple newline-separated instructions in order.
    /// </summary>
    [Fact]
    public void InstructionSequence_WithMultipleLines_ReturnsInstructionsInOrder()
    {
        var result = InstructionParser.InstructionSequence()(new("£a = welcome peer\n£a = appoint 10\nfind £a"));

        result.HasValue.ShouldBeTrue();
        result.Value.Length.ShouldBe(3);
        result.Value[0].ShouldBeOfType<WelcomeInstruction>();
        result.Value[1].ShouldBeOfType<AppointInstruction>();
        result.Value[2].ShouldBeOfType<FindInstruction>();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.InstructionSequence"/> skips comment-only and blank lines.
    /// </summary>
    [Fact]
    public void InstructionSequence_WithCommentsAndBlankLines_SkipsThem()
    {
        var result = InstructionParser.InstructionSequence()(new("@ a comment\n\n£a = welcome peer\n@ another comment\n"));

        result.HasValue.ShouldBeTrue();
        result.Value.Length.ShouldBe(1);
        result.Value[0].ShouldBeOfType<WelcomeInstruction>();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.InstructionSequence"/> propagates an error at the
    /// failure position rather than silently ending the sequence, when a line partially matches an
    /// instruction keyword and then fails.
    /// </summary>
    [Fact]
    public void InstructionSequence_WithMalformedLine_PropagatesError()
    {
        var result = InstructionParser.InstructionSequence()(new("£a = welcome peer\n£b = bogus 5\nfind £a"));

        result.HasValue.ShouldBeFalse();
    }
}
