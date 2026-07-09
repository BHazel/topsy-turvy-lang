# Topsy Turvy: Agent Instructions

## Introduction

This is the codebase for Topsy Turvy, an esoteric but fully functional programming language to give code a flair of Gilbert & Sullivan operettas!

## Language Specification

The complete specification of the language is in the `./SPEC.md` file.

**You must read this file before performing any work so you understand the language specification and grammar.**

## Development Status

A summary of development from the last session, a file inventory and known gaps is documented in the `./DEVELOPMENT.md` file.

**You must read this file before performing any work so you understand the current status.**

## Standing Rules

### Grammar Invariants

The following rules must be preserved in any modification to the language or
its examples.  They reflect deliberate structural decisions that affect all
constructs simultaneously.

**I1 — Prefix notation throughout.**
All operators (arithmetic, boolean, string) use prefix notation with `AND` as
the argument separator. No infix or postfix operators exist.

**I2 — `IF YOU PLEASE.` is the universal expression-list closer.**
`WOVEN OF`, `SUMMON`, `ALL OF`, and `ANY OF` all close with `IF YOU PLEASE.`
No other closer is used for these constructs. It is the formal signal that a
variable-length argument list is complete and the assembled company may proceed.

**I3 — Full stops are mandatory on specific keywords.**
The following keywords require a trailing full stop as part of their token and
are not valid without it:
`FINALE.` `QUITE SO.` `SO MUCH FOR THAT.` `THAT WILL DO.`
`NOTHING COULD BE MORE SATISFACTORY.` `THE CURTAIN RISES.` `ONCE MORE.`
`THE TERM EXPIRES.` `MY DUTY IS DISCHARGED.`
`MY DUTY IS PREMATURELY DISCHARGED.` `IF YOU PLEASE.`

**I4 — Loop labels are optional.**
`BY A LEGAL FICTION` may optionally be followed by `KNOWN AS <label>` in all
loop forms (infinite, ascending, descending, whilst). When present, the label
has no semantic effect beyond documentation. The loop is valid with or without
a label.

**I5 — Switch operates on literals only.**
`WHEN ACTING AS` accepts literal values only — strings, integers, floats,
`VERITY`, `NAY`. Expressions are not valid as case labels.

**I6 — Functions are not closures.**
Functions receive values exclusively through their declared parameters.
Global variables declared in `PRINCIPALS` are accessible everywhere except
inside functions.

**I7 — `PRAY WELCOME` is the sole declaration form.**
Variables may only be declared with `PRAY WELCOME ... AS A ... [BEING ...]`.
There is no implicit declaration; using an undeclared name is an error.

**I8 — The boolean type is `DECREE`; its literals are `VERITY` and `NAY`.**
`VERITY` and `NAY` are the only valid boolean values. Using a non-`DECREE`
expression in any boolean context (conditions, logical operators, guard clauses)
is a compile-time type error. Truthiness coercion has been removed in v0.5.0.

**I9 — `.topsy` is the sole source file extension.**
No other extension is valid. The language name is Topsy Turvy; the extension
remains `.topsy`.

### Retired Keywords

The following keywords appeared in earlier drafts and versions of the language and have been replaced.  They must not be used in any `.topsy` source file or spec example.

