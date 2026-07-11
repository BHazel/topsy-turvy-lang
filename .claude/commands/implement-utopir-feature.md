---
name: implement-utopir-feature
description: >
  Guides the complete implementation or modification of a UtopIR instruction
  or type — from the AST record through the parser, transformer, code
  generator, CIL emitter, EBNF grammar, and tests. Use this skill whenever a
  UtopIR instruction, operand form, or type is being added, changed, or
  removed in `specifications/UtopIR.md` and needs to be propagated through the
  UtopIR toolchain. Invoke it when asked to "add an instruction to UtopIR",
  "implement [instruction] in UtopIR", "extend UtopIR with X", "change the
  UtopIR were cast matrix", "add a UtopIR type", "sync the UtopIR grammar
  after a spec change", or "add/update UtopIR syntax highlighting in VS
  Code". This skill knows UtopIR's own layer order and the
  constraints specific to it — always use it rather than attempting a UtopIR
  change freehand, because the change touches five projects and a naming
  responsibility that is easy to place in the wrong layer. For a Topsy Turvy
  language-syntax change instead of a UtopIR instruction, use
  `implement-topsy-turvy-language-feature`.
---

# UtopIR: Implement or Modify Instruction or Type

You are about to add, change, or remove an instruction or type in UtopIR, the
flat intermediary representation Topsy Turvy lowers to for targeting
compilation backends (currently .NET CIL). UtopIR is a separate, smaller
toolchain from the main Topsy Turvy one: five `.NET` projects
(`UtopIR.Ast`, `UtopIR.Parser`, `UtopIR.Transformer`, `UtopIR.Analysis`,
`UtopIR.Emitters.Cil`), plus a standalone VS Code TextMate grammar for
`.utopir` files (Step 3h) — no LSP, no completions, no REPL integration. A
change must still land consistently across all affected layers or the
transformer will produce instructions the parser cannot read back, the
emitter cannot compile, or a `.utopir` file renders with a broken half-token.

This applies equally to **new** instructions/types and to **modifications**
of existing ones. The layer order and verification steps are the same in
both cases; the only difference is whether each layer needs an addition, an
edit, or a deletion.

---

## Step 1: Load context (before touching any file)

Read all of these before doing anything else.

| File | Why you need it |
|---|---|
| `AGENTS.md` §Synchronisation Points → "New UtopIR Instruction or Construct" | Points back here; also carries the coding-style rules (XML docs, one type per file, named arguments) that apply to every layer below. |
| `DEVELOPMENT.md` | Current UtopIR component-status table (search "UtopIR"), current baseline test counts, non-obvious constraints. |
| `specifications/UtopIR.md` | The authoritative UtopIR specification. This is what you are implementing — read the section for the instruction/type you are adding in full, including its worked example. |
| `specifications/UtopIR.ebnf` | The formal grammar. After you implement the feature, it must match `UtopIR.md`. |

If the UtopIR change exists to support a *new* Topsy Turvy language feature
that has no UtopIR equivalent yet, run `/implement-topsy-turvy-language-feature`
first so the Topsy Turvy `Operator`/AST node the transformer needs to map
from already exists.

---

## Step 2: Identify the delta and confirm scope

Before writing a single line of code, compare `UtopIR.md` against the
current implementation and produce a brief delta summary:

* Is this a new instruction, a new operand form on an existing instruction,
  or a new `UtopIRType`? Each has a different blast radius (see Step 3).
* Is the instruction binary (two operands), unary (one operand), or
  standalone (no target register, e.g. `prentice`)? This decides the AST
  record shape and which existing instruction is the closest template to
  copy — `ArithmeticInstruction`/`BitwiseInstruction` (binary),
  `InvInstruction` (unary), `PrenticeInstruction` (standalone, no target).
* Does the Topsy Turvy operator this instruction lowers from have a
  **different arity** than the UtopIR instruction? (This happened for
  `TRANSPOSITION UP`/`TRANSPOSITION DOWN`: unary in Topsy Turvy, binary in
  UtopIR, with the shift amount synthesised as a literal `1` in the operand's
  own type.) If so, the transformer needs explicit arity-bridging logic, not
  just an operator-to-operation mapping.
