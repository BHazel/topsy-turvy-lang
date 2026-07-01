# UtopIR
## An Intermediary Representation for Topsy Turvy
### Language Specification: Version 0.0.1-preview1

---

## Overview

**UtopIR** is an intermediary representation (IR) for the Topsy Turvy esoteric programming language inspired by other IRs such as the .NET CIL and LLVM IR.  It provides a shorthand and flattened representation of Topsy Turvy intended for use in code emitters for targeting multiple platforms.

The language is designed around virtual registers and a stack.

---

## 1. File Structure

> TODO: The `duty ... discharged` block structure is currently not in scope for development.  The current implementation are only concerned with instructions themselves that will eventually sit within this block.

Unlike in Topsy Turvy, UtopIR does not have an equivalent to the `HARK!`...`FINALE.` keywords.  Instead a file must have an `Opera` function which serves as the entry point to a programme.

```utopir
duty Opera terms TheProps: yarn finds peer
    @ Main programme code...
    find 0
discharged
```

Please see the **Functions** section for more information on functions.

## 2. Comments

Single-line comments are supported in UtopIR and delimited using the `@` character.  Any text following the `@` character on a line is ignored by the compiler.

```utopir
@ This is a comment, ignored by the compiler.
```

## 3. Code Structure

### 3.1. Instructions

UtopIR instructions all take a similar format of `instruction [<operand1>, <operand2>, ...]`, where there can be 0 or more operands as required by the instruction.

### 3.2. Variables

Variables all start with the `£` character and can have any valid name supported in Topsy Turvy following the `£` character.  All variables in UtopIR should be considered virtual registers.

### 3.2.2. Temporary Variables

Although not enforced variables starting with `£_` should be considered temporary variables and used in larger statements and expressions comprised of other expressions, for example, multiple arithmetic operations.

As an example, the arithmetic expression `3 + 4 - 5` in Topsy Turvy would be:

```topsy
Result IS APPOINTED DIFFERENCE OF SUM OF 3 AND 4 AND 5
```

In UtopIR, using temporary variables, would be:

```utopir
£_sum_3_4 = sum 3, 4
£_diff__sum_3_4_5 = diff £_sum_3_4, 5
£Result = appoint £_diff__sum_3_4_5
```

### 3.2.3. Constants

Constants are not directly supported in UtopIR.  A constant declared in Topsy Turvy using the `CONSERVATIVE` modifier will never change throughout the lifetime of a programme, therefore, its literal value can be used inline directly in UtopIR code.

## 4. Instructions

### 4.1. Declaration & Assignment

The following operations work with variables and their values.

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`welcome <type>`|Variable Declaration|Virtual Register|`£Lords = welcome peer`|
|`appoint <value>`|Variable Assignment|Virtual Register|`£Lords = appoint 42`|

#### 4.1.1 `welcome` Instruction

The `welcome` instruction declares a variable of a specified `type` and assigns it to a virtual register.

**Operands**

* **`<type>`:** The type of the variable with options being any supported type; please see Appendix B for the complete type list.

**Format**

```utopir
£<var-name> = welcome <type>
```

**Example**

For the following Topsy Turvy code:

```topsy
PRAY WELCOME LovesickMaidens AS A PEER
PRAY WELCOME PoemSubject AS A YARN
```

the following UtopIR is equivalent:

```utopir
£LovesickMaidens = welcome peer
£PoemSubject = welcome yarn
```

#### 4.1.2 `appoint` Instruction

The `appoint` instruction assigns a spceified `value` to a variable and assigns it to a declared virtual register.  Assigning a value of a type that is incompatible with the variable declaration is a compilation error.

**Operands**

* **`<value>`:** The value to assign to a variable; it can be a literal or a variable.

**Format**

```utopir
£<var-name> = appoint <value>
```

**Example**

For the following Topsy Turvy code:

```topsy
LovesickMaidens IS APPOINTED 20
PoemSubject IS APPOINTED "Hollow"
```

the following UtopIR is equivalent:

```utopir
£LovesickMaidens = appoint 20
£PoemSubject = appoint "Hollow"
```

### 4.2. Arithmetic Operations

The following instructions provide basic arithmetic operations.  Operands must be of the same numeric type in the same instruction; using two types that are incompatible is a compilation error.

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`sum <op1>, <op2>`|Addition|Virtual Register|`£Lords = sum 10, 20`|
|`diff <op1>, <op2>`|Subtraction|Virtual Register|`£Lords = diff 10, 5`|
|`prod <op1>, <op2>`|Multiplication|Virtual Register|`£Lords = prod 10, 2`|
|`quot <op1>, <op2>`|Division|Virtual Register|`£Lords = quot 10, 5`|
|`rem <op1>, <op2>`|Remainder|Virtual Register|`£Lords = rem 10, 5`|
|`max <op1>, <op2>`|Maximum Operand|Virtual Register|`£Biggest = max 10, 5`|
|`min <op1>, <op2>`|Minimum Operand|Virtual Register|`£Smallest = min 10, 5`|