| Retired keyword | Replaced by | Construct |
|---|---|---|
| `ITZ` | `BEING` | Initial value in declaration |
| `NOWT` | `NAUGHT` | Null type and null value |
| `MAYHAPS` | `AS IT WERE` | Expression cast |
| `COMPOUND OF...RESOLVED.` | `WOVEN OF...IF YOU PLEASE.` | String concatenation |
| `ON THE CONTRARY x` | `HARDLY EVER x` | Logical NOT |
| `STAND DOWN.` | `THAT WILL DO.` | Loop break |
| `END OF ENGAGEMENT.` | `THE TERM EXPIRES.` | Loop end |
| `ACCEPTING` | `UNDER THE TERMS OF` | Function parameters |
| `ACCEPTING NOTHING` | `UNDER NO OBLIGATION` | No-parameter function |
| `RESOLVED.` | `IF YOU PLEASE.` | Function call closer |
| `OBSERVE:` | `(ASIDE, AT SOME LENGTH:` | Multi-line comment open |
| `ENOUGH SAID.` | `END OF ASIDE.)` | Multi-line comment close |
| `BECOMES` | `IS APPOINTED` | Assignment |
| `PROCLAIM` | `BEHOLD` | Print output |
| `UPON REFLECTION?` | `SHOULD IT TRANSPIRE THAT` | If condition |
| `INDEED.` | `QUITE SO.` | True branch |
| `PERHAPS` | `OR, IF NOT,` | Else-if |
| `ON THE CONTRARY` | `OTHERWISE,` | Else branch |
| `THUS.` | `SO MUCH FOR THAT.` | End-if |
| `WHAT IS YOUR OFFICE?` | `IN WHICH CAPACITY?` | Switch |
| `IN THE CASE OF` | `WHEN ACTING AS` | Case label |
| `IN ALL OTHER CASES` | `FAILING ALL OF THE ABOVE,` | Default case |
| `MATTER RESOLVED.` | `NOTHING COULD BE MORE SATISFACTORY.` | End switch |
| `ENGAGE` | `SUMMON` | Function call |
| `PRAY ENGAGE THE SERVICES OF` | `PRAY ADMIT` | Import |
| `FREE FROM THIS QUANDARY.` | `THAT WILL DO.` | Loop break |
| `PRAY SUMMON THE SERVICES OF` | `PRAY ADMIT` | Import |
| `PRAY INTRODUCE` | `PRAY WELCOME` | Variable declaration |
| `VERITY` (type name) | `DECREE` | Boolean type |
| `WIN` | `VERITY` | Boolean true literal |
| `FAIL` | `NAY` | Boolean false literal |
| `IS HENCEFORTH A` | `AS IT WERE` | In-place type cast (removed in v0.5.0; use expression cast `AS IT WERE <expr> AS A <type>` instead) |
| `JUST SO` | *(removed)* | Implicit result variable (removed in v0.5.0; standalone expression statements now produce a warning) |

### Specific Files

* **`DEVELOPMENT.md`:** The current development state, which is a summary of the latest changes from the last coding session.
* **`./examples`:** Contains example Topsy Turvy code files.

## Implementation Standards (Interpreter)

### Technical Stack

* **Runtime:** .NET 10
* **Root Namespace:** `BWHazel.TopsyTurvy`
* **Key Libraries:** `Superpower` (Parsing), `System.CommandLine` (CLI), `Spectre.Console` (UX)
* **Architecture:** LSP-ready. Parser must be decoupled from runtime and provide full source mapping (line/column) and diagnostic collections.

### Project Structure

* **Root Directory:** `operetta/`
* **Solution File:** `BWHazel.TopsyTurvy.slnx`
* **Project Layout:** Each project in its own directory: `operetta/[ProjectName]/[ProjectName].csproj`.
* **Namespace Mirroring:** Folder structure must strictly mirror the namespace hierarchy.

### Coding Style

* **File Granularity:** Strictly one type per file.
* **No Implicit Usings:** The `ImplicitUsings` project setting should be disabled. All `using` directives must be explicit.
* **Modern C#:** Use the latest C# language features where appropriate (see patterns below).
* **Explicit Typing:** Prefer explicit types over the `var` keyword.
* **Naming:** Concise but descriptive names with full words preferred.  No abbreviations should be used unless the abbreviation is the canonical name, e.g. `io`, `Ast`.
* **Instance Members:** Always prefix instance member access with `this.`.
* **Exceptions:** Use `TopsyTurvyException`, or a subclass, for all language-level runtime errors.
* **Named Arguments:** Use named arguments whenever parameter purpose is not self-evident from position alone, particularly for `bool`, numeric, and `string` parameters, e.g. `preProcessedOffset: 0, originalLine: 1, originalColumn: 1`.
* **Records:** Use `record` for data-only types with no non-trivial logic.  Prefer positional records where all properties are set at construction.
* **Primary Constructors:** Use primary constructors when a type simply captures its parameters with no additional initialisation logic.
* **Collection Expressions:** Use `[]` over `new List<T>()` or `Array.Empty<T>()`.
* **Target-Typed `new()`:** Use target-typed `new()` where the type is unambiguous from the left-hand side declaration.
* **Documentation:** All `public`, `internal`, and `private` types and members require XML documentation comments. The sole exemption is `private readonly` fields. `<summary>` must be a single sentence; use `<remarks>` for multi-sentence elaboration or non-obvious constraints.

