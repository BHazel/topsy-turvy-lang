# Topsy Turvy
## A Gilbert & Sullivan Operetta Programming Language
### Language Specification — Version 0.3.0

> *"Things are seldom what they seem; skim milk masquerades as cream."*
> — H.M.S. Pinafore

---

## Overview

**Topsy Turvy** is a general-purpose esoteric programming language in the tradition of LOLCODE, themed around the Gilbert & Sullivan operetta canon. Programs are written in the voice of a Victorian theatrical libretto — formal, pompous, comic, and entirely deadpan. The underlying semantics are those of a conventional dynamically-typed procedural language; only the syntax is topsy-turvy.

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
- `BEING <value>` — optional initial value, in the manner of a *Dramatis Personae* parenthetical ("Nanki-Poo, *being* the son of the Mikado..."); if omitted, the variable is initialised to `NAUGHT` (null)
- `THE CURTAIN RISES.` — closes the `PRINCIPALS` block. Once all characters have been introduced and the company is assembled, the curtain rises and the drama begins. The full stop is mandatory.

### 3.1 Constants and Mutability Modifiers

An optional mutability modifier may appear between `AS A` and the type keyword to declare whether a variable is a constant or a mutable variable:

| Modifier | Meaning |
|---|---|
| `CONSERVATIVE` | The variable is a **constant**. Once declared, it cannot be reassigned by `IS APPOINTED`, recast in place by `IS HENCEFORTH A`, or overwritten by `PRAY TELL`. Attempting any of these operations is a runtime error. |
| `LIBERAL` | The variable is explicitly **mutable**. This is identical to declaring without a modifier and exists for documentation clarity. |
| *(none)* | Mutable — the default when no modifier is given. All existing code continues to work unchanged. |

```topsy
PRAY WELCOME LovesickMaidens AS A CONSERVATIVE PEER BEING 20
PRAY WELCOME TotalLords       AS A LIBERAL      PEER BEING  0
PRAY WELCOME Ko-Ko            AS A PEER         BEING  0
```

`CONSERVATIVE` draws from the G&S tradition of immovable institutional authority — the House of Lords in *Iolanthe*, the Lord Chancellor, the ancestral portraits in *Ruddigore*: things that, by long-established rule, simply *cannot* be changed. *"I often think it's comical / Fal lal la! / How Nature always does contrive / Fal lal la! / That every boy and every gal / That's born into the world alive / Is either a little Liberal / Or else a little Conservative! / Fal lal la!"* — *Iolanthe*, Act II. The modifier restores Gilbert's own distinction to the language: those values that are fixed by decree, and those that may yet be persuaded.

### 3.2 Dynamic Typing

Topsy Turvy is a **dynamically typed** language in the Python tradition: type annotations are **advisory**, not enforced.

**The declared type is documentation, not a constraint.** A variable declared as `PEER` may hold a `YARN` value after a subsequent `IS APPOINTED`. The interpreter will not raise an error when a value of a different type is stored. This mirrors Python's behaviour with annotated variables:

```python
# Python — valid at runtime, annotation is advisory
Ko_Ko: int = 42
Ko_Ko = "Lord High Executioner"  # no error
```

The equivalent in Topsy Turvy:

```topsy
PRAY WELCOME Ko-Ko AS A PEER BEING 42
Ko-Ko IS APPOINTED "Lord High Executioner"  ASIDE: perfectly legal — annotation is advisory
```

**Explicit casts are still required to convert values.** Storing a different type does not convert it; `IS HENCEFORTH A` or `AS IT WERE` must be used when a specific type is needed:

```topsy
Ko-Ko IS HENCEFORTH A PEER  ASIDE: now converts whatever Ko-Ko holds to PEER
```

**Type annotations serve three purposes:**
1. They set the initial value's type when `BEING` is provided.
2. They document the programmer's intent for future readers.
3. They inform the LSP hover tooltip.

This philosophy extends to collection types. An `A LITTLE LIST OF YARN` declares the programmer's intent that the list should contain strings — but the runtime will not reject an element of a different type (see §14 Arrays).

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

