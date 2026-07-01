using System;
using System.Collections.Generic;
using System.Globalization;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.UtopIR.Transformer;

/// <summary>
/// Transforms a Topsy Turvy AST (<see cref="ProgramNode"/>) into a flat UtopIR instruction
/// sequence (<see cref="UtopIRProgram"/>).
/// </summary>
/// <remarks>
/// <para>
/// The transformer uses type-switching, consistent with the existing interpreter and
/// type checker in the Topsy Turvy toolchain.
/// </para>
/// <para>
/// Nested expressions such as <c>SUM OF PRODUCT OF a AND b AND c</c> are flattened into
/// a sequence of arithmetic instructions using auto-generated temporary virtual registers.
/// Temporary names follow the pattern <c>_&lt;mnemonic&gt;_&lt;op1&gt;_&lt;op2&gt;</c>,
/// e.g. <c>_prod_a_b</c>, <c>_sum__prod_a_b_c</c>.
/// </para>
/// <para>
/// <b>Scope</b>: This initial implementation covers the instruction set defined in
/// UtopIR v0.0.1-preview1: variable declaration/assignment, arithmetic operations, and
/// top-level programme return.  Constant inlining, functions, control flow, and non-numeric
/// types are deferred to later versions.
/// </para>
/// <para>
/// <b>Constants</b>: Topsy Turvy <c>CONSERVATIVE</c> declarations are currently lowered
/// identically to mutable declarations.  Constant inlining (replacing every reference with
/// its literal value) requires a full symbol-table pass and is deferred.
/// </para>
/// </remarks>
public sealed class TopsyTurvyToUtopIRTransformer
{
    /// <summary>
    /// Transforms the given Topsy Turvy programme into a <see cref="UtopIRProgram"/>.
    /// </summary>
    /// <param name="program">The root AST node of the Topsy Turvy programme to transform.</param>
    /// <returns>A <see cref="UtopIRProgram"/> containing the equivalent UtopIR instruction sequence.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the programme contains an expression type that is not yet supported by the transformer.
    /// </exception>
    public UtopIRProgram Transform(ProgramNode program)
    {
        List<UtopIRInstruction> instructions = [];

        foreach (Statement statement in program.Statements)
        {
            this.TransformStatement(statement, instructions);
        }

        return new UtopIRProgram(instructions);
    }

    /// <summary>
    /// Dispatches a single top-level or block-level statement for transformation.
    /// </summary>
    /// <param name="statement">The statement to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    private void TransformStatement(Statement statement, List<UtopIRInstruction> instructions)
    {
        switch (statement)
        {
            case PrincipalBlockNode principalBlock:
                foreach (Statement declaration in principalBlock.Declarations)
                {
                    this.TransformStatement(declaration, instructions);
                }

                break;
            case DeclarationNode declaration:
                this.TransformDeclaration(declaration, instructions);
                break;
            case AssignmentNode assignment:
                this.TransformAssignment(assignment, instructions);
                break;
            case ProgrammeReturnNode programmeReturn:
                this.TransformProgrammeReturn(programmeReturn, instructions);
                break;
        }
    }

    /// <summary>
    /// Transforms a <see cref="DeclarationNode"/> into a <see cref="WelcomeInstruction"/>
    /// followed by an optional <see cref="AppointInstruction"/> when an initial value is present.
    /// </summary>
    /// <param name="declaration">The declaration statement to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    private void TransformDeclaration(DeclarationNode declaration, List<UtopIRInstruction> instructions)
    {
        UtopIRVariable target = new(declaration.Name);
        UtopIRType type = this.MapType(declaration.Type);
        instructions.Add(new WelcomeInstruction(target, type));

        if (declaration.InitialValue is not null)
        {
            UtopIROperand value = this.TransformExpression(declaration.InitialValue, instructions);
            instructions.Add(new AppointInstruction(target, value));
        }
    }

    /// <summary>
    /// Transforms an <see cref="AssignmentNode"/> into an <see cref="AppointInstruction"/>,
    /// flattening the right-hand-side expression first if needed.
    /// </summary>
    /// <param name="assignment">The assignment statement to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    private void TransformAssignment(AssignmentNode assignment, List<UtopIRInstruction> instructions)
    {
        UtopIRVariable target = new(assignment.Target);
        UtopIROperand value = this.TransformExpression(assignment.Value, instructions);
        instructions.Add(new AppointInstruction(target, value));
    }

