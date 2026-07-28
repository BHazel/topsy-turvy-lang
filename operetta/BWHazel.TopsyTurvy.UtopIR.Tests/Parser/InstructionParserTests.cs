using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Parser;
using Superpower.Model;

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
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£LovesickMaidens = welcome peer"));

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
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£x = appoint £y"));

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
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£x = appoint 42"));

        result.HasValue.ShouldBeTrue();
        AppointInstruction appoint = result.Value.ShouldBeOfType<AppointInstruction>();
        appoint.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(42);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses the <c>naught</c> null
    /// literal, returning the <see cref="NaughtLiteral"/> singleton.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithAppointNaught_ReturnsNaughtLiteral()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£x = appoint naught"));

        result.HasValue.ShouldBeTrue();
        AppointInstruction appoint = result.Value.ShouldBeOfType<AppointInstruction>();
        appoint.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBeSameAs(NaughtLiteral.Instance);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a negative integer literal.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithNegativeLiteral_ReturnsNegativeValue()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£x = appoint -7"));

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
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£Lords = were £LovesickMaidens, chancellor"));

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
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£result = leave"));

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
    [InlineData("sum.f", UtopIRArithmeticOperation.SumFloat)]
    [InlineData("diff.f", UtopIRArithmeticOperation.DiffFloat)]
    [InlineData("prod.f", UtopIRArithmeticOperation.ProdFloat)]
    [InlineData("quot.f", UtopIRArithmeticOperation.QuotFloat)]
    [InlineData("rem.f", UtopIRArithmeticOperation.RemFloat)]
    [InlineData("max.f", UtopIRArithmeticOperation.MaxFloat)]
    [InlineData("min.f", UtopIRArithmeticOperation.MinFloat)]
    public void AssignmentInstruction_WithArithmeticMnemonic_ReturnsCorrectOperation(string mnemonic, UtopIRArithmeticOperation expectedOperation)
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new($"£r = {mnemonic} £a, £b"));

        result.HasValue.ShouldBeTrue();
        ArithmeticInstruction arithmetic = result.Value.ShouldBeOfType<ArithmeticInstruction>();
        arithmetic.Operation.ShouldBe(expectedOperation);
        arithmetic.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        arithmetic.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a floating-point arithmetic instruction with float literal operands.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithFloatMnemonicAndFloatLiterals_ReturnsFloatOperands()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£r = sum.f 1.5, 2.5"));

        result.HasValue.ShouldBeTrue();
        ArithmeticInstruction arithmetic = result.Value.ShouldBeOfType<ArithmeticInstruction>();
        arithmetic.Operation.ShouldBe(UtopIRArithmeticOperation.SumFloat);
        arithmetic.Operand1.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(1.5);
        arithmetic.Operand2.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(2.5);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses each binary bitwise mnemonic to the correct operation.
    /// </summary>
    /// <param name="mnemonic">The UtopIR bitwise mnemonic.</param>
    /// <param name="expectedOperation">The expected <see cref="UtopIRBitwiseOperation"/>.</param>
    [Theory]
    [InlineData("chord", UtopIRBitwiseOperation.Chord)]
    [InlineData("harmony", UtopIRBitwiseOperation.Harmony)]
    [InlineData("discord", UtopIRBitwiseOperation.Discord)]
    [InlineData("transup", UtopIRBitwiseOperation.TransUp)]
    [InlineData("transdown", UtopIRBitwiseOperation.TransDown)]
    public void AssignmentInstruction_WithBitwiseMnemonic_ReturnsCorrectOperation(string mnemonic, UtopIRBitwiseOperation expectedOperation)
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new($"£r = {mnemonic} £a, £b"));

        result.HasValue.ShouldBeTrue();
        BitwiseInstruction bitwise = result.Value.ShouldBeOfType<BitwiseInstruction>();
        bitwise.Operation.ShouldBe(expectedOperation);
        bitwise.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        bitwise.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses an <c>inv</c> assignment with a single operand.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithInv_ReturnsInvInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£r = inv £a"));

        result.HasValue.ShouldBeTrue();
        InvInstruction inv = result.Value.ShouldBeOfType<InvInstruction>();
        inv.Target.Name.ShouldBe("r");
        inv.Operand.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses each comparison mnemonic to the correct operation.
    /// </summary>
    /// <param name="mnemonic">The UtopIR comparison mnemonic.</param>
    /// <param name="expectedOperation">The expected <see cref="UtopIRComparisonOperation"/>.</param>
    [Theory]
    [InlineData("alike", UtopIRComparisonOperation.Alike)]
    [InlineData("unlike", UtopIRComparisonOperation.Unlike)]
    [InlineData("preadam", UtopIRComparisonOperation.PreAdam)]
    [InlineData("lowerdeg", UtopIRComparisonOperation.LowerDeg)]
    [InlineData("alike.f", UtopIRComparisonOperation.AlikeFloat)]
    [InlineData("unlike.f", UtopIRComparisonOperation.UnlikeFloat)]
    [InlineData("preadam.f", UtopIRComparisonOperation.PreAdamFloat)]
    [InlineData("lowerdeg.f", UtopIRComparisonOperation.LowerDegFloat)]
    public void AssignmentInstruction_WithComparisonMnemonic_ReturnsCorrectOperation(string mnemonic, UtopIRComparisonOperation expectedOperation)
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new($"£r = {mnemonic} £a, £b"));

        result.HasValue.ShouldBeTrue();
        ComparisonInstruction comparison = result.Value.ShouldBeOfType<ComparisonInstruction>();
        comparison.Operation.ShouldBe(expectedOperation);
        comparison.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        comparison.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses each binary logical mnemonic to the correct operation.
    /// </summary>
    /// <param name="mnemonic">The UtopIR logical mnemonic.</param>
    /// <param name="expectedOperation">The expected <see cref="UtopIRLogicalOperation"/>.</param>
    [Theory]
    [InlineData("both", UtopIRLogicalOperation.Both)]
    [InlineData("either", UtopIRLogicalOperation.Either)]
    public void AssignmentInstruction_WithLogicalMnemonic_ReturnsCorrectOperation(string mnemonic, UtopIRLogicalOperation expectedOperation)
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new($"£r = {mnemonic} verity, nay"));

        result.HasValue.ShouldBeTrue();
        LogicalInstruction logical = result.Value.ShouldBeOfType<LogicalInstruction>();
        logical.Operation.ShouldBe(expectedOperation);
        logical.Operand1.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(true);
        logical.Operand2.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(false);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>hardly</c> assignment with a single operand.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithHardly_ReturnsHardlyInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£r = hardly £a"));

        result.HasValue.ShouldBeTrue();
        HardlyInstruction hardly = result.Value.ShouldBeOfType<HardlyInstruction>();
        hardly.Target.Name.ShouldBe("r");
        hardly.Operand.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>victim.yarn</c> assignment.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithVictimYarn_ReturnsVictimYarnInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£r = victim.yarn £PoemSubject, 4"));

        result.HasValue.ShouldBeTrue();
        VictimYarnInstruction victimYarn = result.Value.ShouldBeOfType<VictimYarnInstruction>();
        victimYarn.Target.Name.ShouldBe("r");
        victimYarn.YarnString.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("PoemSubject");
        victimYarn.Index.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(4);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>welcome.list</c> assignment.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithWelcomeList_ReturnsWelcomeListInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£Numbers = welcome.list peer, 3"));

        result.HasValue.ShouldBeTrue();
        WelcomeListInstruction welcomeList = result.Value.ShouldBeOfType<WelcomeListInstruction>();
        welcomeList.Target.Name.ShouldBe("Numbers");
        welcomeList.ElementType.ShouldBe(UtopIRType.Peer);
        welcomeList.Size.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(3);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>welcome.list</c> assignment
    /// whose size is a variable rather than a literal.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithWelcomeListVariableSize_ReturnsWelcomeListInstructionWithVariableOperand()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£Numbers = welcome.list peer, £Count"));

        result.HasValue.ShouldBeTrue();
        WelcomeListInstruction welcomeList = result.Value.ShouldBeOfType<WelcomeListInstruction>();
        welcomeList.Target.Name.ShouldBe("Numbers");
        welcomeList.ElementType.ShouldBe(UtopIRType.Peer);
        welcomeList.Size.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("Count");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses <c>welcome.list</c> in
    /// full rather than stopping short at the shared <c>welcome</c> prefix.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithWelcomeList_DoesNotStopAtWelcomePrefix()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£Numbers = welcome.list peer, 3"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldNotBeOfType<WelcomeInstruction>();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>victim.list</c> assignment.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithVictimList_ReturnsVictimListInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£r = victim.list £Numbers, 2"));

        result.HasValue.ShouldBeTrue();
        VictimListInstruction victimList = result.Value.ShouldBeOfType<VictimListInstruction>();
        victimList.Target.Name.ShouldBe("r");
        victimList.Array.Name.ShouldBe("Numbers");
        victimList.Index.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(2);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AppointVictim"/> parses an <c>appoint.victim</c> instruction.
    /// </summary>
    [Fact]
    public void AppointVictim_WithArrayIndexAndValue_ReturnsAppointVictimInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.AppointVictim(new("appoint.victim £Numbers, 1, 10"));

        result.HasValue.ShouldBeTrue();
        AppointVictimInstruction appointVictim = result.Value.ShouldBeOfType<AppointVictimInstruction>();
        appointVictim.Array.Name.ShouldBe("Numbers");
        appointVictim.Index.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(1);
        appointVictim.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(10);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.StandaloneInstruction"/> parses <c>appoint.victim</c> rather
    /// than stopping short at the shared <c>appoint</c> prefix used by the assignment-form instruction.
    /// </summary>
    [Fact]
    public void StandaloneInstruction_WithAppointVictim_ReturnsAppointVictimInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.StandaloneInstruction(new("appoint.victim £Numbers, 1, 10"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBeOfType<AppointVictimInstruction>();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>welcome.gallerypic</c> assignment.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithWelcomeGallerypic_ReturnsWelcomeGallerypicInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£NumberPointer = welcome.gallerypic peer"));

        result.HasValue.ShouldBeTrue();
        WelcomeGallerypicInstruction welcomeGallerypic = result.Value.ShouldBeOfType<WelcomeGallerypicInstruction>();
        welcomeGallerypic.Target.Name.ShouldBe("NumberPointer");
        welcomeGallerypic.PointeeType.ShouldBe(UtopIRType.Peer);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses <c>welcome.gallerypic</c> in
    /// full rather than stopping short at the shared <c>welcome</c> prefix.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithWelcomeGallerypic_DoesNotStopAtWelcomePrefix()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£NumberPointer = welcome.gallerypic peer"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldNotBeOfType<WelcomeInstruction>();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>pictureto</c> assignment.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithPictureto_ReturnsPicturetoInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£NumberPointer = pictureto £Number"));

        result.HasValue.ShouldBeTrue();
        PicturetoInstruction pictureto = result.Value.ShouldBeOfType<PicturetoInstruction>();
        pictureto.Target.Name.ShouldBe("NumberPointer");
        pictureto.Pointee.Name.ShouldBe("Number");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>viewfrom</c> assignment.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithViewfrom_ReturnsViewfromInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£NumberValue = viewfrom £NumberPointer"));

        result.HasValue.ShouldBeTrue();
        ViewfromInstruction viewfrom = result.Value.ShouldBeOfType<ViewfromInstruction>();
        viewfrom.Target.Name.ShouldBe("NumberValue");
        viewfrom.Pointer.Name.ShouldBe("NumberPointer");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.ViewTo"/> parses a <c>viewto</c> instruction.
    /// </summary>
    [Fact]
    public void ViewTo_WithPointerAndValue_ReturnsViewtoInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.ViewTo(new("viewto £NumberPointer, 23"));

        result.HasValue.ShouldBeTrue();
        ViewtoInstruction viewto = result.Value.ShouldBeOfType<ViewtoInstruction>();
        viewto.Pointer.Name.ShouldBe("NumberPointer");
        viewto.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(23);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.StandaloneInstruction"/> parses <c>viewto</c>.
    /// </summary>
    [Fact]
    public void StandaloneInstruction_WithViewTo_ReturnsViewtoInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.StandaloneInstruction(new("viewto £NumberPointer, 23"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBeOfType<ViewtoInstruction>();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses each pointer arithmetic
    /// mnemonic to the correct operation.
    /// </summary>
    /// <param name="mnemonic">The UtopIR pointer arithmetic mnemonic.</param>
    /// <param name="expectedOperation">The expected <see cref="UtopIRPointerArithmeticOperation"/>.</param>
    [Theory]
    [InlineData("sum.g", UtopIRPointerArithmeticOperation.Sum)]
    [InlineData("diff.g", UtopIRPointerArithmeticOperation.Diff)]
    public void AssignmentInstruction_WithPointerArithmeticMnemonic_ReturnsCorrectOperation(string mnemonic, UtopIRPointerArithmeticOperation expectedOperation)
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new($"£r = {mnemonic} £NumbersPointer, 2"));

        result.HasValue.ShouldBeTrue();
        PointerArithmeticInstruction pointerArithmetic = result.Value.ShouldBeOfType<PointerArithmeticInstruction>();
        pointerArithmetic.Operation.ShouldBe(expectedOperation);
        pointerArithmetic.Pointer.Name.ShouldBe("NumbersPointer");
        pointerArithmetic.Offset.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(2);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses pointer arithmetic rather
    /// than stopping short at the shared <c>sum</c>/<c>diff</c> prefix used by ordinary arithmetic.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithPointerArithmetic_DoesNotStopAtArithmeticPrefix()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£r = sum.g £NumbersPointer, 2"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldNotBeOfType<ArithmeticInstruction>();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.Sail"/> parses an unconditional branch to a label.
    /// </summary>
    [Fact]
    public void Sail_WithLabel_ReturnsSailInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.Sail(new("sail !LOGIC"));

        result.HasValue.ShouldBeTrue();
        SailInstruction sail = result.Value.ShouldBeOfType<SailInstruction>();
        sail.Label.Name.ShouldBe("LOGIC");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.SailAlike"/> parses a conditional branch with a value and a label.
    /// </summary>
    [Fact]
    public void SailAlike_WithValueAndLabel_ReturnsSailAlikeInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.SailAlike(new("sailalike £Boolean, !IS_ALIKE"));

        result.HasValue.ShouldBeTrue();
        SailAlikeInstruction sailAlike = result.Value.ShouldBeOfType<SailAlikeInstruction>();
        sailAlike.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("Boolean");
        sailAlike.Label.Name.ShouldBe("IS_ALIKE");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.SailUnlike"/> parses a conditional branch with a value and a label.
    /// </summary>
    [Fact]
    public void SailUnlike_WithValueAndLabel_ReturnsSailUnlikeInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.SailUnlike(new("sailunlike £Boolean, !IS_UNLIKE"));

        result.HasValue.ShouldBeTrue();
        SailUnlikeInstruction sailUnlike = result.Value.ShouldBeOfType<SailUnlikeInstruction>();
        sailUnlike.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("Boolean");
        sailUnlike.Label.Name.ShouldBe("IS_UNLIKE");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.Label"/> parses a bare label declaration.
    /// </summary>
    [Fact]
    public void Label_WithBareName_ReturnsLabelInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.Label(new("!LOGIC"));

        result.HasValue.ShouldBeTrue();
        LabelInstruction label = result.Value.ShouldBeOfType<LabelInstruction>();
        label.Name.Name.ShouldBe("LOGIC");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.StandaloneInstruction"/> parses <c>sailalike</c> rather than
    /// stopping short at the shared <c>sail</c> prefix.
    /// </summary>
    [Fact]
    public void StandaloneInstruction_WithSailAlike_ReturnsSailAlikeInstructionNotSail()
    {
        Result<UtopIRInstruction> result = InstructionParser.StandaloneInstruction(new("sailalike £Boolean, !IS_ALIKE"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBeOfType<SailAlikeInstruction>();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.Prentice"/> parses a <c>prentice</c> instruction.
    /// </summary>
    [Fact]
    public void Prentice_WithLiteral_ReturnsPrenticeInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.Prentice(new("prentice 99"));

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
        Result<UtopIRInstruction> result = InstructionParser.Find(new("find £exitCode"));

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
        Result<UtopIRInstruction> result = InstructionParser.Find(new("find"));

        result.HasValue.ShouldBeTrue();
        FindInstruction find = result.Value.ShouldBeOfType<FindInstruction>();
        find.Value.ShouldBeNull();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.Summon"/> parses a standalone <c>summon</c> instruction.
    /// </summary>
    [Fact]
    public void Summon_WithFunctionReference_ReturnsSummonInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.Summon(new("summon &PreviewBehold"));

        result.HasValue.ShouldBeTrue();
        SummonInstruction summon = result.Value.ShouldBeOfType<SummonInstruction>();
        summon.Function.Name.ShouldBe("PreviewBehold");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses a <c>summon.find</c> right-hand side.
    /// </summary>
    [Fact]
    public void AssignmentInstruction_WithSummonFindRhs_ReturnsSummonFindInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£r = summon.find &PreviewPrayTell"));

        result.HasValue.ShouldBeTrue();
        SummonFindInstruction summonFind = result.Value.ShouldBeOfType<SummonFindInstruction>();
        summonFind.Target.Name.ShouldBe("r");
        summonFind.Function.Name.ShouldBe("PreviewPrayTell");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.StandaloneInstruction"/> parses <c>summon</c> without stopping
    /// short of the function reference operand.
    /// </summary>
    [Fact]
    public void StandaloneInstruction_WithSummon_ReturnsSummonInstruction()
    {
        Result<UtopIRInstruction> result = InstructionParser.StandaloneInstruction(new("summon &PreviewBehold"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBeOfType<SummonInstruction>();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.Instruction"/> fails on an unrecognised assignment right-hand side.
    /// </summary>
    [Fact]
    public void Instruction_WithUnknownRhsKeyword_Fails()
    {
        Result<UtopIRInstruction> result = InstructionParser.Instruction(new("£x = bogus 5"));

        result.HasValue.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.InstructionSequence"/> parses multiple newline-separated instructions in order.
    /// </summary>
    [Fact]
    public void InstructionSequence_WithMultipleLines_ReturnsInstructionsInOrder()
    {
        Result<UtopIRInstruction[]> result = InstructionParser.InstructionSequence()(new("£a = welcome peer\n£a = appoint 10\nfind £a"));

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
        Result<UtopIRInstruction[]> result = InstructionParser.InstructionSequence()(new("@ a comment\n\n£a = welcome peer\n@ another comment\n"));

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
        Result<UtopIRInstruction[]> result = InstructionParser.InstructionSequence()(new("£a = welcome peer\n£b = bogus 5\nfind £a"));

        result.HasValue.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.Summon"/> parses the trailing <c>term</c> signature clause.
    /// </summary>
    [Fact]
    public void Summon_WithSignatureClause_PopulatesParameterTypes()
    {
        Result<UtopIRInstruction> result = InstructionParser.Summon(new("summon &Add, term peer, term peer"));

        result.HasValue.ShouldBeTrue();
        SummonInstruction summon = result.Value.ShouldBeOfType<SummonInstruction>();
        summon.ParameterTypes.Count.ShouldBe(2);
        summon.ParameterTypes[0].Type.ShouldBe(UtopIRType.Peer);
        summon.ParameterTypes[1].Type.ShouldBe(UtopIRType.Peer);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.AssignmentInstruction"/> parses the trailing <c>term</c>
    /// signature clause on a <c>summon.find</c> right-hand side.
    /// </summary>
    [Fact]
    public void SummonFind_WithSignatureClause_PopulatesParameterTypes()
    {
        Result<UtopIRInstruction> result = InstructionParser.AssignmentInstruction(new("£r = summon.find &Add, term peer, term peer"));

        result.HasValue.ShouldBeTrue();
        SummonFindInstruction summonFind = result.Value.ShouldBeOfType<SummonFindInstruction>();
        summonFind.ParameterTypes.Count.ShouldBe(2);
        summonFind.ParameterTypes[0].Type.ShouldBe(UtopIRType.Peer);
        summonFind.ParameterTypes[1].Type.ShouldBe(UtopIRType.Peer);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.FunctionDefinition"/> parses a complete <c>duty</c> ...
    /// <c>discharged</c> block with parameters and a return type.
    /// </summary>
    [Fact]
    public void FunctionDefinition_WithParametersAndReturnType_ReturnsExpectedDefinition()
    {
        const string source =
            """
            duty &Add, term peer %Num1, term peer %Num2, finds peer
                £_sum_Num1_Num2 = sum %Num1, %Num2
                find £_sum_Num1_Num2
            discharged
            """;

        Result<UtopIRFunctionDefinition> result = InstructionParser.FunctionDefinition(new(source));

        result.HasValue.ShouldBeTrue();
        UtopIRFunctionDefinition function = result.Value;
        function.Name.ShouldBe("Add");
        function.Parameters.Count.ShouldBe(2);
        function.Parameters[0].Name.ShouldBe("Num1");
        function.Parameters[0].Type.Type.ShouldBe(UtopIRType.Peer);
        function.Parameters[1].Name.ShouldBe("Num2");
        function.ReturnType.ShouldNotBeNull();
        function.ReturnType!.Type.ShouldBe(UtopIRType.Peer);
        function.Body.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.FunctionDefinition"/> parses a parameterless, void
    /// <c>duty</c> block with an empty body.
    /// </summary>
    [Fact]
    public void FunctionDefinition_WithNoParametersOrReturnType_ReturnsEmptyBody()
    {
        Result<UtopIRFunctionDefinition> result = InstructionParser.FunctionDefinition(new("duty &DoNothing\ndischarged"));

        result.HasValue.ShouldBeTrue();
        result.Value.Name.ShouldBe("DoNothing");
        result.Value.Parameters.ShouldBeEmpty();
        result.Value.ReturnType.ShouldBeNull();
        result.Value.Body.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.FunctionDefinition"/> parses a <c>term list.&lt;type&gt;</c>
    /// parameter into a <see cref="UtopIRTermType"/> with <see cref="UtopIRType.Array"/> and the correct element type.
    /// </summary>
    [Fact]
    public void FunctionDefinition_WithArrayParameter_ReturnsArrayTermType()
    {
        const string source =
            """
            duty &FirstElement, term list.peer %Numbers, finds peer
                find 0
            discharged
            """;

        Result<UtopIRFunctionDefinition> result = InstructionParser.FunctionDefinition(new(source));

        result.HasValue.ShouldBeTrue();
        UtopIRFunctionParameter parameter = result.Value.Parameters[0];
        parameter.Type.Type.ShouldBe(UtopIRType.Array);
        parameter.Type.ElementType.ShouldBe(UtopIRType.Peer);
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.FunctionDefinitionSequence"/> parses multiple function
    /// definitions, separated by a blank line, in source order.
    /// </summary>
    [Fact]
    public void FunctionDefinitionSequence_WithMultipleFunctions_ReturnsInSourceOrder()
    {
        const string source =
            """
            duty &Add, term peer %Num1, term peer %Num2, finds peer
                find %Num1
            discharged

            duty &Opera, finds peer
                find 0
            discharged
            """;

        Result<UtopIRFunctionDefinition[]> result = InstructionParser.FunctionDefinitionSequence()(new(source));

        result.HasValue.ShouldBeTrue();
        result.Value.Length.ShouldBe(2);
        result.Value[0].Name.ShouldBe("Add");
        result.Value[1].Name.ShouldBe("Opera");
    }

    /// <summary>
    /// Tests that <see cref="InstructionParser.FunctionDefinition"/> parses a namespace-qualified
    /// function reference in the <c>duty</c> header.
    /// </summary>
    [Fact]
    public void FunctionDefinition_WithNamespaceQualifiedName_ParsesFullyQualifiedName()
    {
        Result<UtopIRFunctionDefinition> result = InstructionParser.FunctionDefinition(new("duty &Aesthetic*Writing*Greet\ndischarged"));

        result.HasValue.ShouldBeTrue();
        result.Value.Name.ShouldBe("Aesthetic*Writing*Greet");
    }
}