| Type keyword  | Equivalent | Description |
|---------------|------------|-------------|
| `PEER`        | integer    | A Peer of the Realm — a whole number, positive or negative. *From Iolanthe.* |
| `FATHOM`      | float      | A nautical measure — a number of real precision. *From H.M.S. Pinafore.* |
| `YARN`        | string     | A wandering minstrel's stock-in-trade — a sequence of characters. *From The Mikado.* |
| `DECREE`      | boolean    | A ruling that stands or does not stand — either `VERITY` or `NAY`. *The Mikado*, Act I: "So he decreed, in words succinct..." |
| `NAUGHT`      | null       | Nothing. Not even that. |

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
Ko-Ko IS HENCEFORTH A PEER                                          ASIDE: re-cast Ko-Ko to integer in place

PRAY WELCOME age AS A YARN BEING AS IT WERE Ko-Ko AS A YARN         ASIDE: cast in a declaration initialiser

ageStr IS APPOINTED AS IT WERE Ko-Ko AS A YARN                      ASIDE: cast in an assignment

AS IT WERE Ko-Ko AS A YARN                                          ASIDE: standalone cast — result stored in JUST SO
```

- `IS HENCEFORTH A <type>` — casts the variable in place (statement)
- `AS IT WERE <expr> AS A <type>` — a cast **expression** that evaluates to the cast value without mutating the source; "as it were" is the G&S hedging construction, used when a character invokes a convenient fiction about what something actually is. Because it is an expression, it can appear anywhere a value is expected: as the right-hand side of `IS APPOINTED`, as the `BEING` initialiser of a declaration, as a function argument, or as a sub-expression. When used as a standalone statement, the result is stored in the implicit `JUST SO` variable for use in subsequent statements.

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

- `PRAY TELL <variable>` — reads a line from standard input into the variable as a `YARN`; use `IS HENCEFORTH A PEER` to cast to integer if needed. Drawn verbatim from *The Mikado*, Act I — Nanki-Poo's opening recitative: *"Gentlemen, I pray you tell me / Where a gentle maiden dwelleth..."*

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

## 6. String Operations

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

## 7. Comparison & Boolean Logic

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

### Truthiness

When a non-`DECREE` value is used in a boolean context:
- `NAY`: `0`, `0.0`, `""`, `NAUGHT`
- `VERITY`: everything else

---

## 8. Built-In Variables

### JUST SO

Any expression that is evaluated but not explicitly assigned deposits its result in the implicit variable **`JUST SO`**. This is used primarily to feed values into conditional constructs without an intermediate assignment.

> *"Merely corroborative detail, intended to give artistic verisimilitude to an otherwise bald and unconvincing narrative."* — The Mikado

```topsy
ALIKE Ko-Ko AND 0
ASIDE: JUST SO now holds VERITY or NAY
SHOULD IT TRANSPIRE THAT
  QUITE SO.
    BEHOLD "Ko-Ko's value is zero — most irregular."
SO MUCH FOR THAT.
```

When the inline conditional form is used (`SHOULD IT TRANSPIRE THAT <expression>` or `IN WHICH CAPACITY? <expression>`), the expression is evaluated directly and `JUST SO` is bypassed — the expression's result is consumed immediately by the conditional and is not deposited into `JUST SO`.

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

## 9. Conditionals

### If / Else If / Else

Two equivalent forms are supported. In the **two-line form**, the expression is evaluated on the preceding line (depositing its result in `JUST SO`), and `SHOULD IT TRANSPIRE THAT` appears alone on the next line. In the **inline form**, the expression is supplied directly on the same line as the keyword, bypassing `JUST SO`.

**Two-line form:**
```topsy
<expression>
SHOULD IT TRANSPIRE THAT
  QUITE SO.
    <true block>
  OR, IF NOT, <expression>
    <else-if block>
  OTHERWISE,
    <else block>
