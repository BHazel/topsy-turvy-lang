# DEVELOPMENT.md
## Topsy Turvy Language — Technical Reference for Coding Agents

* **Last Updated:** 2026-05-22
* **Current Specification Version:** 0.2.0
* **Grammar Source of Truth:** `SPEC.md` — read this file for all grammar questions.
* **File Extension:** `.topsy`

---

## 1. File Inventory

| File | Role |
|---|---|
| `AGENTS.md` | Project description, instructions to do before starting work and any constraints and rules that must be followed. |
| `SPEC.md` | Authoritative language specification v0.2.0. Full grammar, complete keyword reference, type system, worked examples. All grammar questions are resolved by this file. |
| `DEVELOPMENT.md` | Session state and file inventory. |
| `examples/hello_world.topsy` | Basic output and string concatenation |
| `examples/fizzbuzz.topsy` | While loop, modulo, conditionals (including inline form), boolean type, expression casting |
| `examples/fibonacci.topsy` | Recursive and iterative functions, line continuation |
| `examples/pirates_calculator.topsy` | Interactive loop, switch/case, multiple functions, division-by-zero handling |

All `.topsy` source files are fully compliant with `SPEC.md` v0.2.0.

---

## 2. Last Session Summary

**Session date:** 2026-05-22
**Spec version produced:** 0.2.0

### What was done this session

**Changes to the language:**

- `PRINCIPALS` block given an explicit closer `THE CURTAIN RISES.`; inline `PRAY WELCOME` outside `PRINCIPALS` documented as a free-standing statement requiring no closer
- `PRE-ADAMITE x AND y` (greater-than) and `LOWER DEGREE x AND y` (less-than) added as direct comparison operators; `>=`/`<=` expressed via `HARDLY EVER LOWER DEGREE` / `HARDLY EVER PRE-ADAMITE`
- `SHOULD IT TRANSPIRE THAT` and `IN WHICH CAPACITY?` extended with an inline form — expression supplied directly on the same line, bypassing `JUST SO`; both two-line and inline forms are valid
- `THAT WILL DO.` unified as the universal break keyword for both loops and switch; `FREE FROM THIS QUANDARY.` retired
- Loop label made optional — `KNOWN AS <label>` is now `[KNOWN AS <label>]` in all loop forms
- `ONCE MORE.` added as the loop continue statement, valid in all loop forms
- `A HIDEOUS CURSE ON <value>` added as a throw statement, valid anywhere in the programme; if uncaught, terminates the programme with an error; the caught value is available as `JUST SO` on entry to `MODIFIED RAPTURE`
- `WITH THE GREATEST RESPECT,` question mark removed from the syntax
- `PRAY ADMIT "filename"` replaces `PRAY SUMMON THE SERVICES OF "filename"`
- `~` (the Victorian flourish) fully documented in §18 covering both roles: line continuation (end of line) and string escape prefix (inside `YARN` literals); cross-reference added in §6
- Keywords made case-insensitive; uppercase confirmed as the conventional and recommended style
- `IF YOU PLEASE.` given a dedicated explanatory note in §6 and a standalone entry in §14, clarifying its role as the formal closer for any variable-length argument list (`WOVEN OF`, `SUMMON`, `ALL OF`, `ANY OF`)

**Keyword retirements this session:**
All retired keywords and their replacements are recorded in the `AGENTS.md` retired keywords table.

- `FREE FROM THIS QUANDARY.` → `THAT WILL DO.` (loop break)
- `PRAY SUMMON THE SERVICES OF` → `PRAY ADMIT` (import)

**Files modified:**

- `SPEC.md` — all changes above; version bumped to 0.2.0
- `AGENTS.md` — grammar invariant I5 updated (loop label now optional); I3 updated (`FREE FROM THIS QUANDARY.` removed, `THE CURTAIN RISES.` and `ONCE MORE.` added); two retired keyword rows added
- `examples/hello_world.topsy` — `THE CURTAIN RISES.` added after `PRINCIPALS`
- `examples/fizzbuzz.topsy` — `THE CURTAIN RISES.` added; judgement conditional converted to inline form as a demonstration
- `examples/fibonacci.topsy` — `THE CURTAIN RISES.` added after `PRINCIPALS`
- `examples/pirates_calculator.topsy` — `THE CURTAIN RISES.` added after `PRINCIPALS`; `FREE FROM THIS QUANDARY.` replaced by `THAT WILL DO.`

### Current state

The language grammar is complete and internally consistent across `SPEC.md` v0.2.0
and all four example files. No interpreter or compiler implementation exists.
The grammar has not been expressed as a formal BNF or EBNF grammar file.

### Known gaps and deferred items

**Arithmetic operator G&S attribution** — The operators `SUM OF`, `DIFFERENCE OF`,
`PRODUCT OF`, `QUOTIENT OF`, `REMAINDER OF`, `LARGER OF`, `SMALLER OF` have no
specific G&S source attribution in the spec. This is a known gap and is under
active investigation for a future session.

**No formal grammar file** — No BNF or EBNF representation of the grammar exists.

**No interpreter** — No implementation in any host language exists.
