# Topsy Turvy
## A Gilbert & Sullivan Operetta Programming Language
### Language Specification — Version 0.6.0

> *"Things are seldom what they seem; skim milk masquerades as cream."*
> — H.M.S. Pinafore

---

## Overview

**Topsy Turvy** is a general-purpose esoteric programming language in the tradition of LOLCODE, themed around the Gilbert & Sullivan operetta canon. Programs are written in the voice of a Victorian theatrical libretto — formal, pompous, comic, and entirely deadpan. The underlying semantics are those of a conventional statically-typed procedural language; only the syntax is topsy-turvy.

Source files use the `.topsy` extension.

The name derives from *topsy-turvy* — Gilbert's own word for his dramatic method: the establishment of an absurd premise, followed with absolute logical rigour and a straight face.

---

## 1. Program Structure

Every Topsy program is structured as a G&S operetta. The framing keywords are mandatory.

```topsy
HARK! "The Title of the Piece"
  or, "The Subtitle Explaining the Situation"

  ...body...

FINALE.
```

- **`HARK!`** — Opens the program. The string literal that follows is the program title (a comment, not evaluated). The `or,` subtitle line is optional and also a comment.
- **`AND SO I FIND <value>`** — Optional. Sets the OS exit code returned to the shell when the programme terminates. The expression must evaluate to a `PEER` (integer) value. If omitted, the exit code is `0`. This statement may appear anywhere in the programme body before `FINALE.`; execution unwinds immediately when it is reached.
- **`FINALE.`** — Closes the program and exits. The full stop is mandatory.
- Everything between `HARK!` and `FINALE.` is executed in order.
- Indentation is optional and has no semantic meaning. Indentation in examples follows libretto convention.
- Keywords are case-insensitive. `BEHOLD`, `Behold`, and `behold` are all equally valid. The examples in this specification use uppercase throughout, which is the conventional and recommended style.

### Minimal Program

```topsy
HARK! "The Shortest Operetta"

FINALE.
```

---

## 2. Comments

### Single-Line Comments

```topsy
ASIDE: this is a single-line comment and is ignored by the interpreter
```

`ASIDE:` is the theatrical aside — a remark addressed to the audience, explicitly not heard by the other characters on stage, and equally ignored by the interpreter. Gilbert uses `(Aside)` as a stage direction throughout every libretto: *"(Aside.) Shall I tell her?"* — Nanki-Poo, *The Mikado*, Act I. Everything from `ASIDE:` to the end of the line is ignored.

### Multi-Line Comments

```topsy
(ASIDE, AT SOME LENGTH:
  This is a block comment. It may run to as many lines
  as the occasion demands. The interpreter, like the other
  characters on stage, hears none of it.
END OF ASIDE.)
```

`(ASIDE, AT SOME LENGTH:` opens a block comment. `END OF ASIDE.)` closes it. Everything between is ignored. The form mirrors Gilbert's own parenthetical stage directions, which he routinely extended to considerable length with self-aware elaboration.

---

## 3. Variables & Declarations

### The PRINCIPALS Section

Variables are declared in the `PRINCIPALS` block, which mirrors the *Dramatis Personae* of a G&S libretto. `PRINCIPALS` may appear anywhere before first use, but by convention is placed at the top.

```topsy
PRINCIPALS
  PRAY WELCOME Ko-Ko          AS A PEER     BEING 0
  PRAY WELCOME Pooh-Bah       AS A YARN     BEING "Lord High Everything Else"
  PRAY WELCOME Mabel          AS A FATHOM   BEING 3.14159
  PRAY WELCOME is_guilty      AS A DECREE   BEING VERITY
  PRAY WELCOME mystery        AS A PEER
THE CURTAIN RISES.
```

**Syntax:**
```
PRINCIPALS
  PRAY WELCOME <name> AS A [CONSERVATIVE | LIBERAL] <type> [BEING <value>]
  ...
THE CURTAIN RISES.
```

- `PRAY WELCOME` — the formal welcoming of a new character onto the stage; `PRAY` drawn verbatim from *The Mikado*, Act I (*"Gentlemen, I pray you tell me..."*); `WELCOME` reflecting the theatrical tradition of receiving each new arrival before the assembled company, as in the *Dramatis Personae*
- `<name>` — any valid identifier (letters, digits, hyphens, underscores; must begin with a letter)
- `AS A [CONSERVATIVE | LIBERAL] <type>` — declares the type, with an optional mutability modifier (see §3.1 below)
- `BEING <value>` — optional initial value, in the manner of a *Dramatis Personae* parenthetical ("Nanki-Poo, *being* the son of the Mikado..."); if omitted, the variable is initialised to the type's default value (see §3.4)
- `THE CURTAIN RISES.` — closes the `PRINCIPALS` block. Once all characters have been introduced and the company is assembled, the curtain rises and the drama begins. The full stop is mandatory.

### 3.1 Constants and Mutability Modifiers

An optional mutability modifier may appear between `AS A` and the type keyword to declare whether a variable is a constant or a mutable variable:

| Modifier | Meaning |
|---|---|
| `CONSERVATIVE` | The variable is a **constant**. Once declared, it cannot be reassigned by `IS APPOINTED`. Attempting this operation is a runtime error. |
| `LIBERAL` | The variable is explicitly **mutable**. This is identical to declaring without a modifier and exists for documentation clarity. |
| *(none)* | Mutable — the default when no modifier is given. All existing code continues to work unchanged. |

```topsy
PRAY WELCOME LovesickMaidens AS A CONSERVATIVE PEER BEING 20
PRAY WELCOME TotalLords       AS A LIBERAL      PEER BEING  0
PRAY WELCOME Ko-Ko            AS A PEER         BEING  0
```

`CONSERVATIVE` draws from the G&S tradition of immovable institutional authority — the House of Lords in *Iolanthe*, the Lord Chancellor, the ancestral portraits in *Ruddigore*: things that, by long-established rule, simply *cannot* be changed. *"I often think it's comical / Fal lal la! / How Nature always does contrive / Fal lal la! / That every boy and every gal / That's born into the world alive / Is either a little Liberal / Or else a little Conservative! / Fal lal la!"* — *Iolanthe*, Act II. The modifier restores Gilbert's own distinction to the language: those values that are fixed by decree, and those that may yet be persuaded.

### 3.2 Unsigned Integer Types — The `STANDING` Modifier

The `STANDING` keyword may appear between the mutability modifier and any **integer** type keyword to declare an unsigned variant of that type:

| Declaration form | Width | Range |
|---|---|---|
| `STANDING PEER` | 32-bit unsigned | 0 to 4 294 967 295 |
| `STANDING CHANCELLOR` | 64-bit unsigned | 0 to 18 446 744 073 709 551 615 |
| `STANDING PIRATE` | 16-bit unsigned | 0 to 65 535 |
| `STANDING SAUSAGE-ROLL` | 8-bit unsigned | 0 to 255 |

`STANDING` is a **type modifier**, not a mutability modifier. The full declaration order is:

```
PRAY WELCOME <name> AS A [CONSERVATIVE | LIBERAL] [STANDING] <integer-type> [BEING <value>]
```

```topsy
PRAY WELCOME height    AS A STANDING PEER                ASIDE: unsigned 32-bit integer
PRAY WELCOME BigCount  AS A CONSERVATIVE STANDING CHANCELLOR BEING 0  ASIDE: constant unsigned 64-bit
PRAY WELCOME flags     AS A STANDING SAUSAGE-ROLL BEING 255  ASIDE: maximum unsigned byte
```

`STANDING` may **only** precede an integer type keyword (`PEER`, `CHANCELLOR`, `PIRATE`, `SAUSAGE-ROLL`). Combining it with `FATHOM`, `FOOT`, `YARN`, `STITCH`, `DECREE`, or `NAUGHT` is a parse error.

`STANDING` is drawn from the military standing of the regiment — a soldier placed in a *standing* capacity is confirmed in post, assigned a definite and unambiguous position; so too an unsigned integer occupies a definite, non-negative position on the number line.

### 3.3 Character Literals

A `STITCH` value is written as a **single-quoted character literal**:

```topsy
PRAY WELCOME LetterA  AS A STITCH BEING 'A'
PRAY WELCOME Space    AS A STITCH BEING ' '
PRAY WELCOME Newline  AS A STITCH BEING '~n'
PRAY WELCOME Quote    AS A STITCH BEING '~''
PRAY WELCOME Tilde    AS A STITCH BEING '~~'
```

A character literal opens with `'`, contains exactly one character (or one escape sequence), and closes with `'`. Placing more than one character between the quotes is a syntax error.

The escape sequences inside a character literal use the same Victorian flourish `~` prefix as `YARN`, with `~'` used for a literal single quote in place of the string's `~"`:

