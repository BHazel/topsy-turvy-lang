---
sidebar_position: 7
---

# Analysis

Code analysis enables editors to provide rich experiences when working with Topsy Turvy code, such as syntax highlighting, code navigation, refactoring amongst others.

The _Operetta Language Server_ and Web Editor make use of the Analysis layer to provide these editing features.

## Implementation

In the _Operetta Toolchain_ the Analysis layer is implemented in the `BWHazel.TopsyTurvy.Analysis` namespace and include the following key types:

* **`SymbolTable`:** All declared names built from the AST on each successful parse.
* **`HoverMarkdownBuilder`:** Formats symbol information as Markdown for hover tooltips.
* **`SourceFormatter`:** Formats code according to language standards.
* **`TopsyTurvyCodeGenerator`:** Converts an AST back into valid Topsy Turvy source code.
* **`SourceAnalyser`:** Finds occurrences of a symbol by scanning source text.
    * This is used as a workaround for missing AST position information as outlined in the Known Limitations section on the [Language Server](./language-server.md) page.
* **`KeywordData`:** The authoritative list of all Topsy Turvy keywords, used for completions and formatting.
* **`DocumentationCommentParser`:** Parses documentation comments associated with variables and functions for display in editors.

### Symbol Table

:::warning
There is a known limitation regarding the flat symbol table namespace and source-text scanning for positions.  Please see the [Language Server](./language-server.md) page for full details.
:::

The `SymbolTable` is a registry of every named entity in a Topsy Turvy programme: variables, functions and function parameters.  It is rebuilt from scratch after every successful parse so that editors always have an up-to-date picture of the declared symbols in a programme.

Each entry in the table is a `SymbolInfo` object, which records the following properties:

|Property|Type|Description|
|-|-|-|
|`Name`|`string`|The declared name, e.g. `TotalLords`.|
|`Kind`|`SymbolKind`|Whether the symbol is a variable, function or parameter (please see below).|
|`TypeDisplayName`|`string?`|The Topsy Turvy type keyword for variables, for example `PEER` or `FATHOM`.|
|`TypedParameters`|`IReadOnlyList<TypedParameter>?`|The typed parameters for function symbols.  Each `TypedParameter` record carries the parameter `Name`, declared `LiteralType` and source `Span`.|
|`DefinitionLine`|`int`|The 1-indexed source line where the symbol is declared.  `0` means the position could not be determined.|
|`DefinitionColumn`|`int`|The 1-indexed source column where the symbol name begins.  `0` means the position could not be determined.|
|`Documentation`|`DocumentationComment?`|The documentation associated with the symbol.|

The `SymbolKind` enum classifies what type of named entity a symbol represents:

|Value|Description|
|-|-|
|`Variable`|A variable declared with `PRAY WELCOME`.|
|`Function`|A function declared with `IT IS MY DUTY TO PERFORM`.|
|`Parameter`|A function parameter named in `UNDER THE TERMS OF`.|

#### Constructing the Symbol Table

The `SymbolTable` is constructed by calling `SymbolTable.Build(program, originalSource)`, which takes the root `ProgramNode` from the [AST](./ast.md) and the original, unprocessed source text.

The build proceeds in two steps:

