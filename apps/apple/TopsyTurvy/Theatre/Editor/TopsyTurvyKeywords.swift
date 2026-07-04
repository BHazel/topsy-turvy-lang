import Foundation

/// Mirrors `KeywordData` (`operetta/BWHazel.TopsyTurvy.Analysis/KeywordData.cs`) on the Swift side, for
/// deriving `CodeEditorView`'s `LanguageConfiguration` keyword list.
///
/// `KeywordData` is the authoritative source of truth for language keywords; this array must be kept in step
/// with it whenever a keyword is added, renamed or removed, joining the REPL/Monaco/TextMate highlighting
/// sync group (see `AGENTS.md`).
enum TopsyTurvyKeywords {
    /// All language keywords and their short descriptions, in the same order as `KeywordData.Keywords`.
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
        ("BY", "loop step (ASCENDING/DESCENDING ... BY <expr>)"),
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
        ("TRANSPOSITION UP", "left shift by 1 (unary)"),
        ("TRANSPOSITION DOWN", "right shift by 1 (unary)"),
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
    /// `CodeEditorView` anchors each reserved identifier with a word-boundary (`\b`) on both sides; since a
    /// word boundary requires a transition between a word and a non-word character, a reserved string ending
    /// in punctuation (e.g. `"FINALE."`, `"IF YOU PLEASE."`) would fail to match at its trailing edge, where
    /// both the punctuation and the following whitespace are non-word characters. Splitting on whitespace and
    /// stripping leading/trailing punctuation avoids that failure mode; the cosmetic cost is that trailing
    /// punctuation itself isn't highlighted, matching the word-by-word highlighting already accepted for
    /// multi-word keywords in `TOURING_THEATRE_PLAN.md` §5.4.
    static var reservedWords: [String] {
        var seen = Set<String>()
        var words: [String] = []
        for (keyword, _) in all {
            for fragment in keyword.split(separator: " ") {
                let stripped = fragment.trimmingCharacters(in: wordBoundaryPunctuation)
                if !stripped.isEmpty, seen.insert(stripped).inserted {
                    words.append(stripped)
                }
            }
        }
        return words
    }

    private static let wordBoundaryPunctuation = CharacterSet(charactersIn: "!?.,:()")
}
