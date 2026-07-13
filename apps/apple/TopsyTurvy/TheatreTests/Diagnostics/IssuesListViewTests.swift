import LanguageSupport
import XCTest
@testable import Theatre

/// Tests for the `IssuesListView`.
final class IssuesListViewTests: XCTestCase {
    /// Tests that diagnostics are ordered top-to-bottom, left-to-right, regardless of `Set` iteration order.
    func testSortedOrdersTopToBottomLeftToRight() {
        let third = Self.createDiagnostic(line: 5, column: 1, summary: "third")
        let first = Self.createDiagnostic(line: 1, column: 1, summary: "first")
        let second = Self.createDiagnostic(line: 1, column: 4, summary: "second")

        let sorted = IssuesListView.sorted([third, first, second])

        XCTAssertEqual(sorted.map(\.entity.summary), ["first", "second", "third"])
    }

    /// Tests that an empty set sorts to an empty array.
    func testSortedWithEmptySetReturnsEmptyArray() {
        XCTAssertTrue(IssuesListView.sorted([]).isEmpty)
    }

    /// Tests that `rowIdentity` compares equal for two independently constructed but equivalent diagnostics,
    /// since `Message.id` is a fresh `UUID()` each time and never matches by `entity.id` alone.
    func testRowIdentityIsStableAcrossIndependentlyConstructedEquivalentDiagnostics() {
        let first = Self.createDiagnostic(line: 2, column: 3, summary: "Expected: HARK!")
        let second = Self.createDiagnostic(line: 2, column: 3, summary: "Expected: HARK!")

        XCTAssertNotEqual(first.entity.id, second.entity.id, "Message.id is randomised per construction")
        XCTAssertEqual(first.rowIdentity, second.rowIdentity)
    }

    /// Builds a `TextLocated<Message>` fixture at a given 0-indexed line/column.
    ///
    /// - Parameters:
    ///   - line: The 0-indexed line.
    ///   - column: The 0-indexed column.
    ///   - summary: The diagnostic summary.
    ///
    /// - Returns: A diagnostic.
    private static func createDiagnostic(line: Int, column: Int, summary: String) -> TextLocated<Message> {
        let location = TextLocation(zeroBasedLine: line, column: column)
        let message = Message(category: .error, length: 1, summary: summary, description: nil)
        return TextLocated(location: location, entity: message)
    }
}
