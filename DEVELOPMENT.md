# DEVELOPMENT.md
## Topsy Turvy Language — Technical Reference for Coding Agents

* **Last Updated:** 2026-06-18 (Documentation quality pass — `## Array Constructs` section removed from `parser.md` (language-spec content, not toolchain description); array implementation details folded into existing Environment and Interpreter sections of `runtime.md` rather than given a standalone `## Arrays` section; `ArrayAssignmentNode` renamed to `ArrayElementAssignmentNode` throughout and `ExecuteArrayAssignment` renamed to `ExecuteArrayElementAssignment`; new runtime test `Execute_ArrayIndex_ScalarAssignment_CopiesValueNotReference` added (513 total). **Previously:** Sized array declarations — `PRAY WELCOME arr AS A LITTLE LIST OF 3 YARN` pre-allocates 3 `NAUGHT` elements, enabling `VICTIM n ON arr IS APPOINTED val` without a `BEING` clause. `ArrayDeclarationNode.Size` (`int?`) added; parser optional-integer combinator between `LITTLE LIST OF` and the type keyword; runtime guards for size+BEING conflict and negative size (both produce runtime errors). SymbolTable `TypeDisplayName` updated to reflect size when present (e.g. `LITTLE LIST OF 3 YARN`). 7 new tests added (512 total). `SPEC.md` and `GRAMMAR.ebnf` updated. No tokenizer/grammar changes required — integers already highlighted as `constant.numeric`. **Previously:** Arrays added as a first-class type — `PRAY WELCOME arr AS A LITTLE LIST OF YARN BEING "a" AND "b" IF YOU PLEASE.`, `VICTIM n ON arr` (1-based index expression), `VICTIM n ON arr IS APPOINTED val` (element assignment). `LiteralType.Array` added. `TopsyTurvyValue.Array(List<TopsyTurvyValue>)` factory added. `ArrayDeclarationNode`, `ArrayIndexNode`, `ArrayAssignmentNode` added to the AST project. `ExecuteArrayDeclaration`, `ExecuteArrayAssignment`, `EvaluateArrayIndex` added to the interpreter. `IsConstantInChain` added to `TopsyTurvyEnvironment` to walk the enclosing scope chain for constant enforcement during element mutation. Arrays use reference semantics — two variables assigned from the same source share the same underlying `List<TopsyTurvyValue>`. CONSERVATIVE arrays are fully immutable (elements cannot be reassigned). Empty arrays are falsy; non-empty arrays are truthy. Array traversal should use a WHILST loop — ascending loops always reset their counter to 0 (see §4 Known Gaps). 19 parser and runtime tests added (501 total). `examples/language/arrays.topsy` added. `GRAMMAR.ebnf` and `SPEC.md` updated. Monaco tokenizer and TextMate grammar updated for `A LITTLE LIST OF`, `VICTIM`, `ON`. **Previously:** `AS IT WERE ... AS A` promoted to expression — the non-mutating cast is now an `Expression` node and can appear anywhere a value is expected: `IS APPOINTED AS IT WERE`, `BEING AS IT WERE`, as a function argument, or nested inside another expression. Standalone use still sets `JUST SO` via the `ExpressionStatement` path. `TypeCastNode` removed; `InPlaceCastNode` now extends `Statement` directly; `ExpressionCastNode` extends `Expression`. `TypeKeyword` moved from `StatementParser` to `ExpressionParser`. **Previously:** Constant identifier colouring added — `CONSERVATIVE`-declared variable names are highlighted in a distinct blue in both the VS Code extension (via LSP semantic token `variable.readonly` modifier → `variable.other.constant` TextMate scope) and the Web Editor (via Monaco inline decorations pushed by `setConstantDecorations` after each analysis pass). **Previously:** Named catch binding added — `MODIFIED RAPTURE, Grievance` auto-declares `Grievance` in a child scope for the exception block; backward-compatible with the existing no-binding form. **Previously:** Constants support added — `CONSERVATIVE` and `LIBERAL` mutability modifiers implemented across all layers. `PRAY WELCOME x AS A CONSERVATIVE PEER BEING 20` declares a constant; attempting to reassign, recast in place, or overwrite via `PRAY TELL` raises a runtime error. `LIBERAL` is the optional explicit-mutable counterpart; no modifier defaults to mutable. **Previously:** 2026-06-16: Language Server XML documentation **complete** — all 16 handlers and 2 support files (`LanguageServerConstants`, `LspUtilities`) documented and reviewed. Review identified: `"symbol namem"` typo in `RenameHandler.cs` Handle remarks; missing space before `<c>` tag in `HoverHandler.cs`; minor formatting issues in `CompletionHandler.cs`, `DocumentFormattingHandler.cs`, `DefinitionHandler.cs`, `DocumentSymbolHandler.cs`. `dotnetmd` identified as cause of LSP handler pages missing from Docusaurus API docs — see §4 Known Gaps.)
* **Previously:** 2026-06-15 (Runtime layer XML documentation **complete** — all types documented. `BreakSignal.cs` renamed to `BreakSignalException.cs` for consistency with `ContinueSignalException` and `ReturnSignalException`. `InterpreterExecutionOptions` record added to consolidate `Execute` options parameters; `Execute` signature reduced from 5 parameters to 3: `program`, `cancellationToken`, and `options`. `CancellationToken` kept as a separate parameter per .NET convention (CA2016); it is orthogonal to execution configuration. Ascending loop always resets its counter to 0 per spec — spec-level limitation, not a runtime bug; see §4 Known Gaps. Switch fall-through is the default; `THAT WILL DO.` is required to exit early.)
* **Specification Version:** 0.3.0 — `SPEC.md` is the grammar source of truth
* **File Extension:** `.topsy`
* **Baseline:** `dotnet build interpreter/BWHazel.TopsyTurvy.slnx --no-incremental` — 0 errors, 0 warnings. `dotnet test interpreter/BWHazel.TopsyTurvy.Tests` — 513/513. `dotnet test interpreter/BWHazel.TopsyTurvy.Cli.E2ETests` — 50/50. `dotnet test interpreter/BWHazel.TopsyTurvy.WebEditor.E2ETests` — 6/6 Playwright. `npm run test:grammar` — 4 files/all passing. `npm run test:unit` — 4/4.

