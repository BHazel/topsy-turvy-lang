# UtopIR
## An Intermediary Representation for Topsy Turvy
### Language Specification: Version 0.0.1-preview4

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

Variables all start with the `£` character, followed by the variable name which can comprise a leading letter or underscore, then any mix of letters, digits, hyphens and underscores.  All variables in UtopIR should be considered virtual registers.

### 3.2.2. Temporary Variables

Temporary variables are intended for use in larger statements and expressions comprised of other expressions, for example, multiple arithmetic operations.  While they can be named any valid identifier, a naming convention of starting these variables with `£_` visually distinguishes them from user-declared variables.

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

### 3.3. Labels

Labels are specific points in UtopIR code to enable branching for control flow.  All labels start with the `!` character and follow the same naming rules as variables.  However, a naming convention is, when using letter characters, to only the upper-case variants.  Additionally, although indentation is not a requirement, a convention is to indent all code lines following a label up to another label or branch.

In UtopIR an example of using labels would be:

```utopir
£VarA = welcome decree
£VarA = appoint verity
£VarB = welcome decree
£VarB = appoint nay

£AreEqual_AB = both £VarA, £VarB
sailunlike £AreEqual_AB, !ARE_EQUAL_NAY
find 1

!ARE_EQUAL_NAY
  find 0
```

## 3.4. Functions

### 3.4.1. Function Identifiers

Function identifiers all start with the `&` character, followed by the function name which can comprise a leading letter or underscore, then any mix of letters, digits, hyphens and underscores.

## 4. Instructions

### 4.1. Declaration & Assignment

The following operations work with variables and their values.

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`welcome <type>`|Variable Declaration|Virtual Register|`£Lords = welcome peer`|
|`appoint <value>`|Variable Assignment|Virtual Register|`£Lords = appoint 42`|
|`were <value>, <type>`|Variable Cast|Virtual Register|`£Lords = were £LovesickMaidens, chancellor`|

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

The `appoint` instruction assigns a specified `value` to a variable and assigns it to a declared virtual register.  Assigning a value of a type that is incompatible with the variable declaration is a compilation error.

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

#### 4.1.3. `were` Instruction

The `were` instruction casts a value to a different type and assigns it to a declared virtual register.

**Operands**

* **`<value>`:** The value to assign to a variable; it can be a literal or a variable.
* **`<type>`:** The type of the variable with options being any supported type; please see Appendix B for the complete type list.

**Format**

```utopir
£<var-name> = were <value>, <type>
```

**Example**

For the following Topsy Turvy code:

```topsy
LovesickMaidens IS APPOINTED AS IT WERE Lords AS A CHANCELLOR
```

the following UtopIR is equivalent:

```utopir
£LovesickMaidens = were £Lords, chancellor
```

#### 4.1.4. `welcome.list` Instruction

The `welcome.list` instruction declares an array variable of a specified `type` and `size` and assigns it to a virtual register.

**Operands**

* **`<type>`:** The type of the array variable with options being any supported type; please see Appendix B for the complete type list.
* **`<size>`:** The size of the array variable as an integer literal; any value of any other type is a complilation error.

**Format**

```utopir
£<var-name> = welcome.list <type>, <size>
```

**Example**

For the following Topsy Turvy code:

```topsy
PRAY WELCOME Numbers AS A LITTLE LIST OF 3 PEER
```

or,

```topsy
PRAY WELCOME Numbers AS A LITTLE LIST OF PEER BEING 1 AND 2 AND 3 IF YOU PLEASE.
```

the following UtopIR is equivalent:

```utopir
£Numbers = welcome.list peer, 3
```

#### 4.1.5. `appoint.victim` Instruction

The `appoint.victim` instruction assigns a specified `value` to the element of an array variable at the specified `index`.  Assigning a value of a type that is incompatible with the variable declaration is a compilation error.

**Operands**

* **`<array>`:** The array variable to assign the value to an element; it must be an already declared variable.
* **`<index>`:** The index of the array of the element to assign; it must be an integer value either literal or variable.
* **`<value>`:** The value to assign to the array element; it must have a type compatible with the array variable type and can be either a literal or variable.

**Format**

```utopir
appoint.victim <array>, <index>, <value>
```

**Example**

For the following Topsy Turvy code:

```topsy
VICTIM 1 ON Numbers IS APPOINTED 10
VICTIM 2 ON Numbers IS APPOINTED 20
VICTIM 3 ON Numbers IS APPOINTED 30
```

the following UtopIR is equivalent:

```utopir
appoint.victim £Numbers, 1, 10
appoint.victim £Numbers, 2, 20
appoint.victim £Numbers, 3, 30
```

#### 4.1.6. `victim.yarn` Instruction

The `victim.yarn` instruction selects the `stitch` at the 1-based index of a `yarn` value and assigns it to a virtual register.  The variable being selected from must be of `yarn` type and the target virtual register must be of `stitch` type; the index must be of an integer type.  Using any other types is a compilation error.

**Operands**

* **`<yarn>`:** The `yarn` variable being selected, either literal or variable.
* **`<index>`:** The 1-based integer index of the `stitch` in the `yarn`, either a literal or variable.

**Format**

```utopir
£<var-name> = victim.yarn <yarn>, <index>
```

**Example**

For the following Topsy Turvy code:

```topsy
PoemSubjectLetter4 IS APPOINTED VICTIM 4 ON PoemSubject
```

the following UtopIR is equivalent:

```utopir
£PoemSubjectLetter4 = victim.yarn £PoemSubject, 4
```

#### 4.1.7. `victim.list` Instruction

The `victim.list` instruction selects the element at the 1-based index of an array variable and assigns it to a virtual register.  The array variable being selected from and the target virtual register must be of the same type; the index must be of an integer type.  Using any other types is a compilation error.

**Operands**

* **`<array>`:** The `array` variable being selected and must be an already declared and assigned variable.
* **`<index>`:** The 1-based integer index of the element in the array, either a literal or variable.

**Format**

```utopir
£<var-name> = victim.list <array>, <index>
```

**Example**

For the following Topsy Turvy code:

```topsy
NumbersElement2 IS APPOINTED VICTIM 2 ON Numbers
```

the following UtopIR is equivalent:

```utopir
£NumbersElement2 = victim.list £Numbers, 2
```

#### 4.1.8. `welcome.gallerypic` Instruction

The `welcome.gallerypic` instruction declares an pointer variable of a specified `type` and assigns it to a virtual register.

**Operands**

* **`<type>`:** The type of the pointer variable with options being any supported type (as outlined in Appendix B) or array; please see Appendix B for the complete type list.

**Format**

```utopir
£<var-name> = welcome.gallerypic <type>
```

**Example**

For the following Topsy Turvy code:

```topsy
PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER
```

the following UtopIR is equivalent:

```utopir
£NumberPointer = welcome.gallerypic peer
```

#### 4.1.9. `pictureto` Instruction

The `pictureto` instruction assigns a specified `variable` to a pointer variable.  The type of both the variable and its assigned pointer must be the same with the exceptions as outlined below; otherwise mismatched types is a compilation error.

Pointers to string `yarn` and array variables point to an individual `stitch` character or array element respectively.  Therefore for `yarn` variables, pointers must be of type `stitch`.  For array variables, pointers must be of the type of the array.

**Operands**

* **`<variable>`:** The variable pointee as the target of the pointer; this must be an already declared variable and must have a compatible type as outlined above.

**Format**

```utopir
£<pointer-name> = pictureto £<variable>
```

**Example**

For the following Topsy Turvy code:

```topsy
NumberPointer IS APPOINTED GALLERY PICTURE TO Number
```

the following UtopIR is equivalent:

```utopir
£NumberPointer = pictureto £Number
```

#### 4.1.10. `viewfrom` Instruction

The `viewfrom` instruction dereferences a pointer and assigns the value to a virtual register.  The type of both the pointer being dereferenced and the target virtual register must be the same; using different types is a compilation error.

