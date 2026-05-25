# DEVELOPMENT.md
## Topsy Turvy Language — Technical Reference for Coding Agents

* **Last Updated:** 2026-05-24
* **Current Specification Version:** 0.2.0
* **Interpreter Status:** Under Development (Phase 3 Complete, Phase 4 In Progress)
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

#### Part B2 — The Interpreter (Completed)

**Files created/modified:**

| File | Action |
|---|---|
| `BWHazel.TopsyTurvy.Ast/ExpressionStatement.cs` | Created (moved from Parser project) |
| `BWHazel.TopsyTurvy.Parser/ExpressionStatement.cs` | Deleted |
| `BWHazel.TopsyTurvy.Runtime/Interpreter.cs` | Created |
| `BWHazel.TopsyTurvy.Runtime/BWHazel.TopsyTurvy.Runtime.csproj` | Added `ProjectReference` to Parser |
| `BWHazel.TopsyTurvy.Tests/TopsyTurvyInterpreterTests.cs` | Created |
| `BWHazel.TopsyTurvy.Tests/BWHazel.TopsyTurvy.Tests.csproj` | Added `ProjectReference` to Runtime |
| `BWHazel.TopsyTurvy.Cli/Program.cs` | Rewritten as minimal file runner |

**`Interpreter.cs` design:**
- Fields use `this.` prefix: `private readonly ITopsyTurvyIO io;` and `private readonly Dictionary<string, FunctionDefinitionNode> functions = [];`
- Statement handler methods use `environment` as parameter name; expression evaluators use `env`
- Full coverage: all loop types, conditionals, switch with fall-through, try-catch, recursive functions, imports

**Tests:** 9 tests total — HelloWorld, WovenOf, Arithmetic, InlineConditional, WhilstLoop, AscendingLoop, RecursiveFunction (Fibonacci fib(6)=13), Switch, CaughtException — plus `TestIO` stub.

**Build result:** 0 errors, 11 warnings (pre-existing Superpower nullability warnings). **Test result:** 13/13 passing.

#### Part C — Parser Bug Fixes (Completed)

Five parser bugs resolved (three documented + two discovered during interpreter testing):

| # | Location | Root Cause | Fix |
|---|---|---|---|
| 1 | `StatementParser.LoopTypeParser` | `.Or()` alternatives not wrapped in `.Try()`, so whitespace consumption prevented fallback | Added `.Try()` to each alternative |
| 2 | All body `Many()` calls | `Ws(Statement).Many()` propagates partial failures from whitespace consumption | Changed to `Ws(Statement).Try().Many()` throughout |
| 3 | `ExpressionStatementParser` | Bare `IdentifierExpression` consumed first word of closing keywords | Restricted to `PrefixExpression`, `LiteralExpression`, `JustSoExpression` only |
| 4 | `ParameterList.rest` | `AND SO I FIND` partially consumed as `AND <identifier>` | Added `.Try().Many()` to `rest` parser |
| 5 | `Lexer.NullLiteral`, `Lexer.BooleanLiteral` | `Span.EqualToIgnoreCase` without `.Try()` caused partial-match failures for identifiers starting with `N` or `V` (e.g. `n` partially matched `NAUGHT`/`NAY`) | Added `.Try()` to both span comparisons in each literal |

Additional: `SO` added to `Lexer.Identifier` exclusion list to prevent `AND SO I FIND` being consumed as `AND <identifier>`.

#### Part D — Comment Preprocessor (Completed)

- Created `BWHazel.TopsyTurvy.Parser/AsidePreProcessor.cs` — strips `ASIDE: ...` single-line and `(ASIDE, AT SOME LENGTH: ... END OF ASIDE.)` block comments; preserves newlines for line-number accuracy.
- Registered in `TopsyTurvyParser.cs` before `VictorianFlourishPreProcessor`.

**Smoke test:** `dotnet run --project interpreter/BWHazel.TopsyTurvy.Cli -- examples/hello_world.topsy` produces correct output. ✓

---

## 3. Known Gaps

### `fizzbuzz.topsy` — `AS IT WERE` as expression

`output IS APPOINTED AS IT WERE i AS A YARN` cannot currently parse as an assignment because `AS IT WERE` is a statement-level parser, not an expression. The fizzbuzz example line must be split:
```
AS IT WERE i AS A YARN
output IS APPOINTED JUST SO
```
This is a Phase 4 concern.

---

## 4. Next Phase

### Phase 4: CLI & Integration

- Validate the interpreter against all four example programs in `examples/` (hello_world ✓, fizzbuzz pending, fibonacci pending, pirates_calculator pending)
- Fix `fizzbuzz.topsy` per the known gap above (or extend the parser to support cast expressions)
- Validate `PRAY ADMIT` multi-file loader (implemented in interpreter, needs integration testing)

---

---

## 5. Next Steps (Start of Next Session)

1. Read `AGENTS.md` and `DEVELOPMENT.md` before starting any work.
2. Run `dotnet build interpreter/BWHazel.TopsyTurvy.slnx` and `dotnet test` to confirm baseline.
   - Expected: 0 build errors, 0 test failures, 13/13 passing.
3. Proceed to Phase 4: run each example program through the CLI and fix any issues.
   - `dotnet run --project interpreter/BWHazel.TopsyTurvy.Cli -- examples/hello_world.topsy` ✓
   - `dotnet run --project interpreter/BWHazel.TopsyTurvy.Cli -- examples/fizzbuzz.topsy` (known gap — see §3)
   - `dotnet run --project interpreter/BWHazel.TopsyTurvy.Cli -- examples/fibonacci.topsy`
   - `dotnet run --project interpreter/BWHazel.TopsyTurvy.Cli -- examples/pirates_calculator.topsy`