| Sequence | Meaning |
|---|---|
| `~n` | Newline |
| `~t` | Tab |
| `~'` | Literal single quote |
| `~~` | Literal tilde |

`STITCH` values may also be produced at runtime by indexing into a `YARN` variable — see §16.

### 3.4 Static Typing

Topsy Turvy is a **statically typed** language: type annotations are **enforced** constraints, not documentation.

**The declared type is a binding contract.** Assigning a value of the wrong type to a declared variable is a compile-time type error.

```topsy
PRAY WELCOME Ko-Ko AS A PEER BEING 42
Ko-Ko IS APPOINTED "Lord High Executioner"  ASIDE: TYPE ERROR — cannot assign YARN to PEER
```

**Default values when `BEING` is omitted** are assigned based on the declared type:

| Type | Default value |
|---|---|
| `PEER`, `CHANCELLOR`, `PIRATE`, `SAUSAGE-ROLL` and unsigned variants | `0` |
| `FATHOM`, `FOOT` | `0.0` |
| `DECREE` | `NAY` |
| `STITCH` | `'\0'` (the null character) |
| `YARN` | `""` (the empty string) |
| `A LITTLE LIST OF <T>` | empty array; or type-defaulted elements if a size is given |

**`NAUGHT` as an assigned value** is permitted only for `YARN` and array variables — it signals the absence of a string or the empty state of an array. Assigning `NAUGHT` to a numeric, boolean, or character variable is a type error.

**Explicit casts with `AS IT WERE`** are trusted and not verified at compile time. A cast that is invalid at runtime will still raise a runtime error.

```topsy
PRAY WELCOME age AS A YARN BEING AS IT WERE Ko-Ko AS A YARN   ASIDE: cast in a declaration initialiser
ageStr IS APPOINTED AS IT WERE Ko-Ko AS A YARN                 ASIDE: cast in an assignment
```

`AS A NAUGHT` and `A LITTLE LIST OF NAUGHT` are type errors. `NAUGHT` is valid as a value (the null literal), but may only be assigned where `YARN` or an array is expected.

---

Variables may also be declared inline anywhere in the program using the same `PRAY WELCOME` syntax; inline declarations are free-standing statements and do not require `THE CURTAIN RISES.`

```topsy
ASIDE: a working variable declared mid-programme, outside any PRINCIPALS block
PRAY WELCOME tally AS A PEER BEING 0
tally IS APPOINTED SUM OF tally AND 1
BEHOLD tally
ASIDE: prints 1
```

### Types

| Type keyword    | Equivalent         | Description |
|-----------------|--------------------|-------------|
| `PEER`          | int32              | A Peer of the Realm — a whole number, positive or negative. *From Iolanthe.* |
| `CHANCELLOR`    | int64              | A Lord High Chancellor — the 64-bit integer that governs when PEER's mandate is insufficient. *From Iolanthe.* |
| `PIRATE`        | int16              | A Pirate of Penzance — compact, nimble, and operating on reduced rations. *From The Pirates of Penzance.* |
| `SAUSAGE-ROLL`  | int8               | A morsel of modest proportion — the smallest signed whole number; unpretentious and compact, it asks no more of the stage than the occasion requires. |
| `FATHOM`        | float64            | A nautical measure — a number of real precision. *From H.M.S. Pinafore.* |
| `FOOT`          | float32            | A measure of shorter range — single-precision to FATHOM's double-precision; close enough for most calculations, and rather lighter on the feet. *From H.M.S. Pinafore.* |
| `YARN`          | string             | A wandering minstrel's stock-in-trade — a sequence of characters. *From The Mikado.* |
| `STITCH`        | character          | A single thread of a stitch — the atomic unit of YARN; one character, neither more nor less. *From The Mikado.* |
| `DECREE`        | boolean            | A ruling that stands or does not stand — either `VERITY` or `NAY`. *The Mikado*, Act I: "So he decreed, in words succinct..." |
| `NAUGHT`        | null               | Nothing. Not even that. |

**Boolean literals:**
- `VERITY` — true; *Utopia, Limited* — "Henceforward, **of a verity**, with Fame ourselves we link" — King Paramount
- `NAY` — false; used throughout the canon — *Iolanthe*: "Nay, tempt me not"; *Ruddigore*: "Nay — that may never be"

### Assignment

```topsy
Ko-Ko IS APPOINTED 42
Pooh-Bah IS APPOINTED "First Lord of the Treasury"
is_guilty IS APPOINTED VERITY
```

`IS APPOINTED` — drawn from *The Mikado*, Act I, in which Ko-Ko is raised by official proclamation to the exalted rank of Lord High Executioner: *"Ko-Ko is appointed to the post."* A value is not merely set; it is formally appointed to the variable by due process.

### Type Casting

```topsy
PRAY WELCOME age AS A YARN BEING AS IT WERE Ko-Ko AS A YARN         ASIDE: cast in a declaration initialiser

ageStr IS APPOINTED AS IT WERE Ko-Ko AS A YARN                      ASIDE: cast in an assignment

SUMMON someFunc WITH AS IT WERE Ko-Ko AS A YARN IF YOU PLEASE.      ASIDE: cast as a function argument
```

- `AS IT WERE <expr> AS A <type>` — a cast **expression** that evaluates to the cast value without mutating the source; "as it were" is the G&S hedging construction, used when a character invokes a convenient fiction about what something actually is. Because it is an expression, it can appear anywhere a value is expected: as the right-hand side of `IS APPOINTED`, as the `BEING` initialiser of a declaration, as a function argument, or as a sub-expression.  Explicit casts are trusted at compile time — a cast that is invalid at runtime will raise a runtime error.

---

## 4. I/O

### Output

```topsy
BEHOLD "A Most Ingenious Paradox!"
BEHOLD Ko-Ko
BEHOLD SUM OF Ko-Ko AND 1
BEHOLD "The value is: " AND Ko-Ko WITHOUT CEREMONY
```

- `BEHOLD <expression>` — prints the value followed by a newline. Drawn from *The Mikado*, Act I: *"Behold the Lord High Executioner!"* — the word used for public announcement to an assembled audience. Whatever follows `BEHOLD` is presented for the audience's inspection.
- `BEHOLD <expression> WITHOUT CEREMONY` — prints without a trailing newline
- Multiple values may be concatenated in a `BEHOLD` using `AND`:

```topsy
BEHOLD "Ko-Ko's value is " AND Ko-Ko AND ", which is most irregular."
```

### Input

```topsy
PRAY TELL Ko-Ko
```

- `PRAY TELL <variable>` — reads a line from standard input into the variable. The target variable must be declared as `YARN`; using `PRAY TELL` with any other type is a type error. To obtain a numeric value, read into a `YARN` variable then cast with `AS IT WERE`. Drawn verbatim from *The Mikado*, Act I — Nanki-Poo's opening recitative: *"Gentlemen, I pray you tell me / Where a gentle maiden dwelleth..."*

```topsy
PRAY WELCOME input AS A YARN
PRAY WELCOME candidate AS A PEER
PRAY TELL input
candidate IS APPOINTED AS IT WERE input AS A PEER
```

---

## 5. Arithmetic

All arithmetic uses prefix notation with `AND` as the argument separator.

| Expression                      | Operation |
|---------------------------------|-----------|
| `SUM OF x AND y`                | x + y     |
| `DIFFERENCE OF x AND y`         | x − y     |
| `PRODUCT OF x AND y`            | x × y     |
| `QUOTIENT OF x AND y`           | x ÷ y     |
| `REMAINDER OF x AND y`          | x mod y   |
| `LARGER OF x AND y`             | max(x, y) |
| `SMALLER OF x AND y`            | min(x, y) |

Expressions compose in prefix form:

```topsy
PRODUCT OF SUM OF a AND b AND DIFFERENCE OF c AND d
ASIDE: computes (a + b) * (c - d)
```

If either operand is a `FATHOM`, the result is a `FATHOM`. If both are `PEER`, the result is a `PEER`. Integer division truncates.

---

## 6. Bitwise Operators

Bitwise operators use the same prefix notation as arithmetic operators and require integer operands. Applying a bitwise operator to a `FATHOM`, `FOOT`, `YARN`, `STITCH`, `DECREE`, or `NAUGHT` value is a runtime error.

| Expression                       | Operation                    |
|----------------------------------|------------------------------|
| `CHORD OF x AND y`               | `x & y` (bitwise AND)        |
| `HARMONY OF x AND y`             | `x \| y` (bitwise OR)        |
| `DISCORD OF x AND y`             | `x ^ y` (bitwise XOR)        |
| `INVERSION OF x`                 | `~x` (bitwise NOT)    |
| `TRANSPOSITION UP x`             | `x << 1` (left shift by 1)   |
| `TRANSPOSITION DOWN x`           | `x >> 1` (right shift by 1)  |

