using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Recursive AST-walking interpreter for the Topsy Turvy language.
/// </summary>
public sealed class Interpreter
{
    private readonly ITopsyTurvyIO io;
    private readonly Dictionary<string, FunctionDefinitionNode> functions = [];

    /// <summary>
    /// Initialises a new instance of the <see cref="Interpreter"/> class with the specified I/O handler.
    /// </summary>
    /// <param name="io">The I/O handler used for all input and output operations.</param>
    public Interpreter(ITopsyTurvyIO io)
    {
        this.io = io;
    }

    /// <summary>
    /// Executes a parsed programme and returns a collection of runtime diagnostics.
    /// </summary>
    /// <remarks>
    /// An empty collection with no errors indicates successful execution.
    /// </remarks>
    /// <param name="program">The root node of the parsed programme.</param>
    /// <returns>A <see cref="DiagnosticCollection"/> describing any runtime errors.</returns>
    public DiagnosticCollection Execute(ProgramNode program)
    {
        DiagnosticCollection diagnostics = new();
        TopsyTurvyEnvironment environment = TopsyTurvyEnvironment.CreateGlobal();

        try
        {
            this.ExecuteStatements(program.Statements, environment);
        }
        catch (TopsyTurvyRuntimeException ex)
        {
            diagnostics.Add(new(ex.Message, DiagnosticSeverity.Error, ex.Span ?? program.Span));
        }
        catch (TopsyTurvyThrowException ex)
        {
            diagnostics.Add(new(
                $"Unhandled exception (A HIDEOUS CURSE ON): {ex.ThrowValue}",
                DiagnosticSeverity.Error,
                program.Span));
        }

        return diagnostics;
    }

    /// <summary>
    /// Executes a list of statements.
    /// </summary>
    /// <param name="statements">The statements to execute.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteStatements(IReadOnlyList<Statement> statements, TopsyTurvyEnvironment environment)
    {
        foreach (Statement statement in statements)
        {
            this.ExecuteStatement(statement, environment);
        }
    }

    /// <summary>
    /// Executes a single statement.
    /// </summary>
    /// <param name="statement">The statement to execute.</param>
    /// <param name="environment">The environment.</param>
    /// <exception cref="ReturnSignalException">Thrown when a return statement is encountered.</exception>
    /// <exception cref="TopsyTurvyThrowException">Thrown when a throw statement is encountered.</exception>
    /// <exception cref="BreakSignalException">Thrown when a break statement is encountered.</exception>
    /// <exception cref="ContinueSignalException">Thrown when a continue statement is encountered.</exception>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when an unexpected runtime error occurs.</exception>
    private void ExecuteStatement(Statement statement, TopsyTurvyEnvironment environment)
    {
        switch (statement)
        {
            case PrincipalBlockNode principalBlock:
                this.ExecutePrincipalBlock(principalBlock, environment);
                break;
            case DeclarationNode declaration:
                this.ExecuteDeclaration(declaration, environment);
                break;
            case AssignmentNode assignment:
                this.ExecuteAssignment(assignment, environment);
                break;
            case InPlaceCastNode inPlaceCast:
                this.ExecuteInPlaceCast(inPlaceCast, environment);
                break;
            case ExpressionCastNode expressionCast:
                this.ExecuteExpressionCast(expressionCast, environment);
                break;
            case PrintNode print:
                this.ExecutePrint(print, environment);
                break;
            case InputNode input:
                this.ExecuteInput(input, environment);
                break;
            case ExpressionStatement exprStmt:
                environment.JustSo = this.EvaluateExpression(exprStmt.Expression, environment);
                break;
            case FunctionDefinitionNode functionDefinition:
                this.functions[functionDefinition.Name] = functionDefinition;
                break;
            case ReturnNode returnStatement:
                throw new ReturnSignalException(returnStatement.Value != null ? this.EvaluateExpression(returnStatement.Value, environment) : null);
            case ThrowNode throwStatement:
                throw new TopsyTurvyThrowException(this.EvaluateExpression(throwStatement.Value, environment));
            case BreakNode:
                throw new BreakSignalException();
            case ContinueNode:
                throw new ContinueSignalException();
            case ConditionalNode conditional:
                this.ExecuteConditional(conditional, environment);
                break;
            case SwitchNode switchNode:
                this.ExecuteSwitch(switchNode, environment);
                break;
            case LoopNode loop:
                this.ExecuteLoop(loop, environment);
                break;
            case TryCatchNode tryCatch:
                this.ExecuteTryCatch(tryCatch, environment);
                break;
            case ImportNode importNode:
                this.ExecuteImport(importNode);
                break;
            default:
                throw new TopsyTurvyRuntimeException(
                    $"Unhandled statement type: {statement.GetType().Name}",
                    statement.Span);
        }
    }