| Component | Status |
|---|---|
| AST (`BWHazel.TopsyTurvy.Ast`) | Complete |
| Parser (`BWHazel.TopsyTurvy.Parser`) | Complete |
| Runtime (`BWHazel.TopsyTurvy.Runtime`) | Complete |
| Analysis (`BWHazel.TopsyTurvy.Analysis`) | Complete |
| Integration — all 4 examples pass | Complete |
| LSP server + VS Code extension | Complete |
| CLI (`operetta`) | Complete |
| Web editor (`BWHazel.TopsyTurvy.WebEditor`) | Complete (Blazor WASM) |

---

## 1. File Inventory

| File / Directory | Role |
|---|---|
| `AGENTS.md` | Project rules and constraints — read before starting any work. |
| `.actrc` | Portable `act` configuration for local workflow testing. |
| `scripts/act.sh` | Wrapper for `act` — detects host architecture and Podman socket, then delegates all arguments to `act`. Run from repo root: `scripts/act.sh push`. |
| `.github/workflows/build-test.yml` | Reusable CI workflow — triggered on `push` and `workflow_call`. Two parallel jobs: `language-toolchain` (full .NET build + unit/CLI E2E/WebEditor E2E tests) and `vscode-extension` (CLI build + extension compile, grammar, unit, and integration tests via `coactions/setup-xvfb`). VS Code integration tests require `libgtk-3-0` and related libraries installed via `apt-get`. |
| `.github/workflows/publish.yml` | Manual publish workflow — calls `build-test.yml`, then cross-compiles for `win-x64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`, producing CLI `.tar.gz` and `.vsix` artifacts per platform. |
| `SPEC.md` | Authoritative language specification v0.2.0. |
| `GRAMMAR.ebnf` | Formal EBNF grammar — blueprint for the parser. |
| `examples/` | `hello_world.topsy`, `fizzbuzz.topsy`, `fibonacci.topsy`, `pirates_calculator.topsy` — all execute correctly. `examples/language/arrays.topsy` demonstrates array declaration, element access, element assignment, truthiness, and WHILST-loop traversal. |
| `interpreter/BWHazel.TopsyTurvy.slnx` | Solution file. |
| `interpreter/BWHazel.TopsyTurvy.Ast/` | AST node types. All inherit from `Node` (`required SourceSpan Span`). Also defines `Diagnostic`, `DiagnosticCollection`, `SourceSpan`, `SourceLocation`. `TryCatchNode` carries optional `CaughtValueName` (null = no binding) for `MODIFIED RAPTURE, Name` syntax. `InPlaceCastNode` extends `Statement` directly (no shared `TypeCastNode` base). `ExpressionCastNode` extends `Expression` — the non-mutating cast is an expression, not a statement. `TypeCastNode` has been removed. `ArrayDeclarationNode` — `PRAY WELCOME arr AS A [CONSERVATIVE] LITTLE LIST OF [<size>] <type> [BEING ...]`; carries `int? Size` (pre-allocation count, null = no size). `ArrayIndexNode` extends `Expression` — `VICTIM <index> ON <arr>`. `ArrayElementAssignmentNode` extends `Statement` — `VICTIM <index> ON <arr> IS APPOINTED <value>`. `GenerateDocumentationFile` is enabled; XML documentation comments are present on all types. |
| `interpreter/BWHazel.TopsyTurvy.Analysis/` | Shared analysis library. `SymbolTable`, `SymbolInfo` (now carries `IsConstant`), `SymbolKind`, `DocumentState`, `DepthAction`. `SourceFormatter` — keyword normalisation + libretto indentation. `SourceAnalyser` — `FindSkipRanges`, `BuildLineOffsets`, `IsInSkipRange`, `FindWordOccurrences`, `CountOccurrences`. `KeywordData` — 76-entry keyword list (authoritative source of truth for LSP/completion — must not be duplicated in `Keywords.cs`; now includes `CONSERVATIVE` and `LIBERAL`). `HoverMarkdownBuilder` — Markdown hover strings; shows `(constant)` for `CONSERVATIVE` variables. References: Ast, Parser. Referenced by: LanguageServer, Cli, WebEditor. `GenerateDocumentationFile` is enabled; XML documentation **complete** for all types. |
| `interpreter/BWHazel.TopsyTurvy.Parser/` | Superpower parser. Entry: `TopsyTurvyParser`. Pre-processors: `CommentsPreProcessor`, `VictorianFlourishPreProcessor`. `SourceMap`/`SourceMapping` for offset tracking (`SourceMapping.TransformedOffset` maps a position in the pre-processed text to its original line/column). Pre-processor output carried in `PreProcessResult(TransformedText, SourceMap)`. `TryParse` returns `ParseResult(ProgramNode?, IReadOnlyList<Diagnostic>)`. `TypeKeyword` lives in `ExpressionParser` (moved from `StatementParser` to avoid a circular initialisation dependency). `ExpressionParser.ExpressionCast` is wired into both `Expression` alternatives and `ExpressionStatementParser`. `StatementParser.TypeCast` now only handles in-place casts. `StatementParser.ArrayDeclaration` parses `PRAY WELCOME arr AS A [CONSERVATIVE] LITTLE LIST OF [<size>] <type> [BEING ...]` — after `AS A` is consumed (4 chars including trailing `A`), the remaining token is `LITTLE LIST OF` not `A LITTLE LIST OF` (see §2); the optional size is parsed with `Ws(Lexer.IntegerLiteral).Select(v => (int?)v).Try().OptionalOrDefault(null)` between the `LITTLE LIST OF` keyword and the type keyword. `StatementParser.ArrayElementAssignment` parses `VICTIM <index> ON <arr> IS APPOINTED <value>`. `ExpressionParser.ArrayIndex` parses `VICTIM <index> ON <arr>`. `PrincipalBlock` accepts `Ws(ArrayDeclaration.Or(Declaration)).Try().Many()` to handle mixed declaration lists. `GenerateDocumentationFile` is enabled; XML documentation **complete** for all types. |
| `interpreter/BWHazel.TopsyTurvy.Parser/ParserHelpers.cs` | `internal static class` providing two shared helpers used across `StatementParser` and `TopsyTurvyParser`: `PlaceholderSpan` (the `(0,0)-(0,0)` sentinel assigned to all AST nodes until source-position wiring is implemented) and `Ws<T>(parser)` (wraps a parser with a required-whitespace prefix). Both files import it via `using static`. |
| `interpreter/BWHazel.TopsyTurvy.Parser/ConditionalBodyInfo.cs` | `public record` carrying the parsed body of a conditional statement — `TrueBlock`, `ElseIfs`, and `ElseBlock`. Replaces the anonymous tuple previously returned by the `ConditionalBody` parser, following the pattern established by `LoopDefinition`. |
| `interpreter/BWHazel.TopsyTurvy.Parser/SwitchBodyInfo.cs` | `public record` carrying the parsed body of a switch statement — `Cases` and `DefaultBlock`. Replaces the anonymous tuple previously returned by the `SwitchBody` parser. |
| `interpreter/BWHazel.TopsyTurvy.Runtime/` | Tree-walking interpreter. Entry: `Interpreter`. Key types: `TopsyTurvyValue` (now supports `LiteralType.Array` backed by `List<TopsyTurvyValue>`; `Array(List<TopsyTurvyValue>)` factory; `IsTruthy()` returns `true` for non-empty arrays; `CastTo()` allows cast only to `YARN` or `DECREE`; `ToString()` renders `[elem1, elem2, ...]`), `TopsyTurvyEnvironment` (tracks constants via `HashSet<string>`; `Declare` accepts optional `isConstant`; `Assign` enforces the constant constraint; `IsConstant(name)` query; `IsConstantInChain(name)` walks enclosing scope chain — required for array element mutation which bypasses `Assign`), `ITopsyTurvyIO`/`ConsoleIO`. Signal exceptions: `BreakSignalException`, `ContinueSignalException`, `ReturnSignalException`, `TopsyTurvyThrowException`. `Execute(program, cancellationToken, options?)` — pass `InterpreterExecutionOptions.ExecutionTimeout` for Blazor WASM timeout (see §2). `ExecuteTryCatch` creates a child scope via `CreateNested()` when `CaughtValueName` is set. `GenerateDocumentationFile` is enabled; XML documentation **complete** for all types. |
| `interpreter/BWHazel.TopsyTurvy.Runtime/InterpreterExecutionOptions.cs` | Positional record consolidating the three optional `Execute` parameters: `ExecutionTimeout` (`TimeSpan?`), `SourceFilePath` (`string?`), `SourceFileResolver` (`Func<string, string?>?`). Pass `null` for any unused option. |
| `interpreter/BWHazel.TopsyTurvy.Tests/` | xUnit suite — 501/501 passing. Tests are organised into five subdirectories: `Analysis/`, `Cli/`, `Parser/`, `Runtime/`, `LanguageServer/`. Shared test base classes live in each subdirectory. `Superpower.Model` types are available transitively via the Parser project reference — no direct Superpower package reference needed. The AST project has no dedicated tests — it is exclusively data types. `Parser/TopsyTurvyParserArrayTests.cs` — 12 test methods (16 test cases via Theory) covering `ArrayDeclarationNode` (including sized arrays), `ArrayIndexNode`, `ArrayElementAssignmentNode`, and mixed PRINCIPALS blocks. `Runtime/TopsyTurvyInterpreterArrayTests.cs` — 13 tests covering declaration, sized pre-allocation, element access, element assignment, CONSERVATIVE enforcement, truthiness, reference semantics, scalar-element value copy, and size+BEING/negative-size error paths. See AGENTS.md for test conventions. |
| `interpreter/BWHazel.TopsyTurvy.Cli/` | CLI project. Binary: `operetta`. Version: `0.1.0`. Dependencies: `Spectre.Console 0.55.2`, `System.CommandLine 2.0.8`, `OmniSharp.Extensions.LanguageServer 0.19.9`. References `BWHazel.TopsyTurvy.LanguageServer` (class library). Published as a single self-contained executable. |
| `interpreter/BWHazel.TopsyTurvy.Cli/ProgramExecutionResult.cs` | Discriminated result: `Success()`, `Failure(msg)`, `SyntaxError(errors)`, `RuntimeError(diagnostics)`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/ProgramRunner.cs` | `Run`, `Check`, `ParseFile` — all accept optional `IFileSystem? fileSystem = null`. `ParseFile` returns `(ProgramExecutionResult, ParseResult?)`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/PanelHelper.cs` | All Spectre.Console panel construction. `WriteDefault`, `WriteSuccess`, `WriteUserError`, `WriteSyntaxErrors`, `WriteRuntimeErrors`, `WriteVersionInfo`, `ReportErrors`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/NodeJsonConverter.cs` | `JsonConverter<Node>` for polymorphic AST serialisation with `$type` discriminators. |
| `interpreter/BWHazel.TopsyTurvy.Cli/FileManager.cs` | Shared file-creation and IO utilities. `BuildFileContent(title, subtitle?)` builds scaffold source. Three IO methods accepting `IFileSystem? fileSystem = null`: `TryReadSource`, `TryCreateProgrammeFile`, `TryMountProject`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/CommandBuilders/` | One builder per command — see §3. All scaffold content goes through `FileManager.BuildFileContent`. Includes `IncantationCommandBuilder` — starts the OmniSharp LSP server over stdio; `--stdio` accepted silently for vscode-languageclient compatibility (see §2). |
| `interpreter/BWHazel.TopsyTurvy.Cli.E2ETests/` | Black-box CLI E2E tests — 50/50 passing. `CliFixture` locates the binary from the Debug build output and creates a temp directory prefixed `topsyturvy-cli-e2e-` per run. `CliTestBase` creates a per-test subdirectory and exposes `RunAsync(args)` (10-second timeout, redirected stdout + stderr). Five test classes: `CommissionCommandTests` (11), `MountCommandTests` (11), `RehearseCommandTests` (6), `PerformCommandTests` (9), `PanelOutputTests` (13). |
| `interpreter/BWHazel.TopsyTurvy.LanguageServer/` | OmniSharp LSP **class library** — see §3. Started by the CLI via `operetta sorcerer incantation`. `LspUtilities` — shared `GetSymbolDefinitionLocation` (builds an LSP `Location` for a symbol, converting 1-based to 0-based coordinates) and `MapSymbolKind` (maps `Analysis.SymbolKind` to LSP `SymbolKind`). `DocumentStateManager` — singleton; exposes `FindSymbolInOtherDocuments`, `FindSymbolWithUriInOtherDocuments`, and `GetImportedFunctionSymbols` for cross-document lookups. |
| `extensions/vscode/topsy-turvy/` | VS Code extension: LSP client, TextMate grammar, run/stop commands, panel and file icons. LSP server launched as `operetta sorcerer incantation`. CLI binary bundled under `bin/` via `scripts/publish-cli.mjs`. `src/paths.ts` exports `resolveCliPath` — defaults to bundled binary when present, falls back to dev Debug layout. Tests: grammar (4), unit (4), integration (9). |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/` | Blazor WASM standalone web editor. Toolbar: New Project, New File, Open, Save (left); Run (centre); Clear Output, Download Output, Light/Dark toggle, G&S Labels switch (right). BlazorMonaco editor with Monarch tokenizer, inline diagnostics, hover, completion, and go-to-definition. XtermBlazor output terminal. Three `[JSInvokable]` bridge methods: `GetHoverMarkdown`, `GetSymbolCompletions`, `GetDefinitionLocation` / `SwitchToFileForDefinitionAsync`. Per-file symbol state keyed by filename in `Dictionary<string, SymbolTable>`. See §2 for constraints. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/IO/BufferedWebIO.cs` | `ITopsyTurvyIO` for Blazor WASM — stdin from `Queue<string>`, output lines to `List<string>`. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/IO/VirtualFile.cs` | Record tracking a filename in the in-memory virtual file system used for tab management and ZIP export. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/Dialogs/NewFileDialog.razor` | `MudDialog` for entering a new file name; appends `.topsy` automatically. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/Dialogs/OpenFilesDialog.razor` | `MudDialog` for opening multiple `.topsy` files via drag-and-drop or browse. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/wwwroot/js/topsy-turvy-language.js` | Monaco Monarch tokenizer, keyword completion provider, and language configuration. Defines `window.topsyTurvy`. Extended by `web-editor.js`. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/wwwroot/js/web-editor.js` | Monaco/xterm JS interop — hover, completion, definition providers, terminal fit, download utilities, constant identifier decorations (`setConstantDecorations`). Extends `window.topsyTurvy` via `Object.assign`. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor.E2ETests/` | Playwright xUnit E2E tests — 6/6 passing. Three test classes: `EditorExecutionTests`, `EditorDiagnosticsTests`, `EditorToolbarTests`. `WebEditorAppFixture` starts the Blazor app and launches headless Chromium. |
| `extensions/vscode/topsy-turvy/scripts/publish-cli.mjs` | Node.js ESM script that cross-compiles the CLI for the current (or `DOTNET_RID`-specified) platform into `bin/`. |
| `docs/topsy-turvy/` | Docusaurus v3 documentation site. `npm run start` serves locally; `npm run build` produces `build/`. `npm run api:generate` (`api:clean` + `dotnetmd --config dotnet-md.json`) generates Markdown API reference from XML comments into `docs/api/dotnet/`. Requires `DOTNET_ROLL_FORWARD=LatestMajor` (set in the npm script) because `dotnetmd` targets .NET 8. `docs/guide/`, `docs/concepts/`, and `docs/tutorials/` each contain only a placeholder `index.md` ("Coming Soon") — no content pages yet. `docs/api/` holds the top-level API index. Generated output lands in `docs/api/dotnet/`; `docs/api/dotnet/index.md` is a placeholder with `unlisted: true` to prevent a build error when generated content is absent. Do not delete this placeholder. Sidebar convention: each section uses `link: {type: 'doc', id: 'section/index'}` to make the category label clickable; `items: []` while the section has no content pages, switching to `{type: 'autogenerated', dirName: 'section'}` once pages are added (Docusaurus excludes `index.md` from autogenerated child items automatically). |
| `.claude/commands/implement-language-feature.md` | Project-level Claude Code skill — invoked as `/implement-language-feature`. Guides full-stack implementation of a new language feature across all layers. |