* Does the instruction introduce a mnemonic family (e.g. the `.f`-suffixed
  floating-point arithmetic mnemonics) rather than a single mnemonic? If so,
  check whether the parser's `Or`-chain ordering matters — `Lexer.Keyword`
  has no trailing word-boundary check, so a longer mnemonic that shares a
  prefix with a shorter one (`sum.f` vs `sum`) must be tried first.
* Which operand types is the instruction valid for? Does it need the same
  same-type-in-same-instruction validation and widening the existing
  arithmetic/bitwise instructions already have, or different rules?
* Does this need a corresponding addition to the VS Code TextMate grammar
  (Step 3h)? Any new instruction mnemonic or `UtopIRType` does — it is easy
  to forget since it's the only editor-facing layer and lives in a different
  part of the repository (`extensions/vscode/`, not `operetta/`).

**Present this delta summary to the user and wait for their confirmation
before making any edits.**

---

## Step 3: Implement in layer order

Work through only the layers actually affected. Each layer depends on the
one above it, so do not skip ahead.

**XML documentation comments are required on every new or modified public
type and member throughout all steps below.** `<summary>` must be a single
sentence; use `<remarks>` for multi-sentence elaboration. For a new AST
instruction record, follow the exact structure already used by
`ArithmeticInstruction`/`BitwiseInstruction`/`InvInstruction`: a bulleted
list of the mnemonic(s) covered, the UtopIR source format line, a worked
Topsy Turvy → UtopIR example given first as *bullet points* describing the
emitted instruction sequence, then a C# `new XInstruction(...)` construction
example, then the rendered UtopIR source line. Do not describe the example
only in prose — the C# construction example is load-bearing documentation
for anyone implementing the transformer/emitter layers later.

### 3a. `UtopIR.Ast`

Add the new instruction record (one type per file, `sealed record` inheriting
`UtopIRInstruction`, positional parameters). If the instruction is one of a
related family (arithmetic, bitwise), add or extend the corresponding
operation enum (`UtopIRArithmeticOperation`, `UtopIRBitwiseOperation`) rather
than inventing a new one, unless the new instruction genuinely does not
belong to an existing family.

Add the mnemonic string constant(s) to `UtopIRKeywords.Instructions`, and any
new type keyword to `UtopIRKeywords.TypeNames` if the change also introduces
a `UtopIRType`. If it does introduce a new `UtopIRType`, add the enum value
to `UtopIRType.cs` with an XML doc line matching the existing "(Topsy Turvy
`X`, UtopIR `x`)" format, and add it to Appendix B of `UtopIR.md` if the spec
doesn't already cover it.

### 3b. `UtopIR.Parser`

Three files, in dependency order:

* **`Lexer.cs`** — only touch this if the feature needs a new *literal*
  shape (a new kind of token, not just a new keyword). Most instruction
  additions do not need lexer changes.
* **`OperandParser.cs`** — add a `TextParser<T>` combinator for the new
  mnemonic(s) or operand form, following the existing
  `ArithmeticOperation`/`BitwiseOperation` pattern:
  `Lexer.Keyword(...).Value(EnumValue).Or(...)`, chained longest/most-specific
  mnemonic first per the arity-bridging note in Step 2.
* **`InstructionParser.cs`** — add a private `*Rhs(string target)` parser
  (for an assignment-form instruction) or a public standalone parser (for a
  no-target instruction like `prentice`), then wire it into
  `AssignmentInstruction`'s `.Or(...)` chain or `StandaloneInstruction`'s
  chain. Update the XML doc's bullet list of right-hand-side parsers to
  describe the new one, matching the existing entries' one-line style.

**Build checkpoint:**
```
dotnet build operetta/BWHazel.TopsyTurvy.slnx --no-incremental
```

### 3c. `UtopIR.Transformer`

`TopsyTurvyToUtopIRTransformer.cs`:

* Add a `case` in `TransformExpression`'s dispatch (or wherever the relevant
  Topsy Turvy AST node type/operator is matched) routing to a new
  `Transform*` private method, following the pattern of
  `TransformArithmetic`/`TransformBitwise`.
* If operands can be mixed-type, widen via the existing `Widen`/
  `CastOperandIfNeeded` helpers before constructing the instruction — do not
  duplicate the widening logic.
