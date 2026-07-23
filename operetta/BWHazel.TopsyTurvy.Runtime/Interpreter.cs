using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Recursive AST tree-walking interpreter for the Topsy Turvy language.
/// </summary>
/// <param name="io">The I/O handler used for all input and output operations.</param>
/// <param name="externalFunctions">The catalogue of external functions (Standard Library and any external library) available to <c>SUMMON</c>.</param>
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
/// be used.  The sole exception is a file namespace declaration (<c>TOWN</c>): <see cref="Execute"/> and
/// <see cref="ExecuteImport"/> each pre-scan their own statement list once, before executing any of it, for the
/// at-most-one <see cref="NamespaceDeclarationNode"/> it may contain, so a function namespace membership never
/// depends on where in the file the <c>TOWN</c> keyword appears.  A namespace-open directive (<c>PRAY RECOGNISE</c>),
/// by contrast, follows the interpreter normal order-dependent rule: it only affects bare-name function
/// resolution from the point it is executed onward, exactly as <c>PRAY ADMIT</c> already does for imports.
/// </para>
/// <para>
/// A function declared in a file with a <c>TOWN</c> namespace is registered under its dot-joined fully-qualified
/// name, e.g. <c>Accounts.Payroll.CalculateTax</c>, rather than its bare name, and is not reachable by bare name
/// from outside that namespace.  <c>SUMMON</c> resolves a call name in four tiers, via <see cref="ResolveFunction"/>:
/// * A fully-qualified call name looked up directly,
/// * Then the namespace of the caller.
/// * Then each namespace opened via <c>PRAY RECOGNISE</c>.
/// * Then the global (non-namespaced) function table.
/// Please see <see cref="ResolveFunction"/> for the precise resolution order.
/// </para>
/// <para>
/// At each of those four tiers, a name with no matching Topsy Turvy function falls back to the <paramref name="externalFunctions"/>
/// catalogue, so global namespace functions work with no source-level declaration at all.  A Topsy Turvy function always
/// wins over an external one of the same name at the same tier, so adding a function to the Standard Library can never change
/// the behaviour of an existing programme.  <see cref="ExternalFunctionInvoker"/> is the sole owner of the marshalling between
/// <see cref="TopsyTurvyValue"/> and the CLR values a bound method actually takes and returns.
/// </para>
/// <para>
/// During execution, the interpreter can work with the 6 literal types that the lexer can produce:
/// * <see cref="LiteralType.Integer"/> (PEER)
/// * <see cref="LiteralType.Double"/> (FATHOM)
/// * <see cref="LiteralType.String"/> (YARN)
/// * <see cref="LiteralType.Char"/> (STITCH)
/// * <see cref="LiteralType.Boolean"/> (DECREE)
/// * <see cref="LiteralType.Null"/> (NAUGHT)
/// 
/// The remaining <see cref="LiteralType"/> members are runtime-only types that arise from
/// arithmetic widening or explicit casts: they are never produced as <see cref="LiteralNode"/>
/// instances by the parser.
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
public sealed class Interpreter(ITopsyTurvyIO io, BindingCatalogue? externalFunctions = null)
{
    private readonly ITopsyTurvyIO io = io;
    private readonly ExternalFunctionInvoker externalFunctions = new(externalFunctions ?? BindingCatalogue.Default);
    private readonly Dictionary<string, FunctionDefinitionNode> functions = [];
    private readonly Dictionary<FunctionDefinitionNode, string?> functionNamespaces = [];
    private readonly HashSet<string> openNamespaces = [];
    private CancellationToken cancellationToken;
    private DateTime executionTimeout = DateTime.MinValue;
    private string? sourceDirectory;
    private Func<string, string?>? fileResolver;
    private string? currentExecutionNamespace;

    /// <summary>
    /// Gets a read-only view of all functions defined in this interpreter instance.
    /// </summary>
    /// <remarks>
    /// The dictionary is keyed by the function name, dot-qualified with its declaring file
    /// namespace path when it was declared under a <c>TOWN</c> declaration, e.g. <c>Accounts.Payroll.CalculateTax</c>,
    /// or by its bare name when the declaring file has no <c>TOWN</c> declaration.
    /// </remarks>
    public IReadOnlyDictionary<string, FunctionDefinitionNode> Functions => this.functions;

    /// <summary>
    /// Gets the OS exit code produced by the most recent call to <see cref="Execute"/>.
    /// </summary>
    /// <remarks>
    /// Set to <c>0</c> at the start of every <see cref="Execute"/> call and updated if a top-level
    /// <c>AND SO I FIND &lt;expr&gt;</c> statement is executed.  Callers should read this
    /// property after <see cref="Execute"/> returns to propagate the exit code to the OS.
    /// </remarks>
    public int ExitCode { get; private set; }

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
        this.ExitCode = 0;
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
            string? entryNamespace = FindNamespaceDeclaration(program.Statements);
            if (entryNamespace is not null)
            {
                this.currentExecutionNamespace = entryNamespace;
            }

