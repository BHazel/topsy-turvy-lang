---
sidebar_position: 8
---

# Language Server

The _Operetta Language Server_ implements the [Language Server Protocol](https://microsoft.github.io/language-server-protocol/) (LSP) for the Topsy Turvy language.  The LSP enables code editors and other tools to interact with servers aware of the Topsy Turvy language via JSON-RPC.  The _Operetta CLI_ provides an LSP server with the following command:

```sh
operetta sorcerer incantation
```

The Visual Studio Code _Topsy Turvy_ extension automatically launches the language server.

To understand the language server it can help to think of it as a background process having a continuous conversation with the editor.  The conversation follows the LSP using a message format called JSON-RPC, a simple way of sending requests and responses between two programmes.  The editor sends a request ("what is at this cursor position?") and the server sends back a response ("here is the hover text").

Communication between the editor and server takes two forms:

* **Requests** expect a response: Hover, Completion and Go-to-Definition are all requests where the editor waits for the server to reply before displaying a result.
* **Notifications** are fire-and-forget: the editor sends a notification when a file is opened or edited and does not wait for a reply.  Diagnostic squiggles are a special case: rather than being requested, they are _pushed_ from the server to the editor via a `publishDiagnostics` notification whenever a file changes.

On startup the server advertises its **capabilities**, the set of features it supports.  VS Code reads these on connection and only enables the corresponding UI elements.  If a capability is not advertised, the feature is not offered.

One important convention to note is that LSP uses **0-indexed** line and column numbers (the first character in a file is line 0, column 0), whereas the Topsy Turvy AST uses 1-indexed positions.  The language server converts between the two when reporting positions back to the editor.

The language server is implemented using [OmniSharp](https://github.com/omnisharp).

:::warning
The following known limitation affects the current implementation:

* **Parameter name collisions**: The `SymbolTable` uses a flat namespace shared across all functions.  If two functions declare a parameter with the same name, only the first is recorded; subsequent ones are silently dropped, affecting hover, Go-to-Definition and rename.
:::

## Implementation

The implementation can be found in the `BWHazel.TopsyTurvy.LanguageServer` namespace, backed by types in the `BWHazel.TopsyTurvy.Analysis` namespace.  For more details on the types in the latter namespace please see the [Analysis](./analysis.md) page.

### Handlers

An LSP server implements handlers to respond to requests from editors.  The _Operetta Language Server_ implements numerous handlers as outlined in the table below.  

|VS Code Functionality|Handler Class|Description|
|-|-|-|
|Document Synchronisation|`TextDocumentSyncHandler`|The backbone of the language server.  Triggered whenever a `.topsy` file is opened, edited, saved or closed.  Re-parses the document on each change, updates the document state and publishes diagnostic squiggles to the editor.|
|Hover|`HoverHandler`|Returns a Markdown tooltip when hovering over a symbol: variable, function or parameter.|
|Go to Definition|`DefinitionHandler`|Navigates to the declaration of the symbol under the cursor: `PRAY WELCOME` for variables and parameters, `IT IS MY DUTY TO PERFORM` for functions.  Searches the current document first, then other open documents.|
|IntelliSense / Completion|`CompletionHandler`|Suggests declared symbols and all Topsy Turvy keywords as the user types.  Uses server-side filtering to handle multi-word keywords incrementally as each word is typed.|
|Semantic Syntax Highlighting|`SemanticTokensHandler`|Assigns semantic colours to variables, parameters and functions, enriching the base TextMate grammar colouring with symbol-aware information.|
|Rename Symbol|`RenameHandler`|Renames all occurrences of a symbol across all open documents, skipping occurrences inside strings and comments.  Case-insensitive and whole-word only.|
|Rename Symbol (Prepare)|`PrepareRenameHandler`|Validates that the symbol under the cursor can be renamed before VS Code shows the rename input box.|
|Outline / Go to Symbol in File|`DocumentSymbolHandler`|Populates the VS Code Outline panel with all declared variables and functions from the current document.|
|Find All References|`ReferencesHandler`|Finds all occurrences of a symbol across all open documents.|
|Parameter Hints|`SignatureHelpHandler`|Shows parameter name hints inside a `SUMMON` function call, highlighting the active parameter as the cursor moves through the arguments.|
|Code Folding|`FoldingRangeHandler`|Provides fold regions for all block constructs (`PRINCIPALS`, function bodies, loops, conditionals, switch blocks, try-catch) and multi-line comments.|
|Format Document|`DocumentFormattingHandler`|Normalises all keyword casing to uppercase and applies 2-space libretto indentation to all block constructs.|
|Code Lens|`CodeLensHandler`|Shows inline reference counts above variable and function declarations; clicking a count navigates to Find All References.|
|Go to Symbol in Workspace|`WorkspaceSymbolHandler`|Searches for symbols matching a query string across all open documents.|

### Document State

Before the server can answer any questions about the code, it needs to understand it.  Every time a `.topsy` file is opened or edited, the editor sends the full text of the document to the server.  `TextDocumentSyncHandler` receives this, runs it through the `TryParse()` method on the [Parser](./parser.md) and stores the result in the `DocumentStateManager`.

The `DocumentStateManager` is a central registry: a singleton that holds the current state of every open `.topsy` file.  Each document state contains:

* The raw source text.
* The `SymbolTable` built from the last **successful** parse: a lookup of every declared variable, function and parameter.

A key concept is that the `SymbolTable` is only rebuilt when the parse succeeds.  While mid-edit with a temporary syntax error, the server keeps the last good `SymbolTable`, which means hover tooltips, completions and Go-to-Definition continue to work even while the code is in a broken state.

When a document is closed its state is removed and any squiggles are cleared from the editor.

### Cross-Document Awareness

The language server can see across all `.topsy` files open in the editor at once, not just the file being edited.  `DocumentStateManager` keeps track of every open document and provides methods for searching across them.  Different handlers have different scopes:

* **Current document only**: `FoldingRangeHandler`, `DocumentFormattingHandler`, `PrepareRenameHandler` and `SignatureHelpHandler` only work on the current file.
* **Functions from other documents**: `CompletionHandler` and `SemanticTokensHandler` also pull in function symbols from other open documents, so that functions defined in imported files appear in completions and are highlighted correctly.
* **All symbols across all documents**: `ReferencesHandler`, `RenameHandler` and `CodeLensHandler` search across every open document.  `WorkspaceSymbolHandler` queries all documents by name.
* **Go to Definition**: `DefinitionHandler` tries the current document first; if the symbol is not found it searches other open documents and, when found, returns the URI of that file so VS Code can open it.