1. **Walk the AST**: The builder traverses every statement recursively, collecting `DeclarationNode` instances (variables), `FunctionDefinitionNode` instances (functions and their parameters) and descending into nested blocks such as conditionals, loops and switch statements.
2. **Scan the source for positions**: Because AST span tracking is not yet wired into the parser (please see the Source Positions section on the [AST](./ast.md#source-positions) page), the builder recovers definition positions by searching the raw source text line by line for the declaration keyword followed by the symbol name.  For variables it searches for `PRAY WELCOME` ... `Name` and for functions it searches for `IT IS MY DUTY TO PERFORM` ... `Name`.

#### Example

Given the following programme:

```
HARK! "The Lords"

PRAY WELCOME AllLords AS A PEER
IT IS MY DUTY TO PERFORM TotalLords UNDER THE TERMS OF Conservatives AS A PEER AND Liberals AS A PEER TO FIND PEER
    AND SO I FIND SUM OF Conservatives AND Liberals
MY DUTY IS DISCHARGED.

FINALE.
```

the symbol table would contain four entries:

|Name|Kind|TypeDisplayName|TypedParameters|DefinitionLine|DefinitionColumn|
|-|-|-|-|-|-|
|`AllLords`|`Variable`|`PEER`|_(none)_|3|14|
|`TotalLords`|`Function`|_(none)_|`Conservatives AS A PEER`, `Liberals AS A PEER`|4|26|
|`Conservatives`|`Parameter`|`PEER`|_(none)_|4|26|
|`Liberals`|`Parameter`|`PEER`|_(none)_|4|26|

Note that `Conservatives` and `Liberals` both report the same position as `TotalLords`.  Parameters are assigned the position of the function name itself rather than their own position within the `UNDER THE TERMS OF` clause, as the position scan uses the function declaration keyword to find the function, and parameters reuse that result.

### Source Formatter

The `SourceFormatter` formats a Topsy Turvy source text according to two language standards, applied together in a single pass:

* **Keyword Normalisation**: All language keywords are converted to their canonical case regardless of how they were typed.  For example, `pray welcome` or `PRAY Welcome` are both normalised to `PRAY WELCOME`.
* **Libretto Indentation**: Lines are indented by 2 spaces per nesting level.  The style mirrors a musical libretto: block-opening lines (such as `PRINCIPALS`, `IT IS MY DUTY TO PERFORM`, `SHOULD IT TRANSPIRE THAT`) increase the indent for the lines that follow, and block-closing lines (such as `THE CURTAIN RISES.`, `MY DUTY IS DISCHARGED.`, `SO MUCH FOR THAT.`) decrease it before they are written.  The `or,` subtitle is always indented by exactly 2 spaces regardless of depth.

Block comments and blank lines are preserved verbatim and are not indented or normalised.

As an example, the following unformatted source:

```
hark! "demo"
or, "An Example"
principals
pray welcome Count as a peer
THE CURTAIN RISES.
should it transpire that PRE-ADAMITE Count AND 10
Quite so.
BEHOLD "Count is large"
so much for that.
FINALE.
```

would be formatted as:

```
HARK! "demo"
  or, "An Example"
PRINCIPALS
  PRAY WELCOME Count AS A PEER
THE CURTAIN RISES.
SHOULD IT TRANSPIRE THAT PRE-ADAMITE Count AND 10
  QUITE SO.
  BEHOLD "Count is large"
SO MUCH FOR THAT.
FINALE.
```

Keyword normalisation is performed using compiled regular expressions built once at startup from the `CanonicalKeywords` list.  Keywords are matched longest-first so that shorter keywords that are prefixes of longer ones (for example `WITH` vs `WITH THE GREATEST RESPECT,`) are not matched in place of the longer form.  String literals and line comments are identified as **skip ranges** and excluded from normalisation so that keyword-like text inside strings is left untouched.

The `DepthAction` enum records what depth adjustment each line requires:

|Value|When Applied|Effect|
|-|-|-|
|`None`|All other lines|No change.|
|`PostIncrease1`|Block-opening lines such as `PRINCIPALS`|Indent increases by 1 after the line is written.|
|`PostIncrease2`|`IN WHICH CAPACITY?` (switch)|Indent increases by 2 after the line is written.|
|`PreDecrease1`|Block-closing lines such as `MY DUTY IS DISCHARGED.`|Indent decreases by 1 before the line is written.|
|`PreDecrease2`|`SO MUCH FOR THAT.` / `NOTHING COULD BE MORE SATISFACTORY.`|Indent decreases by 2 before the line is written.|
|`MidBlock`|Mid-block transitions such as `OR, IF NOT,` / `OTHERWISE,`|Indent decreases by 1, the line is written, then increases by 1.|

### Code Generator

The `TopsyTurvyCodeGenerator` converts an AST back into valid Topsy Turvy source code, effectively a reverse of the [Parser](./parser.md).  It provides a single `Generate()` method which takes a complete `ProgramNode` node and walks the complete AST, node by node, emitting source code formatted according to the source formatter regardless of how it was originally written: the AST does not store original code formatting or style.  The generated source code will parse back to a structurally identical AST.

As an example, given the following AST, as constructed by the parser from any source that expresses the same programme:

```csharp
new ProgramNode()
{
    Title = "Demo Programme",
    Statements =
    [
        new DeclarationNode()
        {
            Name = "Count",
            Type = LiteralType.Integer,
            InitialValue = new LiteralNode() { Type = LiteralType.Integer, Value = 0 },
        },
        new PrintNode()
        {
            Expression = new IdentifierNode { Name = "Count" },
        },
    ],
}
```

the code generator produces:

```
HARK! "Demo Programme"

PRAY WELCOME Count AS A PEER BEING 0
BEHOLD Count
FINALE.
```

### Source Analyser

The `SourceAnalyser` provides stateless source-text scanning utilities shared across the Language Server and Web Editor.  Its primary purpose is to locate occurrences of a symbol by name across the source text when AST position information is unavailable.

The key concept is **skip ranges**: regions of the source that must be ignored when scanning for a symbol name.  Keywords and identifiers inside string literals and comments must not be counted as references.  The `FindSkipRanges(source)` method builds a list of absolute character offset ranges covering:

1. Block comments (`(ASIDE, AT SOME LENGTH:` ... `END OF ASIDE.)`)
2. String literals (`"..."`)
3. Line comments (`ASIDE: ...`)

in that priority order, so that a block comment containing a string literal is treated as a block comment rather than a string.

* `FindWordOccurrences(sourceLines, word)` uses these skip ranges to find every whole-word, case-insensitive match of a symbol name, returning `(Line, Character)` pairs in document order.  Whole-word matching checks that the character immediately before and after the match is not a valid identifier character (letters, digits, `-` or `_`).
* `ExtractWordAt(source, line, column)` (on `SymbolTable`) performs the reverse: given a 0-indexed cursor position from the editor, it returns the identifier word the cursor is at by walking left and right from the position to find the full word boundaries.

### Keyword Data

`KeywordData` is the authoritative source of truth for all Topsy Turvy language keywords.  It is a static list of `(Keyword, Detail)` pairs, where the keyword is in canonical case and the detail is a short human-readable description.

This list is consumed by the `CompletionHandler` in the Language Server to offer keyword completions as the user types and by `SourceFormatter` as the `CanonicalKeywords` array for normalisation.  Keeping all keyword definitions in one place ensures both consumers stay in sync.

### Documentation Comments

Variables and functions in Topsy Turvy can have in-line documentation applied above their declarations.  Documentation comments are based on the standard `(ASIDE, AT SOME LENGTH:` ... `END OF ASIDE.)` block comments, using tags within the comments to provide documentation, such as a summary, parameters, etc..  The comments are parsed by the `DocumentationCommentParser` and added as a `DocumentationComment` to the symbol in the symbol table.  While building the symbol table, the source is scanned from each declaration to find an immediately preceding block comment which is then passed to `DocumentationCommentParser` to extract content from any recognised tags.  Any non-block comment code between the declaration and block comment breaks the association and the documentation will just be treated as a regular block comment.

### Hover Markdown Builder

`HoverMarkdownBuilder` produces the Markdown string displayed in a hover tooltip when the cursor rests over a symbol.  It has a single entry point, `Build(symbolInfo)`, which switches on the symbol kind.  The output is passed directly to the Language Server `HoverHandler`, which wraps it in an LSP `MarkupContent` response for the editor to display.

Function signatures are rendered with their full typed parameter list drawn from `TypedParameters`, so tooltips reflects the declared types of each parameter rather than bare names.  When present on a symbol, any documentation is appended to the Markdown string for display in the hover tooltip.
