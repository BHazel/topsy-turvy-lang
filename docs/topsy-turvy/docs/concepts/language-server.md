---
sidebar_position: 9
---

# Language Server

The _Operetta Language Server_ implements the [Language Server Protocol](https://microsoft.github.io/language-server-protocol/) (LSP) for the Topsy Turvy language.  The LSP enables code editors and other tools to interact with servers aware of the Topsy Turvy language via JSON-RPC.  The _Operetta CLI_ provides an LSP server with the following command:

```sh
operetta incantation
```

Standard Library functions are always available to `SUMMON` and an external library assembly can be made available for the whole server session by passing one or many `--admit <path.dll>` flags (alias `--include`) on the same command.  There is no way to admit a library after the server has started.  Please see [Function Binding](./function-binding.md) for more details.

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
|Hover|`HoverHandler`|Returns a Markdown tooltip when hovering over a symbol: variable, function, parameter or namespace.  A namespace tooltip lists the functions declared under it, gathered from across all open documents that declare it.  This also covers Standard Library and admitted external functions, since they are added to the `SymbolTable` alongside declared symbols; please see [Analysis](./analysis.md#constructing-the-symbol-table) and [Function Binding](./function-binding.md).|
|Go to Definition|`DefinitionHandler`|Navigates to the declaration of the symbol under the cursor: `PRAY WELCOME` for variables and parameters, `IT IS MY DUTY TO PERFORM` for functions.  Searches the current document first, then other open documents.|
|IntelliSense / Completion|`CompletionHandler`|Suggests declared symbols and all Topsy Turvy keywords as the user types.  Uses server-side filtering to handle multi-word keywords incrementally as each word is typed.  Also offers known namespace path segments after `TOWN`, `PRAY RECOGNISE` or a `SUMMON` target and scopes function-name suggestions to the exact namespace once a fully-qualified `WITH DUTY` segment is being typed.|
|Semantic Syntax Highlighting|`SemanticTokensHandler`|Assigns semantic colours to variables, parameters and functions, enriching the base TextMate grammar colouring with symbol-aware information.|
|Rename Symbol|`RenameHandler`|Renames all occurrences of a symbol across all open documents, skipping occurrences inside strings and comments.  Case-insensitive and whole-word only.|
|Rename Symbol (Prepare)|`PrepareRenameHandler`|Validates that the symbol under the cursor can be renamed before VS Code shows the rename input box.|
|Outline / Go to Symbol in File|`DocumentSymbolHandler`|Populates the VS Code Outline panel with all declared variables and functions from the current document.  A file with a namespace appears in the Outline too, and its functions are nested under it in the panel and editor breadcrumb bar.|
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
* The document namespace path, if declared, populated alongside the `SymbolTable`.  Empty for a document that declares no namespace.

A key concept is that the `SymbolTable` is only rebuilt when the parse succeeds.  While mid-edit with a temporary syntax error, the server keeps the last good `SymbolTable`, which means hover tooltips, completions and Go-to-Definition continue to work even while the code is in a broken state.

The `BindingCatalogue` admitted at startup, covering the Standard Library and any `--admit` libraries, is fixed for the lifetime of the server: it is registered once and reused for every document, so there is no per-document or in-session way to change it.  Since a bound function has no real source position, its symbol entry uses placeholder positions, so Go-to-Definition on a Standard Library or admitted function name does not navigate anywhere useful.

When a document is closed its state is removed and any squiggles are cleared from the editor.

### Cross-Document Awareness

The language server can see across all `.topsy` files open in the editor at once, not just the file being edited.  `DocumentStateManager` keeps track of every open document and provides methods for searching across them.  Different handlers have different scopes:

* **Current document only**: `FoldingRangeHandler`, `DocumentFormattingHandler`, `PrepareRenameHandler` and `SignatureHelpHandler` only work on the current file.
* **Functions from other documents**: `CompletionHandler` and `SemanticTokensHandler` also pull in function symbols from other open documents, so that functions defined in imported files appear in completions and are highlighted correctly.
* **Namespaces spread across documents**: Since a namespace is not confined to a single file, `DocumentStateManager` can also list every distinct namespace path declared across all open documents and the functions declared under a given path.  `CompletionHandler` and `HoverHandler` use this to offer and describe namespaces regardless of which file declares them.
* **All symbols across all documents**: `RenameHandler` searches across every open document.  `WorkspaceSymbolHandler` queries all documents by name.
* **Import-connected documents only**: `ReferencesHandler` and `CodeLensHandler` scope their cross-file search to documents connected to the current one by a `PRAY ADMIT` import, in either direction, via `DocumentStateManager.GetImportConnectedDocuments`, rather than every open document.  An occurrence in an unrelated open file with no import relationship is not reported.
* **Go to Definition**: `DefinitionHandler` tries the current document first; if the symbol is not found it searches other open documents and, when found, returns the URI of that file so VS Code can open it.
