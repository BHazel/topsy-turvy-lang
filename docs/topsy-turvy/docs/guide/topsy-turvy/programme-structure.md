# File Structure

Every Topsy Turvy file follows the same structure, outlined and explained below.

```topsy
HARK! "Title"
  or, "Subtitle"

ASIDE: Code goes here...

FINALE.
```

* Every file starts with the `HARK! <title>` statement to give the file a title.  This should be brief but give the gist of what the code in the file does.
* The _optional_ `or, <subtitle>` clause provides space for more detail if needed.

:::tip
See the `HARK!` and `or,` clauses like the naming of the G&S comic operas themselves!  For example, **The Mikao or, The Town of Titipu** would be written in Topsy Turvy as:

```topsy
HARK! "Mikado"
  or, "The Town of Titipu"
```
:::

* The `ASIDE:` and all text following is a single-line comment marking where all code must sit in this file.

:::info
Please see the [Comments](./comments.md) page for more information about comments.
:::

* Finally, as with all G&S comic operas, the file ends with the `FINALE.`!

As indicated above, all code sits inside these file structure clauses.

## Principals Block

The `PRINCIPALS` block is an optional block of code located just beneath the title and subtitle for declaring global variables, although variables can be declared at any location in the programme.  It is closed with the `THE CURATIN RISES.` keyword.  See this as programme notes on who the main characters are, ending with the start of the opera!

```topsy
HARK! "Title"
  or, "Subtitle"

PRINCIPALS
  ASIDE: Global variables declared and optionally assigned here...
THE CURATAIN RISES.

ASIDE: Code goes here...

FINALE.
```

:::info
Please see the [Types & Variables](./types-vars.md) page for more information about variables.
:::

## Line Breaks

When lines in source code get very long they can be split using the _Victorian Flourish_ `~` character at the end of the line.  Anything on the following line is treated as a continuation of the previous.

The following code:

```topsy
PRAY WELCOME MyVariable ~
AS A PEER
```

would be treated the same as:

```topsy
PRAY WELCOME MyVariable AS A PEER
```

:::info
This is the syntax for declaring a variable.  Please see the [Types & Variables](./types-vars.md) page for more information.
:::