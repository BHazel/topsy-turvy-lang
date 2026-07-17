using System;
using BWHazel.TopsyTurvy.Ast;
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
}
