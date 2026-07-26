using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.TypeChecker;

/// <summary>
/// Two-pass AST walker that performs type-checking and populates a <see cref="SemanticModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Pass 1 collects all <see cref="FunctionDefinitionNode"/> signatures, including nested ones, before
/// any expression is type-checked.  This allows forward references where a function can be called before
/// its definition in source order.
/// </para>
/// <para>
/// Pass 2 walks every statement and expression, tracking a variable-scope stack and the current
/// function return type.  Type errors are emitted as <see cref="DiagnosticSeverity.Error"/> diagnostics.
/// Warnings, e.g. discarded return values, are <see cref="DiagnosticSeverity.Warning"/>.
/// </para>
/// </remarks>
internal sealed class TypeCheckVisitor
{
    /// <summary>
    /// Defines the order of numeric types for widening conversions.
    /// </summary>
    private static readonly LiteralType[] NumericWideningOrder =
    [
        LiteralType.Double,
        LiteralType.Single,
        LiteralType.UnsignedLong,
        LiteralType.Long,
        LiteralType.UnsignedInteger,
        LiteralType.Integer,
        LiteralType.UnsignedShort,
        LiteralType.Short,
        LiteralType.Byte,
        LiteralType.SignedByte,
    ];

    private static readonly FrozenSet<LiteralType> IntegerTypes = FrozenSet.ToFrozenSet(
        [
            LiteralType.Long,
            LiteralType.Integer,
            LiteralType.Short,
            LiteralType.SignedByte,
            LiteralType.UnsignedLong,
            LiteralType.UnsignedInteger,
            LiteralType.UnsignedShort,
            LiteralType.Byte
        ]);

    private static readonly FrozenSet<LiteralType> NumericTypes = FrozenSet.ToFrozenSet(
        [
            LiteralType.Long,
            LiteralType.Integer,
            LiteralType.Short,
            LiteralType.SignedByte,
            LiteralType.UnsignedLong,
            LiteralType.UnsignedInteger,
            LiteralType.UnsignedShort,
            LiteralType.Byte,
            LiteralType.Double,
            LiteralType.Single,
        ]);

    private readonly SemanticModel model = new();
    private readonly List<Diagnostic> diagnostics = [];

    /// <summary>
    /// The dot-joined namespace paths opened via <c>PRAY RECOGNISE</c> statements encountered so far
    /// in <see cref="CheckStatements"/>, used by <see cref="ResolveFunctionSignature"/> to resolve a
    /// bare <c>SUMMON</c> name.
    /// </summary>
    private readonly HashSet<string> openNamespaces = [];

    /// <summary>
    /// The dot-joined namespace path declared by the entry programme, or <c>null</c>
    /// if it declares none.
    /// </summary>
    /// <remarks>
    /// Set once in <see cref="Visit"/> before either pass runs.  Only the entry programme own body is
    /// ever checked by <see cref="CheckStatements"/>.  Imported files contribute signatures only, their
    /// bodies are never themselves checked so this is a constant for the whole pass rather than a
    /// per-call-frame value.
    /// </remarks>
    private string? entryNamespace;

    /// <summary>
    /// A stack of scope frames, each mapping variable and parameter names to their declared <see cref="LiteralType"/>.
    /// </summary>
    /// <remarks>
    /// A new frame is pushed when entering any block (function body, conditional branch, loop body, try/catch block)
    /// and popped when leaving it.  Walking the stack top-to-bottom replicates the language scope-chain lookup:
    /// innermost scope wins, outermost checked last.
    /// </remarks>
    private readonly Stack<Dictionary<string, LiteralType>> scopeStack = new();

    /// <summary>
    /// A parallel stack to <see cref="scopeStack"/>, tracking the declared element type of each array variable
    /// in the current scope chain.
    /// </summary>
    /// <remarks>
    /// Array variables are recorded as <see cref="LiteralType.Array"/> in <see cref="scopeStack"/>, which carries no
    /// element-type information.  This companion stack stores the element type separately so that array element
    /// assignment (<see cref="CheckArrayElementAssignment"/>) and array index expressions (<see cref="InferArrayIndex"/>)
    /// can verify type compatibility.  It is kept in sync with <see cref="scopeStack"/> by <see cref="PushScope"/>
    /// and <see cref="PopScope"/>.
    /// </remarks>
    private readonly Stack<Dictionary<string, LiteralType>> arrayElementTypeStack = new();

    /// <summary>
    /// A parallel stack to <see cref="scopeStack"/>, tracking the declared pointee type of each pointer variable
    /// in the current scope chain.
    /// </summary>
    /// <remarks>
    /// Pointer variables are recorded as <see cref="LiteralType.Pointer"/> in <see cref="scopeStack"/>, which
    /// carries no pointee-type information.  This companion stack stores the pointee type separately, following
    /// the exact same pattern as <see cref="arrayElementTypeStack"/>, so that pointer assignment
    /// (<see cref="CheckAssignment"/>), dereference (<see cref="InferDereference"/>) and dereference assignment
    /// (<see cref="CheckDereferenceAssignment"/>) can verify type compatibility.  It is kept in sync with
    /// <see cref="scopeStack"/> by <see cref="PushScope"/> and <see cref="PopScope"/>.
    /// </remarks>
    private readonly Stack<Dictionary<string, LiteralType>> pointerPointeeTypeStack = new();

    /// <summary>
    /// A stack of the declared return types of the functions currently being checked, innermost at the top.
    /// </summary>
    /// <remarks>
    /// <c>null</c> at the top indicates the current function is void (declared without <c>TO FIND</c>).  A new
    /// entry is pushed when <see cref="CheckFunctionDefinition"/> enters a function body and popped when it leaves,
    /// allowing <see cref="CheckReturn"/> to validate <c>AND SO I FIND</c> return values and to reject
    /// <c>MY DUTY IS PREMATURELY DISCHARGED.</c> inside typed functions.
    /// </remarks>
    private readonly Stack<LiteralType?> functionReturnTypeStack = new();

    /// <summary>
    /// Runs both passes over the programme and returns the type-check result.
    /// </summary>
    /// <param name="program">The programme to check.</param>
    /// <param name="sourceFileResolver">An optional delegate that resolves an import filename to its source text, <c>null</c> to leave imported functions unresolved.</param>
    /// <param name="externalFunctions">The catalogue of external functions available to <c>SUMMON</c>, defaulting to <see cref="BindingCatalogue.Default"/>.</param>
    /// <returns>The populated <see cref="TypeCheckResult"/>.</returns>
    internal TypeCheckResult Visit(ProgramNode program, Func<string, string?>? sourceFileResolver = null, BindingCatalogue? externalFunctions = null)
    {
        Dictionary<string, LiteralType> rootScope = new(StringComparer.Ordinal)
        {
            [Keywords.SpecialNames.TheProps] = LiteralType.Array,
        };

        Dictionary<string, LiteralType> rootArrayElementScope = new(StringComparer.Ordinal)
        {
            [Keywords.SpecialNames.TheProps] = LiteralType.String,
        };

        this.scopeStack.Push(rootScope);
        this.arrayElementTypeStack.Push(rootArrayElementScope);
        this.pointerPointeeTypeStack.Push(new(StringComparer.Ordinal));

        this.entryNamespace = this.FindNamespaceDeclaration(program.Statements);
        this.CollectFunctionSignatures(program.Statements, sourceFileResolver, visitedImports: [], namespacePrefix: this.entryNamespace);
        this.SeedExternalFunctionSignatures(externalFunctions ?? BindingCatalogue.Default);
        this.CheckStatements(program.Statements);

        this.scopeStack.Pop();
        this.arrayElementTypeStack.Pop();
        this.pointerPointeeTypeStack.Pop();
        return new(this.model, this.diagnostics.AsReadOnly());
    }

    /// <summary>
    /// Seeds a <see cref="FunctionSignature"/> for each external function into the <see cref="SemanticModel"/>, under
    /// the same qualified-name key <see cref="CollectFunctionSignatures"/> already uses for a Topsy Turvy function.
    /// </summary>
    /// <param name="externalFunctions">The catalogue of external functions to seed.</param>
    /// <remarks>
    /// A signature already present at a key, from a Topsy Turvy function collected earlier, is left alone: a
    /// Topsy Turvy function shadows an external one of the same name, so growing the Standard Library can never change
    /// the behaviour of an existing programme.  <see cref="ResolveFunctionSignature"/> needs no changes at all to see
    /// a seeded signature: it already resolves purely against the model, with no idea whether a given signature came
    /// from a Topsy Turvy function or an external one.
    /// </remarks>
    private void SeedExternalFunctionSignatures(BindingCatalogue externalFunctions)
    {
        foreach (BoundFunctionDescriptor functionDescriptor in externalFunctions.Functions)
        {
            string functionKey = QualifyName(functionDescriptor.Namespace, functionDescriptor.Name);
            if (this.model.GetFunctionSignature(functionKey) is not null)
            {
                continue;
            }

            FunctionSignature signature = new(
                [.. functionDescriptor.Parameters.Select(static parameter => parameter.Type)],
                functionDescriptor.ReturnType);

            this.model.SetFunctionSignature(functionKey, signature);
        }
    }

