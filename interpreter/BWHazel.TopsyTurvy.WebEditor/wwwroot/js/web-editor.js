Object.assign(window.topsyTurvy, {
    /**
     * Triggers a browser download of a plain-text file.
     * @param {string} filename Suggested file name for the download.
     * @param {string} content Text content to write into the file.
     */
    downloadText(filename, content) {
        const url = URL.createObjectURL(
            new Blob([content], {
                type: 'text/plain;charset=utf-8'
            })
        );

        const aElement = Object.assign(document.createElement('a'), {
            href: url,
            download: filename
        });

        document.body.appendChild(aElement);
        aElement.click();
        document.body.removeChild(aElement);
        URL.revokeObjectURL(url);
    },

    /**
     * Triggers a browser download of a ZIP file from a base-64 encoded string.
     * @param {string} filename Suggested file name for the download.
     * @param {string} base64 Base-64 encoded ZIP file contents.
     */
    downloadZip(filename, base64) {
        const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
        const url = URL.createObjectURL(new Blob([bytes], { type: 'application/zip' }));
        const aElement = Object.assign(document.createElement('a'), {
            href: url,
            download: filename
        });

        document.body.appendChild(aElement);
        aElement.click();
        document.body.removeChild(aElement);
        URL.revokeObjectURL(url);
    },

    /**
     * Registers the Topsy Turvy language (if not already done) and sets it
     * as the active language on the given Monaco editor instance.
     * @description Called once from the Blazor `OnAfterRenderAsync` on first render.
     * @param {string} editorId The `BlazorMonaco` editor element ID.
     */
    applyLanguageToEditor(editorId) {
        this.registerLanguage();
        this.registerThemes();
        const editorHolder = window.blazorMonaco.editor.getEditorHolder(editorId, true);
        if (!editorHolder) {
            return;
        }

        const model = editorHolder.editor.getModel();
        if (!model) {
            return;
        }

        monaco.editor.setModelLanguage(model, 'topsy-turvy');
        editorHolder.editor.updateOptions({ autoIndent: 'full' });
    },

    /**
     * Applies inline decorations to all constant identifier occurrences.
     * @description Called from Blazor after each analysis pass. Replaces the previous
     *              decoration set so stale ranges are cleared automatically.
     * @param {string} editorId The `BlazorMonaco` editor element ID.
     * @param {object[]} ranges The ranges of constant identifiers to decorate.
     */
    setConstantDecorations(editorId, ranges) {
        const editorHolder = window.blazorMonaco.editor.getEditorHolder(editorId, true);
        if (!editorHolder) {
            return;
        }

        const decorations = ranges.map(range => ({
            range,
            options: {
                inlineClassName: 'topsy-constant-var'
            },
        }));

        if (this._constantDecorations) {
            this._constantDecorations.set(decorations);
        } else {
            this._constantDecorations = editorHolder.editor.createDecorationsCollection(decorations);
        }
    },

    /**
     * Pushes parse-error diagnostics to Monaco as red squiggle markers.
     * @param {string} editorId The BlazorMonaco editor element ID.
     * @param {object[]} markers Monaco marker descriptors from the Blazor parser.
     */
    setModelMarkers(editorId, markers) {
        const editorHolder = window.blazorMonaco.editor.getEditorHolder(editorId, true);
        if (!editorHolder) {
            return;
        }

        const model = editorHolder.editor.getModel();
        if (!model) {
            return;
        }

        monaco.editor.setModelMarkers(model, 'topsy-turvy', markers);
    },

    /**
     * Registers a Monaco hover provider for Topsy Turvy.
     * @description Called once from Blazor in `OnAfterRenderAsync` on first render.  It calls
     *              back into Blazor to retrieve symbol Markdown for the word under the cursor.
     *              Monaco positions are 1-indexed; the C# bridge expects 0-indexed.
     * @param {DotNetObjectReference} dotNetRef Blazor interop reference to the Editor component.
     */
    registerHoverProvider(dotNetRef) {
        monaco.languages.registerHoverProvider('topsy-turvy', {
            async provideHover(model, position) {
                const markdown = await dotNetRef.invokeMethodAsync(
                    'GetHoverMarkdown',
                    position.lineNumber - 1,
                    position.column - 1
                );

                if (!markdown) {
                    return null;
                }

                return {
                    contents: [
                        {
                            value: markdown,
                            isTrusted: true
                        }
                    ]
                };
            }
        });
    },

    /**
     * Registers a Monaco completion provider for Topsy Turvy.
     * @description Runs alongside the keyword provider already registered and
     *              Monaco merges both result sets.  Supplies
     *              symbol names from the current symbol table.
     * @param {DotNetObjectReference} dotNetRef Blazor interop reference to the Editor component.
     */
    registerSymbolCompletionProvider(dotNetRef) {
        monaco.languages.registerCompletionItemProvider('topsy-turvy', {
            async provideCompletionItems(model, position) {
                const symbols = await dotNetRef.invokeMethodAsync('GetSymbolCompletions');
                if (!symbols?.length) {
                    return {
                        suggestions: []
                    };
                }

                const word = model.getWordUntilPosition(position);
                const range = {
                    startLineNumber: position.lineNumber,
                    endLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endColumn: position.column,
                };

                const kindMap = {
                    variable:  monaco.languages.CompletionItemKind.Variable,
                    function:  monaco.languages.CompletionItemKind.Function,
                    parameter: monaco.languages.CompletionItemKind.Variable,
                };

                const suggestions = symbols.map(sym => ({
                    label: sym.name,
                    kind: kindMap[sym.kind] ?? monaco.languages.CompletionItemKind.Variable,
                    detail: sym.detail,
                    insertText: sym.name,
                    filterText: sym.name,
                    range,
                }));

                return {
                    suggestions
                };
            }
        });
    },

    /**
     * Registers a Monaco definition provider for Topsy Turvy.
     * @description Uses the 1-indexed line/column returned by the C# bridge directly
     *              as Monaco range coordinates (both are 1-indexed).  Jumps to the
     *              declaration of the symbol under the cursor within the current file.
     * @param {DotNetObjectReference} dotNetRef Blazor interop reference to the Editor component.
     */
    registerDefinitionProvider(dotNetRef) {
        monaco.languages.registerDefinitionProvider('topsy-turvy', {
            async provideDefinition(model, position) {
                const location = await dotNetRef.invokeMethodAsync(
                    'GetDefinitionLocation',
                    position.lineNumber - 1,
                    position.column - 1
                );

                if (!location) {
                    return null;
                }

                if (location.fileName) {
                    await dotNetRef.invokeMethodAsync('SwitchToFileForDefinitionAsync', location.fileName);
                    await new Promise(r => setTimeout(r, 50));
                }

                return {
                    uri: model.uri,
                    range: {
                        startLineNumber: location.line,
                        startColumn: location.column,
                        endLineNumber: location.line,
                        endColumn: location.column,
                    }
                };
            }
        });
    },

    /**
     * Attaches a `ResizeObserver` to the `terminal-wrapper` CSS class so that
     * {@link fitTerminal} is called automatically whenever the pane resizes.
     * @description This is idempotent so repeated calls are ignored.
     */
    setupTerminalFit() {
        const wrapper = document.querySelector('.terminal-wrapper');
        if (!wrapper || this._terminalResizeObserver) {
            return;
        }

        this._terminalResizeObserver = new ResizeObserver(() => this.fitTerminal());
        this._terminalResizeObserver.observe(wrapper);
    },

    /**
     * Resizes the xterm terminal row count to fill the height of the
     * `terminal-wrapper` CSS class.
     * @description Cell height is read from the xterm internal render
     *              service with DOM measurement and font-size estimation as
     *              fallbacks.
     *              Prefer the precise cell height from the xterm internal
     *              render service.  Fall back to DOM measurement, then to a
     *              font-size estimate.
     */
    fitTerminal() {
        const entries = [...XtermBlazor._terminals.entries()];
        if (!entries.length) {
            return;
        }

        const term = entries[0][1].terminal;

        const wrapper = document.querySelector('.terminal-wrapper');
        if (!wrapper?.clientHeight) {
            return;
        }

        let rowHeight;
        try {
            rowHeight = term._core._renderService.dimensions.css.cell.height;
        } catch {
            const rowElement = wrapper.querySelector('.xterm-rows > div');
            rowHeight = rowElement?.getBoundingClientRect().height
                ?? Math.ceil((term.options?.fontSize ?? 15) * (term.options?.lineHeight ?? 1.2));
        }

        if (!rowHeight) {
            return;
        }

        const newRows = Math.max(1, Math.floor(wrapper.clientHeight / rowHeight));
        if (newRows !== term.rows) {
            term.resize(term.cols, newRows);
        }
    },
});