**Operands**

* **`<pointer>`:** The pointer being dereferenced; this must be an already declared and assigned pointer.

**Format**

```utopir
£<var-name> = viewfrom £<pointer>
```

**Example**

For the following Topsy Turvy code:

```topsy
NumberValue IS APPOINTED VIEW FROM NumberPointer
```

the following UtopIR is equivalent:

```utopir
£NumberValue = viewfrom £NumberPointer
```

#### 4.1.11. `viewto` Instruction

The `viewto` instruction assigns a variable value through a pointer.  The type of both the pointer and the value to assign must be the same; using different types is a compilation error.

**Operands**

* **`<pointer>`:** The pointer used to assign the value; this must be an already declared and assigned pointer.
* **`<value>`:** The value to assign, either a literal or variable.

**Format**

```utopir
viewto £<pointer>, <value>
```

**Example**

For the following Topsy Turvy code:

```topsy
VIEW FROM NumberPointer IS APPOINTED 42
```

the following UtopIR is equivalent:

```utopir
viewto £NumberPointer, 42
```

### 4.2. Arithmetic Operations

This section describes arithmetic operations on supported types.  They are split into two groups:

* Integer Arithmetic, supporting all integer types.
* Floating-Point Arithmetic, supporting all floating-point types; these have the same names as the integer instructions but appended with the `.f` suffix.

#### 4.2.1 Integer Arithmetic

The following instructions provide basic arithmetic operations for integers.  Operands must be of the same type in the same instruction; using two types that are incompatible is a compilation error.

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`sum <op1>, <op2>`|Integer Addition|Virtual Register|`£Lords = sum 10, 20`|
|`diff <op1>, <op2>`|Integer Subtraction|Virtual Register|`£Lords = diff 10, 5`|
|`prod <op1>, <op2>`|Integer Multiplication|Virtual Register|`£Lords = prod 10, 2`|
|`quot <op1>, <op2>`|Integer Division|Virtual Register|`£Lords = quot 10, 5`|
|`rem <op1>, <op2>`|Integer Remainder|Virtual Register|`£Lords = rem 10, 5`|
|`max <op1>, <op2>`|Integer Maximum Operand|Virtual Register|`£Biggest = max 10, 5`|
|`min <op1>, <op2>`|Integer Minimum Operand|Virtual Register|`£Smallest = min 10, 5`|

##### 4.2.1.1. `sum` Instruction