    /// <summary>
    /// Transforms a <see cref="ProgrammeReturnNode"/> into a <see cref="FindInstruction"/>,
    /// flattening the return-value expression first if needed.
    /// </summary>
    /// <param name="programmeReturn">The programme return statement to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    private void TransformProgrammeReturn(ProgrammeReturnNode programmeReturn, List<UtopIRInstruction> instructions)
    {
        UtopIROperand value = this.TransformExpression(programmeReturn.Value, instructions);
        instructions.Add(new FindInstruction(value));
    }

    /// <summary>
    /// Transforms an expression into a <see cref="UtopIROperand"/>, emitting any necessary
    /// intermediate arithmetic instructions first.
    /// </summary>
    /// <param name="expression">The expression to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    /// <returns>
    /// A <see cref="LiteralOperand"/> for literal nodes, a <see cref="VariableOperand"/> for
    /// identifier nodes or a <see cref="VariableOperand"/> referencing a newly emitted
    /// temporary register for arithmetic expressions.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the expression type is not supported in the current transformer scope.</exception>
    private UtopIROperand TransformExpression(Expression expression, List<UtopIRInstruction> instructions)
    {
        switch (expression)
        {
            case LiteralNode literal:
                return new LiteralOperand(literal.Value!);
            case IdentifierNode identifier:
                return new VariableOperand(new UtopIRVariable(identifier.Name));
            case PrefixExpressionNode prefix when this.IsArithmeticOperator(prefix.Operator):
                return this.TransformArithmetic(prefix, instructions);
            default:
                throw new NotSupportedException(
                    $"Expression type '{expression.GetType().Name}' with operator " +
                    $"'{(expression is PrefixExpressionNode prefixExpression ? prefixExpression.Operator : "(none)")}' " +
                    $"is not supported by the transformer in this version.");
        }
    }

    /// <summary>
    /// Transforms an arithmetic <see cref="PrefixExpressionNode"/> by recursively flattening
    /// its operands, emitting an <see cref="ArithmeticInstruction"/> into a temporary register,
    /// and returning a <see cref="VariableOperand"/> referencing that register.
    /// </summary>
    /// <param name="prefix">The arithmetic prefix expression to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    /// <returns>A <see cref="VariableOperand"/> for the temporary register holding the result.</returns>
    private VariableOperand TransformArithmetic(PrefixExpressionNode prefix, List<UtopIRInstruction> instructions)
    {
        UtopIRArithmeticOperation operation = this.MapOperator(prefix.Operator);
        string mnemonic = this.OperationMnemonic(operation);

        UtopIROperand operand1 = this.TransformExpression(prefix.Arguments[0], instructions);
        UtopIROperand operand2 = this.TransformExpression(prefix.Arguments[1], instructions);

        string temporaryVariableName = $"_{mnemonic}_{this.OperandName(operand1)}_{this.OperandName(operand2)}";
        UtopIRVariable temporaryVariable = new(temporaryVariableName);
        instructions.Add(new ArithmeticInstruction(operation, temporaryVariable, operand1, operand2));
        return new VariableOperand(temporaryVariable);
    }

    /// <summary>
    /// Returns a short name string for a <see cref="UtopIROperand"/>, used when constructing
    /// temporary variable names.
    /// </summary>
    /// <param name="operand">The operand whose name to extract.</param>
    /// <returns>
    /// For a <see cref="VariableOperand"/>, the variable name (without <c>£</c>).
    /// For a <see cref="LiteralOperand"/>, the invariant-culture string representation of the value.
    /// </returns>
    private string OperandName(UtopIROperand operand) => operand switch
    {
        VariableOperand variable => variable.Variable.Name,
        LiteralOperand literal => Convert.ToString(literal.Value, CultureInfo.InvariantCulture) ?? "0",
        _ => throw new ArgumentOutOfRangeException(nameof(operand), operand, "Unknown operand type.")
    };

