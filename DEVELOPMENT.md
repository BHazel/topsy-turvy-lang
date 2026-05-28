# DEVELOPMENT.md
## Topsy Turvy Language — Technical Reference for Coding Agents

* **Last Updated:** 2026-05-28
* **Specification Version:** 0.2.0 — `SPEC.md` is the grammar source of truth
* **File Extension:** `.topsy`
* **Baseline:** `dotnet build interpreter/BWHazel.TopsyTurvy.slnx` — 0 errors. `dotnet test` — 17/17.

| Component | Status |
|---|---|
| AST (`BWHazel.TopsyTurvy.Ast`) | Complete |
| Parser (`BWHazel.TopsyTurvy.Parser`) | Complete |
| Runtime (`BWHazel.TopsyTurvy.Runtime`) | Complete |
| Integration — all 4 examples pass | Complete |
| LSP server + VS Code extension | Complete |
| CLI (`operetta`) | Complete |

---

## 1. File Inventory

| File / Directory | Role |
|---|---|
| `AGENTS.md` | Project rules and constraints — read before starting any work. |
| `SPEC.md` | Authoritative language specification v0.2.0. |
| `GRAMMAR.ebnf` | Formal EBNF grammar — blueprint for the parser. |
| `examples/` | `hello_world.topsy`, `fizzbuzz.topsy`, `fibonacci.topsy`, `pirates_calculator.topsy` — all execute correctly. |
| `interpreter/BWHazel.TopsyTurvy.slnx` | Solution file. |
| `interpreter/BWHazel.TopsyTurvy.Ast/` | AST node types. All inherit from `Node` (`required SourceSpan Span`). `Diagnostic`, `DiagnosticCollection`, `SourceSpan`, `SourceLocation`. |
| `interpreter/BWHazel.TopsyTurvy.Parser/` | Superpower parser. Entry: `TopsyTurvyParser`. Pre-processors: `CommentsPreProcessor`, `VictorianFlourishPreProcessor`. `SourceMap`/`SourceMapping` for offset tracking. `TryParse` returns `ParseResult(ProgramNode?, IReadOnlyList<Diagnostic>)`. |
| `interpreter/BWHazel.TopsyTurvy.Runtime/` | Tree-walking interpreter. Entry: `Interpreter`. Key types: `TopsyTurvyValue`, `TopsyTurvyEnvironment`, `ITopsyTurvyIO`/`ConsoleIO`. Signal exceptions: `BreakSignalException`, `ReturnSignalException`, `ContinueSignal`, `TopsyTurvyThrowException`. |
| `interpreter/BWHazel.TopsyTurvy.Tests/` | xUnit suite — 17/17 passing. |
| `interpreter/BWHazel.TopsyTurvy.Cli/` | CLI project. Binary: `operetta`. Version: `0.1.0`. Dependencies: `Spectre.Console 0.55.2`, `Spectre.Console.Json 0.55.2`, `System.CommandLine 2.0.8`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/ProgramExecutionResult.cs` | Discriminated result: `Success()`, `Failure(msg)`, `SyntaxError(errors)`, `RuntimeError(diagnostics)`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/ProgramRunner.cs` | `Run`, `Check`, `ParseFile` — all delegate to private `TryReadSource`. `ParseFile` returns `(ProgramExecutionResult, ParseResult?)`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/PanelHelper.cs` | All Spectre.Console panel construction. `WriteDefault`, `WriteSuccess`, `WriteUserError`, `WriteSyntaxErrors`, `WriteRuntimeErrors`, `WriteVersionInfo`, `ReportErrors`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/NodeJsonConverter.cs` | `JsonConverter<Node>` with `CanConvert` override for polymorphic AST serialisation with `$type` discriminators. |
| `interpreter/BWHazel.TopsyTurvy.Cli/CommandBuilders/` | One builder per command — see §3. |
| `interpreter/BWHazel.TopsyTurvy.LanguageServer/` | OmniSharp LSP server — see §3. |
| `extensions/vscode/topsy-turvy/` | VS Code extension: LSP client, TextMate grammar, run/stop commands, panel and file icons. |

---

## 2. Non-Obvious Constraints

A new session must know these before touching the relevant code.

**PlaceholderSpan**: every AST node carries `Span = (0,0)-(0,0)` — source positions were never wired into the Superpower parser. LSP handlers recover definition positions by scanning raw source lines for `PRAY WELCOME <name>` and `IT IS MY DUTY TO PERFORM <name>`. Once position wiring is added to the parser, `FindDeclarationSpan` can be replaced with `node.Span`.

**`JUST SO` exclusion**: the implicit variable's name contains a space, so it cannot be matched with word-boundary regex. It is excluded from rename, references, and semantic token scanning, and is always added to `SymbolTable` manually.

**LSP stdio**: `.ClearProviders()` must be called before `.AddLanguageProtocolLogging()` in the LSP `Program.cs`. Without it, the default console logger writes to stdout and corrupts the JSON-RPC stream.

**`DocumentStateManager`**: singleton; rebuilds `SymbolTable` only on a successful parse, preserving the last good table during syntax errors.

**`TryParse` reserved-word errors**: returns `ParseResult(program, errors)` with non-null `program` so the symbol table still builds while squiggle diagnostics are published. `Parse()` (used by the CLI) throws `TopsyTurvySyntaxException` instead.