---

## 2. Non-Obvious Constraints

A new session must know these before touching the relevant code.

**`Keywords` class scope**: `Keywords.cs` in `BWHazel.TopsyTurvy.Ast` covers only strings used in code logic across multiple projects — literals (`VERITY`, `NAY`, `NAUGHT`), type names (`PEER`, `FATHOM`, `YARN`, `DECREE`), and the implicit variable (`JUST SO`). Single-site parser grammar strings such as `Lexer.Keyword("BEHOLD")` are intentionally left inline. `KeywordData.Keywords` in Analysis remains the authoritative 76-entry list for LSP/completion and must not be duplicated in `Keywords.cs`.

**`SourceSpan` is a half-open interval**: `Start` is inclusive, `End` is exclusive — `End` points to the column one past the final character of the region, i.e. `[Start, End)`. This matches the LSP range convention used by the web editor. Confirmed by `FindDeclarationSpan` (`endColumn = startColumn + name.Length`) and the error-span construction in `TryParse` (`endColumn + 1`).

**PlaceholderSpan**: every AST node carries `Span = (0,0)-(0,0)` — source positions were never wired into the Superpower parser. LSP handlers recover definition positions by scanning raw source lines for `PRAY WELCOME <name>` and `IT IS MY DUTY TO PERFORM <name>`.

