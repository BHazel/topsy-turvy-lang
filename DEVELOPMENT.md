# DEVELOPMENT.md
## Topsy Turvy Language — Technical Reference for Coding Agents

* **Last Updated:** 2026-05-25
* **Current Specification Version:** 0.2.0
* **Interpreter Status:** Complete (Phase 4)
* **LSP & VS Code Extension:** In Progress (Phase 5)
* **Grammar Source of Truth:** `SPEC.md` — read this file for all grammar questions.
* **File Extension:** `.topsy`

---

## 1. File Inventory

| File / Directory | Role |
|---|---|
| `AGENTS.md` | Project description, standing rules, and constraints that must be read before starting work. |
| `SPEC.md` | Authoritative language specification v0.2.0. Full grammar, keyword reference, type system, worked examples. |
| `DEVELOPMENT.md` | Session state and file inventory. |
| `GRAMMAR.ebnf` | Formal EBNF grammar serving as the blueprint for the parser. |
| `examples/hello_world.topsy` | Basic output and string concatenation. |
| `examples/fizzbuzz.topsy` | While loop, modulo, conditionals, boolean type, expression casting. |
| `examples/fibonacci.topsy` | Recursive and iterative functions, line continuation. |
| `examples/pirates_calculator.topsy` | Interactive loop, switch/case, multiple functions, division-by-zero handling. |
| `interpreter/` | Root directory for the .NET 10 interpreter. |
| `interpreter/BWHazel.TopsyTurvy.slnx` | Solution file. |
| `interpreter/BWHazel.TopsyTurvy.Ast/` | AST node types. All nodes inherit from `Node` (with `SourceSpan Span`). Includes `Diagnostic`, `DiagnosticCollection`, `DiagnosticSeverity`, `SourceSpan`, `SourceLocation`. |
| `interpreter/BWHazel.TopsyTurvy.Parser/` | Superpower-based parser. Entry point: `TopsyTurvyParser`. Includes pre-processors (`CommentsPreProcessor`, `VictorianFlourishPreProcessor`), `SourceMap`/`SourceMapping` for offset tracking, and `TopsyTurvySyntaxException`. |
| `interpreter/BWHazel.TopsyTurvy.Runtime/` | Tree-walking interpreter. Entry point: `Interpreter`. Includes `TopsyTurvyValue`, `TopsyTurvyEnvironment`, `TopsyTurvyRuntimeException`, signal exceptions for break/continue/return/throw. |
| `interpreter/BWHazel.TopsyTurvy.Cli/` | Minimal CLI (`Program.cs`) that runs a `.topsy` file. |
| `interpreter/BWHazel.TopsyTurvy.Tests/` | xUnit test suite (17/17 passing). |
| `interpreter/BWHazel.TopsyTurvy.LanguageServer/` | OmniSharp-based LSP server (Phase 5, in progress). |
| `extensions/vscode/topsy-turvy/` | VS Code extension providing LSP client + syntax highlighting (Phase 5, in progress). |

---

## 2. Completed Phases Summary

### Phase 1 — AST (Complete)

All AST node types implemented and XML-documented. Key design decisions:
- All nodes inherit from `Node` which carries `required SourceSpan Span`.
- `SourceSpan(SourceLocation Start, SourceLocation End)` — 1-indexed line/column.
- `Diagnostic` / `DiagnosticCollection` / `DiagnosticSeverity` provide LSP-ready error reporting.
- Boolean type is `DECREE`; literals are `VERITY` / `NAY`. Null is `NAUGHT`.
- `JUST SO` is the sole implicit variable (accumulator).

### Phase 2 — Parser (Complete)

Superpower 3.2.1 parser. Build: 0 errors. Tests: 17/17 (includes 4 `TryParse` tests added in Phase 5).

Key files: `Lexer.cs`, `StatementParser.cs`, `ExpressionParser.cs`, `TopsyTurvyParser.cs`.

Pre-processing pipeline (`PreProcessorPipeline`) runs before parsing:
1. `CommentsPreProcessor` — strips `ASIDE:` line comments and `(ASIDE, AT SOME LENGTH: ... END OF ASIDE.)` block comments; preserves newlines for line-number accuracy.
2. `VictorianFlourishPreProcessor` — handles line-continuation syntax.

