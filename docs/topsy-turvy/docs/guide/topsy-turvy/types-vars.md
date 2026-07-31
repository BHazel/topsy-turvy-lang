# Types & Variables

## Primitive Types

Topsy Turvy supports numerous primitive types of differnet sizes, outlined in the table below with the correspdonding Topsy Turvy keywords to represent them.

|Keyword|Type|Default|G&S Inspriation|
|-|-|-|-|
|`CHANCELLOR`|64-bit Integer|0|_Iolanthe_|
|`PEER`|32-bit Integer|0|_Iolanthe_|
|`PIRATE`|16-bit Integer|0|_The Pirates of Penzance_|
|`SAUSAGE-ROLL`|8-bit Integer (byte)|0|_The Grand Duke_|
|`FATHOM`|64-bit Floating-Point|0.0|_HMS Pinafore_|
|`FOOT`|32-bit Floating-Point|0.0|_HMS Pinafore_|
|`DECREE`|Boolean|`NAY` (false)|_The Mikado_|
|`YARN`|String|"" (empty string)|_The Mikado_|
|`STITCH`|Character|'\0' (null character)|_The Mikado_|
|`LITTLE LIST OF <type>`|Array of Specified Type|[]|_The Mikado_|

All the integer types support a `STANDING` prefix to the type to make it **unsigned**.

:::info
Please see the [Arrays](./arrays.md) page for more information about arrays and array variables.
:::

### Numeric Widening

Ceratin operations, such as when using arithmetic operators, can combine different numeric types.  In these situations, the widest numeric type is used for both.  For example if an operation accepts a `PEER` (32-bit integer) and a `CHANCELLOR` (64-bit integer) then the `PEER` is cast to a `CHANCELLOR` and the result will also be `CHANCELLOR`.

Likewise, when combining integers and floating-point numbers, the operation will be performed with both values and the result as floating-point numbers.  For example, where a `PEER` and `FATHOM` are in an operation, the `PEER` will be cast to a `FATHOM` and the result will be a `FATHOM`.

## Variables

To declare, or **welcome**, and assign a variable of any of the specified types you use the `PRAY WELCOME` statement in the following format:

```
PRAY WELCOME <variable> AS A <type> BEING <value>
```

The `BEING <value>` can take a literal, another variable or any type-compatible expression.  It is also optional: if omitted the variable is assigned it default value for its type, as outlined in the table above.

For example, the following declares a 32-bit integer and assigns it the literal value `20`:

```
PRAY WELCOME LovesickMaidens AS A PEER BEING 20
```

As another example, the following declares a 64-bit floating-point number and assigns it the result of a multiplication of a literal and another variable:

```
PRAY WELCOME TotalTea AS A FATHOM BEING PRODUCT OF TeaJorumVolume AND 20
```

:::info
Please see the [Arithmetic](./arithmetic.md) page for more information about arithmetic operators.
:::

## Constants

A variable can be declared as a constant by prepending the type with the `CONSERVATIVE` keyword.  Once set as a constant the value cannot be changed and doing so will result in an error.

As an example, the following declares a constant string and assigns its value:

```
PRAY WELCOME PoemSubject AS A CONSERVATIVE YARN BEING "Hollow"
```

:::tip
Although not required, variables can also be explicitly declared variable using the `LIBERAL` keyword as, in a way, every variable declared _into the world alive is either a little **Liberal** or else a little **Conservative**_!
:::

## Assigning Variables

Once declared a variable can be assigned using the `IS APPOINTED` keyword using a literal, variable or expression that **must match** the declared type of the variable.  An type-incompatible assignment is an error.

As an example the following assigns a 64-bit floating-point number to a variable of that type, both using a literal and an arithmetic operation:

```
PRAY WELCOME ParadoxBirthYears AS A FATHOM

ParadoxBirthYears IS APPOINTED 5.25
ParadoxBirthYears IS APPOINTED QUOTIENT OF 21 AND 4
```