            this.ExecuteStatements(program.Statements, environment);
        }
        catch (ProgrammeReturnSignalException programmeReturnSignal)
        {
            this.ExitCode = programmeReturnSignal.ExitCode;
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
            case PointerDeclarationNode pointerDeclaration:
                this.ExecutePointerDeclaration(pointerDeclaration, environment);
                break;
            case AssignmentNode assignment:
                this.ExecuteAssignment(assignment, environment);
                break;
            case ArrayElementAssignmentNode arrayElementAssignment:
                this.ExecuteArrayElementAssignment(arrayElementAssignment, environment);
                break;
            case DereferenceAssignmentNode dereferenceAssignment:
                this.ExecuteDereferenceAssignment(dereferenceAssignment, environment);
                break;
            case PrintNode print:
                this.ExecutePrint(print, environment);
                break;
            case InputNode input:
                this.ExecuteInput(input, environment);
                break;
            case ExpressionStatement expressionStatement:
                this.EvaluateExpression(expressionStatement.Expression, environment);
                break;
            case FunctionDefinitionNode functionDefinition:
                this.functions[QualifyName(this.currentExecutionNamespace, functionDefinition.Name)] = functionDefinition;
                this.functionNamespaces[functionDefinition] = this.currentExecutionNamespace;
                break;
            case NamespaceDeclarationNode:
                // No-op here: applied by the pre-scan in Execute()/ExecuteImport() before statement
                // execution begins, so namespace membership does not depend on the TOWN keyword position
                // relative to the functions it applies to.
                break;
            case RecogniseNode recognise:
                this.openNamespaces.Add(string.Join('.', recognise.Path));
                break;
            case ProgrammeReturnNode programmeReturnStatement:
                throw new ProgrammeReturnSignalException((int)this.EvaluateExpression(programmeReturnStatement.Value, environment).RawValue!);
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
            case GuardNode guard:
                this.ExecuteGuard(guard, environment);
                break;
            case AssertNode assert:
                this.ExecuteAssert(assert, environment);
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
            : GetDefaultValue(node.Type);

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
            ? [.. Enumerable.Repeat(GetDefaultValue(node.ElementType), node.Size.Value)]
            : [.. node.InitialValues.Select(expression => this.EvaluateExpression(expression, environment))];

        environment.Declare(node.Name, TopsyTurvyValue.Array(elements), isConstant: node.IsConstant);
    }

    /// <summary>
    /// Executes a pointer declaration statement.
    /// </summary>
    /// <param name="node">The pointer declaration node.</param>
    /// <param name="environment">The environment.</param>
    /// <remarks>
    /// When no initial value is present, the pointer is declared as <c>NAUGHT</c> (unassigned).  When present, this
    /// method evaluates <see cref="PointerDeclarationNode.InitialValue"/> exactly as <see cref="ExecuteDeclaration"/>
    /// does for a scalar declaration, with no restriction on the expression form here: this method does not check
    /// that the initial value is an address-of expression.  That restriction, and the exact pointee-type match
    /// between the address-of expression and the declared pointee type, are enforced entirely by the type checker
    /// before execution ever reaches this method; an interpreter running on already type-checked input can rely on
    /// the initial value always being a well-formed pointer value or <c>NAUGHT</c>.
    /// </remarks>
    private void ExecutePointerDeclaration(PointerDeclarationNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue value = node.InitialValue != null
            ? this.EvaluateExpression(node.InitialValue, environment)
            : TopsyTurvyValue.Null();

        environment.Declare(node.Name, value, isConstant: node.IsConstant);
    }

    /// <summary>
    /// Executes an array element assignment statement.
    /// </summary>
    /// <param name="node">The array element assignment node.</param>
    /// <param name="environment">The environment.</param>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the index is not an integer, the variable is a YARN (immutable by-position), the variable is not an array, the array is CONSERVATIVE, or the index is out of range.</exception>
    private void ExecuteArrayElementAssignment(ArrayElementAssignmentNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue target = environment.Get(node.ArrayName);
        if (target.LiteralType == LiteralType.String)
        {
            throw new TopsyTurvyRuntimeException(
                $"'{node.ArrayName}' is a YARN: its characters cannot be reassigned individually.",
                node.Span);
        }

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
    /// Executes a write-through assignment via a pointer dereference.
    /// </summary>
    /// <param name="node">The dereference assignment node.</param>
    /// <param name="environment">The environment.</param>
    /// <exception cref="TopsyTurvyRuntimeException">
    /// Thrown when the named variable is not a pointer, the pointer is <c>NAUGHT</c> (unassigned), the target
    /// position is out of bounds, or the pointer refers to a string character, since string characters cannot be
    /// reassigned individually.
    /// </exception>
    private void ExecuteDereferenceAssignment(DereferenceAssignmentNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyPointerTarget target = this.ResolvePointerTarget(node.PointerName, node.Span, environment);
        TopsyTurvyValue value = this.EvaluateExpression(node.Value, environment);
        target.Write(value, node.Span);
    }

    /// <summary>
    /// Resolves the target a named pointer variable currently refers to.
    /// </summary>
    /// <param name="pointerName">The name of the pointer variable.</param>
    /// <param name="span">The source span used in error messages.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The target the pointer currently refers to.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">
    /// Thrown when the named variable is <c>NAUGHT</c> (an unassigned pointer) or is not a pointer at all.
    /// </exception>
    private TopsyTurvyPointerTarget ResolvePointerTarget(string pointerName, SourceSpan span, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue pointerValue = environment.Get(pointerName);
        if (pointerValue.LiteralType == LiteralType.Null)
        {
            throw new TopsyTurvyRuntimeException(
                $"Cannot dereference '{pointerName}': it is NAUGHT (an unassigned pointer).",
                span);
        }

        if (pointerValue.LiteralType != LiteralType.Pointer)
        {
            throw new TopsyTurvyRuntimeException(
                $"'{pointerName}' is not a pointer.",
                span);
        }

        return (TopsyTurvyPointerTarget)pointerValue.RawValue!;
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
        bool condition = this.EvaluateExpression(node.Condition, environment).IsTruthy();

        if (condition)
        {
            this.ExecuteStatements(node.TrueBlock, environment);
            return;
        }

        foreach (ElseIfBranch branch in node.ElseIfs)
        {
            bool branchCondition = this.EvaluateExpression(branch.Condition, environment).IsTruthy();

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
        TopsyTurvyValue switchValue = this.EvaluateExpression(node.Expression, environment);

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

        while (!this.EvaluateExpression(node.Condition!, environment).IsTruthy())
        {
            if (!this.ExecuteLoopBody(node.Body, environment))
            {
                return;
            }

            int step = node.Step is not null
                ? (int)this.EvaluateExpression(node.Step, environment).RawValue!
                : 1;

            TopsyTurvyValue current = environment.Get(loopVariable);
            environment.Assign(loopVariable, TopsyTurvyValue.Integer((int)current.RawValue! + step));
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

            int step = node.Step is not null
                ? (int)this.EvaluateExpression(node.Step, environment).RawValue!
                : 1;
            TopsyTurvyValue current = environment.Get(loopVariable);
            environment.Assign(loopVariable, TopsyTurvyValue.Integer((int)current.RawValue! - step));
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
    /// Executes a guard clause statement.
    /// </summary>
    /// <param name="node">The guard node.</param>
    /// <param name="environment">The environment.</param>
    /// <remarks>
    /// Evaluates the condition: if falsy it executes the else block, otherwise falls through with no effect.
    /// </remarks>
    private void ExecuteGuard(GuardNode node, TopsyTurvyEnvironment environment)
    {
        if (!this.EvaluateExpression(node.Condition, environment).IsTruthy())
        {
            this.ExecuteStatements(node.ElseBlock, environment);
        }
    }

    /// <summary>
    /// Executes an assert statement.
    /// </summary>
    /// <param name="node">The assert node.</param>
    /// <param name="environment">The environment.</param>
    /// <remarks>
    /// Evaluates the condition: if falsy it evaluates and throws the error message otherwise no effect occurs.
    /// </remarks>
    private void ExecuteAssert(AssertNode node, TopsyTurvyEnvironment environment)
    {
        if (!this.EvaluateExpression(node.Condition, environment).IsTruthy())
        {
            throw new TopsyTurvyThrowException(this.EvaluateExpression(node.ErrorMessage, environment));
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
            this.EvaluateExpression(node.Operation, environment);
            this.ExecuteStatements(node.SuccessBlock, environment);
        }
        catch (TopsyTurvyThrowException ex)
        {
            TopsyTurvyEnvironment catchEnvironment = environment.CreateNested();
            catchEnvironment.Declare(node.CaughtValueName, ex.ThrowValue);
            this.ExecuteStatements(node.ExceptionBlock, catchEnvironment);
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

        string? importedNamespace = FindNamespaceDeclaration(imported.Statements);
        foreach (Statement statement in imported.Statements)
        {
            if (statement is FunctionDefinitionNode functionDefinition)
            {
                this.functions[QualifyName(importedNamespace, functionDefinition.Name)] = functionDefinition;
                this.functionNamespaces[functionDefinition] = importedNamespace;
            }
        }
    }

    /// <summary>
    /// Finds the at-most-one namespace declaration in a top-level statement list.
    /// </summary>
    /// <param name="statements">The statements to scan, typically an entire programme or imported file top-level body.</param>
    /// <returns>The dot-joined namespace path, or <c>null</c> if the file declares no namespace.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when more than one <see cref="NamespaceDeclarationNode"/> is found.</exception>
    private static string? FindNamespaceDeclaration(IReadOnlyList<Statement> statements)
    {
        NamespaceDeclarationNode? declaration = null;
        foreach (Statement statement in statements)
        {
            if (statement is NamespaceDeclarationNode namespaceDeclaration)
            {
                if (declaration is not null)
                {
                    throw new TopsyTurvyRuntimeException("A file may declare at most one TOWN.", namespaceDeclaration.Span);
                }

                declaration = namespaceDeclaration;
            }
        }

        return declaration is not null
            ? string.Join('.', declaration.Path)
            : null;
    }

    /// <summary>
    /// Prefixes a function name with its namespace path, if any.
    /// </summary>
    /// <param name="namespacePrefix">The dot-joined namespace path, or <c>null</c> for the global namespace.</param>
    /// <param name="name">The bare function name.</param>
    /// <returns><paramref name="name"/> unchanged when <paramref name="namespacePrefix"/> is <c>null</c>, otherwise <c>"{namespacePrefix}.{name}"</c>.</returns>
    private static string QualifyName(string? namespacePrefix, string name) =>
        namespacePrefix is null
            ? name
            : $"{namespacePrefix}.{name}";

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
        AddressOfExpressionNode addressOf => this.EvaluateAddressOf(addressOf, environment),
        DereferenceExpressionNode dereference => this.EvaluateDereference(dereference, environment),
        TernaryExpressionNode ternary => this.EvaluateTernary(ternary, environment),
        _ => throw new TopsyTurvyRuntimeException(
            $"Unhandled expression type: {expression.GetType().Name}",
            expression.Span)
    };

    /// <summary>
    /// Evaluates an array index expression and returns the element at the given 1-based position.
    /// </summary>
    /// <remarks>
    /// When the named variable is a <c>YARN</c> string, the expression performs 1-based character indexing and returns the
    /// character as a <c>STITCH</c>.
    /// </remarks>
    /// <param name="node">The array index node.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The element at the specified index, or the character at the given position for a <c>YARN</c>.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the index is not an integer, the variable is not an array or string, or the index is out of range.</exception>
    private TopsyTurvyValue EvaluateArrayIndex(ArrayIndexNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue target = environment.Get(node.ArrayName);
        if (target.LiteralType == LiteralType.String)
        {
            (int charIndex, string str) = this.ResolveStringCharacter(node.Index, node.ArrayName, node.Span, environment);
            return TopsyTurvyValue.Char(str[charIndex - 1]);
        }

        (int arrayIndex, List<TopsyTurvyValue> elements) = this.ResolveArrayElement(node.Index, node.ArrayName, node.Span, environment);
        return elements[arrayIndex - 1];
    }

    /// <summary>
    /// Resolves a 1-based character position within a <c>YARN</c> string, validating the index type and bounds.
    /// </summary>
    /// <param name="indexExpression">The expression producing the 1-based character index.</param>
    /// <param name="yarnName">The name of the <c>YARN</c> variable.</param>
    /// <param name="span">The source span of the operation, used in error messages.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>A tuple of the validated 1-based index and the string value.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the index is not a <c>PEER</c> or is out of range.</exception>
    private (int Index, string Value) ResolveStringCharacter(
        Expression indexExpression,
        string yarnName,
        SourceSpan span,
        TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue indexValue = this.EvaluateExpression(indexExpression, environment);
        if (indexValue.LiteralType != LiteralType.Integer)
        {
            throw new TopsyTurvyRuntimeException(
                $"YARN index must be a PEER (integer), got {indexValue.LiteralType}.",
                span);
        }

        string targetString = (string)environment.Get(yarnName).RawValue!;
        int index = (int)indexValue.RawValue!;
        if (index < 1 || index > targetString.Length)
        {
            throw new TopsyTurvyRuntimeException(
                $"YARN index {index} is out of range for '{yarnName}' (length {targetString.Length}).",
                span);
        }

        return (index, targetString);
    }

    /// <summary>
    /// Evaluates an array length expression and returns the number of elements as a <c>PEER</c>.
    /// </summary>
    /// <remarks>
    /// When the named variable is a <c>YARN</c> string, returns the number of characters in the string.
    /// </remarks>
    /// <param name="node">The array length node.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The element count (or character count for <c>YARN</c>) as an integer value.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the named variable is neither an array nor a string.</exception>
    private TopsyTurvyValue EvaluateArrayLength(ArrayLengthNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue arrayValue = environment.Get(node.ArrayName);
        if (arrayValue.LiteralType == LiteralType.String)
        {
            return TopsyTurvyValue.Integer(((string)arrayValue.RawValue!).Length);
        }

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
    /// Evaluates an address-of expression and returns a new pointer value referring to the named variable.
    /// </summary>
    /// <remarks>
    /// When the named variable is an array, the pointer refers to the first element, and when it is a <c>YARN</c>,
    /// the pointer refers to the first character; both enable subsequent pointer arithmetic.  For any other type,
    /// the pointer refers to the variable directly.
    /// </remarks>
    /// <param name="node">The address-of node.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>A pointer value referring to the named variable, array element, or string character.</returns>
    private TopsyTurvyValue EvaluateAddressOf(AddressOfExpressionNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyValue targetValue = environment.Get(node.VariableName);
        TopsyTurvyPointerTarget target = targetValue.LiteralType switch
        {
            LiteralType.Array => TopsyTurvyPointerTarget.ForArrayElement((List<TopsyTurvyValue>)targetValue.RawValue!, index: 0),
            LiteralType.String => TopsyTurvyPointerTarget.ForStringElement(environment, node.VariableName, index: 0),
            _ => TopsyTurvyPointerTarget.ForVariable(environment, node.VariableName)
        };

        return TopsyTurvyValue.Pointer(target);
    }

    /// <summary>
    /// Evaluates a pointer dereference expression and returns the value currently referred to.
    /// </summary>
    /// <param name="node">The dereference node.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The value at the pointer target.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">
    /// Thrown when the named variable is <c>NAUGHT</c> (an unassigned pointer), is not a pointer at all, or the
    /// target position is out of bounds.
    /// </exception>
    private TopsyTurvyValue EvaluateDereference(DereferenceExpressionNode node, TopsyTurvyEnvironment environment)
    {
        TopsyTurvyPointerTarget target = this.ResolvePointerTarget(node.PointerName, node.Span, environment);
        return target.Read(node.Span);
    }

    /// <summary>
    /// Evaluates a ternary expression and returns the appropriate branch value.
    /// </summary>
    /// <param name="node">The ternary expression node.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The <see cref="TernaryExpressionNode.TrueValue"/> when the condition is truthy; otherwise the <see cref="TernaryExpressionNode.FalseValue"/>.</returns>
    private TopsyTurvyValue EvaluateTernary(TernaryExpressionNode node, TopsyTurvyEnvironment environment) =>
        this.EvaluateExpression(node.Condition, environment).IsTruthy()
            ? this.EvaluateExpression(node.TrueValue, environment)
            : this.EvaluateExpression(node.FalseValue, environment);

    /// <summary>
    /// Evaluates a literal expression and returns its value.
    /// </summary>
    /// <param name="node">The literal node.</param>
    /// <returns>The value of the literal.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the literal type is unknown.</exception>
    private static TopsyTurvyValue EvaluateLiteral(LiteralNode node) => node.Type switch
    {
        LiteralType.Integer => TopsyTurvyValue.Integer((int)node.Value!),
        LiteralType.Double => TopsyTurvyValue.Double((double)node.Value!),
        LiteralType.String => TopsyTurvyValue.String((string)node.Value!),
        LiteralType.Char => TopsyTurvyValue.Char((char)node.Value!),
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
            case Operator.InversionOf:
                return ApplyUnaryBitwise(this.EvaluateExpression(node.Arguments[0], environment), value => ~value, "INVERSION OF", node.Span);
            case Operator.TranspositionUp:
                int shiftUpAmount = node.Arguments.Count > 1
                    ? (int)ToLong(this.EvaluateExpression(node.Arguments[1], environment))
                    : 1;
                return ApplyUnaryBitwise(this.EvaluateExpression(node.Arguments[0], environment), value => value << shiftUpAmount, "TRANSPOSITION UP", node.Span);
            case Operator.TranspositionDown:
                int shiftDownAmount = node.Arguments.Count > 1
                    ? (int)ToLong(this.EvaluateExpression(node.Arguments[1], environment))
                    : 1;
                return ApplyUnaryBitwise(this.EvaluateExpression(node.Arguments[0], environment), value => value >> shiftDownAmount, "TRANSPOSITION DOWN", node.Span);
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
                return leftOperand.LiteralType == LiteralType.Pointer
                    ? ApplyPointerArithmetic(leftOperand, rightOperand, offsetSign: 1, span, "SUM OF")
                    : ApplyArithmetic(leftOperand, rightOperand, (a, b) => a + b, (a, b) => a + b, span, "SUM OF");
            case Operator.Difference:
                return leftOperand.LiteralType == LiteralType.Pointer
                    ? ApplyPointerArithmetic(leftOperand, rightOperand, offsetSign: -1, span, "DIFFERENCE OF")
                    : ApplyArithmetic(leftOperand, rightOperand, (a, b) => a - b, (a, b) => a - b, span, "DIFFERENCE OF");
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
            case Operator.ChordOf:
                return ApplyBitwise(leftOperand, rightOperand, (a, b) => a & b, "CHORD OF", span);
            case Operator.HarmonyOf:
                return ApplyBitwise(leftOperand, rightOperand, (a, b) => a | b, "HARMONY OF", span);
            case Operator.DiscordOf:
                return ApplyBitwise(leftOperand, rightOperand, (a, b) => a ^ b, "DISCORD OF", span);
            default:
                throw new TopsyTurvyRuntimeException($"Unhandled binary operator: {binaryOperator}", span);
        }
    }

    /// <summary>
    /// Applies an arithmetic operation to two operands using numeric widening rules.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The widening hierarchy is outlined in <see cref="LiteralType"/>.  If either operand is <see cref="LiteralType.Double"/>,
    /// the result is <see cref="LiteralType.Double"/>; otherwise, if either operand is <see cref="LiteralType.Single"/>, the
    /// result is <see cref="LiteralType.Single"/>.  When both operands are integers, the result type is the wider of the two
    /// integer types.
    /// </para>
    /// </remarks>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="integerOperation">The operation to apply when both operands are integers (receives values as <c>long</c>).</param>
    /// <param name="floatingPointOperation">The operation to apply when either operand is floating-point.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <param name="operatorName">The Topsy Turvy keyword for the operator, used in error messages.</param>
    /// <returns>The result of the arithmetic operation in the widened result type.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the operands are not numeric.</exception>
    private static TopsyTurvyValue ApplyArithmetic(
        TopsyTurvyValue leftOperand,
        TopsyTurvyValue rightOperand,
        Func<long, long, long> integerOperation,
        Func<double, double, double> floatingPointOperation,
        SourceSpan span,
        string operatorName)
    {
        if (!IsNumeric(leftOperand) || !IsNumeric(rightOperand))
        {
            throw new TopsyTurvyRuntimeException(
                $"{operatorName} requires numeric operands, got {leftOperand.LiteralType} and {rightOperand.LiteralType}.",
                span);
        }

        if (leftOperand.LiteralType == LiteralType.Double || rightOperand.LiteralType == LiteralType.Double)
        {
            return TopsyTurvyValue.Double(floatingPointOperation(ToDouble(leftOperand), ToDouble(rightOperand)));
        }

        if (leftOperand.LiteralType == LiteralType.Single || rightOperand.LiteralType == LiteralType.Single)
        {
            return TopsyTurvyValue.Single((float)floatingPointOperation(ToDouble(leftOperand), ToDouble(rightOperand)));
        }

        LiteralType resultType = GetWidestIntegerType(leftOperand.LiteralType, rightOperand.LiteralType);
        long result = integerOperation(ToLong(leftOperand), ToLong(rightOperand));
        return CreateIntegerFromLong(resultType, result);
    }

    /// <summary>
    /// Applies pointer arithmetic, moving a pointer forward or backward through an array or YARN by a given offset.
    /// </summary>
    /// <remarks>
    /// Bounds checking is performed by <see cref="TopsyTurvyPointerTarget.WithOffset"/> at the point of the
    /// arithmetic operation itself, so an out-of-bounds move fails immediately rather than being deferred to a
    /// later dereference.  A pointer to a single variable does not support arithmetic since there is no notion of a
    /// next element; this is likewise enforced by <see cref="TopsyTurvyPointerTarget.WithOffset"/>.
    /// </remarks>
    /// <param name="pointerOperand">The pointer operand.</param>
    /// <param name="offsetOperand">The integer offset operand.</param>
    /// <param name="offsetSign">1 to move forward (<c>SUM OF</c>), or -1 to move backward (<c>DIFFERENCE OF</c>).</param>
    /// <param name="span">The source span of the operation.</param>
    /// <param name="operatorName">The Topsy Turvy keyword for the operator, used in error messages.</param>
    /// <returns>A new pointer value at the shifted target.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">
    /// Thrown when the offset operand is not an integer type, the pointer refers to a single variable, or the
    /// shifted target position is out of bounds.
    /// </exception>
    private static TopsyTurvyValue ApplyPointerArithmetic(
        TopsyTurvyValue pointerOperand,
        TopsyTurvyValue offsetOperand,
        int offsetSign,
        SourceSpan span,
        string operatorName)
    {
        if (!IsIntegerType(offsetOperand))
        {
            throw new TopsyTurvyRuntimeException(
                $"{operatorName} on a pointer requires an integer offset, got {offsetOperand.LiteralType}.",
                span);
        }

        TopsyTurvyPointerTarget target = (TopsyTurvyPointerTarget)pointerOperand.RawValue!;
        long offset = ToLong(offsetOperand) * offsetSign;
        return TopsyTurvyValue.Pointer(target.WithOffset(offset, span));
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
        if (!IsNumeric(leftOperand) || !IsNumeric(rightOperand))
        {
            throw new TopsyTurvyRuntimeException(
                $"QUOTIENT OF requires numeric operands, got {leftOperand.LiteralType} and {rightOperand.LiteralType}.",
                span);
        }

        if (leftOperand.LiteralType == LiteralType.Double || rightOperand.LiteralType == LiteralType.Double)
        {
            double divisor = ToDouble(rightOperand);
            if (divisor == 0.0)
            {
                throw new TopsyTurvyRuntimeException("Division by zero in QUOTIENT OF.", span);
            }

            return TopsyTurvyValue.Double(ToDouble(leftOperand) / divisor);
        }

        if (leftOperand.LiteralType == LiteralType.Single || rightOperand.LiteralType == LiteralType.Single)
        {
            double divisor = ToDouble(rightOperand);
            if (divisor == 0.0)
            {
                throw new TopsyTurvyRuntimeException("Division by zero in QUOTIENT OF.", span);
            }

            return TopsyTurvyValue.Single((float)(ToDouble(leftOperand) / divisor));
        }

        long divisorLong = ToLong(rightOperand);
        if (divisorLong == 0L)
        {
            throw new TopsyTurvyRuntimeException("Division by zero in QUOTIENT OF.", span);
        }

        LiteralType resultType = GetWidestIntegerType(leftOperand.LiteralType, rightOperand.LiteralType);
        return CreateIntegerFromLong(resultType, ToLong(leftOperand) / divisorLong);
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
        if (!IsIntegerType(leftOperand) || !IsIntegerType(rightOperand))
        {
            throw new TopsyTurvyRuntimeException(
                $"REMAINDER OF requires integer operands, got {leftOperand.LiteralType} and {rightOperand.LiteralType}.",
                span);
        }

        long divisor = ToLong(rightOperand);
        if (divisor == 0L)
        {
            throw new TopsyTurvyRuntimeException("Division by zero in REMAINDER OF.", span);
        }

        LiteralType resultType = GetWidestIntegerType(leftOperand.LiteralType, rightOperand.LiteralType);
        return CreateIntegerFromLong(resultType, ToLong(leftOperand) % divisor);
    }

    /// <summary>
    /// Applies a bitwise operation to two integer operands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both operands must be integer types: any non-integer type will result in a runtime exception.
    /// The result type is the wider of the two operand types.
    /// </para>
    /// </remarks>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <param name="operation">A function that applies the bitwise operation to two <see cref="long"/> values.</param>
    /// <param name="operatorName">The operator keyword name, used in error messages.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <returns>The result of the bitwise operation.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when either operand is not an integer type.</exception>
    private static TopsyTurvyValue ApplyBitwise(
        TopsyTurvyValue leftOperand,
        TopsyTurvyValue rightOperand,
        Func<long, long, long> operation,
        string operatorName,
        SourceSpan span)
    {
        if (!IsIntegerType(leftOperand) || !IsIntegerType(rightOperand))
        {
            throw new TopsyTurvyRuntimeException(
                $"{operatorName} requires integer operands, got {leftOperand.LiteralType} and {rightOperand.LiteralType}.",
                span);
        }

        LiteralType resultType = GetWidestIntegerType(leftOperand.LiteralType, rightOperand.LiteralType);
        return CreateIntegerFromLong(resultType, operation(ToLong(leftOperand), ToLong(rightOperand)));
    }

    /// <summary>
    /// Applies a unary bitwise operation to a single integer operand.
    /// </summary>
    /// <param name="operand">The operand.</param>
    /// <param name="operation">A function that applies the bitwise operation to a <see cref="long"/> value.</param>
    /// <param name="operatorName">The operator keyword name, used in error messages.</param>
    /// <param name="span">The source span of the operation.</param>
    /// <returns>The result of the bitwise operation.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the operand is not an integer type.</exception>
    private static TopsyTurvyValue ApplyUnaryBitwise(
        TopsyTurvyValue operand,
        Func<long, long> operation,
        string operatorName,
        SourceSpan span)
    {
        if (!IsIntegerType(operand))
        {
            throw new TopsyTurvyRuntimeException(
                $"{operatorName} requires an integer operand, got {operand.LiteralType}.",
                span);
        }

        return CreateIntegerFromLong(operand.LiteralType, operation(ToLong(operand)));
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
            LiteralType.Double => (double)leftOperand.RawValue! == (double)rightOperand.RawValue!,
            LiteralType.String => string.Equals((string)leftOperand.RawValue!, (string)rightOperand.RawValue!, StringComparison.Ordinal),
            LiteralType.Char => (char)leftOperand.RawValue! == (char)rightOperand.RawValue!,
            LiteralType.Boolean => (bool)leftOperand.RawValue! == (bool)rightOperand.RawValue!,
            LiteralType.Null => true,
            _ => Equals(leftOperand.RawValue, rightOperand.RawValue)
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
    /// <param name="functionName">The name of the function to call, either bare or namespace-qualified.</param>
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
        FunctionResolution resolution = this.ResolveFunction(functionName, span);
        if (resolution.ExternalFunction is BoundFunctionDescriptor externalFunction)
        {
            if (arguments.Count != externalFunction.Parameters.Count)
            {
                throw new TopsyTurvyRuntimeException(
                    $"Function '{functionName}' expects {externalFunction.Parameters.Count} argument(s), got {arguments.Count}.",
                    span);
            }

            return this.externalFunctions.Invoke(externalFunction, arguments, this.io) ?? TopsyTurvyValue.Null();
        }

        FunctionDefinitionNode function = resolution.UserFunction!;
        if (arguments.Count != function.Parameters.Count)
        {
            throw new TopsyTurvyRuntimeException(
                $"Function '{functionName}' expects {function.Parameters.Count} argument(s), got {arguments.Count}.",
                span);
        }

        TopsyTurvyEnvironment scope = TopsyTurvyEnvironment.CreateFunctionEnvironment();
        for (int i = 0; i < function.Parameters.Count; i++)
        {
            scope.Declare(function.Parameters[i].Name, arguments[i]);
        }

        string? callerNamespace = this.currentExecutionNamespace;
        this.currentExecutionNamespace = this.functionNamespaces.GetValueOrDefault(function);

        TopsyTurvyValue returnValue = TopsyTurvyValue.Null();
        try
        {
            this.ExecuteStatements(function.Body, scope);
        }
        catch (ReturnSignalException returnSignal)
        {
            returnValue = returnSignal.Value ?? TopsyTurvyValue.Null();
        }
        finally
        {
            this.currentExecutionNamespace = callerNamespace;
        }

        return returnValue;
    }

    /// <summary>
    /// Resolves a function name, bare or namespace-qualified, to a Topsy Turvy function or an external function.
    /// </summary>
    /// <param name="functionName">The name to resolve.</param>
    /// <param name="span">The source span of the call, used for diagnostics.</param>
    /// <returns>The resolved <see cref="FunctionResolution"/>.</returns>
    /// <remarks>
    /// <para>
    /// A name is resolved in four tiers, the first tier with exactly one match winning:
    /// * Tier 1: A name containing <c>.</c> was produced by a fully-qualified <c>SUMMON</c> target, since <c>.</c>
    ///   never appears in a bare Topsy Turvy identifier, and is looked up directly.
    /// * Tier 2: The namespace of the caller (<see cref="currentExecutionNamespace"/>).
    /// * Tier 3: Each namespace opened with <c>PRAY RECOGNISE</c> in <see cref="openNamespaces"/>.
    ///     * Two or more matches here is an ambiguous reference.
    /// * Tier 4: The global (non-namespaced) dictionary entry.
    /// </para>
    /// <para>
    /// At each of the four tiers above, a Topsy Turvy function is tried first; the <see cref="externalFunctions"/>
    /// catalogue is consulted only when that same tier has no Topsy Turvy function under that name. This is why a
    /// Topsy Turvy function always shadows an external one of the same name at every tier, rather than only at
    /// tier 4: the fallback happens tier by tier, not after all four tiers have already been tried against Topsy
    /// Turvy functions alone.
    /// </para>
    /// </remarks>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when no function matches or when a bare name matches more than one open namespace.</exception>
    private FunctionResolution ResolveFunction(string functionName, SourceSpan span)
    {
        if (functionName.Contains('.'))
        {
            if (this.functions.TryGetValue(functionName, out FunctionDefinitionNode? qualifiedMatch))
            {
                return new(qualifiedMatch, null);
            }

            return this.externalFunctions.Find(functionName) is BoundFunctionDescriptor qualifiedExternalMatch
                ? new(null, qualifiedExternalMatch)
                : throw new TopsyTurvyRuntimeException($"Function '{functionName}' is not defined.", span);
        }

        if (this.currentExecutionNamespace is not null)
        {
            string sameNamespaceQualified = QualifyName(this.currentExecutionNamespace, functionName);
            if (this.functions.TryGetValue(sameNamespaceQualified, out FunctionDefinitionNode? sameNamespaceMatch))
            {
                return new(sameNamespaceMatch, null);
            }

            if (this.externalFunctions.Find(sameNamespaceQualified) is BoundFunctionDescriptor sameNamespaceExternalMatch)
            {
                return new(null, sameNamespaceExternalMatch);
            }
        }

        List<string> matchedNamespaces = [];
        FunctionResolution? openNamespaceMatch = null;
        foreach (string openNamespace in this.openNamespaces)
        {
            string qualified = QualifyName(openNamespace, functionName);
            if (this.functions.TryGetValue(qualified, out FunctionDefinitionNode? candidate))
            {
                matchedNamespaces.Add(openNamespace);
                openNamespaceMatch = new(candidate, null);
            }
            else if (this.externalFunctions.Find(qualified) is BoundFunctionDescriptor externalCandidate)
            {
                matchedNamespaces.Add(openNamespace);
                openNamespaceMatch = new(null, externalCandidate);
            }
        }

        if (matchedNamespaces.Count > 1)
        {
            string namespaceList = string.Join("', '", matchedNamespaces.OrderBy(name => name, StringComparer.Ordinal));
            throw new TopsyTurvyRuntimeException(
                $"Function '{functionName}' is ambiguous between namespaces '{namespaceList}'; please use a fully-qualified name.",
                span);
        }

        if (openNamespaceMatch is not null)
        {
            return openNamespaceMatch.Value;
        }

        if (this.functions.TryGetValue(functionName, out FunctionDefinitionNode? globalMatch))
        {
            return new(globalMatch, null);
        }

        return this.externalFunctions.Find(functionName) is BoundFunctionDescriptor globalExternalMatch
            ? new(null, globalExternalMatch)
            : throw new TopsyTurvyRuntimeException($"Function '{functionName}' is not defined.", span);
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
            double doubleLiteral => value.LiteralType == LiteralType.Double && (double)value.RawValue! == doubleLiteral,
            string stringLiteral => value.LiteralType == LiteralType.String && string.Equals((string)value.RawValue!, stringLiteral, StringComparison.Ordinal),
            char charLiteral => value.LiteralType == LiteralType.Char && (char)value.RawValue! == charLiteral,
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
    /// Determines whether the given value is numeric (any integer or floating-point type).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value is numeric, otherwise <c>false</c>.</returns>
    private static bool IsNumeric(TopsyTurvyValue value) =>
        value.LiteralType is LiteralType.Long
            or LiteralType.UnsignedLong
            or LiteralType.Integer
            or LiteralType.UnsignedInteger
            or LiteralType.Short
            or LiteralType.UnsignedShort
            or LiteralType.SignedByte
            or LiteralType.Byte
            or LiteralType.Double
            or LiteralType.Single;

    /// <summary>
    /// Determines whether the given value is an integer type (any signed or unsigned integer type).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value is an integer type, otherwise <c>false</c>.</returns>
    private static bool IsIntegerType(TopsyTurvyValue value) =>
        value.LiteralType is LiteralType.Long
            or LiteralType.UnsignedLong
            or LiteralType.Integer
            or LiteralType.UnsignedInteger
            or LiteralType.Short
            or LiteralType.UnsignedShort
            or LiteralType.SignedByte
            or LiteralType.Byte;

    /// <summary>
    /// Converts a numeric value to a <see cref="double"/> for floating-point arithmetic and comparisons.
    /// </summary>
    /// <param name="value">The numeric value to convert.</param>
    /// <returns>The converted <see cref="double"/> value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the value is not numeric.</exception>
    private static double ToDouble(TopsyTurvyValue value) => value.LiteralType switch
    {
        LiteralType.Integer => (double)(int)value.RawValue!,
        LiteralType.Long => (double)(long)value.RawValue!,
        LiteralType.Short => (double)(short)value.RawValue!,
        LiteralType.SignedByte => (double)(sbyte)value.RawValue!,
        LiteralType.UnsignedInteger => (double)(uint)value.RawValue!,
        LiteralType.UnsignedLong => (double)(ulong)value.RawValue!,
        LiteralType.UnsignedShort => (double)(ushort)value.RawValue!,
        LiteralType.Byte => (double)(byte)value.RawValue!,
        LiteralType.Double => (double)value.RawValue!,
        LiteralType.Single => (double)(float)value.RawValue!,
        _ => throw new InvalidOperationException("Value is not numeric.")
    };

    /// <summary>
    /// Converts an integer type value to a <see cref="long"/> for integer arithmetic.
    /// </summary>
    /// <param name="value">The integer type value to convert.</param>
    /// <returns>The value represented as a <see cref="long"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the value is not an integer type.</exception>
    private static long ToLong(TopsyTurvyValue value) => value.LiteralType switch
    {
        LiteralType.Integer => (long)(int)value.RawValue!,
        LiteralType.Long => (long)value.RawValue!,
        LiteralType.Short => (long)(short)value.RawValue!,
        LiteralType.SignedByte => (long)(sbyte)value.RawValue!,
        LiteralType.UnsignedInteger => (long)(uint)value.RawValue!,
        LiteralType.UnsignedLong => (long)(ulong)value.RawValue!,
        LiteralType.UnsignedShort => (long)(ushort)value.RawValue!,
        LiteralType.Byte => (long)(byte)value.RawValue!,
        _ => throw new InvalidOperationException("Value is not an integer type.")
    };

    /// <summary>
    /// Returns the wider of two integer-family <see cref="LiteralType"/> values according to the arithmetic widening hierarchy.
    /// </summary>
    /// <remarks>
    /// The widening rank from widest to narrowest is outlined in <see cref="LiteralType"/>.
    /// </remarks>
    /// <param name="a">The first integer type.</param>
    /// <param name="b">The second integer type.</param>
    /// <returns>The wider of the two types.</returns>
    private static LiteralType GetWidestIntegerType(LiteralType a, LiteralType b) =>
        IntegerTypeRank(a) >= IntegerTypeRank(b) ? a : b;

    /// <summary>
    /// Returns the widening rank of an integer <see cref="LiteralType"/>, with higher values indicating a wider type.
    /// </summary>
    /// <remarks>
    /// The full ordering is outlined in <see cref="LiteralType"/>.  Non-integer types fall back to the rank of
    /// <see cref="LiteralType.Integer"/> so that <see cref="GetWidestIntegerType"/> degrades gracefully if
    /// called with a non-integer type.
    /// </remarks>
    /// <param name="type">The integer literal type.</param>
    /// <returns>The numeric rank of the type in the widening hierarchy.</returns>
    private static int IntegerTypeRank(LiteralType type) => type switch
    {
        LiteralType.SignedByte => 0,
        LiteralType.Byte => 1,
        LiteralType.Short => 2,
        LiteralType.UnsignedShort => 3,
        LiteralType.Integer => 4,
        LiteralType.UnsignedInteger => 5,
        LiteralType.Long => 6,
        LiteralType.UnsignedLong => 7,
        _ => 4
    };

    /// <summary>
    /// Creates a <see cref="TopsyTurvyValue"/> of the specified integer type from a <see cref="long"/> result.
    /// </summary>
    /// <param name="type">The target integer literal type.</param>
    /// <param name="value">The computed result as a <see cref="long"/>.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> of the specified type.</returns>
    private static TopsyTurvyValue CreateIntegerFromLong(LiteralType type, long value) => type switch
    {
        LiteralType.Long => TopsyTurvyValue.Long(value),
        LiteralType.Short => TopsyTurvyValue.Short((short)value),
        LiteralType.SignedByte => TopsyTurvyValue.SignedByte((sbyte)value),
        LiteralType.UnsignedInteger => TopsyTurvyValue.UnsignedInteger((uint)value),
        LiteralType.UnsignedLong => TopsyTurvyValue.UnsignedLong((ulong)value),
        LiteralType.UnsignedShort => TopsyTurvyValue.UnsignedShort((ushort)value),
        LiteralType.Byte => TopsyTurvyValue.Byte((byte)value),
        _ => TopsyTurvyValue.Integer((int)value)
    };

    /// <summary>
    /// Gets the default value for a declared type, used when no BEING initialiser is provided.
    /// </summary>
    /// <param name="type">The declared <see cref="LiteralType"/>.</param>
    /// <returns>The default <see cref="TopsyTurvyValue"/> for the given type.</returns>
    private static TopsyTurvyValue GetDefaultValue(LiteralType type) => type switch
    {
        LiteralType.Long => TopsyTurvyValue.Long(0L),
        LiteralType.Short => TopsyTurvyValue.Short(0),
        LiteralType.SignedByte => TopsyTurvyValue.SignedByte(0),
        LiteralType.UnsignedInteger => TopsyTurvyValue.UnsignedInteger(0u),
        LiteralType.UnsignedLong => TopsyTurvyValue.UnsignedLong(0ul),
        LiteralType.UnsignedShort => TopsyTurvyValue.UnsignedShort(0),
        LiteralType.Byte => TopsyTurvyValue.Byte(0),
        LiteralType.Double => TopsyTurvyValue.Double(0.0),
        LiteralType.Single => TopsyTurvyValue.Single(0.0f),
        LiteralType.String => TopsyTurvyValue.String(string.Empty),
        LiteralType.Char => TopsyTurvyValue.Char('\0'),
        LiteralType.Boolean => TopsyTurvyValue.Boolean(false),
        _ => TopsyTurvyValue.Integer(0)
    };
}