#### 4.2.1. `sum` Instruction

The `sum` instruction adds two numeric values together (`<op1> + <op2>`) and assigns the result to a variable.  Operands must be of the same numeric type in the same instruction; using two types that are incompatible is a compilation error.

**Operands**

* **`<op1>`:** The first numeric operand, either literal or variable.
* **`<op2>`:** The second numeric operand, either literal or variable.

**Format**

```utopir
£<var-name> = sum <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Lords = SUM OF 10 AND 20
```

the following UtopIR is equivalent:

```utopir
£Lords = sum 10, 20
```

#### 4.2.2. `diff` Instruction

The `diff` instruction subtracts one operand from another (`<op1> - <op2>`) and assigns the result to a variable.  Operands must be of the same numeric type in the same instruction; using two types that are incompatible is a compilation error.

**Operands**

* **`<op1>`:** The first numeric operand, either literal or variable.
* **`<op2>`:** The second numeric operand, either literal or variable.

**Format**

```utopir
£<var-name> = diff <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Lords = DIFFERENCE OF 20 AND 10
```

the following UtopIR is equivalent:

```utopir
£Lords = diff 20, 10
```

#### 4.2.3. `prod` Instruction

The `prod` instruction multiples two operands together (`<op1> * <op2>`) and assigns the result to a variable.  Operands must be of the same numeric type in the same instruction; using two types that are incompatible is a compilation error.

**Operands**

* **`<op1>`:** The first numeric operand, either literal or variable.
* **`<op2>`:** The second numeric operand, either literal or variable.

**Format**

```utopir
£<var-name> = prod <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
BanquetDrinks = PRODUCT OF 10 AND 20
```

the following UtopIR is equivalent:

```utopir
£BanquetDrinks = prod 10, 20
```

#### 4.2.4. `quot` Instruction

The `quot` instruction divides one operand by another (`<op1> / <op2>`) and assigns the result to a variable.  Operands must be of the same numeric type in the same instruction; using two types that are incompatible is a compilation error.

**Operands**

* **`<op1>`:** The first numeric operand, either literal or variable.
* **`<op2>`:** The second numeric operand, either literal or variable.

**Format**

```utopir
£<var-name> = quot <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
BanquetDrinksPerPerson = QUOTIENT OF 20 AND 10
```

the following UtopIR is equivalent:

```utopir
£BanquetDrinksPerPerson = quot 20, 10
```

#### 4.2.5. `rem` Instruction

The `rem` instruction finds the remainder, or modulo, of one operand from another (`<op1> % <op2>`) and assigns the result to a variable.  Operands must be of the same numeric type in the same instruction; using two types that are incompatible is a compilation error.

**Operands**

* **`<op1>`:** The first numeric operand, either literal or variable.
* **`<op2>`:** The second numeric operand, either literal or variable.

**Format**

```utopir
£<var-name> = rem <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
SpareBanquetDrinks = REMAINDER OF 20 AND 10
```

the following UtopIR is equivalent:

```utopir
£SpareBanquetDrinks = rem 20, 10
```

#### 4.2.5. `max` Instruction

The `max` instruction returns the maximum of the two operands (`max(<op1>, <op2>)`) and assigns the result to a variable.  Operands must be of the same numeric type in the same instruction; using two types that are incompatible is a compilation error.

**Operands**

* **`<op1>`:** The first numeric operand, either literal or variable.
* **`<op2>`:** The second numeric operand, either literal or variable.

**Format**

```utopir
£<var-name> = max <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
FairiesOrLords = LARGER OF 10 AND 20
```

the following UtopIR is equivalent:

```utopir
£FairiesOrLords = max 10, 20
```

#### 4.2.6. `min` Instruction

The `min` instruction returns the minimum of the two operands (`min(<op1>, <op2>)`) and assigns the result to a variable.  Operands must be of the same numeric type in the same instruction; using two types that are incompatible is a compilation error.

**Operands**

* **`<op1>`:** The first numeric operand, either literal or variable.
* **`<op2>`:** The second numeric operand, either literal or variable.

**Format**

```utopir
£<var-name> = min <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
FairiesOrLords = SMALLER OF 10 AND 20
```

the following UtopIR is equivalent:

```utopir
£FairiesOrLords = min 10, 20
```

### 4.3 Stack Operations

The following instructions work with the stack:

|Instruction|Description|Mechanism|Example|
|-|-|-|
|`prentice <op>`|Pushes the operand onto the stack.|Stack|`prentice 42`|
|`leave`|Pops the top value off the stack.|Stack|`£Lords = leave`|

#### 4.3.1. `prentice` Instruction

The `prentice` instruction pushes a value onto the current stack frame.