**`SUMMON` call syntax**: `SUMMON name WITH arg1 AND arg2 IF YOU PLEASE.` / `SUMMON name WITH NOTHING IF YOU PLEASE.` — uses `WITH` as the argument separator, not `AND`.

**Multi-word keyword completion**: `CompletionHandler` uses server-side phrase filtering, `FilterText = lastWord`, `InsertText = keyword[insertOffset..]`, `isIncomplete = true`, and a space trigger character to keep the list live as the user types through multi-word phrases.

**CLI version string**: `AssemblyInformationalVersionAttribute` = `{Version}+{SourceRevisionId}`. `<Version>` in the CLI csproj controls the numeric part; the SDK appends the git SHA. `PedigreeCommandBuilder` splits on `+` and holds `SpecVersion = "0.2.0"` as a private constant.

---

## 3. LSP Handlers & CLI Commands

### LSP Handlers (`interpreter/BWHazel.TopsyTurvy.LanguageServer/`)

| Handler | Purpose |
|---|---|
| `TextDocumentSyncHandler` | `didOpen`/`didChange`/`didSave`/`didClose`. Calls `TryParse`, publishes diagnostics, updates `DocumentStateManager`. |
| `HoverHandler` | Markdown tooltip for variables, functions, parameters, and `JUST SO`. |
| `DefinitionHandler` | Go-to-definition using `DefinitionLine` recovered from source scan. |
| `CompletionHandler` | Symbols + 74 keywords. Server-side phrase filtering for multi-word keywords. Known edge case: variable names that are keyword prefixes may trigger false keyword-context mode. |
| `SemanticTokensHandler` | Colours variables, parameters, functions by whole-word line scan. Excludes `JUST SO`. |
| `RenameHandler` | Whole-word case-insensitive rename; skips comments and strings. Single-file only. |
| `PrepareRenameHandler` | Pre-validates rename targets; returns placeholder range for input box. |
| `DocumentSymbolHandler` | Populates VS Code Outline panel from `SymbolTable`. |
| `ReferencesHandler` | Find All References; respects `IncludeDeclaration`. |
| `SignatureHelpHandler` | Parameter hints inside `SUMMON` calls; counts `AND` tokens for active parameter. |
| `FoldingRangeHandler` | Stack-based fold regions for all block constructs and comments. |
| `DocumentFormattingHandler` | Normalises keyword casing (74 keywords) and applies 2-space libretto indentation. |
| `CodeLensHandler` | Inline reference-count annotations above declarations; links to Find All References. |

### CLI Commands (`interpreter/BWHazel.TopsyTurvy.Cli/CommandBuilders/`)

| Command | Alias(es) | Options | Notes |
|---|---|---|---|
| `perform <file>` | `stage`, `run` | `--tiptoe` | Runs a `.topsy` file. |
| `commission <file>` | `new` | `--title` (required), `--or`, `--tiptoe` | Scaffolds a Hello World `.topsy` file. UTF-8, no BOM. |
| `rehearse <file>` | `check` | `--tiptoe` | Syntax-checks without executing via `ProgramRunner.Check`. |
| `sorcerer promptbook <file>` | `dev ast` | `--abridged`, `--chromatic`, `--tiptoe` | Prints AST as JSON. `NodeJsonConverter` handles polymorphic serialisation. |
| `pedigree` | `info` | `--tiptoe` | Shows CLI version, commit SHA, and language spec version. |

---

## 4. Known Gaps

**LSP squiggle accuracy (deferred)**: when a syntax error appears inside a block construct (function, loop, conditional, switch, try/catch), the squiggle lands on the opening line rather than the error line. Root cause: `Ws(Statement).Try()` in `ProgramParser.body` resets Superpower's error position before the entire construct on failure. Fixing this requires replacing `.Try().Many()` with a custom error-recovery loop — significant parser refactoring, deferred.

**Keyword-as-name squiggle precision**: diagnostic spans are recovered by source-line scanning (`FindDeclarationSpan`) as a workaround for PlaceholderSpan. Replaceable once AST position wiring is added.

### Deferred LSP Features

| Feature | Blocker |
|---|---|
| Inlay Hints (`textDocument/inlayHint`) | Handler not yet written; `SymbolInfo.TypeDisplayName` already carries the data. |
| Call Hierarchy | Requires a call-graph not currently built by `SymbolTable`. |
| Workspace Symbols | `DocumentStateManager` is single-document. Deferred until `PRAY ADMIT` imports are in use. |

---

## 5. Next Steps

1. Read `AGENTS.md` and `DEVELOPMENT.md` before starting any work.
2. Run `dotnet build interpreter/BWHazel.TopsyTurvy.slnx` and `dotnet test` to confirm baseline.
3. **LSP server packaging**: the default binary path only works from the dev repo layout and cannot be distributed as a `.vsix`. Proposed: add `dotnet publish` to the extension build script outputting to `extensions/vscode/topsy-turvy/server/`; update the default path in `extension.ts`; use `--no-self-contained` as the starting point.
4. **LSP squiggle accuracy** (deferred — see §4).
5. **`PRAY ADMIT` multi-file import** — integration testing.
