# DEVELOPMENT.md
## Topsy Turvy Language — Technical Reference for Coding Agents

* **Last Updated:** 2026-05-26
* **Current Specification Version:** 0.2.0
* **Interpreter Status:** Complete (Phase 4)
* **LSP & VS Code Extension:** Core LSP Features Complete (Phase 5)
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

Build: 0 errors. Tests: 17/17. All four example programs execute correctly.

**LSP diagnostic squiggle accuracy** — the primary outstanding issue (see §4 for full history and what has been tried):

1. **Multi-line construct errors**: when there is a syntax error inside a block construct (function definition, loop, conditional, switch, try/catch), the squiggle lands on the **opening line** of the construct rather than the line with the actual error. Root cause: `Ws(Statement).Try()` in `ProgramParser` resets the Superpower error position to before the entire construct whenever any part of it fails. The inner error extraction added in session 2026-05-26 (`StatementParser.Statement.TryParse` re-run at `startOffset`) can only recover the deeper position when the construct parser has **no** `.Try()` in the `Statement.Or()` chain — confirmed for `FunctionDefinition` but the improvement is still limited in practice. All block constructs (`FunctionDefinition`, `Loop`, `Conditional`, `TryCatch`, `Switch`) have been confirmed to lack individual `.Try()` calls, so the inner extraction is attempted for all of them; however user testing shows squiggle placement remains imprecise for block construct errors.

2. **Error messages for block errors** — `.Named()` annotations added in session 2026-05-26 improve the message text (e.g. `"Expected: MY DUTY IS DISCHARGED. (end of function)"` instead of `` "Expected: `MY DUTY IS DISCHARGED.`" ``), but messages are still tied to the wrong source location when the squiggle position is incorrect, making them confusing in practice.

**Root cause of the remaining positioning issue**: `Ws(Statement).Try()` in `ProgramParser.body` is the fundamental constraint. `.Try()` intentionally discards the inner error position so `.Many()` can continue. Any improvement to squiggle placement for block-level errors requires either (a) removing the outer `.Try()` on complex block parsers and replacing with a custom error-recovery loop, or (b) a two-pass strategy where the first pass identifies which line is bad, and the second pass provides a targeted diagnostic. Both are significant parser refactoring tasks.

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

### Part 4 — Symbol Infrastructure — Complete

Five new files added to `BWHazel.TopsyTurvy.LanguageServer/`:

| File | Purpose |
|---|---|
| `SymbolKind.cs` | Enum: `Variable`, `Function`, `Parameter` |
| `SymbolInfo.cs` | Per-symbol metadata: name, kind, type display name, parameter list, definition line/column (1-indexed; 0 = unknown) |
| `SymbolTable.cs` | Built from `(ProgramNode, string originalSource)`. Walks the AST to collect symbols; scans source lines to recover definition positions (workaround for all-zero `PlaceholderSpan`). Static `ExtractWordAt(source, line, column)` extracts the identifier at a 0-indexed LSP position. Case-insensitive `Dictionary<string, SymbolInfo>`. `JUST SO` always added as a built-in variable. |
| `DocumentState.cs` | Per-document: `Source` (current text) + `SymbolTable?` (null until first successful parse) |
| `DocumentStateManager.cs` | Singleton. `Update` always refreshes `Source` but only rebuilds the `SymbolTable` when the parse succeeds, preserving the last good table during syntax errors. Thread-safe via `lock`. |

**`TextDocumentSyncHandler.cs`** updated to inject `DocumentStateManager`; calls `Update(uri, text, result)` after `TryParse` and `Remove(uri)` on `DidClose`.

**`Program.cs`** updated to register `DocumentStateManager` as a singleton and add the three new handlers.

**Known constraint — PlaceholderSpan**: every AST node carries `Span = (0,0)-(0,0)` because source positions were never wired up in the Superpower parser. Definition positions are recovered by scanning original source lines for `PRAY WELCOME <name>` and `IT IS MY DUTY TO PERFORM <name>` patterns. Hover and completion work from the `SymbolTable`; go-to-definition returns a result only when a definition line is found via the scan.

### Part 5 — Hover Handler — Complete

**`HoverHandler.cs`**: extends `HoverHandlerBase`. Extracts the word at the cursor with `SymbolTable.ExtractWordAt`, looks it up in the `SymbolTable`, and returns a Markdown hover. Hover content per kind:

| Kind | Markdown |
|---|---|
| `JUST SO` | `**implicit variable** \`JUST SO\` — receives the result of the last expression` |
| Variable | `**(variable)** \`name\` : PEER` (or FATHOM / YARN / DECREE / NAUGHT) |
| Function | `**(function)** \`name\`(param1, param2)` |
| Parameter | `**(parameter)** \`name\`` |

Returns `null` hover (no tooltip) when the word at cursor is not in the symbol table.

### Part 6 — Go-to-Definition Handler — Complete

**`DefinitionHandler.cs`**: extends `DefinitionHandlerBase`. Extracts word at cursor, looks it up, and returns a `Location` covering the name token on its definition line. Returns an empty result if `DefinitionLine == 0` (position not recoverable from source scan — e.g. parameter, or symbol added before any parse succeeded).

### Part 7 — Completion Handler — Complete (with known limitations)

**`CompletionHandler.cs`**: extends `CompletionHandlerBase`. Returns two merged item groups:

**Symbols** (from `SymbolTable`): `CompletionItemKind.Variable` / `CompletionItemKind.Function`, filtered by the current last word.

**Keywords** (74 static entries): `CompletionItemKind.Keyword`, covering the full keyword set from SPEC.md v0.2.0. The static list was audited against the spec and corrected in this session (removed several non-existent entries, added missing ones). Keywords use `FilterText` and `InsertText` to handle multi-word phrases correctly (see below).

**Multi-word keyword completion approach**: VS Code's completion model treats the text since the last word separator (space) as the "current word" for client-side filtering. Multi-word keywords like `SHOULD IT TRANSPIRE THAT` are therefore invisible to the client-side filter once the user has typed past the first space. The server-side workaround:
1. `GetPhraseContext` reads the trimmed text from line start to cursor (`phrase`) and the last space-delimited word (`lastWord`).
2. Keywords are filtered server-side: only keywords where `keyword.StartsWith(phrase, OrdinalIgnoreCase)` are returned — but only when `phrase` is itself a prefix of at least one keyword. When no keyword starts with `phrase` (e.g. after `result `), all keywords are returned.
3. `FilterText = lastWord` satisfies VS Code's client-side filter for the current word.
4. `InsertText = keyword[insertOffset..]` where `insertOffset = phrase.Length - lastWord.Length` — VS Code deletes its current word and inserts this suffix, reconstructing the full keyword without duplicating already-typed text.
5. `isIncomplete = true` tells VS Code to re-request on each keystroke so the server can return a freshly-filtered list.
6. `TriggerCharacters = [" "]` reopens the list on every space so the list stays live as the user types through multi-word phrases.

**Known limitations**:
- If a variable or function name is a prefix of any keyword (e.g. a variable named `by` when `BY A LEGAL FICTION` is a keyword), keyword-context mode is triggered and insertion may be incorrect. Fix planned — see §5.
- The context check is case-insensitive, so lowercase keyword usage (the language allows it) is handled correctly.

### Open Issues

See §3 Known Gaps for remaining squiggle accuracy issues.

**Fixes applied in session 2026-05-25:**

