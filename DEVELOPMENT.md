# DEVELOPMENT.md
## Topsy Turvy Language — Technical Reference for Coding Agents

* **Last Updated:** 2026-05-29 (session 2)
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
| Web editor (`BWHazel.TopsyTurvy.WebEditor`) | Complete (Blazor WASM) |

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
| `interpreter/BWHazel.TopsyTurvy.Runtime/` | Tree-walking interpreter. Entry: `Interpreter`. Key types: `TopsyTurvyValue`, `TopsyTurvyEnvironment`, `ITopsyTurvyIO`/`ConsoleIO`. Signal exceptions: `BreakSignalException`, `ReturnSignalException`, `ContinueSignal`, `TopsyTurvyThrowException`. `Execute(program, cancellationToken, maxDuration?)` — pass `maxDuration` for Blazor WASM timeout (see §2). |
| `interpreter/BWHazel.TopsyTurvy.Tests/` | xUnit suite — 17/17 passing. |
| `interpreter/BWHazel.TopsyTurvy.Cli/` | CLI project. Binary: `operetta`. Version: `0.1.0`. Dependencies: `Spectre.Console 0.55.2`, `Spectre.Console.Json 0.55.2`, `System.CommandLine 2.0.8`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/ProgramExecutionResult.cs` | Discriminated result: `Success()`, `Failure(msg)`, `SyntaxError(errors)`, `RuntimeError(diagnostics)`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/ProgramRunner.cs` | `Run`, `Check`, `ParseFile` — all delegate to private `TryReadSource`. `ParseFile` returns `(ProgramExecutionResult, ParseResult?)`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/PanelHelper.cs` | All Spectre.Console panel construction. `WriteDefault`, `WriteSuccess`, `WriteUserError`, `WriteSyntaxErrors`, `WriteRuntimeErrors`, `WriteVersionInfo`, `ReportErrors`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/NodeJsonConverter.cs` | `JsonConverter<Node>` with `CanConvert` override for polymorphic AST serialisation with `$type` discriminators. |
| `interpreter/BWHazel.TopsyTurvy.Cli/CommandBuilders/` | One builder per command — see §3. |
| `interpreter/BWHazel.TopsyTurvy.LanguageServer/` | OmniSharp LSP server — see §3. |
| `extensions/vscode/topsy-turvy/` | VS Code extension: LSP client, TextMate grammar, run/stop commands, panel and file icons. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/` | Blazor WASM standalone web editor. `MudToolBar` + `MudSpacer` toolbar with G&S-themed button labels toggle (`areGsLabelsEnabled`). `MudTabs` tab bar with per-file Monaco models and close/add buttons. BlazorMonaco editor with Monarch tokenizer and inline diagnostics. XtermBlazor output terminal sized dynamically via `ResizeObserver`. Pre-supplied stdin textarea. 10-second execution timeout. ZIP export of all open files. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/IO/BufferedWebIO.cs` | `ITopsyTurvyIO` implementation for Blazor WASM. Stdin from a pre-populated `Queue<string>`; output lines collected in a `List<string>` (`OutputLines`) for post-execution rendering and download. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/IO/VirtualFile.cs` | Record tracking a filename in the in-memory virtual file system. Monaco editor models are the source of truth for content; `VirtualFile` is the file registry used for tab management and ZIP export. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/Dialogs/NewFileDialog.razor` | `MudDialog` with a text field for entering a new file name. `.topsy` is appended automatically if omitted. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/wwwroot/js/topsy-turvy-language.js` | Monaco Monarch tokenizer registration, keyword completion provider, and language configuration (auto-closing quotes, auto-indent/de-indent rules, comment toggling). Defines `window.topsyTurvy` with `registerLanguage()`. Extended by `web-editor.js`. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/wwwroot/js/web-editor.js` | All Monaco/xterm JS interop except `registerLanguage`. Extends `window.topsyTurvy` via `Object.assign`. Functions: `applyLanguageToEditor` (sets language on existing model, sets `autoIndent: 'full'`), `setModelMarkers` (takes editor ID), `openFile` (returns `{name, content}`), `downloadText`, `downloadZip`, `setupTerminalFit`, `fitTerminal`. |

---

## 2. Non-Obvious Constraints

A new session must know these before touching the relevant code.

**PlaceholderSpan**: every AST node carries `Span = (0,0)-(0,0)` — source positions were never wired into the Superpower parser. LSP handlers recover definition positions by scanning raw source lines for `PRAY WELCOME <name>` and `IT IS MY DUTY TO PERFORM <name>`. Once position wiring is added to the parser, `FindDeclarationSpan` can be replaced with `node.Span`.

**`JUST SO` exclusion**: the implicit variable's name contains a space, so it cannot be matched with word-boundary regex. It is excluded from rename, references, and semantic token scanning, and is always added to `SymbolTable` manually.

**LSP stdio**: `.ClearProviders()` must be called before `.AddLanguageProtocolLogging()` in the LSP `Program.cs`. Without it, the default console logger writes to stdout and corrupts the JSON-RPC stream.