**`JUST SO` exclusion**: the implicit variable's name contains a space, so it cannot be matched with word-boundary regex. It is excluded from rename, references, and semantic token scanning, and is always added to `SymbolTable` manually.

**LSP stdio**: `.ClearProviders()` must be called before `.AddLanguageProtocolLogging()` when configuring OmniSharp logging. Without it, the default console logger writes to stdout and corrupts the JSON-RPC stream. Configured in `IncantationCommandBuilder.HandleIncantationAsync()`.

**`--stdio` hidden option**: `vscode-languageclient` unconditionally appends `--stdio` to the process args when `TransportKind.stdio` is used. The `incantation` command registers `--stdio` as a hidden `Option<bool>` so `System.CommandLine` accepts it silently. Without this, the CLI rejects it as an unrecognised argument and the LSP server fails to start.

**`OmniSharpServer` alias in `IncantationCommandBuilder`**: `BWHazel.TopsyTurvy.LanguageServer` is both a project namespace (imported via `using`) and the name of OmniSharp's static server class. The alias `using OmniSharpServer = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;` is required to disambiguate. Do not remove it.

**Bundled binary takes precedence**: `resolveCliPath` in `paths.ts` calls `fs.existsSync` on `bin/operetta` at activation time. Once `npm run prebuild:dotnet` has been run, the bundled binary is always used — re-run `npm run prebuild:dotnet` to refresh it, or delete `bin/` to force the dev-layout fallback.