For the binary operators (`CHORD OF`, `HARMONY OF`, `DISCORD OF`), the result type follows the integer widening hierarchy (widest operand type wins). For the unary operators (`INVERSION OF`, `TRANSPOSITION UP`, `TRANSPOSITION DOWN`), the result type is the same as the operand type.

```topsy
BEHOLD CHORD OF 12 AND 10          ASIDE: 8  (1100 & 1010 = 1000)
BEHOLD HARMONY OF 5 AND 3          ASIDE: 7  (0101 | 0011 = 0111)
BEHOLD DISCORD OF 15 AND 9         ASIDE: 6  (1111 ^ 1001 = 0110)
BEHOLD INVERSION OF 0              ASIDE: -1 (bitwise complement of 0)
BEHOLD TRANSPOSITION UP 4          ASIDE: 8  (4 << 1)
BEHOLD TRANSPOSITION DOWN 8        ASIDE: 4  (8 >> 1)
```

---

## 7. String Operations

### Concatenation

```topsy
WOVEN OF "Hello, " AND name AND "!" IF YOU PLEASE.
```

`WOVEN OF <expr> AND <expr> [AND <expr> ...] IF YOU PLEASE.` — concatenates any number of values, automatically casting each to `YARN`. The metaphor follows naturally from `YARN`: threads of text are woven together into a single fabric.

**A note on `IF YOU PLEASE.`** — This closer appears wherever a construct accepts a variable-length list of terms: string concatenation (`WOVEN OF`), function calls (`SUMMON`), and the variadic boolean operators (`ALL OF`, `ANY OF`). It is the formal signal that the enumeration is complete and the assembled company may proceed. Drawn verbatim from *H.M.S. Pinafore*: Sir Joseph Porter insists throughout the opera that every order and request be tendered *"if you please"* — the precise Victorian form for notifying an assembled party that one has finished stating one's terms. An expression-list left open without `IF YOU PLEASE.` to close it is as irregular as giving Sir Joseph a direct order without the proper form of address.

### String Interpolation

Within a `YARN` literal, variables may be interpolated using curly braces:

```topsy
BEHOLD "My name is {name}, Lord High {title}."
```

### Escape Characters

The escape character within `YARN` literals is `~` (the Victorian flourish). `~` also serves as the line-continuation character outside string literals — both roles are fully documented in §18.

| Sequence | Meaning              |
|----------|----------------------|
| `~n`     | Newline              |
| `~t`     | Tab                  |
| `~"`     | Literal double-quote |
| `~~`     | Literal tilde        |

---

## 8. Comparison & Boolean Logic

### Comparison

```topsy
ALIKE x AND y          ASIDE: x == y  =>  VERITY or NAY
UNLIKE x AND y         ASIDE: x != y  =>  VERITY or NAY
PRE-ADAMITE x AND y    ASIDE: x > y   =>  VERITY or NAY
LOWER DEGREE x AND y   ASIDE: x < y   =>  VERITY or NAY
```

`PRE-ADAMITE` — drawn from *Ruddigore*, Act II, in which the ancestral portraits of the Murgatroyd baronets descend through history to a baronet *"of Pre-Adamite antiquity"* — one who predates all others; he who comes before has the greater standing. `PRE-ADAMITE x AND y` yields `VERITY` if `x` has greater standing (i.e. is greater in value) than `y`.

`LOWER DEGREE` — drawn from the pervasive G&S preoccupation with social degree and rank; *The Gondoliers* and *The Mikado* alike make much of the man who occupies a lower degree than another. `LOWER DEGREE x AND y` yields `VERITY` if `x` is of lower degree (i.e. lesser in value) than `y`.

Greater-than-or-equal and less-than-or-equal are expressed by negating the strict form with `HARDLY EVER`:

```topsy
HARDLY EVER LOWER DEGREE x AND y    ASIDE: x >= y
HARDLY EVER PRE-ADAMITE x AND y     ASIDE: x <= y
```

### Boolean Operators

| Expression                               | Operation     |
|------------------------------------------|---------------|
| `BOTH x AND y`                           | Logical AND   |
| `EITHER x OR y`                          | Logical OR    |
| `HARDLY EVER x`                          | Logical NOT   |
| `ALL OF a AND b AND c IF YOU PLEASE.`    | Variadic AND  |
| `ANY OF a AND b AND c IF YOU PLEASE.`    | Variadic OR   |

`HARDLY EVER` is the logical NOT operator — drawn directly from *H.M.S. Pinafore*: "What, never? / No, never! / What, never? / **Well, hardly ever!**" The absolute negative, delivered with comic deflation. `HARDLY EVER x` is the logical inverse of `x`.

### Type Requirements

All operands of `BOTH`, `EITHER`, `HARDLY EVER`, `ALL OF`, and `ANY OF` must be of type `DECREE`. Passing a non-`DECREE` value to a boolean operator is a type error.

All conditions in `SHOULD IT TRANSPIRE THAT`, `WHILST`, `UNTIL`, `YEOMAN`, and the ternary expression must also be of type `DECREE`.

Comparison operators (`ALIKE`, `UNLIKE`, `PRE-ADAMITE`, `LOWER DEGREE`) require operands of the same type, or types that are compatible by integer widening (e.g. comparing a `PEER` to a `CHANCELLOR` is valid; comparing a `PEER` to a `YARN` is a type error). These operators always return `DECREE`.

Arithmetic operators (`SUM OF`, `DIFFERENCE OF`, etc.) require numeric operands. If operands differ, the result type is the wider of the two, following the integer widening hierarchy: `CHANCELLOR` > `PEER` > `PIRATE` > `SAUSAGE-ROLL`; `FATHOM` > `FOOT`. A `PEER` compared to or combined with a `FATHOM` produces a `FATHOM` result.

Bitwise operators (`CHORD OF`, `HARMONY OF`, `DISCORD OF`, `INVERSION OF`, `TRANSPOSITION UP`, `TRANSPOSITION DOWN`) require integer operands. Applying a bitwise operator to a `FATHOM`, `FOOT`, `YARN`, `STITCH`, or `DECREE` value is a type error.

---

## 9. Built-In Variables

### THE PROPS

**`THE PROPS`** is a built-in `CONSERVATIVE LITTLE LIST OF YARN` variable that is always present at programme start. It contains the arguments passed to the programme at the point of invocation, in the order they were provided.

- If no arguments are passed, `THE PROPS` is an empty array (`[]`).
- Elements are always `YARN`; cast to another type if a different type is needed.
- Indexing is 1-based, consistent with all arrays: `VICTIM 1 ON THE PROPS` retrieves the first argument.
- `THE PROPS` is `CONSERVATIVE` — any attempt to reassign the array or any of its elements is a runtime error.
- Per Invariant I7, `THE PROPS` is a global variable and is therefore inaccessible inside functions. Pass individual elements as function arguments when needed.

```topsy
HARK! "THE PROPS example"
BEHOLD VICTIM 1 ON THE PROPS          ASIDE: prints first argument
BEHOLD VICTIM 2 ON THE PROPS          ASIDE: prints second argument
FINALE.
```

> *"The properties of a troupe are the lifeblood of a performance."* — theatrical tradition

---

## 10. Conditionals

### If / Else If / Else

```topsy
SHOULD IT TRANSPIRE THAT <expression>
  QUITE SO.
    <true block>
  OR, IF NOT, <expression>
    <else-if block>
  OTHERWISE,
    <else block>
SO MUCH FOR THAT.
```

- `SHOULD IT TRANSPIRE THAT <expression>` — the conditional phrasing of the Lord Chancellor in *Iolanthe*, who frames every legal determination as something that *transpires* to be the case; `<expression>` must be of type `DECREE`
- `QUITE SO.` — the true branch; verbatim from *The Mikado*, used by Ko-Ko and Pooh-Bah as a crisp affirmation that the established fact is confirmed
- `OR, IF NOT, <expression>` — else-if; the supplied expression must be of type `DECREE`; the Lord Chancellor's habit of carefully enumerating alternatives
- `OTHERWISE,` — the else branch; Pooh-Bah explicitly uses "otherwise" and "on the other hand" when switching between his many logical branches and capacities
- `SO MUCH FOR THAT.` — closes the conditional block; Ko-Ko's characteristic dismissive summary once a matter has been disposed of

**Example:**

```topsy
SHOULD IT TRANSPIRE THAT ALIKE rank AND "Admiral"
  QUITE SO.
    BEHOLD "He is the Ruler of the Queen's Navee!"
  OR, IF NOT, ALIKE rank AND "Captain"
    BEHOLD "What, never? Well, hardly ever!"
  OTHERWISE,
    BEHOLD "A mere landsman."
SO MUCH FOR THAT.
```

### Switch / Case