The `sum` instruction adds two integer values together (`<op1> + <op2>`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

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

##### 4.2.1.2. `diff` Instruction

The `diff` instruction subtracts one integer from another (`<op1> - <op2>`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

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

##### 4.2.1.3. `prod` Instruction

The `prod` instruction multiples two integers together (`<op1> * <op2>`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

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

##### 4.2.1.4. `quot` Instruction

The `quot` instruction divides one integer by another (`<op1> / <op2>`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

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

##### 4.2.1.5. `rem` Instruction

The `rem` instruction finds the remainder, or modulo, of one integer from another (`<op1> % <op2>`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

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

##### 4.2.1.6. `max` Instruction

The `max` instruction returns the maximum of the two integer operands (`max(<op1>, <op2>)`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

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

##### 4.2.1.7. `min` Instruction

The `min` instruction returns the minimum of the two integer operands (`min(<op1>, <op2>)`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

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

#### 4.2.2. Floating-Point Arithmetic

The following instructions provide basic arithmetic operations for floating-point numbers.  Operands must be of the same type in the same instruction; using two different types is a compilation error.

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`sum.f <op1>, <op2>`|Floating-Point Addition|Virtual Register|`£Lords = sum.f 1.5, 2.5`|
|`diff.f <op1>, <op2>`|Floating-Point Subtraction|Virtual Register|`£Lords = diff.f 10.0, 2.5`|
|`prod.f <op1>, <op2>`|Floating-Point Multiplication|Virtual Register|`£Lords = prod.f 1.25, 3.5`|
|`quot.f <op1>, <op2>`|Floating-Point Division|Virtual Register|`£Lords = quot.f 15.0, 7.0`|
|`rem.f <op1>, <op2>`|Floating-Point Remainder|Virtual Register|`£Lords = rem.f 10.0, 5.0`|
|`max.f <op1>, <op2>`|Floating-Point Maximum Operand|Virtual Register|`£Biggest = max.f 10.5, 5.25`|
|`min.f <op1>, <op2>`|Floating-Point Minimum Operand|Virtual Register|`£Smallest = min.f 10.5, 5.25`|

##### 4.2.2.1. `sum.f` Instruction

The `sum.f` instruction adds two floating-point operands together (`<op1> + <op2>`) and assigns the result to a variable.  Operands must be of the same floating-point type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first floating-point operand, either literal or variable.
* **`<op2>`:** The second floating-point operand, either literal or variable.

**Format**

```utopir
£<var-name> = sum.f <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Lords = SUM OF 1.5 AND 2.5
```

the following UtopIR is equivalent:

```utopir
£Lords = sum.f 1.5, 2.5
```

##### 4.2.2.2. `diff.f` Instruction

The `diff.f` instruction subtracts one floating-point operand from another (`<op1> - <op2>`) and assigns the result to a variable.  Operands must be of the same floating-point type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first floating-point operand, either literal or variable.
* **`<op2>`:** The second floating-point operand, either literal or variable.

**Format**

```utopir
£<var-name> = diff.f <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Lords = DIFFERENCE OF 10.0 AND 2.5
```

the following UtopIR is equivalent:

```utopir
£Lords = diff.f 10.0, 2.5
```

##### 4.2.2.3. `prod.f` Instruction

The `prod.f` instruction multiples two floating-point operands together (`<op1> * <op2>`) and assigns the result to a variable.  Operands must be of the same floating-point type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first floating-point operand, either literal or variable.
* **`<op2>`:** The second floating-point operand, either literal or variable.

**Format**

```utopir
£<var-name> = prod.f <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
BanquetDrinks = PRODUCT OF 1.25 AND 3.5
```

the following UtopIR is equivalent:

```utopir
£BanquetDrinks = prod.f 1.25, 3.5
```

##### 4.2.2.4. `quot.f` Instruction

The `quot.f` instruction divides one floating-point operand by another (`<op1> / <op2>`) and assigns the result to a variable.  Operands must be of the same floating-point type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first floating-point operand, either literal or variable.
* **`<op2>`:** The second floating-point operand, either literal or variable.

**Format**

```utopir
£<var-name> = quot.f <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
BanquetDrinksPerPerson = QUOTIENT OF 15.0 AND 7.0
```

the following UtopIR is equivalent:

```utopir
£BanquetDrinksPerPerson = quot.f 15.0, 7.0
```

##### 4.2.2.5. `rem.f` Instruction

The `rem.f` instruction finds the remainder, or modulo, of one floating-point operand from another (`<op1> % <op2>`) and assigns the result to a variable.  Operands must be of the same floating-point type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first floating-point operand, either literal or variable.
* **`<op2>`:** The second floating-point operand, either literal or variable.

**Format**

```utopir
£<var-name> = rem.f <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
SpareBanquetDrinks = REMAINDER OF 10.0 AND 5.0
```

the following UtopIR is equivalent:

```utopir
£SpareBanquetDrinks = rem.f 10.0, 5.0
```

##### 4.2.2.6. `max.f` Instruction

The `max.f` instruction returns the maximum of the two floating-point operands (`max(<op1>, <op2>)`) and assigns the result to a variable.  Operands must be of the same floating-point type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first floating-point operand, either literal or variable.
* **`<op2>`:** The second floating-point operand, either literal or variable.

**Format**

```utopir
£<var-name> = max.f <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
FairiesOrLords = LARGER OF 10.5 AND 5.25
```

the following UtopIR is equivalent:

```utopir
£FairiesOrLords = max.f 10.5, 5.25
```

##### 4.2.2.7. `min.f` Instruction

The `min.f` instruction returns the minimum of the two floating-point operands (`min(<op1>, <op2>)`) and assigns the result to a variable.  Operands must be of the same floating-point type in the same instruction; using two different types is a compilation error.

**Operands**

* **`<op1>`:** The first floating-point operand, either literal or variable.
* **`<op2>`:** The second floating-point operand, either literal or variable.

**Format**

```utopir
£<var-name> = min.f <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
FairiesOrLords = SMALLER OF 10.5 AND 5.25
```

the following UtopIR is equivalent:

```utopir
£FairiesOrLords = min.f 10.5, 5.25
```

#### 4.2.3. Pointer Arithmetic

##### 4.2.3.1. `sum.g` Instruction

The `sum.g` instruction performs additive pointer arithmetic on a specified `pointer` by an `offset` and assigns the result to a variable.  The `pointer` must be a pointer variable to an array or `yarn` string and the offset must be an integer; using any other types is a compilation error.

**Operands**

* **`<pointer>`:** The pointer to an array or `yarn` string, which must be an already declared and assigned pointer.
* **`<offset>`:** The offset to add to the pointer position; must be an integer value, either literal or variable.

**Format**

```utopir
£<var-name> = sum.g <pointer>, <offset>
```

**Example**

For the following Topsy Turvy code:

```topsy
NumbersPointerOffset1 IS APPOINTED SUM OF NumbersPointer AND 1
```

the following UtopIR is equivalent:

```utopir
£NumbersPointerOffset1 = sum.g £NumbersPointer, 1
```

##### 4.2.3.2. `diff.g` Instruction

The `diif.g` instruction performs subtractive pointer arithmetic on a specified `pointer` by an `offset` and assigns the result to a variable.  The `pointer` must be a pointer variable to an array or `yarn` string and the offset must be an integer; using any other types is a compilation error.

**Operands**

* **`<pointer>`:** The pointer to an array or `yarn` string, which must be an already declared and assigned pointer.
* **`<offset>`:** The offset to subtract from the pointer position; must be an integer value, either literal or variable.

**Format**

```utopir
£<var-name> = diff.g <pointer>, <offset>
```

**Example**

For the following Topsy Turvy code:

```topsy
NumbersPointerOffset1 IS APPOINTED DIFFERENCE OF NumbersPointer AND 1
```

the following UtopIR is equivalent:

```utopir
£NumbersPointerOffset1 = diff.g £NumbersPointer, 1
```

### 4.3. Bitwise Operations

The following instructions provide bitwise operations for integers.  Operands must be of the same integer type in the same instruction; using two types that are incompatible or a non-integer type is a compilation error.

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`chord <op1>, <op2>`|Bitwise AND|Virtual Register|`£Result = chord 10, 20`|
|`harmony <op1>, <op2>`|Bitwise OR|Virtual Register|`£Result = harmony 10, 20`|
|`discord <op1>, <op2>`|Bitwise XOR|Virtual Register|`£Result = discord 10, 20`|
|`inv <op1>`|Bitwise NOT|Virtual Register|`£Result = inv 10`|
|`transup <op1>, <op2>`|Left Shift by `<op2>`|Virtual Register|`£Result = transup 10, 1`|
|`transdown <op1>, <op2>`|Right Shift by `<op2>`|Virtual Register|`£Result = transdown 10, 1`|

#### 4.3.1. `chord` Instruction

The `chord` instruction calculates the bitwise AND of two values (`<op1> & <op2>`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two types that are incompatible or a non-integer type is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

**Format**

```utopir
£<var-name> = chord <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Lords = CHORD OF 10 AND 20
```

the following UtopIR is equivalent:

```utopir
£Lords = chord 10, 20
```

#### 4.3.2. `harmony` Instruction

The `harmony` instruction calculates the bitwise OR of two values (`<op1> | <op2>`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two types that are incompatible or a non-integer type is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

**Format**

```utopir
£<var-name> = harmony <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Lords = HARMONY OF 10 AND 20
```

the following UtopIR is equivalent:

```utopir
£Lords = harmony 10, 20
```

#### 4.3.3. `discord` Instruction

The `discord` instruction calculates the bitwise XOR of two values (`<op1> ^ <op2>`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two types that are incompatible or a non-integer type is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

**Format**

```utopir
£<var-name> = discord <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Lords = DISCORD OF 10 AND 20
```

the following UtopIR is equivalent:

```utopir
£Lords = discord 10, 20
```

#### 4.3.4. `inv` Instruction

The `inv` instruction calculates the bitwise NOT of a value (`~<op1>`) and assigns the result to a variable.  The operand must be of an integer type; using a non-integer type is a compilation error.

**Operands**

* **`<op1>`:** The integer operand, either literal or variable.

**Format**

```utopir
£<var-name> = inv <op1>
```

**Example**

For the following Topsy Turvy code:

```topsy
Lords = INVERSION OF 10
```

the following UtopIR is equivalent:

```utopir
£Lords = inv 10
```

#### 4.3.5. `transup` Instruction

The `transup` instruction performs a left shift on a value by a specified amount (`<op1> << <op2>`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two types that are incompatible or a non-integer type is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

**Format**

```utopir
£<var-name> = transup <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Lords = TRANSPOSITION UP 10 BY 2
```

the following UtopIR is equivalent:

```utopir
£Lords = transup 10, 2
```

Setting the shift size in Topsy Turvy is optional and, if not explicitly set, defaults to 1.

#### 4.3.6. `transdown` Instruction

The `transdown` instruction performs a right shift on a value by a specified amount (`<op1> >> <op2>`) and assigns the result to a variable.  Operands must be of the same integer type in the same instruction; using two types that are incompatible or a non-integer type is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

**Format**

```utopir
£<var-name> = transdown <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Lords = TRANSPOSITION DOWN 10 BY 2
```

the following UtopIR is equivalent:

```utopir
£Lords = transdown 10, 2
```

Setting the shift size in Topsy Turvy is optional and, if not explicitly set, defaults to 1.

### 4.4. Stack Operations

The following instructions work with the stack:

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`prentice <op>`|Pushes the operand onto the stack.|Stack|`prentice 42`|
|`leave`|Pops the top value off the stack.|Stack|`£Lords = leave`|

#### 4.4.1. `prentice` Instruction

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

#### 4.4.2. `leave` Instruction

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

### 4.5. Comparison Instructions

This section describes comparison operations on supported types. They are split into two groups:

* Integer Comparison, supporting all integer and the `stitch` character types.
* Floating-Point Comparison, supporting all floating-point types; these have the same names as the integer instructions but appended with the .f suffix.

#### 4.5.1. Integer Comparison

The following instructions provide basic comparison operations for integers. Operands must be of the same type in the same instruction; using two types that are incompatible is a compilation error.

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`alike <op1>, <op2>`|Equality Comparison (`<op1> == <op2>`)|Virtual Register|`£Equal = alike £NumLords, £NumMaidens`|
|`unlike <op1>, <op2>`|Inequality Comparison (`<op1> != <op2>`)|Virtual Register|`£NotEqual = unlike £NumLords, £NumMaidens`|
|`preadam <op1>, <op2>`|Larger Than (`<op1> > <op2>`)|Virtual Register|`£MoreLords = preadam £NumLords, £NumMaidens`|
|`lowerdeg <op1>, <op2>`|Less Than (`<op1> < <op2>`)|Virtual Register|`£MoreMaidens = lowerdeg £NumLords, £NumMaidens`|

##### 4.5.1.1. `alike` Instruction

The `alike` instruction compares two integer values for equality (`<op1> == <op2>`) and assigns the result to a variable of `decree` type.  Operands must be of the same integer type in the same instruction and the result must be assigned to a variable of the `decree` type; using two different operand types is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

**Format**

```utopir
£<var-name> = alike <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Equal = ALIKE NumLords AND NumMaidens
```

the following UtopIR is equivalent:

```utopir
£Equal = alike £NumLords, £NumMaidens
```

##### 4.5.1.2. `unlike` Instruction

The `unlike` instruction compares two integer values for inequality (`<op1> != <op2>`) and assigns the result to a variable of `decree` type.  Operands must be of the same integer type in the same instruction and the result must be assigned to a variable of the `decree` type; using two different operand types is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

**Format**

```utopir
£<var-name> = unlike <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
NotEqual = UNLIKE NumLords AND NumMaidens
```

the following UtopIR is equivalent:

```utopir
£NotEqual = unlike £NumLords, £NumMaidens
```

##### 4.5.1.3. `preadam` Instruction

The `preadam` instruction compares two integer values to determine if the first is greater than the second (`<op1> > <op2>`) and assigns the result to a variable of `decree` type.  Operands must be of the same integer type in the same instruction and the result must be assigned to a variable of the `decree` type; using two different operand types is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

**Format**

```utopir
£<var-name> = preadam <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
MoreLords = PRE-ADAMITE NumLords AND NumMaidens
```

the following UtopIR is equivalent:

```utopir
£MoreLords = preadam £NumLords, £NumMaidens
```

##### 4.5.1.4. `lowerdeg` Instruction

The `lowerdeg` instruction compares two integer values to determine if the first is less than the second (`<op1> < <op2>`) and assigns the result to a variable of `decree` type.  Operands must be of the same integer type in the same instruction and the result must be assigned to a variable of the `decree` type; using two different operand types is a compilation error.

**Operands**

* **`<op1>`:** The first integer operand, either literal or variable.
* **`<op2>`:** The second integer operand, either literal or variable.

**Format**

```utopir
£<var-name> = lowerdeg <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
MoreMaidens = LOWER DEGREE NumLords AND NumMaidens
```

the following UtopIR is equivalent:

```utopir
£MoreMaidens = lowerdeg £NumLords, £NumMaidens
```

#### 4.5.2. Floating-Point Comparison

The following instructions provide basic comparison operations for floating-point numbers. Operands must be of the same type in the same instruction; using two types that are incompatible is a compilation error.

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`alike.f <op1>, <op2>`|Equality Comparison (`<op1> == <op2>`)|Virtual Register|`£Equal = alike.f £NumLords, £NumMaidens`|
|`unlike.f <op1>, <op2>`|Inequality Comparison (`<op1> != <op2>`)|Virtual Register|`£NotEqual = unlike.f £NumLords, £NumMaidens`|
|`preadam.f <op1>, <op2>`|Larger Than (`<op1> > <op2>`)|Virtual Register|`£MoreLords = preadam.f £NumLords, £NumMaidens`|
|`lowerdeg.f <op1>, <op2>`|Less Than (`<op1> < <op2>`)|Virtual Register|`£MoreMaidens = lowerdeg.f £NumLords, £NumMaidens`|

##### 4.5.2.1. `alike.f` Instruction

The `alike.f` instruction compares two floating-point values for equality (`<op1> == <op2>`) and assigns the result to a variable of `decree` type.  Operands must be of the same floating-point type in the same instruction and the result must be assigned to a variable of the `decree` type; using two different operand types is a compilation error.

**Operands**

* **`<op1>`:** The first floating-point operand, either literal or variable.
* **`<op2>`:** The second floating-point operand, either literal or variable.

**Format**

```utopir
£<var-name> = alike.f <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Equal = ALIKE NumLords AND NumMaidens
```

the following UtopIR is equivalent:

```utopir
£Equal = alike.f £NumLords, £NumMaidens
```

##### 4.5.2.2. `unlike.f` Instruction

The `unlike.f` instruction compares two floating-point values for inequality (`<op1> != <op2>`) and assigns the result to a variable of `decree` type.  Operands must be of the same floating-point type in the same instruction and the result must be assigned to a variable of the `decree` type; using two different operand types is a compilation error.

**Operands**

* **`<op1>`:** The first floating-point operand, either literal or variable.
* **`<op2>`:** The second floating-point operand, either literal or variable.

**Format**

```utopir
£<var-name> = unlike.f <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
NotEqual = UNLIKE NumLords AND NumMaidens
```

the following UtopIR is equivalent:

```utopir
£NotEqual = unlike.f £NumLords, £NumMaidens
```

##### 4.5.2.3. `preadam.f` Instruction

The `preadam.f` instruction compares two floating-point values to determine if the first is greater than the second (`<op1> > <op2>`) and assigns the result to a variable of `decree` type.  Operands must be of the same floating-point type in the same instruction and the result must be assigned to a variable of the `decree` type; using two different operand types is a compilation error.

**Operands**

* **`<op1>`:** The first floating-point operand, either literal or variable.
* **`<op2>`:** The second floating-point operand, either literal or variable.

**Format**

```utopir
£<var-name> = preadam.f <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
MoreLords = PRE-ADAMITE NumLords AND NumMaidens
```

the following UtopIR is equivalent:

```utopir
£MoreLords = preadam.f £NumLords, £NumMaidens
```

##### 4.5.2.4. `lowerdeg.f` Instruction

The `lowerdeg.f` instruction compares two floating-point values to determine if the first is less than the second (`<op1> < <op2>`) and assigns the result to a variable of `decree` type.  Operands must be of the same floating-point type in the same instruction and the result must be assigned to a variable of the `decree` type; using two different operand types is a compilation error.

**Operands**

* **`<op1>`:** The first floating-point operand, either literal or variable.
* **`<op2>`:** The second floating-point operand, either literal or variable.

**Format**

```utopir
£<var-name> = lowerdeg.f <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
MoreMaidens = LOWER DEGREE NumLords AND NumMaidens
```

the following UtopIR is equivalent:

```utopir
£MoreMaidens = lowerdeg £NumLords, £NumMaidens
```

### 4.6. Logical Operations

The following instructions provide logical operations for `decree` types.  Operands must be of `decree` type in the same instruction; using a non-`decree` type is a compilation error.

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`both <op1>, <op2>`|Logical AND|Virtual Register|`£Result = both verity, nay`|
|`either <op1>, <op2>`|Logical OR|Virtual Register|`£Result = either verity, nay`|
|`hardly <op1>`|Logical NOT|Virtual Register|`£Result = hardly verity`|

#### 4.6.1. `both` Instruction

The `both` instruction calculates the logical AND of two values (`<op1> AND <op2>`) and assigns the result to a variable.  Operands must be of the `decree` type in the same instruction; using a non-`decree` type is a compilation error.

**Operands**

* **`<op1>`:** The first `decree` operand, either literal or variable.
* **`<op2>`:** The second `decree` operand, either literal or variable.

**Format**

```utopir
£<var-name> = both <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Result = BOTH VERITY AND NAY
```

the following UtopIR is equivalent:

```utopir
£Result = both verity, nay
```

#### 4.6.2. `either` Instruction

The `either` instruction calculates the logical OR of two values (`<op1> OR <op2>`) and assigns the result to a variable.  Operands must be of the `decree` type in the same instruction; using a non-`decree` type is a compilation error.

**Operands**

* **`<op1>`:** The first `decree` operand, either literal or variable.
* **`<op2>`:** The second `decree` operand, either literal or variable.

**Format**

```utopir
£<var-name> = either <op1>, <op2>
```

**Example**

For the following Topsy Turvy code:

```topsy
Result = EITHER VERITY OR NAY
```

the following UtopIR is equivalent:

```utopir
£Result = either verity, nay
```

#### 4.6.3. `hardly` Instruction

The `hardly` instruction performs the logical NOT of a values (`NOT <op1>`) and assigns the result to a variable.  The operand must be of the `decree` type; using a non-`decree` type is a compilation error.

**Operands**

* **`<op1>`:** The first `decree` operand, either literal or variable.

**Format**

```utopir
£<var-name> = hardly <op1>
```

**Example**

For the following Topsy Turvy code:

```topsy
Result = HARDLY EVER VERITY
```

the following UtopIR is equivalent:

```utopir
£Result = hardly verity
```

### 4.7. Control Flow Instructions

The following instructions control the flow of the programme:

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`find <value>`|Returns a value to the calling scope.|Virtual Register/Stack|`find 42`|
|`sail <label>`|Branches to the specified label.|N/A|`sail LABEL`|
|`sailalike <value>, <label>`|Branches to the specified label if the `decree` value indicates equality.|`sailalike £IsLord, !LABEL`|
|`sailunlike <value>, <label>`|Branches to the specified label if the `decree` value indicates inequality.|`sailunlike £IsLord, !LABEL`|

#### 4.7.1. `find` Instruction

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

#### 4.7.2. `sail` Instruction

The `sail` instruction branches the code to the specified label, unconditionally.

**Operands**

* **`<label>`:** The label to branch to.

**Format**

```utopir
sail <label>
```

**Example**

There is no direct equivalent of the branch operations in Topsy Turvy.  The following example branches unconditionally to the specified label.  It should be noted the `find 0` line will never be executed.

```utopir
sail !LOGIC
find 0

!LOGIC
  find 1
```

#### 4.7.3. `sailalike` Instruction

The `sailalike` instruction branches the code to the specified label if the specified `decree` value is `verity`.  The value operand must be of type `decree`; any other type is a compilation error.

**Operands**

* **`<value>`:** The `decree` value being evaluated.
* **`<label>`:** The label to branch to.

**Format**

```utopir
sailalike <value>, <label>
```

**Example**

There is no direct equivalent of the branch operations in Topsy Turvy.  The following example branches unconditionally to the specified label.  It should be noted the `find 0` line will only be executed if `£Boolean` evaluates to `nay`.

```utopir
£Boolean = welcome decree
£Boolean = appoint verity

sailalike £Boolean, !IS_ALIKE
find 0

!IS_ALIKE
  find 1
```

#### 4.7.4. `sailunlike` Instruction

The `sailunlike` instruction branches the code to the specified label if the specified `decree` value is `nay`.  The value operand must be of type `decree`; any other type is a compilation error.

**Operands**

* **`<value>`:** The `decree` value being evaluated.
* **`<label>`:** The label to branch to.

**Format**

```utopir
sailunlike <value>, <label>
```

**Example**

There is no direct equivalent of the branch operations in Topsy Turvy.  The following example branches unconditionally to the specified label.  It should be noted the `find 0` line will only be executed if `£Boolean` evaluates to `verity`.

```utopir
£Boolean = welcome decree
£Boolean = appoint nay

sailunlike £Boolean, !IS_UNLIKE
find 0

!IS_UNLIKE
  find 1
```

### 4.8. Function Call Instructions

The following instructions are used to call functions:

|Instruction|Description|Mechanism|Example|
|-|-|-|-|
|`summon <function>`|Calls a void function or ignores the result.|Stack|`summon &Function`|
|`summon.find <function>`|Calls a returning function and collects the result.|Stack/Virtual Register|`£Result = summon.find &Function`|

The function call instructions use a combination of virtual registers and the stack to perform a function call:
* Each parameter is pushed onto the stack using the `prentice` instruction in the order as defined in the function definition in Topsy Turvy.  If there are no parameters no values are pushed onto the stack.
* A return value is stored in a virtual register.

#### 4.8.1. `summon` Instruction

The `summon` instruction calls a `function`, which must be accessible; calling an inaccessible function is a compilation error.  Both void and returning functions can be called using `summon`, however, a return value will always be ignored.

**Operands**

* **`<function>`:** The function to call.

**Format**

```utopir
summon <function>
```

**Example**

The following example in Topsy Turvy calls a void `&ReadPoem` function with a single `yarn` string parameter:

```topsy
SUMMON ReadPoem WITH Name IF YOU PLEASE.
```

with the following equivalent in UtopIR:

```utopir
prentice £Name
summon &ReadPoem
```

#### 4.8.2. `summon.find` Instruction

The `summon.find` instruction calls a `function`, which must be accessible; calling an inaccessible function is a compilation error.  Only returning functions can be called using `summon.find` and the return value is assigned to a virtual register.

**Operands**

* **`<function>`:** The function to call.

**Format**

```utopir
£<var-name> = summon.find <function>
```

**Example**

The following example in Topsy Turvy calls a `&GetPoem` function with a single `yarn` string parameter and a returned `yarn` value:

```topsy
Poem IS APPOINTED SUMMON GetPoem WITH Name IF YOU PLEASE.
```

with the following equivalent in UtopIR:

```utopir
prentice £Name
£Poem = summon.find &GetPoem
```

## 5. Examples

This section includes brief examples demonstrating how UtopIR instructions are used with Topsy Turvy equivalents.  Not all instructions are covered but sufficient are included in these examples for a working knowledge of UtopIR.

### 5.1. Declaration, Assignment, Casting & Integer Arithmetic

The following example demonstrates how declarations, assignments and casting instructions work along with integer arithmetic instructions.

For the following Topsy Turvy code:

```topsy
PRAY WELCOME Peer1 AS A PEER BEING 42
PRAY WELCOME Peer2 AS A PEER BEING 30
PRAY WELCOME PeerResultA AS A PEER BEING SUM OF Peer1 AND Peer2

PRAY WELCOME Peer3 AS A PEER BEING 4
PRAY WELCOME Chancellor4 AS A CHANCELLOR BEING 8
PRAY WELCOME ChancellorResultB AS A CHANCELLOR
ChancellorResultB IS APPOINTED PRODUCT OF SUM OF Peer3 AND Chancellor4 AND PeerResultA

AND SO I FIND ChancellorResultB
```

the following is the equivalent UtopIR code:

```utopir
@ This section uses the "verbose" temporary variable naming style.
£Peer1 = welcome peer
£Peer1 = appoint 42
£Peer2 = welcome peer
£Peer2 = appoint 30
£PeerResultA = welcome peer
£_sum_Peer1_Peer2 = sum £Peer1, £Peer2
£PeerResultA = appoint £_sum_Peer1_Peer2

@ This section uses the "numeric" temporary variable naming style.
£Peer3 = welcome peer
£Peer3 = appoint 4
£Chancellor4 = welcome chancellor
£_1 = were 8, chancellor
£Chancellor4 = appoint £_1
£ChancellorResultB = welcome chancellor
£_2 = were £Peer3, chancellor
£_3 = sum £_2, £Chancellor4
£_4 = were £PeerResultA, chancellor
£_5 = prod £_3, £_4
£ChancellorResultB = appoint £_5

£PeerReturnResult = welcome peer
£_6 = were £ChancellorResultB, peer
£PeerReturnResult = appoint £_6

find £PeerReturnResult
```

### 5.2. Floating-Point Arithmetic

The following example is similar to the example in §5.1 but using floating-point numbers.

For the following Topsy Turvy code:

```topsy
PRAY WELCOME Fathom1 AS A FATHOM BEING 42.5
PRAY WELCOME Fathom2 AS A FATHOM BEING 30.25
PRAY WELCOME FathomResultA AS A FATHOM BEING SUM OF Fathom1 AND Fathom2

PRAY WELCOME Fathom3 AS A FATHOM BEING 4.75
PRAY WELCOME Foot4 AS A FOOT BEING 8.5
PRAY WELCOME FathomResultB AS A FATHOM
FathomResultB IS APPOINTED PRODUCT OF SUM OF Fathom3 AND Foot4 AND FathomResultA

AND SO I FIND FathomResultB
```

the following is the equivalent UtopIR code:

```utopir
@ This section uses the "verbose" temporary variable naming style.
£Fathom1 = welcome fathom
£Fathom1 = appoint 42.5
£Fathom2 = welcome fathom
£Fathom2 = appoint 30.25
£FathomResultA = welcome fathom
£_sumf_Fathom1_Fathom2 = sum.f £Fathom1, £Fathom2
£FathomResultA = appoint £_sumf_Fathom1_Fathom2

@ This section uses the "numeric" temporary variable naming style.
£Fathom3 = welcome fathom
£Fathom3 = appoint 4.75
£Foot4 = welcome foot
£_1 = were 8.5, foot
£Foot4 = appoint £_1
£FathomResultB = welcome fathom
£_2 = were £Foot4, fathom
£_3 = sum.f £Fathom3, £_2
£_4 = prod.f £_3, £FathomResultA
£FathomResultB = appoint £_4

find £FathomResultB
```

### 5.3. Bitwise Operations

The following example demonstrates bitwise operations.

For the following Topsy Turvy code:

```topsy
ASIDE: 0b1001
PRAY WELCOME PeerMask AS A PEER BEING 9

ASIDE: 0b0011
PRAY WELCOME PeerValue AS A PEER BEING 3

ASIDE: 0b0001
PRAY WELCOME PeerAndResult AS A PEER BEING CHORD OF PeerMask AND PeerValue

ASIDE: 0b1110
PRAY WELCOME PeerNotResult AS A PEER BEING INVERSION OF PeerAndResult
```

the following is the equivalent UtopIR code:

```utopir
£PeerMask = welcome peer
£PeerMask = appoint 9
£PeerValue = welcome peer
£PeerValue = appoint 3
£_chord_PeerMask_PeerValue = chord £PeerMask, £PeerValue
£PeerAndResult = welcome peer
£PeerAndResult = appoint £_chord_PeerMask_PeerValue
£_inv_PeerAndResult = inv £PeerAndResult
£PeerNotResult = welcome peer
£PeerNotResult = appoint £_inv_PeerAndResult

find £PeerNotResult
```

### 5.4. Comparison, Logical & Branch Instructions

#### 5.4.1. If/Else-If/Else Conditional Blocks

This example demonstrates how comparison and logical operators are used in combination with branching instructions to represent a conditional If/Else-If/Else block in Topsy Turvy.

For the following example in Topsy Turvy

```topsy
PRAY WELCOME Peer1 AS A PEER BEING 42
PRAY WELCOME Peer2 AS A PEER BEING 23
PRAY WELCOME PeerResult AS A PEER

SHOULD IT TRANSPIRE THAT PRE-ADAMITE Peer1 AND Peer2
  QUITE SO.
    PeerResult IS APPOINTED 1
  OR, IF NOT, ALIKE Peer1 AND Peer2
    PeerResult IS APPOINTED 0
  OTHERWISE,
    PeerResult IS APPOINTED -1
SO MUCH FOR THAT.

AND SO I FIND PeerResult
```

the following is the equivalent UtopIR code:

```utopir
£Peer1 = welcome peer
£Peer1 = appoint 42
£Peer2 = welcome peer
£Peer2 = appoint 23
£PeerResult = welcome peer

£_preadam_Peer1_Peer2 = preadam £Peer1, £Peer2
£_alike_Peer1_Peer2 = alike £Peer1, £Peer2
sailalike £_preadam_Peer1_Peer2, !T1QS_PREADAM_PEER1_PEER2
sailalike £_alike_Peer1_Peer2, !T1OIN_ALIKE_PEER1_PEER2
sail !T1O

@ If "QUITE SO." branch.
!T1QS_PREADAM_PEER1_PEER2
  £PeerResult = appoint 1
  sail !T1SMFT

@ If Else "OR, IF NOT," branch.
!T1OIN_ALIKE_PEER1_PEER2
  £PeerResult = appoint 0
  sail !T1SMFT

@ Else "OTHERWISE," branch.
!T1O
  £PeerResult = appoint -1

!T1SMFT

find £PeerResult
```

#### 5.4.2. Ternary Expressions

This example demonstrates how comparison and logical operators are used in combination with branching instructions to represent a ternary expression in Topsy Turvy.

For the following example in Topsy Turvy

```topsy
PRAY WELCOME Peer1 AS A PEER BEING 42
PRAY WELCOME Peer2 AS A PEER BEING 23
PRAY WELCOME PeerResult AS A PEER

PeerResult IS APPOINTED 1 SHOULD IT TRANSPIRE THAT ALIKE Peer1 AND Peer2 OTHERWISE, 0

AND SO I FIND PeerResult
```

the following is the equivalent UtopIR code:

```utopir
£Peer1 = welcome peer
£Peer1 = appoint 42
£Peer2 = welcome peer
£Peer2 = appoint 23
£PeerResult = welcome peer

@ Additional variable created for ternary result to support nested expressions.
£_alike_Peer1_Peer2 = alike £Peer1, £Peer2
£_ternary_alike_Peer1_Peer2 = welcome peer
sailalike £_alike_Peer1_Peer2, !T1QS_ALIKE_PEER1_PEER2
sail !T1O

@ Ternary expressions are converted into If/Else equivalent blocks.
@ True Value, equivalent to If "QUITE SO." branch.
!T1QS_ALIKE_PEER1_PEER2
  £_ternary_alike_Peer1_Peer2 = appoint 1
  sail !T1SMFT

@ False Value, equivalent to Else "OTHERWISE," branch.
!T1O
  £_ternary_alike_Peer1_Peer2 = appoint 0

!T1SMFT

£PeerResult = appoint £_ternary_alike_Peer1_Peer2
find £PeerResult
```

#### 5.4.3. Guard Blocks

This example demonstrates how comparison and logical operators are used in combination with branching instructions to represent a guard clause in Topsy Turvy.

For the following example in Topsy Turvy

```topsy
PRAY WELCOME Peer1 AS A PEER BEING 42
PRAY WELCOME Peer2 AS A PEER BEING 23
PRAY WELCOME PeerResult AS A PEER

YEOMAN ALIKE Peer1 AND Peer2
  OTHERWISE,
    PeerResult IS APPOINTED 0
UNDER ORDERS.

PeerResult IS APPOINTED 1
AND SO I FIND PeerResult
```

the following is the equivalent UtopIR code:

```utopir
£Peer1 = welcome peer
£Peer1 = appoint 42
£Peer2 = welcome peer
£Peer2 = appoint 23
£PeerResult = welcome peer

£_alike_Peer1_Peer2 = alike £Peer1, £Peer2
sailunlike £_alike_Peer1_Peer2, !G1O
sail !G1UO

@ Guard condition failed, equivalent to the "OTHERWISE," block.
!G1O
  £PeerResult = appoint 0

!G1UO

£PeerResult = appoint 1
find £PeerResult
```

#### 5.4.4. Switch Blocks

This example demonstrates how comparison and logical operators are used in combination with branching instructions to represent a Switch block in Topsy Turvy.

For the following example in Topsy Turvy:

```topsy
PRAY WELCOME Peer1 AS A PEER BEING 10
PRAY WELCOME PeerResult AS A PEER

IN WHICH CAPACITY? Peer1
  WHEN ACTING AS 10
    PeerResult IS APPOINTED 1
    THAT WILL DO.
  WHEN ACTING AS 20
  WHEN ACTING AS 40
    PeerResult IS APPOINTED 2
    THAT WILL DO.
  WHEN ACTING AS 100
    PeerResult IS APPOINTED 3
  FAILING ALL OF THE ABOVE,
    PeerResult IS APPOINTED -1
NOTHING COULD BE MORE SATISFACTORY.

AND SO I FIND PeerResult
```

the following is the equivalent UtopIR code:

```utopir
£Peer1 = welcome peer
£Peer1 = appoint 10
£PeerResult = welcome peer

£_alike_Peer1_10 = alike £Peer1, 10
sailalike £_alike_Peer1_10, !C1AS_10

£_alike_Peer1_20 = alike £Peer1, 20
sailalike £_alike_Peer1_20, !C1AS_20

£_alike_Peer1_40 = alike £Peer1, 40
sailalike £_alike_Peer1_40, !C1AS_40

£_alike_Peer1_100 = alike £Peer1, 100
sailalike £_alike_Peer1_100, !C1AS_100

sail !C1FAIL

!C1AS_10
  £PeerResult = appoint 1
  sail !C1NCBMS
!C1AS_20
!C1AS_40
  £PeerResult = appoint 2
  sail !C1NCBMS
!C1AS_100
  £PeerResult = appoint 3
  sail !C1NCBMS
!C1FAIL
  £PeerResult = appoint -1

!C1NCBMS

find £PeerResult
```

#### 5.4.5. Basic Loops

This example demonstrates how branching instructions represent basic loops.

**It should be note this example is an infinite loop.**

For the following example in Topsy Turvy:

```topsy
PRAY WELCOME Toggle AS A DECREE BEING VERITY

BY A LEGAL FICTION
  Toggle IS APPOINTED HARDLY EVER Toggle
THE TERM EXPIRES.
```

the following is the equivalent UtopIR code:

```utopir
£Toggle = welcome decree
£Toggle = appoint verity

!L1
  £_hardly_Toggle = hardly £Toggle
  £Toggle = appoint £_hardly_Toggle
  sail !L1

!L1TTE
```

#### 5.4.6. Whilst Loops

This example demonstrates how comparison and logical operators are used in combination with branching instructions to represent basic loops with condition checking.

For the following example in Topsy Turvy:

```topsy
PRAY WELCOME Counter AS A PEER BEING 1
PRAY WELCOME Total AS A PEER BEING 0

BY A LEGAL FICTION WHILST LOWER DEGREE Counter AND 10
  Total IS APPOINTED SUM OF Total AND Counter
  Counter IS APPOINTED SUM OF Counter AND 2
THE TERM EXPIRES.

AND SO I FIND Total
```

the following is the equivalent UtopIR code:

```utopir
£Counter = welcome peer
£Counter = appoint 1
£Total = welcome peer
£Total = appoint 0

!L1W_LOWERDEG_COUNTER_10
  £_lowerdeg_Counter_10 = lowerdeg £Counter, 10
  sailunlike £_lowerdeg_Counter_10, !L1WTTE

  £_sum_Total_Counter = sum £Total, £Counter
  £Total = appoint £_sum_Total_Counter

  £_sum_Counter_2 = sum £Counter, 2
  £Counter = appoint £_sum_Counter_2
  sail !L1W_LOWERDEG_COUNTER_10

!L1WTTE

find £Total
```

#### 5.4.7. Ascending & Descending Loops

This example demonstrates how comparison and logical operators are used in combination with branching instructions to represent ascending and descending loops in Topsy Turvy.

For the following example in Topsy Turvy:

```topsy
PRAY WELCOME Counter AS A PEER BEING 1
PRAY WELCOME Total AS A PEER BEING 0

BY A LEGAL FICTION KNOWN AS Incrementer ASCENDING Counter UNTIL PRE-ADAMITE Counter AND 10
  Total IS APPOINTED SUM OF Total AND Counter
THE TERM EXPIRES.

AND SO I FIND Total
```

the following is the equivalent UtopIR code:

```utopir
£Counter = welcome peer
£Counter = appoint 1
£Total = welcome peer
£Total = appoint 0

!L1ASC_INCREMENTER_PREADAM_COUNTER_10
  £_preadam_Counter_10 = preadam £Counter, 10
  sailalike £_preadam_Counter_10, !L1ASCTTE

  £_sum_Total_Counter = sum £Total, £Counter
  £Total = appoint £_sum_Total_Counter

@ Always included as a "continue" target to ensure counter is incremented/decremented.
!L1ASCOM
  @ Descending loops would use the diff instruction instead of sum.
  £_sum_Counter_1 = sum £Counter, 1
  £Counter = appoint £_sum_Counter_1
  sail !L1ASC_INCREMENTER_PREADAM_COUNTER_10

!L1ASCTTE

find £Total
```

#### 5.4.8. Break and Continue Clauses

UtopIR has no equivalent of the Topsy Turvy `THAT WILL DO.` break clause or `ONCE MORE.` continue clause.  Use of the `sail`, `sailalike` and `sailunlike` instructions with labels will suffice.

For the following example in Topsy Turvy:

```topsy
PRAY WELCOME Counter AS A PEER BEING 1
PRAY WELCOME Total AS A PEER BEING 0

BY A LEGAL FICTION WHILST LOWER DEGREE Counter AND 20
  Counter IS APPOINTED SUM OF Counter AND 1

  SHOULD IT TRANSPIRE THAT ALIKE REMAINDER OF Counter AND 2 AND 0
    QUITE SO.
      ONCE MORE.
    OR, IF NOT, ALIKE Counter AND 13
      THAT WILL DO.
  SO MUCH FOR THAT.

  Total IS APPOINTED SUM OF Total AND Counter
THE TERM EXPIRES.

AND SO I FIND Total
```

the following is the equivalent UtopIR code:

```utopir
£Counter = welcome peer
£Counter = appoint 1
£Total = welcome peer
£Total = appoint 0

!L1W_LOWERDEG_COUNTER_20
  £_lowerdeg_Counter_20 = lowerdeg £Counter, 20
  sailunlike £_lowerdeg_Counter_20, !L1WTTE

  £_sum_Counter_1 = sum £Counter, 1
  £Counter = appoint £_sum_Counter_1

  £_rem_Counter_2 = rem £Counter, 2
  £_alike__rem_Counter_2_0 = alike £_rem_Counter_2, 0
  £_alike_Counter_13 = alike £Counter, 13
  sailalike £_alike__rem_Counter_2_0, !T2QS_ALIKE_REM_COUNTER_2_0
  sailalike £_alike_Counter_13, !T2OIN_ALIKE_COUNTER_13
  sail !T2SMFT

  !T2QS_ALIKE_REM_COUNTER_2_0
    sail !L1W_LOWERDEG_COUNTER_20

  !T2OIN_ALIKE_COUNTER_13
    sail !L1WTTE

  !T2SMFT

  £_sum_Total_Counter = sum £Total, £Counter
  £Total = appoint £_sum_Total_Counter
  sail !L1W_LOWERDEG_COUNTER_20

!L1WTTE

find £Total
```

### 5.5. String & Array Operations

This example demonstrates declaration and assignment of strings and arrays and accessing their characters and elements respectively.

For the following example in Topsy Turvy:

```topsy
PRAY WELCOME PoemSubject AS A YARN BEING "Hollow"
PRAY WELCOME PoemSubjectLetter4 AS A STITCH
PoemSubjectLetter4 IS APPOINTED VICTIM 4 ON PoemSubject

PRAY WELCOME Numbers AS A LITTLE LIST OF PEER BEING 10 AND 20 AND 30 IF YOU PLEASE.
PRAY WELCOME NumbersElement3 AS A PEER BEING VICTIM 3 ON Numbers
```

the following is the equivalent UtopIR code:

```utopir
£PoemSubject = welcome yarn
£PoemSubject = appoint "Hollow"
£PoemSubjectLetter4 = welcome stitch
£_victim_yarn_PoemSubject_4 = victim.yarn £PoemSubject, 4
£PoemSubjectLetter4 = appoint £_victim_yarn_PoemSubject_4

£Numbers = welcome.list peer, 3
appoint.victim £Numbers, 1, 10
appoint.victim £Numbers, 2, 20
appoint.victim £Numbers, 3, 30
£NumbersElement2 = welcome peer
£_victim_list_Numbers_2 = victim.list £Numbers, 2
£NumbersElement2 = appoint £_victim_list_Numbers_2
```

### 5.6 Pointer Operations

This example demonstrates declaration and assignment of pointers, how to dereference and perform arithmetic on them.

For the following example in Topsy Turvy:

```topsy
PRAY WELCOME Number AS A PEER BEING 42
PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Number

PRAY WELCOME NumberValue AS A PEER BEING VIEW FROM NumberPointer
VIEW FROM NumberPointer IS APPOINTED 23

PRAY WELCOME Numbers AS A LITTLE LIST OF PEER BEING 10 AND 20 AND 30 IF YOU PLEASE.
PRAY WELCOME NumbersPointer AS A GALLERY PICTURE OF PEER
NumbersPointer IS APPOINTED GALLERY PICTURE TO Numbers
NumbersPointer IS APPOINTED SUM OF NumbersPointer AND 2

PRAY WELCOME NullPointer AS A GALLERY PICTURE OF PEER
```

the following is the equivalent UtopIR code:

```utopir
£Number = welcome peer
£Number = appoint 42
£NumberPointer = welcome.gallerypic peer
£NumberPointer = pictureto £Number

£NumberValue = welcome peer
£NumberValue = viewfrom £NumberPointer
viewto £NumberPointer, 23

£Numbers = welcome.list peer, 3
appoint.victim £Numbers, 1, 10
appoint.victim £Numbers, 2, 20
appoint.victim £Numbers, 3, 30
£NumbersPointer = welcome.gallerypic peer
£NumbersPointer = pictureto £Numbers
£_sumg_NumbersPointer_2 = sum.g £NumbersPointer, 2
£NumbersPointer = appoint £_sumg_NumbersPointer_2

£NullPointer = welcome.gallerypic peer
£NullPointer = appoint naught
```

### 5.7 Calling Functions

This example demonstrates how to call functions, both void and returning.

**Please note that functions themselves are not yet implemented in UtopIR.**

```topsy
IT IS MY DUTY TO PERFORM DoNothing UNDER NO OBLIGATION
  SUM OF 0 AND 0
MY DUTY IS DISCHARGED.

IT IS MY DUTY TO PERFORM Add UNDER THE TERMS OF Num1 AS A PEER AND Num2 AS A PEER TO FIND PEER
  AND SO I FIND SUM OF Num1 AND Num2
MY DUTY IS DISCHARGED.

PRAY WELCOME AddResult AS A PEER

SUMMON DoNothing WITH NOTHING IF YOU PLEASE.
AddResult IS APPOINTED SUMMON Add WITH 4 AND 5 IF YOU PLEASE.

AND SO I FIND AddResult
```

```utopir
£AddResult = welcome peer
£AddResult = appoint 0

summon &DoNothing

prentice 4
prentice 5
£_summonfind_Add_4_5 = summon.find &Add
£AddResult = appoint £_summon_Add_4_5

find £AddResult
```

## Appendix A. Instruction Reference

|Instruction|Description|Example|
|-|-|-|
|`welcome <type>`|Variable Declaration|`£LovesickMaidens = welcome peer`|
|`welcome.list <type>`|Array Variable Declaration|`£Numbers = welcome.list peer, 3`|
|`appoint <value>`|Variable Assignment|`£LovesickMaidens = appoint 20`|
|`were <value>, <type>`|Variable Cast|`£LovesickMaidens = were £Lords, chancellor`|
|`prentice <value>`|Push onto Stack|`prentice £LovesickMaidens`|
|`leave`|Pop off Stack|`£LovesickMaidens = leave`|
|`sum <op1>, <op2>`|Integer Addition|`£Lords = sum 10, 20`|
|`diff <op1>, <op2>`|Integer Subtraction|`£Lords = diff 10, 5`|
|`prod <op1>, <op2>`|Integer Multiplication|`£Lords = prod 10, 2`|
|`quot <op1>, <op2>`|Integer Division|`£Lords = quot 10, 5`|
|`rem <op1>, <op2>`|Integer Remainder|`£Lords = rem 10, 5`|
|`max <op1>, <op2>`|Integer Maximum Operand|`£Biggest = max 10, 5`|
|`min <op1>, <op2>`|Integer Minimum Operand|`£Smallest = min 10, 5`|
|`sum.f <op1>, <op2>`|Floating-Point Addition|`£Lords = sum.f 2.5, 7.5`|
|`diff.f <op1>, <op2>`|Floating-Point Subtraction|`£Lords = diff.f 15.0, 7.5`|
|`prod.f <op1>, <op2>`|Floating-Point Multiplication|`£Lords = prod.f 2.25, 4.0`|
|`quot.f <op1>, <op2>`|Floating-Point Division|`£Lords = quot.f 10.0, 5.0`|
|`rem.f <op1>, <op2>`|Floating-Point Remainder|`£Lords = rem.f 10.0, 5.0`|
|`max.f <op1>, <op2>`|Floating-Point Maximum Operand|`£Biggest = max.f 10.5, 5.25`|
|`min.f <op1>, <op2>`|Floating-Point Minimum Operand|`£Smallest = min.f 10.5, 5.25`|
|`sum.g <pointer>, <offset>`|Pointer Addition|`£LordsPointer = sum.g £LordsPointer, 2`|
|`diff.g <pointer>, <offset>`|Pointer Subtraction|`£LordsPointer = diff.g £LordsPointer, 2`|
|`find <value>`|Return a Value|`find £Lords`|
|`chord <op1>, <op2>`|Bitwise AND|`£Result = chord 10, 20`|
|`harmony <op1>, <op2>`|Bitwise OR|`£Result = harmony 10, 20`|
|`discord <op1>, <op2>`|Bitwise XOR|`£Result = discord 10, 20`|
|`inv <op1>`|Bitwise NOT|`£Result = inv 10`|
|`transup <op1>, <op2>`|Left Shift by `<op2>`|`£Result = transup 10, 1`|
|`transdown <op1>, <op2>`|Right Shift by `<op2>`|`£Result = transdown 10, 1`|
|`alike <op1>, <op2>`|Integer Equality Comparison (`<op1> == <op2>`)|`£Equal = alike £NumLords, £NumMaidens`|
|`unlike <op1>, <op2>`|Integer Inequality Comparison (`<op1> != <op2>`)|`£NotEqual = unlike £NumLords, £NumMaidens`|
|`preadam <op1>, <op2>`|Integer Larger Than (`<op1> > <op2>`)|`£MoreLords = preadam £NumLords, £NumMaidens`|
|`lowerdeg <op1>, <op2>`|Integer Less Than (`<op1> < <op2>`)|`£MoreMaidens = lowerdeg £NumLords, £NumMaidens`|
|`alike.f <op1>, <op2>`|Floating-Point Equality Comparison (`<op1> == <op2>`)|`£Equal = alike.f £NumLords, £NumMaidens`|
|`unlike.f <op1>, <op2>`|Floating-Point Inequality Comparison (`<op1> != <op2>`)|`£NotEqual = unlike.f £NumLords, £NumMaidens`|
|`preadam.f <op1>, <op2>`|Floating-Point Larger Than (`<op1> > <op2>`)|`£MoreLords = preadam.f £NumLords, £NumMaidens`|
|`lowerdeg.f <op1>, <op2>`|Floating-Point Less Than (`<op1> < <op2>`)|`£MoreMaidens = lowerdeg.f £NumLords, £NumMaidens`|
|`both <op1>, <op2>`|Logical AND|`£Result = both verity, nay`|
|`either <op1>, <op2>`|Logical OR|`£Result = either verity, nay`|
|`hardly <op1>`|Logical NOT|`£Result = hardly verity`|
|`sail <label>`|Branches to the specified label.|`sail LABEL`|
|`sailalike <value>, <label>`|Branches to the specified label if the `decree` value indicates equality.|`sailalike £IsLord, !LABEL`|
|`sailunlike <value>, <label>`|Branches to the specified label if the `decree` value indicates inequality.|`sailunlike £IsLord, !LABEL`|
|`summon <function>`|Calls a void function or ignores the result.|`summon &Function`|
|`summon.find <function>`|Calls a returning function and collects the result.|`£Result = summon.find &Function`|

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
* The Null `naught` literal can be assigned to a pointer without a pointee, an array or `yarn` string.