**`DocumentStateManager`**: singleton; rebuilds `SymbolTable` only on a successful parse, preserving the last good table during syntax errors.

**`TryParse` reserved-word errors**: returns `ParseResult(program, errors)` with non-null `program` so the symbol table still builds while squiggle diagnostics are published. `Parse()` (used by the CLI) throws `TopsyTurvySyntaxException` instead.

**`SUMMON` call syntax**: `SUMMON name WITH arg1 AND arg2 IF YOU PLEASE.` / `SUMMON name WITH NOTHING IF YOU PLEASE.` — uses `WITH` as the argument separator, not `AND`.

**Blazor WASM timeout**: `CancellationTokenSource(TimeSpan)` relies on a timer callback that cannot fire while `Interpreter.Execute()` is blocking the single WASM thread. Set `ExecutionTimeout = TimeSpan.FromSeconds(10)` in `InterpreterExecutionOptions` instead — this stores a `DateTime` deadline checked by `CheckCancellation()` inside every loop body via `DateTime.UtcNow`.

**`PRAY ADMIT` path resolution**: `ExecuteImport` resolves relative import paths against the directory of the *importing* file, not the process CWD. `Interpreter.Execute()` accepts an optional `sourceFilePath` parameter; when `null` (e.g. Blazor WASM), the raw path from the AST is used unchanged.

**Web editor virtual FS resolver**: `Interpreter.Execute()` accepts an optional `Func<string, string?> fileResolver`. When supplied, `ExecuteImport` calls this delegate instead of `File.ReadAllText`, enabling `PRAY ADMIT` to resolve imports from the in-memory virtual filesystem. Import paths must be bare filenames — no directory prefix.

**Cross-document LSP awareness**: `DocumentStateManager.AllDocuments()` returns a snapshot of all open documents. Attempt lookup in the current document's `SymbolTable` first; on failure, iterate `AllDocuments()` skipping the current URI. For `DefinitionHandler`, the URI returned by the cross-file fallback must be used as `Location.Uri` in the response. Only `Function` kind symbols are pulled from other documents for hover, completion, and semantic tokens. `WorkspaceSymbolHandler` and `ReferencesHandler` aggregate across all documents unconditionally.

**Multi-word keyword completion**: `CompletionHandler` uses server-side phrase filtering, `FilterText = lastWord`, `InsertText = keyword[insertOffset..]`, `isIncomplete = true`, and a space trigger character to keep the list live as the user types through multi-word phrases.

**Web editor — terminal fit private API**: `fitTerminal` reads `term._core._renderService.dimensions.css.cell.height`. This is an internal xterm.js property — if the terminal stops filling its pane after an XtermBlazor upgrade, check this path first.

**Web editor — editor content API**: per-file content is stored in `VirtualFile.Content` and loaded/saved via `monacoEditor.SetValue` / `blazorMonaco.editor.getValue`. URI-based Monaco models were considered but abandoned — replacing BlazorMonaco's default model breaks the `onDidChangeModelContent` event system.

**Web editor — output download**: `Editor.razor` stores the last run's output in `lastRunOutput`. `DownloadTerminalAsync` uses this field directly rather than reading the xterm buffer. `lastRunOutput` is reset when the terminal is cleared via `ConfirmClearAsync`.

**Web editor — open file replace**: `OpenSingleFileAsync` handles the case where the opened file shares a name with an already-open tab. When the replaced file is the active tab, call `monacoEditor.SetValue(content)` directly — do not call `SwitchFileAsync`, which would re-sync old content back over `VirtualFile.Content`.