```topsy
IN WHICH CAPACITY? <expression>
  WHEN ACTING AS <literal>
    <block>
    THAT WILL DO.
  WHEN ACTING AS <literal>
  WHEN ACTING AS <literal>
    <block>
    THAT WILL DO.
  FAILING ALL OF THE ABOVE,
    <block>
NOTHING COULD BE MORE SATISFACTORY.
```

- `IN WHICH CAPACITY? <expression>` — drawn from Pooh-Bah's response when addressed: *"In which of my capacities?"* — he holds so many offices that the caller must specify which one they are invoking; the supplied expression is evaluated directly; case literal types must match the switch expression type (see §6)
- `WHEN ACTING AS <literal>` — case label; mirrors Pooh-Bah switching between his official capacities; cases fall through unless broken
- `THAT WILL DO.` — the universal break keyword; used dismissively throughout the G&S canon — by the Mikado, by Ko-Ko, by the Lord Chancellor — to signal that a matter is concluded and no further elaboration is required; valid in both switch and loop contexts
- `FAILING ALL OF THE ABOVE,` — default case; the Lord Chancellor's catch-all when none of the specific provisions apply
- `NOTHING COULD BE MORE SATISFACTORY.` — closes the switch block; verbatim from *The Mikado*, Act II — the Mikado's response upon receiving the report of the (entirely fictitious) execution, delivered with great satisfaction while everything is in fact catastrophically wrong

**Example:**

```topsy
IN WHICH CAPACITY? office
  WHEN ACTING AS "Executioner"
    BEHOLD "I have a little list."
    THAT WILL DO.
  WHEN ACTING AS "Chancellor"
  WHEN ACTING AS "Admiral"
    BEHOLD "A man of many parts."
    THAT WILL DO.
  FAILING ALL OF THE ABOVE,
    BEHOLD "Lord High Everything Else, no doubt."
NOTHING COULD BE MORE SATISFACTORY.
```

### Ternary Expression

A ternary expression is an inline conditional that evaluates to one of two values depending on a condition. It is an **expression**, not a statement, and may appear anywhere a value is expected: the right-hand side of an assignment, a function argument, a `BEHOLD` value, an array initialiser, and so on.

**Syntax:**
```
<true-value> SHOULD IT TRANSPIRE THAT <condition> OTHERWISE, <false-value>
```

- `SHOULD IT TRANSPIRE THAT` and `OTHERWISE,` are reused from the block conditional; no new keywords are introduced.
- `<true-value>` is the value returned when the condition is `VERITY`. It may be any expression that is not itself a ternary (to avoid left-recursion ambiguity). `<true-value>` and `<false-value>` must be of the same type or widening-compatible types; the result type is the wider of the two.
- `<condition>` is the guard condition. It must be of type `DECREE`. It may not itself be a ternary, to avoid `OTHERWISE,` being consumed ambiguously.
- `<false-value>` is the value returned when the condition is `NAY`. It may be any expression, including a nested ternary, enabling right-chaining:

```topsy
a SHOULD IT TRANSPIRE THAT cond1 OTHERWISE, b SHOULD IT TRANSPIRE THAT cond2 OTHERWISE, c
```

**Examples:**
```topsy
ASIDE: Assign a label based on a condition
label IS APPOINTED "Admiral" SHOULD IT TRANSPIRE THAT ALIKE rank AND 10 OTHERWISE, "Captain"

ASIDE: Print a one-liner without an if block
BEHOLD "Prime" SHOULD IT TRANSPIRE THAT SUMMON is_prime WITH n IF YOU PLEASE. OTHERWISE, "Not prime"

ASIDE: Right-chained ternary: three-way selection
title IS APPOINTED "Senior" SHOULD IT TRANSPIRE THAT PRE-ADAMITE age AND 60 OTHERWISE, ~
  "Junior" SHOULD IT TRANSPIRE THAT LOWER DEGREE age AND 30 OTHERWISE, "Mid-level"
```

---

## 11. Guard Clauses

A guard clause checks that a condition holds and executes an `OTHERWISE,` block when it does not.  If the condition is `VERITY`, execution falls through the guard without entering the block.  The condition must be of type `DECREE`.

```
YEOMAN <condition>
  OTHERWISE,
    <action>
UNDER ORDERS.
```

Guard clauses are idiomatic at the top of a function or loop body to assert preconditions before proceeding.  The `OTHERWISE,` block typically contains an early return (`MY DUTY IS PREMATURELY DISCHARGED.`), a `THAT WILL DO.` break, or a throw (`A HIDEOUS CURSE ON`).

### Basic Guard

```topsy
YEOMAN PRE-ADAMITE score AND 0
  OTHERWISE,
    A HIDEOUS CURSE ON "Score must be positive"
UNDER ORDERS.
```

If `score` is greater than `0` the guard passes and execution continues after `UNDER ORDERS.`.  If `score` is `0` or negative the `OTHERWISE,` block executes and throws.

### Guard with Early Return

```topsy
IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name
  YEOMAN UNLIKE name AND ""
    OTHERWISE,
      MY DUTY IS PREMATURELY DISCHARGED.
  UNDER ORDERS.
  BEHOLD WOVEN OF "Hello, " AND name AND "!"
MY DUTY IS DISCHARGED.
```

### Guard with Break

```topsy
BY A LEGAL FICTION ASCENDING i UNTIL 10
  YEOMAN UNLIKE REMAINDER OF i AND 2 AND 0
    OTHERWISE,
      THAT WILL DO.
  UNDER ORDERS.
  BEHOLD i
THE TERM EXPIRES.
```

---

## 12. Loops

### Basic Loop (Infinite / Manual Break)

```topsy
BY A LEGAL FICTION [KNOWN AS <label>]
  <body>
  THAT WILL DO.     ASIDE: break out of the loop
  ONCE MORE.        ASIDE: skip to the next iteration
THE TERM EXPIRES.
```

- `BY A LEGAL FICTION` — begins a loop. The Lord Chancellor in *Iolanthe* and the baronets of *Ruddigore* both operate under legal fictions that force them to repeat actions indefinitely — the precise G&S metaphor for a loop: a construct that, by a convenient fiction, repeats events until reality reasserts itself.
- `KNOWN AS <label>` — optional label for the loop; a legal fiction, like all G&S legal fictions, may be named or may proceed anonymously. Labels are for readability only — `THAT WILL DO.` and `ONCE MORE.` always apply to the nearest enclosing loop regardless of whether any loop carries a label.
- `THAT WILL DO.` — breaks out of the innermost loop immediately; the universal break keyword, valid in both loop and switch contexts — see §10
- `ONCE MORE.` — skips the remainder of the current iteration and proceeds immediately to the next; the stage call to repeat from the top of a passage. Applies to all loop forms.
- `THE TERM EXPIRES.` — closes the loop. Drawn from *The Grand Duke*, Act I: the Statutory Duel law *"expires to-morrow"* — a recurring obligation reaching its natural terminus.

### Ascending (Counted Up) Loop

```topsy
BY A LEGAL FICTION KNOWN AS counter ASCENDING i UNTIL ALIKE i AND 10
  BEHOLD i
THE TERM EXPIRES.
```

- `ASCENDING <var>` — increments `<var>` by 1 at the end of each iteration; `<var>` retains whatever value it already holds on entry to the loop — it is the program's responsibility to initialise it beforehand
- `BY <expression>` — optional; overrides the default step of `1` with the value of `<expression>`, evaluated once per iteration. `<expression>` may be any `PEER` expression, including `0` or a negative value; no runtime validation is performed
- `UNTIL <expression>` — exits when the expression is `VERITY` (checked before each iteration); `<expression>` must be of type `DECREE`. From *The Pirates of Penzance* — Frederic's indenture binds him *"until"* his twenty-first birthday.

### Descending (Counted Down) Loop

```topsy
BY A LEGAL FICTION KNOWN AS countdown DESCENDING i UNTIL ALIKE i AND 0
  BEHOLD i
THE TERM EXPIRES.
```

- `DESCENDING <var>` — decrements `<var>` by 1 at the end of each iteration; `<var>` retains whatever value it already holds on entry to the loop
- `BY <expression>` — optional; overrides the default step of `1` with the value of `<expression>`, evaluated once per iteration. `<expression>` may be any `PEER` expression, including `0` or a negative value; no runtime validation is performed

### While Loop

```topsy
BY A LEGAL FICTION KNOWN AS watchman WHILST UNLIKE Ko-Ko AND 0
  BEHOLD Ko-Ko
  Ko-Ko IS APPOINTED DIFFERENCE OF Ko-Ko AND 1
THE TERM EXPIRES.
```

- `WHILST <expression>` — continues while expression is `VERITY` (checked before each iteration; no automatic variable mutation); `<expression>` must be of type `DECREE`

---

## 13. Functions

### Declaration

```topsy
IT IS MY DUTY TO PERFORM <name> UNDER THE TERMS OF <param1> AS A <type1> [AND <param2> AS A <type2> ...] [TO FIND <return-type>]
  <body>
  AND SO I FIND <expression>
MY DUTY IS DISCHARGED.
```