`SourceMap` / `SourceMapping` map offsets in pre-processed text back to original source positions.

### Phase 3 — Runtime (Complete)

Tree-walking interpreter in `Interpreter.cs`. Build: 0 errors. Tests: 13/13.

Key naming decisions (canonical names to use going forward):

| Type | Note |
|---|---|
| `TopsyTurvyEnvironment` | Variable scope (not `Environment`, to avoid clash with `System.Environment`). Factory methods: `CreateGlobal()`, `CreateNested()`, `CreateFunctionEnvironment()`. |
| `TopsyTurvyValue` | Runtime value wrapper. Properties: `RawValue`, `TopsyTurvyType`. |
| `TopsyTurvyRuntimeException` | Interpreter errors. Carries optional `SourceSpan?` for LSP. |
| `TopsyTurvyThrowException` | Language-level `A HIDEOUS CURSE ON` throw. Carries `TopsyTurvyValue` payload. |
| `BreakSignalException` | Internal signal for `THAT WILL DO.` |
| `ReturnSignalException` | Internal stack-unwinding signal for `ReturnNode`. |
| `ContinueSignal` | Internal signal for `ONCE MORE.` |

`ITopsyTurvyIO` / `ConsoleIO` abstract console I/O (used for testing with `TestIO` stub).

### Phase 4 — CLI & Integration (Complete)

All four example programs execute correctly:
- `examples/hello_world.topsy` ✓
- `examples/fizzbuzz.topsy` ✓
- `examples/fibonacci.topsy` ✓
- `examples/pirates_calculator.topsy` ✓

Six parser bugs were found and fixed during integration. Notable decisions:
- `SUMMON` uses `WITH` as argument separator (not `AND`): `SUMMON name WITH arg1 AND arg2 IF YOU PLEASE.` / `SUMMON name WITH NOTHING IF YOU PLEASE.`
- Float literal requires decimal point to disambiguate from integer.
- Non-variadic operators (`SUM`, `ALIKE`, etc.) accept at most one `AND <expr>`.
- `AS IT WERE` cannot appear as an expression in an assignment RHS — must be a separate statement.

---

## 3. Known Gaps

Phase 5 is in progress. See the open issues section in Phase 5 below. All four example programs execute correctly. Build: 0 errors. Tests: 17/17.

---

## 4. Phase 5: LSP + VS Code Extension (In Progress)

### Goal

Add developer tooling: a Language Server Protocol server that publishes syntax-error diagnostics to editors, and a VS Code extension that integrates with it and provides syntax highlighting for `.topsy` files.

### Part 1 — Parser Enhancement (`BWHazel.TopsyTurvy.Parser`) — Complete

**New file: `ParseResult.cs`**
- Record: `ParseResult(ProgramNode? Program, IReadOnlyList<Diagnostic> Diagnostics)`
- `bool Success => Program is not null`

**New method: `TopsyTurvyParser.TryParse(string source) : ParseResult`**
- Runs pre-processor pipeline and attempts to parse.
- On failure: reads `result.ErrorPosition.Line/Column` from Superpower (1-indexed), constructs a `Diagnostic(message, Error, SourceSpan)`, returns `ParseResult(null, [diagnostic])`.
- On success: returns `ParseResult(program, [])`.
- Note: Superpower stops at the first parse error, so `TryParse` returns at most one diagnostic.
- Existing `Parse()` method is unchanged (CLI and tests unaffected).
- 4 new unit tests added in `TopsyTurvyParserTests.cs` covering valid source, syntax error, empty source, and multi-line error position.

### Part 2 — Language Server (`BWHazel.TopsyTurvy.LanguageServer`) — Partially Working

**Project**: Added `ProjectReference` to `BWHazel.TopsyTurvy.Parser`.

**`Program.cs`**: OmniSharp LSP host on stdio. Registers `TextDocumentSyncHandler`.
- Key fix: `.ClearProviders()` must be called before `.AddLanguageProtocolLogging()`. Without it, .NET's default console logger writes to stdout and corrupts the LSP stdio stream, causing the client to error with "Header must provide a Content-Length property".

