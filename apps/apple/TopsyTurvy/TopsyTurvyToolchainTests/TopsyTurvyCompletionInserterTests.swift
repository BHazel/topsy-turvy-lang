import XCTest
@testable import Theatre

/// Tests for `TopsyTurvyCompletionInserter`'s pure splice-and-advance-cursor logic.
final class TopsyTurvyCompletionInserterTests: XCTestCase {
    /// Tests that a suffix `InsertText` (continuing a partially typed word) is appended at the cursor, and
    /// the cursor advances to immediately after the inserted text.
    func testInsertAppendsSuffixAndAdvancesCursor() {
        let item = CompletionItemPayload(Label: "HARK!", Kind: "Keyword", Detail: nil, InsertText: "K!")

        let result = TopsyTurvyCompletionInserter.insert(item, into: "HAR", at: NSRange(location: 3, length: 0))

        XCTAssertEqual(result.text, "HARK!")
        XCTAssertEqual(result.selection, NSRange(location: 5, length: 0))
    }

    /// Tests inserting a full candidate (no suffix trimming) into an empty buffer.
    func testInsertFullCandidateIntoEmptyBuffer() {
        let item = CompletionItemPayload(Label: "HARK!", Kind: "Keyword", Detail: nil, InsertText: "HARK!")

        let result = TopsyTurvyCompletionInserter.insert(item, into: "", at: NSRange(location: 0, length: 0))

        XCTAssertEqual(result.text, "HARK!")
        XCTAssertEqual(result.selection, NSRange(location: 5, length: 0))
    }

    /// Tests that inserting mid-buffer preserves the text both before and after the splice point.
    func testInsertPreservesSurroundingText() {
        let item = CompletionItemPayload(Label: "WELCOME", Kind: "Keyword", Detail: nil, InsertText: "COME")

        let result = TopsyTurvyCompletionInserter.insert(item, into: "PRAY WEL x", at: NSRange(location: 8, length: 0))

        XCTAssertEqual(result.text, "PRAY WELCOME x")
        XCTAssertEqual(result.selection, NSRange(location: 12, length: 0))
    }

    /// Tests that a non-empty selection is replaced entirely, not just inserted alongside.
    func testInsertReplacesNonEmptySelection() {
        let item = CompletionItemPayload(Label: "QUX", Kind: "Variable", Detail: nil, InsertText: "QUX")

        let result = TopsyTurvyCompletionInserter.insert(item, into: "FOO BAR BAZ", at: NSRange(location: 4, length: 3))

        XCTAssertEqual(result.text, "FOO QUX BAZ")
        XCTAssertEqual(result.selection, NSRange(location: 7, length: 0))
    }
}
