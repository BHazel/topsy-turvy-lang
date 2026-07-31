# Logical & Bitwise Operators

Topsy Turvy provides numerous operators for performing logical and comparison operations, including bitwise.

## Logical Operations

Logical operations compare Boolean `DECREE` values and expressions to return a `DECREE` as the result.  Supported operators are outlined in the table below:

|Operator|Description|Operation|
|-|-|-|
|`BOTH x AND y`|Logical AND|`x AND y`|
|`EITHER x OR y`|Logical OR|`x OR y`|
|`HARDLY EVER x`|Logical NOT|`!x`|

Two additional variadic operators are supported, which can take any number of `DECREE` values, which are outlined below:

|Operator|Description|Operation|
|-|-|-|
|`ALL OF x AND y AND ... IF YOU PLEASE.`|Variadic AND|`x AND y AND ...`|
|`ANY OF x AND y AND ... IF YOU PLEASE.`|Variadic OR|`x OR y OR ...`|

As with other operators they can be combined with others to create complex expressions.