    /// <summary>
    /// Executes a principal block.
    /// </summary>
    /// <param name="node">The principal block node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecutePrincipalBlock(PrincipalBlockNode node, TopsyTurvyEnvironment environment)
    {
        foreach (DeclarationNode declaration in node.Declarations)
        {
            this.ExecuteDeclaration(declaration, environment);
        }
    }

    /// <summary>
    /// Executes a variable declaration statement.
    /// </summary>
    /// <param name="node">The declaration node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteDeclaration(DeclarationNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue value = node.InitialValue != null
            ? this.EvaluateExpression(node.InitialValue, environment)
            : TopsyTurvyValue.Null();

        environment.Declare(node.Name, value);
    }

    /// <summary>
    /// Executes an assignment statement.
    /// </summary>
    /// <param name="node">The assignment node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteAssignment(AssignmentNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue value = this.EvaluateExpression(node.Value, environment);
        environment.Assign(node.Target, value);
    }

    /// <summary>
    /// Executes an in-place cast statement.
    /// </summary>
    /// <param name="node">The in-place cast node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteInPlaceCast(InPlaceCastNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue value = environment.Get(node.Target).CastTo(node.NewType);
        environment.Assign(node.Target, value);
    }

    /// <summary>
    /// Executes an expression cast statement.
    /// </summary>
    /// <param name="node">The expression cast node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteExpressionCast(ExpressionCastNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue value = this.EvaluateExpression(node.Expression, environment).CastTo(node.NewType);
        environment.JustSo = value;
    }

    /// <summary>
    /// Executes a print statement.
    /// </summary>
    /// <param name="node">The print node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecutePrint(PrintNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue value = this.EvaluateExpression(node.Expression, environment);
        string text = Interpolate(value.ToString(), environment);
        this.io.WriteLine(text, node.SuppressNewline);
    }

    /// <summary>
    /// Executes an input statement.
    /// </summary>
    /// <param name="node">The input node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteInput(InputNode node, TopsyTurvyEnvironment environment)
    {
        string line = this.io.ReadLine();
        environment.Assign(node.Target, TopsyTurvyValue.String(line));
    }

    /// <summary>
    /// Executes a conditional block.
    /// </summary>
    /// <param name="node">The conditional node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteConditional(ConditionalNode node, TopsyTurvyEnvironment environment)
    {
        bool condition = node.Condition != null
            ? this.EvaluateExpression(node.Condition, environment).IsTruthy()
            : environment.JustSo.IsTruthy();

        if (condition)
        {
            this.ExecuteStatements(node.TrueBlock, environment);
            return;
        }

        foreach (ElseIfBranch branch in node.ElseIfs)
        {
            bool branchCondition = branch.Condition != null
                ? this.EvaluateExpression(branch.Condition, environment).IsTruthy()
                : environment.JustSo.IsTruthy();

            if (branchCondition)
            {
                this.ExecuteStatements(branch.Block, environment);
                return;
            }
        }

        if (node.ElseBlock.Count > 0)
        {
            this.ExecuteStatements(node.ElseBlock, environment);
        }
    }

    /// <summary>
    /// Executes a switch block.
    /// </summary>
    /// <param name="node">The switch node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteSwitch(SwitchNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue switchValue = node.Expression != null
            ? this.EvaluateExpression(node.Expression, environment)
            : environment.JustSo;

        bool isCaseMatched = false;
        try
        {
            foreach (SwitchCase switchCase in node.Cases)
            {
                if (!isCaseMatched && CaseMatches(switchValue, switchCase.Literal))
                {
                    isCaseMatched = true;
                }

                if (isCaseMatched)
                {
                    this.ExecuteStatements(switchCase.Block, environment);
                }
            }

            if (!isCaseMatched && node.DefaultBlock.Count > 0)
            {
                this.ExecuteStatements(node.DefaultBlock, environment);
            }
        }
        catch (BreakSignalException)
        {
        }
    }

    /// <summary>
    /// Executes a loop block.
    /// </summary>
    /// <param name="node">The loop node.</param>
    /// <param name="environment">The environment.</param>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when an unknown loop type is encountered.</exception>
    private void ExecuteLoop(LoopNode node, TopsyTurvyEnvironment environment)
    {
        switch (node.Type)
        {
            case LoopType.Infinite:
                this.ExecuteInfiniteLoop(node.Body, environment);
                break;
            case LoopType.Ascending:
                this.ExecuteAscendingLoop(node, environment);
                break;
            case LoopType.Descending:
                this.ExecuteDescendingLoop(node, environment);
                break;
            case LoopType.Whilst:
                this.ExecuteWhilstLoop(node, environment);
                break;
            default:
                throw new TopsyTurvyRuntimeException($"Unknown loop type: {node.Type}", node.Span);
        }
    }

    /// <summary>
    /// Executes an infinite loop.
    /// </summary>
    /// <param name="loopBody">The body of the loop.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteInfiniteLoop(IReadOnlyList<Statement> loopBody, TopsyTurvyEnvironment environment)
    {
        while (true)
        {
            try
            {
                this.ExecuteStatements(loopBody, environment);
            }
            catch (ContinueSignalException)
            {
            }
            catch (BreakSignalException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Executes an ascending loop.
    /// </summary>
    /// <param name="node">The loop node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteAscendingLoop(LoopNode node, TopsyTurvyEnvironment environment)
    {
        string loopVariable = node.LoopVariable!;
        environment.Assign(loopVariable, TopsyTurvyValue.Integer(0));

        while (!this.EvaluateExpression(node.Condition!, environment).IsTruthy())
        {
            try
            {
                this.ExecuteStatements(node.Body, environment);
            }
            catch (ContinueSignalException)
            {
            }
            catch (BreakSignalException)
            {
                return;
            }

            TopsyTurvyValue current = environment.Get(loopVariable);
            environment.Assign(loopVariable, TopsyTurvyValue.Integer((int)current.RawValue! + 1));
        }
    }

    /// <summary>
    /// Executes a descending loop.
    /// </summary>
    /// <param name="node">The loop node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteDescendingLoop(LoopNode node, TopsyTurvyEnvironment environment)
    {
        string loopVariable = node.LoopVariable!;

        while (!this.EvaluateExpression(node.Condition!, environment).IsTruthy())
        {
            try
            {
                this.ExecuteStatements(node.Body, environment);
            }
            catch (ContinueSignalException)
            {
            }
            catch (BreakSignalException)
            {
                return;
            }

            TopsyTurvyValue current = environment.Get(loopVariable);
            environment.Assign(loopVariable, TopsyTurvyValue.Integer((int)current.RawValue! - 1));
        }
    }

    /// <summary>
    /// Executes a while loop.
    /// </summary>
    /// <param name="node">The loop node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteWhilstLoop(LoopNode node, TopsyTurvyEnvironment environment)
    {
        while (this.EvaluateExpression(node.Condition!, environment).IsTruthy())
        {
            try
            {
                this.ExecuteStatements(node.Body, environment);
            }
            catch (ContinueSignalException)
            {
            }
            catch (BreakSignalException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Executes a try-catch block.
    /// </summary>
    /// <param name="node">The try-catch node.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteTryCatch(TryCatchNode node, TopsyTurvyEnvironment environment)
    {
        try
        {
            TopsyTurvyValue result = this.EvaluateExpression(node.Operation, environment);
            environment.JustSo = result;
            this.ExecuteStatements(node.SuccessBlock, environment);
        }
        catch (TopsyTurvyThrowException ex)
        {
            environment.JustSo = ex.ThrowValue;
            this.ExecuteStatements(node.ExceptionBlock, environment);
        }
    }

    /// <summary>
    /// Executes an import statement.
    /// </summary>
    /// <param name="importNode">The import node.</param>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the import cannot be read.</exception>
    private void ExecuteImport(ImportNode importNode)
    {
        string sourceToImport;
        try
        {
            sourceToImport = File.ReadAllText(importNode.FilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new TopsyTurvyRuntimeException($"Cannot read import '{importNode.FilePath}': {ex.Message}", importNode.Span);
        }

        TopsyTurvyParser parser = new();
        ProgramNode imported = parser.Parse(sourceToImport);
        foreach (Statement statement in imported.Statements)
        {
            if (statement is FunctionDefinitionNode functionDefinition)
            {
                this.functions[functionDefinition.Name] = functionDefinition;
            }
        }
    }

    /// <summary>
    /// Evaluates an expression and returns its value.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The value of the expression.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the expression type is unhandled.</exception>
    private TopsyTurvyValue EvaluateExpression(Expression expression, TopsyTurvyEnvironment environment) => expression switch
    {
        LiteralNode literal          => EvaluateLiteral(literal),
        IdentifierNode ident         => environment.Get(ident.Name),
        PrefixExpressionNode prefix  => EvaluatePrefix(prefix, environment),
        _                            => throw new TopsyTurvyRuntimeException(
                                            $"Unhandled expression type: {expression.GetType().Name}",
                                            expression.Span)
    };

    /// <summary>
    /// Evaluates a literal expression and returns its value.
    /// </summary>
    /// <param name="node">The literal node.</param>
    /// <returns>The value of the literal.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the literal type is unknown.</exception>
    private static TopsyTurvyValue EvaluateLiteral(LiteralNode node) => node.Type switch
    {
        LiteralType.Integer => TopsyTurvyValue.Integer((int)node.Value!),
        LiteralType.Float   => TopsyTurvyValue.Float((double)node.Value!),
        LiteralType.String  => TopsyTurvyValue.String((string)node.Value!),
        LiteralType.Boolean => TopsyTurvyValue.Boolean((bool)node.Value!),
        LiteralType.Null    => TopsyTurvyValue.Null(),
        _                   => throw new TopsyTurvyRuntimeException($"Unknown literal type: {node.Type}")
    };

    /// <summary>
    /// Evaluates a prefix expression and returns its value.
    /// </summary>
    /// <param name="node">The prefix expression node.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The value of the prefix expression.</returns>
    private TopsyTurvyValue EvaluatePrefix(PrefixExpressionNode node, TopsyTurvyEnvironment environment)
    {
        switch (node.Operator)
        {
            case Operator.Summon:
                return this.EvaluateSummon(node, environment);
            case Operator.HardlyEver:
                return TopsyTurvyValue.Boolean(!this.EvaluateExpression(node.Arguments[0], environment).IsTruthy());
            case Operator.Both:
                if (!this.EvaluateExpression(node.Arguments[0], environment).IsTruthy())
                    return TopsyTurvyValue.Boolean(false);
                return TopsyTurvyValue.Boolean(this.EvaluateExpression(node.Arguments[1], environment).IsTruthy());
            case Operator.Either:
                if (this.EvaluateExpression(node.Arguments[0], environment).IsTruthy())
                    return TopsyTurvyValue.Boolean(true);
                return TopsyTurvyValue.Boolean(this.EvaluateExpression(node.Arguments[1], environment).IsTruthy());
            case Operator.WovenOf:
                return this.EvaluateWovenOf(node.Arguments, environment);
            case Operator.AllOf:
                return this.EvaluateAllOf(node.Arguments, environment);
            case Operator.AnyOf:
                return this.EvaluateAnyOf(node.Arguments, environment);
            default:
                TopsyTurvyValue left = this.EvaluateExpression(node.Arguments[0], environment);
                TopsyTurvyValue right = this.EvaluateExpression(node.Arguments[1], environment);
                return this.EvaluateBinaryOp(node.Operator, left, right, node.Span);
        }
    }

    /// <summary>
    /// Evaluates a binary operator with the given operands and returns the result.
    /// </summary>
    /// <param name="op">The binary operator.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="span">The source span of the operator.</param>
    /// <returns>The result of the binary operation.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the binary operator is unhandled.</exception>
    private TopsyTurvyValue EvaluateBinaryOp(Operator op, TopsyTurvyValue left, TopsyTurvyValue right, SourceSpan span)
    {
        switch (op)
        {
            case Operator.Sum:
                return ApplyArithmetic(left, right, (a, b) => a + b, (a, b) => a + b, span, "SUM OF");
            case Operator.Difference:
                return ApplyArithmetic(left, right, (a, b) => a - b, (a, b) => a - b, span, "DIFFERENCE OF");
            case Operator.Product:
                return ApplyArithmetic(left, right, (a, b) => a * b, (a, b) => a * b, span, "PRODUCT OF");
            case Operator.Quotient:
                return ApplyQuotient(left, right, span);
            case Operator.Remainder:
                return ApplyRemainder(left, right, span);
            case Operator.Larger:
                return SelectValue(left, right, greater: true, span);
            case Operator.Smaller:
                return SelectValue(left, right, greater: false, span);
            case Operator.Alike:
                return TopsyTurvyValue.Boolean(AreEqual(left, right));
            case Operator.Unlike:
                return TopsyTurvyValue.Boolean(!AreEqual(left, right));
            case Operator.PreAdamite:
                if (!IsNumeric(left) || !IsNumeric(right))
                    throw new TopsyTurvyRuntimeException(
                        $"PRE-ADAMITE requires numeric operands, got {left.TopsyTurvyType} and {right.TopsyTurvyType}.",
                        span);
                return TopsyTurvyValue.Boolean(CompareNumeric(left, right, span) > 0);
            case Operator.LowerDegree:
                if (!IsNumeric(left) || !IsNumeric(right))
                    throw new TopsyTurvyRuntimeException(
                        $"LOWER DEGREE requires numeric operands, got {left.TopsyTurvyType} and {right.TopsyTurvyType}.",
                        span);
                return TopsyTurvyValue.Boolean(CompareNumeric(left, right, span) < 0);
            default:
                throw new TopsyTurvyRuntimeException($"Unhandled binary operator: {op}", span);
        }
    }

    /// <summary>
    /// Applies an arithmetic operation to two operands.
    /// </summary>
    /// <remarks>
    /// If both operands are integers, the integer operation is applied and an
    /// integer result is returned.  If at least one operand is a float, both
    /// operands are converted to floats, the float operation is applied, and a
    /// float result is returned.
    /// </remarks>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="intOp">The operation to apply if both operands are integers.</param>
    /// <param name="floatOp">The operation to apply if at least one operand is a float.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <param name="operatorName">The Topsy Turvy keyword for the operator, used in error messages.</param>
    /// <returns>The result of the arithmetic operation.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the operands are not numeric.</exception>
    private static TopsyTurvyValue ApplyArithmetic(
        TopsyTurvyValue left,
        TopsyTurvyValue right,
        Func<int, int, int> intOp,
        Func<double, double, double> floatOp,
        SourceSpan span,
        string operatorName)
    {
        if (left.TopsyTurvyType == LiteralType.Integer && right.TopsyTurvyType == LiteralType.Integer)
        {
            return TopsyTurvyValue.Integer(intOp((int)left.RawValue!, (int)right.RawValue!));
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            return TopsyTurvyValue.Float(floatOp(ToDouble(left), ToDouble(right)));
        }

        throw new TopsyTurvyRuntimeException(
            $"{operatorName} requires numeric operands, got {left.TopsyTurvyType} and {right.TopsyTurvyType}.",
            span);
    }

    /// <summary>
    /// Applies the quotient operation to two operands.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <remarks>
    /// Performs integer division if both operands are integers, or floating-point division otherwise.
    /// </remarks>
    /// <returns>The result of the division.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when division by zero occurs or operands are not numeric.</exception>
    private static TopsyTurvyValue ApplyQuotient(TopsyTurvyValue left, TopsyTurvyValue right, SourceSpan span)
    {
        if (left.TopsyTurvyType == LiteralType.Integer && right.TopsyTurvyType == LiteralType.Integer)
        {
            int divisor = (int)right.RawValue!;
            if (divisor == 0)
                throw new TopsyTurvyRuntimeException("Division by zero in QUOTIENT OF.", span);
            return TopsyTurvyValue.Integer((int)left.RawValue! / divisor);
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            double divisor = ToDouble(right);
            if (divisor == 0.0)
                throw new TopsyTurvyRuntimeException("Division by zero in QUOTIENT OF.", span);
            return TopsyTurvyValue.Float(ToDouble(left) / divisor);
        }

        throw new TopsyTurvyRuntimeException(
            $"QUOTIENT OF requires numeric operands, got {left.TopsyTurvyType} and {right.TopsyTurvyType}.",
            span);
    }

    /// <summary>
    /// Applies the remainder operation to two operands.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <returns>The result of the remainder operation.</returns>
    /// <exception cref="TopsyTurvyRuntimeException"></exception>
    private static TopsyTurvyValue ApplyRemainder(TopsyTurvyValue left, TopsyTurvyValue right, SourceSpan span)
    {
        if (left.TopsyTurvyType == LiteralType.Integer && right.TopsyTurvyType == LiteralType.Integer)
        {
            int divisor = (int)right.RawValue!;
            if (divisor == 0)
                throw new TopsyTurvyRuntimeException("Division by zero in REMAINDER OF.", span);
            return TopsyTurvyValue.Integer((int)left.RawValue! % divisor);
        }

        throw new TopsyTurvyRuntimeException(
            $"REMAINDER OF requires integer operands, got {left.TopsyTurvyType} and {right.TopsyTurvyType}.",
            span);
    }

    /// <summary>
    /// Selects either the left or right operand based on their comparison.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="greater">If <c>true</c>, selects the greater operand, otherwise selects the lesser operand.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <returns>The selected operand.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the operands are not numeric.</exception>
    private static TopsyTurvyValue SelectValue(
        TopsyTurvyValue left,
        TopsyTurvyValue right,
        bool greater,
        SourceSpan span)
    {
        if (!IsNumeric(left) || !IsNumeric(right))
        {
            throw new TopsyTurvyRuntimeException(
                $"LARGER OF / SMALLER OF require numeric operands, got {left.TopsyTurvyType} and {right.TopsyTurvyType}.",
                span);
        }

        int comparison = CompareNumeric(left, right, span);
        return (greater ? comparison >= 0 : comparison <= 0) ? left : right;
    }

    /// <summary>
    /// Determines whether two values are equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><c>true</c> if the values are equal, otherwise <c>false</c>.</returns>
    private static bool AreEqual(TopsyTurvyValue left, TopsyTurvyValue right)
    {
        if (left.TopsyTurvyType != right.TopsyTurvyType)
        {
            if (IsNumeric(left) && IsNumeric(right))
            {
                return ToDouble(left) == ToDouble(right);
            }

            return false;
        }

        return left.TopsyTurvyType switch
        {
            LiteralType.Integer => (int)left.RawValue! == (int)right.RawValue!,
            LiteralType.Float   => (double)left.RawValue! == (double)right.RawValue!,
            LiteralType.String  => string.Equals((string)left.RawValue!, (string)right.RawValue!, StringComparison.Ordinal),
            LiteralType.Boolean => (bool)left.RawValue! == (bool)right.RawValue!,
            LiteralType.Null    => true,
            _                   => false
        };
    }

    /// <summary>
    /// Compares two numeric values and returns an integer indicating their relative order.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <returns>An integer indicating the relative order of the operands.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the operands are not numeric.</exception>
    private static int CompareNumeric(TopsyTurvyValue left, TopsyTurvyValue right, SourceSpan span)
    {
        if (!IsNumeric(left) || !IsNumeric(right))
        {
            throw new TopsyTurvyRuntimeException(
                $"Numeric comparison requires numeric operands, got {left.TopsyTurvyType} and {right.TopsyTurvyType}.",
                span);
        }

        return ToDouble(left).CompareTo(ToDouble(right));
    }

    /// <summary>
    /// Evaluates the string concatenation operator.
    /// </summary>
    /// <remarks>
    /// Concatenates the string representations of all expressions with interpolation.
    /// </remarks>
    /// <param name="expressions">The list of expressions to concatenate.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>A <see cref="TopsyTurvyValue"/> representing the concatenated string.</returns>
    private TopsyTurvyValue EvaluateWovenOf(IReadOnlyList<Expression> expressions, TopsyTurvyEnvironment environment)
    {
        StringBuilder stringBuilder = new();
        foreach (Expression expression in expressions)
        {
            TopsyTurvyValue value = this.EvaluateExpression(expression, environment);
            stringBuilder.Append(Interpolate(value.ToString(), environment));
        }

        return TopsyTurvyValue.String(stringBuilder.ToString());
    }

    /// <summary>
    /// Evaluates the all-of operator.
    /// </summary>
    /// <param name="expressions">The list of expressions to evaluate.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>A <see cref="TopsyTurvyValue"/> representing the result of the all-of operation.</returns>
    private TopsyTurvyValue EvaluateAllOf(IReadOnlyList<Expression> expressions, TopsyTurvyEnvironment environment)
    {
        foreach (Expression expression in expressions)
        {
            if (!this.EvaluateExpression(expression, environment).IsTruthy())
            {
                return TopsyTurvyValue.Boolean(false);
            }
        }

        return TopsyTurvyValue.Boolean(true);
    }

    /// <summary>
    /// Evaluates the any-of operator.
    /// </summary>
    /// <param name="expressions">The list of expressions to evaluate.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>A <see cref="TopsyTurvyValue"/> representing the result of the any-of operation.</returns>
    private TopsyTurvyValue EvaluateAnyOf(IReadOnlyList<Expression> expressions, TopsyTurvyEnvironment environment)
    {
        foreach (Expression expression in expressions)
        {
            if (this.EvaluateExpression(expression, environment).IsTruthy())
            {
                return TopsyTurvyValue.Boolean(true);
            }
        }

        return TopsyTurvyValue.Boolean(false);
    }

    /// <summary>
    /// Evaluates a function call.
    /// </summary>
    /// <param name="node">The prefix expression node.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>A <see cref="TopsyTurvyValue"/> representing the result of the function call.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the function call is invalid.</exception>
    private TopsyTurvyValue EvaluateSummon(PrefixExpressionNode node, TopsyTurvyEnvironment environment)
    {
        if (node.Arguments.Count == 0 || node.Arguments[0] is not IdentifierNode functionIdentifier)
        {
            throw new TopsyTurvyRuntimeException(
                "SUMMON requires a function name as its first argument.",
                node.Span);
        }

        List<TopsyTurvyValue> arguments = node.Arguments
            .Skip(1)
            .Select(argument => EvaluateExpression(argument, environment))
            .ToList();

        return this.EvaluateFunctionCall(functionIdentifier.Name, arguments, environment, node.Span);
    }

    /// <summary>
    /// Evaluates a function call with the given name and arguments in the specified environment.
    /// </summary>
    /// <param name="name">The name of the function to call.</param>
    /// <param name="arguments">The list of arguments to pass to the function.</param>
    /// <param name="callingEnvironment">The environment from which the function is called.</param>
    /// <param name="span">The source span of the function call.</param>
    /// <returns>A <see cref="TopsyTurvyValue"/> representing the result of the function call.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the function call is invalid.</exception>
    private TopsyTurvyValue EvaluateFunctionCall(
        string name,
        List<TopsyTurvyValue> arguments,
        TopsyTurvyEnvironment callingEnvironment,
        SourceSpan span)
    {
        if (!functions.TryGetValue(name, out FunctionDefinitionNode? function))
        {
            throw new TopsyTurvyRuntimeException($"Function '{name}' is not defined.", span);
        }

        if (arguments.Count != function.Parameters.Count)
        {
            throw new TopsyTurvyRuntimeException(
                $"Function '{name}' expects {function.Parameters.Count} argument(s), got {arguments.Count}.",
                span);
        }

        TopsyTurvyEnvironment scope = TopsyTurvyEnvironment.CreateFunctionEnvironment();
        for (int i = 0; i < function.Parameters.Count; i++)
        {
            scope.Declare(function.Parameters[i], arguments[i]);
        }

        TopsyTurvyValue returnValue = TopsyTurvyValue.Null();
        try
        {
            ExecuteStatements(function.Body, scope);
        }
        catch (ReturnSignalException returnSignal)
        {
            returnValue = returnSignal.Value ?? TopsyTurvyValue.Null();
        }

        callingEnvironment.JustSo = returnValue;
        return returnValue;
    }

    /// <summary>
    /// Determines whether a switch case matches the given value.
    /// </summary>
    /// <param name="value">The value to compare against the case.</param>
    /// <param name="literal">The literal value of the case.</param>
    /// <returns><c>true</c> if the case matches the value, otherwise <c>false</c>.</returns>
    private static bool CaseMatches(TopsyTurvyValue value, object? literal)
    {
        if (literal == null)
        {
            return value.TopsyTurvyType == LiteralType.Null;
        }

        return literal switch
        {
            int i    => value.TopsyTurvyType == LiteralType.Integer && (int)value.RawValue! == i,
            double d => value.TopsyTurvyType == LiteralType.Float   && (double)value.RawValue! == d,
            string s => value.TopsyTurvyType == LiteralType.String  && string.Equals((string)value.RawValue!, s, StringComparison.Ordinal),
            bool b   => value.TopsyTurvyType == LiteralType.Boolean && (bool)value.RawValue! == b,
            _        => false
        };
    }

    /// <summary>
    /// Performs string interpolation on the given template.
    /// </summary>
    /// <param name="template">The template string containing placeholders.</param>
    /// <param name="environment">The environment..</param>
    /// <returns>The interpolated string.</returns>
    private static string Interpolate(string template, TopsyTurvyEnvironment environment)
    {
        return Regex.Replace(template, @"\{([^}]+)\}", match =>
        {
            string name = match.Groups[1].Value;
            try
            {
                return environment.Get(name).ToString();
            }
            catch (TopsyTurvyRuntimeException)
            {
                return match.Value;
            }
        });
    }

    /// <summary>
    /// Determines whether the given value is numeric.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value is numeric, otherwise <c>false</c>.</returns>
    private static bool IsNumeric(TopsyTurvyValue value) =>
        value.TopsyTurvyType is LiteralType.Integer or LiteralType.Float;

    /// <summary>
    /// Converts a numeric value to a double.
    /// </summary>
    /// <param name="value">The numeric value to convert.</param>
    /// <returns>The converted double value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the value is not numeric.</exception>
    private static double ToDouble(TopsyTurvyValue value) => value.TopsyTurvyType switch
    {
        LiteralType.Integer => (double)(int)value.RawValue!,
        LiteralType.Float   => (double)value.RawValue!,
        _                   => throw new InvalidOperationException("Value is not numeric.")
    };
}