- `IT IS MY DUTY TO PERFORM <name>` — declares a function; the G&S obligation formula, used throughout the canon
- `UNDER THE TERMS OF <param> AS A <type> [AND <param> AS A <type> ...]` — declares typed parameters. Each parameter requires a type annotation. From *The Pirates of Penzance*: Frederic's indenture specifies the exact *terms* under which his obligation is to be performed.
- `UNDER NO OBLIGATION` — for functions with no parameters
- `TO FIND <type>` — declares the return type. Omitting `TO FIND` means the function is **void** — it returns no value. `TO FIND NAUGHT` is not valid because `NAUGHT` is not a type; void is expressed by omitting `TO FIND` entirely.
- `AND SO I FIND <expression>` — returns a value; the judicial verdict formula from *Trial by Jury*; the expression type must match the declared `TO FIND` type; using `AND SO I FIND` in a void function (one without `TO FIND`) is a type error
- `MY DUTY IS DISCHARGED.` — closes the function; the obligation is fulfilled
- `MY DUTY IS PREMATURELY DISCHARGED.` — early return with no value; valid only in void functions; using it in a function declared `TO FIND <type>` is a type error
- Functions have their own scope; they receive values only through parameters

**Type checking rules:**
- All argument types in a call must match the declared parameter types (or be widening-compatible)
- Return expressions must match the declared `TO FIND` type
- Every code path through a typed function must reach an `AND SO I FIND`
- A void function with no reachable `AND SO I FIND` is valid

### Calling

```topsy
SUMMON factorial WITH 5 IF YOU PLEASE.
PRAY WELCOME answer AS A PEER BEING SUMMON factorial WITH n IF YOU PLEASE.
SUMMON greet WITH NOTHING IF YOU PLEASE.
```

- `SUMMON <name> WITH <arg1> [AND <arg2> ...] IF YOU PLEASE.` — calls a function. `SUMMON` is verbatim from *The Mikado*, Act I — Ko-Ko: *"I summon my guard."* To summon a named party to perform their duty, with the specified terms, if they would be so kind. `IF YOU PLEASE` is verbatim from *H.M.S. Pinafore* — Sir Joseph Porter's insistence on the proper form of address.
- `SUMMON <name> WITH NOTHING IF YOU PLEASE.` — calls a function with no arguments
- The return value may be used directly in an expression; using the return value of a void function is a type error

**Example — Factorial:**

```topsy
IT IS MY DUTY TO PERFORM factorial UNDER THE TERMS OF n AS A PEER TO FIND PEER
  SHOULD IT TRANSPIRE THAT ALIKE n AND 0
    QUITE SO.
      AND SO I FIND 1
  SO MUCH FOR THAT.
  AND SO I FIND PRODUCT OF n AND SUMMON factorial WITH DIFFERENCE OF n AND 1 IF YOU PLEASE.
MY DUTY IS DISCHARGED.
```

**Example — Void Function:**

```topsy
IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
  BEHOLD WOVEN OF "Hello, " AND name AND "!" IF YOU PLEASE.
MY DUTY IS DISCHARGED.

SUMMON greet WITH "Ko-Ko" IF YOU PLEASE.
```

---

## 14. Exception Handling

### Throwing

```topsy
A HIDEOUS CURSE ON <value>
```

`A HIDEOUS CURSE ON <value>` — raises an exception, carrying `<value>` as the exception payload. `<value>` must be of type `YARN`; throwing a non-`YARN` value is a type error. Drawn from *Ruddigore*: the Murgatroyd baronets are bound by an ancestral curse — to hurl a hideous curse upon something is the most dramatically appropriate signal that affairs have gone catastrophically wrong. May be used anywhere in the programme; if uncaught by a `WITH THE GREATEST RESPECT` block the programme terminates with an error and the cursed value is reported.

### Catching

```topsy
WITH THE GREATEST RESPECT, <operation>
  WITH GRATITUDE
    <success block>
  MODIFIED RAPTURE, <name>
    <exception block>
THAT CONCLUDES THE MATTER.
```

- `WITH THE GREATEST RESPECT, <operation>` — wraps a potentially-failing operation; catches any exception raised by `A HIDEOUS CURSE ON` within `<operation>`
- `WITH GRATITUDE` — the success handler; entered when no exception is raised
- `MODIFIED RAPTURE, <name>` — the exception handler; from *The Pirates of Penzance*: Mabel's "Oh joy! Oh rapture! — *modified* rapture!" upon learning the bad news; the named binding is required — the cursed value is auto-declared as `<name>` with type `YARN`, scoped to the exception block (no prior `PRAY WELCOME` required)
- `THAT CONCLUDES THE MATTER.` — closes the block

**Example:**

```topsy
IT IS MY DUTY TO PERFORM checked_divide UNDER THE TERMS OF a AS A PEER AND b AS A PEER TO FIND PEER
  SHOULD IT TRANSPIRE THAT ALIKE b AND 0
    QUITE SO.
      A HIDEOUS CURSE ON "Division by zero — the Pirate King is most displeased."
  SO MUCH FOR THAT.
  AND SO I FIND QUOTIENT OF a AND b
MY DUTY IS DISCHARGED.

PRAY WELCOME result AS A PEER
WITH THE GREATEST RESPECT, SUMMON checked_divide WITH 10 AND 0 IF YOU PLEASE.
  WITH GRATITUDE
    result IS APPOINTED SUMMON checked_divide WITH 10 AND 2 IF YOU PLEASE.
    BEHOLD WOVEN OF "Result: " AND result IF YOU PLEASE.
  MODIFIED RAPTURE, Grievance
    BEHOLD WOVEN OF "A curse has been invoked: " AND Grievance IF YOU PLEASE.
THAT CONCLUDES THE MATTER.
```

### Assert Statements

```topsy
THE LAW IS <condition> THAT <error-message>
```

`THE LAW IS <condition> THAT <error-message>` — asserts that a runtime invariant holds.  `<condition>` must be of type `DECREE`; `<error-message>` must be of type `YARN`.  If `<condition>` is `NAY`, it throws using the same mechanism as `A HIDEOUS CURSE ON`, carrying `<error-message>` as the payload; the exception may be caught by a `WITH THE GREATEST RESPECT` block.  If the condition is `VERITY`, execution continues with no effect.

- `THE LAW IS` — opens the assertion; the Mikado and Lord Chancellor are the ultimate arbiters of law and decree — when the law is invoked, it must hold
- `THAT` — separates the condition from the error message; a structural separator (not in the keyword completion list)

**Example:**

```topsy
THE LAW IS PRE-ADAMITE score AND 0 THAT "Score must be positive"
```

```topsy
ASIDE: Assert with a caught exception
WITH THE GREATEST RESPECT, SUMMON validate WITH score IF YOU PLEASE.
  WITH GRATITUDE
    BEHOLD "Validation passed"
  MODIFIED RAPTURE, err
    BEHOLD WOVEN OF "Validation failed: " AND err IF YOU PLEASE.
THAT CONCLUDES THE MATTER.
```

---

## 15. Libraries & Imports

```topsy
PRAY ADMIT "filename"
```

`PRAY ADMIT "filename"` — admits another `.topsy` file into the current programme's company. All `IT IS MY DUTY TO PERFORM` declarations in that file become available. Drawn from the theatrical tradition of formally admitting a new party to the assembled company — the doorkeeper admits the newcomer, who then takes their place among the principals already on stage.

---

## 16. Arrays

Arrays are ordered, indexed collections of values. An array is declared with the `LITTLE LIST OF` type annotation and accessed or mutated element-by-element with `VICTIM`.

### Declaring an Array

```
PRAY WELCOME <name> AS A [CONSERVATIVE | LIBERAL] LITTLE LIST OF [<size>] <type>
    [BEING <expr> AND <expr> [AND <expr> ...] IF YOU PLEASE.]
```

```topsy
PRAY WELCOME miscreants AS A LITTLE LIST OF YARN BEING "Pooh-Bah" AND "Ko-Ko" AND "Pish-Tush" IF YOU PLEASE.
PRAY WELCOME scores     AS A LITTLE LIST OF PEER BEING 10 AND 20 AND 30 IF YOU PLEASE.
PRAY WELCOME empty      AS A LITTLE LIST OF PEER
PRAY WELCOME slots      AS A LITTLE LIST OF 3 YARN
```