SO MUCH FOR THAT.
```

**Inline form:**
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

- `SHOULD IT TRANSPIRE THAT` — the conditional phrasing of the Lord Chancellor in *Iolanthe*, who frames every legal determination as something that *transpires* to be the case; in the two-line form it evaluates `JUST SO` as a `DECREE`; in the inline form the supplied expression is evaluated directly
- `QUITE SO.` — the true branch; verbatim from *The Mikado*, used by Ko-Ko and Pooh-Bah as a crisp affirmation that the established fact is confirmed
- `OR, IF NOT, <expression>` — else-if; evaluates a new expression; the Lord Chancellor's habit of carefully enumerating alternatives
- `OTHERWISE,` — the else branch; Pooh-Bah explicitly uses "otherwise" and "on the other hand" when switching between his many logical branches and capacities
- `SO MUCH FOR THAT.` — closes the conditional block; Ko-Ko's characteristic dismissive summary once a matter has been disposed of

**Example (two-line form):**

```topsy
ALIKE rank AND "Admiral"
SHOULD IT TRANSPIRE THAT
  QUITE SO.
    BEHOLD "He is the Ruler of the Queen's Navee!"
  OR, IF NOT, ALIKE rank AND "Captain"
    BEHOLD "What, never? Well, hardly ever!"
  OTHERWISE,
    BEHOLD "A mere landsman."
SO MUCH FOR THAT.
```

**Example (inline form):**

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

As with the conditional, two equivalent forms are supported. In the **two-line form**, the expression is evaluated on the preceding line (depositing its result in `JUST SO`), and `IN WHICH CAPACITY?` appears alone on the next line. In the **inline form**, the expression follows immediately after the `?`.

**Two-line form:**
```topsy
<expression>
IN WHICH CAPACITY?
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

**Inline form:**
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

- `IN WHICH CAPACITY?` — drawn from Pooh-Bah's response when addressed: *"In which of my capacities?"* — he holds so many offices that the caller must specify which one they are invoking; in the two-line form it switches on `JUST SO`; in the inline form the supplied expression is evaluated directly
- `WHEN ACTING AS <literal>` — case label; mirrors Pooh-Bah switching between his official capacities; cases fall through unless broken
- `THAT WILL DO.` — the universal break keyword; used dismissively throughout the G&S canon — by the Mikado, by Ko-Ko, by the Lord Chancellor — to signal that a matter is concluded and no further elaboration is required; valid in both switch and loop contexts
- `FAILING ALL OF THE ABOVE,` — default case; the Lord Chancellor's catch-all when none of the specific provisions apply
- `NOTHING COULD BE MORE SATISFACTORY.` — closes the switch block; verbatim from *The Mikado*, Act II — the Mikado's response upon receiving the report of the (entirely fictitious) execution, delivered with great satisfaction while everything is in fact catastrophically wrong

**Example (two-line form):**

```topsy
office
IN WHICH CAPACITY?
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

**Example (inline form):**

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

---

## 10. Loops

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
- `THAT WILL DO.` — breaks out of the innermost loop immediately; the universal break keyword, valid in both loop and switch contexts — see §9
- `ONCE MORE.` — skips the remainder of the current iteration and proceeds immediately to the next; the stage call to repeat from the top of a passage. Applies to all loop forms.
- `THE TERM EXPIRES.` — closes the loop. Drawn from *The Grand Duke*, Act I: the Statutory Duel law *"expires to-morrow"* — a recurring obligation reaching its natural terminus.

### Ascending (Counted Up) Loop

```topsy
BY A LEGAL FICTION KNOWN AS counter ASCENDING i UNTIL ALIKE i AND 10
  BEHOLD i
THE TERM EXPIRES.
```

- `ASCENDING <var>` — increments `<var>` by 1 at the end of each iteration; `<var>` begins at `0`
- `UNTIL <expression>` — exits when the expression is `VERITY` (checked before each iteration). From *The Pirates of Penzance* — Frederic's indenture binds him *"until"* his twenty-first birthday.

### Descending (Counted Down) Loop

```topsy
BY A LEGAL FICTION KNOWN AS countdown DESCENDING i UNTIL ALIKE i AND 0
  BEHOLD i
THE TERM EXPIRES.
```

- `DESCENDING <var>` — decrements `<var>` by 1 at the end of each iteration

### While Loop

```topsy
BY A LEGAL FICTION KNOWN AS watchman WHILST UNLIKE Ko-Ko AND 0
  BEHOLD Ko-Ko
  Ko-Ko IS APPOINTED DIFFERENCE OF Ko-Ko AND 1
THE TERM EXPIRES.
```

- `WHILST <expression>` — continues while expression is `VERITY` (checked before each iteration; no automatic variable mutation)

---

## 11. Functions

### Declaration

```topsy
IT IS MY DUTY TO PERFORM <name> UNDER THE TERMS OF <param1> [AND <param2> ...]
  <body>
  AND SO I FIND <expression>
