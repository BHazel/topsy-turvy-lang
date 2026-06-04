# Topsy Turvy

Topsy Turvy is an esoteric but fully functional programming language themed around the weird and wonderful world of the comic operas of Gilbert & Sullivan!  This extension provides rich language support for it.

## Features

Rich language support for Topsy Turvy `.topsy` files including:

* File icons.
* Syntax highlighting.
* Completion (IntelliSense) for keywords, variables and functions.
* Hover details for variables and functions.
* Go-to-Definition and CodeLens for variables and functions.
* Code formatting.

## Requirements

You need Visual Studio Code to be installed as it relies on an included LSP server to provide a majority of the rich language support.

## Extension Settings

This extension contributes the following settings:

* `topsy-turvy.cliPath`: Absolute path to the Topsy Turvy CLI executable (`operetta`).  Leave empty to use the binary bundled with the extension.

## Known Issues

**This extension should be considered beta software and is early in development.**

* CodeLens appears to require all files to be opened at least once for reference counts to work.
* Indentation may have some inconsistencies.

## Release Notes

### 0.1.0

Initial release of the extension.