### Test Project Conventions

These rules apply to all files under `operetta/BWHazel.TopsyTurvy.Tests/`.

* **Test Class Naming:** `{TestedClass}Tests`, one test class per production type, one file per test class.
* **Test Method Naming:** `{MethodOrProperty}_{Condition}_{ExpectedOutcome}` (e.g. `StringLiteral_WithoutClosingQuote_Fails`).
* **XML Summary Format:** Test method summaries differ by project layer:
  * **Interpreter tests** (`TopsyTurvyInterpreter*Tests`): `Tests that the <see cref="Interpreter.Execute"/> method {present-tense action}.`
  * **Parser tests** (`TopsyTurvyParser*Tests`): `Tests that {language construct description} produces a <see cref="ResultNode"/> with {property} set correctly.`  Use `<see cref>` for all referenced node types and `LiteralType` values.
  * **Class-level summaries: interpreter**: `{Category} tests for the <see cref="Interpreter"/> class.`
  * **Class-level summaries: parser**: `Tests for {form description} parsed by the <see cref="TopsyTurvyParser"/> class.`
* **Theory Parameters:** All parameters on a `[Theory]` test method must have a corresponding `<param>` tag in the XML documentation.
* **Arrange / Act / Assert:** Separate each phase with a blank line. Do not put inline `//` comments inside test bodies; if the intent is unclear rewrite the XML summary to be more specific.
* **Test Stubs and Helpers:** Each stub or helper class lives in its own file, is named with a `Test` prefix, e.g. `TestPrefixingPreProcessor`, and is declared `internal sealed`. Place it in the same subdirectory as the tests that use it.
* **Superpower Combinators:** Call them directly as delegates using `new TextSpan("input")` from `Superpower.Model`; no direct `PackageReference` to Superpower is required in the test project as it is available transitively via the Parser project reference.
* **What not to Test:** `Lexer.Whitespace`, `Lexer.WhitespaceRequired`, and `Lexer.IntegerLiteral` are intentionally untested; each is a single-line delegation to a Superpower library primitive with no custom logic.

### Visual Editor Conventions

These rules apply to `operetta/BWHazel.TopsyTurvy.WebEditor/Visual/` and `Components/VisualEditor/`. Read §2 of `DEVELOPMENT.md` for the full constraint set.

**Port Alignment:** `VisualPortRole` determines `PortAlignment` inside `MakePort`; never pass an alignment explicitly:

| `VisualPortRole` | `PortAlignment` | Edge Type |
|---|---|---|
| `FlowIn` | Top | Programme Flow (execution enters node from above) |
| `FlowOut` | Bottom | Programme Flow (execution continues below) |
| `BranchOut` | Bottom | Programme Flow (branch choice exits below) |
| `DataIn` | Left | Data / Expression (value flows in from the left) |
| `DataOut` | Right | Data / Expression (value flows out to the right) |

**Block Closer Pattern:** For any block node (Conditional, Loop, TryCatch, Switch, Guard):
1. Build all branches first, collecting each tail node.
2. `maxTailY = branches.Max(t => t.Position.Y)`
3. Place closer at `(openerX, maxTailY + NodeLayoutContext.RowSpacing)`.
4. Call `layout.AdvancePrimaryYTo(closerY + NodeLayoutContext.RowSpacing)`.

**Branch Centreing** Branch `i` of `N` branches is placed at `openerX + (i - (N-1)/2.0) * spacing`. Default spacing: 660 px (Conditional, TryCatch), 440 px (Switch).

**Expression Anchoring:** Expression nodes are placed at `(anchor.X - ExprColumnWidth, anchor.Y + portIndex * ExprPortSpacingY)` relative to their consuming statement node. `ExprColumnWidth = 210.0`, `ExprPortSpacingY = 80.0`.

**CSS Prefix:** All visual node CSS classes use `visual-node-` (e.g. `visual-node-header`, `visual-node-flow-top`). The retired `vn-` prefix must not be re-introduced.

### Web Editor Conventions

These rules apply to `operetta/BWHazel.TopsyTurvy.WebEditor/`.