MY DUTY IS DISCHARGED.
```

- `IT IS MY DUTY TO PERFORM <name>` — declares a function; the G&S obligation formula, used throughout the canon
- `UNDER THE TERMS OF <param1> [AND <param2> ...]` — declares parameters. From *The Pirates of Penzance*: Frederic's indenture specifies the exact *terms* under which his obligation is to be performed. Parameters are the terms of the indenture.
- `UNDER NO OBLIGATION` — for functions with no parameters
- `AND SO I FIND <expression>` — returns a value; the judicial verdict formula from *Trial by Jury*
- `MY DUTY IS DISCHARGED.` — closes the function; the obligation is fulfilled
- `MY DUTY IS PREMATURELY DISCHARGED.` — early return with no value
- Functions have their own scope; they receive values only through parameters

### Calling

```topsy
SUMMON factorial WITH 5 IF YOU PLEASE.
PRAY WELCOME answer AS A PEER BEING SUMMON factorial WITH n IF YOU PLEASE.
SUMMON greet WITH NOTHING IF YOU PLEASE.
```

- `SUMMON <name> WITH <arg1> [AND <arg2> ...] IF YOU PLEASE.` — calls a function. `SUMMON` is verbatim from *The Mikado*, Act I — Ko-Ko: *"I summon my guard."* To summon a named party to perform their duty, with the specified terms, if they would be so kind. `IF YOU PLEASE` is verbatim from *H.M.S. Pinafore* — Sir Joseph Porter's insistence on the proper form of address.
- `SUMMON <name> WITH NOTHING IF YOU PLEASE.` — calls a function with no arguments
- The return value becomes `JUST SO`, or can be used directly in an expression

**Example — Factorial:**

```topsy
IT IS MY DUTY TO PERFORM factorial UNDER THE TERMS OF n
  ALIKE n AND 0
  SHOULD IT TRANSPIRE THAT
    QUITE SO.
      AND SO I FIND 1
  SO MUCH FOR THAT.
  AND SO I FIND PRODUCT OF n AND SUMMON factorial WITH DIFFERENCE OF n AND 1 IF YOU PLEASE.
MY DUTY IS DISCHARGED.
```

---

## 12. Exception Handling

### Throwing

```topsy
A HIDEOUS CURSE ON <value>
```

`A HIDEOUS CURSE ON <value>` — raises an exception, carrying `<value>` as the exception payload. Drawn from *Ruddigore*: the Murgatroyd baronets are bound by an ancestral curse — to hurl a hideous curse upon something is the most dramatically appropriate signal that affairs have gone catastrophically wrong. May be used anywhere in the programme; if uncaught by a `WITH THE GREATEST RESPECT` block the programme terminates with an error and the cursed value is reported.

### Catching

```topsy
WITH THE GREATEST RESPECT, <operation>
  WITH GRATITUDE
    <success block>
  MODIFIED RAPTURE[, <name>]
    <exception block>
THAT CONCLUDES THE MATTER.
```

- `WITH THE GREATEST RESPECT, <operation>` — wraps a potentially-failing operation; catches any exception raised by `A HIDEOUS CURSE ON` within `<operation>`
- `WITH GRATITUDE` — the success handler; entered when no exception is raised
- `MODIFIED RAPTURE[, <name>]` — the exception handler; from *The Pirates of Penzance*: Mabel's "Oh joy! Oh rapture! — *modified* rapture!" upon learning the bad news; the cursed value is always available as `JUST SO` on entry to this block; if `, <name>` is given, the cursed value is also auto-declared as a named variable scoped to the exception block (no prior `PRAY WELCOME` required)
- `THAT CONCLUDES THE MATTER.` — closes the block

**Example:**

```topsy
IT IS MY DUTY TO PERFORM checked_divide UNDER THE TERMS OF a AND b
  ALIKE b AND 0
  SHOULD IT TRANSPIRE THAT
    QUITE SO.
      A HIDEOUS CURSE ON "Division by zero — the Pirate King is most displeased."
  SO MUCH FOR THAT.
  AND SO I FIND QUOTIENT OF a AND b
MY DUTY IS DISCHARGED.