    /// <summary>
    /// Finds the at-most-one namespace declaration in a top-level statement list, emitting an error
    /// diagnostic for a second or subsequent one.
    /// </summary>
    /// <param name="statements">The statements to scan, typically an entire programme or imported file top-level body.</param>
    /// <returns>The dot-joined namespace path, or <c>null</c> if the file declares no namespace.</returns>
    private string? FindNamespaceDeclaration(IReadOnlyList<Statement> statements)
    {
        NamespaceDeclarationNode? declaration = null;
        foreach (Statement statement in statements)
        {
            if (statement is NamespaceDeclarationNode namespaceDeclaration)
            {
                if (declaration is not null)
                {
                    this.Error("A file may declare at most one TOWN.", namespaceDeclaration.Span);
                    continue;
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
    /// Walks the AST to collect all function signatures, including nested functions and those defined in
    /// imported files and stores them in the <see cref="SemanticModel"/>.
    /// </summary>
    /// <param name="statements">The list of statements to process.</param>
    /// <param name="sourceFileResolver">An optional delegate that resolves an import filename to its source text, <c>null</c> to leave imported functions unresolved.</param>
    /// <param name="visitedImports">
    /// The filenames already imported in this pass, so a circular <c>PRAY ADMIT</c> chain terminates rather
    /// than recursing indefinitely.
    /// </param>
    /// <param name="namespacePrefix">
    /// The dot-joined namespace path every function signature collected from <paramref name="statements"/>
    /// is qualified with, or <c>null</c> for the global namespace.  A nested function definition inherits
    /// the same prefix as its enclosing statement list, since a namespace declaration cannot itself be
    /// nested inside a function body.
    /// </param>
    private void CollectFunctionSignatures(IReadOnlyList<Statement> statements, Func<string, string?>? sourceFileResolver, HashSet<string> visitedImports, string? namespacePrefix)
    {
        foreach (Statement statement in statements)
        {
            if (statement is FunctionDefinitionNode function)
            {
                FunctionSignature signature = new(
                    [.. function.Parameters.Select(static parameter => parameter.Type)],
                    function.ReturnType);

                this.model.SetFunctionSignature(QualifyName(namespacePrefix, function.Name), signature);
                this.CollectFunctionSignatures(function.Body, sourceFileResolver, visitedImports, namespacePrefix);
            }
            else if (statement is ImportNode importNode && sourceFileResolver is not null && visitedImports.Add(importNode.FilePath))
            {
                this.CollectImportedFunctionSignatures(importNode.FilePath, sourceFileResolver, visitedImports);
            }
        }
    }

    /// <summary>
    /// Resolves and parses a <c>PRAY ADMIT</c> import and collects its function signatures.
    /// </summary>
    /// <remarks>
    /// An unresolvable or unparsable import is silently skipped here, since it will surface as
    /// a proper runtime diagnostic at execution time.  The first pass makes already-valid
    /// imported functions known.  The imported file own namespace, if it declares one via
    /// <c>TOWN</c>, is discovered independently of the importing file namespace and used to
    /// qualify only the functions collected from this call.
    /// </remarks>
    /// <param name="filePath">The imported file's path, as written in the <c>PRAY ADMIT</c> statement.</param>
    /// <param name="sourceFileResolver">Resolves an import's filename to its source text.</param>
    /// <param name="visitedImports">The filenames already imported in this pass.</param>
    private void CollectImportedFunctionSignatures(string filePath, Func<string, string?> sourceFileResolver, HashSet<string> visitedImports)
    {
        string? importedSource = sourceFileResolver(filePath);
        if (importedSource is null)
        {
            return;
        }

        ParseResult parseResult = new TopsyTurvyParser().TryParse(importedSource);
        if (parseResult.Success && parseResult.Program is not null)
        {
            string? importedNamespace = this.FindNamespaceDeclaration(parseResult.Program.Statements);
            this.CollectFunctionSignatures(parseResult.Program.Statements, sourceFileResolver, visitedImports, importedNamespace);
        }
    }

    /// <summary>
    /// Walks the AST to check types of statements and expressions, emitting diagnostics for any type errors or warnings.
    /// </summary>
    /// <param name="statements">The list of statements to process.</param>
    private void CheckStatements(IReadOnlyList<Statement> statements)
    {
        foreach (Statement statement in statements)
        {
            this.CheckStatement(statement);
        }
    }

    /// <summary>
    /// Checks a single statement for type correctness.
    /// </summary>
    /// <param name="statement">The statement to check.</param>
    private void CheckStatement(Statement statement)
    {
        switch (statement)
        {
            case PrincipalBlockNode principalBlock:
                foreach (Statement declaration in principalBlock.Declarations)
                {
                    this.CheckStatement(declaration);
                }

                break;
            case DeclarationNode declaration:
                this.CheckDeclaration(declaration);
                break;
            case ArrayDeclarationNode arrayDeclaration:
                this.CheckArrayDeclaration(arrayDeclaration);
                break;
            case PointerDeclarationNode pointerDeclaration:
                this.CheckPointerDeclaration(pointerDeclaration);
                break;
            case AssignmentNode assignment:
                this.CheckAssignment(assignment);
                break;
            case ArrayElementAssignmentNode arrayAssignment:
                this.CheckArrayElementAssignment(arrayAssignment);
                break;
            case DereferenceAssignmentNode dereferenceAssignment:
                this.CheckDereferenceAssignment(dereferenceAssignment);
                break;
            case InputNode input:
                this.CheckInput(input);
                break;
            case PrintNode print:
                this.EvaluateExpression(print.Expression);
                break;
            case ThrowNode throwNode:
                this.CheckThrow(throwNode);
                break;
            case ReturnNode returnNode:
                this.CheckReturn(returnNode);
                break;
            case ConditionalNode conditional:
                this.CheckConditional(conditional);
                break;
            case SwitchNode switchNode:
                this.CheckSwitch(switchNode);
                break;
            case LoopNode loop:
                this.CheckLoop(loop);
                break;
            case GuardNode guard:
                this.CheckGuard(guard);
                break;
            case AssertNode assertNode:
                this.CheckAssert(assertNode);
                break;
            case TryCatchNode tryCatch:
                this.CheckTryCatch(tryCatch);
                break;
            case FunctionDefinitionNode function:
                this.CheckFunctionDefinition(function);
                break;
            case RecogniseNode recognise:
                this.openNamespaces.Add(string.Join('.', recognise.Path));
                break;
            case ExpressionStatement expressionStatement:
                LiteralType? expressionType = this.EvaluateExpression(expressionStatement.Expression);
                if (expressionType is not null)
                {
                    this.Warn(
                        "Return value discarded: the result of this expression is not assigned.",
                        expressionStatement.Span);
                }

                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Checks a variable declaration for type correctness.
    /// </summary>
    /// <param name="node">The variable declaration node to check.</param>
    private void CheckDeclaration(DeclarationNode node)
    {
        if (node.Type == LiteralType.Null)
        {
            this.Error("AS A NAUGHT is not a valid type annotation.  A concrete type must be specified.", node.Span);
            return;
        }

        this.DeclareSymbol(node.Name, node.Type);
        if (node.InitialValue is not null)
        {
            LiteralType? valueType = this.EvaluateExpression(node.InitialValue, out bool isVoid);
            if (isVoid)
            {
                this.Error(
                    $"Cannot assign void to variable '{node.Name}' declared as {TypeName(node.Type)}.",
                    node.Span);
            }
            else if (valueType is not null && !this.IsAssignableFrom(node.Type, valueType.Value))
            {
                this.Error(
                    $"Cannot assign {TypeName(valueType.Value)} to variable '{node.Name}' declared as {TypeName(node.Type)}.",
                    node.Span);
            }
        }
    }

    /// <summary>
    /// Checks an array declaration for type correctness.
    /// </summary>
    /// <param name="node">The array declaration node to check.</param>
    private void CheckArrayDeclaration(ArrayDeclarationNode node)
    {
        if (node.ElementType == LiteralType.Null)
        {
            this.Error("A LITTLE LIST OF NAUGHT is not a valid array type.  A concrete element type must be specified.", node.Span);
            return;
        }

        this.DeclareSymbol(node.Name, LiteralType.Array);
        this.DeclareArrayElementType(node.Name, node.ElementType);
    }

    /// <summary>
    /// Checks a pointer declaration for type correctness.
    /// </summary>
    /// <remarks>
    /// The <see cref="LiteralType.Null"/> check here rejects <c>NAUGHT</c> as the declared pointee <em>type</em>
    /// (<c>GALLERY PICTURE OF NAUGHT</c>), the same way <see cref="CheckDeclaration"/> and
    /// <see cref="CheckArrayDeclaration"/> reject it as a variable or element type: a pointee slot must name a
    /// concrete type.  This is unrelated to a pointer runtime <em>value</em> being <c>NAUGHT</c>, meaning
    /// unassigned, which remains valid and is permitted separately by <see cref="IsAssignableFrom"/>.
    /// </remarks>
    /// <param name="node">The pointer declaration node to check.</param>
    private void CheckPointerDeclaration(PointerDeclarationNode node)
    {
        if (node.PointeeType == LiteralType.Null)
        {
            this.Error(
                $"A {Keywords.TypeNames.GalleryPictureOf} {Keywords.TypeNames.Naught} is not a valid pointer type.  A concrete pointee type must be specified.",
                node.Span);
            return;
        }

        this.DeclareSymbol(node.Name, LiteralType.Pointer);
        this.DeclarePointerPointeeType(node.Name, node.PointeeType);

        if (node.InitialValue is not null)
        {
            this.CheckPointerAssignmentValue(node.PointeeType, node.InitialValue, node.Span);
        }
    }

    /// <summary>
    /// Checks the value assigned to a pointer variable, whether at declaration or by a later assignment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An address-of expression is checked for an exact pointee-type match against <paramref name="expectedPointeeType"/>
    /// via <see cref="CheckAddressOf"/>, and <c>NAUGHT</c> is always valid.  A literal value or a plain identifier
    /// referring to another pointer is a type error: pointers are formed only by
    /// <c>GALLERY PICTURE TO &lt;variable&gt;</c>, never copied, aliased, or assigned as a literal directly.
    /// </para>
    /// <para>
    /// Any other expression, most notably pointer arithmetic (<c>SUM OF</c>/<c>DIFFERENCE OF</c> re-assigned back
    /// onto the same pointer variable), is accepted as long as it evaluates to <see cref="LiteralType.Pointer"/>.
    /// Its resolved pointee type is not checked against <paramref name="expectedPointeeType"/> here since the flat
    /// <see cref="LiteralType"/> model carries no pointee-type payload for an arbitrary expression the way it does
    /// for a literal address-of; this is safe in practice because the only expression forms that legitimately
    /// produce a pointer, other than address-of itself, derive it from an existing pointer of the same declared
    /// pointee type.
    /// </para>
    /// <para>
    /// The value expression is always walked, even when the outer form itself is rejected, so that errors nested
    /// inside it, such as an undeclared identifier, are still reported.
    /// </para>
    /// </remarks>
    /// <param name="expectedPointeeType">The pointer declared pointee type.</param>
    /// <param name="valueExpression">The expression being assigned.</param>
    /// <param name="span">The source span used in error messages.</param>
    private void CheckPointerAssignmentValue(LiteralType expectedPointeeType, Expression valueExpression, SourceSpan span)
    {
        if (valueExpression is LiteralNode { Type: LiteralType.Null })
        {
            return;
        }

        if (valueExpression is AddressOfExpressionNode addressOf)
        {
            this.CheckAddressOf(addressOf, expectedPointeeType);
            return;
        }

        if (valueExpression is LiteralNode or IdentifierNode)
        {
            this.EvaluateExpression(valueExpression);
            this.Error(
                $"A pointer may only be assigned {Keywords.Pointers.GalleryPictureTo} <variable>, {Keywords.Literals.Naught}, or the result of pointer arithmetic.",
                span);
            return;
        }

        LiteralType? valueType = this.EvaluateExpression(valueExpression, out bool isVoid);
        if (isVoid)
        {
            this.Error("Cannot assign void to a pointer variable.", span);
        }
        else if (valueType is not null && valueType.Value != LiteralType.Pointer)
        {
            this.Error($"Cannot assign {TypeName(valueType.Value)} to a pointer variable.", span);
        }
    }

    /// <summary>
    /// Checks that an address-of expression resolved pointee type exactly matches an expected pointee type.
    /// </summary>
    /// <remarks>
    /// Matching is exact: unlike ordinary assignment, no numeric widening is permitted, since a pointer aliases
    /// real storage and a write through it must preserve that storage actual type.  When the target variable is
    /// an array, its declared element type stands in for the pointee type; when it is a YARN, <c>STITCH</c>
    /// stands in, since both decay to a pointer at their first element or character.
    /// </remarks>
    /// <param name="node">The address-of expression to check.</param>
    /// <param name="expectedPointeeType">The pointee type the resulting pointer must exactly match.</param>
    private void CheckAddressOf(AddressOfExpressionNode node, LiteralType expectedPointeeType)
    {
        LiteralType? targetType = this.LookupSymbol(node.VariableName);
        if (targetType is null)
        {
            this.Error($"'{node.VariableName}' is not declared.", node.Span);
            return;
        }

        LiteralType actualPointeeType;
        if (targetType.Value == LiteralType.Array)
        {
            LiteralType? elementType = this.LookupArrayElementType(node.VariableName);
            if (elementType is null)
            {
                return;
            }

            actualPointeeType = elementType.Value;
        }
        else if (targetType.Value == LiteralType.String)
        {
            actualPointeeType = LiteralType.Char;
        }
        else
        {
            actualPointeeType = targetType.Value;
        }

        if (actualPointeeType != expectedPointeeType)
        {
            this.Error(
                $"Cannot point a {Keywords.TypeNames.GalleryPictureOf} {TypeName(expectedPointeeType)} at '{node.VariableName}' ({TypeName(actualPointeeType)}).",
                node.Span);
        }
    }

    /// <summary>
    /// Checks an assignment statement for type correctness.
    /// </summary>
    /// <param name="node">The assignment node to check.</param>
    private void CheckAssignment(AssignmentNode node)
    {
        LiteralType? declaredType = this.LookupSymbol(node.Target);

        if (declaredType == LiteralType.Pointer)
        {
            LiteralType? pointeeType = this.LookupPointerPointeeType(node.Target);
            if (pointeeType is not null)
            {
                this.CheckPointerAssignmentValue(pointeeType.Value, node.Value, node.Span);
            }
            else
            {
                this.EvaluateExpression(node.Value);
            }

            return;
        }

        LiteralType? valueType = this.EvaluateExpression(node.Value, out bool isVoid);

        if (declaredType is null)
        {
            this.Error($"'{node.Target}' is not declared.", node.Span);
            return;
        }

        if (isVoid)
        {
            this.Error($"Cannot assign void to '{node.Target}' (declared as {TypeName(declaredType.Value)}).", node.Span);
            return;
        }

        if (valueType is null)
        {
            return;
        }

        if (!this.IsAssignableFrom(declaredType.Value, valueType.Value))
        {
            this.Error(
                $"Cannot assign {TypeName(valueType.Value)} to '{node.Target}' (declared as {TypeName(declaredType.Value)}).",
                node.Span);
        }
    }

    /// <summary>
    /// Checks an array element assignment statement for type correctness.
    /// </summary>
    /// <param name="node">The array element assignment node to check.</param>
    private void CheckArrayElementAssignment(ArrayElementAssignmentNode node)
    {
        LiteralType? indexType = this.EvaluateExpression(node.Index, out bool indexIsVoid);
        LiteralType? valueType = this.EvaluateExpression(node.Value, out bool valueIsVoid);

        if (indexIsVoid)
        {
            this.Error("Array index must be an integer type, got void.", node.Span);
        }
        else if (indexType is not null && !IntegerTypes.Contains(indexType.Value))
        {
            this.Error($"Array index must be an integer type, got {TypeName(indexType.Value)}.", node.Span);
        }

        LiteralType? elementType = this.LookupArrayElementType(node.ArrayName);
        if (elementType is not null && valueIsVoid)
        {
            this.Error(
                $"Cannot assign void to element of array '{node.ArrayName}' declared as {TypeName(elementType.Value)}.",
                node.Span);
        }
        else if (elementType is not null && valueType is not null && !this.IsAssignableFrom(elementType.Value, valueType.Value))
        {
            this.Error(
                $"Cannot assign {TypeName(valueType.Value)} to element of array '{node.ArrayName}' declared as {TypeName(elementType.Value)}.",
                node.Span);
        }
    }

    /// <summary>
    /// Checks a pointer write-through assignment statement for type correctness.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="CheckPointerAssignmentValue"/>, which governs re-pointing a pointer variable itself,
    /// this governs an ordinary write to the location the pointer already refers to, so it uses the same
    /// widening rules as any other assignment via <see cref="IsAssignableFrom"/>.
    /// </remarks>
    /// <param name="node">The dereference assignment node to check.</param>
    private void CheckDereferenceAssignment(DereferenceAssignmentNode node)
    {
        LiteralType? declaredType = this.LookupSymbol(node.PointerName);
        LiteralType? valueType = this.EvaluateExpression(node.Value, out bool isVoid);

        if (declaredType is null)
        {
            this.Error($"'{node.PointerName}' is not declared.", node.Span);
            return;
        }

        if (declaredType.Value != LiteralType.Pointer)
        {
            this.Error($"'{node.PointerName}' is not a pointer.", node.Span);
            return;
        }

        LiteralType? pointeeType = this.LookupPointerPointeeType(node.PointerName);
        if (pointeeType is not null && isVoid)
        {
            this.Error(
                $"Cannot assign void through pointer '{node.PointerName}' declared as {Keywords.TypeNames.GalleryPictureOf} {TypeName(pointeeType.Value)}.",
                node.Span);
        }
        else if (pointeeType is not null && valueType is not null && !this.IsAssignableFrom(pointeeType.Value, valueType.Value))
        {
            this.Error(
                $"Cannot assign {TypeName(valueType.Value)} through pointer '{node.PointerName}' declared as {Keywords.TypeNames.GalleryPictureOf} {TypeName(pointeeType.Value)}.",
                node.Span);
        }
    }

    /// <summary>
    /// Checks an input statement for type correctness.
    /// </summary>
    /// <param name="node">The input node to check.</param>
    private void CheckInput(InputNode node)
    {
        LiteralType? declaredType = this.LookupSymbol(node.Target);
        if (declaredType is null)
        {
            this.Error($"'{node.Target}' is not declared.", node.Span);
            return;
        }

        if (declaredType.Value != LiteralType.String)
        {
            this.Error(
                $"PRAY TELL can only target a YARN variable: '{node.Target}' is declared as {TypeName(declaredType.Value)}.",
                node.Span);
        }
    }

    /// <summary>
    /// Checks a throw statement for type correctness.
    /// </summary>
    /// <param name="node">The throw node to check.</param>
    private void CheckThrow(ThrowNode node)
    {
        LiteralType? valueType = this.EvaluateExpression(node.Value, out bool isVoid);
        if (isVoid)
        {
            this.Error("A HIDEOUS CURSE ON requires a YARN value, got void.", node.Span);
        }
        else if (valueType is not null && valueType.Value != LiteralType.String)
        {
            this.Error(
                $"A HIDEOUS CURSE ON requires a YARN value, got {TypeName(valueType.Value)}.",
                node.Span);
        }
    }

    /// <summary>
    /// Checks a return statement for type correctness.
    /// </summary>
    /// <param name="node">The return node to check.</param>
    private void CheckReturn(ReturnNode node)
    {
        LiteralType? currentReturnType = this.functionReturnTypeStack.Count > 0
            ? this.functionReturnTypeStack.Peek()
            : null;

        if (node.Value is null)
        {
            if (currentReturnType is not null)
            {
                this.Error(
                    $"MY DUTY IS PREMATURELY DISCHARGED cannot appear in a function declared TO FIND {TypeName(currentReturnType.Value)}.",
                    node.Span);
            }

            return;
        }

        LiteralType? valueType = this.EvaluateExpression(node.Value, out bool isVoid);
        if (this.functionReturnTypeStack.Count == 0)
        {
            this.Error("AND SO I FIND cannot appear outside a function.", node.Span);
            return;
        }

        if (currentReturnType is null)
        {
            this.Error("AND SO I FIND cannot appear in a void function (no TO FIND declared).", node.Span);
            return;
        }

        if (isVoid)
        {
            this.Error(
                $"Cannot return void from a function declared TO FIND {TypeName(currentReturnType.Value)}.",
                node.Span);
        }
        else if (valueType is not null && !this.IsAssignableFrom(currentReturnType.Value, valueType.Value))
        {
            this.Error(
                $"Cannot return {TypeName(valueType.Value)} from a function declared TO FIND {TypeName(currentReturnType.Value)}.",
                node.Span);
        }
    }

    /// <summary>
    /// Checks a conditional statement for type correctness.
    /// </summary>
    /// <param name="node">The conditional node to check.</param>
    private void CheckConditional(ConditionalNode node)
    {
        LiteralType? conditionType = this.EvaluateExpression(node.Condition, out bool conditionIsVoid);
        if (conditionIsVoid)
        {
            this.Error("SHOULD IT TRANSPIRE THAT condition must be DECREE, got void.", node.Condition.Span);
        }
        else if (conditionType is not null && conditionType.Value != LiteralType.Boolean)
        {
            this.Error(
                $"SHOULD IT TRANSPIRE THAT condition must be DECREE, got {TypeName(conditionType.Value)}.",
                node.Condition.Span);
        }

        this.PushScope();
        this.CheckStatements(node.TrueBlock);
        this.PopScope();

        foreach (ElseIfBranch elseIf in node.ElseIfs)
        {
            LiteralType? elseIfConditionType = this.EvaluateExpression(elseIf.Condition, out bool elseIfConditionIsVoid);
            if (elseIfConditionIsVoid)
            {
                this.Error("OR, IF NOT, condition must be DECREE, got void.", elseIf.Condition.Span);
            }
            else if (elseIfConditionType is not null && elseIfConditionType.Value != LiteralType.Boolean)
            {
                this.Error(
                    $"OR, IF NOT, condition must be DECREE, got {TypeName(elseIfConditionType.Value)}.",
                    elseIf.Condition.Span);
            }

            this.PushScope();
            this.CheckStatements(elseIf.Block);
            this.PopScope();
        }

        if (node.ElseBlock.Count > 0)
        {
            this.PushScope();
            this.CheckStatements(node.ElseBlock);
            this.PopScope();
        }
    }

    /// <summary>
    /// Checks a switch statement for type correctness.
    /// </summary>
    /// <param name="node">The switch node to check.</param>
    private void CheckSwitch(SwitchNode node)
    {
        LiteralType? switchType = this.EvaluateExpression(node.Expression, out bool switchIsVoid);
        if (switchIsVoid)
        {
            this.Error("IN WHICH CAPACITY? expression cannot be void.", node.Span);
        }

        foreach (SwitchCase switchCase in node.Cases)
        {
            if (switchType is not null && switchCase.Literal is not null)
            {
                LiteralType? caseType = InferLiteralValueType(switchCase.Literal);
                if (caseType is not null && !this.IsAssignableFrom(switchType.Value, caseType.Value))
                {
                    this.Error(
                        $"WHEN ACTING AS case type {TypeName(caseType.Value)} does not match IN WHICH CAPACITY? expression type {TypeName(switchType.Value)}.",
                        node.Span);
                }
            }

            this.PushScope();
            this.CheckStatements(switchCase.Block);
            this.PopScope();
        }

        if (node.DefaultBlock.Count > 0)
        {
            this.PushScope();
            this.CheckStatements(node.DefaultBlock);
            this.PopScope();
        }
    }

    /// <summary>
    /// Checks a loop statement for type correctness.
    /// </summary>
    /// <param name="node">The loop node to check.</param>
    private void CheckLoop(LoopNode node)
    {
        if (node.Condition is not null)
        {
            LiteralType? conditionType = this.EvaluateExpression(node.Condition, out bool conditionIsVoid);
            if (conditionIsVoid)
            {
                this.Error("Loop condition must be DECREE, got void.", node.Condition.Span);
            }
            else if (conditionType is not null && conditionType.Value != LiteralType.Boolean)
            {
                this.Error(
                    $"Loop condition must be DECREE, got {TypeName(conditionType.Value)}.",
                    node.Condition.Span);
            }
        }

        this.PushScope();
        if (node.LoopVariable is not null)
        {
            this.DeclareSymbol(node.LoopVariable, LiteralType.Integer);
        }

        this.CheckStatements(node.Body);
        this.PopScope();
    }

    /// <summary>
    /// Checks a guard statement for type correctness.
    /// </summary>
    /// <param name="node">The guard node to check.</param>
    private void CheckGuard(GuardNode node)
    {
        LiteralType? conditionType = this.EvaluateExpression(node.Condition, out bool conditionIsVoid);
        if (conditionIsVoid)
        {
            this.Error("YEOMAN condition must be DECREE, got void.", node.Condition.Span);
        }
        else if (conditionType is not null && conditionType.Value != LiteralType.Boolean)
        {
            this.Error(
                $"YEOMAN condition must be DECREE, got {TypeName(conditionType.Value)}.",
                node.Condition.Span);
        }
    }

    /// <summary>
    /// Checks an assert statement for type correctness.
    /// </summary>
    /// <param name="node">The assert node to check.</param>
    private void CheckAssert(AssertNode node)
    {
        LiteralType? conditionType = this.EvaluateExpression(node.Condition, out bool conditionIsVoid);
        if (conditionIsVoid)
        {
            this.Error("THE LAW IS condition must be DECREE, got void.", node.Condition.Span);
        }
        else if (conditionType is not null && conditionType.Value != LiteralType.Boolean)
        {
            this.Error(
                $"THE LAW IS condition must be DECREE, got {TypeName(conditionType.Value)}.",
                node.Condition.Span);
        }

        LiteralType? messageType = this.EvaluateExpression(node.ErrorMessage, out bool messageIsVoid);
        if (messageIsVoid)
        {
            this.Error("THE LAW IS error message must be YARN, got void.", node.ErrorMessage.Span);
        }
        else if (messageType is not null && messageType.Value != LiteralType.String)
        {
            this.Error(
                $"THE LAW IS error message must be YARN, got {TypeName(messageType.Value)}.",
                node.ErrorMessage.Span);
        }
    }

    /// <summary>
    /// Checks a try-catch statement for type correctness.
    /// </summary>
    /// <param name="node">The try-catch node to check.</param>
    private void CheckTryCatch(TryCatchNode node)
    {
        this.EvaluateExpression(node.Operation);

        this.PushScope();
        this.CheckStatements(node.SuccessBlock);
        this.PopScope();

        this.PushScope();
        this.DeclareSymbol(node.CaughtValueName, LiteralType.String);
        this.CheckStatements(node.ExceptionBlock);
        this.PopScope();
    }

    /// <summary>
    /// Checks a function definition for type correctness.
    /// </summary>
    /// <param name="node">The function definition node to check.</param>
    private void CheckFunctionDefinition(FunctionDefinitionNode node)
    {
        this.PushScope();
        this.functionReturnTypeStack.Push(node.ReturnType);

        foreach (TypedParameter parameter in node.Parameters)
        {
            this.DeclareSymbol(parameter.Name, parameter.Type);
        }

        this.CheckStatements(node.Body);
        this.functionReturnTypeStack.Pop();
        this.PopScope();
    }

    /// <summary>
    /// Evaluates an expression and returns its inferred <see cref="LiteralType"/>, or <c>null</c> if the type cannot be determined.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <returns>The inferred type of the expression, or <c>null</c> if the type cannot be determined.</returns>
    private LiteralType? EvaluateExpression(Expression expression) => this.EvaluateExpression(expression, out _);

    /// <summary>
    /// Evaluates an expression and returns its inferred <see cref="LiteralType"/>, additionally reporting whether
    /// the expression is a call to a void function.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="isVoid">Set to <c>true</c> when the expression is a <c>SUMMON</c> call to a void function.</param>
    /// <returns>The inferred type of the expression, or <c>null</c> if the type cannot be determined or the expression is void.</returns>
    /// <remarks>
    /// A void function call is the only expression shape that can itself be void: every other kind either
    /// resolves to a concrete type or fails to infer for an unrelated reason, such as an undeclared identifier or an
    /// already-reported operand error. <paramref name="isVoid"/> lets a caller distinguish "could not infer a
    /// type" from "this is genuinely void," which a bare <c>null</c> result cannot do on its own.
    /// </remarks>
    private LiteralType? EvaluateExpression(Expression expression, out bool isVoid)
    {
        isVoid = false;
        LiteralType? result;
        if (expression is PrefixExpressionNode prefix)
        {
            result = this.InferPrefixExpression(prefix, out isVoid);
        }
        else
        {
            result = expression switch
            {
                LiteralNode literal => InferLiteralNode(literal),
                IdentifierNode identifier => this.InferIdentifier(identifier),
                TernaryExpressionNode ternary => this.InferTernary(ternary),
                ExpressionCastNode cast => cast.NewType,
                ArrayIndexNode arrayIndex => this.InferArrayIndex(arrayIndex),
                AddressOfExpressionNode addressOf => this.InferAddressOf(addressOf),
                DereferenceExpressionNode dereference => this.InferDereference(dereference),
                _ => null
            };
        }

        if (result is not null)
        {
            this.model.SetExpressionType(expression, result);
        }

        return result;
    }

    /// <summary>
    /// Infers the type of a literal node.
    /// </summary>
    /// <param name="node">The literal node to infer the type of.</param>
    /// <returns>The inferred type of the literal node.</returns>
    private static LiteralType? InferLiteralNode(LiteralNode node) => node.Type;

    /// <summary>
    /// Infers the type of an identifier node.
    /// </summary>
    /// <param name="node">The identifier node to infer the type of.</param>
    /// <returns>The inferred type of the identifier node, or <c>null</c> if the type cannot be determined.</returns>
    private LiteralType? InferIdentifier(IdentifierNode node)
    {
        LiteralType? type = this.LookupSymbol(node.Name);
        if (type is null)
        {
            this.Error($"'{node.Name}' is not declared.", node.Span);
        }

        return type;
    }

    /// <summary>
    /// Infers the type of a prefix expression node based on its operator and argument types.
    /// </summary>
    /// <param name="node">The prefix expression node to infer the type of.</param>
    /// <param name="isVoid">Set to <c>true</c> when the node is a <c>SUMMON</c> call to a void function.</param>
    /// <returns>The inferred type of the prefix expression node, or <c>null</c> if the type cannot be determined.</returns>
    /// <remarks>
    /// <see cref="Operator.Summon"/> is the only operator whose own result can be void; every other operator
    /// always produces a concrete type (or fails independently), so <paramref name="isVoid"/> is only ever set by
    /// <see cref="InferSummon"/>.
    /// </remarks>
    private LiteralType? InferPrefixExpression(PrefixExpressionNode node, out bool isVoid)
    {
        if (node.Operator == Operator.Summon)
        {
            return this.InferSummon(node, out isVoid);
        }

        isVoid = false;
        return node.Operator switch
        {
            Operator.Both => this.InferBooleanOperator(node),
            Operator.Either => this.InferBooleanOperator(node),
            Operator.HardlyEver => this.InferBooleanOperator(node),
            Operator.AllOf => this.InferBooleanOperator(node),
            Operator.AnyOf => this.InferBooleanOperator(node),
            Operator.Sum => this.InferArithmeticOperator(node),
            Operator.Difference => this.InferArithmeticOperator(node),
            Operator.Product => this.InferArithmeticOperator(node),
            Operator.Quotient => this.InferArithmeticOperator(node),
            Operator.Remainder => this.InferArithmeticOperator(node),
            Operator.Larger => this.InferArithmeticOperator(node),
            Operator.Smaller => this.InferArithmeticOperator(node),
            Operator.Alike => this.InferEqualityOperator(node),
            Operator.Unlike => this.InferEqualityOperator(node),
            Operator.PreAdamite => this.InferComparisonOperator(node),
            Operator.LowerDegree => this.InferComparisonOperator(node),
            Operator.ChordOf => this.InferBitwiseOperator(node),
            Operator.HarmonyOf => this.InferBitwiseOperator(node),
            Operator.DiscordOf => this.InferBitwiseOperator(node),
            Operator.InversionOf => this.InferBitwiseOperator(node),
            Operator.TranspositionUp => this.InferTranspositionOperator(node),
            Operator.TranspositionDown => this.InferTranspositionOperator(node),
            Operator.WovenOf => LiteralType.String,
            _ => null
        };
    }

    /// <summary>
    /// Infers the type of a boolean operator prefix expression node.
    /// </summary>
    /// <param name="node">The prefix expression node to infer the type of.</param>
    /// <returns>The inferred type of the boolean operator prefix expression node, or <c>null</c> if the type cannot be determined.</returns>
    private LiteralType? InferBooleanOperator(PrefixExpressionNode node)
    {
        foreach (Expression argument in node.Arguments)
        {
            LiteralType? argumentType = this.EvaluateExpression(argument, out bool isVoid);
            if (isVoid)
            {
                this.Error($"{node.Operator} operand must be DECREE, got void.", argument.Span);
            }
            else if (argumentType is not null && argumentType.Value != LiteralType.Boolean)
            {
                this.Error(
                    $"{node.Operator} operand must be DECREE, got {TypeName(argumentType.Value)}.",
                    argument.Span);
            }
        }

        return LiteralType.Boolean;
    }

    /// <summary>
    /// Infers the type of an arithmetic operator prefix expression node.
    /// </summary>
    /// <param name="node">The prefix expression node to infer the type of.</param>
    /// <returns>The inferred type of the arithmetic operator prefix expression node, or <c>null</c> if the type cannot be determined.</returns>
    private LiteralType? InferArithmeticOperator(PrefixExpressionNode node)
    {
        bool isAdditive = node.Operator is Operator.Sum or Operator.Difference;

        LiteralType? widenedType = null;
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            Expression argument = node.Arguments[i];
            LiteralType? argumentType = this.EvaluateExpression(argument, out bool isVoid);
            if (isVoid)
            {
                this.Error($"{node.Operator} operand must be numeric, got void.", argument.Span);
                return null;
            }

            if (argumentType is null)
            {
                continue;
            }

            if (isAdditive && i == 0 && argumentType.Value == LiteralType.Pointer)
            {
                return this.InferPointerArithmetic(node);
            }

            if (!NumericTypes.Contains(argumentType.Value))
            {
                this.Error(
                    $"{node.Operator} operand must be numeric, got {TypeName(argumentType.Value)}.",
                    argument.Span);

                return null;
            }

            widenedType = widenedType is null
                ? argumentType
                : Widen(widenedType.Value, argumentType.Value);
        }

        return widenedType;
    }

    /// <summary>
    /// Infers the type of pointer arithmetic: a <c>SUM OF</c> or <c>DIFFERENCE OF</c> expression whose first
    /// operand is a pointer.
    /// </summary>
    /// <remarks>
    /// The first operand has already been evaluated as <see cref="LiteralType.Pointer"/> by the caller.  Whether
    /// the pointer actually supports arithmetic, meaning it targets an array or YARN element rather than a single
    /// variable, is a property of its runtime target rather than of its declared type, so it cannot be checked
    /// here; it is enforced instead at the point of the arithmetic operation by the runtime pointer target.
    /// </remarks>
    /// <param name="node">The prefix expression node to infer the type of.</param>
    /// <returns><see cref="LiteralType.Pointer"/>.</returns>
    private LiteralType? InferPointerArithmetic(PrefixExpressionNode node)
    {
        if (node.Arguments.Count < 2)
        {
            this.Error($"{node.Operator} on a pointer requires an offset operand.", node.Span);
            return LiteralType.Pointer;
        }

        LiteralType? offsetType = this.EvaluateExpression(node.Arguments[1], out bool offsetIsVoid);
        if (offsetIsVoid)
        {
            this.Error($"{node.Operator} on a pointer requires an integer offset, got void.", node.Arguments[1].Span);
        }
        else if (offsetType is not null && !IntegerTypes.Contains(offsetType.Value))
        {
            this.Error(
                $"{node.Operator} on a pointer requires an integer offset, got {TypeName(offsetType.Value)}.",
                node.Arguments[1].Span);
        }

        return LiteralType.Pointer;
    }

    /// <summary>
    /// Infers the type of an equality operator prefix expression node.
    /// </summary>
    /// <param name="node">The prefix expression node to infer the type of.</param>
    /// <returns>The inferred type of the equality operator prefix expression node, or <c>null</c> if the type cannot be determined.</returns>
    private LiteralType? InferEqualityOperator(PrefixExpressionNode node)
    {
        if (node.Arguments.Count < 2)
        {
            return LiteralType.Boolean;
        }

        LiteralType? leftType = this.EvaluateExpression(node.Arguments[0], out bool leftIsVoid);
        LiteralType? rightType = this.EvaluateExpression(node.Arguments[1], out bool rightIsVoid);
        if (leftIsVoid || rightIsVoid)
        {
            this.Error($"{node.Operator} cannot compare a void value.", node.Span);
        }
        else if (leftType is not null && rightType is not null)
        {
            if (!this.AreCompatible(leftType.Value, rightType.Value))
            {
                this.Error(
                    $"{node.Operator} cannot compare {TypeName(leftType.Value)} and {TypeName(rightType.Value)}.",
                    node.Span);
            }
        }

        return LiteralType.Boolean;
    }

    /// <summary>
    /// Infers the type of a comparison operator prefix expression node.
    /// </summary>
    /// <param name="node">The prefix expression node to infer the type of.</param>
    /// <returns>The inferred type of the comparison operator prefix expression node, or <c>null</c> if the type cannot be determined.</returns>
    private LiteralType? InferComparisonOperator(PrefixExpressionNode node)
    {
        foreach (Expression argument in node.Arguments)
        {
            LiteralType? argumentType = this.EvaluateExpression(argument, out bool isVoid);
            if (isVoid)
            {
                this.Error($"{node.Operator} operand must be numeric, got void.", argument.Span);
            }
            else if (argumentType is not null && !NumericTypes.Contains(argumentType.Value))
            {
                this.Error(
                    $"{node.Operator} operand must be numeric, got {TypeName(argumentType.Value)}.",
                    argument.Span);
            }
        }

        return LiteralType.Boolean;
    }

    /// <summary>
    /// Infers the type of a bitwise operator prefix expression node.
    /// </summary>
    /// <param name="node">The prefix expression node to infer the type of.</param>
    /// <returns>The inferred type of the bitwise operator prefix expression node, or <c>null</c> if the type cannot be determined.</returns>
    private LiteralType? InferBitwiseOperator(PrefixExpressionNode node)
    {
        LiteralType? widenedType = null;
        foreach (Expression argument in node.Arguments)
        {
            LiteralType? argumentType = this.EvaluateExpression(argument, out bool isVoid);
            if (isVoid)
            {
                this.Error($"{node.Operator} requires integer operands, got void.", argument.Span);
                return null;
            }

            if (argumentType is null)
            {
                continue;
            }

            if (!IntegerTypes.Contains(argumentType.Value))
            {
                this.Error(
                    $"{node.Operator} requires integer operands, got {TypeName(argumentType.Value)}.",
                    argument.Span);
                return null;
            }

            widenedType = widenedType is null
                ? argumentType
                : Widen(widenedType.Value, argumentType.Value);
        }

        return widenedType;
    }

    /// <summary>
    /// Infers the type of a <see cref="Operator.TranspositionUp"/> or <see cref="Operator.TranspositionDown"/>
    /// prefix expression node.
    /// </summary>
    /// <param name="node">The prefix expression node to infer the type of.</param>
    /// <returns>The inferred type of the shifted value (<c>Arguments[0]</c>), or <c>null</c> if it cannot be determined.</returns>
    private LiteralType? InferTranspositionOperator(PrefixExpressionNode node)
    {
        LiteralType? valueType = this.EvaluateExpression(node.Arguments[0], out bool valueIsVoid);
        if (valueIsVoid)
        {
            this.Error($"{node.Operator} requires integer operands, got void.", node.Arguments[0].Span);
            return null;
        }

        if (valueType is not null && !IntegerTypes.Contains(valueType.Value))
        {
            this.Error(
                $"{node.Operator} requires integer operands, got {TypeName(valueType.Value)}.",
                node.Arguments[0].Span);

            return null;
        }

        if (node.Arguments.Count > 1)
        {
            LiteralType? shiftType = this.EvaluateExpression(node.Arguments[1], out bool shiftIsVoid);
            if (shiftIsVoid)
            {
                this.Error($"{node.Operator} BY clause requires an integer operand, got void.", node.Arguments[1].Span);
            }
            else if (shiftType is not null && !IntegerTypes.Contains(shiftType.Value))
            {
                this.Error(
                    $"{node.Operator} BY clause requires an integer operand, got {TypeName(shiftType.Value)}.",
                    node.Arguments[1].Span);
            }
        }

        return valueType;
    }

    /// <summary>
    /// Infers the type of a summon operator prefix expression node.
    /// </summary>
    /// <param name="node">The prefix expression node to infer the type of.</param>
    /// <param name="isVoid">Set to <c>true</c> when the resolved function is void.</param>
    /// <returns>The inferred type of the summon operator prefix expression node, or <c>null</c> if the type cannot be determined or the function is void.</returns>
    private LiteralType? InferSummon(PrefixExpressionNode node, out bool isVoid)
    {
        isVoid = false;
        if (node.Arguments.Count == 0 || node.Arguments[0] is not IdentifierNode functionIdentifier)
        {
            return null;
        }

        FunctionSignature? signature = this.ResolveFunctionSignature(functionIdentifier.Name, node.Span);
        if (signature is null)
        {
            return null;
        }

        IReadOnlyList<Expression> callArguments = [.. node.Arguments.Skip(1)];
        if (callArguments.Count != signature.ParameterTypes.Count)
        {
            this.Error(
                $"Function '{functionIdentifier.Name}' expects {signature.ParameterTypes.Count} argument(s), got {callArguments.Count}.",
                node.Span);

            isVoid = signature.ReturnType is null;
            return signature.ReturnType;
        }

        for (int i = 0; i < callArguments.Count; i++)
        {
            LiteralType? argumentType = this.EvaluateExpression(callArguments[i], out bool argumentIsVoid);
            LiteralType parameterType = signature.ParameterTypes[i];

            if (argumentIsVoid)
            {
                this.Error(
                    $"Argument {i + 1} of '{functionIdentifier.Name}' expects {TypeName(parameterType)}, got void.",
                    callArguments[i].Span);
            }
            else if (argumentType is not null && !this.IsAssignableFrom(parameterType, argumentType.Value))
            {
                this.Error(
                    $"Argument {i + 1} of '{functionIdentifier.Name}' expects {TypeName(parameterType)}, got {TypeName(argumentType.Value)}.",
                    callArguments[i].Span);
            }
        }

        isVoid = signature.ReturnType is null;
        return signature.ReturnType;
    }

    /// <summary>
    /// Resolves a <c>SUMMON</c> call target, bare or namespace-qualified, to its function signature.
    /// </summary>
    /// <param name="functionName">The name to resolve.</param>
    /// <param name="span">The source span of the call, used for diagnostics.</param>
    /// <returns>The resolved <see cref="FunctionSignature"/>, or <c>null</c> if it could not be resolved.</returns>
    /// <remarks>
    /// <para>
    /// A name containing <c>.</c> was produced by a fully-qualified <c>SUMMON</c> target and is looked up
    /// directly, since <c>.</c> never appears in a bare Topsy Turvy identifier.
    /// </para>
    /// <para>
    /// A bare name is resolved in tiers, the first tier with exactly one match winning:
    /// * The entry programme namespace (<see cref="entryNamespace"/>).
    /// * Then each namespace opened with <c>PRAY RECOGNISE</c> in <see cref="openNamespaces"/>.
    ///     * Two or more matches here is an ambiguous reference.
    /// * Then finally the global (non-namespaced) signature.
    /// </para>
    /// <para>
    /// An unresolved name emits an <c>Error</c> diagnostic itself, unlike most <c>Infer*</c> methods in
    /// this class, since a missing <c>SemanticModel</c> function signature carries no other diagnostic
    /// path back to the caller.
    /// </para>
    /// </remarks>
    private FunctionSignature? ResolveFunctionSignature(string functionName, SourceSpan span)
    {
        if (functionName.Contains('.'))
        {
            FunctionSignature? qualifiedMatch = this.model.GetFunctionSignature(functionName);
            if (qualifiedMatch is null)
            {
                this.Error($"Function '{functionName}' is not defined.", span);
            }

            return qualifiedMatch;
        }

        if (this.entryNamespace is not null)
        {
            FunctionSignature? sameNamespaceMatch = this.model.GetFunctionSignature(QualifyName(this.entryNamespace, functionName));
            if (sameNamespaceMatch is not null)
            {
                return sameNamespaceMatch;
            }
        }

        List<string> matchedNamespaces = [];
        FunctionSignature? openNamespaceMatch = null;
        foreach (string openNamespace in this.openNamespaces)
        {
            FunctionSignature? functionCandidate = this.model.GetFunctionSignature(QualifyName(openNamespace, functionName));
            if (functionCandidate is not null)
            {
                matchedNamespaces.Add(openNamespace);
                openNamespaceMatch = functionCandidate;
            }
        }

        if (matchedNamespaces.Count > 1)
        {
            string namespaceList = string.Join("', '", matchedNamespaces.OrderBy(static name => name, StringComparer.Ordinal));
            this.Error(
                $"Function '{functionName}' is ambiguous between namespaces '{namespaceList}'; please use a fully-qualified name.",
                span);

            return null;
        }

        if (openNamespaceMatch is not null)
        {
            return openNamespaceMatch;
        }

        FunctionSignature? globalMatch = this.model.GetFunctionSignature(functionName);
        if (globalMatch is null)
        {
            this.Error($"Function '{functionName}' is not defined.", span);
        }

        return globalMatch;
    }

    /// <summary>
    /// Infers the type of a ternary expression node based on its condition and arm types.
    /// </summary>
    /// <param name="node">The ternary expression node to infer the type of.</param>
    /// <returns>The inferred type of the ternary expression node, or <c>null</c> if the type cannot be determined.</returns>
    private LiteralType? InferTernary(TernaryExpressionNode node)
    {
        LiteralType? conditionType = this.EvaluateExpression(node.Condition, out bool conditionIsVoid);
        if (conditionIsVoid)
        {
            this.Error("Ternary condition must be DECREE, got void.", node.Condition.Span);
        }
        else if (conditionType is not null && conditionType.Value != LiteralType.Boolean)
        {
            this.Error(
                $"Ternary condition must be DECREE, got {TypeName(conditionType.Value)}.",
                node.Condition.Span);
        }

        LiteralType? trueType = this.EvaluateExpression(node.TrueValue, out bool trueIsVoid);
        LiteralType? falseType = this.EvaluateExpression(node.FalseValue, out bool falseIsVoid);
        if (trueIsVoid || falseIsVoid)
        {
            this.Error("Ternary arm cannot be void.", node.Span);
            return null;
        }

        if (trueType is null || falseType is null)
        {
            return trueType ?? falseType;
        }

        if (!this.AreCompatible(trueType.Value, falseType.Value))
        {
            this.Error(
                $"Ternary arms have incompatible types: {TypeName(trueType.Value)} and {TypeName(falseType.Value)}.",
                node.Span);

            return null;
        }

        return Widen(trueType.Value, falseType.Value);
    }

    /// <summary>
    /// Infers the type of an array index expression node based on the index type and the array element type.
    /// </summary>
    /// <param name="node">The array index expression node to infer the type of.</param>
    /// <returns>The inferred type of the array index expression node, or <c>null</c> if the type cannot be determined.</returns>
    private LiteralType? InferArrayIndex(ArrayIndexNode node)
    {
        LiteralType? indexType = this.EvaluateExpression(node.Index, out bool indexIsVoid);
        if (indexIsVoid)
        {
            this.Error("Array index must be an integer type, got void.", node.Index.Span);
        }
        else if (indexType is not null && !IntegerTypes.Contains(indexType.Value))
        {
            this.Error($"Array index must be an integer type, got {TypeName(indexType.Value)}.", node.Index.Span);
        }

        LiteralType? targetType = this.LookupSymbol(node.ArrayName);
        if (targetType == LiteralType.String)
        {
            return LiteralType.Char;
        }

        return this.LookupArrayElementType(node.ArrayName);
    }

    /// <summary>
    /// Infers the type of an address-of expression node.
    /// </summary>
    /// <remarks>
    /// Performs only the basic check that the target variable is declared.  The exact pointee-type match against
    /// a specific declared pointer type is enforced separately by <see cref="CheckAddressOf"/> wherever this
    /// expression appears as a pointer declaration initial value or a pointer assignment value, the only two
    /// positions documented as valid for it.  Reached from any other position, this still returns
    /// <see cref="LiteralType.Pointer"/> so that an ordinary type mismatch in that position, for example passing
    /// it where a <c>PEER</c> argument is expected, is reported naturally by the surrounding check rather than by
    /// a dedicated wrong-context diagnostic.
    /// </remarks>
    /// <param name="node">The address-of expression node to infer the type of.</param>
    /// <returns><see cref="LiteralType.Pointer"/>, or <c>null</c> if the target variable is not declared.</returns>
    private LiteralType? InferAddressOf(AddressOfExpressionNode node)
    {
        if (this.LookupSymbol(node.VariableName) is null)
        {
            this.Error($"'{node.VariableName}' is not declared.", node.Span);
            return null;
        }

        return LiteralType.Pointer;
    }

    /// <summary>
    /// Infers the type of a pointer dereference expression node.
    /// </summary>
    /// <param name="node">The dereference expression node to infer the type of.</param>
    /// <returns>The declared pointee type of the pointer, or <c>null</c> if the pointer variable is not declared or is not a pointer.</returns>
    private LiteralType? InferDereference(DereferenceExpressionNode node)
    {
        LiteralType? declaredType = this.LookupSymbol(node.PointerName);
        if (declaredType is null)
        {
            this.Error($"'{node.PointerName}' is not declared.", node.Span);
            return null;
        }

        if (declaredType.Value != LiteralType.Pointer)
        {
            this.Error($"'{node.PointerName}' is not a pointer.", node.Span);
            return null;
        }

        return this.LookupPointerPointeeType(node.PointerName);
    }

    /// <summary>
    /// Pushes a new scope frame onto the scope stack.
    /// </summary>
    /// <remarks>
    /// Creates a new variable/parameter scope for the current block of code.
    /// </remarks>
    private void PushScope()
    {
        this.scopeStack.Push(new(StringComparer.Ordinal));
        this.arrayElementTypeStack.Push(new(StringComparer.Ordinal));
        this.pointerPointeeTypeStack.Push(new(StringComparer.Ordinal));
    }

    /// <summary>
    /// Pops the current scope frame from the scope stack.
    /// </summary>
    private void PopScope()
    {
        this.scopeStack.Pop();
        this.arrayElementTypeStack.Pop();
        this.pointerPointeeTypeStack.Pop();
    }

    /// <summary>
    /// Declares the element type of an array variable in the current scope frame.
    /// </summary>
    /// <param name="name">The name of the array variable.</param>
    /// <param name="elementType">The element type of the array.</param>
    private void DeclareArrayElementType(string name, LiteralType elementType)
    {
        if (this.arrayElementTypeStack.Count > 0)
        {
            this.arrayElementTypeStack.Peek()[name] = elementType;
        }
    }

    /// <summary>
    /// Looks up the declared element type of an array variable in the current scope stack.
    /// </summary>
    /// <param name="name">The name of the array variable.</param>
    /// <returns>The declared element type of the array variable, or <c>null</c> if not found.</returns>
    private LiteralType? LookupArrayElementType(string name)
    {
        foreach (Dictionary<string, LiteralType> frame in this.arrayElementTypeStack)
        {
            if (frame.TryGetValue(name, out LiteralType type))
            {
                return type;
            }
        }

        return null;
    }

    /// <summary>
    /// Declares the pointee type of a pointer variable in the current scope frame.
    /// </summary>
    /// <param name="name">The name of the pointer variable.</param>
    /// <param name="pointeeType">The pointee type of the pointer.</param>
    private void DeclarePointerPointeeType(string name, LiteralType pointeeType)
    {
        if (this.pointerPointeeTypeStack.Count > 0)
        {
            this.pointerPointeeTypeStack.Peek()[name] = pointeeType;
        }
    }

    /// <summary>
    /// Looks up the declared pointee type of a pointer variable in the current scope stack.
    /// </summary>
    /// <param name="name">The name of the pointer variable.</param>
    /// <returns>The declared pointee type of the pointer variable, or <c>null</c> if not found.</returns>
    private LiteralType? LookupPointerPointeeType(string name)
    {
        foreach (Dictionary<string, LiteralType> frame in this.pointerPointeeTypeStack)
        {
            if (frame.TryGetValue(name, out LiteralType type))
            {
                return type;
            }
        }

        return null;
    }

    /// <summary>
    /// Declares a symbol in the current scope frame.
    /// </summary>
    /// <param name="name">The name of the symbol.</param>
    /// <param name="type">The type of the symbol.</param>
    private void DeclareSymbol(string name, LiteralType type)
    {
        if (this.scopeStack.Count > 0)
        {
            this.scopeStack.Peek()[name] = type;
        }

        this.model.SetSymbolType(name, type);
    }

    /// <summary>
    /// Looks up the declared type of a symbol in the current scope stack.
    /// </summary>
    /// <param name="name">The name of the symbol.</param>
    /// <returns>The declared type of the symbol, or <c>null</c> if not found.</returns>
    private LiteralType? LookupSymbol(string name)
    {
        foreach (Dictionary<string, LiteralType> scope in this.scopeStack)
        {
            if (scope.TryGetValue(name, out LiteralType type))
            {
                return type;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether a value of a specified type can be assigned to a variable or slot declared as the specified declared type.
    /// </summary>
    /// <param name="declaredType">The declared type of the variable or slot.</param>
    /// <param name="valueType">The type of the value being assigned.</param>
    /// <remarks>
    /// NAUGHT can only be assigned to YARN variables, arrays, or pointers.  Numeric types are compatible if
    /// widening applies.  All other mismatches are type errors.
    /// </remarks>
    /// <returns><c>true</c> if the value can be assigned, otherwise, <c>false</c>.</returns>
    private bool IsAssignableFrom(LiteralType declaredType, LiteralType valueType)
    {
        if (valueType == LiteralType.Null)
        {
            return declaredType == LiteralType.String || declaredType == LiteralType.Array || declaredType == LiteralType.Pointer;
        }

        if (declaredType == valueType)
        {
            return true;
        }

        if (NumericTypes.Contains(declaredType) && NumericTypes.Contains(valueType))
        {
            return IndexOfNumericWidening(declaredType) <= IndexOfNumericWidening(valueType);
        }

        return false;
    }

    /// <summary>
    /// Determines if two types are compatible for comparison or assignment purposes.
    /// </summary>
    /// <remarks>
    /// A pointer is additionally considered compatible with <see cref="LiteralType.Null"/> so that an unassigned
    /// pointer can be checked with <c>ALIKE NAUGHT</c> or <c>UNLIKE NAUGHT</c>; there is no dedicated is-null
    /// construct in the language.
    /// </remarks>
    private bool AreCompatible(LiteralType a, LiteralType b)
    {
        if (a == b)
        {
            return true;
        }

        if ((a == LiteralType.Pointer && b == LiteralType.Null) || (a == LiteralType.Null && b == LiteralType.Pointer))
        {
            return true;
        }

        return NumericTypes.Contains(a) && NumericTypes.Contains(b);
    }

    /// <summary>
    /// Returns the wider of two numeric types, according to the defined widening order.
    /// </summary>
    /// <param name="a">The first numeric type.</param>
    /// <param name="b">The second numeric type.</param>
    /// <returns>The wider of the two numeric types.</returns>
    private static LiteralType Widen(LiteralType a, LiteralType b)
    {
        int indexA = IndexOfNumericWidening(a);
        int indexB = IndexOfNumericWidening(b);
        return indexA <= indexB
            ? a
            : b;
    }

    /// <summary>
    /// Returns the index of a numeric type in the defined widening order.
    /// </summary>
    /// <param name="type">The numeric type.</param>
    /// <returns>The index of the numeric type in the widening order.</returns>
    private static int IndexOfNumericWidening(LiteralType type)
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
    /// Infers the type of a literal value based on its runtime type.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <returns>The inferred literal type, or null if the type cannot be inferred.</returns>
    private static LiteralType? InferLiteralValueType(object? value)
    {
        return value switch
        {
            int => LiteralType.Integer,
            long => LiteralType.Long,
            short => LiteralType.Short,
            sbyte => LiteralType.SignedByte,
            uint => LiteralType.UnsignedInteger,
            ulong => LiteralType.UnsignedLong,
            ushort => LiteralType.UnsignedShort,
            byte => LiteralType.Byte,
            double => LiteralType.Double,
            float => LiteralType.Single,
            bool => LiteralType.Boolean,
            string => LiteralType.String,
            char => LiteralType.Char,
            _ => null
        };
    }

    /// <summary>
    /// Returns the Topsy Turvy keyword for a given literal type.
    /// </summary>
    /// <param name="type">The literal type.</param>
    /// <returns>The Topsy Turvy keyword for the literal type.</returns>
    private static string TypeName(LiteralType type) => type switch
    {
        LiteralType.Integer => Keywords.TypeNames.Peer,
        LiteralType.Long => Keywords.TypeNames.Chancellor,
        LiteralType.Short => Keywords.TypeNames.Pirate,
        LiteralType.SignedByte => Keywords.TypeNames.SausageRoll,
        LiteralType.UnsignedInteger => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Peer}",
        LiteralType.UnsignedLong => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Chancellor}",
        LiteralType.UnsignedShort => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Pirate}",
        LiteralType.Byte => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.SausageRoll}",
        LiteralType.Double => Keywords.TypeNames.Fathom,
        LiteralType.Single => Keywords.TypeNames.Foot,
        LiteralType.String => Keywords.TypeNames.Yarn,
        LiteralType.Char => Keywords.TypeNames.Stitch,
        LiteralType.Boolean => Keywords.TypeNames.Decree,
        LiteralType.Null => Keywords.TypeNames.Naught,
        LiteralType.Array => Keywords.TypeNames.LittleListOf,
        LiteralType.Pointer => Keywords.TypeNames.GalleryPictureOf,
        _ => type.ToString()
    };

    /// <summary>
    /// Adds an error diagnostic to the diagnostics list.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="span">The source span where the error occurred.</param>
    private void Error(string message, SourceSpan span) =>
        this.diagnostics.Add(new Diagnostic(message, DiagnosticSeverity.Error, span));

    /// <summary>
    /// Adds a warning diagnostic to the diagnostics list.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <param name="span">The source span where the warning occurred.</param>
    private void Warn(string message, SourceSpan span) =>
        this.diagnostics.Add(new Diagnostic(message, DiagnosticSeverity.Warning, span));
}