* If the Topsy Turvy operator's arity differs from the UtopIR instruction's
  (see Step 2), synthesise the missing operand explicitly and matched to the
  correct CLR/`UtopIRType` (see `CreateOneLiteral` for the shift-amount
  precedent).
* **Naming responsibility**: call `this.formatter.CreateName(mnemonic,
  operandPart1, operandPart2, ...)` to name the temporary register. Do
  **not** pre-sanitise the mnemonic or operand strings in the transformer —
  how a mnemonic's punctuation is rendered (e.g. whether a `.` is dropped or
  replaced) is `ITemporaryVariableNameFormatter`'s responsibility, since a
  different formatter implementation may render it differently or ignore it
  entirely (`IncrementingIntVariableFormatter` ignores both arguments). This
  was moved out of the transformer once already — do not reintroduce
  formatter-specific string logic here.
* Add a mnemonic-lookup helper (`OperationMnemonic`-style) if the new
  instruction is part of an operation-enum family; this is also what gets
  passed as `CreateName`'s first argument.

### 3d. `UtopIR.Analysis`

`UtopIRCodeGenerator.cs`: add a `case` in `WriteInstruction`'s switch that
renders the instruction's UtopIR source line, and a mnemonic-lookup switch
mirroring the transformer's if the instruction belongs to an operation-enum
family. Keep the rendered format byte-for-byte identical to what
`InstructionParser` accepts — round-tripping through `UtopIRCodeGenerator`
then `UtopIRParser` must reproduce the same AST.

### 3e. `UtopIR.Emitters.Cil`

`CilEmitter.cs` is the largest layer to touch, and has more than one
extension point depending on what changed:

**New instruction**, add:
* A `case` in `EmitInstruction`'s dispatch switch, calling a new `Emit*`
  method.
