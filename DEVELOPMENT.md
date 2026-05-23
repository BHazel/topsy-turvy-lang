# DEVELOPMENT.md
## Topsy Turvy Language — Technical Reference for Coding Agents

* **Last Updated:** 2026-05-23
* **Current Specification Version:** 0.2.0
* **Interpreter Status:** Under Development (Phase 2 Complete)
* **Grammar Source of Truth:** `SPEC.md` — read this file for all grammar questions.
* **File Extension:** `.topsy`

---

## 1. File Inventory

| File | Role |
|---|---|
| `AGENTS.md` | Project description, instructions to do before starting work and any constraints and rules that must be followed. |
| `SPEC.md` | Authoritative language specification v0.2.0. Full grammar, complete keyword reference, type system, worked examples. All grammar questions are resolved by this file. |
| `DEVELOPMENT.md` | Session state and file inventory. |
| `GRAMMAR.ebnf` | Formal EBNF grammar of the language, serving as the blueprint for the parser. |
| `examples/hello_world.topsy` | Basic output and string concatenation |
| `examples/fizzbuzz.topsy` | While loop, modulo, conditionals (including inline form), boolean type, expression casting |
| `examples/fibonacci.topsy` | Recursive and iterative functions, line continuation |
| `examples/pirates_calculator.topsy` | Interactive loop, switch/case, multiple functions, division-by-zero handling |
| `interpreter/` | Root directory for the .NET 10 interpreter implementation. |

---

## 2. Last Session Summary

### Phase 1: Foundation & Design (Already Completed)
The AST is implemented as a strongly-typed hierarchy in `BWHazel.TopsyTurvy.Ast`, designed for LSP-readiness with mandatory source tracking.

**AST Implementation Details:**
*   **Core Infrastructure**: 
    *   `TopsyTurvyNode`: Base abstract class. Every node must possess a `TopsyTurvySourceSpan Span`, ensuring every AST element maps back to its exact source coordinates (`Line`, `Column`).
    *   `TopsyTurvyExpression` / `TopsyTurvyStatement`: Primary base types for all language constructs.
*   **Expression Nodes**:
    *   `LiteralNode`: Stores values of type `TopsyTurvyLiteralType` (`Integer`, `Float`, `String`, `Boolean`, `Null`).
    *   `IdentifierNode`: Represents variable or function names.
    *   `PrefixExpressionNode`: Implements the prefix notation requirements. Uses `TopsyTurvyOperator` to distinguish between Fixed-arity (Arithmetic, Boolean, Comparison) and Variadic operators (Concatenation, Function Calls, Variadic Logic).
*   **Statement Nodes**:
    *   `TopsyTurvyProgramNode`: The root node capturing the operetta's `Title`, optional `Subtitle`, and the sequence of `Statements`.
    *   `PrincipalBlockNode`: Encapsulates the `PRINCIPALS` section.
    *   `DeclarationNode`: Handles variable declarations (`PRAY WELCOME`), including type and optional initial values.
    *   `AssignmentNode`: Maps the `IS APPOINTED` operation.
    *   `TypeCastNode`: Split into `InPlaceCastNode` (mutating) and `ExpressionCastNode` (non-mutating).
    *   `PrintNode` & `InputNode`: Map `BEHOLD` (with newline toggle) and `PRAY TELL`.
    *   `ConditionalNode`: Implements the `SHOULD IT TRANSPIRE THAT` logic, supporting a true block, a list of `ElseIfBranch` records, and an optional else block.
    *   `SwitchNode`: Maps `IN WHICH CAPACITY?`, managing a list of `SwitchCase` records (Literal, Block, Break-flag) and a default block.
    *   `LoopNode`: Supports all four loop types (`Infinite`, `Ascending`, `Descending`, `Whilst`) along with optional labels and loop variables.
    *   `FunctionDefinitionNode`: Captures function signatures (`UNDER THE TERMS OF`), bodies, and return expressions.
    *   `ImportNode`: Maps the `PRAY ADMIT` directive.
    *   `TryCatchNode`: Implements the `WITH THE GREATEST RESPECT` block with success and exception (`MODIFIED RAPTURE`) handlers.
    *   `ExpressionStatement`: Allows standalone expressions to be treated as statements.
*   **I/O Abstraction**: Implemented `ITopsyTurvyIO` in `BWHazel.TopsyTurvy.Runtime` to decouple the interpreter from the console.
*   **Infrastructure**: Established project references (`Ast` $\rightarrow$ `Parser` $\rightarrow$ `Runtime` $\rightarrow$ `Cli`).

