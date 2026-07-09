import Observation

/// Controls the completions bar located, always present even when no candidates are offered, on top of keyboard.
///
/// Recomputes the completion candidates after a short debounce whenever the cursor location changes, filtering further
/// as the user types.
@MainActor
@Observable
final class CompletionBarController {
    /// The completion candidates for the current cursor position, as of the last finished update.
    private(set) var candidates: [CompletionItemInfo] = []

    /// The word currently being completed, as of the last finished update.
    ///
    /// When a candidate is tapped, this is the range that gets replaced, not just added after.
    ///
    /// The `InsertText` of a candidate is always the whole word, not just what is left to type.
    private(set) var lastWord = ""

    /// The count of how many times `scheduleUpdate` has been called.
    ///
    /// If a newer call comes in before the debounce of an older one finishes, the older one checks this count and
    /// skips applying its now-outdated result.
    private var generation = 0

    /// Recomputes the candidates for the given 0-indexed cursor position after a short debounce so it does not
    /// run on every keystroke.
    ///
    /// Calls the Topsy Turvy toolchain session directly rather than through `TopsyTurvyLanguageService`, since the
    /// text tracking in that type updates on its own schedule and is not reliable enough for this.
    ///
    /// - Parameters:
    ///   - session: The Topsy Turvy toolchain session.
    ///   - source: The source text.
    ///   - line: The 0-indexed cursor line.
    ///   - column: The 0-indexed cursor column.
    func scheduleUpdate(session: TopsyTurvySession, source: String, line: Int32, column: Int32) {
        self.generation += 1
        let thisGeneration = self.generation

        Task {
            try? await Task.sleep(for: .milliseconds(150))
            guard thisGeneration == self.generation else {
                return
            }

            let result = await session.completions(source: source, line: line, column: column)
            guard thisGeneration == self.generation else {
                return
            }

            let context = Self.phraseContext(source: source, line: line, column: column)
            self.candidates = Self.filter(result.Items, phrase: context.phrase, lastWord: context.lastWord)
            self.lastWord = context.lastWord
        }
    }

    /// Filters the candidate results.
    ///
    /// The native completion call returns every keyword unfiltered regardless of whether anything matches the phrase
    /// typed so far.
    ///
    /// - Parameters:
    ///   - items: The completion candidates.
    ///   - phrase: The currently typed phrase.
    ///   - lastWord: The last word typed.
    ///
    /// - Returns: The filtered completion candidates.
    private static func filter(_ items: [CompletionItemInfo], phrase: String, lastWord: String) -> [CompletionItemInfo] {
        items.filter { item in
            if item.Kind == "Keyword" {
                return phrase.isEmpty || item.Label.range(of: phrase, options: [.caseInsensitive, .anchored]) != nil
            }
            
            return lastWord.isEmpty || item.Label.range(of: lastWord, options: [.caseInsensitive, .anchored]) != nil
        }
    }

    /// Determines the text typed so far on `line` up to `column` and the last word within it.
    ///
    /// This matches the same logic the native completion engine uses internally since it does not return
    /// these values itself.
    ///
    /// - Parameters:
    ///   - source: The source text.
    ///   - line: The 0-indexed line.
    ///   - column: The 0-indexed column.
    ///
    /// - Returns: The phrase typed so far, and the last word within it.
    private static func phraseContext(source: String, line: Int32, column: Int32) -> (phrase: String, lastWord: String) {
        let lines = source.components(separatedBy: "\n")
        guard line >= 0, Int(line) < lines.count else {
            return ("", "")
        }

        let lineText = lines[Int(line)]
        let utf16Line = lineText.utf16
        let safeColumn = min(max(Int(column), 0), utf16Line.count)
        let cursorIndex = utf16Line.index(utf16Line.startIndex, offsetBy: safeColumn)
        let textBeforeCursor = String(decoding: utf16Line[..<cursorIndex], as: UTF16.self)

        let phrase = String(textBeforeCursor.drop { $0.isWhitespace })
        guard let lastSpaceIndex = phrase.lastIndex(of: " ") else {
            return (phrase, phrase)
        }
        
        return (phrase, String(phrase[phrase.index(after: lastSpaceIndex)...]))
    }
}
