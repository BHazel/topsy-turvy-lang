# DEVELOPMENT.md
## Topsy Turvy Language — Technical Reference for Coding Agents

* **Last Updated:** 2026-05-24
* **Current Specification Version:** 0.2.0
* **Interpreter Status:** Under Development (Phase 3, Parts A & B1 Complete)
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
All AST types implemented and fully XML-documented.

### Phase 2: The Parser (Already Completed)
Parser fully implemented using Superpower 3.2.1. Build: 0 errors, 0 warnings. Tests: 4/4 passing.

### Type rename pass (Already Completed)
All types had the `TopsyTurvy` prefix dropped (e.g. `TopsyTurvyNode` → `Node`), except interfaces and the public `TopsyTurvyParser` entry-point class.

---

### Phase 3: The Runtime (In Progress)

#### Part A — Parser Prerequisite Fixes (Completed)

Four parser gaps blocked execution of the example programs. All are now fixed.

**New AST node types (`BWHazel.TopsyTurvy.Ast/`):**

| File | Node | Surface Syntax |
|---|---|---|
| `ReturnNode.cs` | `ReturnNode : Statement` | `AND SO I FIND <expr>` (Value non-null) or `MY DUTY IS PREMATURELY DISCHARGED.` (Value null) |
| `ThrowNode.cs` | `ThrowNode : Statement` | `A HIDEOUS CURSE ON <expr>` |
| `BreakNode.cs` | `BreakNode : Statement` | `THAT WILL DO.` (loop and switch) |
| `ContinueNode.cs` | `ContinueNode : Statement` | `ONCE MORE.` (loop only) |

**AST modifications:**
- `FunctionDefinitionNode.cs` — removed `ReturnValue` property; returns are now `ReturnNode` instances in the body list
- `SwitchCase.cs` — removed `HasBreak` property; break is now a `BreakNode` in the case body

**Parser modifications (`StatementParser.cs`):**
- Added 5 new combinators: `Return`, `EarlyDischarge`, `Curse`, `Break`, `Continue`
- `FunctionDefinition`: closer is now exclusively `MY DUTY IS DISCHARGED.`; premature discharge is a body statement
- Loop body: simplified — `BreakNode`/`ContinueNode` parse at any nesting depth (removed null-filtering)
- Switch case body: no trailing `THAT WILL DO.` clause; `BreakNode` appears naturally in the body

**Additional parser fix (`ExpressionParser.cs`):**
- Added `JustSoExpression` combinator: matches the two-word keyword `JUST SO` before the single-word `IdentifierExpression`, producing `IdentifierNode { Name = "JUST SO" }`. Without this fix, `JUST SO` in expressions would parse as two separate identifier tokens.

**Build result:** 0 errors, 0 warnings. **Test result:** 4/4 passing (no behaviour change).

---

#### Part B1 — Runtime Value and Scope Types (Completed)

All new files are in `BWHazel.TopsyTurvy.Runtime/`.

**Type naming note:** The user applied the following naming refinements after initial implementation. These are the canonical names to use going forward:

| My initial name | Canonical name |
|---|---|
| `RuntimeException` | `TopsyTurvyRuntimeException` |
| `TopsyTurvyCurseException` | `TopsyTurvyThrowException` |
| `BreakSignal` | `BreakSignalException` |
| `CreateChild()` / `CreateFunctionScope()` | `CreateNested()` / `CreateFunctionEnvironment()` |
| `TopsyTurvyValue.Raw` / `.Type` | `TopsyTurvyValue.RawValue` / `.TopsyTurvyType` |

**New files:**