### Phase 2: The Parser (Completed)
- **Pre-processing**: `ITopsyTurvyPreProcessor`, `TopsyTurvyPreProcessorPipeline`, and `VictorianFlourishPreProcessor` remain as implemented.
- **Lexing**: `TopsyTurvyLexer` rewritten to use the correct Superpower 3.2.1 API (`Parsers.Character`, `Parsers.Span`, `Parsers.Numerics`). `Keyword()` wraps `Span.EqualToIgnoreCase` in `Try()` to ensure clean backtracking on partial keyword matches.
- **Expression Parsing**: `TopsyTurvyExpressionParser` fully fixed:
  - `OperatorToken` uses `Parse.OneOf(...)` with each keyword wrapped in `.Try()`.
  - `.Return()` replaced by `.Value()` throughout.
  - Explicit `(TopsyTurvyExpression)` casts added to `LiteralExpression` and `IdentifierExpression`.
  - `PrefixExpression` uses `Parse.Ref(() => Expression)` for recursive argument slots (breaking static-init circular dependency).
  - Static fields reordered so `OperatorToken`, `LiteralExpression`, `IdentifierExpression`, `PrefixExpression` are all declared before `Expression`.
- **Statement Parsing**: `TopsyTurvyStatementParser` fully fixed:
  - All `Token.*` replaced with correct Superpower API.
  - `Statement` field made `public static readonly`.
  - `Optional<T>` replaced with `.Try().OptionalOrDefault(null!)` throughout (reference types cannot use `Combinators.Optional<T>`).
  - `Assignment` and `InPlaceCast` wrapped in `.Try()` so greedy identifier consumption backtracks cleanly.
  - Static fields reordered in strict dependency order; `Statement` declared last.
  - `Parse.Ref(() => Statement)` used in recursive body parsers (conditional, switch, loop, function, try/catch bodies).
- **Parser Entry**: `TopsyTurvyParser` fixed to use `TryParse` and handle `Result<T>`.
- **New files**: `ExpressionStatement.cs` and `TopsyTurvyParseException.cs` extracted as separate files per the one-type-per-file rule.
- **Solution**: `BWHazel.TopsyTurvy.Tests` added to `BWHazel.TopsyTurvy.slnx`.
- **Build result**: 0 errors, 0 warnings across all 5 projects.
- **Test result**: 5/5 tests passing (`ParserTests` + `UnitTest1`).

### Type rename and XML documentation pass
All types across all four projects were renamed to drop the `TopsyTurvy` prefix (except the two interfaces, per the standing convention, and the public `TopsyTurvyParser` entry-point class). File names were updated to match. Full XML `<summary>`, `<remarks>`, `<param>`, and `<returns>` documentation was added to every type and public member across all projects.

**Rename map (type → new name):**

| Old | New | Project |
|---|---|---|
| `TopsyTurvyNode` | `Node` | Ast |
| `TopsyTurvyExpression` | `Expression` | Ast |
| `TopsyTurvyStatement` | `Statement` | Ast |
| `TopsyTurvyProgramNode` | `ProgramNode` | Ast |
| `TopsyTurvySourceLocation` | `SourceLocation` | Ast |
| `TopsyTurvySourceSpan` | `SourceSpan` | Ast |
| `TopsyTurvyDiagnostic` | `Diagnostic` | Ast |
| `TopsyTurvyDiagnosticSeverity` | `DiagnosticSeverity` | Ast |
| `TopsyTurvyDiagnosticCollection` | `DiagnosticCollection` | Ast |
| `TopsyTurvyLiteralType` | `LiteralType` | Ast |
| `TopsyTurvyOperator` | `Operator` | Ast |
| `TopsyTurvyLexer` | `Lexer` | Parser |
| `TopsyTurvyExpressionParser` | `ExpressionParser` | Parser |
| `TopsyTurvyStatementParser` | `StatementParser` | Parser |
| `TopsyTurvyParseException` | `SyntaxException` | Parser |
| `TopsyTurvyPreProcessorPipeline` | `PreProcessorPipeline` | Parser |
| `TopsyTurvyPreProcessResult` | `PreProcessResult` | Parser |
| `TopsyTurvySourceMap` | `SourceMap` | Parser |

- **Build result**: 0 errors, 0 warnings.
- **Test result**: 5/5 tests passing.

---

## Next Phases

**Phase 3: The Runtime (Next)**
- Implement the `Environment`, `JustSoRegister`, and the recursive AST evaluator.
- Implement the "Curse" mechanism (`TopsyTurvyException`) and scoping rules.

**Phase 4: CLI & Integration**
- Implement the CLI entrance using `System.CommandLine` and `Spectre.Console`.
- Implement the `PRAY ADMIT` multi-file loader.
- Validate the interpreter against the `examples/` suite.

---

## 3. Next Steps
- Add xUnit integration tests parsing the `examples/` suite files end-to-end.
- Initialise Phase 3: The Runtime (`Environment`, `JustSoRegister`, recursive AST evaluator, scoping rules).
- Wire up the `TopsyTurvyPreProcessorPipeline` source-map so parse errors report original line/column numbers through line-continuations.
- Expand the `Identifier` reserved-keyword exclusion list to cover all single-word Topsy Turvy keywords that could be mistaken for variable names.
