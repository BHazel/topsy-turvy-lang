# DEVELOPMENT.md
## Topsy Turvy Language — Technical Reference for Coding Agents

* **Last Updated:** 2026-06-02 (tech review continued: Lexer, SourceMap, and PreProcessorPipeline tests added; test stub classes extracted; XML doc and test conventions codified)
* **Specification Version:** 0.2.0 — `SPEC.md` is the grammar source of truth
* **File Extension:** `.topsy`
* **Baseline:** `dotnet build interpreter/BWHazel.TopsyTurvy.slnx --no-incremental` — 0 errors, 0 warnings. `dotnet test` — 261/261.

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
| `SPEC.md` | Authoritative language specification v0.2.0. |
| `GRAMMAR.ebnf` | Formal EBNF grammar — blueprint for the parser. |
| `examples/` | `hello_world.topsy`, `fizzbuzz.topsy`, `fibonacci.topsy`, `pirates_calculator.topsy` — all execute correctly. |
| `interpreter/BWHazel.TopsyTurvy.slnx` | Solution file. |
| `interpreter/BWHazel.TopsyTurvy.Ast/` | AST node types. All inherit from `Node` (`required SourceSpan Span`). `Diagnostic`, `DiagnosticCollection`, `SourceSpan`, `SourceLocation`. |
| `interpreter/BWHazel.TopsyTurvy.Analysis/` | Shared analysis library. `SymbolTable`, `SymbolInfo`, `SymbolKind`, `DocumentState`, `DepthAction` — moved from LanguageServer. `SourceFormatter` — keyword normalisation + libretto indentation. `SourceAnalyser` — `FindSkipRanges`, `BuildLineOffsets`, `IsInSkipRange`, `IsIdentifierChar`, `CountOccurrences`. `KeywordData` — 74-entry keyword list. `HoverMarkdownBuilder` — Markdown hover strings. References: Ast, Parser. Referenced by: LanguageServer, Cli, WebEditor. |
| `interpreter/BWHazel.TopsyTurvy.Parser/` | Superpower parser. Entry: `TopsyTurvyParser`. Pre-processors: `CommentsPreProcessor`, `VictorianFlourishPreProcessor`. `SourceMap`/`SourceMapping` for offset tracking. `TryParse` returns `ParseResult(ProgramNode?, IReadOnlyList<Diagnostic>)`. |
| `interpreter/BWHazel.TopsyTurvy.Runtime/` | Tree-walking interpreter. Entry: `Interpreter`. Key types: `TopsyTurvyValue`, `TopsyTurvyEnvironment`, `ITopsyTurvyIO`/`ConsoleIO`. Signal exceptions: `BreakSignalException`, `ReturnSignalException`, `ContinueSignal`, `TopsyTurvyThrowException`. `Execute(program, cancellationToken, maxDuration?)` — pass `maxDuration` for Blazor WASM timeout (see §2). |
| `interpreter/BWHazel.TopsyTurvy.Tests/` | xUnit suite — 261/261 passing. `Nullable: enable`, `ImplicitUsings: disable` (xUnit's `Xunit` namespace is brought in via `<Using Include="Xunit" />`; all other usings are explicit). References: Analysis, Parser, Runtime. Tests are organised into three subdirectories mirroring the project they cover: `Analysis/` (SymbolTable, SourceAnalyser, SourceFormatter, KeywordData, HoverMarkdownBuilder); `Parser/` (TopsyTurvyParser, TopsyTurvyParserStatement, TopsyTurvyParserExpression, CommentsPreProcessor, VictorianFlourishPreProcessor, Lexer, SourceMap, PreProcessorPipeline); `Runtime/` (TopsyTurvyInterpreter). The AST project has no dedicated tests — it is exclusively data types with no non-trivial logic. Test stubs and helpers live in the same subdirectory as the tests that use them, in their own files named with a `Test` prefix (e.g. `TestPrefixingPreProcessor.cs`, `TestIO.cs`), declared `internal sealed`. `Superpower.Model` types (`TextSpan`, `Result<T>`) are available transitively via the Parser project reference — no direct Superpower package reference is needed in the test project. **Lexer coverage note**: `Lexer.Whitespace`, `Lexer.WhitespaceRequired`, and `Lexer.IntegerLiteral` are intentionally not tested — each is a single-line delegation to a Superpower library primitive with no custom logic. All other `Lexer` members have direct combinator tests in `LexerTests.cs`. **Test conventions**: (1) every test method XML summary follows `Tests that the <see cref="X.Y"/> method/parser/property ...`; (2) Theory method parameters are always documented with `<param>`; (3) test bodies use a blank line between each of Arrange, Act, and Assert; (4) no inline `//` comments inside test bodies — the XML summary must be descriptive enough. |
| `interpreter/BWHazel.TopsyTurvy.Cli/` | CLI project. Binary: `operetta`. Version: `0.1.0`. Dependencies: `Spectre.Console 0.55.2`, `Spectre.Console.Json 0.55.2`, `System.CommandLine 2.0.8`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/ProgramExecutionResult.cs` | Discriminated result: `Success()`, `Failure(msg)`, `SyntaxError(errors)`, `RuntimeError(diagnostics)`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/ProgramRunner.cs` | `Run`, `Check`, `ParseFile` — all delegate to private `TryReadSource`. `ParseFile` returns `(ProgramExecutionResult, ParseResult?)`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/PanelHelper.cs` | All Spectre.Console panel construction. `WriteDefault`, `WriteSuccess`, `WriteUserError`, `WriteSyntaxErrors`, `WriteRuntimeErrors`, `WriteVersionInfo`, `ReportErrors`. |
| `interpreter/BWHazel.TopsyTurvy.Cli/NodeJsonConverter.cs` | `JsonConverter<Node>` with `CanConvert` override for polymorphic AST serialisation with `$type` discriminators. |
| `interpreter/BWHazel.TopsyTurvy.Cli/FileManager.cs` | Shared file-creation utilities. `BuildFileContent(title, subtitle?)` builds scaffold source text. `Utf8NoBom` is the shared `UTF8Encoding` instance used by all file-writing commands. `DefaultProgrammeTitle` (`"Programme"`) is the single source of truth for the default title used in both `--title` option descriptions and null-coalescing in command handlers. |
| `interpreter/BWHazel.TopsyTurvy.Cli/CommandBuilders/` | One builder per command — see §3. All scaffold file content goes through `FileManager.BuildFileContent`; no duplication between command builders. |
| `interpreter/BWHazel.TopsyTurvy.LanguageServer/` | OmniSharp LSP server — see §3. |
| `extensions/vscode/topsy-turvy/` | VS Code extension: LSP client, TextMate grammar, run/stop commands, panel and file icons. Two configurable paths in `extension.ts`: `topsy-turvy.serverPath` (LSP server, defaults to `BWHazel.TopsyTurvy.LanguageServer/bin/.../BWHazel.TopsyTurvy.LanguageServer`) and `topsy-turvy.cliPath` (CLI for run/stop commands, resolved via `resolveTopsyTurvyCliPath` to `BWHazel.TopsyTurvy.Cli/bin/.../operetta` or `operetta.exe` on Windows). Both paths fall back to their defaults when the setting is empty. **Tooling**: Prettier (`^3.5.3`) and ESLint (`^9.39.3`) configured — `npm run lint` (ESLint with `typescript-eslint` + `eslint-config-prettier`), `npm run format` / `npm run format:check` (Prettier, 4-space indent, single quotes, trailing commas). `.prettierrc` and `.prettierignore` are excluded from the packaged `.vsix` via `.vscodeignore`. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/` | Blazor WASM standalone web editor. `MudToolBar` + `MudSpacer` toolbar: left group — New Project/Mount (`CreateNewFolder`), New File/Commission (`InsertDriveFile`), Open/Recall (`FolderOpen`), Save/Pen (`Save`); centre — Run/Perform; right group — Clear Output, Download Output, divider, Light Mode/Dark Mode toggle, G&S Labels switch. All buttons are labelled `MudButton` with `StartIcon`. G&S-themed label toggle (`areGsLabelsEnabled`). `MudTabs` tab bar (no embedded add button — New File is in the toolbar). BlazorMonaco editor with Monarch tokenizer, inline diagnostics, hover tooltips, symbol completion, and go-to-definition. XtermBlazor output terminal sized dynamically via `ResizeObserver`. Pre-supplied stdin textarea. 10-second execution timeout. ZIP export of all open files. Multi-file open via `OpenFilesDialog` (drag-and-drop + browse). **Intelligence bridge**: `Editor` implements `IDisposable`; `dotNetRef` (`DotNetObjectReference<Editor>`) created on first render and passed to `topsyTurvy.registerHoverProvider`, `topsyTurvy.registerSymbolCompletionProvider`, and `topsyTurvy.registerDefinitionProvider`. Per-file symbol state is held in `Dictionary<string, SymbolTable> symbolTables` and `Dictionary<string, string> parsedSources` (keyed by file name); `ActiveSymbolTable` and `ActiveParsedSource` expose the active file's entries. `BuildSymbolTableForFile(fileName, source)` populates the dictionaries eagerly when files are opened and is also called by `UpdateDiagnosticsAsync` (debounced on edit) for the active file. Entries are removed on file close and cleared on new project. All three bridge methods search the active file's table first, then fall back to other files' tables for cross-file awareness. **Hover**: `[JSInvokable] GetHoverMarkdown(line, column)` — returns `HoverMarkdownBuilder.Build(info)` for the symbol at the 0-indexed cursor position, or `null`. **Symbol completion**: `[JSInvokable] GetSymbolCompletions()` — returns all symbols (name, kind, detail) across all open files, deduped by name, excluding `JUST SO`; the JS provider uses `model.getWordUntilPosition` for the insert range and maps kind strings to `CompletionItemKind`. **Go-to-definition**: `[JSInvokable] GetDefinitionLocation(line, column)` — returns `{ line, column, fileName }` (line/column 1-indexed); `fileName` is `null` for same-file hits or the target file name for cross-file hits. When `fileName` is set, the JS provider calls `[JSInvokable] SwitchToFileForDefinitionAsync(fileName)` before returning the Monaco location, switching the active tab so Monaco's cursor jump lands in the correct file. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/IO/BufferedWebIO.cs` | `ITopsyTurvyIO` implementation for Blazor WASM. Stdin from a pre-populated `Queue<string>`; output lines collected in a `List<string>` (`OutputLines`) for post-execution rendering and download. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/IO/VirtualFile.cs` | Record tracking a filename in the in-memory virtual file system. Monaco editor models are the source of truth for content; `VirtualFile` is the file registry used for tab management and ZIP export. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/Dialogs/NewFileDialog.razor` | `MudDialog` with a text field for entering a new file name. `.topsy` is appended automatically if omitted. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/Dialogs/OpenFilesDialog.razor` | `MudDialog` for opening multiple `.topsy` files. Drop zone uses a transparent `InputFile` overlay (`open-files-input`) on a styled div (`open-files-drop-zone`) — supports both click-to-browse and drag-and-drop. Returns `IReadOnlyList<IBrowserFile>` via `DialogResult`. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/wwwroot/js/topsy-turvy-language.js` | Monaco Monarch tokenizer registration, keyword completion provider, and language configuration (auto-closing quotes, auto-indent/de-indent rules, comment toggling). Defines `window.topsyTurvy` with `registerLanguage()`. Extended by `web-editor.js`. |
| `interpreter/BWHazel.TopsyTurvy.WebEditor/wwwroot/js/web-editor.js` | All Monaco/xterm JS interop except `registerLanguage`. Extends `window.topsyTurvy` via `Object.assign`. Functions: `applyLanguageToEditor` (sets language on existing model, sets `autoIndent: 'full'`), `setModelMarkers` (takes editor ID), `downloadText`, `downloadZip`, `setupTerminalFit`, `fitTerminal`, `registerHoverProvider` (Monaco hover → `GetHoverMarkdown`), `registerSymbolCompletionProvider` (Monaco completion → `GetSymbolCompletions`), `registerDefinitionProvider` (Monaco definition → `GetDefinitionLocation`; calls `SwitchToFileForDefinitionAsync` and waits 50 ms before returning the location when the definition is in another file). File opening is handled by `OpenFilesDialog.razor` via Blazor's `InputFile` — no JS required. |

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

**Web editor — open file replace**: `OpenSingleFileAsync` handles the case where the opened file shares a name with an already-open tab. When the file being replaced is the active tab, `SwitchFileAsync` must not be called — it would re-sync the old Monaco content back over `VirtualFile.Content`, undoing the replacement. Instead, call `monacoEditor.SetValue(content)` directly. `SwitchFileAsync` is only safe to call when the replaced file is not the active tab.

**Try/catch inline expression**: `WITH THE GREATEST RESPECT, <expression>` requires the expression on the *same line* as the keyword — it is not a block header. Writing the expression on the following line causes the parser to return `null` for the program (parse failure). `WITH GRATITUDE` and `MODIFIED RAPTURE` are mid-block keywords that follow the same pattern as `OTHERWISE,` — they appear as their own lines. This also means the formatter's indentation rules for `WITH THE GREATEST RESPECT,` diverge from valid parser input: the formatter treats it as a block opener (content on subsequent lines) purely for display purposes; the body lines shown indented beneath it in formatted output are the `WITH GRATITUDE` / `MODIFIED RAPTURE` / `THAT CONCLUDES THE MATTER.` continuation lines, not expression bodies.

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
| `mount <project>` | `init` | `--title`, `--or`, `--hollow`, `--tiptoe` | Creates a project directory. Without `--hollow`, also scaffolds `<project>/<project>.topsy`. `--title` defaults to `"Programme"`. |
| `commission <file>` | `new` | `--title`, `--or`, `--tiptoe` | Scaffolds a Hello World `.topsy` file. UTF-8, no BOM. `--title` defaults to `"Programme"`. Both commands delegate content to `FileManager.BuildFileContent`. |
| `rehearse <file>` | `check` | `--tiptoe` | Syntax-checks without executing via `ProgramRunner.Check`. |
| `sorcerer promptbook <file>` | `dev ast` | `--abridged`, `--chromatic`, `--tiptoe` | Prints AST as JSON. `NodeJsonConverter` handles polymorphic serialisation. |
| `pedigree` | `info` | `--tiptoe` | Shows CLI version, commit SHA, and language spec version. |

---

## 4. Known Gaps

**Monarch tokenizer keyword duplication**: the keyword list still appears in two places: `topsy-turvy-language.js` (Monarch tokenizer + completion provider) and `extensions/vscode/topsy-turvy/syntaxes/topsy-turvy.tmLanguage.json` (TextMate grammar). The `CompletionHandler.cs` copy has been removed — it now delegates to `KeywordData.Keywords` in `BWHazel.TopsyTurvy.Analysis`. The JS and TextMate copies cannot consume the C# library and remain separate.

**Web editor interactive stdin (deferred)**: `BufferedWebIO` reads from a pre-supplied `Queue<string>` drawn from a textarea. True line-by-line interactive stdin (prompting after each `PRAY TELL`) requires making `ITopsyTurvyIO` async — straightforward future step with the same interface, a new `InteractiveWebIO` implementation, and an XtermBlazor input handler.

**LSP squiggle accuracy (deferred)**: when a syntax error appears inside a block construct (function, loop, conditional, switch, try/catch), the squiggle lands on the opening line rather than the error line. Root cause: `Ws(Statement).Try()` in `ProgramParser.body` resets Superpower's error position before the entire construct on failure. Fixing this requires replacing `.Try().Many()` with a custom error-recovery loop — significant parser refactoring, deferred.

**Keyword-as-name squiggle precision**: diagnostic spans are recovered by source-line scanning (`FindDeclarationSpan`) as a workaround for PlaceholderSpan. Replaceable once AST position wiring is added.

**Web editor JS not linted or formatted**: `interpreter/BWHazel.TopsyTurvy.WebEditor/wwwroot/js/topsy-turvy-language.js` and `web-editor.js` have no ESLint or Prettier configuration. The VS Code extension's tooling (`extensions/vscode/topsy-turvy/`) does not cover these files. A root-level `package.json` with `eslint` (`@eslint/js` recommended, browser globals) and `prettier` would be the natural fix, but was considered overkill for two files at the current stage.

**Keyword strings duplicated throughout the .NET solution**: language token strings (`"VERITY"`, `"NAY"`, `"NAUGHT"`, `"JUST SO"`, type names `"PEER"`, `"FATHOM"`, `"YARN"`, `"DECREE"`) appear as inline string literals in Parser, Runtime, Analysis, and LanguageServer. The planned fix is a `static class Keywords` with `const string` fields in `BWHazel.TopsyTurvy.Ast` (referenced by all projects) with nested classes `Literals`, `TypeNames`, and `SpecialNames`. The intent is to centralise **all** keyword strings, not just the cross-project tokens. `KeywordData.Keywords` in Analysis remains the authoritative 74-entry list for LSP/completion.

**Code duplication in LSP handlers and elsewhere**: ten duplication patterns were identified during the tech review. The highest-value items are: (1) `FindSymbolInOtherDocuments` private method copy-pasted across `HoverHandler`, `DefinitionHandler`, `ReferencesHandler`, `RenameHandler` — should move to `DocumentStateManager`; (2) word-boundary scan loop duplicated in `ReferencesHandler`, `RenameHandler`, and `SourceAnalyser` — extract to `SourceAnalyser.FindWordOccurrences`; (3) `IsIdentifierChar` duplicated between `SourceAnalyser` (public) and `SymbolTable` (private) — remove the private copy; (4) loop body execution pattern (`CheckCancellation / try-catch ContinueSignal / BreakSignal`) repeated four times in `Interpreter` — extract to `ExecuteLoopBody` helper. Lower-priority items: tiptoe error-reporting pattern in CLI command builders, `PanelHelper` panel-creation boilerplate, `SourceFormatter` keyword dispatch loops.

### Deferred LSP Features

| Feature | Blocker |
|---|---|
| Inlay Hints (`textDocument/inlayHint`) | Handler not yet written; `SymbolInfo.TypeDisplayName` already carries the data. |
| Call Hierarchy | Requires a call-graph not currently built by `SymbolTable`. |

### Deferred Web Editor Features

| Feature | Blocker |
|---|---|
| Interactive stdin (line-by-line `PRAY TELL`) | `ITopsyTurvyIO` must become async; requires `InteractiveWebIO` + XtermBlazor input handler |

---

## 5. Next Steps

1. Read `AGENTS.md` and `DEVELOPMENT.md` before starting any work.
2. Run `dotnet build interpreter/BWHazel.TopsyTurvy.slnx --no-incremental` and `dotnet test` to confirm baseline.
3. **Keyword constants refactoring**: centralise all language token strings into a `static class Keywords` in `BWHazel.TopsyTurvy.Ast` — see §4 for design. Replace every inline string literal across Parser, Runtime, Analysis, and LanguageServer. All keyword strings, not just the cross-project ones.
4. **Code duplication refactoring**: resolve the ten patterns identified in §4, prioritising the four high-value LSP/runtime items.
5. **LSP server packaging**: the default binary path only works from the dev repo layout and cannot be distributed as a `.vsix`. Proposed: add `dotnet publish` to the extension build script outputting to `extensions/vscode/topsy-turvy/server/`; update the default path in `extension.ts`; use `--no-self-contained` as the starting point.
6. **LSP squiggle accuracy** (deferred — see §4).
