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

**I4 — `JUST SO` is the sole implicit variable.**
No other implicit accumulator exists. `JUST SO` receives the result of any
expression not explicitly assigned. Conditionals and switch always read from
`JUST SO`.

**I5 — Loop labels are optional.**
`BY A LEGAL FICTION` may optionally be followed by `KNOWN AS <label>` in all
loop forms (infinite, ascending, descending, whilst). When present, the label
has no semantic effect beyond documentation. The loop is valid with or without
a label.

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

- **Root Directory:** `interpreter/`
- **Solution File:** `BWHazel.TopsyTurvy.slnx`
- **Project Layout:** Each project in its own directory: `interpreter/[ProjectName]/[ProjectName].csproj`.
- **Namespace Mirroring:** Folder structure must strictly mirror the namespace hierarchy.

### Coding Style

- **Explicit Typing:** Prefer explicit types over the `var` keyword.
- **File Granularity:** Strictly one type per file.
- **Naming:** Concise but descriptive variable names.
- **Modern C#:** Use the latest C# features.
- **Exceptions:** Use `TopsyTurvyException` for language-specific errors.