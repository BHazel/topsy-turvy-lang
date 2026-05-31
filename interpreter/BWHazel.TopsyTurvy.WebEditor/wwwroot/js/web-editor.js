Object.assign(window.topsyTurvy, {
    /**
     * Opens a native file picker filtered to `.topsy` files.
     * @description Resolves `null` if the user cancels the picker.
     * @returns {Promise<{name: string, content: string} | null>} The file name and contents, or `null` if cancelled.
     */
    openFile() {
        const input = Object.assign(document.createElement('input'), {
            type: 'file',
            accept: '.topsy',
        });

        return new Promise((resolve) => {
            let isResolved = false;

            input.addEventListener('change', async () => {
                isResolved = true;
                const file = input.files?.[0];
                if (file) {
                    resolve({ name: file.name, content: await file.text() });
                } else {
                    resolve(null);
                }
            });

            window.addEventListener('focus', function onFocus() {
                window.removeEventListener('focus', onFocus);
                setTimeout(() => {
                    if (!isResolved) {
                        resolve(null);
                    }
                }, 300);
            });

            input.click();
        });
    },

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