**`TextDocumentSyncHandler.cs`**: Implements `TextDocumentSyncHandlerBase` (OmniSharp abstract base).
- `DidOpen` / `DidChange` / `DidSave`: call `TryParse`, map `Diagnostic[]` to LSP diagnostics (1-indexed `SourceLocation` → 0-indexed LSP `Position`), publish via `languageServer.TextDocument.PublishDiagnostics`.
- `DidClose`: publishes empty diagnostics to clear squiggles.
- Sync kind: `Full` (resend whole document on change).
- OmniSharp API notes: use `TextDocumentSelector.ForLanguage(id)` (not `DocumentSelector`); capability parameter type is `TextSynchronizationCapability` (not `TextDocumentSyncCapability`); use `Container<Diagnostic>` for the diagnostics list.
- `BWHazel.TopsyTurvy.Ast.Diagnostic` and `DiagnosticSeverity` are aliased as `AstDiagnostic` / `AstDiagnosticSeverity` to avoid clash with the OmniSharp types of the same name.
- **Status**: server starts, connects, `DidOpen`/`DidChange` handlers are confirmed invoked (verified with a temporary `window/showMessage` notification). `TryParse` returns correct counts (0 for valid code, 1 for invalid). However, `PublishDiagnostics` notifications are not producing squiggles in VS Code — root cause not yet identified (see open issues).

### Part 3 — VS Code Extension (`extensions/vscode/topsy-turvy`) — Partially Working

**`package.json`**:
- `vscode-languageclient ^9.0.1` in `dependencies`.
- Language contribution: id `topsy-turvy`, extension `.topsy`.
- Grammar contribution: `syntaxes/topsy-turvy.tmLanguage.json`, scope `source.topsy`.
- Setting `topsy-turvy.serverPath` (overridable path to the language server binary).
- Activation on `onLanguage:topsy-turvy`.

**`language-configuration.json`** (new): line comment `ASIDE:`, block comment `(ASIDE, AT SOME LENGTH: ... END OF ASIDE.)`, auto-close `"`.

**`syntaxes/topsy-turvy.tmLanguage.json`**: TextMate grammar covering keywords, comments, strings, numbers, boolean/null literals, operators, I/O, exceptions, import, implicit variable, types, and identifiers. **Partially working — see open issues.**

**`extension.ts`**: Creates and starts a `LanguageClient` (stdio transport) pointing at the compiled language server binary. Path is read from `topsy-turvy.serverPath` setting, defaulting to the Debug build output path.

### Open Issues

#### Issue 1 — `PublishDiagnostics` produces no squiggles in VS Code

**Symptom**: `window/showMessage` notifications work; `TryParse` returns the correct error count; but `textDocument/publishDiagnostics` notifications do not produce red squiggles in VS Code.

**Investigation so far**:
- `TextDocumentSyncHandler.DidOpen` and `DidChange` handlers are definitely invoked (confirmed with `ShowMessage`).
- `TryParse` returns correct error counts (0 for valid, 1 for invalid — Superpower reports at most one error per parse attempt). This is expected and by design: Superpower is a single-pass parser combinator that stops at the first unrecoverable token; there is no built-in error recovery. Supporting multiple simultaneous diagnostics would require implementing error-recovery (e.g. synchronising at statement boundaries after each failure), which is a non-trivial future enhancement.
- Positions are valid: `ErrorPosition.Line` and `.Column` are both ≥ 1 from Superpower; after subtracting 1 the LSP positions are ≥ 0.
- Build is clean; no compile errors on the handler.

**Potential causes and fixes to try (in order)**:

1. **Lazy LINQ evaluation** — `result.Diagnostics.Select(...)` is lazy; if `Container<Diagnostic>` defers enumeration and an exception is thrown during serialisation, OmniSharp may silently drop the notification. Fix: materialise with `.ToList()` before passing to `new Container<Diagnostic>(...)`.

2. **Silent exception in `PublishDiagnostics`** — OmniSharp may swallow exceptions thrown in notification handlers. Fix: wrap the `PublishDiagnostics` call in a try/catch and route errors to `this.languageServer.Window.ShowMessage` or server-side logging.

3. **`Diagnostic.Message` is null** — Superpower's `Result<T>.ErrorMessage` is `string?` and may be null or empty for certain failures. A null `Message` could cause a JSON serialisation error that kills the notification silently. Fix: default to a non-null fallback: `result.ErrorMessage ?? "Syntax error"`.