    /// <summary>
    /// Returns whether the given <see cref="Operator"/> is one of the arithmetic operators
    /// supported by the transformer.
    /// </summary>
    /// <param name="theOperator">The operator to test.</param>
    /// <returns><c>true</c> if the operator maps to a <see cref="UtopIRArithmeticOperation"/>, otherwise <c>false</c>.</returns>
    private bool IsArithmeticOperator(Operator theOperator) =>
        theOperator is Operator.Sum
            or Operator.Difference
            or Operator.Product
            or Operator.Quotient
            or Operator.Remainder
            or Operator.Larger
            or Operator.Smaller;

    /// <summary>
    /// Maps a Topsy Turvy <see cref="Operator"/> to the corresponding
    /// <see cref="UtopIRArithmeticOperation"/>.
    /// </summary>
    /// <param name="theOperator">The Topsy Turvy operator to map.</param>
    /// <returns>The corresponding <see cref="UtopIRArithmeticOperation"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="theOperator"/> is not an arithmetic operator.</exception>
    private UtopIRArithmeticOperation MapOperator(Operator theOperator) => theOperator switch
    {
        Operator.Sum => UtopIRArithmeticOperation.Sum,
        Operator.Difference => UtopIRArithmeticOperation.Diff,
        Operator.Product => UtopIRArithmeticOperation.Prod,
        Operator.Quotient => UtopIRArithmeticOperation.Quot,
        Operator.Remainder => UtopIRArithmeticOperation.Rem,
        Operator.Larger => UtopIRArithmeticOperation.Max,
        Operator.Smaller => UtopIRArithmeticOperation.Min,
        _ => throw new ArgumentOutOfRangeException(nameof(theOperator), theOperator, "Operator is not arithmetic.")
    };

    /// <summary>
    /// Returns the UtopIR mnemonic string for the given <see cref="UtopIRArithmeticOperation"/>.
    /// </summary>
    /// <param name="operation">The arithmetic operation.</param>
    /// <remarks>
    /// Used when generating temporary variable names.
    /// </remarks>
    /// <returns>The mnemonic string.</returns>
    private string OperationMnemonic(UtopIRArithmeticOperation operation) => operation switch
    {
        UtopIRArithmeticOperation.Sum => UtopIRKeywords.Instructions.Sum,
        UtopIRArithmeticOperation.Diff => UtopIRKeywords.Instructions.Diff,
        UtopIRArithmeticOperation.Prod => UtopIRKeywords.Instructions.Prod,
        UtopIRArithmeticOperation.Quot => UtopIRKeywords.Instructions.Quot,
        UtopIRArithmeticOperation.Rem => UtopIRKeywords.Instructions.Rem,
        UtopIRArithmeticOperation.Max => UtopIRKeywords.Instructions.Max,
        UtopIRArithmeticOperation.Min => UtopIRKeywords.Instructions.Min,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown arithmetic operation.")
    };

    /// <summary>
    /// Maps a Topsy Turvy <see cref="LiteralType"/> to the corresponding <see cref="UtopIRType"/>.
    /// </summary>
    /// <param name="type">The Topsy Turvy literal type to map.</param>
    /// <returns>The corresponding <see cref="UtopIRType"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="type"/> has no UtopIR equivalent.</exception>
    private UtopIRType MapType(LiteralType type) => type switch
    {
        LiteralType.Integer => UtopIRType.Peer,
        LiteralType.Long => UtopIRType.Chancellor,
        LiteralType.Short => UtopIRType.Pirate,
        LiteralType.SignedByte => UtopIRType.SausageRoll,
        LiteralType.UnsignedInteger => UtopIRType.StandingPeer,
        LiteralType.UnsignedLong => UtopIRType.StandingChancellor,
        LiteralType.UnsignedShort => UtopIRType.StandingPirate,
        LiteralType.Byte => UtopIRType.StandingSausageRoll,
        LiteralType.Double => UtopIRType.Fathom,
        LiteralType.Single => UtopIRType.Foot,
        LiteralType.Boolean => UtopIRType.Decree,
        LiteralType.Char => UtopIRType.Stitch,
        LiteralType.String => UtopIRType.Yarn,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"LiteralType '{type}' has no UtopIR equivalent.")
    };
}
