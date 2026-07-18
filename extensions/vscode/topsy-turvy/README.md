# Topsy Turvy

Topsy Turvy is an esoteric but fully functional programming language themed around the weird and wonderful world of the comic operas of Gilbert & Sullivan!  This extension provides rich language support for it and its intermediate representation, UtopIR.

## Features

Rich language support for Topsy Turvy `.topsy` files including:

* File icons.
* Syntax highlighting.
* Completion (IntelliSense) for keywords, variables and functions.
* Hover details for variables and functions.
* Go-to-Definition and CodeLens for variables and functions.
* Code formatting.
* Palette and toolbar commands for toolchain actions.
* Snippets of common code constructs.

Basic language support for UtopIR `.utopir` files including:

* File icons.
* Syntax highlighting.
* Palette and toolbar commands for assembly emit actions.

## Requirements

You need Visual Studio Code to be installed as it relies on an included LSP server to provide a majority of the rich language support.

## Extension Settings

This extension contributes the following settings:

* `topsy-turvy.cliPath`: Absolute path to the Topsy Turvy CLI executable (`operetta`).  Leave empty to use the binary bundled with the extension.
* `topsy-turvy.commandLineArguments`: Preset command-line arguments to pass into the interpreter when running a Topsy Turvy programme.
* `topsy-turvy.performTiptoeMode`: A checkbox on whether to run a Topsy Turvy programme in Tiptoe mode.
* `topsy-turvy.rehearseTiptoeMode`: A checkbox on whether to check a Topy Turvy programme in Tiptoe mode.
* `topsy-turvy.cadenzaTiptoeMode`: A checkbox on whether to run the REPL in Tiptoe mode.
* `topsy-turvy.playbillRenderMarkdown`: A checkbox on whether to render generated Markdown documentation.
* `topsy-turvy.commissionSkipSubtitle`: A checkbox on whether to skip prompting for a programme subtitle when adding a new Topsy Turvy file.
* `topsy-turvy.mountSkipSubtitle`: A checkbox on whether to skip prompting for a programme subtitle when creating a new Topsy Turvy project.
* `topsy-turvy.enableUtopirPreviewCommands`: A checkbox on whether to enable previre UtopIR features.

## Known Issues

**This extension should be considered beta software and is early in development.**

* CodeLens appears to require all files to be opened at least once for reference counts to work.
* Indentation may have some inconsistencies.

## Release Notes

### 0.7.0

* Support for the latest language features.

### 0.6.0

* Basic syntax highlighting and file icons for UtopIR.

### 0.5.0

* Support for the latest language features.
* A new setting to enable the preview features of UtopIR.

### 0.4.0

* Support for the latest language features.

### 0.3.0

* Support for all new language features.
* Commands to wrap the CLI across the Command Palette, Editor and Context Menus:
    * File Actions: Perform, Rehearse, Cue Processor, Generate Playbill Documentation, View AST Promptbook.
    * Creation Actions: Mount Project, Commission File.
    * Cadenza REPL.
* Command configuration in Settings, including Perform command-line arguments.
* Snippets for many common Topsy Turvy constructs.

### 0.2.0

* Support for all new language features.
* Support for command-line arguments in the _Set Command-Line Arguments_ command.

### 0.1.0

Initial release of the extension.
