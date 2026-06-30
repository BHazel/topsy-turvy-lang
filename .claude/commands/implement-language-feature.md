---
name: implement-language-feature
description: >
  Guides the complete, full-stack implementation or modification of a Topsy
  Turvy language feature — from grammar through parser, runtime, LSP,
  Monaco/TextMate syntax highlighting, REPL highlighting, and tests. Use this
  skill whenever a keyword, operator, or construct is being added, changed, or
  removed in SPEC.md and needs to be propagated throughout the codebase. Invoke
  it when asked to "add a feature to Topsy Turvy", "implement
  [keyword/construct] in the language", "extend the language with X", "change
  the syntax/behaviour of [keyword]", "remove [keyword] from the language",
  "rename [keyword]", or "sync the grammar files after a spec change". This
  skill knows the full layer order and all project constraints — always use it
  rather than attempting a language change freehand, because the change touches
  at least seven files across four projects and several invariants must hold
  simultaneously.
---

# Topsy Turvy: Implement or Modify Language Feature

You are about to add, change, or remove a feature in the Topsy Turvy
programming language. This codebase spans a .NET interpreter, an LSP server,
a Blazor WASM web editor, a VS Code extension, and two grammar file formats.
A change to the language must land consistently in all of them or the tooling
will silently disagree with the runtime.

This applies equally to **new** constructs and to **modifications** of existing
ones — changing a keyword's syntax, making an optional clause mandatory,
renaming a keyword, or removing a construct entirely. The layer order and
verification steps are the same in all cases; the only difference is whether
each layer needs an addition, an edit, or a deletion.

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

**XML documentation comments are required on every new or modified public type and member throughout all steps below.** `<summary>` must be a single sentence; use `<remarks>` for multi-sentence elaboration or non-obvious constraints. This applies to AST node types, parser fields, runtime methods, analysis types, and LSP handlers alike. Do not defer XML docs to the end: write them as you add each type or member.

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

### 3i. Visual graph builder: `WebEditor/Visual/VisualGraphBuilder.cs`

If the feature adds a new AST statement or expression node, or renames an existing node's keyword, update `VisualGraphBuilder` to reflect it. The file is ~1100 lines but its structure is straightforward: a `BuildStatement` dispatch switch and a `CreateExpressionNode` compound section.

**Statement Nodes:** Add a new `case` branch in `BuildStatement` that calls a new `Create*Node` helper. The helper must follow the block-closer pattern documented in `AGENTS.md §Visual Editor Conventions` if it introduces a new block construct.

**Expression Nodes:** Add a new `if (expression is ...)` branch in `CreateExpressionNode`. Place expression nodes at `(anchor.X - ExprColumnWidth, anchor.Y + portIndex * ExprPortSpacingY)`.

**Port Conventions:** Always use `MakePort(parent, label, VisualPortRole.*)`. Never pass a `PortAlignment` directly; alignment is derived automatically from role.

**Current Node → Keyword Mapping** (keep this table in sync when AST nodes are added or renamed):

