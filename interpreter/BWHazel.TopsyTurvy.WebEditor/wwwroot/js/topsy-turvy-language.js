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

        const keywords = [
            ["HARK!",                               "program header"],
            ["or,",                                 "program subtitle"],
            ["FINALE.",                             "program end"],
            ["PRINCIPALS",                          "global declarations block"],
            ["THE CURTAIN RISES.",                  "end of declarations"],
            ["PRAY WELCOME",                        "variable declaration"],
            ["AS A",                                "type annotation"],
            ["BEING",                               "initial value"],
            ["IS APPOINTED",                        "assignment"],
            ["IS HENCEFORTH A",                     "in-place cast"],
            ["AS IT WERE",                          "expression cast"],
            ["BEHOLD",                              "print"],
            ["WITHOUT CEREMONY",                    "print (no newline)"],
            ["PRAY TELL",                           "input"],
            ["ASIDE:",                              "line comment"],
            ["(ASIDE, AT SOME LENGTH:",             "block comment (open)"],
            ["END OF ASIDE.)",                      "block comment (close)"],
            ["SHOULD IT TRANSPIRE THAT",            "if condition"],
            ["QUITE SO.",                           "then branch"],
            ["OR, IF NOT,",                         "else-if"],
            ["OTHERWISE,",                          "else branch"],
            ["SO MUCH FOR THAT.",                   "end if"],
            ["IN WHICH CAPACITY?",                  "switch"],
            ["WHEN ACTING AS",                      "case label"],
            ["FAILING ALL OF THE ABOVE,",           "default case"],
            ["NOTHING COULD BE MORE SATISFACTORY.", "end switch"],
            ["BY A LEGAL FICTION",                  "loop"],
            ["ASCENDING",                           "increment loop variable"],
            ["DESCENDING",                          "decrement loop variable"],
            ["UNTIL",                               "loop exit condition"],
            ["WHILST",                              "loop while condition"],
            ["THE TERM EXPIRES.",                   "end loop"],
            ["ONCE MORE.",                          "continue"],
            ["THAT WILL DO.",                       "break (loop or switch)"],
            ["IT IS MY DUTY TO PERFORM",            "function definition"],
            ["UNDER THE TERMS OF",                  "function parameters"],
            ["UNDER NO OBLIGATION",                 "no-parameter function"],
            ["AND SO I FIND",                       "return with value"],
            ["MY DUTY IS DISCHARGED.",              "end function"],
            ["MY DUTY IS PREMATURELY DISCHARGED.",  "return (no value)"],
            ["SUMMON",                              "function call"],
            ["WITH",                                "function call arguments"],
            ["IF YOU PLEASE.",                      "end expression list"],
            ["A HIDEOUS CURSE ON",                  "throw"],
            ["WITH THE GREATEST RESPECT,",          "try block"],
            ["WITH GRATITUDE",                      "success handler"],
            ["MODIFIED RAPTURE",                    "exception handler"],
            ["THAT CONCLUDES THE MATTER.",          "end try/catch"],
            ["PRAY ADMIT",                          "import"],
            ["SUM OF",                              "addition"],
            ["DIFFERENCE OF",                       "subtraction"],
            ["PRODUCT OF",                          "multiplication"],
            ["QUOTIENT OF",                         "division"],
            ["REMAINDER OF",                        "modulo"],
            ["LARGER OF",                           "maximum"],
            ["SMALLER OF",                          "minimum"],
            ["WOVEN OF",                            "string concatenation"],
            ["BOTH",                                "logical AND"],
            ["EITHER",                              "logical OR"],
            ["HARDLY EVER",                         "logical NOT"],
            ["ALIKE",                               "equality (==)"],
            ["UNLIKE",                              "inequality (!=)"],
            ["PRE-ADAMITE",                         "greater than (>)"],
            ["LOWER DEGREE",                        "less than (<)"],
            ["ALL OF",                              "all-true (variadic AND)"],
            ["ANY OF",                              "any-true (variadic OR)"],
            ["VERITY",                              "boolean true"],
            ["NAY",                                 "boolean false"],
            ["NAUGHT",                              "null"],
            ["JUST SO",                             "implicit accumulator"],
            ["PEER",                                "integer type"],
            ["FATHOM",                              "float type"],
            ["YARN",                                "string type"],
            ["DECREE",                              "boolean type"],
        ];

        monaco.languages.registerCompletionItemProvider('topsy-turvy', {
            triggerCharacters: [' '],
            provideCompletionItems(model, position) {
                const lineText = model.getLineContent(position.lineNumber);
                const leadingSpaces = lineText.match(/^\s*/)[0].length;
                const linePrefix = lineText.substring(leadingSpaces, position.column - 1);
                const upperCaseLinePrefix = linePrefix.toUpperCase();

                if (!upperCaseLinePrefix) {
                    return {
                        suggestions: []
                    };
                }

                const suggestions = keywords
                    .filter(([keyword]) =>
                        keyword.toUpperCase().startsWith(upperCaseLinePrefix) &&
                        keyword.toUpperCase() !== upperCaseLinePrefix)
                    .map(([keyword, description]) => ({
                        label: keyword,
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        detail: description,
                        insertText: keyword,
                        filterText: keyword,
                        range: {
                            startLineNumber: position.lineNumber,
                            endLineNumber: position.lineNumber,
                            startColumn: leadingSpaces + 1,
                            endColumn: position.column,
                        },
                    }));

                return {
                    suggestions
                };
            },
        });

        monaco.languages.setLanguageConfiguration('topsy-turvy', {
            comments: {
                lineComment: 'ASIDE:',
            },
            autoClosingPairs: [
                {
                    open: '"',
                    close: '"',
                    notIn: ['string']
                },
            ],
            surroundingPairs: [
                {
                    open: '"',
                    close: '"'
                },
            ],
            indentationRules: {
                increaseIndentPattern: new RegExp(
                    '^\\s*(' +
                    'HARK!|' +
                    'PRINCIPALS|' +
                    'SHOULD IT TRANSPIRE THAT|' +
                    'QUITE SO\\.|' +
                    'OR, IF NOT,|' +
                    'OTHERWISE,|' +
                    'WHEN ACTING AS |' +
                    'FAILING ALL OF THE ABOVE,|' +
                    'BY A LEGAL FICTION |' +
                    'IT IS MY DUTY TO PERFORM |' +
                    'WITH THE GREATEST RESPECT,|' +
                    'WITH GRATITUDE|' +
                    'MODIFIED RAPTURE|' +
                    'IN WHICH CAPACITY\\?' +
                    ')',
                    'i'
                ),
                decreaseIndentPattern: new RegExp(
                    '^\\s*(' +
                    'THE CURTAIN RISES\\.|' +
                    'SO MUCH FOR THAT\\.|' +
                    'MY DUTY IS DISCHARGED\\.|' +
                    'MY DUTY IS PREMATURELY DISCHARGED\\.|' +
                    'THE TERM EXPIRES\\.|' +
                    'NOTHING COULD BE MORE SATISFACTORY\\.|' +
                    'THAT CONCLUDES THE MATTER\\.|' +
                    'OR, IF NOT,|' +
                    'OTHERWISE,|' +
                    'FINALE\\.|' +
                    'WITH GRATITUDE|' +
                    'MODIFIED RAPTURE' +
                    ')',
                    'i'
                ),
            },
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
};