**`DocumentStateManager`**: singleton; rebuilds `SymbolTable` only on a successful parse, preserving the last good table during syntax errors.

**`TryParse` reserved-word errors**: returns `ParseResult(program, errors)` with non-null `program` so the symbol table still builds while squiggle diagnostics are published. `Parse()` (used by the CLI) throws `TopsyTurvySyntaxException` instead.

**`SUMMON` call syntax**: `SUMMON name WITH arg1 AND arg2 IF YOU PLEASE.` / `SUMMON name WITH NOTHING IF YOU PLEASE.` — uses `WITH` as the argument separator, not `AND`.

**Blazor WASM timeout**: `CancellationTokenSource(TimeSpan)` relies on a timer callback that cannot fire while `Interpreter.Execute()` is blocking the single WASM thread. Pass `maxDuration: TimeSpan.FromSeconds(10)` to `Execute()` instead — this stores a `DateTime` deadline checked by `CheckCancellation()` inside every loop body via `DateTime.UtcNow`, which works synchronously without requiring a thread yield.

**`PRAY ADMIT` path resolution**: `ExecuteImport` resolves relative import paths against the directory of the *importing* file, not the process CWD. `Interpreter.Execute()` accepts an optional `sourceFilePath` parameter; when supplied, it computes `sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath))` which is then used in `ExecuteImport`. `ProgramRunner.Run` passes `filePath` to `Execute`. When executing from a string without a backing file (e.g. Blazor WASM), `sourceFilePath` is `null` and the raw path from the AST is used unchanged. `TopsyTurvySyntaxException` thrown by parsing an imported file is caught inside `ExecuteImport` and re-thrown as `TopsyTurvyRuntimeException` so it is handled by the normal error pipeline.

**Web editor virtual FS resolver**: `Interpreter.Execute()` also accepts an optional `Func<string, string?> fileResolver` parameter. When supplied, `ExecuteImport` calls this delegate instead of `File.ReadAllText` — enabling `PRAY ADMIT` to resolve imports from the in-memory virtual filesystem. The web editor builds a `Dictionary<string, string>` snapshot of all open Monaco models just before execution and passes `name => snapshot.GetValueOrDefault(name)` as the resolver. Import paths in `.topsy` source must be bare filenames (e.g. `"utils.topsy"`) — no directory prefix. CLI and tests pass no `fileResolver` and are unaffected.

**Cross-document LSP awareness**: `DocumentStateManager.AllDocuments()` returns a snapshot of all open documents. Every handler that needs cross-file visibility calls this method — do not add new per-document-only lookups for symbols. The established pattern is: attempt lookup in the current document's `SymbolTable` first; on failure, iterate `AllDocuments()` skipping the current URI and search each other document's table. For `DefinitionHandler`, the URI returned by this fallback must be used as the `Location.Uri` in the response (not `request.TextDocument.Uri`). For semantic tokens, hover, and completion, only `Function` kind symbols are pulled from other documents (variables and parameters are not importable). `WorkspaceSymbolHandler` and `ReferencesHandler` always aggregate across all open documents unconditionally.

**Multi-word keyword completion**: `CompletionHandler` uses server-side phrase filtering, `FilterText = lastWord`, `InsertText = keyword[insertOffset..]`, `isIncomplete = true`, and a space trigger character to keep the list live as the user types through multi-word phrases.

**Web editor — terminal fit private API**: `fitTerminal` reads `term._core._renderService.dimensions.css.cell.height` to obtain the exact rendered cell height. This is an internal xterm.js property with no public equivalent and may break on xterm version upgrades. The fallback chain is: DOM measurement of `.xterm-rows > div`, then `ceil(fontSize * lineHeight)`. If the terminal stops filling its pane after an XtermBlazor upgrade, check this path first.

**Web editor — editor content API**: the editor uses a single Monaco editor instance. Per-file content is stored in `VirtualFile.Content` and loaded/saved via `monacoEditor.SetValue` / `blazorMonaco.editor.getValue`. URI-based Monaco models were considered but abandoned — replacing BlazorMonaco's default model breaks the `onDidChangeModelContent` event system. Do not attempt to switch to URI-based models without first verifying BlazorMonaco event compatibility.

**Web editor — output download**: `Editor.razor` stores the last run's output in `lastRunOutput` (a `string` field, `string.Join('\n', io.OutputLines)`). `DownloadTerminalAsync` uses this field directly rather than reading the xterm terminal buffer. `lastRunOutput` is reset to `string.Empty` when the terminal is cleared via `ConfirmClearAsync`.

**Web editor — open file replace**: `OpenFileAsync` handles the case where the opened file shares a name with an already-open tab. When the file being replaced is the active tab, `SwitchFileAsync` must not be called — it would re-sync the old Monaco content back over `VirtualFile.Content`, undoing the replacement. Instead, call `monacoEditor.SetValue(content)` directly. `SwitchFileAsync` is only safe to call when the replaced file is not the active tab.

