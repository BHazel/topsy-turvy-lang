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
| `GRAMMAR.ebnf` | Formal EBNF grammar of the language, serving as the blueprint for the parser. |
| `examples/hello_world.topsy` | Basic output and string concatenation |
| `examples/fizzbuzz.topsy` | While loop, modulo, conditionals (including inline form), boolean type, expression casting |
| `examples/fibonacci.topsy` | Recursive and iterative functions, line continuation |
| `examples/pirates_calculator.topsy` | Interactive loop, switch/case, multiple functions, division-by-zero handling |
| `interpreter/` | Root directory for the .NET 10 interpreter implementation. |

---

## 2. Last Session Summary

**Session date:** 2026-05-22
**Phase completed:** Phase 0 (Foundational Specification & Infrastructure)

### What was done this session

**Implementation Planning & Setup:**
- **Formal Grammar:** Authored `GRAMMAR.ebnf` to provide an unambiguous blueprint for the parser, specifically resolving prefix recursion and variadic closures (`IF YOU PLEASE.`).
- **Infrastructure Setup:** 
    - Created the `interpreter/` directory.
    - Initialized the .NET 10 solution `BWHazel.TopsyTurvy.slnx`.
    - Setup four core projects: `BWHazel.TopsyTurvy.Ast`, `BWHazel.TopsyTurvy.Parser`, `BWHazel.TopsyTurvy.Runtime`, and `BWHazel.TopsyTurvy.Cli`.
- **Architectural Decisions:**
    - **LSP-Ready:** Designed the parser to support source mapping and diagnostic collections for future Language Server integration.
    - **Platform Agnostic:** Integrated an `ITopsyIO` abstraction to decouple the runtime from the console, enabling future web (Blazor) front-ends.
- **Engineering Standards:** Established and recorded strict coding styles in `AGENTS.md`:
    - Root Namespace: `BWHazel.TopsyTurvy` with mirrored folder structure.
    - Explicit typing (no `var`), one type per file, and usage of .NET 10 / C# 13+ features.
    - Custom exception: `TopsyTurvyException`.

**Files Added/Modified:**
- `AGENTS.md` — updated with detailed implementation standards and directory layout.
- `GRAMMAR.ebnf` — created the formal language grammar.
- `interpreter/` — created solution, projects, and directory structure.

### Current state

Phase 0 is complete. The foundational blueprint and project infrastructure are established. The project has transitioned from the specification phase to the implementation phase.

### Next Phases

**Phase 1: Foundation & Design**
- Implement `SourceSpan` and diagnostic models in `BWHazel.TopsyTurvy.Ast`.
- Define the AST node hierarchy aligned with the EBNF grammar.
- Define the `ITopsyIO` interface.

**Phase 2: The Parser**
- Implement the "Victorian Flourish" (`~`) line-continuation pre-processor.
- Develop the `Superpower` monadic parser based on the EBNF grammar.

**Phase 3: The Runtime**
- Implement the `Environment`, `JustSoRegister`, and the recursive AST evaluator.
- Implement the "Curse" mechanism (`TopsyTurvyException`) and scoping rules.

**Phase 4: CLI & Integration**
- Implement the CLI entrance using `System.CommandLine` and `Spectre.Console`.
- Implement the `PRAY ADMIT` multi-file loader.
- Validate the interpreter against the `examples/` suite.
