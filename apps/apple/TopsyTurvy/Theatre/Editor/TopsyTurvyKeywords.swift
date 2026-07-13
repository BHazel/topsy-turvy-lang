import Foundation

/// Keywords for the Topsy Turvy language.
enum TopsyTurvyKeywords {
    /// All language keywords and their short descriptions.
    static let all: [(keyword: String, detail: String)] = [
        ("HARK!", "program header"),
        ("or,", "program subtitle"),
        ("FINALE.", "program end"),
        ("PRINCIPALS", "global declarations block"),
        ("THE CURTAIN RISES.", "end of declarations"),
        ("PRAY WELCOME", "variable declaration"),
        ("AS A", "type annotation"),
        ("BEING", "initial value"),
        ("CONSERVATIVE", "constant declaration modifier"),
        ("LIBERAL", "mutable declaration modifier"),
        ("IS APPOINTED", "assignment"),
        ("AS IT WERE", "expression cast"),
        ("BEHOLD", "print"),
        ("WITHOUT CEREMONY", "print (no newline)"),
        ("PRAY TELL", "input"),
        ("ASIDE:", "line comment"),
        ("(ASIDE, AT SOME LENGTH:", "block comment (open)"),
        ("END OF ASIDE.)", "block comment (close)"),
        ("LEGEND:", "documentation comment (summary)"),
        ("RECITATIVE:", "documentation comment (remarks)"),
        ("ARTICLE", "documentation comment (parameter)"),
        ("CONSEQUENCE", "documentation comment (return value)"),
        ("CURSES", "documentation comment (thrown value)"),
        ("CHORUS:", "documentation comment (code example)"),
        ("ENSEMBLE:", "documentation comment (see also)"),
        ("STATUTORY:", "documentation comment (deprecation notice)"),
        ("SHOULD IT TRANSPIRE THAT", "if condition"),
        ("QUITE SO.", "then branch"),
        ("OR, IF NOT,", "else-if"),
        ("OTHERWISE,", "else branch"),
        ("SO MUCH FOR THAT.", "end if"),
        ("IN WHICH CAPACITY?", "switch"),
        ("WHEN ACTING AS", "case label"),
        ("FAILING ALL OF THE ABOVE,", "default case"),
        ("NOTHING COULD BE MORE SATISFACTORY.", "end switch"),
        ("BY A LEGAL FICTION", "loop"),
        ("ASCENDING", "increment loop variable"),
        ("DESCENDING", "decrement loop variable"),
        ("BY", "loop step / transposition shift amount (ASCENDING/DESCENDING ... BY <expr>, TRANSPOSITION UP/DOWN ... BY <expr>)"),
        ("UNTIL", "loop exit condition"),
        ("WHILST", "loop while condition"),
        ("THE TERM EXPIRES.", "end loop"),
        ("ONCE MORE.", "continue"),
        ("THAT WILL DO.", "break (loop or switch)"),
        ("IT IS MY DUTY TO PERFORM", "function definition"),
        ("UNDER THE TERMS OF", "function parameters"),
        ("UNDER NO OBLIGATION", "no-parameter function"),
        ("TO FIND", "function return type"),
        ("AND SO I FIND", "return with value"),
        ("MY DUTY IS DISCHARGED.", "end function"),
        ("MY DUTY IS PREMATURELY DISCHARGED.", "return (no value)"),
        ("SUMMON", "function call"),
        ("WITH", "function call arguments"),
        ("IF YOU PLEASE.", "end expression list"),
        ("A HIDEOUS CURSE ON", "throw"),
        ("WITH THE GREATEST RESPECT,", "try block"),
        ("WITH GRATITUDE", "success handler"),
        ("MODIFIED RAPTURE", "exception handler"),
        ("THAT CONCLUDES THE MATTER.", "end try/catch"),
        ("PRAY ADMIT", "import"),
        ("SUM OF", "addition"),
        ("DIFFERENCE OF", "subtraction"),
        ("PRODUCT OF", "multiplication"),
        ("QUOTIENT OF", "division"),
        ("REMAINDER OF", "modulo"),
        ("LARGER OF", "maximum"),
        ("SMALLER OF", "minimum"),
        ("WOVEN OF", "string concatenation"),
        ("BOTH", "logical AND"),
        ("EITHER", "logical OR"),
        ("OR", "logical OR separator (EITHER x OR y)"),
        ("HARDLY EVER", "logical NOT"),
        ("ALIKE", "equality (==)"),
        ("UNLIKE", "inequality (!=)"),
        ("PRE-ADAMITE", "greater than (>)"),
        ("LOWER DEGREE", "less than (<)"),
        ("ALL OF", "all-true (variadic AND)"),
        ("ANY OF", "any-true (variadic OR)"),
        ("CHORD OF", "bitwise AND"),
        ("HARMONY OF", "bitwise OR"),
        ("DISCORD OF", "bitwise XOR"),
        ("INVERSION OF", "bitwise NOT (unary)"),
        ("TRANSPOSITION UP", "left shift, defaults to 1 (optional: ... BY <expr>)"),
        ("TRANSPOSITION DOWN", "right shift, defaults to 1 (optional: ... BY <expr>)"),
        ("VERITY", "boolean true"),
        ("NAY", "boolean false"),
        ("NAUGHT", "null"),
        ("PEER", "32-bit signed integer type"),
        ("CHANCELLOR", "64-bit signed integer type"),
        ("PIRATE", "16-bit signed integer type"),
        ("SAUSAGE-ROLL", "8-bit signed integer type"),
        ("STANDING", "unsigned integer modifier"),
        ("FATHOM", "64-bit double-precision float type"),
        ("FOOT", "32-bit single-precision float type"),
        ("YARN", "string type"),
        ("STITCH", "single character type"),
        ("DECREE", "boolean type"),
        ("A LITTLE LIST OF", "array type annotation"),
        ("VICTIM", "array element access / assignment"),
        ("ON", "array index separator"),
        ("RECKONING OF", "array length expression"),
        ("YEOMAN", "guard clause"),
        ("UNDER ORDERS.", "end guard"),
        ("THE LAW IS", "assert statement"),
    ]

    /// The keyword list flattened into individual, punctuation-stripped words, deduplicated, for use as
    /// `LanguageConfiguration.reservedIdentifiers`.
    ///
    /// `CodeEditorView` matches reserved identifiers on a word-boundary basis, so a multi-word or
    /// punctuated keyword must be split into its bare word components first; the punctuation itself is
    /// simply not highlighted as a result.
    static var reservedWords: [String] {
        var keywordComponentsSeen = Set<String>()
        var reservedWords: [String] = []
        for (keyword, _) in self.all {
            for keywordComponent in keyword.split(separator: " ") {
                let strippedComponent = keywordComponent.trimmingCharacters(in: self.wordBoundaryPunctuation)
                if !strippedComponent.isEmpty, keywordComponentsSeen.insert(strippedComponent).inserted {
                    reservedWords.append(strippedComponent)
                }
            }
        }
        
        return reservedWords
    }

    /// The punctuation characters stripped from a keyword when splitting it into reserved words.
    private static let wordBoundaryPunctuation = CharacterSet(charactersIn: "!?.,:()")
}