- `A LITTLE LIST OF <type>` — the array type annotation; drawn from Ko-Ko's famous "I've Got a Little List" from *The Mikado*, in which he catalogues all the people who would not be missed — every array is, at heart, such a list.
- `<type>` — the declared element type (`PEER`, `FATHOM`, `YARN`, `DECREE`, `STITCH`, etc.); assigning an element of the wrong type is a type error. `NAUGHT` is not a valid element type.
- `<size>` — an optional integer literal placed between `LITTLE LIST OF` and `<type>`; pre-allocates the array with that many elements, each initialised to the type's default value (see §3.4), making element assignment (`VICTIM n ON arr IS APPOINTED val`) usable without a `BEING` clause. A size of `0` produces an empty array. A negative size is a runtime error.
- `BEING <expr> AND <expr> ... IF YOU PLEASE.` — initial element list; follows the same `IF YOU PLEASE.` convention as other variable-length constructs (see §7); omitting `BEING` produces an empty array, **not** `NAUGHT`.
- `<size>` and `BEING` are **mutually exclusive** — providing both on the same declaration is a runtime error.
- `CONSERVATIVE` — a constant array; the variable cannot be reassigned and no element can be replaced after declaration.
- Index positions are **1-based**: the first element is at position 1.
- Arrays have **reference semantics**: assigning an array variable to another variable makes both names point to the same list. Mutating via either name is visible through the other.

### Reading an Element

```topsy
VICTIM <index> ON <array>
```

```topsy
BEHOLD VICTIM 1 ON miscreants         ASIDE: prints Pooh-Bah
PRAY WELCOME first AS A YARN BEING VICTIM 1 ON miscreants
```

`VICTIM <index> ON <array-or-yarn>` is an **expression** that evaluates to the element at position `<index>`. `<index>` is 1-based — `VICTIM 1` is the first element. `<index>` may be any expression that evaluates to a `PEER`. When applied to a `YARN` variable, it evaluates to the `STITCH` (character) at that position. Accessing an out-of-range index is a runtime error.

*`VICTIM` — Ko-Ko's little list consists of intended victims; every item retrieved from the list is, necessarily, a victim.*

### Array Length

```topsy
RECKONING OF <array-or-yarn>
```

```topsy
RECKONING OF miscreants                            ASIDE: evaluates to 3 (for a 3-element array)
length IS APPOINTED RECKONING OF miscreants
BEHOLD SUM OF RECKONING OF miscreants AND 1        ASIDE: prints 4
```

`RECKONING OF <array-or-yarn>` is an **expression** that evaluates to the number of elements in `<array>` as a `PEER` (integer), or the number of characters in a `YARN` variable. The result is always ≥ 0. Applying it to a variable that is neither an array nor a string is a runtime error.

*`RECKONING OF` — Ko-Ko keeps a careful reckoning of his little list; every tally is a formal accounting of what is owed.*

### Setting an Element

```topsy
VICTIM <index> ON <array> IS APPOINTED <value>
```

```topsy
VICTIM 2 ON miscreants IS APPOINTED "Nanki-Poo"
```

Replaces the element at position `<index>` with `<value>`. If the array was declared `CONSERVATIVE`, attempting to set an element is a runtime error.

### Display

`BEHOLD` renders an array as a comma-separated, bracket-enclosed list of its elements' string representations:

```topsy
BEHOLD miscreants   ASIDE: prints ["Pooh-Bah", "Ko-Ko", "Pish-Tush"]
```

### Strings as Character Sequences

A `YARN` variable may be used with `VICTIM` and `RECKONING OF` as if it were a `LITTLE LIST OF STITCH`:

**Reading a character:**

```topsy
VICTIM <index> ON <yarn-variable>
```

```topsy
PRAY WELCOME word AS A YARN BEING "Mikado"
BEHOLD VICTIM 1 ON word          ASIDE: prints M
BEHOLD VICTIM 6 ON word          ASIDE: prints o
PRAY WELCOME ch AS A STITCH BEING VICTIM 3 ON word   ASIDE: ch = 'k'
```

`VICTIM n ON <yarn>` evaluates to a `STITCH` value — the character at 1-based position `n`. Accessing a position less than 1 or greater than the string's length is a runtime error.

**String length:**

```topsy
RECKONING OF <yarn-variable>
```

```topsy
length IS APPOINTED RECKONING OF word    ASIDE: evaluates to 6
```

`RECKONING OF <yarn>` evaluates to a `PEER` (integer) equal to the number of characters in the string.

**Strings are immutable:** `VICTIM n ON <yarn> IS APPOINTED val` is a runtime error — individual characters in a `YARN` cannot be replaced. Use `WOVEN OF` to build a new string.

---

## 17. Documentation Comments

A **documentation comment** is an `(ASIDE, AT SOME LENGTH: ... END OF ASIDE.)` block placed immediately before a `PRAY WELCOME` declaration or an `IT IS MY DUTY TO PERFORM` function declaration. Blank lines between the block and the declaration are allowed; any intervening non-blank line breaks the association and the block is treated as a plain comment with no special meaning.

Documentation comments are not executed. They annotate the programme for human readers and tooling that can display rich descriptions when hovering over a symbol in an editor.

### 15.1 Tags

Within a documentation comment, content is organised by **keyword tags**. Each tag opens a section that continues across as many lines as needed, until the next tag or the end of the block. Tags are case-insensitive.

| Tag | Purpose | Occurrences |
|---|---|---|
| `LEGEND: <text>` | One-line summary of the symbol | Once |
| `RECITATIVE: <text>` | Additional remarks; may span multiple lines | Once |
| `ARTICLE <name> (<type>): <text>` | Description of a function parameter; `<name>` is the parameter name, `<type>` is its declared type | Once per parameter |
| `CONSEQUENCE (<type>): <text>` | Description of the return value; `<type>` is the declared return type | Once |
| `CURSES <name> (<type>): <text>` | Description of a thrown value; `<name>` is the identifier passed to `A HIDEOUS CURSE ON`, `<type>` is its type | Once per thrown value |
| `CHORUS: <text>` | Code example; all lines following until the next tag form a code block | Multiple |
| `ENSEMBLE: <text>` | See-also reference | Multiple |
| `STATUTORY: <text>` | Deprecation notice; marks the symbol as deprecated | Once |

A block with no recognised tags is treated as a plain comment.

### 15.2 Examples

**Documented variable:**

```topsy
(ASIDE, AT SOME LENGTH:
  LEGEND: Holds the numbers for the range calculation.
END OF ASIDE.)
PRAY WELCOME Numbers AS A PEER
```

**Documented function:**

```topsy
(ASIDE, AT SOME LENGTH:
  LEGEND: Sums the numbers in a range.
  RECITATIVE: Any additional remarks.
    Further elaboration on the second line.
  ARTICLE Start (PEER): The starting number.
  ARTICLE End (PEER): The ending number.
  CONSEQUENCE (PEER): The total sum.
  CURSES SameValues (YARN): Thrown if Start and End are the same.
  CHORUS:
  SUMMON SumRange WITH 1 AND 10 IF YOU PLEASE.
  ENSEMBLE: AnotherFunc
END OF ASIDE.)
IT IS MY DUTY TO PERFORM SumRange UNDER THE TERMS OF Start AS A PEER AND End AS A PEER TO FIND PEER
  PRAY WELCOME Total AS A PEER BEING 0
  ASIDE: Code logic...
  A HIDEOUS CURSE ON "Start and End must differ."
  AND SO I FIND Total
MY DUTY IS DISCHARGED.
```

**Deprecated symbol:**

```topsy
(ASIDE, AT SOME LENGTH:
  LEGEND: Old sum variable. Use SumRange instead.
  STATUTORY: Use SumRange, which supports all range sizes.
END OF ASIDE.)
PRAY WELCOME OldSum AS A PEER
```

---

## 18. Complete Keyword Reference

