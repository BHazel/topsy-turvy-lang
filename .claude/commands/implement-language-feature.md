---
name: implement-language-feature
description: >
  Guides the complete, full-stack implementation of a new Topsy Turvy language
  feature — from grammar through parser, runtime, LSP, Monaco/TextMate syntax
  highlighting, and tests. Use this skill whenever a new keyword, operator,
  construct, or language change has been added to SPEC.md and needs to be
  propagated throughout the codebase. Also invoke it when asked to "add a
  feature to Topsy Turvy", "implement [keyword/construct] in the language",
  "extend the language with X", or "sync the grammar files after a spec
  change". This skill knows the full layer order and all project constraints —
  always use it rather than attempting a language change freehand, because the
  change touches at least seven files across four projects and several
  invariants must hold simultaneously.
---

# Topsy Turvy: Implement Language Feature

You are about to implement a new feature in the Topsy Turvy programming
language. This codebase spans a .NET interpreter, an LSP server, a Blazor
WASM web editor, a VS Code extension, and two grammar file formats. A change
to the language must land consistently in all of them or the tooling will
silently disagree with the runtime.

This skill walks you through every layer in the correct order and tells you
what to verify at each step.

---

## Step 1: Load context (before touching any file)

Read all four of these files in full before doing anything else. They are
short but every constraint in them is load-bearing.

| File | Why you need it |
|---|---|
| `AGENTS.md` | Grammar invariants I1–I10, retired keyword list, coding standards, test conventions. Violations here break the language's design contract. |
| `DEVELOPMENT.md` | Current baseline test counts, file inventory, non-obvious constraints (§2). Read §2 carefully — it contains traps that are easy to fall into. |
| `SPEC.md` | The authoritative language specification. ~800 lines. This is what you are implementing. |
| `GRAMMAR.ebnf` | The formal grammar (~135 lines). After you implement the feature, it must match SPEC.md. |

---

## Step 2: Identify the delta and confirm scope

Before writing a single line of code, compare SPEC.md against the current
implementation and produce a brief delta summary:

* Which new keywords or token sequences does the feature introduce?
* Which existing production rules in GRAMMAR.ebnf need extending?
* Which layers are definitely affected (parser, runtime, analysis, LSP, grammar files)?
* Does the feature introduce new block structure (open/close keywords)? If so, the LSP formatter, folder and indentation rules all need updating.
* Does it violate any invariant from AGENTS.md §Grammar Invariants? If so, stop and discuss with the user before proceeding.

**Present this delta summary to the user and wait for their confirmation before making any edits.** This is the cheapest place to catch a misunderstanding.

---

## Step 3: Implement in layer order

Work through only the layers that are actually affected. Each layer depends
on the one above it, so do not skip ahead.

### 3a. `GRAMMAR.ebnf`

Add or update the production rules. Keep the style consistent with what is
already there. After editing, read it back and check that every new token
referenced in a rule has a corresponding terminal definition.

### 3b. `BWHazel.TopsyTurvy.Analysis/KeywordData.cs`

This is the **single C# source of truth** for all language keywords. If the
feature adds any new keywords, append them here with a short descriptive
label (matching the style of the existing 74 entries). Everything downstream
— LSP completion, hover text, the Monaco keyword list — flows from this file.

Do not add the same keyword string anywhere else in the .NET solution without
also adding it here first.

### 3c. `BWHazel.TopsyTurvy.Parser/`

Implement the lexer tokens and parser combinators for the new construct.
This project uses Superpower. Match the patterns already in `TopsyTurvyParser`
— use `Token.EqualTo`, `Parse.Ref`, and `.Select()` for AST construction.
Add any new AST node types to `BWHazel.TopsyTurvy.Ast/` first (one type per
file, namespace mirrors folder path, positional records for data-only nodes).

**Build checkpoint** — after completing the parser layer, run:
```
dotnet build interpreter/BWHazel.TopsyTurvy.slnx --no-incremental
```
Fix all errors before continuing. It is much easier to diagnose parse errors
before the runtime is tangled in.

### 3d. `BWHazel.TopsyTurvy.Runtime/`

Implement execution semantics in the tree-walking interpreter (`Interpreter.cs`).
Add a new `Execute` or `Evaluate` overload for each new AST node type. Follow
the signal-exception pattern for control flow (see `BreakSignalException`,
`ReturnSignalException`) — do not use return values to signal control flow.

Use `TopsyTurvyException` (or a subclass) for all language-level runtime
errors. Prefix all instance member access with `this.`.

### 3e. `BWHazel.TopsyTurvy.Analysis/`

Update any of these that are affected by the new construct:

