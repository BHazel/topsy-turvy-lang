window.topsyTurvy = {
    /**
     * Registers the Topsy Turvy language with Monaco and installs the
     * Monarch tokenizer for syntax highlighting.
     */
    registerLanguage() {
        monaco.languages.register({
            id: 'topsy-turvy',
            extensions: ['.topsy']
        });

        monaco.languages.setMonarchTokensProvider('topsy-turvy', {
            ignoreCase: true,
            defaultToken: '',
            tokenizer: {
                root: [
                    // Block comment: (ASIDE, AT SOME LENGTH: ... END OF ASIDE.)
                    [/\(ASIDE,\s+AT\s+SOME\s+LENGTH:/, 'comment', '@blockComment'],

                    // Line comment: ASIDE: ...
                    [/\bASIDE:.*$/, 'comment'],

                    // String literal
                    [/"/, 'string', '@string'],

                    // Numbers — float before integer
                    [/\b[0-9]+\.[0-9]+\b/, 'number.float'],
                    [/\b[0-9]+\b/, 'number'],

                    // Implicit variable — before other keyword rules
                    [/\bJUST\s+SO\b/, 'variable'],

                    // Long multi-word keywords with no shared prefix conflicts
                    [/\bNOTHING\s+COULD\s+BE\s+MORE\s+SATISFACTORY\./, 'keyword'],
                    [/\bFAILING\s+ALL\s+OF\s+THE\s+ABOVE,/, 'keyword'],
                    [/\bSHOULD\s+IT\s+TRANSPIRE\s+THAT\b/, 'keyword'],
                    [/\bIN\s+WHICH\s+CAPACITY\?/, 'keyword'],
                    [/\bWHEN\s+ACTING\s+AS\b/, 'keyword'],
                    [/\bIT\s+IS\s+MY\s+DUTY\s+TO\s+PERFORM\b/, 'keyword'],
                    [/\bA\s+HIDEOUS\s+CURSE\s+ON\b/, 'keyword'],
                    [/\bBY\s+A\s+LEGAL\s+FICTION\b/, 'keyword'],
                    [/\bSO\s+MUCH\s+FOR\s+THAT\./, 'keyword'],
                    [/\bTHAT\s+CONCLUDES\s+THE\s+MATTER\./, 'keyword'],
                    [/\bWITHOUT\s+CEREMONY\b/, 'keyword'],
                    [/\bMODIFIED\s+RAPTURE\b/, 'keyword'],
                    [/\bIF\s+YOU\s+PLEASE\./, 'keyword'],
                    [/\bONCE\s+MORE\./, 'keyword'],

                    // MY DUTY IS PREMATURELY DISCHARGED must precede MY DUTY IS DISCHARGED
                    [/\bMY\s+DUTY\s+IS\s+PREMATURELY\s+DISCHARGED\./, 'keyword'],
                    [/\bMY\s+DUTY\s+IS\s+DISCHARGED\./, 'keyword'],

                    // UNDER THE TERMS OF must precede UNDER NO OBLIGATION
                    [/\bUNDER\s+THE\s+TERMS\s+OF\b/, 'keyword'],
                    [/\bUNDER\s+NO\s+OBLIGATION\b/, 'keyword'],

                    // AND SO I FIND must precede standalone AND
                    [/\bAND\s+SO\s+I\s+FIND\b/, 'keyword'],

                    // OR, IF NOT, must precede standalone OR,
                    [/\bOR,\s+IF\s+NOT,/, 'keyword'],
                    [/\bOR,/, 'keyword'],

                    // THE CURTAIN RISES and THE TERM EXPIRES share THE prefix
                    [/\bTHE\s+CURTAIN\s+RISES\./, 'keyword'],
                    [/\bTHE\s+TERM\s+EXPIRES\./, 'keyword'],

                    // THAT WILL DO and THAT CONCLUDES (already done above)
                    [/\bTHAT\s+WILL\s+DO\./, 'keyword'],

                    // QUITE SO
                    [/\bQUITE\s+SO\./, 'keyword'],

                    // WITH group — longest first to avoid shorter match winning
                    [/\bWITH\s+THE\s+GREATEST\s+RESPECT,/, 'keyword'],
                    [/\bWITH\s+GRATITUDE\b/, 'keyword'],
                    [/\bWITH\s+NOTHING\b/, 'keyword'],

                    // AS IT WERE must precede AS A
                    [/\bAS\s+IT\s+WERE\b/, 'keyword'],
                    [/\bAS\s+A\b/, 'keyword'],

                    // IS HENCEFORTH A must precede IS APPOINTED
                    [/\bIS\s+HENCEFORTH\s+A\b/, 'keyword'],
                    [/\bIS\s+APPOINTED\b/, 'keyword'],

                    // PRAY group — order doesn't matter since second words differ,
                    // but list all three together for clarity
                    [/\bPRAY\s+TELL\b/, 'keyword'],
                    [/\bPRAY\s+ADMIT\b/, 'keyword'],
                    [/\bPRAY\s+WELCOME\b/, 'keyword'],

                    // Remaining multi-word operators and keywords
                    [/\b(SUM|DIFFERENCE|PRODUCT|QUOTIENT|REMAINDER|LARGER|SMALLER)\s+OF\b/, 'keyword'],
                    [/\bWOVEN\s+OF\b/, 'keyword'],
                    [/\bHARDLY\s+EVER\b/, 'keyword'],
                    [/\bLOWER\s+DEGREE\b/, 'keyword'],
                    [/\bALL\s+OF\b/, 'keyword'],
                    [/\bANY\s+OF\b/, 'keyword'],
                    [/\bKNOWN\s+AS\b/, 'keyword'],

                    // Single-word keywords
                    [/\bHARK!/, 'keyword'],
                    [/\bFINALE\./, 'keyword'],
                    [/\bPRINCIPALS\b/, 'keyword'],
                    [/\bOTHERWISE,/, 'keyword'],
                    [/\bASCENDING\b/, 'keyword'],
                    [/\bDESCENDING\b/, 'keyword'],
                    [/\bUNTIL\b/, 'keyword'],
                    [/\bWHILST\b/, 'keyword'],
                    [/\bSUMMON\b/, 'keyword'],
                    [/\bBEHOLD\b/, 'keyword'],
                    [/\bBEING\b/, 'keyword'],
                    [/\bBOTH\b/, 'keyword'],
                    [/\bEITHER\b/, 'keyword'],

                    // Operators that need to follow their multi-word variants above
                    [/\bAND\b/, 'keyword'],
                    [/\bWITH\b/, 'keyword'],
                    [/\b(ALIKE|UNLIKE)\b/, 'keyword'],
                    [/\bPRE-ADAMITE\b/, 'keyword'],

                    // Boolean and null literals
                    [/\bVERITY\b/, 'keyword'],
                    [/\bNAY\b/, 'keyword'],
                    [/\bNAUGHT\b/, 'keyword'],

                    // Type names
                    [/\bPEER\b/, 'type'],
                    [/\bFATHOM\b/, 'type'],
                    [/\bYARN\b/, 'type'],
                    [/\bDECREE\b/, 'type'],

                    // Identifiers (after all keyword rules)
                    [/[A-Za-z][A-Za-z0-9_-]*/, ''],
                ],
                blockComment: [
                    [/END\s+OF\s+ASIDE\.\)/, 'comment', '@pop'],
                    [/./, 'comment'],
                ],
                string: [
                    [/~[nt"~]/, 'string.escape'],
                    [/"/, 'string', '@pop'],
                    [/[^"~]+/, 'string'],
                ],
            },
        });
    },

    /**
     * Opens a native file picker filtered to `.topsy` files.
     * @description Resolves `null` if the user cancels the picker or closes the file dialog without selecting a file.
     * @returns {Promise<string | null>} The file contents, or `null` if cancelled.
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
                resolve(file
                    ? await file.text()
                    : null
                );
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
            const rowElelement = wrapper.querySelector('.xterm-rows > div');
            rowHeight = rowElelement?.getBoundingClientRect().height
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
};
