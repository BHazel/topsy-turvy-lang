using System;
using System.Collections.Generic;
using System.Globalization;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

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
/// Naming is delegated to an <see cref="ITemporaryVariableNameFormatter"/>, supplied via
/// the constructor, by default an <see cref="IncrementingIntVariableFormatter"/>.
/// </para>
/// <para>
/// ## Widening
/// Mixed-type arithmetic, e.g. <c>SUM OF &lt;peer&gt; AND &lt;chancellor&gt;</c>, is
/// legal in Topsy Turvy.  The result type is the wider of the two operand types, matching the
/// <c>TypeCheckVisitor.Widen</c> ordering, an independent copy, consistent with this project
/// existing pattern of per-layer widening tables.  When an <see cref="ArithmeticInstruction"/>
/// operands would otherwise differ in type, the narrower operand is cast into a temporary register
/// via an inserted <see cref="WereInstruction"/> before the arithmetic instruction is emitted, so
/// every <see cref="ArithmeticInstruction"/> the transformer produces always has matching operand types.
/// </para>
/// <para>
/// ## Scope
/// This initial implementation covers the instruction set defined in
/// UtopIR v0.0.1-preview1: variable declaration/assignment, arithmetic operations, and
/// top-level programme return.  Constant inlining, functions, control flow, and non-numeric
/// types are deferred to later versions.
/// </para>
/// <para>
/// ## Constants
/// Topsy Turvy <c>CONSERVATIVE</c> declarations are currently lowered
/// identically to mutable declarations.  Constant inlining (replacing every reference with
/// its literal value) requires a full symbol-table pass and is deferred.
/// </para>
/// </remarks>
/// <param name="formatter">The formatter used to name temporary virtual registers, or <c>null</c> to use a fresh <see cref="IncrementingIntVariableFormatter"/>.
/// </param>
public sealed class TopsyTurvyToUtopIRTransformer(ITemporaryVariableNameFormatter? formatter = null)
{
    private readonly ITemporaryVariableNameFormatter formatter = formatter ?? new IncrementingIntVariableFormatter();

    /// <summary>
    /// Defines the order of numeric <see cref="UtopIRType"/> variants for widening conversions
    /// where index 0 is widest, mirroring <c>TypeCheckVisitor.NumericWideningOrder</c>.
    /// </summary>
    private static readonly UtopIRType[] NumericWideningOrder =
    [
        UtopIRType.Fathom,
        UtopIRType.Foot,
        UtopIRType.StandingChancellor,
        UtopIRType.Chancellor,
        UtopIRType.StandingPeer,
        UtopIRType.Peer,
        UtopIRType.StandingPirate,
        UtopIRType.Pirate,
        UtopIRType.StandingSausageRoll,
        UtopIRType.SausageRoll
    ];

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
        Dictionary<string, UtopIRType> declaredTypes = [];

        foreach (Statement statement in program.Statements)
        {
            this.TransformStatement(statement, instructions, declaredTypes);
        }

