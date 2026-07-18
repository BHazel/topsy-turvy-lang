---
sidebar_position: 1
---

# Grammar

A programming language needs two things to be well-defined: a human-readable specification that explains what every construct means, and a formal grammar that defines precisely what the language looks like in text.  For Topsy Turvy these are:

* **`specifications/TopsyTurvy.md`:** The authoritative language specification, written in prose in human-friendly language and for those who want to understand what the language does.  It describes every keyword, its meaning, examples and edge cases.
* **`specifications/TopsyTurvy.ebnf`:** The formal grammar, derived from `TopsyTurvy.md`, for tools to understand what a valid programme looks like.  It gives an exact, unambiguous definition of what a valid Topsy Turvy programme looks like.

## The Language Specification (TopsyTurvy.md)

`TopsyTurvy.md` is the primary reference for writing Topsy Turvy programmes.  It is structured into sections for each language feature and for each feature explains:

* **The Syntax:** What keywords to use and in what order.
* **The Semantics**: What the construct actually does when the programme runs.
* **Examples:** Concrete code snippets showing the construct in use.

When `TopsyTurvy.md` and `TopsyTurvy.ebnf` disagree, `TopsyTurvy.md` is the authority.  The grammar captures the shape of the language precisely, but it does not capture everything: semantic rules such as type casting behaviour, scoping, _truthiness_ and the meaning of each keyword are documented only in `TopsyTurvy.md`.

## The Grammar (TopsyTurvy.ebnf)

`TopsyTurvy.ebnf` is written in  **Extended Backus-Naur Form (EBNF)**, a standard notation for writing down the grammar of any language.  It was originally developed in the 1960s as Backus-Naur Form (BNF) by John Backus and Peter Naur to describe the syntax of ALGOL 60, one of the first high-level programming languages.  The "extended" variant adds shorthand for optional parts and repetition, making grammars more concise.

A grammar written in EBNF is a set of **rules**.  Each rule defines a named language construct by saying what it must look like in terms of literal text and other named constructs.  Together the rules describe every valid programme in the language, starting from the top-level `Program` rule and expanding down to individual characters.

The specification and grammar are implemented by the _Operetta Toolchain_ in the `BWHazel.TopsyTurvy` solution.

## Reading the Grammar

### Rules

Every rule in the grammar has the form:

```
RuleName = definition ;
```

The rule **name** is on the left of `=`.  The **definition** is on the right.  A semicolon `;` ends the rule.  For example:

```ebnf
Type = "PEER" | "FATHOM" | "YARN" | "DECREE" ;
```

This says that a `Type` is one of the Topsy Turvy type keywords (simplified here, please see `Type` in `TopsyTurvy.ebnf` for the full, current set).

### Terminals and Non-Terminals

A grammar uses two kinds of element:

* **Terminals** are literal text that appears verbatim in the source code.  In EBNF they are written in double quotes: `"PRAY WELCOME"`, `"AS A"`, `"VERITY"`.  They are called terminals because they are final, irreducible tokens: you cannot expand them further.
* **Non-terminals** are names of other rules: `Identifier`, `Type`, `Expression`.  They are expanded by looking up their own rule definition.  Non-terminals are written without quotes.

`TopsyTurvy.ebnf` also uses character-class notation (similar to regular expressions) for individual characters:

```ebnf
Identifier = [a-zA-Z], { [a-zA-Z0-9_-] } ;
```

Here `[a-zA-Z]` means "any single letter", and `[a-zA-Z0-9_-]` means "any letter, digit, hyphen or underscore".

### Notation Reference

| Symbol | Name | Meaning |
|-|-|-|
| `=` | Definition | Assigns a definition to a rule name. |
| `;` | Terminator | Ends a rule. |
| `,` | Concatenation | The items on either side appear one after another. |
| `\|` | Alternation | Exactly one of the alternatives is chosen. |
| `[ ... ]` | Option | The contents appear zero or one times (optional). |
| `{ ... }` | Repetition | The contents appear zero or more times. |
| `( ... )` | Grouping | Groups elements so that alternation and concatenation apply to the group as a whole. |
| `" ... "` | Terminal string | Text that appears literally in the source. |
| `(* ... *)` | Comment | A note in the grammar file; not part of the language. |

### Worked Example

The following rule defines what every Topsy Turvy programme must look like:

```ebnf
Program = "HARK!", StringLiteral, [ "\n", "or,", StringLiteral ],
          StatementList, "FINALE." ;
```

Reading left to right, `,` means "followed by":

1. `"HARK!"`: The literal keyword `HARK!`.
2. `StringLiteral`: The programme title, which is itself a rule, a quoted string.
3. `[ "\n", "or,", StringLiteral ]`: Optionally a newline then `or,` then the subtitle string.
    * The square brackets mean this entire clause may be omitted.
4. `StatementList`: The body of the programme defined as `{ Statement }`, zero or more statements.
5. `"FINALE."`: The closing keyword.

This corresponds to the familiar programme shape:

```
HARK! "The Title"
  or, "The Optional Subtitle"

  ... body ...

FINALE.
```

As a second example, consider how a variable declaration is defined:

```ebnf
MutabilityModifier  = "CONSERVATIVE" | "LIBERAL" ;

Declaration = "PRAY WELCOME", Identifier, "AS A", [ MutabilityModifier ], Type,
              [ "BEING", Expression ] ;
```

Reading left to right:

1. `"PRAY WELCOME"`: The variable declaration keyword.
2. `Identifier`: The variable name.
3. `"AS A"`: The type annotation keyword.
4. `[ MutabilityModifier ]`: Optionally `CONSERVATIVE` (constant) or `LIBERAL` (explicitly mutable).
    * The square brackets mean the modifier may be omitted; if absent the variable is mutable by default.
5. `Type`: The type keyword, for example `PEER`, `FATHOM`, `YARN` or `DECREE`.
6. `[ "BEING", Expression ]`: Optionally `BEING` followed by an initial value.
    * The square brackets mean the initial value clause may be omitted; if absent the variable is initialised to the default value for its type, for example `0` for `PEER`.  Please see the [Runtime](./runtime.md) page for more information.

Finally, alternation with grouping:

```ebnf
LoopType = ( "ASCENDING", Identifier, "UNTIL", Expression )
         | ( "DESCENDING", Identifier, "UNTIL", Expression ) ;
```

The `|` between the two groups means a loop header is either the ascending form or the descending form.  Both groups are in parentheses so that the `|` applies to each entire form rather than just the adjacent terms.