**`CONSERVATIVE` constant enforcement covers all three mutation paths**: `IS APPOINTED` (assignment), `IS HENCEFORTH A` (in-place cast), and `PRAY TELL` (input) all ultimately call `TopsyTurvyEnvironment.Assign`, which now checks `this.constants.Contains(name)` before allowing the write.  No additional guard in the interpreter is required for the cast and input paths — the constraint is enforced centrally in `Assign`.  The expression cast `AS IT WERE` does **not** need a guard because it produces a new value without mutating the source variable.

**`LITTLE LIST OF` in the parser, not `A LITTLE LIST OF`**: In the source text `AS A LITTLE LIST OF YARN`, the phrase `AS A` is consumed by `Lexer.Keyword("AS A")` — this matches exactly 4 characters `A-S-space-A`, positioning the cursor immediately after the final `A`. The remaining token stream therefore starts with ` LITTLE LIST OF`, not ` A LITTLE LIST OF`. The array-type combinator must match `Lexer.Keyword("LITTLE LIST OF")`. Using `Lexer.Keyword("A LITTLE LIST OF")` will fail to match because the `A` was already consumed. This applies in `StatementParser.ArrayDeclaration`, `ExpressionParser.ExpressionCast`, and anywhere else `AS A` is followed by an array type. `GRAMMAR.ebnf` carries an explanatory comment on the `ArrayDeclaration` production.

**Array element assignment bypasses `TopsyTurvyEnvironment.Assign`**: `VICTIM n ON arr IS APPOINTED val` mutates the `List<TopsyTurvyValue>` stored inside the array value directly — it does not call `Assign`. The standard CONSERVATIVE guard in `Assign` is therefore never triggered. Instead, `ExecuteArrayElementAssignment` calls `environment.IsConstantInChain(arrName)` (which walks the enclosing scope chain) before mutating the list. Do not rely on `Assign` for constant enforcement in array element mutations.

**Sized array size+BEING mutual exclusivity is a runtime error, not a parse error**: When `ArrayDeclarationNode.Size` is non-null and `InitialValues.Count > 0`, `ExecuteArrayDeclaration` throws a `TopsyTurvyRuntimeException`. The parser does not prevent this combination — it happily parses `PRAY WELCOME arr AS A LITTLE LIST OF 3 PEER BEING 1 AND 2 AND 3 IF YOU PLEASE.` into an `ArrayDeclarationNode` with both `Size = 3` and three `InitialValues`. The guard lives exclusively in the runtime. Similarly, a negative size is caught only at runtime.

**Array reference semantics**: `PRAY WELCOME alias AS A LITTLE LIST OF PEER` followed by `alias IS APPOINTED original` makes both variables point to the same `List<TopsyTurvyValue>` instance. Mutating an element via `alias` is visible via `original` and vice versa. There is no copy-on-assign. This is consistent with how .NET reference types work and is intentional — value semantics for arrays are deferred to a future struct/class type system.

**1-based array indexing and ascending loops**: `VICTIM 1 ON arr` accesses the first element. The ascending loop (`BY A LEGAL FICTION ASCENDING x UNTIL n`) resets its counter to 0 at the start of each iteration — using it to index into an array would start at `VICTIM 0`, which is out-of-range. Use a WHILST loop for array traversal: `count IS APPOINTED 1`, then `BY A LEGAL FICTION WHILST HARDLY EVER PRE-ADAMITE count AND length`, incrementing `count` manually inside the loop body. See `examples/language/arrays.topsy` for the canonical pattern.

**`AS IT WERE` standalone statement sets `JUST SO` via `ExpressionStatement`**: `AS IT WERE <expr> AS A <type>` is now an expression, not a dedicated statement.  When used as a standalone statement it is parsed as an `ExpressionStatement` wrapping the `ExpressionCastNode`.  The `ExpressionStatement` dispatch path in `Interpreter.Execute` sets `environment.JustSo` to the evaluated expression result.  There is no `ExecuteExpressionCast` method — cast evaluation is handled entirely inside `EvaluateExpression`.

**Try/catch inline expression**: `WITH THE GREATEST RESPECT, <expression>` requires the expression on the *same line* as the keyword. Writing it on the following line causes a parse failure. The formatter's indentation rules for this construct diverge from valid parser input by design.

**`MODIFIED RAPTURE` catch binding creates a child scope**: when `MODIFIED RAPTURE, Name` is used, the interpreter calls `environment.CreateNested()` and declares `Name` in that child scope.  The binding is inaccessible outside the exception block — accessing it after `THAT CONCLUDES THE MATTER.` produces a runtime error.  `JUST SO` is always set regardless of whether a binding is present (backward compatibility).  The comma delimiter (`MODIFIED RAPTURE, Name`) is required to avoid ambiguity with statement-starting identifiers (e.g. `result IS APPOINTED ...`) that the parser would otherwise greedily consume as the binding name.

**Web editor — MudTabs API (MudBlazor 9.x)**: `ScrollButtons` was renamed to `AlwaysShowScrollButtons`. `PanelClass` belongs on `MudTabPanel`, not `MudTabs`. Using the old names produces MUD0002 analyzer warnings.

**Web editor — auto-indent**: Monaco's `indentationRules` only take effect when `autoIndent` is `'full'` (numeric `4`). This is a construction-time option — set via `AutoIndent = "full"` in `StandaloneEditorConstructionOptions`; `updateOptions` has no effect on it.

**CLI version string**: `AssemblyInformationalVersionAttribute` = `{Version}+{SourceRevisionId}`. `PedigreeCommandBuilder` splits on `+` and holds `SpecVersion = "0.2.0"` as a private constant.

**`--tiptoe` output routing**: with `--tiptoe`, errors go to stderr and program output to stdout. Without it, all output — including error panels — goes to stdout via `AnsiConsole.Write`. E2E tests that assert on error messages must use `--tiptoe`.