| AST type | Visual title | `VisualNodeKind` |
|---|---|---|
| `ProgramNode` (header) | `HARK!` | `Program` |
| `ProgramNode` (finale) | `FINALE.` | `Program` |
| `DeclarationNode` | `PRAY WELCOME` | `Declaration` |
| `ArrayDeclarationNode` | `PRAY WELCOME` | `Declaration` |
| `AssignmentNode` | `IS APPOINTED` | `Assignment` |
| `ArrayElementAssignmentNode` | `IS APPOINTED` (subtitle: `{arr} [at index]`) | `Assignment` |
| `PrintNode` | `BEHOLD` / `BEHOLD WITHOUT FANFARE` | `Print` |
| `InputNode` | `PRAY TELL` | `Input` |
| `ConditionalNode` (opener) | `SHOULD IT TRANSPIRE THAT` | `Conditional` |
| `ConditionalNode` (closer) | `SO MUCH FOR THAT.` | `Conditional` |
| `LoopNode` (opener) | `BY A LEGAL FICTION` | `Loop` |
| `LoopNode` (closer) | `THE TERM EXPIRES.` | `Loop` |
| `FunctionDefinitionNode` (no body) | `IT IS MY DUTY TO PERFORM` | `Function` |
| `FunctionDefinitionNode` (with body, opener) | `IT IS MY DUTY TO PERFORM` | `Function` |
| `FunctionDefinitionNode` (closer) | `MY DUTY IS DISCHARGED.` | `Function` |
| `ReturnNode` | `AND SO I FIND` | `Function` |
| `ThrowNode` | `A HIDEOUS CURSE ON` | `ErrorHandling` |
| `TryCatchNode` (opener) | `WITH THE GREATEST RESPECT,` | `ErrorHandling` |
| `TryCatchNode` (closer) | `THAT CONCLUDES THE MATTER.` | `ErrorHandling` |
| `SwitchNode` (opener) | `IN WHICH CAPACITY?` | `Conditional` |
| `SwitchNode` (closer) | `NOTHING COULD BE MORE SATISFACTORY.` | `Conditional` |
| `ImportNode` | `PRAY ADMIT` | `Other` |
| `GuardNode` (opener) | `YEOMAN` | `ControlFlow` |
| `GuardNode` (closer) | `UNDER ORDERS.` | `ControlFlow` |
| `AssertNode` | `THE LAW IS` | `ErrorHandling` |
| `ExpressionStatement` | `EXPRESSION` | `Other` |
| `LiteralNode` | literal value string | `Literal` |
| `IdentifierNode` / `ParameterNode` | name | `Identifier` / `Parameter` |
| `PrefixExpressionNode` (operator) | operator keyword (e.g. `SUM OF`) | `Operator` |
| `TernaryExpressionNode` | `SHOULD IT TRANSPIRE THAT` | `Conditional` |
| `ArrayIndexNode` | array name (subtitle: `at index`) | `Identifier` |
| `ArrayLengthNode` | `RECKONING OF` | `Operator` |
| `ExpressionCastNode` | `AS IT WERE` (subtitle: `AS A {type}`) | `Operator` |
| `FunctionCallNode` (SUMMON) | `SUMMON` | `Operator` |
| branch-entry headers | `QUITE SO.` / `OR, IF NOT,` / `OTHERWISE,` / `WHEN ACTING AS {val}` / `FAILING ALL OF THE ABOVE,` / `MODIFIED RAPTURE,` / `OTHERWISE,` (guard) | matches parent kind |

### 3j. Visual editor supporting files

Four additional files in `WebEditor/Visual/` must be kept in sync with `VisualGraphBuilder` whenever a node type or keyword changes:

**`VisualGraphToAstConverter.cs`** — The converter walks the live diagram and reconstructs an AST. If a new statement node is added:
- Add a `case` in `ReconstructSingleStatement` (for non-block nodes) or `ReconstructBlock` (for block openers), naming the new `StatementType` string.
- Add a corresponding `Reconstruct*Factory` method that builds the AST node from visual node properties and DataIn port links, following the pattern of the existing factory methods.
- If the new node has branch bodies (BranchOut ports), update `WalkBranchBody` callers or add new branch label constants to match the port labels used in `VisualGraphBuilder`.
- If the new node introduces a new branch-entry header node (`statementType` ending in `Branch`, e.g. `"TryCatchSuccessBranch"`), add that string to the `is "..." or "..."` skip list in `WalkFlowStatements` so header artefact nodes are never reconstructed as statements.

**`VisualNodeFactory.cs`** — The factory creates visual nodes without an AST input (for the context menu). If a new statement node is added:
- Add a `case` in the `CreateStatement` switch (for statement nodes) or `CreateExpression` switch (for expression nodes).
- For block types, create a `Create*Block` helper that adds opener + branch-entry headers + closer, sets `PairedCloserId`/`PairedOpenerId`, and adds all required ports (FlowIn, FlowOut, BranchOut, DataIn) with labels matching the port labels in `VisualGraphBuilder`.

**`VisualContextMenu.razor`** — The context menu lists every addable node type. Add a `<div class="visual-context-menu-item">` entry in the appropriate section (Statement / Control Flow / Error Handling / Function / Expression) for the new node type. The `@onclick` handler must pass the matching `StatementType` string to `OnAddStatementNode` or `OnAddExpression`.

