using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Recursive AST tree-walking interpreter for the Topsy Turvy language.
/// </summary>
/// <param name="io">The I/O handler used for all input and output operations.</param>
/// <remarks>
/// <para>
/// The interpreter is a tree-walking interpreter that executes the AST of a parsed programme directly.  It works by recursively
/// executing statements in an AST <see cref="ProgramNode"/> sequentially from beginning to end.  For each statement, the interpreter
/// uses pattern matching to determine which AST node type it is and executes the corresponding logic for that statement type.
/// Expressions are evaluated recursively in a similar manner when executing a statement.  During execution the interpreter
/// maintains a global environment, <see cref="TopsyTurvyEnvironment"/>, optionally with nested environments, that stores the
/// current state of all variables and their values, as well as the <c>JUST SO</c> implicit variable.  The interpreter also
/// maintains a dictionary of all function definitions encountered during execution.
/// </para>
/// <para>
/// During interpretation at runtime, the interpreter may encounter errors, such as referencing undefined variables, type errors
/// or division by zero, to name a few.  When an error occurs, the interpreter throws a <see cref="TopsyTurvyRuntimeException"/>
/// with a descriptive error message and the source span of the error and ends execution; it does not try to recover and continue
/// after an error occurs.  It should be noted that exceptions are used for control flow within the interpreter, specifically for
/// the break statement (<c>THAT WILL DO.</c>), continue statement (<c>ONCE MORE.</c>) and the return statement from a function
/// (<c>AND SO I FIND</c> and <c>MY DUTY IS PREMATURELY DISCHARGED.</c>).  The throw statement (<c>A HIDEOUS CURSE ON</c>) also
/// uses an exception, <see cref="TopsyTurvyThrowException"/>, but this is intended to be caught by a try-catch block within the
/// Topsy Turvy programme and should not be used for control flow in the interpreter itself.
/// </para>
/// <para>
/// The interpreter does not pre-scan the code, therefore, declarations and function definitions must be encountered before they can
/// be used.
/// </para>
/// <para>
/// The interpreter is initialised using an <see cref="ITopsyTurvyIO"/> implementation that handles all input and output operations.
/// This allows the interpreter to be used in different environments where input and output may be handled differently.  Once
/// initialised, a programme can be executed by calling the <see cref="Execute"/> method with a parsed <see cref="ProgramNode"/>.
/// The execution can be configured with:
/// * A <see cref="CancellationToken"/> to allow cancellation of the execution.
/// * An <see cref="InterpreterExecutionOptions"/> object to configure the execution.
/// </para>
/// <code>
/// string sourceText = File.ReadAllText("programme.topsy");
/// TopsyTurvyParser parser = new();
/// ProgramNode program = parser.Parse(sourceText);
///
/// ConsoleIO io = new();
/// Interpreter interpreter = new(io);
/// InterpreterExecutionOptions options = new(
///     ExecutionTimeout: TimeSpan.FromSeconds(30),
///     SourceFilePath: "programme.topsy",
///     SourceFileResolver: null);
/// DiagnosticCollection diagnostics = interpreter.Execute(program, options: options);
/// </code>
/// </remarks>
public sealed class Interpreter(ITopsyTurvyIO io)
{
    private readonly ITopsyTurvyIO io = io;
    private readonly Dictionary<string, FunctionDefinitionNode> functions = [];
    private CancellationToken cancellationToken;
    private DateTime executionTimeout = DateTime.MinValue;
    private string? sourceDirectory;
    private Func<string, string?>? fileResolver;