**Spectre.Console panel header truncation in non-TTY mode**: when stdout is redirected, Spectre.Console truncates panel headers to fit the content width with a Unicode ellipsis (`…`). A short body (e.g. a short filename) produces a narrow panel. Workaround: use a long filename to widen the panel, or assert only the unambiguous prefix.

**SignatureHelp two-update pattern**: signature help tests for in-progress `SUMMON` calls (where `IF YOU PLEASE.` has not been typed yet) must seed `DocumentStateManager` with a successful parse before updating with the incomplete source, so the last-good symbol table is preserved and available to the handler.

---

## 3. LSP Handlers & CLI Commands

### LSP Handlers (`interpreter/BWHazel.TopsyTurvy.LanguageServer/` — class library, started via `operetta sorcerer incantation`)

| Handler | Purpose |
|---|---|
| `TextDocumentSyncHandler` | `didOpen`/`didChange`/`didSave`/`didClose`. Calls `TryParse`, publishes diagnostics, updates `DocumentStateManager`. |
| `HoverHandler` | Markdown tooltip for variables, functions, parameters, and `JUST SO`. |
| `DefinitionHandler` | Go-to-definition using `DefinitionLine` recovered from source scan. |
| `CompletionHandler` | Symbols + 74 keywords. Server-side phrase filtering for multi-word keywords. Known edge case: variable names that are keyword prefixes may trigger false keyword-context mode. |
| `SemanticTokensHandler` | Colours variables, parameters, functions by whole-word line scan. Emits the `readonly` modifier for `CONSERVATIVE` constant variables, mapping to the `variable.other.constant` TextMate scope via `semanticTokenScopes` in `package.json`. Excludes `JUST SO`. |
| `RenameHandler` | Whole-word case-insensitive rename; skips comments and strings. Single-file only. |
| `PrepareRenameHandler` | Pre-validates rename targets; returns placeholder range for input box. |
| `DocumentSymbolHandler` | Populates VS Code Outline panel from `SymbolTable`. |
| `ReferencesHandler` | Find All References; respects `IncludeDeclaration`. |
| `SignatureHelpHandler` | Parameter hints inside `SUMMON` calls; counts `AND` tokens for active parameter. |
| `FoldingRangeHandler` | Stack-based fold regions for all block constructs and comments. |
| `DocumentFormattingHandler` | Normalises keyword casing (74 keywords) and applies 2-space libretto indentation. |
| `CodeLensHandler` | Inline reference-count annotations above declarations; links to Find All References. |
| `WorkspaceSymbolHandler` | Handles `workspace/symbol` requests; searches all open documents by query string. |

### CLI Commands (`interpreter/BWHazel.TopsyTurvy.Cli/CommandBuilders/`)

| Command | Alias(es) | Options | Notes |
|---|---|---|---|
| `perform <file>` | `stage`, `run` | `--tiptoe` | Runs a `.topsy` file. |
| `mount <project>` | `init` | `--title`, `--or`, `--hollow`, `--tiptoe` | Creates a project directory. Without `--hollow`, also scaffolds `<project>/<project>.topsy`. |
| `commission <file>` | `new` | `--title`, `--or`, `--tiptoe` | Scaffolds a Hello World `.topsy` file. UTF-8, no BOM. Both commands delegate content to `FileManager.BuildFileContent`. |
| `rehearse <file>` | `check` | `--tiptoe` | Syntax-checks without executing via `ProgramRunner.Check`. |
| `sorcerer promptbook <file>` | `dev ast` | `--abridged`, `--chromatic`, `--tiptoe` | Prints AST as JSON via `NodeJsonConverter`. |
| `sorcerer incantation` | `dev lsp` | `--stdio` (hidden) | Starts the OmniSharp LSP server over stdio. Invoked by the VS Code extension. |
| `pedigree` | `info` | `--tiptoe` | Shows CLI version, commit SHA, and language spec version. |

---

## 4. Known Gaps

**`PrincipalBlockNode.Declarations` uses a weak base type instead of a discriminated union**: `Declarations` is typed as `IReadOnlyList<Statement>` rather than a union of `DeclarationNode | ArrayDeclarationNode`, because C# does not yet provide first-class discriminated-union syntax. Callers must pattern-match at runtime to determine the concrete type. This will be revisited when discriminated unions land in C# / .NET 11.

**Ascending loop always starts from 0 (spec-level limitation)**: `ASCENDING <var>` resets the loop variable to `0` at the start of every loop per the spec (`<var>` begins at `0`). There is no syntax to specify a different starting value. Descending loops have no such constraint — they use the variable's current declared value. Fix requires a spec change (remove the "begins at `0`" constraint, or add `FROM <n>` syntax) and removing the hardcoded `Assign(..., Integer(0))` in `ExecuteAscendingLoop`.

**Monarch tokenizer keyword duplication**: the keyword list appears in `topsy-turvy-language.js` (Monarch tokenizer) and `topsy-turvy.tmLanguage.json` (TextMate grammar). The JS and TextMate copies cannot consume the C# library and remain separate. When adding a language feature, update both in the same change.

**Web editor interactive stdin (deferred)**: `BufferedWebIO` reads from a pre-supplied `Queue<string>`. True interactive stdin requires making `ITopsyTurvyIO` async — a new `InteractiveWebIO` implementation and an XtermBlazor input handler.

**LSP squiggle accuracy (deferred)**: syntax errors inside block constructs land on the opening line rather than the error line. Root cause: `.Try().Many()` in `ProgramParser.body` resets Superpower's error position. Fixing requires a custom error-recovery loop — significant parser refactoring.

**Keyword-as-name squiggle precision**: diagnostic spans are recovered by source-line scanning as a workaround for PlaceholderSpan. Replaceable once AST position wiring is added.

