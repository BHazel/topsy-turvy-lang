---
sidebar_position: 5
---

# Type Checker

The Type Checker analyses a parsed Topsy Turvy programme and verifies that all types are used consistently before the programme is executed.  It sits between the [Parser](./parser.md) and the [Runtime](./runtime.md) in the execution pipeline: a successful parse that produces a valid AST is a necessary precondition, and the runtime only receives the programme if the type check also succeeds.

## Implementation

The Type Checker is implemented in the `BWHazel.TopsyTurvy.TypeChecker` namespace.  Its public entry point is the `TopsyTurvyTypeChecker` class, which exposes a single `Check(ProgramNode, sourceFileResolver)` method and returns a `TypeCheckResult`.  The optional `sourceFileResolver` delegate resolves a `PRAY ADMIT` import filename to its source text so that functions declared in imported files are visible to the check.  Passing `null` leaves imported functions unresolved.

### Type Check Result

`TypeCheckResult` is a record containing two members:

* **`Model`:** A `SemanticModel` mapping of the type information derived during the check (please see below).
* **`Diagnostics`:** An `IReadOnlyList<Diagnostic>` of all issues discovered during the check.  Each `Diagnostic` carries a `DiagnosticSeverity` of either `Error` or `Warning`.

The `Success` property returns `true` only when no error severity diagnostic was produced.  Warnings do not prevent execution whereas errors do.

### Semantic Model

The `SemanticModel` stores the inferred type information for the programme so that it can be queried after the check completes.  It exposes three query methods:

|Method|Description|
|-|-|
|`GetExpressionType(Expression)`|Returns the inferred `LiteralType?` for a given AST expression node, or `null` if the type could not be determined.|
|`GetSymbolType(string)`|Returns the declared `LiteralType?` for a named variable or parameter in the current scope.|
|`GetFunctionSignature(string)`|Returns the `FunctionSignature` for a named function, or `null` if not found.|

The model uses object identity (reference equality) rather than value equality when keying on expression nodes so that structurally-identical nodes in different positions of the tree are tracked separately.

### Function Signature

The `FunctionSignature` is a record that captures the type information needed to type-check a function call.  `ReturnType` is `null` for void functions, those declared without `TO FIND`.

## Two-Pass Architecture

The type check proceeds in two sequential passes over the AST, both driven by the internal `TypeCheckVisitor` class.

### Pass 1: Function Signature Collection

The first pass scans every `FunctionDefinitionNode` in the programme, including those nested inside loops, conditionals and other functions, and records a `FunctionSignature` in the `SemanticModel` for each one.

This pass exists solely to support **forward references** where a call to a function that is declared later in the source must still resolve correctly during pass 2.  Without a dedicated first pass the order in which functions appear in the source would determine whether calls to them type-check.

When the current file declares a namespace with `TOWN`, every signature collected from it is recorded under a namespace-qualified key rather than its bare name.  The same qualification applies when collecting signatures contributed by a `PRAY ADMIT`-imported file: its own namespace, if it declares one, is used, not the importing file.

### Pass 2: Statement and Expression Type Checking

The second pass walks every node in the programme carrying three parallel scope stacks:

|Stack|Contents|Lifetime|
|-|-|-|
|Scope|Maps of variable and parameter name to the declared `LiteralType`, one frame per block.|Pushed on block entry and popped on block exit.|
|Array Element Type|Maps of array variable name to element `LiteralType`, mirrors the Scope Stack frame structure.  The literal type for an array is `Array` with no type information, which this stack stores.|Same lifetime as Scope Stack.|
|Function Return Type|The declared return type `LiteralType?` of the enclosing function or `null` for void.|Pushed on function entry and popped on exit.|

Each expression node in the AST is visited to infer its type and the result is recorded in the `SemanticModel`.  Statement nodes are checked against the inferred types of their constituent expressions.  Examples include:

* **Assignment:** The right-hand side type must be compatible with the variable declared type.
* **Function call:** Argument count and types must match the callee `FunctionSignature`.  A bare function name is resolved to a signature by trying, in order:
    * The caller namespace.
    * Each namespace opened with `PRAY RECOGNISE`.
    * Then the global, non-namespaced, table.
    * Matching more than one open namespace is itself an error, requiring a fully-qualified name to disambiguate.
* **`SHOULD IT TRANSPIRE THAT` / `WHILST` conditions:** The condition expression must be `DECREE`.
* **`AND SO I FIND`:** The expression type must match the enclosing function return type.  Using it in a void function is an error.
* **`A HIDEOUS CURSE ON`:** The payload must be `YARN`.
* **`PRAY TELL`:** The target variable must be declared as `YARN`.
* **Array Element Assignment:** The assigned value must match the array declared element type.

Type compatibility follows a **widening hierarchy** for numeric types.  Narrower integer and floating-point types are considered compatible with wider ones, e.g. `PEER` is compatible with `FATHOM`.  All other combinations are strict.

Expression casts (`AS IT WERE` ... `AS A`) are trusted at compile time: the type checker records the declared target type as the inferred type of the cast expression without verifying that the conversion is safe.  This is a deliberate design decision: cast safety is the programmer's responsibility and is enforced at runtime by the interpreter `CastTo()` method.

## Integration with the Pipeline

The `TopsyTurvyTypeChecker` is invoked by the CLI `ProgramRunner` immediately after a successful parse.  If the `TypeCheckResult` is not successful, execution stops and the diagnostics are displayed to the user and the interpreter is never called.

The Language Server, specifically the `TextDocumentSyncHandler`, runs the same type check after every successful parse and merges the resulting diagnostics with any parse diagnostics before publishing them to the editor.