1. **`PublishDiagnostics` squiggles** (`TextDocumentSyncHandler.cs`, `TopsyTurvyParser.cs`):
   - Materialised the LINQ projection with `.ToList()` before passing to `Container<Diagnostic>`.
   - Wrapped the full `PublishDiagnostics` method body in `try/catch`; exceptions surface as `ShowMessage` errors instead of being swallowed silently.
   - Added `using LspRange = ...Range` alias to resolve `System.Range` / OmniSharp `Range` ambiguity introduced by `using System;`.
   - **Squiggle position fix** (`TopsyTurvyParser.cs`): `TryParse` now uses `result.ErrorPosition.Absolute` (Superpower's zero-based character offset) with `processed.SourceMap.GetOriginalLocation()` to remap the error from pre-processed text coordinates back to original source coordinates. Previously the raw `Line`/`Column` from the pre-processed text was used directly, causing squiggles to land in wrong positions when comments or `~` continuations shifted offsets.
   - **Zero-width span fix** (`TopsyTurvyParser.cs`): `TryParse` now extends the diagnostic span from the error position to the end of the bad line (scanning forward to the next `\n`/`\r` in the pre-processed text, then remapping via `SourceMap`). Previously `Start == End` produced a zero-width range, which VS Code renders unpredictably (squiggle at wrong line, no hover tooltip).
   - **Hover message fix** (`TopsyTurvyParser.cs`): Superpower's `Result<T>.ErrorMessage` is always null when parsers have no `.Named(...)` annotations. The diagnostic message is now built from `result.Expectations[]` with a "Syntax error" fallback.

2. **Multi-line block comments** (`topsy-turvy.tmLanguage.json`): Removed the inner `"patterns": [{ "match": ".*" }]` from `"block-comments"`. The outer `begin`/`end` rule is sufficient for multi-line colouring.

3. **`IT IS MY DUTY TO PERFORM` keyword** (`topsy-turvy.tmLanguage.json`): Added to `"functions"` patterns. Replaced the invalid `ALL HANDS ON DECK` entry (not in SPEC.md) with the correct declaration form.

4. **Grammar audit fixes** (`topsy-turvy.tmLanguage.json`):
   - Types pattern corrected from `NUMBER|DECIMAL` (invalid) to `PEER|FATHOM` (SPEC.md §3).
   - Added `LARGER` and `SMALLER` to the arithmetic operator group (SPEC.md §5).
   - Added `IS HENCEFORTH A` to the `"cast"` patterns (SPEC.md §3).
   - Added `WITHOUT CEREMONY` to the `"io"` patterns (SPEC.md §4).

5. **Whitespace skip fix** (`TopsyTurvyParser.cs`): After `result.ErrorPosition.Absolute`, `TryParse` now skips any leading whitespace before computing the span. When `Ws(Statement).Try()` fails in the body, Superpower resets the error position to the `\n` at the end of the previous valid statement (before the `Ws` call), not the start of the bad line — so without this skip, squiggles appeared one line above the actual error. Fixed by advancing `startOffset` past all whitespace characters.

6. **SourceMap line-shift fix** (`VictorianFlourishPreProcessor.cs`): `currentLine` is now always incremented for every physical line processed, including `~` continuation lines. Previously it was only incremented for non-continuation lines. A continuation block of N physical lines (e.g. the 3-line `AND SO I FIND ~ / SUM OF ~ / SUMMON...` in `fibonacci.topsy`) left `currentLine` under-counted by N−1, shifting all subsequent SourceMap entries N−1 lines too low. The fibonacci file's 3-line continuation block shifted entries by −3, causing the squiggle for an error near `BEHOLD` on line 52 to appear on line 49 instead.

7. **Inner error extraction** (`TopsyTurvyParser.cs`): After finding `startOffset` (the beginning of the failing construct), `TryParse` now re-runs `StatementParser.Statement.TryParse` on the content starting there. `FunctionDefinition` has no `.Try()` in the `Statement.Or()` chain, so when it fails deep inside (e.g. a typo in `MY DUTY IS DISCHARGED.`), the hard failure preserves the inner error position (`innerResult.ErrorPosition.Absolute > 0`). That inner offset is used for the span and message instead of the outer reset position. Result: squiggle lands on `MY DUTY IS DISCHARsGED.` with message `"Expected: \`MY DUTY IS DISCHARGED.\`"` rather than on `IT IS MY DUTY TO PERFORM...` with `"Unexpected: IT IS MY DUTY..."`.
   - **Limitation**: this only helps for constructs that have no `.Try()` in the `Statement.Or()` chain. Other multi-statement constructs (`Loop`, `Conditional`, `TryCatch`, `Switch`) need to be checked — if any of them have `.Try()` applied individually, the inner error extraction approach will see `innerResult.ErrorPosition.Absolute = 0` (reset by `.Try()`) and fall back to the outer reset position.

8. **Message fix for body errors** (`TopsyTurvyParser.cs`): When `Ws(Statement).Try().Many()` swallows the inner error, the only remaining `Expectations` is `["FINALE."]` — correct but unhelpful. When `Expectations` contains only "FINALE." and the error is not at EOF, the message now shows the bad line content (`"Unexpected: <line>"`) rather than `"Expected: FINALE."`. If the error is at EOF, `"Expected: \`FINALE.\`"` is preserved as it is correct in that context (missing program end marker).

9. **`.Named()` annotations** (`Lexer.cs`, `StatementParser.cs`, `TopsyTurvyParser.cs`): Added `.Named("description")` to key parsers so that `Expectations[]` — and therefore the hover message — reads as plain English rather than backtick-wrapped raw syntax. Superpower's `.Named(name)` replaces the parser's failure expectation with `name`. Changes:
   - `Lexer.StringLiteral` → `"string literal"` (was `"\""` — a bare quote character).
   - `Lexer.Identifier` → `"identifier"` (was `"letter"` or `"identifier (not a reserved keyword)"`).
   - `Keyword("QUITE SO.")` in `ConditionalBody` → `"QUITE SO. (then-block)"`.
   - `Keyword("SO MUCH FOR THAT.")` in `Conditional` → `"SO MUCH FOR THAT. (end of conditional)"`.
   - `Keyword("NOTHING COULD BE MORE SATISFACTORY.")` in `Switch` → `"NOTHING COULD BE MORE SATISFACTORY. (end of switch)"`.
   - `Keyword("THE TERM EXPIRES.")` in `Loop` → `"THE TERM EXPIRES. (end of loop)"`.
   - `Keyword("THAT CONCLUDES THE MATTER.")` in `TryCatch` → `"THAT CONCLUDES THE MATTER. (end of try/catch)"`.
   - `Keyword("MY DUTY IS DISCHARGED.")` in `FunctionDefinition` → `"MY DUTY IS DISCHARGED. (end of function)"`.
   - `Keyword("THE CURTAIN RISES.")` in `PrincipalBlock` → `"THE CURTAIN RISES. (end of declarations)"`.
   - `Lexer.StringLiteral` in `ProgramParser` title position → `"program title"` (inline, does not affect other string literal uses).
   - `Keyword("FINALE.")` in `ProgramParser` → `"FINALE. (program end)"`. The `onlyExpectsFinale` detection in `TryParse` still matches because the string still contains "FINALE".

**Fixes applied in session 2026-05-26 (session 1):**

10. **Case-insensitive keyword highlighting** (`topsy-turvy.tmLanguage.json`): Added the Oniguruma `(?i)` inline flag to every pattern in the grammar that contains letter characters — all `"match"` patterns that previously started with `\b` now start with `(?i)\b`, and the comment patterns (`"ASIDE:.*$"`, `\(ASIDE,\s+AT\s+SOME\s+LENGTH:`, `END\s+OF\s+ASIDE\.\)`) are also prefixed with `(?i)`. This ensures keywords highlight correctly when written in any mixture of upper and lower case (e.g. `hark!`, `Hark!`, `HARK!` all highlight identically). Number and identifier patterns are unaffected functionally; `(?i)` on a digit-only or `[A-Za-z]` class pattern is harmless.

11. **`or,` subtitle keyword** (`topsy-turvy.tmLanguage.json`): Added `"(?i)\\bor,"` to the `"program-structure"` patterns group alongside `HARK!` and `FINALE.`. The `or,` keyword is used in the optional programme subtitle line (`HARK! "Title" or, "Subtitle"`) and was previously not syntax-highlighted at all.

**Fixes applied in session 2026-05-26 (session 2):**

12. **LSP symbol infrastructure** (`SymbolKind.cs`, `SymbolInfo.cs`, `SymbolTable.cs`, `DocumentState.cs`, `DocumentStateManager.cs`): Full implementation of per-document symbol state, AST walking, and source-scan-based definition position recovery. `TextDocumentSyncHandler` and `Program.cs` updated to wire in `DocumentStateManager`.

13. **Hover handler** (`HoverHandler.cs`): Implemented `HoverHandlerBase`. Extracts word at cursor, looks up symbol, returns Markdown hover content.

14. **Go-to-definition handler** (`DefinitionHandler.cs`): Implemented `DefinitionHandlerBase`. Returns `Location` at the definition line for known symbols; empty result when position is unknown (PlaceholderSpan limitation).

15. **Completion handler** (`CompletionHandler.cs`): Implemented `CompletionHandlerBase`. Returns all declared symbols plus 74 keywords from SPEC.md v0.2.0. Keyword list audited and corrected against the spec. Multi-word keyword completion uses server-side phrase filtering, `InsertText` with character offset, `FilterText = lastWord`, `isIncomplete = true`, and a space trigger character. Context detection (`isKeywordContext`) uses `Keywords.Any(k => k.Keyword.StartsWith(phrase, OrdinalIgnoreCase))` to avoid false-positive keyword mode after variable names, and to correctly handle case-insensitive keyword usage.

16. **Semantic tokens handler** (`SemanticTokensHandler.cs`): Implemented `SemanticTokensHandlerBase`. Registers a legend with token types `["variable", "parameter", "function"]`. On each `textDocument/semanticTokens/full` request, scans the source text line-by-line for whole-word occurrences of every symbol in the `SymbolTable` (case-insensitive, skipping `JUST SO` which contains a space), collects `(line, char, length, tokenType)` tuples, sorts them by position, and pushes to `SemanticTokensBuilder`. Multi-word built-ins (currently only `JUST SO`) are excluded from scanning as they cannot be tokenised reliably as a single span. Editor themes (e.g. GitHub Dark) then apply colours: `variable` → white, `parameter` → orange, `function` → purple/blue.

---

## 5. Next Steps (Start of Next Session)

1. Read `AGENTS.md` and `DEVELOPMENT.md` before starting any work.
2. Run `dotnet build interpreter/BWHazel.TopsyTurvy.slnx` and `dotnet test` to confirm baseline (0 errors, 17/17 tests).
3. Consider Phase 6:

   - **Prevent keywords as variable/function names** (planned, low-medium complexity):
     The language allows identifiers to start with uppercase and keywords are case-insensitive, so `BEHOLD`, `SUMMON`, or `by` are currently valid variable names — creating an ambiguity in completion context detection and potential parser confusion.
     **Implementation plan:**
     - Define a static `HashSet<string>` of reserved words in `TopsyTurvyParser` (or a shared constants file). Reserve the **first word** of each multi-word keyword and all complete single-word keywords (e.g. `BEHOLD`, `SUMMON`, `PEER`, `FATHOM`, `YARN`, `DECREE`, `PRAY`, `BY`, `SHOULD`, `SUMMON`, `WITH`, etc.). Avoid reserving very common English words that appear only mid-keyword (e.g. `AND`, `OF`, `AS`) to keep the language expressive.
     - After a successful parse, add a **semantic validation walk** inside `TopsyTurvyParser.TryParse`: iterate all `DeclarationNode.Name` and `FunctionDefinitionNode.Name` values in the AST, checking each case-insensitively against the reserved set.
     - Emit a `Diagnostic` with `DiagnosticSeverity.Error` for any match, appended to the `ParseResult.Diagnostics` list before returning.
     - No changes required to the runtime, LSP server, or VS Code extension — the diagnostic surfaces as a red squiggle automatically via the existing `TextDocumentSyncHandler` pipeline.

   - **Fix hard-coded LSP server path in VS Code extension**:
     `extension.ts` (compiled to `dist/extension.js`) resolves the server binary via `context.asAbsolutePath(path.join("..", "..", "..", "interpreter", "BWHazel.TopsyTurvy.LanguageServer", "bin", "Debug", "net10.0", "BWHazel.TopsyTurvy.LanguageServer"))`. This is hard-coded to the `Debug` build configuration and `net10.0` target, and only works when the extension folder is in its current position relative to the repo root. It should be made configurable (read from the `topsy-turvy.serverPath` setting with a sensible default, handling the `Release` vs `Debug` distinction and/or using a `dotnet run` invocation as the transport).

   - **Remaining LSP squiggle accuracy** (see §3 Known Gaps — known limitation, deferred):
     The root cause is `Ws(Statement).Try()` in `ProgramParser.body`. Fixing this properly requires a custom error-recovery loop in place of `.Try().Many()`, which is significant parser refactoring. The current behaviour (squiggle on the opening line of the failing construct) is a known limitation.

   - **Runtime error messages** — review every `throw new TopsyTurvyRuntimeException(...)` in `Interpreter.cs` and replace terse messages with context-rich ones (e.g. include the actual vs. expected type, the variable name, or a hint about the relevant keyword). Straightforward string-editing work with no architectural risk.

   - **`PRAY ADMIT` multi-file import** — integration testing.