* In the new `Emit*` method: validate operand types match and are of the
  expected family (integer/float/etc.), throwing `InvalidOperationException`
  defensively even though the transformer is supposed to guarantee this —
  see `EmitArithmetic`'s and `EmitBitwise`'s remarks for why. If the target
  register may not have been explicitly declared with a `WelcomeInstruction`
  (true for any instruction whose result feeds a temporary register), "auto
  declare" it: `if (!locals.TryGetValue(...)) { declare + RegisterLocalName
  + record its type }`.
* Select opcodes via a dedicated `Emit*Opcode` helper if the instruction is
  part of a family (mirrors `EmitArithmeticOpcode`/`EmitBitwiseOpcode`), not
  inline in the dispatch method.

**New `UtopIRType`** (whether or not it also adds a new instruction), check
all four of these switches — they are the actual extension points for a
type, not the instruction dispatch:
* `MapToClrType` — the UtopIR type's CLR representation.
* `EmitConversion` — the `conv.*` opcode for casting *to* this type via
  `were`. Look for an existing type with the same CIL stack representation
  (e.g. `stitch`/`char` reuses `standingpirate`/`ushort`'s `conv.u2`, since
  both are 16-bit unsigned on the CIL stack) before assuming a new opcode is
  needed.
* `EmitLoadLiteralValue` — the `ldc.*` opcode for a literal of this type.
  Loading and converting are different concerns with potentially different
  opcodes for the same type (again, `char`: `ldc.i4` to load, `conv.u2` to
  convert into) — document why if they differ, briefly.
* `InferOperandType`/`IsIntegerType`/`IsFloatType`/`IsUnsignedType` (or a new
  predicate, if the type doesn't fit an existing category) — whichever
  category predicates decide validation and opcode selection elsewhere in
  the file need the new type added to their `is X or Y or ...` pattern.

**Build checkpoint:**
```
dotnet build operetta/BWHazel.TopsyTurvy.UtopIR.Emitters.Cil/BWHazel.TopsyTurvy.UtopIR.Emitters.Cil.csproj
```

### 3f. `specifications/UtopIR.ebnf`

Add or update the production rules to match `UtopIR.md` and the parser. Keep
a comment noting any ordering constraint from Step 2 (mnemonic-prefix
collisions), the way `ArithmeticOp`'s split into `IntegerArithmeticOp`/
`FloatArithmeticOp` already documents the `.f`-before-plain requirement.

### 3g. `BWHazel.TopsyTurvy.Cli`

`UtopIRInstructionJsonConverter`/`UtopIROperandJsonConverter` are
reflection-based and pick up new instruction/operand record types
automatically — check but do not expect to need changes here. The
`OptionsResolvers/` (`UtopIrResolver`, `UtopIrAstResolver`,
`DotNetCilOptionsResolver`, `VariableFormatConfig`) are instruction-agnostic
plumbing and also should not need per-instruction changes; if a change here
does seem to be needed, that is a signal something instruction-specific has
leaked into a layer that is supposed to be generic.

### 3h. VS Code TextMate grammar (`extensions/vscode/topsy-turvy`)

UtopIR has no language server, so `.utopir` files get syntax highlighting
purely from a TextMate grammar — `extensions/vscode/topsy-turvy/syntaxes/utopir.tmLanguage.json`
(`scopeName: "source.utopir"`). It is organised one `repository` group per
`UtopIR.md` §4.x subsection (`declaration`/`assignment`/`cast` for §4.1,
`arithmetic-integer`/`arithmetic-float` for §4.2, `bitwise` for §4.3,
`stack` for §4.4, `control-flow` for §4.5), plus `types` for
`UtopIRKeywords.TypeNames` and `variables` for the `£`-sigil identifier
fallback. A new instruction within an existing family extends that group's
alternation; a genuinely new §4.x subsection gets its own new group and a
new top-level `{"include": ...}` entry.

**The `.f`-before-plain ordering trap applies here too**, for the identical
reason already documented on `OperandParser.ArithmeticOperation` (3b):
TextMate/Oniguruma alternations are first-match-wins, and `\bsum\b` matches
successfully against `sum.f` since `.` is a non-word character forming a
false boundary — the same class of bug already shows up a third time in this
grammar's own `numbers` group (float pattern listed before integer for
exactly this reason). If a new instruction introduces a `.f`-suffixed (or
any other prefix-sharing) mnemonic family, its repository group's
`{"include": ...}` entry must appear *before* the plain-mnemonic group's
entry in the top-level `patterns` array — this constraint travels with
*include order* here, not with a single regex's alternative order, since the
two mnemonic families live in separate repository groups.

If the change adds a new `UtopIRType` (3a), add its literal
`UtopIRKeywords.TypeNames` string to the `types` group's alternation. Unlike
the `.f` case, there is no prefix-collision ordering concern among type
names (`standingpeer` is not a string-prefix of `peer`, or vice versa) —
append in spec order for readability; position doesn't affect correctness.

Add a fixture to `extensions/vscode/topsy-turvy/tests/grammar/`, extension
`.utopir-test`, using the same `// SYNTAX TEST "source.utopir"` + `//^`
caret-annotation format as the `.topsy-test` files (one file per semantic
category: `comments`, `literals`, `keywords`, `variables`). **Caret
alignment gotcha**: the assertion line overlays the source line directly,
column for column — the `^` character's own absolute column in the
assertion line *is* the source column being tested, with no adjustment for
the leading `//`. This only looks like it works differently for something
like `PEER\n//^ storage.type.topsy` because the whole word is one token, so
any column inside it passes regardless of precision. For a narrow assertion
(one escape character, one suffix), get the column exactly right or the
check silently targets the wrong character. Also keep every marker
substring unique within its source line — a marker like `"t"` used to
target the `~t` escape in `£x = appoint "~t"` will instead match the `t`
inside `appoint` if that occurs first in the string. For a `.f`-family
addition specifically, put the caret on the `.f` suffix itself (not the
mnemonic prefix) — that is the regression guard for the ordering trap above.

**Grammar test checkpoint:**
```
cd extensions/vscode/topsy-turvy && npm run test:grammar:utopir
```

This is grammar-only. Do not add LSP registration, `activationEvents`
entries, or completion providers as part of this step — `contributes.languages`/
`contributes.grammars` tokenize declaratively without activating the
extension, so none of that is needed just to light up highlighting. If
UtopIR ever gets an LSP, that is a separate, much larger change.

---

## Step 4: Write tests