        return new UtopIRProgram(instructions);
    }

    /// <summary>
    /// Dispatches a single top-level or block-level statement for transformation.
    /// </summary>
    /// <param name="statement">The statement to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    /// <param name="declaredTypes">The map from variable name to its known <see cref="UtopIRType"/>, used for widening.</param>
    private void TransformStatement(Statement statement, List<UtopIRInstruction> instructions, Dictionary<string, UtopIRType> declaredTypes)
    {
        switch (statement)
        {
            case PrincipalBlockNode principalBlock:
                foreach (Statement declaration in principalBlock.Declarations)
                {
                    this.TransformStatement(declaration, instructions, declaredTypes);
                }

                break;
            case DeclarationNode declaration:
                this.TransformDeclaration(declaration, instructions, declaredTypes);
                break;
            case AssignmentNode assignment:
                this.TransformAssignment(assignment, instructions, declaredTypes);
                break;
            case ProgrammeReturnNode programmeReturn:
                this.TransformProgrammeReturn(programmeReturn, instructions, declaredTypes);
                break;
        }
    }

    /// <summary>
    /// Transforms a <see cref="DeclarationNode"/> into a <see cref="WelcomeInstruction"/>
    /// followed by an optional <see cref="AppointInstruction"/> when an initial value is present.
    /// </summary>
    /// <remarks>
    /// When the declared type is an integer type and the initial value inferred type differs (most
    /// commonly because integer literals default to <see cref="UtopIRType.Peer"/>/<see cref="UtopIRType.Chancellor"/>
    /// regardless of the declared target type), a <see cref="WereInstruction"/> cast is inserted via
    /// <see cref="CastOperandIfNeeded"/> so the emitted <see cref="AppointInstruction"/> never stores a
    /// mismatched-width value directly.
    /// </remarks>
    /// <param name="declaration">The declaration statement to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    /// <param name="declaredTypes">The map from variable name to its known <see cref="UtopIRType"/>, used for widening.</param>
    private void TransformDeclaration(DeclarationNode declaration, List<UtopIRInstruction> instructions, Dictionary<string, UtopIRType> declaredTypes)
    {
        UtopIRVariable target = new(declaration.Name);
        UtopIRType type = this.MapType(declaration.Type);
        instructions.Add(new WelcomeInstruction(target, type));
        declaredTypes[declaration.Name] = type;

        if (declaration.InitialValue is not null)
        {
            UtopIROperand value = this.TransformExpression(declaration.InitialValue, instructions, declaredTypes);
            if (this.IsIntegerType(type))
            {
                UtopIRType valueType = this.InferOperandType(value, declaredTypes);
                value = this.CastOperandIfNeeded(value, valueType, type, instructions, declaredTypes);
            }

            instructions.Add(new AppointInstruction(target, value));
        }
    }

    /// <summary>
    /// Transforms an <see cref="AssignmentNode"/> into an <see cref="AppointInstruction"/>,
    /// flattening the right-hand-side expression first if needed.
    /// </summary>
    /// <remarks>
    /// Casts the value to the target declared type when needed, mirroring <see cref="TransformDeclaration"/>.
    /// The target is already declared by the time an assignment can reference it.
    /// </remarks>
    /// <param name="assignment">The assignment statement to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    /// <param name="declaredTypes">The map from variable name to its known <see cref="UtopIRType"/>, used for widening.</param>
    private void TransformAssignment(AssignmentNode assignment, List<UtopIRInstruction> instructions, Dictionary<string, UtopIRType> declaredTypes)
    {
        UtopIRVariable target = new(assignment.Target);
        UtopIROperand value = this.TransformExpression(assignment.Value, instructions, declaredTypes);
        UtopIRType targetType = declaredTypes[assignment.Target];
        if (this.IsIntegerType(targetType))
        {
            UtopIRType valueType = this.InferOperandType(value, declaredTypes);
            value = this.CastOperandIfNeeded(value, valueType, targetType, instructions, declaredTypes);
        }

        instructions.Add(new AppointInstruction(target, value));
    }

    /// <summary>
    /// Transforms a <see cref="ProgrammeReturnNode"/> into a <see cref="FindInstruction"/>,
    /// flattening the return-value expression first if needed.
    /// </summary>
    /// <param name="programmeReturn">The programme return statement to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    /// <param name="declaredTypes">The map from variable name to its known <see cref="UtopIRType"/>, used for widening.</param>
    private void TransformProgrammeReturn(ProgrammeReturnNode programmeReturn, List<UtopIRInstruction> instructions, Dictionary<string, UtopIRType> declaredTypes)
    {
        UtopIROperand value = this.TransformExpression(programmeReturn.Value, instructions, declaredTypes);
        instructions.Add(new FindInstruction(value));
    }

    /// <summary>
    /// Transforms an expression into a <see cref="UtopIROperand"/>, emitting any necessary
    /// intermediate arithmetic instructions first.
    /// </summary>
    /// <param name="expression">The expression to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    /// <param name="declaredTypes">The map from variable name to its known <see cref="UtopIRType"/>, used for widening.</param>
    /// <returns>
    /// A <see cref="LiteralOperand"/> for literal nodes, a <see cref="VariableOperand"/> for
    /// identifier nodes or a <see cref="VariableOperand"/> referencing a newly emitted
    /// temporary register for arithmetic expressions.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the expression type is not supported in the current transformer scope.</exception>
    private UtopIROperand TransformExpression(Expression expression, List<UtopIRInstruction> instructions, Dictionary<string, UtopIRType> declaredTypes)
    {
        switch (expression)
        {
            case LiteralNode literal:
                return new LiteralOperand(literal.Value!);
            case IdentifierNode identifier:
                return new VariableOperand(new UtopIRVariable(identifier.Name));
            case PrefixExpressionNode prefix when this.IsArithmeticOperator(prefix.Operator):
                return this.TransformArithmetic(prefix, instructions, declaredTypes);
            case ExpressionCastNode cast:
                return this.TransformCast(cast, instructions, declaredTypes);
            default:
                throw new NotSupportedException(
                    $"Expression type '{expression.GetType().Name}' with operator " +
                    $"'{(expression is PrefixExpressionNode prefixExpression ? prefixExpression.Operator : "(none)")}' " +
                    $"is not supported by the transformer in this version.");
        }
    }

    /// <summary>
    /// Transforms an arithmetic <see cref="PrefixExpressionNode"/> by recursively flattening
    /// its operands, widening the narrower operand into a temporary register if the operand
    /// types differ, emitting an <see cref="ArithmeticInstruction"/> into a temporary register,
    /// and returning a <see cref="VariableOperand"/> referencing that register.
    /// </summary>
    /// <param name="prefix">The arithmetic prefix expression to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    /// <param name="declaredTypes">The map from variable name to its known <see cref="UtopIRType"/>, used for widening.</param>
    /// <returns>A <see cref="VariableOperand"/> for the temporary register holding the result.</returns>
    private VariableOperand TransformArithmetic(PrefixExpressionNode prefix, List<UtopIRInstruction> instructions, Dictionary<string, UtopIRType> declaredTypes)
    {
        UtopIRArithmeticOperation operation = this.MapOperator(prefix.Operator);
        string mnemonic = this.OperationMnemonic(operation);

        UtopIROperand operand1 = this.TransformExpression(prefix.Arguments[0], instructions, declaredTypes);
        UtopIROperand operand2 = this.TransformExpression(prefix.Arguments[1], instructions, declaredTypes);

        UtopIRType type1 = this.InferOperandType(operand1, declaredTypes);
        UtopIRType type2 = this.InferOperandType(operand2, declaredTypes);
        UtopIRType widenedType = this.Widen(type1, type2);

        operand1 = this.CastOperandIfNeeded(operand1, type1, widenedType, instructions, declaredTypes);
        operand2 = this.CastOperandIfNeeded(operand2, type2, widenedType, instructions, declaredTypes);

        string temporaryVariableName = this.formatter.CreateName(mnemonic, this.OperandName(operand1), this.OperandName(operand2));
        UtopIRVariable temporaryVariable = new(temporaryVariableName);
        instructions.Add(new ArithmeticInstruction(operation, temporaryVariable, operand1, operand2));
        declaredTypes[temporaryVariableName] = widenedType;
        return new VariableOperand(temporaryVariable);
    }

    /// <summary>
    /// Transforms an <c>AS IT WERE</c> cast by flattening its source expression,
    /// emitting a <see cref="WereInstruction"/> into a temporary register and
    /// returning a <see cref="VariableOperand"/> referencing that register.
    /// </summary>
    /// <param name="cast">The cast expression to transform.</param>
    /// <param name="instructions">The instruction list being built.</param>
    /// <param name="declaredTypes">The map from variable name to its known <see cref="UtopIRType"/>, used for widening.</param>
    /// <returns>A <see cref="VariableOperand"/> for the temporary register holding the cast value.</returns>
    private VariableOperand TransformCast(ExpressionCastNode cast, List<UtopIRInstruction> instructions, Dictionary<string, UtopIRType> declaredTypes)
    {
        UtopIROperand value = this.TransformExpression(cast.Expression, instructions, declaredTypes);
        UtopIRType targetType = this.MapType(cast.NewType);

        string temporaryVariableName = this.formatter.CreateName(UtopIRKeywords.Instructions.Were, this.OperandName(value), targetType.ToString().ToLowerInvariant());
        UtopIRVariable temporaryVariable = new(temporaryVariableName);
        instructions.Add(new WereInstruction(temporaryVariable, value, targetType));
        declaredTypes[temporaryVariableName] = targetType;
        return new VariableOperand(temporaryVariable);
    }

    /// <summary>
    /// Casts an operand into a new temporary register via an inserted <see cref="WereInstruction"/>
    /// when its type differs from <paramref name="targetType"/>.
    /// </summary>
    /// <param name="operand">The operand to cast if needed.</param>
    /// <param name="operandType">The operand already-known type.</param>
    /// <param name="targetType">The type the operand must be cast to.</param>
    /// <param name="instructions">The instruction list being built.</param>
    /// <param name="declaredTypes">The map from variable name to its known <see cref="UtopIRType"/>, used for widening.</param>
    /// <returns><paramref name="operand"/> unchanged if already of <paramref name="targetType"/>, otherwise a <see cref="VariableOperand"/> referencing the new temporary register.</returns>
    private UtopIROperand CastOperandIfNeeded(UtopIROperand operand, UtopIRType operandType, UtopIRType targetType, List<UtopIRInstruction> instructions, Dictionary<string, UtopIRType> declaredTypes)
    {
        if (operandType == targetType)
        {
            return operand;
        }

        string temporaryVariableName = this.formatter.CreateName(UtopIRKeywords.Instructions.Were, this.OperandName(operand), targetType.ToString().ToLowerInvariant());
        UtopIRVariable temporaryVariable = new(temporaryVariableName);
        instructions.Add(new WereInstruction(temporaryVariable, operand, targetType));
        declaredTypes[temporaryVariableName] = targetType;
        return new VariableOperand(temporaryVariable);
    }

    /// <summary>
    /// Determines whether the given <see cref="UtopIRType"/> is one of the integer types.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><c>true</c> if <paramref name="type"/> is an integer type, otherwise <c>false</c>.</returns>
    private bool IsIntegerType(UtopIRType type) =>
        type is UtopIRType.Chancellor
            or UtopIRType.Peer
            or UtopIRType.Pirate
            or UtopIRType.SausageRoll
            or UtopIRType.StandingChancellor
            or UtopIRType.StandingPeer
            or UtopIRType.StandingPirate
            or UtopIRType.StandingSausageRoll;

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
    /// Infers the <see cref="UtopIRType"/> of a <see cref="UtopIROperand"/>, used to decide whether
    /// arithmetic operands need widening.
    /// </summary>
    /// <param name="operand">The operand to infer a type for.</param>
    /// <param name="declaredTypes">The map from variable name to its already-known <see cref="UtopIRType"/>.</param>
    /// <returns>
    /// For a <see cref="VariableOperand"/>, the already-recorded type of the referenced variable.
    /// For a <see cref="LiteralOperand"/>, the type inferred from the literal own CLR runtime type.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when a <see cref="LiteralOperand"/> CLR runtime type has no corresponding <see cref="UtopIRType"/> in this version.</exception>
    private UtopIRType InferOperandType(UtopIROperand operand, Dictionary<string, UtopIRType> declaredTypes) => operand switch
    {
        VariableOperand variableOperand => declaredTypes[variableOperand.Variable.Name],
        LiteralOperand literalOperand => literalOperand.Value switch
        {
            long => UtopIRType.Chancellor,
            int => UtopIRType.Peer,
            short => UtopIRType.Pirate,
            sbyte => UtopIRType.SausageRoll,
            ulong => UtopIRType.StandingChancellor,
            uint => UtopIRType.StandingPeer,
            ushort => UtopIRType.StandingPirate,
            byte => UtopIRType.StandingSausageRoll,
            _ => throw new NotSupportedException($"Literal value of CLR type '{literalOperand.Value.GetType().Name}' has no corresponding UtopIR type in this version.")
        },
        _ => throw new NotSupportedException($"Operand type '{operand.GetType().Name}' is not supported.")
    };

    /// <summary>
    /// Returns the wider of two numeric <see cref="UtopIRType"/> variants, according to widening rules.
    /// </summary>
    /// <param name="a">The first numeric type.</param>
    /// <param name="b">The second numeric type.</param>
    /// <returns>The wider of the two types.</returns>
    private UtopIRType Widen(UtopIRType a, UtopIRType b) =>
        this.IndexOfNumericWidening(a) <= this.IndexOfNumericWidening(b)
            ? a
            : b;

    /// <summary>
    /// Gets the index of a <see cref="UtopIRType"/> in <see cref="NumericWideningOrder"/>.
    /// </summary>
    /// <param name="type">The numeric type.</param>
    /// <returns>The index of the type in the widening order, or <see cref="int.MaxValue"/> if not found.</returns>
    private int IndexOfNumericWidening(UtopIRType type)
    {
        for (int i = 0; i < NumericWideningOrder.Length; i++)
        {
            if (NumericWideningOrder[i] == type)
            {
                return i;
            }
        }

        return int.MaxValue;
    }

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