**Web editor — MudTabs API (MudBlazor 9.x)**: `ScrollButtons` was renamed to `AlwaysShowScrollButtons` on `MudTabs`. `PanelClass` belongs on `MudTabPanel`, not `MudTabs`. Using the old names produces MUD0002 analyzer warnings.

**Web editor — auto-indent**: Monaco's `indentationRules` (defined in `setLanguageConfiguration`) only take effect when the editor's `autoIndent` option is set to `'full'` (numeric `4`). This is a construction-time option; `updateOptions` has no effect on it. Set it via `AutoIndent = "full"` in `StandaloneEditorConstructionOptions` in `GetEditorOptions`.

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
| `WorkspaceSymbolHandler` | Handles `workspace/symbol` requests; searches all open documents by query string. |

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

**Monarch tokenizer keyword duplication**: the keyword list appears in three places: `topsy-turvy-language.js` (Monarch tokenizer + completion provider), `extensions/vscode/topsy-turvy/syntaxes/topsy-turvy.tmLanguage.json` (TextMate grammar), and `CompletionHandler.cs` (LSP). A future `BWHazel.TopsyTurvy.Analysis` shared project would consolidate these.

**Web editor hover / symbol completion / go-to-definition (deferred)**: these features require analysis logic (hover text, symbol enumeration, definition line scanning) currently embedded in the LSP handler classes. Implementing them in the web editor without the `BWHazel.TopsyTurvy.Analysis` refactor would duplicate that logic. The architecture is: (1) extract analysis logic into a shared `BWHazel.TopsyTurvy.Analysis` project; (2) add `[JSInvokable]` methods on `Editor.razor` backed by that shared project; (3) register Monaco hover/completion/definition providers in JS that call `DotNetObjectReference.invokeMethodAsync`. None of these features are blocked by PlaceholderSpan — hover and symbol completion only need symbol names and types, and go-to-definition can use the same source-line scanning workaround already in `DefinitionHandler.cs`. Hover was never present in the web editor; it only exists in the VS Code extension via LSP.

**Web editor interactive stdin (deferred)**: `BufferedWebIO` reads from a pre-supplied `Queue<string>` drawn from a textarea. True line-by-line interactive stdin (prompting after each `PRAY TELL`) requires making `ITopsyTurvyIO` async — straightforward future step with the same interface, a new `InteractiveWebIO` implementation, and an XtermBlazor input handler.

**LSP squiggle accuracy (deferred)**: when a syntax error appears inside a block construct (function, loop, conditional, switch, try/catch), the squiggle lands on the opening line rather than the error line. Root cause: `Ws(Statement).Try()` in `ProgramParser.body` resets Superpower's error position before the entire construct on failure. Fixing this requires replacing `.Try().Many()` with a custom error-recovery loop — significant parser refactoring, deferred.

**Keyword-as-name squiggle precision**: diagnostic spans are recovered by source-line scanning (`FindDeclarationSpan`) as a workaround for PlaceholderSpan. Replaceable once AST position wiring is added.

### Deferred LSP Features

| Feature | Blocker |
|---|---|
| Inlay Hints (`textDocument/inlayHint`) | Handler not yet written; `SymbolInfo.TypeDisplayName` already carries the data. |
| Call Hierarchy | Requires a call-graph not currently built by `SymbolTable`. |

### Deferred Web Editor Features

| Feature | Blocker |
|---|---|
| Hover tooltips | `BWHazel.TopsyTurvy.Analysis` refactor + `[JSInvokable]` bridge |
| Symbol completion (variables, functions, parameters) | `BWHazel.TopsyTurvy.Analysis` refactor + `[JSInvokable]` bridge |
| Go-to-definition | `BWHazel.TopsyTurvy.Analysis` refactor + `[JSInvokable]` bridge (PlaceholderSpan is not a blocker — line-scanning workaround suffices) |
| Interactive stdin (line-by-line `PRAY TELL`) | `ITopsyTurvyIO` must become async; requires `InteractiveWebIO` + XtermBlazor input handler |

---

## 5. Next Steps

1. Read `AGENTS.md` and `DEVELOPMENT.md` before starting any work.
2. Run `dotnet build interpreter/BWHazel.TopsyTurvy.slnx` and `dotnet test` to confirm baseline.
3. **LSP server packaging**: the default binary path only works from the dev repo layout and cannot be distributed as a `.vsix`. Proposed: add `dotnet publish` to the extension build script outputting to `extensions/vscode/topsy-turvy/server/`; update the default path in `extension.ts`; use `--no-self-contained` as the starting point.
4. **LSP squiggle accuracy** (deferred — see §4).
5. **`BWHazel.TopsyTurvy.Analysis` refactor**: extract hover, completion, and definition logic from LSP handlers into a shared project. This is the prerequisite for web editor hover/symbol completion/go-to-definition and will also eliminate the keyword triplification (see §4).
