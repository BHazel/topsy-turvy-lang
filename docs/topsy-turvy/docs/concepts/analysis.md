---
sidebar_position: 8
---

# Analysis

Code analysis enables editors to provide rich experiences when working with Topsy Turvy code, such as syntax highlighting, code navigation, refactoring amongst others.

The _Operetta Language Server_, Web Editor and Operetta Theatre iOS app make use of the Analysis layer to provide these editing features.

## Implementation

In the _Operetta Toolchain_ the Analysis layer is implemented in the `BWHazel.TopsyTurvy.Analysis` namespace and include the following key types:

* **`SymbolTable`:** All declared names built from the AST on each successful parse.
* **`HoverMarkdownBuilder`:** Formats symbol information as Markdown for hover tooltips.
* **`SourceFormatter`:** Formats code according to language standards.
* **`TopsyTurvyCodeGenerator`:** Converts an AST back into valid Topsy Turvy source code.
* **`SourceAnalyser`:** Finds occurrences of a symbol by scanning source text.
    * This is used as a workaround for missing AST position information as outlined in the Known Limitations section on the [Language Server](./language-server.md) page.
* **`SourceTokeniser`:** Scans source text into categorised token spans for editor syntax highlighting.
* **`KeywordData`:** The authoritative list of all Topsy Turvy keywords, used for completions and formatting.
* **`DocumentationCommentParser`:** Parses documentation comments associated with variables and functions for display in editors.

### Symbol Table

:::warning
There is a known limitation regarding the flat symbol table namespace.  Please see the [Language Server](./language-server.md) page for full details.
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
|`Namespace`|A namespace declared with `TOWN`.|

#### Constructing the Symbol Table

The `SymbolTable` is constructed by calling `SymbolTable.Build(program, originalSource)`, which takes the root `ProgramNode` from the [AST](./ast.md) and the original, unprocessed source text.

Standard Library and admitted external functions are added separately, after `Build`, by calling `SymbolTable.AddExternalFunction(name, parameters, returnType, returnArrayElementType, documentation)` once per function.  This call returns `false`, without overwriting the existing entry, if a symbol of that name already exists and was not itself added by an earlier call to `AddExternalFunction`: a Topsy Turvy declaration always shadows an external function of the same bare name.  This population step is performed by a per-host `ExternalFunctionRegistrar` class, one each in the Language Server, Web Editor and Embedded components rather than in `Analysis` itself, since each host loads its own `BindingCatalogue` differently.  Please see [Function Binding](./function-binding.md) for more details of how Standard Library and external functions become available to `SUMMON` calls.

