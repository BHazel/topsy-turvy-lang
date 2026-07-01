using System;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Transformer;

namespace BWHazel.TopsyTurvy.UtopIR.Tests.Transformer;

/// <summary>
/// Tests for <see cref="TopsyTurvyToUtopIRTransformer"/>.
/// </summary>
public class TopsyTurvyToUtopIRTransformerTests
{
    private readonly TopsyTurvyToUtopIRTransformer transformer = new();

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
            new DeclarationNode() { Name = "x", Type = LiteralType.Integer, Span = PlaceholderSpan });

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
    /// Tests that a declaration whose initial value is an identifier reference emits <c>welcome</c> followed by <c>appoint</c> with a <see cref="VariableOperand"/>.
    /// </summary>
    [Fact]
    public void Transform_DeclarationWithIdentifierInitialValue_EmitsWelcomeThenAppointWithVariable()
    {
        ProgramNode program = Programme(
            new DeclarationNode()
            {
                Name = "y",
                Type = LiteralType.Integer,
                InitialValue = new IdentifierNode { Name = "x", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(2);
        AppointInstruction appoint = result.Instructions[1].ShouldBeOfType<AppointInstruction>();
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
                    new DeclarationNode() { Name = "a", Type = LiteralType.Integer, Span = PlaceholderSpan },
                    new DeclarationNode() { Name = "b", Type = LiteralType.Long, Span = PlaceholderSpan }
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
            new AssignmentNode()
            {
                Target = "x",
                Value = new LiteralNode() { Value = 99, Type = LiteralType.Integer, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(1);
        AppointInstruction appoint = result.Instructions[0].ShouldBeOfType<AppointInstruction>();
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
            new AssignmentNode()
            {
                Target = "y",
                Value = new IdentifierNode() { Name = "x", Span = PlaceholderSpan },
                Span = PlaceholderSpan
            });

        UtopIRProgram result = this.transformer.Transform(program);

        result.Instructions.Count.ShouldBe(1);
        AppointInstruction appoint = result.Instructions[0].ShouldBeOfType<AppointInstruction>();
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

        ArithmeticInstruction arithmetic = result.Instructions[0].ShouldBeOfType<ArithmeticInstruction>();
        arithmetic.Operation.ShouldBe(expectedUtopirOperation);
    }

    /// <summary>
    /// Tests that a binary arithmetic expression on two variables emits an <see cref="ArithmeticInstruction"/> whose operands reference those variables and whose target is a temporary register named after the operation and operands.
    /// </summary>
    [Fact]
    public void Transform_ArithmeticOnTwoVariables_EmitsArithmeticInstructionWithCorrectTempName()
    {
        ProgramNode program = Programme(
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

        result.Instructions.Count.ShouldBe(2);
        ArithmeticInstruction arithmetic = result.Instructions[0].ShouldBeOfType<ArithmeticInstruction>();
        arithmetic.Target.Name.ShouldBe("_sum_Peer1_Peer2");
        arithmetic.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("Peer1");
        arithmetic.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("Peer2");
        AppointInstruction appoint = result.Instructions[1].ShouldBeOfType<AppointInstruction>();
        appoint.Target.Name.ShouldBe("result");
        appoint.Value.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("_sum_Peer1_Peer2");
    }

    /// <summary>
    /// Tests that a binary arithmetic expression on two literals emits an <see cref="ArithmeticInstruction"/> whose target temporary name embeds the literal values.
    /// </summary>
    [Fact]
    public void Transform_ArithmeticOnTwoLiterals_EmbedLiteralValuesInTempName()
    {
        ProgramNode program = Programme(
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

        ArithmeticInstruction arithmetic = result.Instructions[0].ShouldBeOfType<ArithmeticInstruction>();
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

        result.Instructions.Count.ShouldBe(3);
        ArithmeticInstruction prod = result.Instructions[0].ShouldBeOfType<ArithmeticInstruction>();
        prod.Operation.ShouldBe(UtopIRArithmeticOperation.Prod);
        prod.Target.Name.ShouldBe("_prod_a_b");
        ArithmeticInstruction sum = result.Instructions[1].ShouldBeOfType<ArithmeticInstruction>();
        sum.Operation.ShouldBe(UtopIRArithmeticOperation.Sum);
        sum.Target.Name.ShouldBe("_sum__prod_a_b_c");
        sum.Operand1.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("_prod_a_b");
        sum.Operand2.ShouldBeOfType<VariableOperand>().Variable.Name.ShouldBe("c");
        result.Instructions[2].ShouldBeOfType<AppointInstruction>()
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
            new DeclarationNode() { Name = "v", Type = topsyTurvyType, Span = PlaceholderSpan });

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
                    Operator = Operator.Both,
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
                Name = "Peer1", Type = LiteralType.Integer,
                InitialValue = new LiteralNode() { Value = 42, Type = LiteralType.Integer, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            },
            new DeclarationNode()
            {
                Name = "Peer2", Type = LiteralType.Integer,
                InitialValue = new LiteralNode() { Value = 30, Type = LiteralType.Integer, Span = PlaceholderSpan },
                Span = PlaceholderSpan
            },
            new DeclarationNode()
            {
                Name = "PeerResult", Type = LiteralType.Integer,
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