* **JS Interop — Inbound (JS → C#):** Methods invoked from JavaScript must be `public` and marked `[JSInvokable]`.  They are called via `dotNetRef` (`DotNetObjectReference<Editor>`), which is created in `OnAfterRenderAsync` on first render and disposed in `Dispose()`.
* **JS interop — Outbound (C# → JS):** Call JavaScript via `JSRuntime.InvokeAsync`; keep all JS logic in `web-editor.js` (or `topsy-turvy-language.js` for language registration). Do not scatter JS calls across multiple Razor files.
* **Virtual File System:** `VirtualFile` is the file registry (name, open/close state). The Monaco editor models are the source of truth for file content: always read/write content via `monacoEditor.GetValue`/`monacoEditor.SetValue`, not from `VirtualFile`.
* **MudBlazor API:** Use current MudBlazor 9.x property names.

## Documentation Site (`docs/topsy-turvy/`)

The Docusaurus site under `docs/topsy-turvy/` is structured into three content areas:

* **Concepts (`docs/concepts/`):** High-level explanations of each toolchain component (parser, runtime, analysis layer, etc.).  These pages must describe *how the component works*, not what the language does.  Do not copy or summarise language-spec content here: that belongs in `SPEC.md`.  Before adding content to a concepts page apply this test: _does this sentence describe the toolchain component (a class, a design decision, an implementation mechanism) or does it describe the language feature (syntax, semantics, example programs)?_  Only the former belongs here.  Appropriate content includes how classes relate to each other, non-obvious design choices (e.g. using .NET exceptions as control-flow signals), storage representation of new types, and enforcement mechanisms that are not obvious from the source.  Syntax tables, keyword lists, example programs, cast rules and traversal patterns are usually considered language specification detail and should be avoided but could be useful in context.  Do not create a new subsection for each language feature: fold implementation notes into the existing component section (Environment, Interpreter, etc.) where they naturally belong.
* **Guide (`docs/guide/`):** Task-oriented how-to pages for users of the language and toolchain.
* **API Reference (`docs/api/`):** Auto-generated from XML documentation comments; do not edit generated files by hand.

## Synchronisation Points

Several areas of the codebase must be kept consistent whenever related changes are made.  Failing to update all files in a group leaves the tooling in an inconsistent state, for example, a new keyword that highlights in the VS Code extension but not in the REPL, or a class whose XML documentation no longer matches its implementation.

### New Language Keyword or Construct

Use the `/implement-language-feature` skill as it is the single source of truth for layer order, build checkpoints and per-layer constraints.  Do not attempt a language change without it: the change touches at least eight files across four projects and several invariants must hold simultaneously.

The skill also covers the visual editor layer (`VisualGraphBuilder`). Every new or renamed AST node type that produces a statement or expression needs a corresponding `Create*` or expression branch in `VisualGraphBuilder.cs`: consult the node-mapping table in the skill for the full list.

`apps/apple/TopsyTurvy/Theatre/Editor/TopsyKeywords.swift` joins the REPL/Monaco/TextMate keyword-highlighting group as of Phase 2: it mirrors `KeywordData.Keywords` on the Swift side (word-by-word, since `CodeEditorView`'s reserved-identifier matching cannot anchor a trailing word-boundary against a keyword's own punctuation) to build `TopsyLanguageConfiguration`'s `reservedIdentifiers`. Update it alongside every other keyword-list sync point whenever `KeywordData.Keywords` changes.

### New REPL Command

| # | File | What to Update |
|---|---|---|
| 1 | `Repl/ReplConstants.cs` | Add the command string constant and its alias. |
| 2 | `Repl/ReplSession.cs` | Handle the command in the input dispatch loop. |
| 3 | `Cli.E2ETests/CadenzaCommandTests.cs` | Add an E2E test piping the command via stdin. |
| 4 | `docs/tooling/cli-repl.md` | Document the command in the REPL Session section. |
| 5 | `DEVELOPMENT.md` §3 | Update the `cadenza` CLI command row. |

### New CLI Command

| # | File | What to Update |
|---|---|---|
| 1 | `Cli/CommandBuilders/` | New `*CommandBuilder.cs` class. |
| 2 | `Cli/Program.cs` | Wire the builder into the root command. |
| 3 | `Cli.E2ETests/` | New `*CommandTests.cs` test class. |
| 4 | `DEVELOPMENT.md` §3 | Add a row to the CLI Commands table. |

### Native Export (`operetta/BWHazel.TopsyTurvy.Embedded/`)

The v1 native export contract (`TOURING_THEATRE_PLAN.md` §4.2) is frozen — any change to its shape must update every file below in the same commit, and bump `topsyturvy_api_version()`. The Embedded project is organised by utility, not flat: `NativeInterop/` (session/callback/IO plumbing, namespace `BWHazel.TopsyTurvy.Embedded.NativeInterop`) and `Analysis/` (hover/completion/format/diagnostic JSON payload types, namespace `BWHazel.TopsyTurvy.Embedded.Analysis`), with `NativeExports.cs` at the project root. All types are `public`.

| # | File | What to Update |
|---|---|---|
| 1 | `Embedded/NativeExports.cs` | The `[UnmanagedCallersOnly]` export itself (C# method is PascalCase; only the `EntryPoint` string is `topsyturvy_*`). |
| 2 | `apps/apple/TopsyTurvy/Frameworks/include/topsyturvytoolchain.h` | The matching C declaration and any function-pointer typedef. |
| 3 | `apps/apple/TopsyTurvy/TopsyTurvyToolchainTests/TopsyTurvyToolchainTests.swift` | Swift smoke coverage for the new/changed export. |
| 4 | `operetta/BWHazel.TopsyTurvy.Tests/Embedded/NativeExportsTests.cs` | In-process xUnit coverage, calling the export via a `delegate* unmanaged<...>` obtained from `&NativeExports.Method` — `[UnmanagedCallersOnly]` methods cannot be called directly, even in-process (CS8901). |

`apps/apple/TopsyTurvy/TopsyTurvy.xcodeproj` remains the umbrella project (like a `.sln`); within it, the app target is `Theatre` and the XCFramework/module the app and tests link against is `TopsyTurvyToolchain` — keep these three names distinct when adding new targets or files.
| 5 | A future JNI shim (`apps/android/`, not yet built) | Once the Android lane exists, its bindings too. |

`PhraseContext.cs` in this project deliberately duplicates `CompletionHandler.GetPhraseContext` in `BWHazel.TopsyTurvy.LanguageServer` (the Embedded project cannot reference the OmniSharp-dependent LanguageServer project) — if the phrase-parsing rule ever changes, update both.

### XML Documentation

All `public`, `internal` and `private` types and members require XML documentation comments.  Whenever a class or method is added or its behaviour changes its XML documentation must be updated in the same commit.  Stale XML comments are actively harmful as they are the source of truth for the generated API reference and for IntelliSense tooltips.

### Docusaurus Documentation (`docs/topsy-turvy/`)

| Area | Path | When to Update |
|---|---|---|
| Concepts / Language Design | `docs/concepts/` | A toolchain component high-level behaviour has materially changed. |
| Concepts / Tooling | `docs/tooling/` | The CLI REPL behaviour changes, such as a new command, new editing feature, new mode. |
| API Reference | `docs/api/dotnet/` | Auto-generated so do not edit by hand; re-run `npm run api:generate`. |

The `cli-repl.md` page describes how the REPL works in detail.  It must be kept in sync with `ReplSession`, `ReplInputReader` and `ReplHighlighter`, specifically:

* New REPL Commands: Update the REPL Session section and the Mermaid flowchart.
* New Keystroke Handlers in `ReplInputReader`: Update the Input Reader section, Key Dispatch list and, if a new redraw pattern is involved, Keeping the Cursor in Sync.
* New Token Categories or Priority Changes in `ReplHighlighter`: Update the Highlighter section, Keyword Table category/colour row, Scanning priority list, or Word Boundaries if the boundary logic changes.

## Implementing Language Features

When a new language feature is added to `SPEC.md`, changes are required across multiple files spanning four projects.  Before starting any implementation work read the complete workflow and constraints in `.claude/commands/implement-language-feature.md`. That file is the single authoritative guide for this process and covers layer order, build checkpoints, per-layer constraints and what to verify at each step.

After completing the implementation, documentation must also be kept in sync:

* **XML documentation comments:** Update the `<summary>` and `<remarks>` blocks on any modified or newly added public type or member.  The XML docs are the source of truth for the API reference; stale comments are worse than no comments.
* **Docusaurus concepts pages:** Update a concepts page only if the component's high-level behaviour has materially changed, for example, if the parser now handles a new construct category or the runtime introduces a new execution model.  Do not add language-spec detail (keyword syntax, example programs, cast rules) to a concepts page; that content belongs in `SPEC.md`.