Add tests to `operetta/BWHazel.TopsyTurvy.UtopIR.Tests/`, one subdirectory
per layer (`Parser/`, `Transformer/`, `Analysis/`, `Emitters/`), following
existing conventions:

* One test class per production type; method naming
  `{MethodOrProperty}_{Condition}_{ExpectedOutcome}`.
* `[Theory]`/`[InlineData]` for covering every mnemonic in a family in one
  method (see `AssignmentInstruction_WithArithmeticMnemonic_ReturnsCorrectOperation`
  for the pattern).
* Emitter tests that assert actual execution results build and run a real
  in-memory assembly via `CilEmitter`/`Assembly.LoadFrom`/reflection
  invocation (see `RunProgram`/`RunProgramWithIlSource` helpers in
  `CilEmitterTests`) — do not just assert on `CilEmitResult.IlSource`
  text unless the specific opcode choice is what's under test.
* Cover: happy-path parse, transform (including any widening/casting
  behaviour), code generation round-trip, CIL emission with a real exit-code
  assertion, and the validation error case (wrong/mismatched operand types)
  if the instruction has one.

Add at least one full-pipeline test to
`operetta/BWHazel.TopsyTurvy.UtopIR.E2ETests/` — either
`TopsyTurvyUtopirCilPipelineIntegrationTests.cs` (Topsy Turvy source →
transform → generate → emit → run) if the feature is reachable from Topsy
Turvy source, or `UtopIrSourcePipelineIntegrationTests.cs` (hand-written
`.utopir` source → parse → emit → run) if it is UtopIR-only.

**Full test run checkpoint:**
```
dotnet test operetta/BWHazel.TopsyTurvy.UtopIR.Tests
dotnet test operetta/BWHazel.TopsyTurvy.UtopIR.E2ETests
```
If the change is reachable from Topsy Turvy source, also run:
```
dotnet test operetta/BWHazel.TopsyTurvy.Tests
dotnet test operetta/BWHazel.TopsyTurvy.Cli.E2ETests
```
Check `DEVELOPMENT.md` for the current baseline — new tests should push the
numbers up, never down.

---

## Step 5: Add an example (if appropriate)

If the feature is substantial enough to warrant a standalone demonstration,
add a worked example to `UtopIR.md` §5 following the existing structure: a
Topsy Turvy source block, a "the following is the equivalent UtopIR code"
sentence, then the UtopIR source block. Keep the "verbose"/"numeric" naming
style split already used in §5.1/§5.2 if the example is long enough to
benefit from both.

---

## Step 6: Update DEVELOPMENT.md

This is the last step, after everything else is green:

* **Header** — update the "Last Updated" date and summarise the change in
  one clause, in a new entry (never edit a past dated entry to reflect a
  later change — add a new one instead, even to correct an earlier one).
* **UtopIR component-status table rows** — update the version tag (e.g.
  `v0.0.1-previewN`) and one-line summary for every UtopIR project row this
  change touched.
* **§1 File Inventory** — add or update rows for any new or significantly
  changed files, including `specifications/UtopIR.ebnf` and, if touched,
  `extensions/vscode/topsy-turvy/syntaxes/utopir.tmLanguage.json`.
* **Baseline counts** — update test numbers to reflect the new passing
  total for `BWHazel.TopsyTurvy.UtopIR.Tests` and
  `BWHazel.TopsyTurvy.UtopIR.E2ETests`.

---

## Step 7: Update documentation

### XML documentation comments

Review the `<summary>` and `<remarks>` blocks on every public type or member
that was added or modified — not just the new ones. Stale descriptions on
existing members (e.g. a `<remarks>` block on `CilEmitter` claiming only
integer types are supported, after a feature adds floating-point support) are
worse than no comments, since they are the source of truth for the generated
API reference.

### Docusaurus concepts pages (`docs/topsy-turvy/docs/concepts/`)

Update a concepts page only if a UtopIR component's high-level behaviour has
materially changed — for example, if the emitter now supports a new backend
concern, or the transformer's widening algorithm changes. Do **not** add
UtopIR-spec detail (mnemonic syntax, example programs, cast rules) to a
concepts page; that content belongs in `specifications/UtopIR.md` only.