**`VisualTypeMaps.cs`** — If the feature adds or renames a `LiteralType` enum value, add or update the corresponding entry in `VisualTypeMaps.TypeToKeyword` using the `Keywords.TypeNames.*` constant (never a raw string literal).

### 3k. Code generator and tests: `Analysis/TopsyTurvyCodeGenerator.cs` and `Tests/Analysis/TopsyTurvyCodeGeneratorTests.cs`

If the feature adds, removes, or renames a statement or expression construct, update the code generator and add corresponding round-trip tests.

**Code generator** (`BWHazel.TopsyTurvy.Analysis/TopsyTurvyCodeGenerator.cs`):
- Add a new `case` in `WriteStatement` for new statement node types, emitting the correct keyword sequence, indentation (use the `indent` local, `depth + 1` for nested blocks), and terminating punctuation.
- Add a new `case` in `WriteExpression` for new expression node types.
- Update `OperatorKeyword` if a new `Operator` enum value is introduced.
- Update `TypeKeyword` if a new `LiteralType` enum value is introduced.
- Generated source must satisfy all grammar invariants (I1–I10 in `AGENTS.md`): `IF YOU PLEASE.` on variadic close, full stops on block closers, etc.
- Verify by parsing the output: `new TopsyTurvyParser().TryParse(generated).Diagnostics` must be empty.

**Tests** (`BWHazel.TopsyTurvy.Tests/Analysis/TopsyTurvyCodeGeneratorTests.cs`):
- Add at least one round-trip test per new statement and expression construct.
- Round-trip pattern: write source as a raw string literal → `GenerateFromSource(source)` → `parser.TryParse(generated)` → `result.Diagnostics.ShouldBeEmpty()` → assert structural properties on `result.Program`.
- Use `[Theory]` with `[InlineData]` when covering multiple variants of the same construct (e.g. operator keywords, type keywords).
- Avoid reserved identifier names: `i`, `a`, and `b` are reserved by the language and must not be used as variable or parameter names in test source.
- Check `DEVELOPMENT.md` for the current baseline test count and confirm the new tests push it up.

### 3l. REPL highlighter: `Repl/ReplHighlighter.cs`

Add the new keyword(s) to the keyword table inside the `ReplHighlighter` static
constructor. Place each entry in the appropriate colour category (programme
structure, control flow, declarations, type names, operators, I/O and functions,
boolean literals, null literal, or special variables). The table is sorted by
length descending at class-load time, so manual ordering is not required, but
group new entries with their semantic neighbours to keep the list readable.

If the feature introduces a construct that belongs to a brand-new colour
category, add a new comment block and choose a colour from the established
Spectre.Console CLI palette — do not reuse an existing category colour for a
semantically unrelated group.

There are no automated tests for the highlighter keyword table. After editing,
run `operetta cadenza` and type a line that exercises the new keyword to verify
it is highlighted correctly in Aesthetic Mode. Tiptoe Mode requires no change —
no highlighting is applied there.

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
Check `DEVELOPMENT.md` for the current baseline — it is updated after each session. New tests should push the numbers up, never down.

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
* **§4 Known Gaps** — record anything intentionally deferred (e.g. LSP inlay hints for the new construct); remove any Known Gap that this feature resolves.
* **§5 Next Steps** — update or remove items that this feature resolves; add follow-on work.
* **Baseline counts** at the top — update test numbers to reflect the new passing total.

---

## Step 7: Update documentation

### XML documentation comments

Review the `<summary>` and `<remarks>` blocks on every public type or member that was added or modified during this implementation — not just the new ones. Stale descriptions on existing members are as harmful as missing ones because they are the source of truth for the generated API reference.

### Docusaurus concepts pages (`docs/topsy-turvy/docs/concepts/`)

Update a concepts page only if the feature materially changes what a toolchain component does at a high level, for example, if the parser now handles a new category of construct, or the runtime introduces a new execution mechanism.

Do **not** add language-spec detail to a concepts page. Keyword syntax, example programs, operator rules, type cast behaviour — anything that describes what the language does rather than how the component works — belongs in `SPEC.md` only, not in a concepts page.