**Operands**

* `**<value>**:` The value to push onto the stack; either literal or variable.

**Format**

```utopir
prentice <value>
```

**Example**

There is no direct equivalent of the stack operations in Topsy Turvy.  The following example pushes a literal and a variable onto the current stack frame.

```utopir
prentice 42
prentice £LovesickMaidens
```

#### 4.3.2. `leave` Instruction

The `leave` instruction pops a value from the current stack frame and assigns it to a virtual register.

**Operands**

_None_

**Format**

```utopir
£<var-name> = leave
```

**Example**

There is no direct equivalent of the stack operations in Topsy Turvy.  The following example pops the value off the top of the stack and assigns it to a virtual register.

```utopir
£LovesickMaidens = leave
```

### 4.4. Control Flow Instructions

The following instructions control the flow of the programme:

|Instruction|Description|Mechanism|Example|
|-|-|-|
|`find <value>`|Returns a value to the calling scope.|Virtual Register/Stack|`find 42`|

#### 4.4.1. `find` Instruction

The `find` instruction returns a value to a calling scope and can be a literal or variable value.

**Operands**

* **`<value>` _(optional)_:** Returns the specified value to the calling scope; if there is no value to return, such as a void function, this is omitted.

**Format**

```utopir
find [<value>]
```

**Example**

The following example in Topsy Turvy returns a value from a function:

```topsy
AND SO I FIND 42
```

with the following equivalent in UtopIR:

```utopir
find 42
```

To return no value the `find` instruction is called without an operand:

```utopir
find
```

## 5. Complete Example

The following example demonstrates the instruction set defined.

For the following Topsy Turvy code:

```topsy
PRAY WELCOME Peer1 AS A PEER BEING 42
PRAY WELCOME Peer2 AS A PEER BEING 30
PRAY WELCOME PeerResultA AS A PEER BEING SUM OF Peer1 AND Peer2

PRAY WELCOME Peer3 AS A PEER BEING 4
PRAY WELCOME Peer4 AS A PEER BEING 8
PRAY WELCOME PeerResultB AS A PEER
PeerResultB IS APPOINTED PRODUCT OF SUM OF Peer3 AND Peer4 AND PeerResult

AND SO I FIND PeerResultB
```

```utopir
£Peer1 = welcome peer
£Peer1 = appoint 42
£Peer2 = welcome peer
£Peer2 = appoint 30
£PeerResultA = welcome peer
£_sum_Peer1_Peer2 = sum £Peer1, £Peer2
£PeerResultA = appoint £_sum_Peer1_Peer2

£Peer3 = welcome peer
£Peer3 = appoint 4
£Peer4 = welcome peer
£Peer4 = appoint 8
£PeerResultB = welcome peer
£_sum_Peer3_Peer4 = sum £Peer3, £Peer4
£_prod__sum_Peer3_Peer4_PeerResultA = prod £_sum_Peer3_Peer4, £PeerResultA
£PeerResultB = appoint £_prod__sum_Peer3_Peer4_PeerResultA

find £PeerResultB
```

## Appendix A. Instruction Reference

|Instruction|Description|Example|
|-|-|-|
|`welcome <type>`|Variable Declaration|`£LovesickMaidens = welcome peer`|
|`appoint <value>`|Variable Assignment|`£LovesickMaidens = appoint 20`|
|`prentice <value>`|Push onto Stack|`prentice £LovesickMaidens`|
|`leave`|Pop off Stack|`£LovesickMaidens = leave`|
|`sum <op1>, <op2>`|Addition|`£Lords = sum 10, 20`|
|`diff <op1>, <op2>`|Subtraction|`£Lords = diff 10, 5`|
|`prod <op1>, <op2>`|Multiplication|`£Lords = prod 10, 2`|
|`quot <op1>, <op2>`|Division|`£Lords = quot 10, 5`|
|`rem <op1>, <op2>`|Remainder|`£Lords = rem 10, 5`|
|`find <value>`|Return a Value|`find £Lords`|

## Appendix B. Type Reference

|Type|Topsy Turvy Type|UtopIR Type|
|-|-|-|
|64-bit Integer|`CHANCELLOR`|`chancellor`|
|32-bit Integer|`PEER`|`peer`|
|16-bit Integer|`PIRATE`|`pirate`|
|8-bit Integer|`SAUSAGE-ROLL`|`sausageroll`|
|64-bit Floating-Point|`FATHOM`|`fathom`|
|32-bit Floating-Point|`FOOT`|`foot`|
|Boolean|`DECREE`|`decree`|
|Character|`STITCH`|`stitch`|
|String|`YARN`|`yarn`|

* For unsigned integers append the type with `standing`, e.g. for an unsigned 64-bit integer the type would be `standingchancellor`.
* The Boolean `decree` type defines its _true_ and _false_ literals as `verity` and `nay`.