- **`SourceFormatter.cs`** — add indentation rules for new block openers/closers. The formatter uses a stack-based approach; new block keywords need entries in both the increase-indent and decrease-indent sets.
- **`SourceAnalyser.cs`** — update `FindSkipRanges` if the new construct introduces a new comment-like or string-like region that should be excluded from symbol scanning.
- **`HoverMarkdownBuilder.cs`** — add hover descriptions if the feature introduces new symbol kinds.

### 3f. `BWHazel.TopsyTurvy.LanguageServer/`

Update handlers only if the feature changes block structure, introduces new
symbol kinds, or adds keywords that need special treatment:

- **`FoldingRangeHandler`** — new block openers/closers need fold regions.
- **`DocumentFormattingHandler`** — new block openers/closers need indent rules (mirrors the SourceFormatter change above).
- **`CompletionHandler`** — new keywords are picked up automatically from `KeywordData.Keywords` — no change needed unless the feature has unusual completion semantics.
- **`SemanticTokensHandler`** — new symbol kinds need token type mappings.

### 3g. Monaco tokenizer: `topsy-turvy-language.js`

Two things to update in `interpreter/BWHazel.TopsyTurvy.WebEditor/wwwroot/js/topsy-turvy-language.js`:

**Keyword completion list** — append new keyword/description pairs to the
`keywords` array so it exactly mirrors `KeywordData.Keywords`. The arrays
must stay in sync; a mismatch means the Web Editor's completion list diverges
from the LSP's list.

**Monarch tokenizer rules** — add regex rules for the new tokens. Ordering
is critical: longer / more-specific patterns must appear before shorter ones
that share a prefix. Look at the existing comment blocks in the tokenizer
(e.g. `// MY DUTY IS PREMATURELY DISCHARGED must precede MY DUTY IS DISCHARGED`)
for the established convention. Group the new rules with their semantic
neighbours.

### 3h. TextMate grammar: `topsy-turvy.tmLanguage.json`

Add the new keywords to the correct semantic group in
`extensions/vscode/topsy-turvy/syntaxes/topsy-turvy.tmLanguage.json`.
The grammar is organised into named groups (control-flow, operators, loops,
functions, etc.) — place each new keyword in the group that matches its role.
A keyword in the wrong group will get the right colour for the wrong reason,
which creates confusion when the language evolves.

**Grammar test checkpoint** — after editing both grammar files, run:
```
cd extensions/vscode/topsy-turvy && npm run test:grammar
```
All four snapshot files must pass. If a snapshot changes legitimately, update
it with `vscode-tmgrammar-test --updateSnapshot`.

---

## Step 4: Write tests

Add tests to `interpreter/BWHazel.TopsyTurvy.Tests/` following project
conventions:

* One test class per production type, in a subdirectory that mirrors the project it covers.
* Method naming: `{MethodOrProperty}_{Condition}_{ExpectedOutcome}`
* Every test method needs an XML `<summary>` following the pattern:
  `Tests that the <see cref="X.Y"/> method/parser/property {action}.`
* `[Theory]` parameters need `<param>` tags.
* Arrange / Act / Assert separated by blank lines, no inline `//` comments.
* For parser tests, use `new TextSpan("input")` directly — no Superpower
  `PackageReference` needed in the test project (it is available transitively).

Cover at minimum:
* Happy-path parse of the new construct.
* Runtime execution of a simple program using the construct.
* Any error cases (malformed syntax, type mismatches) that the spec defines.

**Full test run checkpoint:**
```
dotnet test interpreter/BWHazel.TopsyTurvy.Tests
dotnet test interpreter/BWHazel.TopsyTurvy.Cli.E2ETests
```
The baseline is 458 unit tests + 50 E2E tests. New tests should push these
numbers up, never down.

---

## Step 5: Add an example (if appropriate)

If the feature is substantial enough to warrant a standalone demonstration,
add a `.topsy` file to `examples/`. All four existing examples
(`hello_world.topsy`, `fizzbuzz.topsy`, `fibonacci.topsy`,
`pirates_calculator.topsy`) must continue to execute correctly — run them
with `dotnet run --project interpreter/BWHazel.TopsyTurvy.Cli -- perform
examples/<name>.topsy` to verify.

---

## Step 6: Update DEVELOPMENT.md

This is the last step, after everything else is green:

* **Header** — update the "Last Updated" date and summarise the new feature in one clause.
* **§1 File Inventory** — add or update rows for any new or significantly changed files.
* **§2 Non-Obvious Constraints** — add an entry if the feature introduced a new non-obvious behaviour (e.g. a new skip range, a new signal exception, a new ordering constraint).
* **§4 Known Gaps** — record anything intentionally deferred (e.g. LSP inlay hints for the new construct).
* **§5 Next Steps** — update or remove items that this feature resolves; add follow-on work.
* **Baseline counts** at the top — update test numbers to reflect the new passing total.
