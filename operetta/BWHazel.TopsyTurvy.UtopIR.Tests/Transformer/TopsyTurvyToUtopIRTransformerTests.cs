using System;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Transformer;
using BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

namespace BWHazel.TopsyTurvy.UtopIR.Tests.Transformer;

/// <summary>
/// Tests for <see cref="TopsyTurvyToUtopIRTransformer"/>.
/// </summary>
public class TopsyTurvyToUtopIRTransformerTests
{
    private readonly TopsyTurvyToUtopIRTransformer transformer = new(new InstructionDetailVariableFormatter());

    /// <summary>A zero-origin span used for all synthetic test AST nodes.</summary>
    private static readonly SourceSpan PlaceholderSpan = new(new(0, 0), new(0, 0));

    /// <summary>
    /// Tests that a declaration with no initial value emits a single <c>welcome</c> instruction.
    /// </summary>
    [Fact]
    public void Transform_DeclarationWithNoInitialValue_EmitsSingleWelcomeInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "x", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(1);
        WelcomeInstruction welcome = result.Instructions[0].ShouldBeOfType<WelcomeInstruction>();
        welcome.Target.Name.ShouldBe("x");
        welcome.Type.ShouldBe(UtopIRType.Peer);
    }

    /// <summary>
    /// Tests that a declaration with a literal initial value emits <c>welcome</c> followed by <c>appoint</c> with a <see cref="LiteralOperand"/>.
    /// </summary>
    [Fact]
    public void Transform_DeclarationWithLiteralInitialValue_EmitsWelcomeThenAppoint()
    {
        ProgramNode program = Programme(
            new DeclarationNode()
            {
                Name = "n",
                NameSpan = PlaceholderSpan,
                Type = LiteralType.Integer,
                InitialValue = new LiteralNode { Value = 42, Type = LiteralType.Integer, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(2);
        result.Instructions[0].ShouldBeOfType<WelcomeInstruction>().Target.Name.ShouldBe("n");
        AppointInstruction appoint = result.Instructions[1].ShouldBeOfType<AppointInstruction>();
        appoint.Target.Name.ShouldBe("n");
        LiteralOperand literal = appoint.Value.ShouldBeOfType<LiteralOperand>();
        literal.Value.ShouldBe(42);
    }

    /// <summary>
    /// Tests that appointing an in-range integer literal to a <c>chancellor</c> declaration inserts a widening <see cref="WereInstruction"/> so the literal type matches the declared target type.
    /// </summary>
    [Fact]
    public void Transform_DeclarationWithMismatchedLiteralType_InsertsWereInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode()
            {
                Name = "big_number",
                NameSpan = PlaceholderSpan,
                Type = LiteralType.Long,
                InitialValue = new LiteralNode() { Value = 200, Type = LiteralType.Integer, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(3);
        result.Instructions[0].ShouldBeOfType<WelcomeInstruction>().Target.Name.ShouldBe("big_number");
        WereInstruction were = result.Instructions[1].ShouldBeOfType<WereInstruction>();
        were.Type.ShouldBe(UtopIRType.Chancellor);
        were.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(200);
        AppointInstruction appoint = result.Instructions[2].ShouldBeOfType<AppointInstruction>();
        appoint.Target.Name.ShouldBe("big_number");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(were.Target.Name);
    }

    /// <summary>
    /// Tests that assigning an in-range integer literal to an already-declared <c>chancellor</c> variable inserts a widening <see cref="WereInstruction"/>.
    /// </summary>
    [Fact]
    public void Transform_AssignmentWithMismatchedLiteralType_InsertsWereInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "big_number", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "big_number",
                Value = new LiteralNode() { Value = 200, Type = LiteralType.Integer, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(3);
        WereInstruction were = result.Instructions[1].ShouldBeOfType<WereInstruction>();
        were.Type.ShouldBe(UtopIRType.Chancellor);
        AppointInstruction appoint = result.Instructions[2].ShouldBeOfType<AppointInstruction>();
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(were.Target.Name);
    }

    /// <summary>
    /// Tests that a declaration whose initial value is an identifier reference emits <c>welcome</c> followed by <c>appoint</c> with a <see cref="VariableOperand"/>.
    /// </summary>
    [Fact]
    public void Transform_DeclarationWithIdentifierInitialValue_EmitsWelcomeThenAppointWithVariable()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "x", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode()
            {
                Name = "y",
                NameSpan = PlaceholderSpan,
                Type = LiteralType.Integer,
                InitialValue = new IdentifierNode { Name = "x", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(3);
        AppointInstruction appoint = result.Instructions[2].ShouldBeOfType<AppointInstruction>();
        VariableOperand variable = appoint.Value.ShouldBeOfType<VariableOperand>();
        variable.Variable.Name.ShouldBe("x");
    }

    /// <summary>
    /// Tests that declarations inside a <see cref="PrincipalBlockNode"/> are emitted in order.
    /// </summary>
    [Fact]
    public void Transform_PrincipalBlock_EmitsDeclarationsInOrder()
    {
        ProgramNode program = Programme(
            new PrincipalBlockNode()
            {
                Declarations =
                [
                    new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
                    new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan }
                ],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(2);
        result.Instructions[0].ShouldBeOfType<WelcomeInstruction>().Target.Name.ShouldBe("a");
        result.Instructions[1].ShouldBeOfType<WelcomeInstruction>().Target.Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that a simple literal assignment emits a single <see cref="AppointInstruction"/>.
    /// </summary>
    [Fact]
    public void Transform_AssignmentWithLiteralValue_EmitsSingleAppointInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "x", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "x",
                Value = new LiteralNode() { Value = 99, Type = LiteralType.Integer, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(2);
        AppointInstruction appoint = result.Instructions[1].ShouldBeOfType<AppointInstruction>();
        appoint.Target.Name.ShouldBe("x");
        appoint.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(99);
    }

    /// <summary>
    /// Tests that an identifier assignment emits a single <see cref="AppointInstruction"/> with a <see cref="VariableOperand"/>.
    /// </summary>
    [Fact]
    public void Transform_AssignmentWithIdentifierValue_EmitsSingleAppointInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "x", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "y", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "y",
                Value = new IdentifierNode() { Name = "x", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(3);
        AppointInstruction appoint = result.Instructions[2].ShouldBeOfType<AppointInstruction>();
        appoint.Target.Name.ShouldBe("y");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("x");
    }

    /// <summary>
    /// Tests that a programme return with a literal value emits a <see cref="FindInstruction"/> with a <see cref="LiteralOperand"/>.
    /// </summary>
    [Fact]
    public void Transform_ProgrammeReturnWithLiteral_EmitsFindInstruction()
    {
        ProgramNode program = Programme(
            new ProgrammeReturnNode()
            {
                Value = new LiteralNode() { Value = 0, Type = LiteralType.Integer, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(1);
        FindInstruction find = result.Instructions[0].ShouldBeOfType<FindInstruction>();
        find.Value.ShouldNotBeNull();
        find.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(0);
    }

    /// <summary>
    /// Tests that a programme return with an identifier emits a <see cref="FindInstruction"/> with a <see cref="VariableOperand"/>.
    /// </summary>
    [Fact]
    public void Transform_ProgrammeReturnWithIdentifier_EmitsFindInstructionWithVariable()
    {
        ProgramNode program = Programme(
            new ProgrammeReturnNode()
            {
                Value = new IdentifierNode() { Name = "exitCode", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        FindInstruction find = result.Instructions[0].ShouldBeOfType<FindInstruction>();
        find.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("exitCode");
    }

    /// <summary>
    /// Tests that each arithmetic Topsy Turvy operator maps to the correct <see cref="UtopIRArithmeticOperation"/> in the emitted instruction.
    /// </summary>
    /// <param name="topsyTurvyOperator">The Topsy Turvy operator to test.</param>
    /// <param name="expectedUtopirOperation">The expected UtopIR operation.</param>
    [Theory]
    [InlineData(Operator.Sum, UtopIRArithmeticOperation.Sum)]
    [InlineData(Operator.Difference, UtopIRArithmeticOperation.Diff)]
    [InlineData(Operator.Product, UtopIRArithmeticOperation.Prod)]
    [InlineData(Operator.Quotient, UtopIRArithmeticOperation.Quot)]
    [InlineData(Operator.Remainder, UtopIRArithmeticOperation.Rem)]
    [InlineData(Operator.Larger, UtopIRArithmeticOperation.Max)]
    [InlineData(Operator.Smaller, UtopIRArithmeticOperation.Min)]
    public void Transform_ArithmeticOperator_MapsToCorrectOperation(Operator topsyTurvyOperator, UtopIRArithmeticOperation expectedUtopirOperation)
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = topsyTurvyOperator,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        ArithmeticInstruction arithmetic = result.Instructions[3].ShouldBeOfType<ArithmeticInstruction>();
        arithmetic.Operation.ShouldBe(expectedUtopirOperation);
    }

    /// <summary>
    /// Tests that each arithmetic Topsy Turvy operator maps to the corresponding <c>.f</c>-suffixed <see cref="UtopIRArithmeticOperation"/> when the operands are floating-point.
    /// </summary>
    /// <param name="topsyTurvyOperator">The Topsy Turvy operator to test.</param>
    /// <param name="expectedUtopirOperation">The expected UtopIR floating-point operation.</param>
    [Theory]
    [InlineData(Operator.Sum, UtopIRArithmeticOperation.SumFloat)]
    [InlineData(Operator.Difference, UtopIRArithmeticOperation.DiffFloat)]
    [InlineData(Operator.Product, UtopIRArithmeticOperation.ProdFloat)]
    [InlineData(Operator.Quotient, UtopIRArithmeticOperation.QuotFloat)]
    [InlineData(Operator.Remainder, UtopIRArithmeticOperation.RemFloat)]
    [InlineData(Operator.Larger, UtopIRArithmeticOperation.MaxFloat)]
    [InlineData(Operator.Smaller, UtopIRArithmeticOperation.MinFloat)]
    public void Transform_ArithmeticOperatorOnFloats_MapsToFloatOperation(Operator topsyTurvyOperator, UtopIRArithmeticOperation expectedUtopirOperation)
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Double, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Double, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Double, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = topsyTurvyOperator,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        ArithmeticInstruction arithmetic = result.Instructions[3].ShouldBeOfType<ArithmeticInstruction>();
        arithmetic.Operation.ShouldBe(expectedUtopirOperation);
    }

    /// <summary>
    /// Tests that mixed integer and floating-point arithmetic widens the integer operand to the floating-point type and selects the floating-point operation group.
    /// </summary>
    [Fact]
    public void Transform_MixedIntegerAndFloatArithmetic_WidensIntegerAndUsesFloatOperation()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Double, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Double, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.Sum,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        WereInstruction were = result.Instructions[3].ShouldBeOfType<WereInstruction>();
        were.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        were.Type.ShouldBe(UtopIRType.Fathom);
        ArithmeticInstruction arithmetic = result.Instructions[4].ShouldBeOfType<ArithmeticInstruction>();
        arithmetic.Operation.ShouldBe(UtopIRArithmeticOperation.SumFloat);
        arithmetic.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(were.Target.Name);
        arithmetic.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that a <c>foot</c> declaration initialised with a double literal inserts a narrowing <see cref="WereInstruction"/> so the literal type matches the declared target type.
    /// </summary>
    [Fact]
    public void Transform_FootDeclarationWithDoubleLiteral_InsertsWereInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode()
            {
                Name = "Foot4",
                NameSpan = PlaceholderSpan,
                Type = LiteralType.Single,
                InitialValue = new LiteralNode() { Value = 8.5, Type = LiteralType.Double, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(3);
        result.Instructions[0].ShouldBeOfType<WelcomeInstruction>().Type.ShouldBe(UtopIRType.Foot);
        WereInstruction were = result.Instructions[1].ShouldBeOfType<WereInstruction>();
        were.Type.ShouldBe(UtopIRType.Foot);
        were.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(8.5);
        AppointInstruction appoint = result.Instructions[2].ShouldBeOfType<AppointInstruction>();
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(were.Target.Name);
    }

    /// <summary>
    /// Tests that a binary arithmetic expression on two variables emits an <see cref="ArithmeticInstruction"/> whose operands reference those variables and whose target is a temporary register named after the operation and operands.
    /// </summary>
    [Fact]
    public void Transform_ArithmeticOnTwoVariables_EmitsArithmeticInstructionWithCorrectTempName()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Peer1", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "Peer2", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.Sum,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "Peer1", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "Peer2", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(5);
        ArithmeticInstruction arithmetic = result.Instructions[3].ShouldBeOfType<ArithmeticInstruction>();
        arithmetic.Target.Name.ShouldBe("_sum_Peer1_Peer2");
        arithmetic.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("Peer1");
        arithmetic.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("Peer2");
        AppointInstruction appoint = result.Instructions[4].ShouldBeOfType<AppointInstruction>();
        appoint.Target.Name.ShouldBe("result");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("_sum_Peer1_Peer2");
    }

    /// <summary>
    /// Tests that each binary bitwise Topsy Turvy operator maps to the correct <see cref="UtopIRBitwiseOperation"/> in the emitted instruction.
    /// </summary>
    /// <param name="topsyTurvyOperator">The Topsy Turvy operator to test.</param>
    /// <param name="expectedUtopirOperation">The expected UtopIR bitwise operation.</param>
    [Theory]
    [InlineData(Operator.ChordOf, UtopIRBitwiseOperation.Chord)]
    [InlineData(Operator.HarmonyOf, UtopIRBitwiseOperation.Harmony)]
    [InlineData(Operator.DiscordOf, UtopIRBitwiseOperation.Discord)]
    public void Transform_BinaryBitwiseOperator_MapsToCorrectOperation(Operator topsyTurvyOperator, UtopIRBitwiseOperation expectedUtopirOperation)
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = topsyTurvyOperator,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        BitwiseInstruction bitwise = result.Instructions[3].ShouldBeOfType<BitwiseInstruction>();
        bitwise.Operation.ShouldBe(expectedUtopirOperation);
        bitwise.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        bitwise.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that <c>INVERSION OF</c> emits an <see cref="InvInstruction"/> with the operand and a temporary register named after the operation.
    /// </summary>
    [Fact]
    public void Transform_InversionOf_EmitsInvInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.InversionOf,
                    Arguments = [new IdentifierNode() { Name = "a", Span = PlaceholderSpan }],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        InvInstruction inv = result.Instructions[2].ShouldBeOfType<InvInstruction>();
        inv.Target.Name.ShouldBe("_inv_a");
        inv.Operand.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        result.Instructions[3].ShouldBeOfType<AppointInstruction>()
            .Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("_inv_a");
    }

    /// <summary>
    /// Tests that the unary <c>TRANSPOSITION UP</c> operator lowers to a binary <see cref="BitwiseInstruction"/> with a shift amount of one matching the operand type.
    /// </summary>
    [Fact]
    public void Transform_TranspositionUpOnChancellor_EmitsBitwiseInstructionWithMatchingShiftAmount()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.TranspositionUp,
                    Arguments = [new IdentifierNode() { Name = "a", Span = PlaceholderSpan }],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        BitwiseInstruction shift = result.Instructions[2].ShouldBeOfType<BitwiseInstruction>();
        shift.Operation.ShouldBe(UtopIRBitwiseOperation.TransUp);
        shift.Target.Name.ShouldBe("_transup_a_1");
        shift.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        shift.Operand2.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(1L);
    }

    /// <summary>
    /// Tests that the unary <c>TRANSPOSITION DOWN</c> operator lowers to a binary <see cref="BitwiseInstruction"/> with a shift amount of one.
    /// </summary>
    [Fact]
    public void Transform_TranspositionDown_EmitsBitwiseInstructionWithShiftAmountOfOne()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.TranspositionDown,
                    Arguments = [new IdentifierNode() { Name = "a", Span = PlaceholderSpan }],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        BitwiseInstruction shift = result.Instructions[2].ShouldBeOfType<BitwiseInstruction>();
        shift.Operation.ShouldBe(UtopIRBitwiseOperation.TransDown);
        shift.Target.Name.ShouldBe("_transdown_a_1");
        shift.Operand2.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <c>TRANSPOSITION UP</c> operator with a <c>BY</c> clause lowers to a <see cref="BitwiseInstruction"/> using the transformed shift-amount expression instead of the synthesised literal 1.
    /// </summary>
    [Fact]
    public void Transform_TranspositionUpWithByClause_EmitsBitwiseInstructionWithTransformedShiftAmount()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "shiftAmount", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.TranspositionUp,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "shiftAmount", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        BitwiseInstruction shift = result.Instructions[3].ShouldBeOfType<BitwiseInstruction>();
        shift.Operation.ShouldBe(UtopIRBitwiseOperation.TransUp);
        shift.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        shift.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("shiftAmount");
    }

    /// <summary>
    /// Tests that a <c>TRANSPOSITION UP</c> <c>BY</c> clause operand of a narrower integer type than the shifted value is cast to match via an inserted <see cref="WereInstruction"/>.
    /// </summary>
    [Fact]
    public void Transform_TranspositionUpWithByClauseOfNarrowerType_InsertsWereInstructionForShiftAmount()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "shiftAmount", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.TranspositionUp,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "shiftAmount", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        WereInstruction were = result.Instructions[3].ShouldBeOfType<WereInstruction>();
        were.Type.ShouldBe(UtopIRType.Chancellor);
        were.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("shiftAmount");
        BitwiseInstruction shift = result.Instructions[4].ShouldBeOfType<BitwiseInstruction>();
        shift.Operation.ShouldBe(UtopIRBitwiseOperation.TransUp);
        shift.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        shift.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(were.Target.Name);
    }

    /// <summary>
    /// Tests that mixed-width binary bitwise operands widen the narrower operand before the <see cref="BitwiseInstruction"/> is emitted.
    /// </summary>
    [Fact]
    public void Transform_MixedTypeBitwise_WidensNarrowerOperand()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.ChordOf,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        WereInstruction were = result.Instructions[3].ShouldBeOfType<WereInstruction>();
        were.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        were.Type.ShouldBe(UtopIRType.Chancellor);
        BitwiseInstruction bitwise = result.Instructions[4].ShouldBeOfType<BitwiseInstruction>();
        bitwise.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(were.Target.Name);
        bitwise.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that each comparison Topsy Turvy operator maps to the correct <see cref="UtopIRComparisonOperation"/> in the emitted instruction, and that the target is always declared as <see cref="UtopIRType.Decree"/> regardless of the integer operand type.
    /// </summary>
    /// <param name="topsyTurvyOperator">The Topsy Turvy operator to test.</param>
    /// <param name="expectedUtopirOperation">The expected UtopIR operation.</param>
    [Theory]
    [InlineData(Operator.Alike, UtopIRComparisonOperation.Alike)]
    [InlineData(Operator.Unlike, UtopIRComparisonOperation.Unlike)]
    [InlineData(Operator.PreAdamite, UtopIRComparisonOperation.PreAdam)]
    [InlineData(Operator.LowerDegree, UtopIRComparisonOperation.LowerDeg)]
    public void Transform_ComparisonOperator_MapsToCorrectOperation(Operator topsyTurvyOperator, UtopIRComparisonOperation expectedUtopirOperation)
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = topsyTurvyOperator,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        ComparisonInstruction comparison = result.Instructions[3].ShouldBeOfType<ComparisonInstruction>();
        comparison.Operation.ShouldBe(expectedUtopirOperation);
    }

    /// <summary>
    /// Tests that each comparison Topsy Turvy operator maps to the corresponding <c>.f</c>-suffixed <see cref="UtopIRComparisonOperation"/> when the operands are floating-point.
    /// </summary>
    /// <param name="topsyTurvyOperator">The Topsy Turvy operator to test.</param>
    /// <param name="expectedUtopirOperation">The expected UtopIR floating-point operation.</param>
    [Theory]
    [InlineData(Operator.Alike, UtopIRComparisonOperation.AlikeFloat)]
    [InlineData(Operator.Unlike, UtopIRComparisonOperation.UnlikeFloat)]
    [InlineData(Operator.PreAdamite, UtopIRComparisonOperation.PreAdamFloat)]
    [InlineData(Operator.LowerDegree, UtopIRComparisonOperation.LowerDegFloat)]
    public void Transform_ComparisonOperatorOnFloats_MapsToFloatOperation(Operator topsyTurvyOperator, UtopIRComparisonOperation expectedUtopirOperation)
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Double, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Double, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = topsyTurvyOperator,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        ComparisonInstruction comparison = result.Instructions[3].ShouldBeOfType<ComparisonInstruction>();
        comparison.Operation.ShouldBe(expectedUtopirOperation);
    }

    /// <summary>
    /// Tests that a comparison between mismatched integer widths widens the narrower operand into a temporary register before comparing, matching the widening behaviour of <c>TransformArithmetic</c>.
    /// </summary>
    [Fact]
    public void Transform_MixedTypeComparison_WidensNarrowerOperand()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.PreAdamite,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        WereInstruction were = result.Instructions[3].ShouldBeOfType<WereInstruction>();
        were.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        were.Type.ShouldBe(UtopIRType.Chancellor);
        ComparisonInstruction comparison = result.Instructions[4].ShouldBeOfType<ComparisonInstruction>();
        comparison.Operation.ShouldBe(UtopIRComparisonOperation.PreAdam);
        comparison.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(were.Target.Name);
        comparison.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that a comparison temporary register is always recorded with a declared type of <see cref="UtopIRType.Decree"/>, not the widened operand type, by nesting the comparison as an operand of an outer comparison and confirming no spurious <see cref="WereInstruction"/> is inserted for it.
    /// </summary>
    [Fact]
    public void Transform_NestedComparison_RecordsDecreeTypeForComparisonTarget()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.Alike,
                    Arguments =
                    [
                        new PrefixExpressionNode()
                        {
                            Operator = Operator.Alike,
                            Arguments =
                            [
                                new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                                new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                            ],
                            Span = PlaceholderSpan
                        },
                        new IdentifierNode() { Name = "flag", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        ComparisonInstruction innerComparison = result.Instructions[4].ShouldBeOfType<ComparisonInstruction>();
        ComparisonInstruction outerComparison = result.Instructions[5].ShouldBeOfType<ComparisonInstruction>();
        outerComparison.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(innerComparison.Target.Name);
        outerComparison.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("flag");
    }

    /// <summary>
    /// Tests that each binary logical Topsy Turvy operator maps to the correct <see cref="UtopIRLogicalOperation"/> in the emitted instruction, with no widening of the already-<c>decree</c> operands.
    /// </summary>
    /// <param name="topsyTurvyOperator">The Topsy Turvy operator to test.</param>
    /// <param name="expectedUtopirOperation">The expected UtopIR operation.</param>
    [Theory]
    [InlineData(Operator.Both, UtopIRLogicalOperation.Both)]
    [InlineData(Operator.Either, UtopIRLogicalOperation.Either)]
    public void Transform_LogicalOperator_MapsToCorrectOperation(Operator topsyTurvyOperator, UtopIRLogicalOperation expectedUtopirOperation)
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = topsyTurvyOperator,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        LogicalInstruction logical = result.Instructions[3].ShouldBeOfType<LogicalInstruction>();
        logical.Operation.ShouldBe(expectedUtopirOperation);
        logical.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        logical.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that <c>HARDLY EVER</c> emits a <see cref="HardlyInstruction"/> with the operand and a temporary register named after the operation.
    /// </summary>
    [Fact]
    public void Transform_HardlyEver_EmitsHardlyInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.HardlyEver,
                    Arguments = [new IdentifierNode() { Name = "a", Span = PlaceholderSpan }],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        HardlyInstruction hardly = result.Instructions[2].ShouldBeOfType<HardlyInstruction>();
        hardly.Target.Name.ShouldBe("_hardly_a");
        hardly.Operand.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        result.Instructions[3].ShouldBeOfType<AppointInstruction>()
            .Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("_hardly_a");
    }

    /// <summary>
    /// Tests that a <c>stitch</c> declaration initialised with a character literal emits <c>welcome</c> then <c>appoint</c> with no intermediate cast.
    /// </summary>
    [Fact]
    public void Transform_StitchDeclarationWithCharLiteral_EmitsWelcomeThenAppoint()
    {
        ProgramNode program = Programme(
            new DeclarationNode()
            {
                Name = "Letter",
                NameSpan = PlaceholderSpan,
                Type = LiteralType.Char,
                InitialValue = new LiteralNode() { Value = 'A', Type = LiteralType.Char, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(2);
        result.Instructions[0].ShouldBeOfType<WelcomeInstruction>().Type.ShouldBe(UtopIRType.Stitch);
        AppointInstruction appoint = result.Instructions[1].ShouldBeOfType<AppointInstruction>();
        appoint.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe('A');
    }

    /// <summary>
    /// Tests that an <c>AS IT WERE</c> cast to <c>STITCH</c> emits a <see cref="WereInstruction"/> with the <see cref="UtopIRType.Stitch"/> destination type.
    /// </summary>
    [Fact]
    public void Transform_ExpressionCastToStitch_EmitsWereInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "n", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "c", NameSpan = PlaceholderSpan, Type = LiteralType.Char, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "c",
                Value = new ExpressionCastNode()
                {
                    Expression = new IdentifierNode() { Name = "n", Span = PlaceholderSpan },
                    NewType = LiteralType.Char,
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        WereInstruction were = result.Instructions[2].ShouldBeOfType<WereInstruction>();
        were.Type.ShouldBe(UtopIRType.Stitch);
        were.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("n");
        AppointInstruction appoint = result.Instructions[3].ShouldBeOfType<AppointInstruction>();
        appoint.Target.Name.ShouldBe("c");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(were.Target.Name);
    }

    /// <summary>
    /// Tests that an <c>AS IT WERE</c> cast emits a <see cref="WereInstruction"/> with the mapped destination type.
    /// </summary>
    [Fact]
    public void Transform_ExpressionCast_EmitsWereInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new ExpressionCastNode()
                {
                    Expression = new IdentifierNode() { Name = "Lords", Span = PlaceholderSpan },
                    NewType = LiteralType.Long,
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(3);
        WereInstruction were = result.Instructions[1].ShouldBeOfType<WereInstruction>();
        were.Type.ShouldBe(UtopIRType.Chancellor);
        were.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("Lords");
        AppointInstruction appoint = result.Instructions[2].ShouldBeOfType<AppointInstruction>();
        appoint.Target.Name.ShouldBe("result");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(were.Target.Name);
    }

    /// <summary>
    /// Tests that <c>PEER + CHANCELLOR</c> widens the narrower <c>peer</c> operand, regardless of which side it appears on.
    /// </summary>
    [Theory]
    [InlineData(LiteralType.Integer, LiteralType.Long, "a")]
    [InlineData(LiteralType.Long, LiteralType.Integer, "b")]
    public void Transform_MixedTypeArithmetic_WidensNarrowerOperandRegardlessOfPosition(LiteralType aType, LiteralType bType, string narrowerOperandName)
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = aType, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = bType, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.Sum,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        WereInstruction were = result.Instructions[3].ShouldBeOfType<WereInstruction>();
        were.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(narrowerOperandName);
        were.Type.ShouldBe(UtopIRType.Chancellor);
        ArithmeticInstruction arithmetic = result.Instructions[4].ShouldBeOfType<ArithmeticInstruction>();
        (arithmetic.Operand1 as VariableOperand)!.Variable.Name.ShouldBe(narrowerOperandName == "a" ? were.Target.Name : "a");
        (arithmetic.Operand2 as VariableOperand)!.Variable.Name.ShouldBe(narrowerOperandName == "b" ? were.Target.Name : "b");
    }

    /// <summary>
    /// Tests that same-typed arithmetic operands emit no <see cref="WereInstruction"/>.
    /// </summary>
    [Fact]
    public void Transform_SameTypeArithmetic_EmitsNoWereInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.Sum,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.ShouldNotContain(instruction => instruction is WereInstruction);
        ArithmeticInstruction arithmetic = result.Instructions[3].ShouldBeOfType<ArithmeticInstruction>();
        arithmetic.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("a");
        arithmetic.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("b");
    }

    /// <summary>
    /// Tests that a binary arithmetic expression on two literals emits an <see cref="ArithmeticInstruction"/> whose target temporary name embeds the literal values.
    /// </summary>
    [Fact]
    public void Transform_ArithmeticOnTwoLiterals_EmbedLiteralValuesInTempName()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode
                {
                    Operator = Operator.Sum,
                    Arguments =
                    [
                        new LiteralNode() { Value = 3, Type = LiteralType.Integer, Span = PlaceholderSpan },
                        new LiteralNode() { Value = 4, Type = LiteralType.Integer, Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        ArithmeticInstruction arithmetic = result.Instructions[1].ShouldBeOfType<ArithmeticInstruction>();
        arithmetic.Target.Name.ShouldBe("_sum_3_4");
    }

    /// <summary>
    /// Tests that a nested arithmetic expression such as <c>SUM OF PRODUCT OF a AND b AND c</c> is flattened into two sequential arithmetic instructions with the inner temporary register feeding the outer one, matching the example in the UtopIR specification.
    /// </summary>
    [Fact]
    public void Transform_NestedArithmetic_FlattensIntoSequentialInstructions()
    {
        // SUM OF PRODUCT OF a AND b AND c
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "a", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "b", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "c", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new AssignmentNode()
            {
                Target = "result",
                Value = new PrefixExpressionNode()
                {
                    Operator = Operator.Sum,
                    Arguments =
                    [
                        new PrefixExpressionNode()
                        {
                            Operator = Operator.Product,
                            Arguments =
                            [
                                new IdentifierNode { Name = "a", Span = PlaceholderSpan },
                                new IdentifierNode { Name = "b", Span = PlaceholderSpan }
                            ],
                            Span = PlaceholderSpan
                        },
                        new IdentifierNode() { Name = "c", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(7);
        ArithmeticInstruction prod = result.Instructions[4].ShouldBeOfType<ArithmeticInstruction>();
        prod.Operation.ShouldBe(UtopIRArithmeticOperation.Prod);
        prod.Target.Name.ShouldBe("_prod_a_b");
        ArithmeticInstruction sum = result.Instructions[5].ShouldBeOfType<ArithmeticInstruction>();
        sum.Operation.ShouldBe(UtopIRArithmeticOperation.Sum);
        sum.Target.Name.ShouldBe("_sum__prod_a_b_c");
        sum.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("_prod_a_b");
        sum.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("c");
        result.Instructions[6].ShouldBeOfType<AppointInstruction>()
            .Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("_sum__prod_a_b_c");
    }

    /// <summary>
    /// Tests that each supported <see cref="LiteralType"/> maps to the correct <see cref="UtopIRType"/> in a <see cref="WelcomeInstruction"/>.
    /// </summary>
    /// <param name="topsyTurvyType">The Topsy Turvy literal type.</param>
    /// <param name="expectedUtopirType">The expected UtopIR type.</param>
    [Theory]
    [InlineData(LiteralType.Integer, UtopIRType.Peer)]
    [InlineData(LiteralType.Long, UtopIRType.Chancellor)]
    [InlineData(LiteralType.Short, UtopIRType.Pirate)]
    [InlineData(LiteralType.SignedByte, UtopIRType.SausageRoll)]
    [InlineData(LiteralType.UnsignedInteger, UtopIRType.StandingPeer)]
    [InlineData(LiteralType.UnsignedLong, UtopIRType.StandingChancellor)]
    [InlineData(LiteralType.UnsignedShort, UtopIRType.StandingPirate)]
    [InlineData(LiteralType.Byte, UtopIRType.StandingSausageRoll)]
    [InlineData(LiteralType.Double, UtopIRType.Fathom)]
    [InlineData(LiteralType.Single, UtopIRType.Foot)]
    [InlineData(LiteralType.Boolean, UtopIRType.Decree)]
    [InlineData(LiteralType.Char, UtopIRType.Stitch)]
    [InlineData(LiteralType.String, UtopIRType.Yarn)]
    public void Transform_DeclarationType_MapsToCorrectUtopIRType(LiteralType topsyTurvyType, UtopIRType expectedUtopirType)
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "v", NameSpan = PlaceholderSpan, Type = topsyTurvyType, Span = PlaceholderSpan });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions[0].ShouldBeOfType<WelcomeInstruction>().Type.ShouldBe(expectedUtopirType);
    }

    /// <summary>
    /// Tests that an unsupported expression throws <see cref="NotSupportedException"/> rather than silently producing wrong output.
    /// </summary>
    [Fact]
    public void Transform_UnsupportedExpression_ThrowsNotSupportedException()
    {
        ProgramNode program = Programme(
            new AssignmentNode()
            {
                Target = "x",
                Value = new PrefixExpressionNode
                {
                    Operator = Operator.WovenOf,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "a", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "b", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            });

        Should.Throw<NotSupportedException>(() => this.transformer.Transform(program));
    }

    /// <summary>
    /// Tests that a complete programme with two declared variables, arithmetic combining them, and a programme return produces the expected UtopIR instruction sequence.
    /// </summary>
    [Fact]
    public void Transform_FullProgrammeWithArithmeticAndReturn_ProducesExpectedInstructions()
    {
        // PRAY WELCOME Peer1 AS A PEER BEING 42
        // PRAY WELCOME Peer2 AS A PEER BEING 30
        // PRAY WELCOME PeerResult AS A PEER BEING SUM OF Peer1 AND Peer2
        // AND SO I FIND PeerResult
        ProgramNode program = Programme(
            new DeclarationNode()
            {
                Name = "Peer1", NameSpan = PlaceholderSpan, Type = LiteralType.Integer,
                InitialValue = new LiteralNode() { Value = 42, Type = LiteralType.Integer, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            },
            new DeclarationNode()
            {
                Name = "Peer2", NameSpan = PlaceholderSpan, Type = LiteralType.Integer,
                InitialValue = new LiteralNode() { Value = 30, Type = LiteralType.Integer, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            },
            new DeclarationNode()
            {
                Name = "PeerResult", NameSpan = PlaceholderSpan, Type = LiteralType.Integer,
                InitialValue = new PrefixExpressionNode()
                {
                    Operator = Operator.Sum,
                    Arguments =
                    [
                        new IdentifierNode() { Name = "Peer1", Span = PlaceholderSpan },
                        new IdentifierNode() { Name = "Peer2", Span = PlaceholderSpan }
                    ],
                    Span = PlaceholderSpan
                },
                Span = PlaceholderSpan
            },
            new ProgrammeReturnNode()
            {
                Value = new IdentifierNode() { Name = "PeerResult", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        // £Peer1 = welcome peer
        // £Peer1 = appoint 42
        // £Peer2 = welcome peer
        // £Peer2 = appoint 30
        // £PeerResult = welcome peer
        // £_sum_Peer1_Peer2 = sum £Peer1, £Peer2
        // £PeerResult = appoint £_sum_Peer1_Peer2
        // find £PeerResult
        result.Instructions.Count.ShouldBe(8);
        result.Instructions[0].ShouldBeOfType<WelcomeInstruction>().Target.Name.ShouldBe("Peer1");
        result.Instructions[1].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(42);
        result.Instructions[2].ShouldBeOfType<WelcomeInstruction>().Target.Name.ShouldBe("Peer2");
        result.Instructions[3].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(30);
        result.Instructions[4].ShouldBeOfType<WelcomeInstruction>().Target.Name.ShouldBe("PeerResult");
        ArithmeticInstruction sum = result.Instructions[5].ShouldBeOfType<ArithmeticInstruction>();
        sum.Operation.ShouldBe(UtopIRArithmeticOperation.Sum);
        sum.Target.Name.ShouldBe("_sum_Peer1_Peer2");
        result.Instructions[6].ShouldBeOfType<AppointInstruction>()
            .Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("_sum_Peer1_Peer2");
        result.Instructions[7].ShouldBeOfType<FindInstruction>()
            .Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("PeerResult");
    }

    /// <summary>
    /// Tests that an If/Else-If/Else conditional produces the exact expected instruction and
    /// label sequence.
    /// </summary>
    [Fact]
    public void Transform_ConditionalWithElseIfAndElse_ReproducesSpecificationExample()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Peer1", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(42), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "Peer2", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(23), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "PeerResult", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new ConditionalNode()
            {
                Condition = Prefix(Operator.PreAdamite, Identifier("Peer1"), Identifier("Peer2")),
                TrueBlock = [Assign("PeerResult", IntLiteral(1))],
                ElseIfs = [new ElseIfBranch(Prefix(Operator.Alike, Identifier("Peer1"), Identifier("Peer2")), [Assign("PeerResult", IntLiteral(0))])],
                ElseBlock = [Assign("PeerResult", IntLiteral(-1))],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(19);
        result.Instructions[5].ShouldBeOfType<ComparisonInstruction>().Operation.ShouldBe(UtopIRComparisonOperation.PreAdam);
        result.Instructions[6].ShouldBeOfType<ComparisonInstruction>().Operation.ShouldBe(UtopIRComparisonOperation.Alike);
        result.Instructions[7].ShouldBeOfType<SailAlikeInstruction>().Label.Name.ShouldBe("T1QS_PREADAM_PEER1_PEER2");
        result.Instructions[8].ShouldBeOfType<SailAlikeInstruction>().Label.Name.ShouldBe("T1OIN_ALIKE_PEER1_PEER2");
        result.Instructions[9].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("T1O");
        result.Instructions[10].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("T1QS_PREADAM_PEER1_PEER2");
        result.Instructions[11].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(1);
        result.Instructions[12].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("T1SMFT");
        result.Instructions[13].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("T1OIN_ALIKE_PEER1_PEER2");
        result.Instructions[14].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(0);
        result.Instructions[15].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("T1SMFT");
        result.Instructions[16].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("T1O");
        result.Instructions[17].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(-1);
        result.Instructions[18].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("T1SMFT");
    }

    /// <summary>
    /// Tests that a conditional with no else block branches its fallback directly to the closing
    /// label, with no else label emitted at all.
    /// </summary>
    [Fact]
    public void Transform_ConditionalWithNoElseBlock_FallsThroughToClosingLabel()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "A", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "R", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new ConditionalNode()
            {
                Condition = Identifier("A"),
                TrueBlock = [Assign("R", IntLiteral(1))],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(8);
        result.Instructions[2].ShouldBeOfType<SailAlikeInstruction>().Label.Name.ShouldBe("T1QS_A");
        result.Instructions[3].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("T1SMFT");
        result.Instructions.OfType<LabelInstruction>().Select(l => l.Name.Name).ShouldNotContain(name => name == "T1O");
    }

    /// <summary>
    /// Tests that a ternary expression produces the exact expected instruction and label
    /// sequence.
    /// </summary>
    /// <remarks>
    /// Follows the generic expression-transform contract: both branches <c>appoint</c> into a
    /// shared temporary register that is declared with <c>welcome</c> before the branch, with the enclosing
    /// assignment appointing from that register afterwards.
    /// </remarks>
    [Fact]
    public void Transform_Ternary_ReproducesSpecificationExample()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Peer1", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(42), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "Peer2", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(23), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "PeerResult", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            Assign("PeerResult", new TernaryExpressionNode()
            {
                TrueValue = IntLiteral(1),
                Condition = Prefix(Operator.Alike, Identifier("Peer1"), Identifier("Peer2")),
                FalseValue = IntLiteral(0),
                Span = PlaceholderSpan
            }));

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(16);
        ComparisonInstruction comparison = result.Instructions[5].ShouldBeOfType<ComparisonInstruction>();
        UtopIRVariable sharedTemporary = result.Instructions[6].ShouldBeOfType<WelcomeInstruction>().Target;
        sharedTemporary.Name.ShouldNotBe(comparison.Target.Name);
        result.Instructions[7].ShouldBeOfType<SailAlikeInstruction>().Label.Name.ShouldBe("T1QS_ALIKE_PEER1_PEER2");
        result.Instructions[8].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("T1O");
        result.Instructions[9].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("T1QS_ALIKE_PEER1_PEER2");
        AppointInstruction trueAppoint = result.Instructions[10].ShouldBeOfType<AppointInstruction>();
        trueAppoint.Target.Name.ShouldBe(sharedTemporary.Name);
        trueAppoint.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(1);
        result.Instructions[11].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("T1SMFT");
        result.Instructions[12].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("T1O");
        AppointInstruction falseAppoint = result.Instructions[13].ShouldBeOfType<AppointInstruction>();
        falseAppoint.Target.Name.ShouldBe(sharedTemporary.Name);
        falseAppoint.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(0);
        result.Instructions[14].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("T1SMFT");
        AppointInstruction finalAppoint = result.Instructions[15].ShouldBeOfType<AppointInstruction>();
        finalAppoint.Target.Name.ShouldBe("PeerResult");
        finalAppoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(sharedTemporary.Name);
    }

    /// <summary>
    /// Tests that a guard clause produces the exact expected instruction and label sequence.
    /// </summary>
    [Fact]
    public void Transform_Guard_ReproducesSpecificationExample()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Peer1", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(42), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "Peer2", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(23), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "PeerResult", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new GuardNode()
            {
                Condition = Prefix(Operator.Alike, Identifier("Peer1"), Identifier("Peer2")),
                ElseBlock = [Assign("PeerResult", IntLiteral(0))],
                Span = PlaceholderSpan
            },
            Assign("PeerResult", IntLiteral(1)));

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(12);
        result.Instructions[5].ShouldBeOfType<ComparisonInstruction>().Operation.ShouldBe(UtopIRComparisonOperation.Alike);
        result.Instructions[6].ShouldBeOfType<SailUnlikeInstruction>().Label.Name.ShouldBe("G1O");
        result.Instructions[7].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("G1UO");
        result.Instructions[8].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("G1O");
        result.Instructions[9].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(0);
        result.Instructions[10].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("G1UO");
        result.Instructions[11].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(1);
        result.Instructions.OfType<LabelInstruction>().Select(l => l.Name.Name).ShouldNotContain(name => name.StartsWith("G1QS"));
    }

    /// <summary>
    /// Tests that a switch block with a fallthrough case and a default block produces the exact
    /// expected instruction and label sequence.
    /// </summary>
    [Fact]
    public void Transform_SwitchWithFallthroughAndDefault_ReproducesSpecificationExample()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Peer1", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(10), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "PeerResult", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new SwitchNode()
            {
                Expression = Identifier("Peer1"),
                Cases =
                [
                    new SwitchCase(10, [Assign("PeerResult", IntLiteral(1)), new BreakNode() { Span = PlaceholderSpan }]),
                    new SwitchCase(20, []),
                    new SwitchCase(40, [Assign("PeerResult", IntLiteral(2)), new BreakNode() { Span = PlaceholderSpan }]),
                    new SwitchCase(100, [Assign("PeerResult", IntLiteral(3))])
                ],
                DefaultBlock = [Assign("PeerResult", IntLiteral(-1))],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(24);
        result.Instructions[4].ShouldBeOfType<SailAlikeInstruction>().Label.Name.ShouldBe("C1AS_10");
        result.Instructions[6].ShouldBeOfType<SailAlikeInstruction>().Label.Name.ShouldBe("C1AS_20");
        result.Instructions[8].ShouldBeOfType<SailAlikeInstruction>().Label.Name.ShouldBe("C1AS_40");
        result.Instructions[10].ShouldBeOfType<SailAlikeInstruction>().Label.Name.ShouldBe("C1AS_100");
        result.Instructions[11].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("C1FAIL");
        result.Instructions[12].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("C1AS_10");
        result.Instructions[13].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(1);
        result.Instructions[14].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("C1NCBMS");
        result.Instructions[15].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("C1AS_20");
        result.Instructions[16].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("C1AS_40");
        result.Instructions[17].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(2);
        result.Instructions[18].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("C1NCBMS");
        result.Instructions[19].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("C1AS_100");
        result.Instructions[20].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(3);
        result.Instructions[21].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("C1FAIL");
        result.Instructions[22].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(-1);
        result.Instructions[23].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("C1NCBMS");
    }

    /// <summary>
    /// Tests that an infinite loop produces the exact expected instruction and label sequence.
    /// </summary>
    [Fact]
    public void Transform_InfiniteLoop_ReproducesSpecificationExample()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Toggle", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Infinite,
                Body = [Assign("Toggle", Prefix(Operator.HardlyEver, Identifier("Toggle")))],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(7);
        result.Instructions[2].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1");
        result.Instructions[3].ShouldBeOfType<HardlyInstruction>();
        result.Instructions[4].ShouldBeOfType<AppointInstruction>().Target.Name.ShouldBe("Toggle");
        result.Instructions[5].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("L1");
        result.Instructions[6].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1TTE");
    }

    /// <summary>
    /// Tests that a Whilst loop produces the exact expected instruction and label sequence.
    /// </summary>
    [Fact]
    public void Transform_WhilstLoop_ReproducesSpecificationExample()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Counter", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(1), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "Total", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(0), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Whilst,
                Condition = Prefix(Operator.LowerDegree, Identifier("Counter"), IntLiteral(10)),
                Body =
                [
                    Assign("Total", Prefix(Operator.Sum, Identifier("Total"), Identifier("Counter"))),
                    Assign("Counter", Prefix(Operator.Sum, Identifier("Counter"), IntLiteral(2)))
                ],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(13);
        result.Instructions[4].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1W_LOWERDEG_COUNTER_10");
        result.Instructions[5].ShouldBeOfType<ComparisonInstruction>().Operation.ShouldBe(UtopIRComparisonOperation.LowerDeg);
        result.Instructions[6].ShouldBeOfType<SailUnlikeInstruction>().Label.Name.ShouldBe("L1WTTE");
        result.Instructions[7].ShouldBeOfType<ArithmeticInstruction>().Operation.ShouldBe(UtopIRArithmeticOperation.Sum);
        result.Instructions[8].ShouldBeOfType<AppointInstruction>().Target.Name.ShouldBe("Total");
        result.Instructions[9].ShouldBeOfType<ArithmeticInstruction>().Operation.ShouldBe(UtopIRArithmeticOperation.Sum);
        result.Instructions[10].ShouldBeOfType<AppointInstruction>().Target.Name.ShouldBe("Counter");
        result.Instructions[11].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("L1W_LOWERDEG_COUNTER_10");
        result.Instructions[12].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1WTTE");
    }

    /// <summary>
    /// Tests that an ascending loop produces the exact expected instruction and label sequence.
    /// </summary>
    /// <remarks>
    /// The condition is checked before the first iteration (a <c>sailalike</c> exit), and a
    /// distinct <c>OM</c> label separates the body from the increment.
    /// </remarks>
    [Fact]
    public void Transform_AscendingLoop_ReproducesCorrectedSpecificationExample()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Counter", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(1), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "Total", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(0), Span = PlaceholderSpan },
            new LoopNode()
            {
                Label = "Incrementer",
                Type = LoopType.Ascending,
                Condition = Prefix(Operator.PreAdamite, Identifier("Counter"), IntLiteral(10)),
                LoopVariable = "Counter",
                Body = [Assign("Total", Prefix(Operator.Sum, Identifier("Total"), Identifier("Counter")))],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(14);
        result.Instructions[4].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1ASC_INCREMENTER_PREADAM_COUNTER_10");
        result.Instructions[5].ShouldBeOfType<ComparisonInstruction>().Operation.ShouldBe(UtopIRComparisonOperation.PreAdam);
        result.Instructions[6].ShouldBeOfType<SailAlikeInstruction>().Label.Name.ShouldBe("L1ASCTTE");
        result.Instructions[7].ShouldBeOfType<ArithmeticInstruction>().Operation.ShouldBe(UtopIRArithmeticOperation.Sum);
        result.Instructions[8].ShouldBeOfType<AppointInstruction>().Target.Name.ShouldBe("Total");
        result.Instructions[9].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1ASCOM");
        ArithmeticInstruction step = result.Instructions[10].ShouldBeOfType<ArithmeticInstruction>();
        step.Operation.ShouldBe(UtopIRArithmeticOperation.Sum);
        step.Operand2.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(1);
        result.Instructions[11].ShouldBeOfType<AppointInstruction>().Target.Name.ShouldBe("Counter");
        result.Instructions[12].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("L1ASC_INCREMENTER_PREADAM_COUNTER_10");
        result.Instructions[13].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1ASCTTE");
    }

    /// <summary>
    /// Tests that a descending loop uses <c>diff</c> for its step and the <c>DESC</c> label forms,
    /// distinguishing it from the ascending case.
    /// </summary>
    [Fact]
    public void Transform_DescendingLoop_UsesDiffAndDescendingLabels()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Counter", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(10), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Descending,
                Condition = Prefix(Operator.LowerDegree, Identifier("Counter"), IntLiteral(0)),
                LoopVariable = "Counter",
                Body = [],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(10);
        result.Instructions[2].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1DESC_LOWERDEG_COUNTER_0");
        result.Instructions[4].ShouldBeOfType<SailAlikeInstruction>().Label.Name.ShouldBe("L1DESCTTE");
        result.Instructions[5].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1DESCOM");
        result.Instructions[6].ShouldBeOfType<ArithmeticInstruction>().Operation.ShouldBe(UtopIRArithmeticOperation.Diff);
        result.Instructions[8].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("L1DESC_LOWERDEG_COUNTER_0");
        result.Instructions[9].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1DESCTTE");
    }

    /// <summary>
    /// Tests that the explicit <c>BY</c> step expression of an ascending loop is transformed
    /// dynamically each time and not treated as a compile-time constant.
    /// </summary>
    [Fact]
    public void Transform_AscendingLoop_WithExplicitStep_TransformsStepExpression()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Counter", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(0), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "StepAmount", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(3), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Ascending,
                Condition = Prefix(Operator.PreAdamite, Identifier("Counter"), IntLiteral(20)),
                LoopVariable = "Counter",
                Step = Identifier("StepAmount"),
                Body = [],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        ArithmeticInstruction step = result.Instructions.OfType<ArithmeticInstruction>().Single();
        step.Operation.ShouldBe(UtopIRArithmeticOperation.Sum);
        step.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("StepAmount");
    }

    /// <summary>
    /// Tests that an ascending loop with no explicit step defaults to a literal <c>1</c> boxed as
    /// the declared type of the loop variable itself.
    /// </summary>
    [Fact]
    public void Transform_AscendingLoop_WithNoStepAndChancellorLoopVariable_DefaultsToLongOne()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Counter", NameSpan = PlaceholderSpan, Type = LiteralType.Long, InitialValue = LongLiteral(1L), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Ascending,
                Condition = Prefix(Operator.PreAdamite, Identifier("Counter"), LongLiteral(10L)),
                LoopVariable = "Counter",
                Body = [],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        ArithmeticInstruction step = result.Instructions.OfType<ArithmeticInstruction>().Single();
        LiteralOperand stepOperand = step.Operand2.ShouldBeOfType<LiteralOperand>();
        stepOperand.Value.ShouldBeOfType<long>();
        stepOperand.Value.ShouldBe(1L);
    }

    /// <summary>
    /// Tests that <c>THAT WILL DO.</c> inside the body of a Whilst loop sails to the closing
    /// label of the loop itself.
    /// </summary>
    [Fact]
    public void Transform_BreakInsideWhilstLoop_SailsToLoopClosingLabel()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Whilst,
                Condition = Identifier("Flag"),
                Body = [new BreakNode() { Span = PlaceholderSpan }],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        string closingLabel = result.Instructions[3].ShouldBeOfType<SailUnlikeInstruction>().Label.Name;
        result.Instructions[4].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe(closingLabel);
    }

    /// <summary>
    /// Tests that <c>ONCE MORE.</c> inside the body of a Whilst loop sails to the opening
    /// (condition-check) label of the loop itself.
    /// </summary>
    [Fact]
    public void Transform_ContinueInsideWhilstLoop_SailsToLoopOpeningLabel()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Whilst,
                Condition = Identifier("Flag"),
                Body = [new ContinueNode() { Span = PlaceholderSpan }],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        string openingLabel = result.Instructions[2].ShouldBeOfType<LabelInstruction>().Name.Name;
        result.Instructions[4].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe(openingLabel);
    }

    /// <summary>
    /// Tests that <c>THAT WILL DO.</c> inside a switch nested in a loop targets the closing label
    /// of the switch itself, not that of the enclosing loop.
    /// </summary>
    /// <remarks>
    /// Matches <c>Interpreter.ExecuteSwitch</c> catching the break itself.
    /// </remarks>
    [Fact]
    public void Transform_BreakInsideSwitchInsideLoop_SailsToSwitchClosingLabel_NotLoopClosingLabel()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "X", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(1), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Whilst,
                Condition = Identifier("Flag"),
                Body =
                [
                    new SwitchNode()
                    {
                        Expression = Identifier("X"),
                        Cases = [new SwitchCase(1, [new BreakNode() { Span = PlaceholderSpan }])],
                        Span = PlaceholderSpan
                    }
                ],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        string loopClosingLabel = result.Instructions[5].ShouldBeOfType<SailUnlikeInstruction>().Label.Name;
        SailInstruction breakSail = result.Instructions.OfType<SailInstruction>().First(s => s.Label.Name.StartsWith("C2"));
        breakSail.Label.Name.ShouldBe("C2NCBMS");
        breakSail.Label.Name.ShouldNotBe(loopClosingLabel);
    }

    /// <summary>
    /// Tests that <c>ONCE MORE.</c> inside a switch nested in a loop skips the switch entirely and
    /// targets the continue target of the loop itself.
    /// </summary>
    [Fact]
    public void Transform_ContinueInsideSwitchInsideLoop_SailsToLoopContinueTarget_SkippingSwitch()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "X", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(1), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Whilst,
                Condition = Identifier("Flag"),
                Body =
                [
                    new SwitchNode()
                    {
                        Expression = Identifier("X"),
                        Cases = [new SwitchCase(1, [new ContinueNode() { Span = PlaceholderSpan }])],
                        Span = PlaceholderSpan
                    }
                ],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        string loopOpeningLabel = result.Instructions[4].ShouldBeOfType<LabelInstruction>().Name.Name;
        SailInstruction continueSail = result.Instructions.OfType<SailInstruction>().First(sailInstruction => sailInstruction.Label.Name == loopOpeningLabel);
        continueSail.Label.Name.ShouldBe(loopOpeningLabel);
    }

    /// <summary>
    /// Tests that <c>THAT WILL DO.</c> with no enclosing switch or loop throws
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Transform_BreakWithNoEnclosingConstruct_ThrowsInvalidOperationException()
    {
        ProgramNode program = Programme(new BreakNode() { Span = PlaceholderSpan });

        Should.Throw<InvalidOperationException>(() => this.transformer.Transform(program));
    }

    /// <summary>
    /// Tests that <c>ONCE MORE.</c> with no enclosing loop throws
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Transform_ContinueWithNoEnclosingConstruct_ThrowsInvalidOperationException()
    {
        ProgramNode program = Programme(new ContinueNode() { Span = PlaceholderSpan });

        Should.Throw<InvalidOperationException>(() => this.transformer.Transform(program));
    }

    /// <summary>
    /// Tests that the label counter is a single sequence shared across every control-flow
    /// construct kind.
    /// </summary>
    /// <remarks>
    /// A loop containing a nested conditional numbers the loop <c>1</c> and the conditional
    /// <c>2</c>, not <c>1</c> again.
    /// </remarks>
    [Fact]
    public void Transform_LoopContainingConditional_SharesGlobalCounterAcrossConstructKinds()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Whilst,
                Condition = Identifier("Flag"),
                Body =
                [
                    new ConditionalNode()
                    {
                        Condition = Identifier("Flag"),
                        TrueBlock = [new BreakNode() { Span = PlaceholderSpan }],
                        Span = PlaceholderSpan
                    }
                ],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions[2].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1W_FLAG");
        LabelInstruction conditionalTrueLabel = result.Instructions.OfType<LabelInstruction>().First(l => l.Name.Name.StartsWith("T"));
        conditionalTrueLabel.Name.Name.ShouldBe("T2QS_FLAG");
    }

    /// <summary>
    /// Tests that a conditional whose true branch ends in <c>ONCE MORE.</c> and whose else-if
    /// branch ends in <c>THAT WILL DO.</c> does not append a redundant, unreachable sail after
    /// either branch.
    /// </summary>
    [Fact]
    public void Transform_ConditionalWithBreakAndContinueBranches_SkipsRedundantClosingSails()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Whilst,
                Condition = Identifier("Flag"),
                Body =
                [
                    new ConditionalNode()
                    {
                        Condition = Identifier("A"),
                        TrueBlock = [new ContinueNode() { Span = PlaceholderSpan }],
                        ElseIfs = [new ElseIfBranch(Identifier("B"), [new BreakNode() { Span = PlaceholderSpan }])],
                        Span = PlaceholderSpan
                    }
                ],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(14);
        result.Instructions[8].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("L1W_FLAG");
        result.Instructions[9].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("T2OIN_B");
        result.Instructions[10].ShouldBeOfType<SailInstruction>().Label.Name.ShouldBe("L1WTTE");
        result.Instructions[11].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("T2SMFT");
    }

    /// <summary>
    /// Tests that a ternary expression with branches of different literal types widens the shared
    /// temporary to the wider type, casting only the narrower branch.
    /// </summary>
    [Fact]
    public void Transform_Ternary_WithDifferentBranchTypes_WidensToWiderType()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "Result", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            Assign("Result", new TernaryExpressionNode()
            {
                TrueValue = IntLiteral(1),
                Condition = Identifier("Flag"),
                FalseValue = LongLiteral(2L),
                Span = PlaceholderSpan
            }));

        UtopIRProgram result = this.transformer.Transform(program);

        WelcomeInstruction sharedTemporary = result.Instructions[3].ShouldBeOfType<WelcomeInstruction>();
        sharedTemporary.Type.ShouldBe(UtopIRType.Chancellor);
        WereInstruction were = result.Instructions[7].ShouldBeOfType<WereInstruction>();
        were.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(1);
        were.Type.ShouldBe(UtopIRType.Chancellor);
        result.Instructions[8].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(were.Target.Name);
        result.Instructions[11].ShouldBeOfType<AppointInstruction>().Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(2L);
    }

    /// <summary>
    /// Tests that a ternary expression with identifier branches of different declared types widens
    /// the shared temporary to the wider type, casting only the narrower branch.
    /// </summary>
    [Fact]
    public void Transform_Ternary_WithIdentifierBranchesOfDifferentTypes_WidensToWiderType()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "A", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "B", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            new DeclarationNode() { Name = "Result", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            Assign("Result", new TernaryExpressionNode()
            {
                TrueValue = Identifier("A"),
                Condition = Identifier("Flag"),
                FalseValue = Identifier("B"),
                Span = PlaceholderSpan
            }));

        UtopIRProgram result = this.transformer.Transform(program);

        WelcomeInstruction sharedTemporary = result.Instructions[5].ShouldBeOfType<WelcomeInstruction>();
        sharedTemporary.Type.ShouldBe(UtopIRType.Chancellor);
        WereInstruction were = result.Instructions[9].ShouldBeOfType<WereInstruction>();
        were.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("A");
        were.Type.ShouldBe(UtopIRType.Chancellor);
    }

    /// <summary>
    /// Tests that a ternary branch built from <c>NOT</c> infers its type from its own argument.
    /// </summary>
    [Fact]
    public void Transform_Ternary_WithInversionOfBranch_InfersOperandTypeFromArgument()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "Result", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            Assign("Result", new TernaryExpressionNode()
            {
                TrueValue = Prefix(Operator.InversionOf, Identifier("Flag")),
                Condition = Identifier("Flag"),
                FalseValue = Identifier("Flag"),
                Span = PlaceholderSpan
            }));

        UtopIRProgram result = this.transformer.Transform(program);

        WelcomeInstruction sharedTemporary = result.Instructions.OfType<WelcomeInstruction>().Last();
        sharedTemporary.Type.ShouldBe(UtopIRType.Decree);
        result.Instructions.OfType<WereInstruction>().ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a ternary branch built from a comparison operator always infers <see cref="UtopIRType.Decree"/>.
    /// </summary>
    [Fact]
    public void Transform_Ternary_WithComparisonBranch_InfersDecreeType()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "Result", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            Assign("Result", new TernaryExpressionNode()
            {
                TrueValue = Prefix(Operator.Alike, IntLiteral(1), IntLiteral(2)),
                Condition = Identifier("Flag"),
                FalseValue = Identifier("Flag"),
                Span = PlaceholderSpan
            }));

        UtopIRProgram result = this.transformer.Transform(program);

        WelcomeInstruction sharedTemporary = result.Instructions.OfType<WelcomeInstruction>().Last();
        sharedTemporary.Type.ShouldBe(UtopIRType.Decree);
        result.Instructions.OfType<WereInstruction>().ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a ternary branch built from an arithmetic expression infers its own widened
    /// operand type, which the outer ternary then widens again against the other branch.
    /// </summary>
    [Fact]
    public void Transform_Ternary_WithArithmeticBranch_InfersWidenedOperandType()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Flag", NameSpan = PlaceholderSpan, Type = LiteralType.Boolean, InitialValue = BoolLiteral(true), Span = PlaceholderSpan },
            new DeclarationNode() { Name = "Result", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            Assign("Result", new TernaryExpressionNode()
            {
                TrueValue = Prefix(Operator.Sum, IntLiteral(1), IntLiteral(2)),
                Condition = Identifier("Flag"),
                FalseValue = LongLiteral(3L),
                Span = PlaceholderSpan
            }));

        UtopIRProgram result = this.transformer.Transform(program);

        WelcomeInstruction sharedTemporary = result.Instructions.OfType<WelcomeInstruction>().Last();
        sharedTemporary.Type.ShouldBe(UtopIRType.Chancellor);
        result.Instructions.OfType<WereInstruction>().ShouldNotBeEmpty();
    }

    /// <summary>
    /// Tests that a nested compound condition (a comparison wrapping an arithmetic sub-expression)
    /// builds its label text recursively, embedding the sub-expression own mnemonic and operands.
    /// </summary>
    [Fact]
    public void Transform_WhilstLoop_WithNestedCompoundCondition_BuildsRecursiveLabelText()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Counter", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(1), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Whilst,
                Condition = Prefix(Operator.Alike, Prefix(Operator.Remainder, Identifier("Counter"), IntLiteral(2)), IntLiteral(0)),
                Body = [],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions[2].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe("L1W_ALIKE_REM_COUNTER_2_0");
    }

    /// <summary>
    /// Tests that a unary operator used as a loop condition maps to its correct label mnemonic.
    /// </summary>
    /// <remarks>
    /// An integer operand is used for every case, even though <see cref="Operator.InversionOf"/>
    /// and <see cref="Operator.HardlyEver"/> are semantically boolean-only, since
    /// <see cref="Operator.TranspositionUp"/>/<see cref="Operator.TranspositionDown"/>
    /// default-shift path requires the shifted value declared type to be an integer type.
    /// </remarks>
    [Theory]
    [InlineData(Operator.InversionOf, "INV")]
    [InlineData(Operator.HardlyEver, "HARDLY")]
    [InlineData(Operator.TranspositionUp, "TRANSUP")]
    [InlineData(Operator.TranspositionDown, "TRANSDOWN")]
    public void BuildConditionText_ForUnaryOperator_UsesCorrectMnemonic(Operator theOperator, string expectedMnemonic)
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Counter", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(1), Span = PlaceholderSpan },
            new LoopNode()
            {
                Type = LoopType.Whilst,
                Condition = Prefix(theOperator, Identifier("Counter")),
                Body = [],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions[2].ShouldBeOfType<LabelInstruction>().Name.Name.ShouldBe($"L1W_{expectedMnemonic}_COUNTER");
    }

    /// <summary>
    /// Tests that a switch case with a <c>null</c> literal value is treated as <c>0</c> in its
    /// label text.
    /// </summary>
    [Fact]
    public void Transform_SwitchWithNullLiteralCase_TreatsAsZero()
    {
        ProgramNode program = Programme(
            new DeclarationNode()
            {
                Name = "Name",
                NameSpan = PlaceholderSpan,
                Type = LiteralType.String,
                InitialValue = new LiteralNode() { Value = "x", Type = LiteralType.String, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            },
            new SwitchNode()
            {
                Expression = Identifier("Name"),
                Cases = [new SwitchCase(null, [])],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.OfType<SailAlikeInstruction>().Single().Label.Name.ShouldBe("C1AS_0");
    }

    /// <summary>
    /// Tests that a <c>VICTIM &lt;index&gt; ON &lt;yarn&gt;</c> expression, used as the initial value of
    /// a <c>stitch</c> declaration, welcomes the declared variable as <see cref="UtopIRType.Stitch"/>
    /// (from the declaration type, via <c>MapType</c>) and separately emits a
    /// <see cref="VictimYarnInstruction"/> into a temporary register, appointed into that declared
    /// variable afterwards.
    /// </summary>
    [Fact]
    public void Transform_ArrayIndexOnYarnVariable_EmitsVictimYarnInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode()
            {
                Name = "PoemSubject",
                NameSpan = PlaceholderSpan,
                Type = LiteralType.String,
                InitialValue = new LiteralNode() { Value = "Hollow", Type = LiteralType.String, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            },
            new DeclarationNode()
            {
                Name = "PoemSubjectLetter4",
                NameSpan = PlaceholderSpan,
                Type = LiteralType.Char,
                InitialValue = new ArrayIndexNode() { Index = IntLiteral(4), ArrayName = "PoemSubject", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        WelcomeInstruction welcomeLetter = result.Instructions.OfType<WelcomeInstruction>().Single(welcomeInstruction => welcomeInstruction.Target.Name == "PoemSubjectLetter4");
        welcomeLetter.Type.ShouldBe(UtopIRType.Stitch);

        VictimYarnInstruction victimYarn = result.Instructions.OfType<VictimYarnInstruction>().Single();
        victimYarn.YarnString.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("PoemSubject");
        victimYarn.Index.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(4);

        AppointInstruction appoint = result.Instructions.OfType<AppointInstruction>().Last();
        appoint.Target.Name.ShouldBe("PoemSubjectLetter4");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(victimYarn.Target.Name);
    }

    /// <summary>
    /// Tests that an <see cref="ArrayDeclarationNode"/> with initial values emits a
    /// <see cref="WelcomeListInstruction"/> sized to the initial value count, followed by one
    /// 1-based <see cref="AppointVictimInstruction"/> per initial value, in source order.
    /// </summary>
    [Fact]
    public void Transform_ArrayDeclarationWithInitialValues_EmitsWelcomeListThenAppointVictimPerElement()
    {
        ProgramNode program = Programme(
            new ArrayDeclarationNode()
            {
                Name = "Numbers",
                NameSpan = PlaceholderSpan,
                ElementType = LiteralType.Integer,
                IsConstant = false,
                InitialValues = [IntLiteral(10), IntLiteral(20), IntLiteral(30)],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        WelcomeListInstruction welcomeList = result.Instructions.OfType<WelcomeListInstruction>().Single();
        welcomeList.Target.Name.ShouldBe("Numbers");
        welcomeList.ElementType.ShouldBe(UtopIRType.Peer);
        welcomeList.Size.ShouldBe(3);

        AppointVictimInstruction[] appointVictims = [.. result.Instructions.OfType<AppointVictimInstruction>()];
        appointVictims.Length.ShouldBe(3);
        for (int i = 0; i < appointVictims.Length; i++)
        {
            appointVictims[i].Array.Name.ShouldBe("Numbers");
            appointVictims[i].Index.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(i + 1);
            appointVictims[i].Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe((i + 1) * 10);
        }
    }

    /// <summary>
    /// Tests that an <see cref="ArrayDeclarationNode"/> with a bare <see cref="ArrayDeclarationNode.Size"/>
    /// and no initial values emits only a <see cref="WelcomeListInstruction"/> sized accordingly, with no
    /// <see cref="AppointVictimInstruction"/>.
    /// </summary>
    [Fact]
    public void Transform_ArrayDeclarationWithBareSize_EmitsWelcomeListOnly()
    {
        ProgramNode program = Programme(
            new ArrayDeclarationNode()
            {
                Name = "Numbers",
                NameSpan = PlaceholderSpan,
                ElementType = LiteralType.Integer,
                Size = 3,
                IsConstant = false,
                InitialValues = [],
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(1);
        WelcomeListInstruction welcomeList = result.Instructions[0].ShouldBeOfType<WelcomeListInstruction>();
        welcomeList.Size.ShouldBe(3);
        result.Instructions.OfType<AppointVictimInstruction>().ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that an <see cref="ArrayElementAssignmentNode"/> emits an <see cref="AppointVictimInstruction"/>
    /// with the transformed index and value operands.
    /// </summary>
    [Fact]
    public void Transform_ArrayElementAssignment_EmitsAppointVictimInstruction()
    {
        ProgramNode program = Programme(
            new ArrayDeclarationNode()
            {
                Name = "Numbers",
                NameSpan = PlaceholderSpan,
                ElementType = LiteralType.Integer,
                Size = 3,
                IsConstant = false,
                InitialValues = [],
                Span = PlaceholderSpan
            },
            new ArrayElementAssignmentNode()
            {
                Index = IntLiteral(2),
                ArrayName = "Numbers",
                Value = IntLiteral(20),
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        AppointVictimInstruction appointVictim = result.Instructions.OfType<AppointVictimInstruction>().Single();
        appointVictim.Array.Name.ShouldBe("Numbers");
        appointVictim.Index.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(2);
        appointVictim.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(20);
    }

    /// <summary>
    /// Tests that a <c>VICTIM &lt;index&gt; ON &lt;array&gt;</c> expression on an array variable emits a
    /// <see cref="VictimListInstruction"/> into a temporary register typed as the array element type.
    /// </summary>
    [Fact]
    public void Transform_ArrayIndexOnArrayVariable_EmitsVictimListInstruction()
    {
        ProgramNode program = Programme(
            new ArrayDeclarationNode()
            {
                Name = "Numbers",
                NameSpan = PlaceholderSpan,
                ElementType = LiteralType.Integer,
                IsConstant = false,
                InitialValues = [IntLiteral(10), IntLiteral(20), IntLiteral(30)],
                Span = PlaceholderSpan
            },
            new DeclarationNode()
            {
                Name = "NumbersElement2",
                NameSpan = PlaceholderSpan,
                Type = LiteralType.Integer,
                InitialValue = new ArrayIndexNode() { Index = IntLiteral(2), ArrayName = "Numbers", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        VictimListInstruction victimList = result.Instructions.OfType<VictimListInstruction>().Single();
        victimList.Array.Name.ShouldBe("Numbers");
        victimList.Index.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(2);

        AppointInstruction appoint = result.Instructions.OfType<AppointInstruction>().Last();
        appoint.Target.Name.ShouldBe("NumbersElement2");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(victimList.Target.Name);
    }

    /// <summary>
    /// Tests that a <see cref="PointerDeclarationNode"/> with an <see cref="AddressOfExpressionNode"/>
    /// initial value emits a <see cref="WelcomeGallerypicInstruction"/> followed directly by a
    /// <see cref="PicturetoInstruction"/> targeting the declared pointer, with no intervening
    /// <see cref="AppointInstruction"/> or temporary register.
    /// </summary>
    [Fact]
    public void Transform_PointerDeclarationWithAddressOf_EmitsWelcomeGallerypicThenPictureto()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Number", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(42), Span = PlaceholderSpan },
            new PointerDeclarationNode()
            {
                Name = "NumberPointer",
                NameSpan = PlaceholderSpan,
                PointeeType = LiteralType.Integer,
                InitialValue = new AddressOfExpressionNode() { VariableName = "Number", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        WelcomeGallerypicInstruction welcomeGallerypic = result.Instructions.OfType<WelcomeGallerypicInstruction>().Single();
        welcomeGallerypic.Target.Name.ShouldBe("NumberPointer");
        welcomeGallerypic.PointeeType.ShouldBe(UtopIRType.Peer);

        PicturetoInstruction pictureto = result.Instructions.OfType<PicturetoInstruction>().Single();
        pictureto.Target.Name.ShouldBe("NumberPointer");
        pictureto.Pointee.Name.ShouldBe("Number");

        result.Instructions.OfType<AppointInstruction>().ShouldAllBe(appoint => appoint.Target.Name == "Number");
    }

    /// <summary>
    /// Tests that a <see cref="PointerDeclarationNode"/> with no initial value (<c>NAUGHT</c>) emits a
    /// <see cref="WelcomeGallerypicInstruction"/> followed by an <see cref="AppointInstruction"/> of
    /// the <see cref="NaughtLiteral"/> singleton, with no <see cref="PicturetoInstruction"/>.
    /// </summary>
    [Fact]
    public void Transform_PointerDeclarationWithNoInitialValue_EmitsWelcomeGallerypicThenAppointNaught()
    {
        ProgramNode program = Programme(
            new PointerDeclarationNode()
            {
                Name = "NumberPointer",
                NameSpan = PlaceholderSpan,
                PointeeType = LiteralType.Integer,
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(2);
        result.Instructions[0].ShouldBeOfType<WelcomeGallerypicInstruction>();
        AppointInstruction appoint = result.Instructions[1].ShouldBeOfType<AppointInstruction>();
        appoint.Target.Name.ShouldBe("NumberPointer");
        appoint.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBeOfType<NaughtLiteral>();
        result.Instructions.OfType<PicturetoInstruction>().ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a <see cref="DereferenceExpressionNode"/> emits a <see cref="ViewfromInstruction"/>
    /// into a temporary register, appointed into the declared variable.
    /// </summary>
    [Fact]
    public void Transform_DereferenceExpression_EmitsViewfromInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Number", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(42), Span = PlaceholderSpan },
            new PointerDeclarationNode()
            {
                Name = "NumberPointer",
                NameSpan = PlaceholderSpan,
                PointeeType = LiteralType.Integer,
                InitialValue = new AddressOfExpressionNode() { VariableName = "Number", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            },
            new DeclarationNode()
            {
                Name = "NumberValue",
                NameSpan = PlaceholderSpan,
                Type = LiteralType.Integer,
                InitialValue = new DereferenceExpressionNode() { PointerName = "NumberPointer", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        ViewfromInstruction viewfrom = result.Instructions.OfType<ViewfromInstruction>().Single();
        viewfrom.Pointer.Name.ShouldBe("NumberPointer");

        AppointInstruction appoint = result.Instructions.OfType<AppointInstruction>().Last();
        appoint.Target.Name.ShouldBe("NumberValue");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(viewfrom.Target.Name);
    }

    /// <summary>
    /// Tests that a <see cref="DereferenceAssignmentNode"/> emits a <see cref="ViewtoInstruction"/>.
    /// </summary>
    [Fact]
    public void Transform_DereferenceAssignment_EmitsViewtoInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Number", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(42), Span = PlaceholderSpan },
            new PointerDeclarationNode()
            {
                Name = "NumberPointer",
                NameSpan = PlaceholderSpan,
                PointeeType = LiteralType.Integer,
                InitialValue = new AddressOfExpressionNode() { VariableName = "Number", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            },
            new DereferenceAssignmentNode()
            {
                PointerName = "NumberPointer",
                Value = IntLiteral(23),
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        ViewtoInstruction viewto = result.Instructions.OfType<ViewtoInstruction>().Single();
        viewto.Pointer.Name.ShouldBe("NumberPointer");
        viewto.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(23);
    }

    /// <summary>
    /// Tests that <c>SUM OF pointer AND offset</c> (sharing the same <see cref="Operator.Sum"/> and
    /// <see cref="PrefixExpressionNode"/> shape as ordinary arithmetic) emits a
    /// <see cref="PointerArithmeticInstruction"/> rather than an <see cref="ArithmeticInstruction"/>,
    /// distinguished purely by the first operand being a recorded pointer variable.
    /// </summary>
    [Fact]
    public void Transform_SumOfPointerAndOffset_EmitsPointerArithmeticInstructionNotArithmeticInstruction()
    {
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Number", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, InitialValue = IntLiteral(42), Span = PlaceholderSpan },
            new PointerDeclarationNode()
            {
                Name = "NumberPointer",
                NameSpan = PlaceholderSpan,
                PointeeType = LiteralType.Integer,
                InitialValue = new AddressOfExpressionNode() { VariableName = "Number", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            },
            new PointerDeclarationNode()
            {
                Name = "NumberPointerOffset",
                NameSpan = PlaceholderSpan,
                PointeeType = LiteralType.Integer,
                Span = PlaceholderSpan
            },
            new AssignmentNode()
            {
                Target = "NumberPointerOffset",
                Value = Prefix(Operator.Sum, Identifier("NumberPointer"), IntLiteral(1)),
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        PointerArithmeticInstruction pointerArithmetic = result.Instructions.OfType<PointerArithmeticInstruction>().Single();
        pointerArithmetic.Operation.ShouldBe(UtopIRPointerArithmeticOperation.Sum);
        pointerArithmetic.Pointer.Name.ShouldBe("NumberPointer");
        pointerArithmetic.Offset.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(1);
        result.Instructions.OfType<ArithmeticInstruction>().ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a standalone <c>SUMMON</c> of a void function emits a <c>prentice</c> per argument
    /// followed by a <c>summon</c>.
    /// </summary>
    [Fact]
    public void Transform_SummonStatement_WithVoidFunction_EmitsPrenticeThenSummon()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new ExpressionStatement
            {
                Expression = Prefix(Operator.Summon, Identifier("TestVoid"), IntLiteral(5)),
                Span = PlaceholderSpan
            });

        UtopIRProgram result = transformerWithTestCatalogue.Transform(program);

        result.Instructions.Count.ShouldBe(2);
        PrenticeInstruction prentice = result.Instructions[0].ShouldBeOfType<PrenticeInstruction>();
        prentice.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(5);
        SummonInstruction summon = result.Instructions[1].ShouldBeOfType<SummonInstruction>();
        summon.Function.Name.ShouldBe("TestVoid");
    }

    /// <summary>
    /// Tests that a <c>SUMMON</c> of a value-returning function used in expression position emits a
    /// <c>prentice</c>, a <c>summon.find</c> into a temporary register, then an <c>appoint</c> of the
    /// assignment target from that register.
    /// </summary>
    [Fact]
    public void Transform_SummonFindExpression_WithValueFunction_EmitsPrenticeSummonFindThenAppoint()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            Assign("result", Prefix(Operator.Summon, Identifier("TestValue"), IntLiteral(7))));

        UtopIRProgram result = transformerWithTestCatalogue.Transform(program);

        result.Instructions.Count.ShouldBe(4);
        result.Instructions[0].ShouldBeOfType<WelcomeInstruction>();
        PrenticeInstruction prentice = result.Instructions[1].ShouldBeOfType<PrenticeInstruction>();
        prentice.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(7);
        SummonFindInstruction summonFind = result.Instructions[2].ShouldBeOfType<SummonFindInstruction>();
        summonFind.Function.Name.ShouldBe("TestValue");
        AppointInstruction appoint = result.Instructions[3].ShouldBeOfType<AppointInstruction>();
        appoint.Target.Name.ShouldBe("result");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(summonFind.Target.Name);
    }

    /// <summary>
    /// Tests that a narrower literal argument is widened with a <see cref="WereInstruction"/> before
    /// being pushed with <c>prentice</c>, when the resolved function parameter type is wider.
    /// </summary>
    [Fact]
    public void Transform_SummonFindExpression_WithNarrowerLiteralArgument_InsertsWereInstructionBeforePrentice()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Long, Span = PlaceholderSpan },
            Assign("result", Prefix(Operator.Summon, Identifier("TestWiden"), IntLiteral(5))));

        UtopIRProgram result = transformerWithTestCatalogue.Transform(program);

        WereInstruction were = result.Instructions.OfType<WereInstruction>().Single();
        were.Type.ShouldBe(UtopIRType.Chancellor);
        were.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(5);
        PrenticeInstruction prentice = result.Instructions.OfType<PrenticeInstruction>().Single();
        prentice.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(were.Target.Name);
    }

    /// <summary>
    /// Tests that a <c>SUMMON</c> of a name that is also a user-defined function in the programme
    /// throws, even though a catalogue function of the same name exists: a user-defined function must
    /// always shadow an external one of the same name.
    /// </summary>
    [Fact]
    public void Transform_SummonStatement_WithUserDefinedFunctionName_ThrowsNotSupportedException()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new FunctionDefinitionNode()
            {
                Name = "TestVoid",
                NameSpan = PlaceholderSpan,
                Parameters = [],
                Body = [],
                Span = PlaceholderSpan
            },
            new ExpressionStatement
            {
                Expression = Prefix(Operator.Summon, Identifier("TestVoid")),
                Span = PlaceholderSpan
            });

        Should.Throw<NotSupportedException>(() => transformerWithTestCatalogue.Transform(program));
    }

    /// <summary>
    /// Tests that a <c>SUMMON</c> of a name found in neither the programme own functions nor the
    /// external catalogue throws.
    /// </summary>
    [Fact]
    public void Transform_SummonStatement_WithUnknownFunctionName_ThrowsNotSupportedException()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new ExpressionStatement
            {
                Expression = Prefix(Operator.Summon, Identifier("DoesNotExist")),
                Span = PlaceholderSpan
            });

        Should.Throw<NotSupportedException>(() => transformerWithTestCatalogue.Transform(program));
    }

    /// <summary>
    /// Tests that summoning a void function in expression position (<c>summon.find</c>) throws, since
    /// there is no return value to store.
    /// </summary>
    [Fact]
    public void Transform_SummonFindExpression_WithVoidFunction_ThrowsNotSupportedException()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "result", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            Assign("result", Prefix(Operator.Summon, Identifier("TestVoid"), IntLiteral(5))));

        Should.Throw<NotSupportedException>(() => transformerWithTestCatalogue.Transform(program));
    }

    /// <summary>
    /// Tests that a <c>SUMMON</c> whose first argument is not a function name identifier throws.
    /// </summary>
    [Fact]
    public void Transform_SummonStatement_WithNonIdentifierFirstArgument_ThrowsNotSupportedException()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new ExpressionStatement
            {
                Expression = Prefix(Operator.Summon, IntLiteral(5)),
                Span = PlaceholderSpan
            });

        Should.Throw<NotSupportedException>(() => transformerWithTestCatalogue.Transform(program));
    }

    /// <summary>
    /// Tests that <c>BEHOLD</c> of a plain string literal lowers to a <c>prentice</c> of the text, a
    /// <c>prentice</c> of <c>withCeremony</c>, then a <c>summon</c> of <c>PreviewBehold</c>.
    /// </summary>
    [Fact]
    public void Transform_Print_WithStringLiteral_EmitsPrenticeTextPrenticeCeremonyThenSummon()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new PrintNode
            {
                Expression = new LiteralNode { Value = "hello", Type = LiteralType.String, Span = PlaceholderSpan },
                SuppressNewline = false,
                Span = PlaceholderSpan
            });

        UtopIRProgram result = transformerWithTestCatalogue.Transform(program);

        result.Instructions.Count.ShouldBe(3);
        PrenticeInstruction textPrentice = result.Instructions[0].ShouldBeOfType<PrenticeInstruction>();
        textPrentice.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe("hello");
        PrenticeInstruction ceremonyPrentice = result.Instructions[1].ShouldBeOfType<PrenticeInstruction>();
        ceremonyPrentice.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(true);
        SummonInstruction summon = result.Instructions[2].ShouldBeOfType<SummonInstruction>();
        summon.Function.Name.ShouldBe("PreviewBehold");
    }

    /// <summary>
    /// Tests that <c>BEHOLD ... WITHOUT CEREMONY</c> pushes <c>false</c> for <c>withCeremony</c>.
    /// </summary>
    [Fact]
    public void Transform_Print_WithSuppressNewline_PushesFalseForWithCeremony()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new PrintNode
            {
                Expression = new LiteralNode { Value = "hello", Type = LiteralType.String, Span = PlaceholderSpan },
                SuppressNewline = true,
                Span = PlaceholderSpan
            });

        UtopIRProgram result = transformerWithTestCatalogue.Transform(program);

        PrenticeInstruction ceremonyPrentice = result.Instructions.OfType<PrenticeInstruction>().ElementAt(1);
        ceremonyPrentice.Value.ShouldBeOfType<LiteralOperand>().Value.ShouldBe(false);
    }

    /// <summary>
    /// Tests that <c>BEHOLD</c> of a string literal containing a <c>{...}</c> interpolation placeholder
    /// throws, since interpolation is expected to be resolved before a programme reaches UtopIR.
    /// </summary>
    [Fact]
    public void Transform_Print_WithInterpolationPlaceholder_ThrowsNotSupportedException()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new PrintNode
            {
                Expression = new LiteralNode { Value = "hello {Name}", Type = LiteralType.String, Span = PlaceholderSpan },
                SuppressNewline = false,
                Span = PlaceholderSpan
            });

        Should.Throw<NotSupportedException>(() => transformerWithTestCatalogue.Transform(program));
    }

    /// <summary>
    /// Tests that <c>BEHOLD</c> of a <c>yarn</c>-typed variable pushes a <see cref="VariableOperand"/>
    /// for the text argument, since a <c>yarn</c> is already the CLR <see cref="string"/> the Standard
    /// Library function expects.
    /// </summary>
    [Fact]
    public void Transform_Print_WithYarnTypedVariable_PushesVariableOperand()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Greeting", NameSpan = PlaceholderSpan, Type = LiteralType.String, Span = PlaceholderSpan },
            new PrintNode { Expression = Identifier("Greeting"), SuppressNewline = false, Span = PlaceholderSpan });

        UtopIRProgram result = transformerWithTestCatalogue.Transform(program);

        PrenticeInstruction textPrentice = result.Instructions.OfType<PrenticeInstruction>().First();
        textPrentice.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("Greeting");
    }

    /// <summary>
    /// Tests that <c>BEHOLD</c> of a non-<c>yarn</c>-typed variable throws, since converting it to
    /// <c>yarn</c> has no UtopIR lowering yet.
    /// </summary>
    [Fact]
    public void Transform_Print_WithNonYarnTypedVariable_ThrowsNotSupportedException()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Number", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan },
            new PrintNode { Expression = Identifier("Number"), SuppressNewline = false, Span = PlaceholderSpan });

        Should.Throw<NotSupportedException>(() => transformerWithTestCatalogue.Transform(program));
    }

    /// <summary>
    /// Tests that <c>BEHOLD</c> of an expression that is neither a plain string literal nor a variable
    /// reference throws.
    /// </summary>
    [Fact]
    public void Transform_Print_WithArithmeticExpression_ThrowsNotSupportedException()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new PrintNode { Expression = Prefix(Operator.Sum, IntLiteral(1), IntLiteral(2)), SuppressNewline = false, Span = PlaceholderSpan });

        Should.Throw<NotSupportedException>(() => transformerWithTestCatalogue.Transform(program));
    }

    /// <summary>
    /// Tests that <c>BEHOLD</c> throws a clear error when the supplied catalogue has no expected function.
    /// </summary>
    [Fact]
    public void Transform_Print_WithNoPreviewBeholdInCatalogue_ThrowsNotSupportedException()
    {
        TopsyTurvyToUtopIRTransformer transformerWithEmptyCatalogue = new(new InstructionDetailVariableFormatter(), externalFunctions: BindingCatalogue.Empty);
        ProgramNode program = Programme(
            new PrintNode
            {
                Expression = new LiteralNode { Value = "hello", Type = LiteralType.String, Span = PlaceholderSpan },
                SuppressNewline = false,
                Span = PlaceholderSpan
            });

        Should.Throw<NotSupportedException>(() => transformerWithEmptyCatalogue.Transform(program));
    }

    /// <summary>
    /// Tests that <c>PRAY TELL</c> lowers to a <c>summon.find</c> with the expected function appointed
    /// into the target variable.
    /// </summary>
    [Fact]
    public void Transform_Input_EmitsSummonFindThenAppointToTarget()
    {
        TopsyTurvyToUtopIRTransformer transformerWithTestCatalogue = TransformerWithTestCatalogue();
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Greeting", NameSpan = PlaceholderSpan, Type = LiteralType.String, Span = PlaceholderSpan },
            new InputNode { Target = "Greeting", Span = PlaceholderSpan });

        UtopIRProgram result = transformerWithTestCatalogue.Transform(program);

        SummonFindInstruction summonFind = result.Instructions.OfType<SummonFindInstruction>().Single();
        summonFind.Function.Name.ShouldBe("PreviewPrayTell");
        AppointInstruction appoint = result.Instructions.OfType<AppointInstruction>().Single();
        appoint.Target.Name.ShouldBe("Greeting");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe(summonFind.Target.Name);
    }

    /// <summary>
    /// Tests that <c>PRAY TELL</c> throws a clear error when the supplied catalogue has no
    /// expected function.
    /// </summary>
    [Fact]
    public void Transform_Input_WithNoPreviewPrayTellInCatalogue_ThrowsNotSupportedException()
    {
        TopsyTurvyToUtopIRTransformer transformerWithEmptyCatalogue = new(new InstructionDetailVariableFormatter(), externalFunctions: BindingCatalogue.Empty);
        ProgramNode program = Programme(
            new DeclarationNode() { Name = "Greeting", NameSpan = PlaceholderSpan, Type = LiteralType.String, Span = PlaceholderSpan },
            new InputNode { Target = "Greeting", Span = PlaceholderSpan });

        Should.Throw<NotSupportedException>(() => transformerWithEmptyCatalogue.Transform(program));
    }

    /// <summary>
    /// Builds a <see cref="TopsyTurvyToUtopIRTransformer"/> using <see cref="TestExternalFunctionBindingClass"/>
    /// as its catalogue, so <c>summon</c>/<c>summon.find</c>/<c>BEHOLD</c>/<c>PRAY TELL</c> tests do not
    /// depend on the real Standard Library.
    /// </summary>
    /// <returns>The constructed transformer.</returns>
    private static TopsyTurvyToUtopIRTransformer TransformerWithTestCatalogue() =>
        new(new InstructionDetailVariableFormatter(), externalFunctions: BindingCatalogue.Create(typeof(TestExternalFunctionBindingClass)));

    /// <summary>
    /// Wraps a list of statements in a minimal <see cref="ProgramNode"/> for transformation.
    /// </summary>
    /// <param name="statements">The statements to include.</param>
    /// <returns>A <see cref="ProgramNode"/> containing those statements.</returns>
    private static ProgramNode Programme(params Statement[] statements) =>
        new()
        {
            Title = "Test",
            Statements = statements,
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Builds an <see cref="IdentifierNode"/> referencing the given name.
    /// </summary>
    /// <param name="name">The name of the variable to reference.</param>
    /// <returns>The constructed <see cref="IdentifierNode"/>.</returns>
    private static IdentifierNode Identifier(string name) => new()
    {
        Name = name,
        Span = PlaceholderSpan
    };

    /// <summary>
    /// Builds an integer <see cref="LiteralNode"/>.
    /// </summary>
    /// <param name="value">The integer value to wrap.</param>
    /// <returns>The constructed <see cref="LiteralNode"/>.</returns>
    private static LiteralNode IntLiteral(int value) => new()
    {
        Value = value,
        Type = LiteralType.Integer,
        Span = PlaceholderSpan
    };

    /// <summary>
    /// Builds a long <see cref="LiteralNode"/>.
    /// </summary>
    /// <param name="value">The long value to wrap.</param>
    /// <returns>The constructed <see cref="LiteralNode"/>.</returns>
    private static LiteralNode LongLiteral(long value) => new()
    {
        Value = value,
        Type = LiteralType.Long,
        Span = PlaceholderSpan
    };

    /// <summary>
    /// Builds a boolean <see cref="LiteralNode"/>.
    /// </summary>
    /// <param name="value">The boolean value to wrap.</param>
    /// <returns>The constructed <see cref="LiteralNode"/>.</returns>
    private static LiteralNode BoolLiteral(bool value) => new()
    {
        Value = value,
        Type = LiteralType.Boolean,
        Span = PlaceholderSpan
    };

    /// <summary>
    /// Builds a binary or unary <see cref="PrefixExpressionNode"/>.
    /// </summary>
    /// <param name="theOperator">The operator to apply.</param>
    /// <param name="arguments">The operator arguments (one for a unary operator, two for a binary operator).</param>
    /// <returns>The constructed <see cref="PrefixExpressionNode"/>.</returns>
    private static PrefixExpressionNode Prefix(Operator theOperator, params Expression[] arguments) =>
        new()
        {
            Operator = theOperator,
            Arguments = arguments,
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Builds an <see cref="AssignmentNode"/>.
    /// </summary>
    /// <param name="target">The name of the variable being assigned to.</param>
    /// <param name="value">The expression to assign.</param>
    /// <returns>The constructed <see cref="AssignmentNode"/>.</returns>
    private static AssignmentNode Assign(string target, Expression value) =>
        new()
        {
            Target = target,
            Value = value,
            Span = PlaceholderSpan
        };
}