WITH THE GREATEST RESPECT, SUMMON checked_divide WITH 10 AND 0 IF YOU PLEASE.
  WITH GRATITUDE
    BEHOLD WOVEN OF "Result: " AND JUST SO IF YOU PLEASE.
  MODIFIED RAPTURE
    BEHOLD WOVEN OF "A curse has been invoked: " AND JUST SO IF YOU PLEASE.
THAT CONCLUDES THE MATTER.
```

The optional `<name>` after `MODIFIED RAPTURE` binds the cursed value to a named variable for the duration of the exception block.  The named variable is auto-declared — no `PRAY WELCOME` is needed — and is not accessible outside the block.  `JUST SO` is still set regardless.

```topsy
WITH THE GREATEST RESPECT, SUMMON checked_divide WITH 10 AND 0 IF YOU PLEASE.
  WITH GRATITUDE
    BEHOLD WOVEN OF "Result: " AND JUST SO IF YOU PLEASE.
  MODIFIED RAPTURE, Grievance
    BEHOLD WOVEN OF "A curse has been invoked: " AND Grievance IF YOU PLEASE.
THAT CONCLUDES THE MATTER.
```

---

## 13. Libraries & Imports

```topsy
PRAY ADMIT "filename"
```

`PRAY ADMIT "filename"` — admits another `.topsy` file into the current programme's company. All `IT IS MY DUTY TO PERFORM` declarations in that file become available. Drawn from the theatrical tradition of formally admitting a new party to the assembled company — the doorkeeper admits the newcomer, who then takes their place among the principals already on stage.

---

## 14. Arrays

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
- `<type>` — the declared element type (`PEER`, `FATHOM`, `YARN`, `DECREE`, or `NAUGHT`); advisory only — see §3.2.
- `<size>` — an optional integer literal placed between `LITTLE LIST OF` and `<type>`; pre-allocates the array with that many `NAUGHT` elements, making element assignment (`VICTIM n ON arr IS APPOINTED val`) usable without a `BEING` clause. A size of `0` produces an empty array. A negative size is a runtime error.
- `BEING <expr> AND <expr> ... IF YOU PLEASE.` — initial element list; follows the same `IF YOU PLEASE.` convention as other variable-length constructs (see §6); omitting `BEING` produces an empty array, **not** `NAUGHT`.
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

`VICTIM <index> ON <array>` is an **expression** that evaluates to the element at position `<index>`. `<index>` is 1-based — `VICTIM 1` is the first element. `<index>` may be any expression that evaluates to a `PEER`. Accessing an out-of-range index is a runtime error.

*`VICTIM` — Ko-Ko's little list consists of intended victims; every item retrieved from the list is, necessarily, a victim.*

### Setting an Element

```topsy
VICTIM <index> ON <array> IS APPOINTED <value>
```

```topsy
VICTIM 2 ON miscreants IS APPOINTED "Nanki-Poo"
```

Replaces the element at position `<index>` with `<value>`. If the array was declared `CONSERVATIVE`, attempting to set an element is a runtime error.

### Array Truthiness

| State      | Truthiness |
|------------|------------|
| Non-empty  | `VERITY`   |
| Empty      | `NAY`      |

### Display

`BEHOLD` renders an array as a comma-separated, bracket-enclosed list of its elements' string representations:

```topsy
BEHOLD miscreants   ASIDE: prints ["Pooh-Bah", "Ko-Ko", "Pish-Tush"]
```

### Note on Element-Type Enforcement

Per §3.2, the declared element type is advisory. `VICTIM n ON arr IS APPOINTED 42` is valid even if `arr` was declared `A LITTLE LIST OF YARN` — no runtime error will be raised. This matches the general dynamic-typing philosophy of the language.

---

## 15. Documentation Comments

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
  CURSES SameValues (DECREE): Thrown if Start and End are the same.
  CHORUS:
  SUMMON SumRange WITH 1 AND 10 IF YOU PLEASE.
  ENSEMBLE: AnotherFunc
END OF ASIDE.)
IT IS MY DUTY TO PERFORM SumRange UNDER THE TERMS OF Start AND End
  PRAY WELCOME Total AS A PEER BEING 0
  ASIDE: Code logic...
  A HIDEOUS CURSE ON SameValues
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

## 16. Complete Keyword Reference

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
| `IS HENCEFORTH A`                                | In-place cast               | *Iolanthe* — the Fairy Queen's transforming declaration                           |
| `AS IT WERE`                                     | Expression cast             | G&S hedging construction — invoking a convenient fiction about what something is  |
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
| `IF YOU PLEASE.`                                 | Variable-length list closer | Verbatim *H.M.S. Pinafore* — Sir Joseph's insistence on the proper form of address; closes any open-ended argument list: `WOVEN OF`, `SUMMON`, `ALL OF`, `ANY OF` |
| `JUST SO`                                        | Implicit result variable    | *The Mikado* — "Just so!" — the thing just established                            |
| `VERITY`                                          | Boolean true                | *Utopia, Limited* — "Henceforward, of a verity, with Fame ourselves we link"      |
| `NAY`                                             | Boolean false               | Throughout the canon — *Iolanthe*: "Nay, tempt me not"; *Ruddigore*: "Nay — that may never be" |
| `SHOULD IT TRANSPIRE THAT`                       | If condition (two-line or inline) | Lord Chancellor's conditional reasoning, *Iolanthe*; two-line form reads `JUST SO`, inline form takes expression directly |
| `QUITE SO.`                                      | True branch                 | Verbatim *The Mikado* — Ko-Ko and Pooh-Bah's crisp affirmation                    |
| `OR, IF NOT,`                                    | Else-if                     | Lord Chancellor's enumeration of alternatives                                     |
| `OTHERWISE,`                                     | Else branch                 | Pooh-Bah switching between logical branches and capacities                        |
| `SO MUCH FOR THAT.`                              | End if                      | Ko-Ko's dismissive summary once a matter is disposed of                           |
| `IN WHICH CAPACITY?`                             | Switch (two-line or inline) | Verbatim Pooh-Bah register, *The Mikado* — "In which of my capacities?"; two-line form switches on `JUST SO`, inline form takes expression directly |
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
| `IT IS MY DUTY TO PERFORM`                       | Function definition         | G&S obligation formula — used throughout the canon                                |
| `UNDER THE TERMS OF`                             | Function parameters         | *Pirates of Penzance* — Frederic's indenture specifies the *terms*                |
| `UNDER NO OBLIGATION`                            | No parameters               | A function bound by no terms                                                      |
| `AND SO I FIND`                                  | Return with value           | *Trial by Jury* — the judicial verdict formula                                    |
| `MY DUTY IS DISCHARGED.`                         | End function                | The obligation is fulfilled                                                       |
| `MY DUTY IS PREMATURELY DISCHARGED.`             | Return (no value)           | Early exit — duty cut short                                                       |
| `SUMMON ... WITH ... IF YOU PLEASE.`             | Function call               | *The Mikado*, Act I — Ko-Ko: *"I summon my guard"*; `IF YOU PLEASE` from *Pinafore* |
| `SUMMON ... WITH NOTHING IF YOU PLEASE.`         | Call with no args           | —                                                                                 |
| `A HIDEOUS CURSE ON`                             | Throw exception             | *Ruddigore* — the Murgatroyd ancestral curse; raises an exception with the given value; terminates programme if uncaught |
| `WITH THE GREATEST RESPECT,`                     | Try block                   | Victorian preamble acknowledging things may go awry                               |
| `WITH GRATITUDE`                                 | Success handler             | —                                                                                 |
| `MODIFIED RAPTURE[, <name>]`                     | Exception handler           | *Pirates of Penzance* — Mabel: "Oh joy! Oh rapture! — *modified* rapture!"; cursed value available as `JUST SO`; optional `, <name>` auto-declares a binding in the exception block scope |
| `THAT CONCLUDES THE MATTER.`                     | End try/catch               | —                                                                                 |
| `PRAY ADMIT`                                     | Import                      | Formally admits another `.topsy` file into the programme's company                |
| `A LITTLE LIST OF <type>`                        | Array type annotation       | *The Mikado*, Act I — Ko-Ko's "I've Got a Little List"; every array is a catalogue of victims |
| `VICTIM <index> ON <array>`                      | Array element access        | The item at position `<index>` (1-based) on Ko-Ko's list                          |
| `VICTIM <index> ON <array> IS APPOINTED <value>` | Array element assignment    | Replaces the item at position `<index>` with `<value>`                            |

---

## 17. Type Reference

| Keyword                 | Type         | Values                                      |
|-------------------------|--------------|---------------------------------------------|
| `PEER`                  | Integer      | Any whole number                            |
| `FATHOM`                | Float        | Any real number                             |
| `YARN`                  | String       | Any sequence of characters in `""`          |
| `DECREE`                | Boolean      | `VERITY` or `NAY`                           |
| `NAUGHT`                | Null         | `NAUGHT`                                    |
| `A LITTLE LIST OF <T>`  | Array of `T` | Ordered 1-based collection; element type advisory (see §3.2) |

---

## 18. Operator Precedence

Because Topsy uses prefix notation throughout, there is no operator precedence ambiguity. Expressions are parsed left-to-right, with each operator consuming its arguments greedily.

```topsy
SUM OF PRODUCT OF 3 AND 4 AND 5
ASIDE: = (3 * 4) + 5 = 17
ASIDE: PRODUCT OF 3 AND 4 resolves first, yielding 12
ASIDE: then SUM OF 12 AND 5 = 17
```

---

## 19. Scoping

- Variables declared in `PRINCIPALS` or at the top level are **global**.
- Variables declared with `PRAY WELCOME` inside a function body are **local** to that function.
- Functions do not close over outer scope — they receive values only through parameters.
- Loop bodies share the scope of their enclosing block.

---

## 20. Line Structure

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

**String escape prefix** — `~` inside a `YARN` literal introduces an escape sequence (see §6):

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

## 21. A Note on Style

The spirit of Topsy is the spirit of Gilbert & Sullivan: **formal, absurd, and utterly deadpan.** Programmers are encouraged to:

- Name their variables after G&S characters and concepts
- Write comments in the style of stage directions
- Give their programs a proper title *and* an `or,` subtitle
- Treat their `PRINCIPALS` section as a genuine *Dramatis Personae* with descriptive parentheticals in `ASIDE:` comments
- Approach every `IT IS MY DUTY TO PERFORM` with the gravity the occasion demands
- Write keywords in uppercase — it is not required, but it is the convention, and it gives the libretto its proper authority on the page

A well-written Topsy program, read aloud, should be indistinguishable from the libretto of a previously undiscovered Savoy opera.

---

## 22. Complete Example

```topsy
HARK! "The Gondolier's Dilemma"
  or, "A Computation in Which Primality is Determined"

