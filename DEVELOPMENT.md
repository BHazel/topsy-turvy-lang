# DEVELOPMENT.md
## Topsy Turvy Language — Technical Reference for Coding Agents

**Last Updated:** 2026-05-21
**Current Specification Version:** 0.1.0
**Grammar Source of Truth:** `SPEC.md` — read this file for all grammar questions.
**File Extension:** `.topsy`

---

## 1. File Inventory

| File | Role |
|---|---|
| `SPEC.md` | Authoritative language specification v0.1.0. Full grammar, complete keyword reference, type system, worked examples. All grammar questions are resolved by this file. |
| `DEVELOPMENT.md` | Agent operational reference. Session state, retired keywords, grammar invariants. Not a grammar source. |
| `examples/hello_world.topsy` | Basic output and string concatenation |
| `examples/fizzbuzz.topsy` | While loop, modulo, conditionals, boolean type, expression casting |
| `examples/fibonacci.topsy` | Recursive and iterative functions, line continuation |
| `examples/pirates_calculator.topsy` | Interactive loop, switch/case, multiple functions, division-by-zero handling |

All `.topsy` source files are fully compliant with `SPEC.md` v0.1.0.

---

## 2. Last Session Summary

**Session date:** 2026-05-21
**Spec version produced:** 0.1.0 (first stable release)

### What was done this session

This was the founding session. The Topsy Turvy language was designed and
specified from scratch across a single working session.

**Files created:**
- `SPEC.md` — full language specification, revised through four beta drafts to v0.1.0
- `examples/hello_world.topsy`
- `examples/fizzbuzz.topsy`
- `examples/fibonacci.topsy`
- `examples/pirates_calculator.topsy`
- `DEVELOPMENT.md` (this file)

**Key structural decisions made:**

- Prefix notation adopted throughout for all arithmetic and boolean operators
- `IF YOU PLEASE.` adopted as the universal expression-list closer, applied consistently to `WOVEN OF`, `SUMMON`, `ALL OF`, and `ANY OF`
- `JUST SO` adopted as the sole implicit accumulator variable; feeds conditionals and switch
- Boolean type name (`DECREE`) separated from boolean literals (`VERITY` / `NAY`) to avoid ambiguity
- `WHILST` retained as the continue-while-true loop condition; `UNTIL` used as the exit-when-true condition — both coexist
- Function scope is not closured; parameters are the only value-input mechanism
- `.topsy` retained as the file extension despite the full language name being Topsy Turvy

**Keyword revisions made during this session:**
All keywords were established and revised iteratively. The complete record of
every retired keyword and its replacement is in §3 below.

### Current state

The language grammar is complete and internally consistent across `SPEC.md`
and all four example files. No interpreter or compiler implementation exists.
The grammar has not been expressed as a formal BNF or EBNF grammar file.

### Known gaps and deferred items

**Arithmetic operator G&S attribution** — The operators `SUM OF`, `DIFFERENCE OF`,
`PRODUCT OF`, `QUOTIENT OF`, `REMAINDER OF`, `LARGER OF`, `SMALLER OF` have no
specific G&S source attribution in the spec. This is a known gap and is under
active investigation for a future session.

**No formal grammar file** — No BNF or EBNF representation of the grammar exists.

**No interpreter** — No implementation in any host language exists.

---

## 3. Retired Keywords

The following keywords appeared in earlier drafts and have been replaced. They
must not be used in any `.topsy` source file or spec example.

| Retired keyword | Replaced by | Construct |
|---|---|---|
| `ITZ` | `BEING` | Initial value in declaration |
| `NOWT` | `NAUGHT` | Null type and null value |
| `MAYHAPS` | `AS IT WERE` | Expression cast |
| `COMPOUND OF...RESOLVED.` | `WOVEN OF...IF YOU PLEASE.` | String concatenation |
| `ON THE CONTRARY x` | `HARDLY EVER x` | Logical NOT |
| `STAND DOWN.` | `FREE FROM THIS QUANDARY.` | Loop break |
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
| `PRAY ENGAGE THE SERVICES OF` | `PRAY SUMMON THE SERVICES OF` | Import |
| `PRAY INTRODUCE` | `PRAY WELCOME` | Variable declaration |
| `VERITY` (type name) | `DECREE` | Boolean type |
| `WIN` | `VERITY` | Boolean true literal |
| `FAIL` | `NAY` | Boolean false literal |

---

## 4. Grammar Invariants

The following rules must be preserved in any modification to the language or
its examples. They reflect deliberate structural decisions that affect all
constructs simultaneously.

**I1 — Prefix notation throughout.**
All operators (arithmetic, boolean, string) use prefix notation with `AND` as
the argument separator. No infix or postfix operators exist.

**I2 — `IF YOU PLEASE.` is the universal expression-list closer.**
`WOVEN OF`, `SUMMON`, `ALL OF`, and `ANY OF` all close with `IF YOU PLEASE.`
No other closer is used for these constructs.

**I3 — Full stops are mandatory on specific keywords.**
The following keywords require a trailing full stop as part of their token and
are not valid without it:
`FINALE.` `QUITE SO.` `SO MUCH FOR THAT.` `THAT WILL DO.`
`NOTHING COULD BE MORE SATISFACTORY.` `FREE FROM THIS QUANDARY.`
`THE TERM EXPIRES.` `MY DUTY IS DISCHARGED.`
`MY DUTY IS PREMATURELY DISCHARGED.` `IF YOU PLEASE.`

**I4 — `JUST SO` is the sole implicit variable.**
No other implicit accumulator exists. `JUST SO` receives the result of any
expression not explicitly assigned. Conditionals and switch always read from
`JUST SO`.

**I5 — All loops require a label.**
`BY A LEGAL FICTION KNOWN AS <label>` requires a non-empty label identifier in
all loop forms (infinite, ascending, descending, whilst). The label has no
semantic effect beyond documentation but is syntactically required.

**I6 — Switch operates on literals only.**
`WHEN ACTING AS` accepts literal values only — strings, integers, floats,
`VERITY`, `NAY`. Expressions are not valid as case labels.

**I7 — Functions are not closures.**
Functions receive values exclusively through their declared parameters.
Global variables declared in `PRINCIPALS` are accessible everywhere except
inside functions.

**I8 — `PRAY WELCOME` is the sole declaration form.**
Variables may only be declared with `PRAY WELCOME ... AS A ... [BEING ...]`.
There is no implicit declaration; using an undeclared name is an error.

**I9 — The boolean type is `DECREE`; its literals are `VERITY` and `NAY`.**
`VERITY` and `NAY` are not interchangeable with `1`/`0` or `NAUGHT` in typed
contexts. Truthiness coercion applies only when a non-`DECREE` value is used
in a boolean context.

**I10 — `.topsy` is the sole source file extension.**
No other extension is valid. The language name is Topsy Turvy; the extension
remains `.topsy`.
