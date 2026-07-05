import Observation

/// Drives the keyboard completion bar: recomputes `candidates` after a short single-flight debounce whenever
/// the cursor location changes, narrowing as the user types. Deliberately always leaves `candidates` in place
/// (including empty) rather than hiding the bar — it is an always-visible fixture, not a popup.
@MainActor
@Observable
final class CompletionBarController {
    private(set) var candidates: [CompletionItemPayload] = []

    /// The trailing word already typed as of the last completed `scheduleUpdate` — i.e. the range a tapped
    /// candidate's `InsertText` should *replace*, not be appended after (`InsertText` is always the full
    /// replacement text, per `NativeExports.BuildSymbolItem`/`BuildKeywordItem`'s own semantics: a symbol's
    /// `InsertText` is unconditionally its full name, never trimmed by what's already typed). Tracked here,
    /// alongside `candidates`, rather than recomputed fresh at tap time, so what gets replaced always matches
    /// what was actually displayed.
    private(set) var lastWord = ""

    /// Incremented on every `scheduleUpdate` call; a stale generation after the debounce wait means a newer
    /// cursor/text change has superseded this call, mirroring `TopsyTurvySession.scheduleAnalyse`'s own guard.
    private var generation = 0

    /// Recomputes `candidates` for the given 0-indexed cursor position, after a ~150ms debounce — short enough
    /// to feel live while typing, mirroring the ~400ms analyse debounce's single-flight-supersede pattern at a
    /// faster cadence appropriate for a bar the user is actively typing against.
    ///
    /// Calls `session.complete` directly rather than through `TopsyTurvyLanguageService`, deliberately bypassing
    /// that type's own `currentText`/`locationService` caching (populated asynchronously by `CodeEditorView`'s
    /// package-internal wiring, on a schedule this bar's caller doesn't control) — `source` and `line`/`column`
    /// here are computed directly from the editor's own live `text`/`position` state, the same proven pattern
    /// `WorkspaceEditorHostView.navigateToDiagnostic` already uses for the reverse conversion.
    /// - Parameters:
    ///   - session: The document's session.
    ///   - source: The current, live document text.
    ///   - line: The 0-indexed cursor line.
    ///   - column: The 0-indexed cursor column.
    func scheduleUpdate(session: TopsyTurvySession, source: String, line: Int32, column: Int32) {
        generation += 1
        let thisGeneration = generation

        Task {
            try? await Task.sleep(for: .milliseconds(150))
            guard thisGeneration == generation else { return }

            let result = await session.complete(source: source, line: line, column: column)
            guard thisGeneration == generation else { return }

            let context = Self.phraseContext(source: source, line: line, column: column)
            candidates = Self.narrow(result.Items, phrase: context.phrase, lastWord: context.lastWord)
            lastWord = context.lastWord
        }
    }

    /// Narrows the engine's raw candidates for this bar's always-visible, narrow-as-typed UX. The native
    /// `topsyturvy_complete` export (shared by the CLI/LSP/web editor, not something to change for this one
    /// consumer) falls back to returning every keyword unfiltered whenever the phrase typed so far doesn't
    /// match any keyword's own prefix — a reasonable default for an on-demand, explicitly triggered popup, but
    /// one that floods an always-visible bar almost constantly, since most keystrokes aren't the start of a
    /// keyword at all. Keywords are re-filtered here by the whole phrase (since a keyword's `Label` can be
    /// multi-word, e.g. "PRAY WELCOME"); everything else is re-filtered by just the trailing word, mirroring
    /// the native export's own (already-correct, unconditional) symbol-filtering rule.
    private static func narrow(_ items: [CompletionItemPayload], phrase: String, lastWord: String) -> [CompletionItemPayload] {
        items.filter { item in
            if item.Kind == "Keyword" {
                return phrase.isEmpty || item.Label.range(of: phrase, options: [.caseInsensitive, .anchored]) != nil
            }
            return lastWord.isEmpty || item.Label.range(of: lastWord, options: [.caseInsensitive, .anchored]) != nil
        }
    }

    /// Extracts the trimmed phrase on `line` up to `column` and its trailing space-delimited word — a Swift
    /// mirror of the native `PhraseContext.Get` algorithm, since the native response doesn't expose these
    /// intermediate values and this bar needs them for its own re-filtering above. This is plain cursor-context
    /// string extraction, not a reimplementation of any parsing/analysis logic.
    private static func phraseContext(source: String, line: Int32, column: Int32) -> (phrase: String, lastWord: String) {
        let lines = source.components(separatedBy: "\n")
        guard line >= 0, Int(line) < lines.count else { return ("", "") }

        let lineText = lines[Int(line)]
        let utf16 = lineText.utf16
        let safeColumn = min(max(Int(column), 0), utf16.count)
        let cursorIndex = utf16.index(utf16.startIndex, offsetBy: safeColumn)
        let textBeforeCursor = String(decoding: utf16[..<cursorIndex], as: UTF16.self)

        let phrase = String(textBeforeCursor.drop { $0.isWhitespace })
        guard let lastSpaceIndex = phrase.lastIndex(of: " ") else { return (phrase, phrase) }
        return (phrase, String(phrase[phrase.index(after: lastSpaceIndex)...]))
    }
}
