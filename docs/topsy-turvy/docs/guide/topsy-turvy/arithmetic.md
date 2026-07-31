# Arithmetic

Topsy Turvy provides numerous operators for performing arithmetic operations, outlined in the table below.

|Operator|Description|Operation|
|-|-|-|
|`SUM OF x AND y`|Addition|`x + y`|
|`DIFFERENCE OF x AND y`|Subtraction|`x - y`|
|`PRODUCT OF x AND y`|Multiplication|`x * y`|
|`QUOTIENT OF x AND y`|Division|`x / y`|
|`REMAINDER OF x AND y`|Remainder / Modulo|`x % y`|
|`LARGER OF x AND y`|Larger Number|`max(x, y)`|
|`SMALLER OF x AND y`|Smaller Number|`min(x, y)`|

The `x` and `y` operands can be literals, variables or other expressions which return a numeric type.

They operate on all supported numeric types and perform widening when the two operands are of different compatible types.

The following code demonstrates a couple of examples.

```
PRAY WELCOME SumResult AS A PEER
SumResult IS APPOINTED SUM OF 4 AND 5

PRAY WELCOME ProductResult AS A CHANCELLOR BEING PRODUCT OF 10 AND 20
```

In the second example the two literal values are widened to be of type `CHANCELLOR` to match the type of `ProductResult`.

:::info
Please see the [Types & Variables](./types-vars.md) page for more information about widening.
:::

Arithmetic operators can also be combined to create complex mathematical expressions, as the following example shows.

```
ASIDE: Calculate ((72 + 560) x 2) / 4

PRAY WELCOME Result AS A PEER BEING ~
  QUOTIENT OF ~
    PRODUCT OF ~
      SUM OF 72 AND 560 ~
    AND 2
  AND 4
```