| Keyword                                          | Role                        | G&S Source / Note                                                                 |
|--------------------------------------------------|-----------------------------|-----------------------------------------------------------------------------------|
| `HARK!`                                          | Program start               | Theatrical attention-getter throughout the canon                                  |
| `FINALE.`                                        | Program end                 | Standard G&S ending                                                               |
| `PRINCIPALS`                                     | Variable declaration block  | Dramatis Personae                                                                 |
| `THE CURTAIN RISES.`                             | Close `PRINCIPALS` block    | Once all characters are assembled, the curtain rises and the drama begins         |
| `PRAY WELCOME`                                   | Variable declaration        | *The Mikado*, Act I — `PRAY` verbatim; welcoming each new variable before the assembled company |
| `AS A`                                           | Type annotation             | —                                                                                 |
| `BEING`                                          | Initial value               | *Dramatis Personae* parentheticals — "Nanki-Poo, *being* the son of the Mikado..." |
| `CONSERVATIVE`                                   | Constant modifier           | *Iolanthe*, Act II — "every boy and every gal ... is either a little Liberal or else a little Conservative"; a value fixed by decree, immovable by any subsequent appointment |
| `LIBERAL`                                        | Explicit mutable modifier   | *Iolanthe*, Act II — same verse; the mutable counterpart to `CONSERVATIVE`; optional, as mutability is the default |
| `IS APPOINTED`                                   | Assignment                  | *The Mikado*, Act I — Ko-Ko raised to Lord High Executioner by official proclamation |
| `AS IT WERE`                                     | Expression cast             | G&S hedging construction — invoking a convenient fiction about what something is |
| `BEHOLD`                                         | Print output                | *The Mikado*, Act I — "Behold the Lord High Executioner!" — public announcement   |
| `WITHOUT CEREMONY`                               | Suppress newline on output  | Victorian politeness turned off                                                   |
| `PRAY TELL`                                      | Read input                  | *The Mikado*, Act I — Nanki-Poo: *"Gentlemen, I pray you tell me..."*             |
| `ASIDE:`                                         | Single-line comment         | Stage direction throughout every G&S libretto — heard by the audience, not the characters |
| `(ASIDE, AT SOME LENGTH:`                        | Multi-line comment (open)   | Gilbert's own parenthetical stage direction style                                 |
| `END OF ASIDE.)`                                 | Multi-line comment (close)  | Closes the parenthetical aside                                                    |
| `LEGEND:`                                        | Documentation — summary     | Placed inside a documentation comment block; one-line summary of the symbol       |
| `RECITATIVE:`                                    | Documentation — remarks     | Placed inside a documentation comment block; additional remarks, may span multiple lines |
| `ARTICLE <name> (<type>):`                       | Documentation — parameter   | Placed inside a documentation comment block; describes a function parameter       |
| `CONSEQUENCE (<type>):`                          | Documentation — return      | Placed inside a documentation comment block; describes the return value           |
| `CURSES <name> (<type>):`                        | Documentation — thrown      | Placed inside a documentation comment block; describes a thrown value             |
| `CHORUS:`                                        | Documentation — example     | Placed inside a documentation comment block; introduces a code example            |
| `ENSEMBLE:`                                      | Documentation — see also    | Placed inside a documentation comment block; see-also reference                   |
| `STATUTORY:`                                     | Documentation — deprecated  | Placed inside a documentation comment block; marks the symbol as deprecated       |
| `SUM OF ... AND ...`                             | Addition                    | —                                                                                 |
| `DIFFERENCE OF ... AND ...`                      | Subtraction                 | —                                                                                 |
| `PRODUCT OF ... AND ...`                         | Multiplication              | —                                                                                 |
| `QUOTIENT OF ... AND ...`                        | Division                    | —                                                                                 |
| `REMAINDER OF ... AND ...`                       | Modulo                      | —                                                                                 |
| `LARGER OF ... AND ...`                          | Maximum                     | —                                                                                 |
| `SMALLER OF ... AND ...`                         | Minimum                     | —                                                                                 |
| `WOVEN OF ... AND ... IF YOU PLEASE.`            | String concatenation        | Threads of `YARN` woven together; `IF YOU PLEASE` verbatim from *H.M.S. Pinafore* |
| `ALIKE ... AND ...`                              | Equality (==)               | —                                                                                 |
| `UNLIKE ... AND ...`                             | Inequality (!=)             | —                                                                                 |
| `PRE-ADAMITE ... AND ...`                        | Greater-than (>)            | *Ruddigore*, Act II — baronet "of Pre-Adamite antiquity"; he who predates all others has the greater standing |
| `LOWER DEGREE ... AND ...`                       | Less-than (<)               | *The Gondoliers* / *The Mikado* — the man of lower degree is beneath another     |
| `BOTH ... AND ...`                               | Logical AND                 | —                                                                                 |
| `EITHER ... OR ...`                              | Logical OR                  | —                                                                                 |
| `HARDLY EVER ...`                                | Logical NOT                 | *H.M.S. Pinafore* — "What, never? Well, **hardly ever!**"                         |
| `ALL OF ... IF YOU PLEASE.`                      | Variadic AND                | —                                                                                 |
| `ANY OF ... IF YOU PLEASE.`                      | Variadic OR                 | —                                                                                 |
| `CHORD OF ... AND ...`                           | Bitwise AND                 | —                                                                                 |
| `HARMONY OF ... AND ...`                         | Bitwise OR                  | —                                                                                 |
| `DISCORD OF ... AND ...`                         | Bitwise XOR                 | —                                                                                 |
| `INVERSION OF ...`                               | Bitwise NOT (unary)         | —                                                                                 |
| `TRANSPOSITION UP ...`                           | Left shift by 1 (unary)     | —                                                                                 |
| `TRANSPOSITION DOWN ...`                         | Right shift by 1 (unary)    | —                                                                                 |
| `IF YOU PLEASE.`                                 | Variable-length list closer | Verbatim *H.M.S. Pinafore* — Sir Joseph's insistence on the proper form of address; closes any open-ended argument list: `WOVEN OF`, `SUMMON`, `ALL OF`, `ANY OF` |
| `VERITY`                                          | Boolean true                | *Utopia, Limited* — "Henceforward, of a verity, with Fame ourselves we link"      |
| `NAY`                                             | Boolean false               | Throughout the canon — *Iolanthe*: "Nay, tempt me not"; *Ruddigore*: "Nay — that may never be" |
| `SHOULD IT TRANSPIRE THAT`                       | If condition; ternary separator | Lord Chancellor's conditional reasoning, *Iolanthe*; expression is required on the same line; also serves as the condition separator in ternary expressions; condition must be `DECREE` |
| `QUITE SO.`                                      | True branch                 | Verbatim *The Mikado* — Ko-Ko and Pooh-Bah's crisp affirmation                    |
| `OR, IF NOT,`                                    | Else-if                     | Lord Chancellor's enumeration of alternatives                                     |
| `OTHERWISE,`                                     | Else branch                 | Pooh-Bah switching between logical branches and capacities                        |
| `SO MUCH FOR THAT.`                              | End if                      | Ko-Ko's dismissive summary once a matter is disposed of                           |
| `IN WHICH CAPACITY?`                             | Switch                      | Verbatim Pooh-Bah register, *The Mikado* — "In which of my capacities?"; expression is required on the same line; case literal types must match the switch expression type |
| `WHEN ACTING AS`                                 | Case label                  | Pooh-Bah switching between his official capacities                                |
| `THAT WILL DO.`                                  | Break (loop or switch)      | Universal break — used dismissively throughout the canon; valid in both loop and switch contexts |
| `FAILING ALL OF THE ABOVE,`                      | Default case                | Lord Chancellor's catch-all provision                                             |
| `NOTHING COULD BE MORE SATISFACTORY.`            | End switch                  | Verbatim *The Mikado*, Act II — the Mikado's response to the fictitious execution  |
| `BY A LEGAL FICTION [KNOWN AS <label>]`           | Loop start                  | *Iolanthe* / *Ruddigore* — the legal fiction that permits repetition; label is optional |
| `ASCENDING`                                      | Increment loop var          | Ascending the peerage hierarchy — *Iolanthe*                                      |
| `DESCENDING`                                     | Decrement loop var          | Descending same                                                                   |
| `UNTIL`                                          | Loop exit condition         | *Pirates of Penzance* — Frederic bound *"until"* his 21st birthday                |
| `WHILST`                                         | Loop while condition        | Continue while true                                                               |
| `ONCE MORE.`                                     | Continue (loop)             | The stage call to repeat from the top of a passage; skips to the next iteration in all loop forms |
| `THE TERM EXPIRES.`                              | End loop                    | *The Grand Duke*, Act I — verbatim: the Statutory Duel law *"expires to-morrow"*  |
| `YEOMAN <condition>`                             | Guard clause start          | *The Yeoman of the Guard* — a yeoman stands watch and enforces; opens the guard block |
| `UNDER ORDERS.`                                  | End guard clause            | The yeoman's orders are discharged; closes the guard block                        |
| `IT IS MY DUTY TO PERFORM`                       | Function definition         | G&S obligation formula — used throughout the canon                                |
| `UNDER THE TERMS OF`                             | Function parameters         | *Pirates of Penzance* — Frederic's indenture specifies the *terms*; each parameter requires `AS A <type>` |
| `UNDER NO OBLIGATION`                            | No parameters               | A function bound by no terms                                                      |
| `TO FIND`                                        | Return type declaration     | Declares the function's return type; omitting it means void (no return value); `TO FIND NAUGHT` is not valid |
| `AND SO I FIND`                                  | Return with value           | *Trial by Jury* — the judicial verdict formula; expression must match the `TO FIND` type; type error in a void function |
| `MY DUTY IS DISCHARGED.`                         | End function                | The obligation is fulfilled                                                       |
| `MY DUTY IS PREMATURELY DISCHARGED.`             | Return (no value)           | Early exit — duty cut short; valid only in void functions; type error in a function declared with `TO FIND` |
| `SUMMON ... WITH ... IF YOU PLEASE.`             | Function call               | *The Mikado*, Act I — Ko-Ko: *"I summon my guard"*; `IF YOU PLEASE` from *Pinafore* |
| `SUMMON ... WITH NOTHING IF YOU PLEASE.`         | Call with no args           | —                                                                                 |
| `A HIDEOUS CURSE ON`                             | Throw exception             | *Ruddigore* — the Murgatroyd ancestral curse; raises an exception with the given `YARN` value; a non-`YARN` value is a type error; terminates programme if uncaught |
| `WITH THE GREATEST RESPECT,`                     | Try block                   | Victorian preamble acknowledging things may go awry                               |
| `WITH GRATITUDE`                                 | Success handler             | —                                                                                 |
| `MODIFIED RAPTURE, <name>`                       | Exception handler           | *Pirates of Penzance* — Mabel: "Oh joy! Oh rapture! — *modified* rapture!"; the named binding is **mandatory**; auto-declares `<name>` as a `YARN` variable scoped to the exception block |
| `THAT CONCLUDES THE MATTER.`                     | End try/catch               | —                                                                                 |
| `THE LAW IS <condition> THAT <error-message>`    | Assert statement            | The Mikado and Lord Chancellor as ultimate arbiters of law; `<condition>` must be `DECREE`, `<error-message>` must be `YARN`; if `NAY`, throws with the error message as payload |
| `THAT`                                           | Assert separator            | Structural separator between condition and error message within `THE LAW IS`; not in the keyword completion list |
| `PRAY ADMIT`                                     | Import                      | Formally admits another `.topsy` file into the programme's company                |
| `A LITTLE LIST OF <type>`                        | Array type annotation       | *The Mikado*, Act I — Ko-Ko's "I've Got a Little List"; every array is a catalogue of victims |
| `VICTIM <index> ON <array-or-yarn>`              | Array element / character access | The item at position `<index>` (1-based) on Ko-Ko's list; on a YARN, returns the STITCH at that position |
| `VICTIM <index> ON <array> IS APPOINTED <value>` | Array element assignment    | Replaces the item at position `<index>` with `<value>`; not valid on YARN (strings are immutable) |
| `RECKONING OF <array-or-yarn>`                   | Array / string length       | Ko-Ko's careful reckoning of his list; on a YARN, evaluates to the number of characters |
| `STANDING`                                       | Unsigned integer modifier   | Makes an integer type unsigned; placed between the mutability modifier and the integer type keyword (`STANDING PEER`, `STANDING CHANCELLOR`, `STANDING PIRATE`, `STANDING SAUSAGE-ROLL`) |

