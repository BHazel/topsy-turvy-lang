import XCTest
@testable import Theatre

/// Tests for the `CompletionBarController`.
@MainActor
final class CompletionBarControllerTests: XCTestCase {
    /// Tests that a completed update populates `candidates` with the matching keyword for a partially typed
    /// word.
    func testScheduleUpdatePopulatesCandidatesForPartialKeyword() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock {
            await session.close()
        }

        let controller = CompletionBarController()

        controller.scheduleUpdate(session: session, source: "HAR", line: 0, column: 3)
        await self.waitUntil {
            controller.candidates.contains {
                $0.Label == "HARK!"
            }
        }

        XCTAssertTrue(controller.candidates.contains {
            $0.Label == "HARK!"
        })
    }

    /// Tests that a superseded update does not interfere with the candidates from the call that supersedes it.
    ///
    /// Uses two separate lines since `PhraseContext.Get` matches the whole line up to the cursor, not just the last
    /// word.
    func testScheduleUpdateDiscardsSupersededCalls() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock { await session.close() }

        let source = "HAR\nPRA"
        let controller = CompletionBarController()
        controller.scheduleUpdate(session: session, source: source, line: 0, column: 3)
        controller.scheduleUpdate(session: session, source: source, line: 1, column: 3)
        await self.waitUntil {
            controller.candidates.contains {
                $0.Label.hasPrefix("PRAY")
            }
        }

        XCTAssertTrue(controller.candidates.contains { $0.Label.hasPrefix("PRAY") })
        XCTAssertFalse(controller.candidates.contains { $0.Label == "HARK!" })
    }

    /// Tests that a phrase matching no keyword filters to an empty list.
    func testScheduleUpdateNarrowsToNoKeywordsWhenPhraseMatchesNone() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock {
            await session.close()
        }

        let source = "HAR PRA"
        let controller = CompletionBarController()
        controller.scheduleUpdate(session: session, source: source, line: 0, column: Int32(source.utf16.count))
        
        await self.waitUntil {
            controller.lastWord == "PRA"
        }

        XCTAssertTrue(controller.candidates.isEmpty)
    }

    /// Tests that a declared variable is offered while unrelated keywords are filtered away.
    func testScheduleUpdateOffersMatchingVariableWithoutFloodingKeywords() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock {
            await session.close()
        }

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
        await self.waitUntil {
            controller.candidates.map(\.Label) == ["xylophone"]
        }

        XCTAssertEqual(controller.candidates.map(\.Label), ["xylophone"])
    }

    /// Tests that `lastWord` tracks the trailing word already typed, the range a tapped candidate should
    /// replace rather than be appended after.
    func testScheduleUpdateTracksLastWordForReplacement() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock {
            await session.close()
        }

        let controller = CompletionBarController()
        controller.scheduleUpdate(session: session, source: "FIN", line: 0, column: 3)
        await self.waitUntil {
            controller.lastWord == "FIN"
        }

        XCTAssertEqual(controller.lastWord, "FIN")
    }
    
    /// Polls until `predicate` is satisfied or `timeout` elapses.
    ///
    /// - Parameters:
    ///   - timeout: The timeout to wait.
    ///   - predicate: The condition to wait for.
    private func waitUntil(timeout: TimeInterval = 2, _ predicate: () -> Bool) async {
        let deadline = Date().addingTimeInterval(timeout)
        while !predicate(), Date() < deadline {
            try? await Task.sleep(for: .milliseconds(25))
        }
    }
}