The builder walks the AST recursively, collecting `DeclarationNode` instances (variables), `FunctionDefinitionNode` instances (functions and their parameters) and descending into nested blocks such as conditionals, loops and switch statements.  For declarations and functions, definition positions are taken from the node `NameSpan`, not `Span`: `Span` covers the whole statement starting at its opening keyword, while `NameSpan` covers only the declared identifier (please see the Source Positions section on the [AST](./ast.md#source-positions) page).  Parameters carry their own `Span` independently of the function they belong to, so each reports its own position within the `UNDER THE TERMS OF` clause rather than reusing the position of the function.

A file `NamespaceDeclarationNode`, if present, is also collected as a `Namespace` symbol.  Its entry `Name` is not a literal source identifier but the node path segments joined with `*`, for example `Accounts*Payroll`, the same short-hand form accepted in source, and the canonical form the code generator emits regardless of whether the source used `WITH DISTRICT` or `*` to write it.

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
|`Conservatives`|`Parameter`|`PEER`|_(none)_|4|56|
|`Liberals`|`Parameter`|`PEER`|_(none)_|4|84|

Note that `Conservatives` and `Liberals` each report their own position within the `UNDER THE TERMS OF` clause, rather than the position of `TotalLords` itself.

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

### Source Tokeniser

:::warning
The Source Tokeniser is a third, independent syntax-highlighting mechanism intended for when using the Language Server is not available, such as in the native, embedded toolchain.  In other situations the Language Server should be the preferred method for syntax highlighting.  Please see the [Language Server](./language-server.md) page for more information.
:::

The `SourceTokeniser` scans Topsy Turvy source text into a flat, document-ordered list of categorised token spans for editors to apply syntax highlighting.  Unlike `SymbolTable`, this is a lexical scan, not a parse: it does not consult the AST or the symbol table.  It emits a best-efforts category for every recognisable span even when the surrounding source does not currently form valid syntax, for example while the user is still typing, and never throws.

Scanning is performed by the single entry point, `Tokenise(source)`, which returns a list of `SourceToken` records, each carrying a category and a source `Span`.  A token can have one of the following categories:

|Category|Description|
|-|-|
|`comment`|A line or block comment.|
|`string`|A single- or double-quoted character or string literal, honouring the `~`-escape character.|
|`number`|An integer or floating-point numeric literal.|
|`variable`|The special `THE PROPS` name.|
|`keyword`|A recognised Topsy Turvy keyword.|
|`type`|A keyword naming a type, for example `PEER` or `LITTLE LIST OF`.|
|`keywordOther`|A keyword relating to other keywords, for example the `STANDING` modifier, categorised separately from other keywords.|
|`identifier`|A user-declared name.|

Keyword phrases are matched by longest-match against `KeywordData.Keywords` at every candidate word-start position, trying each keyword in descending length order rather than re-splitting each phrase into individual words.  This resolves prefix collisions, for example correctly matching the whole of `MY DUTY IS DISCHARGED.` rather than stopping early at a shorter phrase that happens to be a prefix of a longer one.  As with `SourceFormatter`, matching is case-insensitive and requires a non-identifier character, or end-of-source, immediately after the final word so a keyword-like sequence embedded inside a longer identifier is not mistakenly matched as a keyword.

Token spans are reported using the same 1-indexed, half-open `Line`/`Column` convention as `DiagnosticInfo`, not the 0-indexed convention used by the hover and completion exports.

### Keyword Data

`KeywordData` is the authoritative source of truth for all Topsy Turvy language keywords.  It is a static list of `(Keyword, Detail)` pairs, where the keyword is in canonical case and the detail is a short human-readable description.

This list is consumed by the `CompletionHandler` in the Language Server to offer keyword completions as the user types and by `SourceFormatter` as the `CanonicalKeywords` array for normalisation.  Keeping all keyword definitions in one place ensures both consumers stay in sync.

### Documentation Comments

Variables and functions in Topsy Turvy can have in-line documentation applied above their declarations.  Documentation comments are based on the standard `(ASIDE, AT SOME LENGTH:` ... `END OF ASIDE.)` block comments, using tags within the comments to provide documentation, such as a summary, parameters, etc..  The comments are parsed by the `DocumentationCommentParser` and added as a `DocumentationComment` to the symbol in the symbol table.  While building the symbol table, the source is scanned from each declaration to find an immediately preceding block comment which is then passed to `DocumentationCommentParser` to extract content from any recognised tags.  Any non-block comment code between the declaration and block comment breaks the association and the documentation will just be treated as a regular block comment.

### Hover Markdown Builder

`HoverMarkdownBuilder` produces the Markdown string displayed in a hover tooltip when the cursor rests over a symbol.  It has a single entry point, `Build(symbolInfo)`, which switches on the symbol kind.  The output is passed directly to the Language Server `HoverHandler`, which wraps it in an LSP `MarkupContent` response for the editor to display.

Function signatures are rendered with their full typed parameter list drawn from `TypedParameters`, so tooltips reflects the declared types of each parameter rather than bare names.  When present on a symbol, any documentation is appended to the Markdown string for display in the hover tooltip.

Namespace hovers use a separate entry point, `BuildNamespaceHover(namespacePath, functions)`, rather than `Build(symbolInfo)`, since the tooltip content, the namespace path plus a list of the functions declared under it, is gathered from across every open document sharing that namespace, not from a single `SymbolInfo`.  The Language Server `HoverHandler` calls this instead of `Build` whenever the symbol under the cursor is a `Namespace`, or the cursor is over a bare namespace segment that has no `SymbolInfo` of its own.
