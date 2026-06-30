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

    semanticTokensData: null,
    semanticTokensListeners: [],

    /**
     * Stores the latest encoded semantic token array and notifies Monaco to re-request tokens.
     * @description Called from Blazor after each analysis pass with the delta-encoded token data.
     * @param {number[]} data Flat delta-encoded token array (5 integers per token).
     */
    setSemanticTokens(data) {
        this.semanticTokensData = data;
        this.semanticTokensListeners.forEach(listener => listener());
    },

    /**
     * Registers a Monaco `DocumentSemanticTokensProvider` for the Topsy Turvy language.
     * @description Called once from Blazor in `OnAfterRenderAsync` on first render.
     *              The provider uses a push model: C# pushes encoded token data after each
     *              analysis pass via {@link setSemanticTokens}, and the `onDidChange` event
     *              notifies Monaco to re-request tokens immediately.
     */
    registerSemanticTokensProvider() {
        const self = this;
        const legend = {
            tokenTypes: ['variable', 'variable.parameter', 'variable.function'],
            tokenModifiers: ['readonly', 'deprecated']
        };

        monaco.languages.registerDocumentSemanticTokensProvider('topsy-turvy', {
            onDidChange(listener) {
                self.semanticTokensListeners.push(listener);
                return {
                    dispose() {
                        const index = self.semanticTokensListeners.indexOf(listener);
                        if (index >= 0) {
                            self.semanticTokensListeners.splice(index, 1);
                        }
                    }
                };
            },

            getLegend() {
                return legend;
            },

            provideDocumentSemanticTokens(model, lastResultId, token) {
                if (!self.semanticTokensData) {
                    return null;
                }

                return {
                    data: new Uint32Array(self.semanticTokensData),
                    resultId: null
                };
            },

            releaseDocumentSemanticTokens(resultId) {}
        });
    },

    /**
     * Enables semantic highlighting on the given Monaco editor instance and patches
     * both custom themes with the semantic token colour rules.
     * @description Called once from Blazor in `OnAfterRenderAsync` on first render.
     *              Semantic token colour rules are applied here, not in registerThemes,
     *              so they are always loaded from this file, avoiding stale-cache issues
     *              with topsy-turvy-language.js.  Calling defineTheme for an already-defined
     *              theme also invalidates the Monaco internal _tokenTheme cache, ensuring the
     *              new rules take effect immediately.
     * @param {string} editorId The `BlazorMonaco` editor element ID.
     */
    enableSemanticHighlighting(editorId) {
        monaco.editor.defineTheme('topsy-turvy-dark', {
            base: 'vs-dark',
            inherit: true,
            rules: [
                { token: 'variable',          foreground: 'D4D4D4' },
                { token: 'variable.function', foreground: 'DCDCAA' },
                { token: 'variable.readonly', foreground: '4FC1FF' },
            ],
            colors: {},
        });

        monaco.editor.defineTheme('topsy-turvy-light', {
            base: 'vs',
            inherit: true,
            rules: [
                { token: 'variable',          foreground: '000000' },
                { token: 'variable.function', foreground: '795E26' },
                { token: 'variable.readonly', foreground: '0070C1' },
            ],
            colors: {},
        });

        const editorHolder = window.blazorMonaco.editor.getEditorHolder(editorId, true);
        if (editorHolder) {
            editorHolder.editor.updateOptions({ 'semanticHighlighting.enabled': true });
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
                    contents: markdown.split('\n\n---\n\n').map(part => ({
                        value: part,
                        isTrusted: true
                    }))
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
     * Reads the persisted editor preferences from Local Storage.
     * @returns {object} The preferences object or an empty object if none is stored.
     */
    loadPreferences() {
        try {
            return JSON.parse(localStorage.getItem('topsy-turvy-editor')) ?? {};
        } catch {
            return {};
        }
    },

    /**
     * Saves the editor preferences to Local Storage.
     * @description Existing keys not present in `preferences` are preserved,
     *              so JS-managed keys, such as split ratios, and Blazor-managed keys,
     *              such as files and dark theme, can be written independently without
     *              affecting each other.
     * @param {object} preferences Editor preferences to persist.
     */
    savePreferences(preferences) {
        const combinedPreferences = {
            ...this.loadPreferences(),
            ...preferences
        };

        localStorage.setItem('topsy-turvy-editor', JSON.stringify(combinedPreferences));
    },

    /**
     * Links up the drag handle between the editor and output panes.
     * @description Called once from Blazor in `OnAfterRenderAsync` on first render.
     *              Restores a persisted split from Local Storage and attaches
     *              mouse and touch listeners so the user can drag the divider to
     *              resize the two panes.  The Monaco `automaticLayout` and the
     *              terminal `ResizeObserver` handle relayout automatically.
     */
    initPaneDrag() {
        const self = this;
        const panesContainer = document.querySelector('.panes-container');
        const paneDivider = document.querySelector('.pane-divider');
        if (!panesContainer || !paneDivider) {
            return;
        }

        const mobileQuery = window.matchMedia('(max-width: 959px)');
        const DIVIDER_PIXELS = 6;
        const MIN_SPLIT_RATIO = 0.15;
        const MAX_SPLIT_RATIO = 0.85;
        const dragConfig = {
            isActive: false,
            pointerStart: 0,
            ratioStart: 0
        };

        // Determines if the current layout is mobile.
        const isMobile = () => mobileQuery.matches;

        // Gets the size of the container along the axis of the split.
        const getContainerAxisSize = () => {
            const containerBoundingClientRectangle = panesContainer.getBoundingClientRect();
            return isMobile()
                ? containerBoundingClientRectangle.height
                : containerBoundingClientRectangle.width;
        };

        // Gets the current split ratio of the two panes.
        const getCurrentSplitRatio = () => {
            const computedStyle = window.getComputedStyle(panesContainer);
            const gridTemplate = isMobile()
                ? computedStyle.gridTemplateRows
                : computedStyle.gridTemplateColumns;
            
            return parseFloat(gridTemplate) / getContainerAxisSize();
        };

        // Gets the pointer position along the axis of the split, supporting both mouse and touch events.
        const getPointerAxisPosition = (event) => {
            const firstTouch = event.touches?.[0];
            return isMobile()
                ? (firstTouch?.clientY ?? event.clientY)
                : (firstTouch?.clientX ?? event.clientX);
        };

        // Applies the given split ratio to the panes container clamping it within the allowed range.
        const applyPaneSplit = (splitRatio) => {
            const clampedSplitRatio = Math.max(MIN_SPLIT_RATIO, Math.min(MAX_SPLIT_RATIO, splitRatio));
            const firstPaneSplitPercentage = (clampedSplitRatio * 100).toFixed(3) + '%';
            if (isMobile()) {
                panesContainer.style.gridTemplateColumns = '';
                panesContainer.style.gridTemplateRows = `${firstPaneSplitPercentage} ${DIVIDER_PIXELS}px 1fr`;
            } else {
                panesContainer.style.gridTemplateRows = '';
                panesContainer.style.gridTemplateColumns = `${firstPaneSplitPercentage} ${DIVIDER_PIXELS}px 1fr`;
            }
        };

        // Restores the split ratio from Local Storage or defaults to 50/50 if none is stored.
        const restoreSplit = () => {
            const preferences = self.loadPreferences();
            const savedRatio = isMobile()
                ? preferences.verticalSplit
                : preferences.horizontalSplit;
            
            applyPaneSplit(typeof savedRatio === 'number'
                ? savedRatio
                : 0.5
            );
        };

        // Handles the start of a drag operation, storing the initial pointer position and split ratio.
        const onDragStart = (event) => {
            dragConfig.ratioStart = getCurrentSplitRatio();
            dragConfig.pointerStart = getPointerAxisPosition(event);
            dragConfig.isActive = true;
            event.preventDefault();
        };

        // Handles the movement of the pointer during a drag operation, updating the split ratio accordingly.
        const onDragMove = (event) => {
            if (!dragConfig.isActive) {
                return;
            }

            const pointerDelta = getPointerAxisPosition(event) - dragConfig.pointerStart;
            applyPaneSplit(dragConfig.ratioStart + pointerDelta / getContainerAxisSize());
            event.preventDefault();
        };

        // Handles the end of a drag operation saving the final split ratio to Local Storage.
        const onDragEnd = () => {
            if (!dragConfig.isActive) {
                return;
            }

            dragConfig.isActive = false;
            self.savePreferences(isMobile()
                ? { verticalSplit: getCurrentSplitRatio() }
                : { horizontalSplit: getCurrentSplitRatio() }
            );
        };

        restoreSplit();
        mobileQuery.addEventListener('change', restoreSplit);

        paneDivider.addEventListener('mousedown', onDragStart);
        paneDivider.addEventListener('touchstart', onDragStart, { passive: false });
        window.addEventListener('mousemove', onDragMove);
        window.addEventListener('touchmove', onDragMove, { passive: false });
        window.addEventListener('mouseup', onDragEnd);
        window.addEventListener('touchend', onDragEnd);
    },

    /**
     * Updates the xterm terminal colour theme to match the current light or dark mode of the editor.
     * @description Called from Blazor in `OnParametersSetAsync` whenever the theme changes.
     *              XtermBlazor does not re-apply `Options` after initialisation, so the theme
     *              must be updated directly on the xterm instance via this method.
     * @param {boolean} isDarkMode Whether dark mode is active.
     */
    setTerminalTheme(isDarkMode) {
        const entries = [...XtermBlazor._terminals.entries()];
        if (!entries.length) {
            return;
        }

        const term = entries[0][1].terminal;
        term.options.theme = isDarkMode
            ? { background: '#1e1e1e', foreground: '#d4d4d4' }
            : { background: '#f5f5f5', foreground: '#1e1e1e' };
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

window.visualSymbolPanel = {
    /**
     * Starts a drag operation to resize the visual symbol panel.
     * @param {number} startX The initial X coordinate of the mouse when the drag starts.
     * @param {number} initialWidth The initial width of the panel.
     * @param {object} dotNetRef A reference to the .NET object for invoking methods.
     */
    startResize(startX, initialWidth, dotNetRef) {
        const onMouseMove = (e) => {
            const width = Math.round(initialWidth + (e.clientX - startX));
            dotNetRef.invokeMethodAsync('SetPanelWidth', width);
        };

        const onMouseUp = () => {
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
        };
        
        document.body.style.cursor = 'ew-resize';
        document.body.style.userSelect = 'none';
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    },
};