**`ValidateSymbolNames` only checks top-level statements**: `TopsyTurvyParser.ValidateSymbolNames` walks only the top-level statement list of `ProgramNode`. Declarations nested inside function bodies, conditionals, loops, or try-catch blocks are not validated — a variable declared with a reserved-keyword name inside a nested block will not produce a diagnostic. Note that the parser may independently reject some of these cases (reserved words that cannot be lexed as identifiers), but the semantic check is incomplete by design. Fix requires recursing into all block-bearing statement types (`FunctionDefinitionNode`, `ConditionalNode`, `LoopNode`, `TryCatchNode`, etc.) or introducing an AST visitor.

**Web editor JS not linted or formatted**: `topsy-turvy-language.js` and `web-editor.js` have no ESLint or Prettier configuration.

**VS Code extension file icon**: the `icon` field on the `languages` contribution is only shown when no file icon theme is active. Long-term fix: submit a PR to `vscode-icons` adding a `.topsy` entry.

**`<example>` must be nested inside `<remarks>` for dotnetmd rendering**: the standard XML doc convention places `<example>` as a top-level sibling of `<summary>`, but dotnetmd only renders examples when `<example>` is nested inside `<remarks>`. Methods with no `<param>` tags (e.g. `TopsyTurvyValue.Null()`) and `override` methods (e.g. `TopsyTurvyValue.ToString()`) are additionally prone to their documentation being silently dropped. The non-standard placement must be maintained until dotnetmd is replaced or this behaviour changes upstream.

**`dotnetmd` cannot document LSP handlers and has additional rendering defects**: `dotnetmd` silently skips any class whose base type cannot be resolved from locally available XML documentation files. All Language Server handlers inherit from OmniSharp base classes (e.g. `HoverHandlerBase`, `CodeLensHandlerBase`) in NuGet packages that do not ship XML docs, so `dotnetmd` drops all handler pages without warning — only `DocumentStateManager` (base type `Object`) and other non-handler types survive. Additional defects: `<c>` inline code tags are not converted to Markdown backticks (rendered as literal `<c>text</c>` in output); `<remarks>` content is run together without line breaks. `DefaultDocumentation` (`scripts/generate-api-docs.sh`) is a working alternative: it documents all public types including handlers and processes the full XML doc set correctly. However, switching requires migrating all XML documentation comments from the project's current inline Markdown style (raw `*` bullet syntax, backtick spans, and other Markdown embedded within `<remarks>`) to standard .NET XML doc tags (`<list type="bullet"><item>`, `<c>`, `<para>`, etc.), because `DefaultDocumentation` treats XML node content as plain text rather than Markdown. Namespace title formatting in `DefaultDocumentation` output also differs from the current Docusaurus page structure and would require post-processing refinement before the generated pages are publication-ready.

**AST source positions not wired into the parser (PlaceholderSpan)**: every AST node carries `Span = (0,0)-(0,0)`. Definition positions for hover, go-to-definition, and the symbol table are recovered by scanning raw source lines for `PRAY WELCOME <name>` and `IT IS MY DUTY TO PERFORM <name>` (`FindDefinitionLine` in `SymbolTable`; `FindDeclarationSpan` in LSP handlers). Fix requires wiring line/column into the Superpower parser at each parse site; once done, `node.Span` replaces all source-scan workarounds and `FindDefinitionLine` becomes redundant.

**`SymbolTable` uses a flat namespace — parameters from different functions collide**: `SymbolTable` is a single `Dictionary<string, SymbolInfo>` (case-insensitive). If two functions declare a parameter with the same name (e.g. both have a parameter called `name`), only the first is kept; the second is silently dropped by the `ContainsKey` guard in `AddFunction`. Consequences: hover, go-to-definition, and rename all use the first function's definition position for any subsequently declared parameter of the same name, and Find All References aggregates occurrences from both functions under one symbol. Fix requires scoped symbol tables — a global scope for variables and functions, plus a per-function scope for parameters — and updates to all LSP handlers and web editor bridge methods that call `TryGetSymbol`.

### Deferred LSP Features

| Feature | Blocker |
|---|---|
| Inlay Hints (`textDocument/inlayHint`) | Handler not yet written; `SymbolInfo.TypeDisplayName` already carries the data. |
| Call Hierarchy | Requires a call-graph not currently built by `SymbolTable`. |
| Hierarchical `DocumentSymbol` (`textDocument/documentSymbol`) | `DocumentSymbolHandler` currently returns flat `SymbolInformation` (the older LSP format), which lists every symbol at the same level in the Outline panel. The newer `DocumentSymbol` format supports nesting — parameters under their parent function, variables under the PRINCIPALS block. Upgrading requires building a tree during `SymbolTable.Build` and returning `DocumentSymbol` instances instead of wrapping `SymbolInformation` in `SymbolInformationOrDocumentSymbol`. The flat format is used because the current `SymbolTable` has no hierarchy, and the flat namespace Known Gap (parameter name collision) would need to be resolved first. |

### Deferred Web Editor Features

| Feature | Blocker |
|---|---|
| Interactive stdin (line-by-line `PRAY TELL`) | `ITopsyTurvyIO` must become async; requires `InteractiveWebIO` + XtermBlazor input handler |

---

## 5. Next Steps

1. Read `AGENTS.md` and `DEVELOPMENT.md` before starting any work.
2. Run `dotnet build interpreter/BWHazel.TopsyTurvy.slnx --no-incremental` and `dotnet test` to confirm baseline.
3. **VS Code Marketplace publication**: register a publisher at `marketplace.visualstudio.com/manage`, confirm `publisher` in `package.json` matches, then `vsce publish` from the publish workflow or locally.
4. **VS Code file icon**: submit a PR to `vscode-icons` adding a `.topsy` entry — see §4 Known Gaps.
5. **LSP squiggle accuracy** (deferred — see §4).