| File | Class | Role |
|---|---|---|
| `TopsyTurvyValue.cs` | `TopsyTurvyValue` | Runtime value wrapper. Wraps `object? RawValue` + `LiteralType TopsyTurvyType`. Static factories: `Integer`, `Float`, `String`, `Boolean`, `Null`. `IsTruthy()`, `CastTo(LiteralType)`, `ToString()`. Booleans render as `VERITY`/`NAY`; null as `NAUGHT`. |
| `TopsyTurvyEnvironment.cs` | `TopsyTurvyEnvironment` | Variable scope with enclosing-chain lookup. `JustSo` property holds the implicit register. `CreateGlobal()`, `CreateNested()`, `CreateFunctionEnvironment()` factory methods. `Get("JUST SO")` returns `JustSo` directly. |
| `TopsyTurvyRuntimeException.cs` | `TopsyTurvyRuntimeException` | Interpreter errors (undeclared variable, type mismatch, etc.). Carries optional `SourceSpan?` for LSP diagnostics. |
| `TopsyTurvyThrowException.cs` | `TopsyTurvyThrowException` | Language-level throw (`A HIDEOUS CURSE ON`). Carries `TopsyTurvyValue` as the cursed payload. Caught by `TryCatchNode` evaluator; if uncaught, terminates the programme. |
| `ReturnSignal.cs` | `ReturnSignal` | Internal stack-unwinding signal for `ReturnNode`. Not language-visible. `TopsyTurvyValue? Value` (null = premature discharge). |
| `BreakSignal.cs` | `BreakSignalException` | Internal signal for `THAT WILL DO.` in loops and switches. Not language-visible. |
| `ContinueSignal.cs` | `ContinueSignal` | Internal signal for `ONCE MORE.` in loops. Not language-visible. |
| `ConsoleIO.cs` | `ConsoleIO` | Concrete `ITopsyTurvyIO` for real console I/O. |

**Build result:** 0 errors, 11 warnings (pre-existing Superpower nullability warnings). **Test result:** 4/4 passing.

---

## 3. Next Phases

### Phase 3 (Continued): Part B2 — The Interpreter

**New file:** `BWHazel.TopsyTurvy.Runtime/Interpreter.cs`

Public API:
```csharp
public sealed class Interpreter
{
    public Interpreter(ITopsyTurvyIO io) { … }

    /// <summary>
    /// Executes a parsed programme. Returns a DiagnosticCollection for LSP readiness.
    /// An empty collection with no errors indicates successful execution.
    /// </summary>
    public DiagnosticCollection Execute(ProgramNode program) { … }
}
```

The interpreter is a recursive AST walker. Key design points:

**Statement dispatch** — `ExecuteStatement(Statement stmt, TopsyTurvyEnvironment env)`:

| Node type | Action |
|---|---|
| `PrincipalBlockNode` | Declare all variables; use `env.Declare(name, value)` |
| `DeclarationNode` | Evaluate `InitialValue` (or `Null()`); `env.Declare` |
| `AssignmentNode` | Evaluate `Value`; `env.Assign` |
| `InPlaceCastNode` | `env.Get`; `CastTo`; `env.Assign` |
| `ExpressionCastNode` | Evaluate expression; `CastTo`; set `env.JustSo` |
| `PrintNode` | Evaluate; interpolate `{variable}` in string; `_io.WriteLine` |
| `InputNode` | `_io.ReadLine()`; `env.Assign` as YARN |
| `ExpressionStatement` | Evaluate; **set `env.JustSo`** to result |
| `ConditionalNode` | Read `env.JustSo` (two-line form) or evaluate inline condition; execute matching block in the **same** `env` (spec §17: loops and conditionals share scope) |
| `SwitchNode` | Same two-form rule; walk cases; catch `BreakSignalException` to prevent fall-through |
| `LoopNode` | Per type — see below; catch `BreakSignalException` / `ContinueSignal` |
| `FunctionDefinitionNode` | Register in `_functions` dictionary |
| `ReturnNode` | Evaluate `Value` (or `Null()`); `throw new ReturnSignal(value)` |
| `ThrowNode` | Evaluate `Value`; `throw new TopsyTurvyThrowException(value)` |
| `BreakNode` | `throw new BreakSignalException()` |
| `ContinueNode` | `throw new ContinueSignal()` |
| `TryCatchNode` | Evaluate `Operation`; on success: set `JustSo`, run `SuccessBlock`; on `TopsyTurvyThrowException`: set `JustSo` to cursed value, run `ExceptionBlock` |
| `ImportNode` | Read file; re-parse; register function definitions only |