    /// <summary>
    /// Executes a parsed programme and returns a collection of runtime diagnostics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The main entry-point for the interpreter to execute a parsed Topsy Turvy programme as a <see cref="ProgramNode"/>.
    /// The interpretation follows a "tree-walk" process where the interpreter recursively executes statements in the AST
    /// sequentially from beginning to end.  On completion, an empty <see cref="DiagnosticCollection"/> indicates successful
    /// execution.
    /// </para>
    /// <para>
    /// The execution can be configured using an <see cref="InterpreterExecutionOptions"/> object.  One important configuration
    /// is the ability to specify a timeout to guard against infinite loops or excessively long execution times.  Pass a
    /// <see cref="CancellationToken"/> from a <see cref="CancellationTokenSource"/> to guard against infinite loops.  The
    /// <see cref="InterpreterExecutionOptions.ExecutionTimeout"/> property sets a deadline that is checked in addition to the
    /// cancellation token to allow timeouts to work in single-threaded environments such as Blazor WebAssembly.
    /// </para>
    /// <para>
    /// When executing a programme, the a pre-existing <see cref="TopsyTurvyEnvironment"/> can be provided for re-use across
    /// multiple calls to <see cref="Execute"/>.  This enables the interpreter to be used in REPL sessions where the environment
    /// is preserved between calls.  When provided, a new global environment is not created and <c>THE PROPS</c> is not re-declared.
    /// </para>
    /// </remarks>
    /// <param name="program">The root node of the parsed programme.</param>
    /// <param name="cancellationToken">A token that can be used to cancel execution.</param>
    /// <param name="options">Optional execution options; pass <c>null</c> to use defaults.</param>
    /// <param name="sessionEnvironment">An optional pre-existing environment to reuse across multiple calls.</param>
    /// <returns>A <see cref="DiagnosticCollection"/> describing any runtime errors.</returns>
    public DiagnosticCollection Execute(ProgramNode program, CancellationToken cancellationToken = default, InterpreterExecutionOptions? options = null, TopsyTurvyEnvironment? sessionEnvironment = null)
    {
        this.cancellationToken = cancellationToken;
        this.executionTimeout = options?.ExecutionTimeout.HasValue == true
            ? DateTime.UtcNow + options.ExecutionTimeout.Value
            : DateTime.MinValue;

        this.sourceDirectory = options?.SourceFilePath is not null
            ? Path.GetDirectoryName(Path.GetFullPath(options.SourceFilePath))
            : null;

        this.fileResolver = options?.SourceFileResolver;

        DiagnosticCollection diagnostics = new();
        TopsyTurvyEnvironment environment = sessionEnvironment ?? TopsyTurvyEnvironment.CreateGlobal();

        if (sessionEnvironment is null)
        {
            List<TopsyTurvyValue> commandLineArgElements = options?.CommandLineArguments is not null
                ? [.. options.CommandLineArguments.Select(TopsyTurvyValue.String)]
                : [];
            environment.Declare(Keywords.SpecialNames.TheProps, TopsyTurvyValue.Array(commandLineArgElements), isConstant: true);
        }

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
        catch (OperationCanceledException)
        {
            diagnostics.Add(new("Execution timed out.", DiagnosticSeverity.Error, program.Span));
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
            case ArrayDeclarationNode arrayDeclaration:
                this.ExecuteArrayDeclaration(arrayDeclaration, environment);
                break;
            case AssignmentNode assignment:
                this.ExecuteAssignment(assignment, environment);
                break;
            case ArrayElementAssignmentNode arrayElementAssignment:
                this.ExecuteArrayElementAssignment(arrayElementAssignment, environment);
                break;
            case InPlaceCastNode inPlaceCast:
                this.ExecuteInPlaceCast(inPlaceCast, environment);
                break;
            case PrintNode print:
                this.ExecutePrint(print, environment);
                break;
            case InputNode input:
                this.ExecuteInput(input, environment);
                break;
            case ExpressionStatement expressionStatement:
                environment.JustSo = this.EvaluateExpression(expressionStatement.Expression, environment);
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
        this.ExecuteStatements(node.Declarations, environment);
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

        environment.Declare(node.Name, value, isConstant: node.IsConstant);
    }

    /// <summary>
    /// Executes an array declaration statement.
    /// </summary>
    /// <param name="node">The array declaration node.</param>
    /// <param name="environment">The environment.</param>
    /// <exception cref="TopsyTurvyRuntimeException">
    /// Thrown when <see cref="ArrayDeclarationNode.Size"/> is set and <see cref="ArrayDeclarationNode.InitialValues"/>
    /// is non-empty (mutually exclusive), or when <see cref="ArrayDeclarationNode.Size"/> is negative.
    /// </exception>
    private void ExecuteArrayDeclaration(ArrayDeclarationNode node, TopsyTurvyEnvironment environment)
    {
        if (node.Size.HasValue && node.InitialValues.Count > 0)
        {
            throw new TopsyTurvyRuntimeException(
                $"Array '{node.Name}' specifies both a size and a BEING initialiser: these are mutually exclusive.",
                node.Span);
        }

        if (node.Size.HasValue && node.Size.Value < 0)
        {
            throw new TopsyTurvyRuntimeException(
                $"Array '{node.Name}' was declared with a negative size ({node.Size.Value}).",
                node.Span);
        }

        List<TopsyTurvyValue> elements = node.Size.HasValue
            ? [.. Enumerable.Repeat(TopsyTurvyValue.Null(), node.Size.Value)]
            : [.. node.InitialValues.Select(expression => this.EvaluateExpression(expression, environment))];

        environment.Declare(node.Name, TopsyTurvyValue.Array(elements), isConstant: node.IsConstant);
    }

    /// <summary>
    /// Executes an array element assignment statement.
    /// </summary>
    /// <param name="node">The array element assignment node.</param>
    /// <param name="environment">The environment.</param>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the index is not an integer, the variable is not an array, the array is CONSERVATIVE, or the index is out of range.</exception>
    private void ExecuteArrayElementAssignment(ArrayElementAssignmentNode node, TopsyTurvyEnvironment environment)
    {
        (int index, List<TopsyTurvyValue> elements) = this.ResolveArrayElement(node.Index, node.ArrayName, node.Span, environment);

        if (environment.IsConstantInChain(node.ArrayName))
        {
            throw new TopsyTurvyRuntimeException(
                $"'{node.ArrayName}' is CONSERVATIVE: its elements cannot be reassigned.",
                node.Span);
        }

        elements[index - 1] = this.EvaluateExpression(node.Value, environment);
    }

    /// <summary>
    /// Resolves an array element by evaluating the index expression and returning the 1-based position and the element list.
    /// </summary>
    /// <param name="indexExpression">The expression that evaluates to the 1-based element index.</param>
    /// <param name="arrayName">The name of the array variable.</param>
    /// <param name="span">The source span used in error messages.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The validated 1-based index and the backing element list.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the index is not an integer, the variable is not an array, or the index is out of range.</exception>
    private (int Index, List<TopsyTurvyValue> Elements) ResolveArrayElement(
        Expression indexExpression,
        string arrayName,
        SourceSpan span,
        TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue indexValue = this.EvaluateExpression(indexExpression, environment);
        if (indexValue.LiteralType != LiteralType.Integer)
        {
            throw new TopsyTurvyRuntimeException(
                $"Array index must be a PEER (integer), got {indexValue.LiteralType}.",
                span);
        }

        TopsyTurvyValue arrayValue = environment.Get(arrayName);
        if (arrayValue.LiteralType != LiteralType.Array)
        {
            throw new TopsyTurvyRuntimeException(
                $"'{arrayName}' is not an array.",
                span);
        }

        int index = (int)indexValue.RawValue!;
        List<TopsyTurvyValue> elements = (List<TopsyTurvyValue>)arrayValue.RawValue!;

        if (index < 1 || index > elements.Count)
        {
            throw new TopsyTurvyRuntimeException(
                $"Array index {index} is out of range for '{arrayName}' (length {elements.Count}).",
                span);
        }

        return (index, elements);
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
    /// <remarks>
    /// Case fall-through is the default behaviour in Topsy Turvy unless a <c>THAT WILL DO.</c> (break) statement is used to
    /// exit the switch block.  This means that once a case is matched, all subsequent cases will be executed until the end of the
    /// switch block or a break statement is encountered, even if their case literal does not match the switch expression value.
    /// </remarks>
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
    /// Checks if the execution timeout has been reached or if cancellation has been requested.
    /// </summary>
    /// <exception cref="OperationCanceledException">Thrown when execution should be cancelled.</exception>
    private void CheckCancellation()
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        if (this.executionTimeout != DateTime.MinValue && DateTime.UtcNow > this.executionTimeout)
        {
            throw new OperationCanceledException(this.cancellationToken);
        }
    }

    /// <summary>
    /// Checks for cancellation, then executes the loop body statements.
    /// </summary>
    /// <param name="body">The loop body statements to execute.</param>
    /// <param name="environment">The environment.</param>
    /// <returns><c>true</c> if execution should continue to the next iteration, <c>false</c> if a break was requested.</returns>
    private bool ExecuteLoopBody(IReadOnlyList<Statement> body, TopsyTurvyEnvironment environment)
    {
        this.CheckCancellation();
        try
        {
            this.ExecuteStatements(body, environment);
        }
        catch (ContinueSignalException)
        {
        }
        catch (BreakSignalException)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Executes an infinite loop.
    /// </summary>
    /// <param name="loopBody">The body of the loop.</param>
    /// <param name="environment">The environment.</param>
    private void ExecuteInfiniteLoop(IReadOnlyList<Statement> loopBody, TopsyTurvyEnvironment environment)
    {
        while (this.ExecuteLoopBody(loopBody, environment))
        {
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
            if (!this.ExecuteLoopBody(node.Body, environment))
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
            if (!this.ExecuteLoopBody(node.Body, environment))
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
            if (!this.ExecuteLoopBody(node.Body, environment))
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
            if (node.CaughtValueName is not null)
            {
                TopsyTurvyEnvironment catchEnvironment = environment.CreateNested();
                catchEnvironment.Declare(node.CaughtValueName, ex.ThrowValue);
                this.ExecuteStatements(node.ExceptionBlock, catchEnvironment);
            }
            else
            {
                this.ExecuteStatements(node.ExceptionBlock, environment);
            }
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
        if (this.fileResolver is not null)
        {
            sourceToImport = this.fileResolver(importNode.FilePath)
                ?? throw new TopsyTurvyRuntimeException($"Cannot resolve import '{importNode.FilePath}'.", importNode.Span);
        }
        else
        {
            string resolvedPath = this.sourceDirectory is not null && !Path.IsPathRooted(importNode.FilePath)
                ? Path.GetFullPath(Path.Combine(this.sourceDirectory, importNode.FilePath))
                : importNode.FilePath;

            try
            {
                sourceToImport = File.ReadAllText(resolvedPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new TopsyTurvyRuntimeException($"Cannot read import '{resolvedPath}': {ex.Message}", importNode.Span);
            }
        }

        TopsyTurvyParser parser = new();
        ProgramNode imported;
        try
        {
            imported = parser.Parse(sourceToImport);
        }
        catch (TopsyTurvySyntaxException ex)
        {
            string errors = string.Join("; ", ex.Errors);
            throw new TopsyTurvyRuntimeException($"Syntax errors in import '{importNode.FilePath}': {errors}", importNode.Span);
        }

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
        LiteralNode literal => EvaluateLiteral(literal),
        IdentifierNode ident => environment.Get(ident.Name),
        PrefixExpressionNode prefix => EvaluatePrefix(prefix, environment),
        ExpressionCastNode cast => this.EvaluateExpression(cast.Expression, environment).CastTo(cast.NewType),
        ArrayIndexNode arrayIndex => this.EvaluateArrayIndex(arrayIndex, environment),
        ArrayLengthNode arrayLength => this.EvaluateArrayLength(arrayLength, environment),
        _ => throw new TopsyTurvyRuntimeException(
            $"Unhandled expression type: {expression.GetType().Name}",
            expression.Span)
    };

    /// <summary>
    /// Evaluates an array index expression and returns the element at the given 1-based position.
    /// </summary>
    /// <param name="node">The array index node.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the index is not an integer, the variable is not an array, or the index is out of range.</exception>
    private TopsyTurvyValue EvaluateArrayIndex(ArrayIndexNode node, TopsyTurvyEnvironment environment)
    {
        (int index, List<TopsyTurvyValue> elements) = this.ResolveArrayElement(node.Index, node.ArrayName, node.Span, environment);
        return elements[index - 1];
    }

    /// <summary>
    /// Evaluates an array length expression and returns the number of elements as a <c>PEER</c>.
    /// </summary>
    /// <param name="node">The array length node.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The element count as an integer value.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the named variable is not an array.</exception>
    private TopsyTurvyValue EvaluateArrayLength(ArrayLengthNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue arrayValue = environment.Get(node.ArrayName);
        if (arrayValue.LiteralType != LiteralType.Array)
        {
            throw new TopsyTurvyRuntimeException(
                $"'{node.ArrayName}' is not an array.",
                node.Span);
        }

        List<TopsyTurvyValue> elements = (List<TopsyTurvyValue>)arrayValue.RawValue!;
        return TopsyTurvyValue.Integer(elements.Count);
    }

    /// <summary>
    /// Evaluates a literal expression and returns its value.
    /// </summary>
    /// <param name="node">The literal node.</param>
    /// <returns>The value of the literal.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the literal type is unknown.</exception>
    private static TopsyTurvyValue EvaluateLiteral(LiteralNode node) => node.Type switch
    {
        LiteralType.Integer => TopsyTurvyValue.Integer((int)node.Value!),
        LiteralType.Float => TopsyTurvyValue.Float((double)node.Value!),
        LiteralType.String => TopsyTurvyValue.String((string)node.Value!),
        LiteralType.Boolean => TopsyTurvyValue.Boolean((bool)node.Value!),
        LiteralType.Null => TopsyTurvyValue.Null(),
        _ => throw new TopsyTurvyRuntimeException($"Unknown literal type: {node.Type}")
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
                {
                    return TopsyTurvyValue.Boolean(false);
                }

                return TopsyTurvyValue.Boolean(this.EvaluateExpression(node.Arguments[1], environment).IsTruthy());
            case Operator.Either:
                if (this.EvaluateExpression(node.Arguments[0], environment).IsTruthy())
                {
                    return TopsyTurvyValue.Boolean(true);
                }

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
                return this.EvaluateBinaryOperator(node.Operator, left, right, node.Span);
        }
    }

    /// <summary>
    /// Evaluates a binary operator with the given operands and returns the result.
    /// </summary>
    /// <param name="binaryOperator">The binary operator.</param>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="span">The source span of the operator.</param>
    /// <returns>The result of the binary operation.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the binary operator is unhandled.</exception>
    private TopsyTurvyValue EvaluateBinaryOperator(Operator binaryOperator, TopsyTurvyValue leftOperand, TopsyTurvyValue rightOperand, SourceSpan span)
    {
        switch (binaryOperator)
        {
            case Operator.Sum:
                return ApplyArithmetic(leftOperand, rightOperand, (a, b) => a + b, (a, b) => a + b, span, "SUM OF");
            case Operator.Difference:
                return ApplyArithmetic(leftOperand, rightOperand, (a, b) => a - b, (a, b) => a - b, span, "DIFFERENCE OF");
            case Operator.Product:
                return ApplyArithmetic(leftOperand, rightOperand, (a, b) => a * b, (a, b) => a * b, span, "PRODUCT OF");
            case Operator.Quotient:
                return ApplyQuotient(leftOperand, rightOperand, span);
            case Operator.Remainder:
                return ApplyRemainder(leftOperand, rightOperand, span);
            case Operator.Larger:
                return SelectValue(leftOperand, rightOperand, selectIsGreater: true, span);
            case Operator.Smaller:
                return SelectValue(leftOperand, rightOperand, selectIsGreater: false, span);
            case Operator.Alike:
                return TopsyTurvyValue.Boolean(AreEqual(leftOperand, rightOperand));
            case Operator.Unlike:
                return TopsyTurvyValue.Boolean(!AreEqual(leftOperand, rightOperand));
            case Operator.PreAdamite:
                if (!IsNumeric(leftOperand) || !IsNumeric(rightOperand))
                {
                    throw new TopsyTurvyRuntimeException(
                        $"PRE-ADAMITE requires numeric operands, got {leftOperand.LiteralType} and {rightOperand.LiteralType}.",
                        span);
                }

                return TopsyTurvyValue.Boolean(CompareNumeric(leftOperand, rightOperand, span) > 0);
            case Operator.LowerDegree:
                if (!IsNumeric(leftOperand) || !IsNumeric(rightOperand))
                {
                    throw new TopsyTurvyRuntimeException(
                        $"LOWER DEGREE requires numeric operands, got {leftOperand.LiteralType} and {rightOperand.LiteralType}.",
                        span);
                }

                return TopsyTurvyValue.Boolean(CompareNumeric(leftOperand, rightOperand, span) < 0);
            default:
                throw new TopsyTurvyRuntimeException($"Unhandled binary operator: {binaryOperator}", span);
        }
    }

    /// <summary>
    /// Applies an arithmetic operation to two operands.
    /// </summary>
    /// <remarks>
    /// If both operands are integers, the integer operation is applied and an
    /// integer result is returned.  If at least one operand is floating-point, both
    /// operands are converted to floats, the float operation is applied, and a
    /// floating-point result is returned.
    /// </remarks>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="integerOperation">The operation to apply if both operands are integers.</param>
    /// <param name="floatingPointOperation">The operation to apply if at least one operand is a float.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <param name="operatorName">The Topsy Turvy keyword for the operator, used in error messages.</param>
    /// <returns>The result of the arithmetic operation.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the operands are not numeric.</exception>
    private static TopsyTurvyValue ApplyArithmetic(
        TopsyTurvyValue leftOperand,
        TopsyTurvyValue rightOperand,
        Func<int, int, int> integerOperation,
        Func<double, double, double> floatingPointOperation,
        SourceSpan span,
        string operatorName)
    {
        if (leftOperand.LiteralType == LiteralType.Integer && rightOperand.LiteralType == LiteralType.Integer)
        {
            return TopsyTurvyValue.Integer(integerOperation((int)leftOperand.RawValue!, (int)rightOperand.RawValue!));
        }

        if (IsNumeric(leftOperand) && IsNumeric(rightOperand))
        {
            return TopsyTurvyValue.Float(floatingPointOperation(ToDouble(leftOperand), ToDouble(rightOperand)));
        }

        throw new TopsyTurvyRuntimeException(
            $"{operatorName} requires numeric operands, got {leftOperand.LiteralType} and {rightOperand.LiteralType}.",
            span);
    }

    /// <summary>
    /// Applies the quotient operation to two operands.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <remarks>
    /// Performs integer division if both operands are integers, or floating-point division otherwise.
    /// </remarks>
    /// <returns>The result of the division.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when division by zero occurs or operands are not numeric.</exception>
    private static TopsyTurvyValue ApplyQuotient(TopsyTurvyValue leftOperand, TopsyTurvyValue rightOperand, SourceSpan span)
    {
        if (leftOperand.LiteralType == LiteralType.Integer && rightOperand.LiteralType == LiteralType.Integer)
        {
            int divisor = (int)rightOperand.RawValue!;
            if (divisor == 0)
            {
                throw new TopsyTurvyRuntimeException("Division by zero in QUOTIENT OF.", span);
            }

            return TopsyTurvyValue.Integer((int)leftOperand.RawValue! / divisor);
        }

        if (IsNumeric(leftOperand) && IsNumeric(rightOperand))
        {
            double divisor = ToDouble(rightOperand);
            if (divisor == 0.0)
            {
                throw new TopsyTurvyRuntimeException("Division by zero in QUOTIENT OF.", span);                
            }

            return TopsyTurvyValue.Float(ToDouble(leftOperand) / divisor);
        }

        throw new TopsyTurvyRuntimeException(
            $"QUOTIENT OF requires numeric operands, got {leftOperand.LiteralType} and {rightOperand.LiteralType}.",
            span);
    }

    /// <summary>
    /// Applies the remainder operation to two operands.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <returns>The result of the remainder operation.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Exception thrown when division by zero occurs or operands are not integers.</exception>
    private static TopsyTurvyValue ApplyRemainder(TopsyTurvyValue leftOperand, TopsyTurvyValue rightOperand, SourceSpan span)
    {
        if (leftOperand.LiteralType == LiteralType.Integer && rightOperand.LiteralType == LiteralType.Integer)
        {
            int divisor = (int)rightOperand.RawValue!;
            if (divisor == 0)
            {
                throw new TopsyTurvyRuntimeException("Division by zero in REMAINDER OF.", span);                
            }

            return TopsyTurvyValue.Integer((int)leftOperand.RawValue! % divisor);
        }

        throw new TopsyTurvyRuntimeException(
            $"REMAINDER OF requires integer operands, got {leftOperand.LiteralType} and {rightOperand.LiteralType}.",
            span);
    }

    /// <summary>
    /// Selects either the left or right operand based on their comparison.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="selectIsGreater">If <c>true</c>, selects the greater operand, otherwise selects the lesser operand.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <returns>The selected operand.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the operands are not numeric.</exception>
    private static TopsyTurvyValue SelectValue(TopsyTurvyValue leftOperand, TopsyTurvyValue rightOperand, bool selectIsGreater, SourceSpan span)
    {
        if (!IsNumeric(leftOperand) || !IsNumeric(rightOperand))
        {
            throw new TopsyTurvyRuntimeException(
                $"LARGER OF / SMALLER OF require numeric operands, got {leftOperand.LiteralType} and {rightOperand.LiteralType}.",
                span);
        }

        int comparison = CompareNumeric(leftOperand, rightOperand, span);
        return (selectIsGreater
            ? comparison >= 0
            : comparison <= 0)
                ? leftOperand
                : rightOperand;
    }

    /// <summary>
    /// Determines whether two values are equal.
    /// </summary>
    /// <remarks>
    /// Comparisons are performed cross-type, for example, an integer of <c>5</c> is considered equal to a float of <c>5.0</c> as
    /// both are converted to floating-point for the comparison.
    /// </remarks>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <returns><c>true</c> if the values are equal, otherwise <c>false</c>.</returns>
    private static bool AreEqual(TopsyTurvyValue leftOperand, TopsyTurvyValue rightOperand)
    {
        if (leftOperand.LiteralType != rightOperand.LiteralType)
        {
            if (IsNumeric(leftOperand) && IsNumeric(rightOperand))
            {
                return ToDouble(leftOperand) == ToDouble(rightOperand);
            }

            return false;
        }

        return leftOperand.LiteralType switch
        {
            LiteralType.Integer => (int)leftOperand.RawValue! == (int)rightOperand.RawValue!,
            LiteralType.Float => (double)leftOperand.RawValue! == (double)rightOperand.RawValue!,
            LiteralType.String => string.Equals((string)leftOperand.RawValue!, (string)rightOperand.RawValue!, StringComparison.Ordinal),
            LiteralType.Boolean => (bool)leftOperand.RawValue! == (bool)rightOperand.RawValue!,
            LiteralType.Null => true,
            _ => false
        };
    }

    /// <summary>
    /// Compares two numeric values and returns an integer indicating their relative order.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <returns>An integer indicating the relative order of the operands.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the operands are not numeric.</exception>
    private static int CompareNumeric(TopsyTurvyValue leftOperand, TopsyTurvyValue rightOperand, SourceSpan span)
    {
        if (!IsNumeric(leftOperand) || !IsNumeric(rightOperand))
        {
            throw new TopsyTurvyRuntimeException(
                $"Numeric comparison requires numeric operands, got {leftOperand.LiteralType} and {rightOperand.LiteralType}.",
                span);
        }

        return ToDouble(leftOperand).CompareTo(ToDouble(rightOperand));
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

        List<TopsyTurvyValue> arguments = [.. node.Arguments
            .Skip(1)
            .Select(argument => EvaluateExpression(argument, environment))];

        return this.EvaluateFunctionCall(functionIdentifier.Name, arguments, environment, node.Span);
    }

    /// <summary>
    /// Evaluates a function call with the given name and arguments in the specified environment.
    /// </summary>
    /// <param name="functionName">The name of the function to call.</param>
    /// <param name="arguments">The list of arguments to pass to the function.</param>
    /// <param name="callingEnvironment">The environment from which the function is called.</param>
    /// <param name="span">The source span of the function call.</param>
    /// <returns>A <see cref="TopsyTurvyValue"/> representing the result of the function call.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the function call is invalid.</exception>
    private TopsyTurvyValue EvaluateFunctionCall(
        string functionName,
        List<TopsyTurvyValue> arguments,
        TopsyTurvyEnvironment callingEnvironment,
        SourceSpan span)
    {
        if (!this.functions.TryGetValue(functionName, out FunctionDefinitionNode? function))
        {
            throw new TopsyTurvyRuntimeException($"Function '{functionName}' is not defined.", span);
        }

        if (arguments.Count != function.Parameters.Count)
        {
            throw new TopsyTurvyRuntimeException(
                $"Function '{functionName}' expects {function.Parameters.Count} argument(s), got {arguments.Count}.",
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
            this.ExecuteStatements(function.Body, scope);
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
            return value.LiteralType == LiteralType.Null;
        }

        return literal switch
        {
            int integerLiteral => value.LiteralType == LiteralType.Integer && (int)value.RawValue! == integerLiteral,
            double doubleLiteral => value.LiteralType == LiteralType.Float && (double)value.RawValue! == doubleLiteral,
            string stringLiteral => value.LiteralType == LiteralType.String && string.Equals((string)value.RawValue!, stringLiteral, StringComparison.Ordinal),
            bool boolLiteral => value.LiteralType == LiteralType.Boolean && (bool)value.RawValue! == boolLiteral,
            _ => false
        };
    }

    /// <summary>
    /// Performs string interpolation on the given template.
    /// </summary>
    /// <remarks>
    /// If the interpolation does not match an identifier in the environment, the placeholder template is left intact in the
    /// output string.
    /// </remarks>
    /// <param name="template">The template string containing placeholders.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The interpolated string.</returns>
    private static string Interpolate(string template, TopsyTurvyEnvironment environment)
    {
        return Regex.Replace(template, @"\{([^}]+)\}", match =>
        {
            string identifier = match.Groups[1].Value;
            try
            {
                return environment.Get(identifier).ToString();
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
        value.LiteralType is LiteralType.Integer or LiteralType.Float;

    /// <summary>
    /// Converts a numeric value to a double.
    /// </summary>
    /// <param name="value">The numeric value to convert.</param>
    /// <returns>The converted double value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the value is not numeric.</exception>
    private static double ToDouble(TopsyTurvyValue value) => value.LiteralType switch
    {
        LiteralType.Integer => (double)(int)value.RawValue!,
        LiteralType.Float => (double)value.RawValue!,
        _ => throw new InvalidOperationException("Value is not numeric.")
    };
}
