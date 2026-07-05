import XCTest
@testable import Theatre

/// Tests for `CompletionBarController`'s debounce/narrowing state machine, against the real engine via
/// `TopsyTurvySession.complete` directly (mirroring `TopsyTurvySessionTests`'s round-trip approach, not a
/// canned mock) — matching how `WorkspaceEditorHostView` actually calls it (bypassing `TopsyTurvyLanguageService`,
/// see `CompletionBarController.scheduleUpdate`'s doc comment for why).
@MainActor
final class CompletionBarControllerTests: XCTestCase {
    /// Tests that a completed update populates `candidates` with the matching keyword for a partially typed
    /// word.
    func testScheduleUpdatePopulatesCandidatesForPartialKeyword() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock { await session.close() }

        let controller = CompletionBarController()
        controller.scheduleUpdate(session: session, source: "HAR", line: 0, column: 3)
        try? await Task.sleep(for: .milliseconds(300))

        XCTAssertTrue(controller.candidates.contains { $0.Label == "HARK!" })
    }

    /// Tests that an update superseded by a later call before its debounce completes does not clobber the
    /// superseding call's candidates. Uses two separate lines (rather than two words on one line) so each
    /// cursor position's keyword-prefix filtering is unambiguous — `PhraseContext.Get`'s "phrase" is the whole
    /// line up to the cursor, not just the last word, so two words sharing a line would both need to match a
    /// keyword prefix together.
    func testScheduleUpdateDiscardsSupersededCalls() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock { await session.close() }

        let source = "HAR\nPRA"
        let controller = CompletionBarController()
        controller.scheduleUpdate(session: session, source: source, line: 0, column: 3) // after "HAR"
        controller.scheduleUpdate(session: session, source: source, line: 1, column: 3) // after "PRA", supersedes immediately
        try? await Task.sleep(for: .milliseconds(300))

        XCTAssertTrue(controller.candidates.contains { $0.Label.hasPrefix("PRAY") })
        XCTAssertFalse(controller.candidates.contains { $0.Label == "HARK!" })
    }

    /// Tests that a phrase not matching any keyword's prefix narrows to no keywords, rather than the native
    /// export's own on-demand-popup-oriented fallback of returning every keyword unfiltered — the bug behind
    /// the completion bar showing every keyword regardless of what was typed (2026-07-05).
    func testScheduleUpdateNarrowsToNoKeywordsWhenPhraseMatchesNone() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock { await session.close() }

        let source = "HAR PRA"
        let controller = CompletionBarController()
        controller.scheduleUpdate(session: session, source: source, line: 0, column: Int32(source.utf16.count))
        try? await Task.sleep(for: .milliseconds(300))

        XCTAssertTrue(controller.candidates.isEmpty)
    }

    /// Tests that a declared variable is offered while unrelated keywords are narrowed away, for a trailing
    /// word that doesn't match any keyword's prefix. Needs a syntactically complete programme (unlike the
    /// other tests here) — `SymbolTable.Build` only runs when `parseResult.Program` is non-`nil`.
    func testScheduleUpdateOffersMatchingVariableWithoutFloodingKeywords() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock { await session.close() }

        let source = """
        HARK! "Test"

        PRINCIPALS
          PRAY WELCOME xylophone AS A PEER BEING 1
        THE CURTAIN RISES.

        xylophone IS APPOINTED xy

        FINALE.
        """
        let lines = source.components(separatedBy: "\n")
        let targetLine = try XCTUnwrap(lines.firstIndex { $0.hasSuffix("xy") })

        let controller = CompletionBarController()
        controller.scheduleUpdate(session: session, source: source, line: Int32(targetLine), column: Int32(lines[targetLine].utf16.count))
        try? await Task.sleep(for: .milliseconds(300))

        XCTAssertEqual(controller.candidates.map(\.Label), ["xylophone"])
    }

    /// Tests that `lastWord` tracks the trailing word already typed as of the completed update — the range a
    /// tapped candidate's `InsertText` should replace (`WorkspaceEditorHostView.insertCompletion`), not be
    /// appended after; getting this wrong produced the reported "FIN" + tapping "FINALE." → "FINFINALE." bug
    /// (2026-07-05), since `InsertText` is always the *full* replacement text (a symbol's is unconditionally
    /// its whole name; a keyword's, once correctly narrowed, is also whole in the single-word case).
    func testScheduleUpdateTracksLastWordForReplacement() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock { await session.close() }

        let controller = CompletionBarController()
        controller.scheduleUpdate(session: session, source: "FIN", line: 0, column: 3)
        try? await Task.sleep(for: .milliseconds(300))

        XCTAssertEqual(controller.lastWord, "FIN")
    }
}