ASIDE: Determine whether a number is prime.

PRINCIPALS
  PRAY WELCOME candidate AS A PEER
  PRAY WELCOME i         AS A PEER
THE CURTAIN RISES.

IT IS MY DUTY TO PERFORM is_prime UNDER THE TERMS OF n
  ALIKE n AND SMALLER OF n AND 1
  SHOULD IT TRANSPIRE THAT
    QUITE SO.
      AND SO I FIND NAY
  SO MUCH FOR THAT.
  PRAY WELCOME i AS A PEER BEING 2
  BY A LEGAL FICTION KNOWN AS trial WHILST UNLIKE i AND PRODUCT OF i AND i
    UNLIKE REMAINDER OF n AND i AND 0
    SHOULD IT TRANSPIRE THAT
      QUITE SO.
        AND SO I FIND NAY
    SO MUCH FOR THAT.
    i IS APPOINTED SUM OF i AND 1
  THE TERM EXPIRES.
  AND SO I FIND VERITY
MY DUTY IS DISCHARGED.

BEHOLD "Pray enter a number for examination:"
PRAY TELL candidate
candidate IS HENCEFORTH A PEER

SUMMON is_prime WITH candidate IF YOU PLEASE.
SHOULD IT TRANSPIRE THAT
  QUITE SO.
    BEHOLD WOVEN OF candidate AND " is prime — a most singular distinction." IF YOU PLEASE.
  OTHERWISE,
    BEHOLD WOVEN OF candidate AND " is not prime — as one might have expected." IF YOU PLEASE.
SO MUCH FOR THAT.

FINALE.
```

---

*Topsy Turvy — Version 0.3.0 — In the Gilbert & Sullivan tradition of telling a perfectly outrageous story in a completely deadpan way.*
