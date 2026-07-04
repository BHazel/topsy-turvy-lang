import LanguageSupport
import XCTest
@testable import Theatre

/// Tests for `IssuesListView.sorted(_:)`, the diagnostics display-ordering logic.
final class IssuesListViewTests: XCTestCase {
    /// Tests that diagnostics are ordered top-to-bottom, left-to-right, regardless of `Set` iteration order.
    func testSortedOrdersTopToBottomLeftToRight() {
        let third = Self.diagnostic(line: 5, column: 1, summary: "third")
        let first = Self.diagnostic(line: 1, column: 1, summary: "first")
        let second = Self.diagnostic(line: 1, column: 4, summary: "second")

        let sorted = IssuesListView.sorted([third, first, second])

        XCTAssertEqual(sorted.map(\.entity.summary), ["first", "second", "third"])
    }

    /// Tests that an empty set sorts to an empty array.
    func testSortedWithEmptySetReturnsEmptyArray() {
        XCTAssertTrue(IssuesListView.sorted([]).isEmpty)
    }

    /// Regression test for a real bug: `Message.id` is `UUID()`, assigned fresh on every construction (no
    /// memberwise override), so two `TextLocated<Message>` values built independently from identical content
    /// (as happens on every debounced re-analysis pass) never compare equal by `entity.id` even though they
    /// represent the same logical diagnostic. Using `entity.id` as `List`'s row identity made SwiftUI treat
    /// every row as freshly inserted on every edit, severe enough on macOS to repeatedly rebuild the backing
    /// `NSTableView` while typing and steal first responder from the editor. `rowIdentity` must be derived
    /// from content instead, so two independently-constructed-but-equivalent diagnostics match.
    func testRowIdentityIsStableAcrossIndependentlyConstructedEquivalentDiagnostics() {
        let first = Self.diagnostic(line: 2, column: 3, summary: "Expected: HARK!")
        let second = Self.diagnostic(line: 2, column: 3, summary: "Expected: HARK!")

        XCTAssertNotEqual(first.entity.id, second.entity.id, "Message.id is randomised per construction")
        XCTAssertEqual(first.rowIdentity, second.rowIdentity)
    }

    /// Builds a `TextLocated<Message>` fixture at a given 0-indexed line/column.
    private static func diagnostic(line: Int, column: Int, summary: String) -> TextLocated<Message> {
        let location = TextLocation(zeroBasedLine: line, column: column)
        let message = Message(category: .error, length: 1, summary: summary, description: nil)
        return TextLocated(location: location, entity: message)
    }
}