4. **Position pre-processing offset mismatch** — `TryParse` runs `CommentsPreProcessor` and `VictorianFlourishPreProcessor` before parsing; error positions are relative to the pre-processed text, not the original source. This would produce wrong squiggle positions rather than missing ones, but could cause negative column values in some edge cases. Fix (long-term): integrate `SourceMap` position remapping.

5. **`DidSave` handler not sending diagnostics on initial open** — The extension sends `DidOpen` on file open. Confirm the OmniSharp routing is calling the correct overload; if `DidSave` is being called instead of `DidOpen` at some point and does not call `PublishDiagnostics` in all code paths, squiggles could be cleared immediately. Check: add identical `ShowMessage` probes to `DidSave` and `DidClose` to distinguish.

#### Issue 2 — Multi-line block comments highlighted on first line only

**Symptom**: `(ASIDE, AT SOME LENGTH: ... END OF ASIDE.)` spanning multiple lines only applies `comment.block.topsy` colouring to the first line.

**Cause**: The `"block-comments"` repository entry in `topsy-turvy.tmLanguage.json` contains inner `"patterns": [{ "match": ".*", "name": "comment.block.topsy" }]`. The `.*` regex does not match newlines, so the scope is only applied to the content of the first matched line. The outer `begin`/`end` rule provides multi-line span and the correct `name` already — the inner patterns are not only redundant but actively break the behaviour.

**Fix**: Remove the `"patterns"` array from the `"block-comments"` repository entry. The `begin`/`end` rule with `name: "comment.block.topsy"` is sufficient to colour the entire block.

#### Issue 3 — `IT IS MY DUTY TO PERFORM` not highlighted

**Symptom**: The function declaration keyword is not coloured as a keyword.

**Cause**: `IT IS MY DUTY TO PERFORM` is absent from the `"functions"` pattern group in the tmLanguage grammar. The grammar has `MY DUTY IS DISCHARGED.` and `MY DUTY IS PREMATURELY DISCHARGED.` (return keywords) but not the opening declaration form.

**Fix**: Add to the `"functions"` patterns array:
```json
{ "match": "\\bIT\\s+IS\\s+MY\\s+DUTY\\s+TO\\s+PERFORM\\b", "name": "keyword.other.function.topsy" }
```

---

## 5. Next Steps (Start of Next Session)

1. Read `AGENTS.md` and `DEVELOPMENT.md` before starting any work.
2. Run `dotnet build interpreter/BWHazel.TopsyTurvy.slnx` and `dotnet test` to confirm baseline (0 errors, 17/17 tests).
3. Work through Phase 5 open issues in order:
   - **Issue 1 (diagnostics)**: try the potential fixes in order — materialise LINQ, add try/catch, fix null message, check DidSave probe.
   - **Issue 2 (block comments)**: remove inner `patterns` from the `block-comments` repository entry in `topsy-turvy.tmLanguage.json`.
   - **Issue 3 (function keyword)**: add `IT IS MY DUTY TO PERFORM` pattern to the `functions` group.
   - After fixes: audit the full tmLanguage grammar against `SPEC.md` for any other missing multi-word keywords.
4. Once Phase 5 issues are resolved, consider Phase 6:
   - LSP hover support (show variable type / function signature on hover)
   - LSP go-to-definition (navigate to `IT IS MY DUTY TO PERFORM` declaration)
   - LSP completion (suggest keywords at the current cursor position)
   - `PRAY ADMIT` multi-file import integration testing
   - **Error message improvements** (three layers, work in order):
     1. **Source map verification** — confirm `TryParse` and `Parse` use `SourceMap`/`SourceMapping` to remap Superpower error positions from pre-processed text back to original source line/column. Without this, errors point at the wrong line when comments or line-continuation syntax have shifted offsets.
     2. **Runtime error messages** — review every `throw new TopsyTurvyRuntimeException(...)` in `Interpreter.cs` and replace terse messages with context-rich ones (e.g. include the actual vs. expected type, the variable name, or a hint about the relevant keyword). Straightforward string-editing work.
     3. **Parser error messages** — add `.Named("...")` annotations to key combinators in `Lexer.cs`, `StatementParser.cs`, and `ExpressionParser.cs`. Superpower uses these names when building its error strings, so annotating e.g. the `FINALE.` parser as `.Named("program end (FINALE.)")` produces a readable "expected program end (FINALE.)" instead of a raw token description.
