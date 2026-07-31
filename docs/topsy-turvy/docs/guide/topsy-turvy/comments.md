# Comments

Topsy Turvy supports comments and can be considered stage directions for your code!  Both single-line and multi-line block comments are supported.  Both types are centered around the `ASIDE:` keyword.  When code is run, comments are ignored.

## Single-Line Comments

A single-line comment takes the form:

```
ASIDE: Comment text.
```

All text following the `ASIDE:` keyword is ignored up to the end of the line.

## Multi-Line Block Comments

A multi-line comment takes the form:

```
(ASIDE, AT SOME LENGTH:
    Comment text.
    It can span multiple lines.
END OF ASIDE.)
```

:::info
Multi-line block comments are the basis for providing in-line documentation for variables and functions.  Please see the [Code Documentation](./code-documentation.md) page for more info.
:::