**Loop execution** — `BY A LEGAL FICTION`:

| Type | Behaviour |
|---|---|
| `Infinite` | Execute body; repeat; catch `BreakSignalException` to exit |
| `Ascending` | `env.Assign(var, Integer(0))`; check `UNTIL` condition before each iter; execute body; increment by 1; catch `BreakSignalException`/`ContinueSignal` (continue still increments) |
| `Descending` | Check `UNTIL` condition before each iter; execute body; decrement by 1; catch signals as above |
| `Whilst` | Check condition (truthy) before each iter; catch signals as above |

**Function calls** — `PrefixExpressionNode` with `Operator.Summon`:
1. Evaluate all arguments
2. Look up function in `_functions`; throw `TopsyTurvyRuntimeException` if not found
3. Check argument count matches parameter count
4. `TopsyTurvyEnvironment scope = TopsyTurvyEnvironment.CreateFunctionEnvironment()`
5. Declare each parameter in `scope` with evaluated argument value
6. `ExecuteStatements(body, scope)`; catch `ReturnSignal` for return value
7. If no `ReturnSignal`, return value is `TopsyTurvyValue.Null()`
8. Set **calling** env's `JustSo` to the return value

**String interpolation** — helper method scans a YARN value for `{identifier}` patterns and substitutes with `env.Get(name).ToString()`. Called by `PrintNode` and `WOVEN OF` evaluation.

**JUST SO update rules:**
- Set by: `ExpressionStatement`, function call result (on calling env), `TryCatchNode` entering `WITH GRATITUDE` (operation result), `TryCatchNode` entering `MODIFIED RAPTURE` (cursed value)
- NOT set by: assignment, declaration, print, input, in-place cast, inline conditional/switch

**LSP readiness:** `Execute` catches `TopsyTurvyRuntimeException` and uncaught `TopsyTurvyThrowException`, converts them to `Diagnostic` entries (severity `Error`) using the existing `DiagnosticCollection` from the Ast project, and returns the collection. The CLI reports errors with line/column.

---

### Phase 3 (Continued): Part C — Tests

**New file:** `BWHazel.TopsyTurvy.Tests/TopsyTurvyInterpreterTests.cs`

Use a `TestIO` stub that captures output into `List<string>` and feeds a pre-configured `Queue<string>` for `ReadLine`. Priority test scenarios:

1. Hello World — basic output, `WOVEN OF` concatenation
2. Arithmetic — `SUM OF`, `PRODUCT OF`, composition
3. Conditional — two-line and inline `SHOULD IT TRANSPIRE THAT`
4. Loop — `WHILST` and `ASCENDING`
5. Function — recursive fibonacci (validates `ReturnNode`, mid-body return)
6. Switch — fall-through and `THAT WILL DO.`
7. Exception — `A HIDEOUS CURSE ON` caught by `WITH THE GREATEST RESPECT`
8. Type cast — `IS HENCEFORTH A`, `AS IT WERE`

---

### Phase 4: CLI & Integration

- Implement the CLI entry point using `System.CommandLine` and `Spectre.Console`
- Implement `PRAY ADMIT` multi-file loader
- Validate the interpreter against all four example programs in `examples/`

---

## 4. Next Steps (Start of Next Session)

1. Read `AGENTS.md` and `DEVELOPMENT.md` before starting any work.
2. Run `dotnet build interpreter/BWHazel.TopsyTurvy.slnx` and `dotnet test` to confirm baseline (expect: 0 errors, 4/4 tests).
3. Implement **Part B2**: `Interpreter.cs` in `BWHazel.TopsyTurvy.Runtime/` following the specification in section 3 above.
4. Wire `Program.cs` minimally to run `hello_world.topsy` as a smoke test.
5. Implement **Part C**: `TopsyTurvyInterpreterTests.cs`.
6. Proceed to Phase 4: CLI & Integration.