---

## 19. Type Reference

| Keyword                    | Width                        | Values / Range |
|----------------------------|------------------------------|----------------|
| `PEER`                     | 32-bit signed integer        | −2 147 483 648 to 2 147 483 647 |
| `STANDING PEER`            | 32-bit unsigned integer      | 0 to 4 294 967 295 |
| `CHANCELLOR`               | 64-bit signed integer        | −9 223 372 036 854 775 808 to 9 223 372 036 854 775 807 |
| `STANDING CHANCELLOR`      | 64-bit unsigned integer      | 0 to 18 446 744 073 709 551 615 |
| `PIRATE`                   | 16-bit signed integer        | −32 768 to 32 767 |
| `STANDING PIRATE`          | 16-bit unsigned integer      | 0 to 65 535 |
| `SAUSAGE-ROLL`             | 8-bit signed integer         | −128 to 127 |
| `STANDING SAUSAGE-ROLL`    | 8-bit unsigned integer       | 0 to 255 |
| `FATHOM`                   | 64-bit float (double)        | Double-precision floating-point |
| `FOOT`                     | 32-bit float (single)        | Single-precision floating-point |
| `YARN`                     | string                       | Any sequence of characters in `""`; also supports `VICTIM` and `RECKONING OF` (see §16) |
| `STITCH`                   | character                    | Any single character; literal form `'A'` (see §3.3) |
| `DECREE`                   | boolean                      | `VERITY` or `NAY` |
| `NAUGHT`                   | null value only              | `NAUGHT` is a value, not a type. It may only be assigned to `YARN` or array variables. `AS A NAUGHT` is a type error. |
| `A LITTLE LIST OF <T>`     | ordered list                 | Ordered 1-based collection; element type is enforced — assigning a wrong-type element is a type error |

---

## 20. Operator Precedence

Because Topsy uses prefix notation throughout, there is no operator precedence ambiguity. Expressions are parsed left-to-right, with each operator consuming its arguments greedily.

```topsy
SUM OF PRODUCT OF 3 AND 4 AND 5
ASIDE: = (3 * 4) + 5 = 17
ASIDE: PRODUCT OF 3 AND 4 resolves first, yielding 12
ASIDE: then SUM OF 12 AND 5 = 17
```

---

## 21. Scoping

- Variables declared in `PRINCIPALS` or at the top level are **global**.
- Variables declared with `PRAY WELCOME` inside a function body are **local** to that function.
- Functions do not close over outer scope — they receive values only through parameters.
- Loop bodies share the scope of their enclosing block.

---

## 22. Line Structure

- Each statement occupies one line.
- `;` may be used to place two statements on one line (use sparingly; it is not very Victorian).
- Blank lines are ignored.
- Leading and trailing whitespace is ignored.

### The Victorian Flourish (`~`)

`~` is the Victorian flourish character and serves two distinct roles, unambiguous by context:

**Line continuation** — `~` at the end of a line (outside a string literal) continues the current statement onto the next line:

```topsy
AND SO I FIND ~
  SUM OF ~
    SUMMON fibonacci WITH DIFFERENCE OF n AND 1 IF YOU PLEASE. ~
    AND SUMMON fibonacci WITH DIFFERENCE OF n AND 2 IF YOU PLEASE.
```

**String escape prefix** — `~` inside a `YARN` literal introduces an escape sequence (see §7):

| Sequence | Meaning              |
|----------|----------------------|
| `~n`     | Newline              |
| `~t`     | Tab                  |
| `~"`     | Literal double-quote |
| `~~`     | Literal tilde        |

```topsy
BEHOLD "First line~nSecond line~n~tIndented third line"
```

A `~` at the end of a line is always a continuation character; a `~` inside a string literal is always an escape prefix. The two roles never overlap.

---

## 23. A Note on Style

The spirit of Topsy is the spirit of Gilbert & Sullivan: **formal, absurd, and utterly deadpan.** Programmers are encouraged to:

- Name their variables after G&S characters and concepts
- Write comments in the style of stage directions
- Give their programs a proper title *and* an `or,` subtitle
- Treat their `PRINCIPALS` section as a genuine *Dramatis Personae* with descriptive parentheticals in `ASIDE:` comments
- Approach every `IT IS MY DUTY TO PERFORM` with the gravity the occasion demands
- Write keywords in uppercase — it is not required, but it is the convention, and it gives the libretto its proper authority on the page

A well-written Topsy program, read aloud, should be indistinguishable from the libretto of a previously undiscovered Savoy opera.

---

## 24. Complete Example

```topsy
HARK! "The Gondolier's Dilemma"
  or, "A Computation in Which Primality is Determined"

ASIDE: Determine whether a number is prime.

PRINCIPALS
  PRAY WELCOME candidate AS A PEER
  PRAY WELCOME input     AS A YARN
THE CURTAIN RISES.

IT IS MY DUTY TO PERFORM is_prime UNDER THE TERMS OF n AS A PEER TO FIND DECREE
  SHOULD IT TRANSPIRE THAT ALIKE n AND SMALLER OF n AND 1
    QUITE SO.
      AND SO I FIND NAY
  SO MUCH FOR THAT.
  PRAY WELCOME i AS A PEER BEING 2
  BY A LEGAL FICTION KNOWN AS trial WHILST LOWER DEGREE PRODUCT OF i AND i AND SUM OF n AND 1
    SHOULD IT TRANSPIRE THAT ALIKE REMAINDER OF n AND i AND 0
      QUITE SO.
        AND SO I FIND NAY
    SO MUCH FOR THAT.
    i IS APPOINTED SUM OF i AND 1
  THE TERM EXPIRES.
  AND SO I FIND VERITY
MY DUTY IS DISCHARGED.

BEHOLD "Pray enter a number for examination:"
PRAY TELL input
candidate IS APPOINTED AS IT WERE input AS A PEER

SHOULD IT TRANSPIRE THAT SUMMON is_prime WITH candidate IF YOU PLEASE.
  QUITE SO.
    BEHOLD WOVEN OF candidate AND " is prime — a most singular distinction." IF YOU PLEASE.
  OTHERWISE,
    BEHOLD WOVEN OF candidate AND " is not prime — as one might have expected." IF YOU PLEASE.
SO MUCH FOR THAT.

FINALE.
```

---

*Topsy Turvy — In the Gilbert & Sullivan tradition of telling a perfectly outrageous story in a completely deadpan way.*
