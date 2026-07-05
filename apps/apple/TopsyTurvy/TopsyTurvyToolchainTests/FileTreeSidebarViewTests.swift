import XCTest
@testable import Theatre

/// Tests for `FileTreeSidebarView.node(at:in:)`, the selection-to-node lookup logic.
final class FileTreeSidebarViewTests: XCTestCase {
    /// Tests that a top-level node is found by its URL.
    func testNodeFindsTopLevelFile() {
        let fileURL = URL(fileURLWithPath: "/workspace/main.topsy")
        let nodes = [WorkspaceFileNode(url: fileURL, isDirectory: false, children: [])]

        let found = FileTreeSidebarView.node(at: fileURL, in: nodes)

        XCTAssertEqual(found?.url, fileURL)
    }

    /// Tests that a node nested inside a subfolder is found by searching recursively.
    func testNodeFindsNestedFile() {
        let nestedURL = URL(fileURLWithPath: "/workspace/sub/nested.topsy")
        let nested = WorkspaceFileNode(url: nestedURL, isDirectory: false, children: [])
        let folder = WorkspaceFileNode(url: URL(fileURLWithPath: "/workspace/sub"), isDirectory: true, children: [nested])

        let found = FileTreeSidebarView.node(at: nestedURL, in: [folder])

        XCTAssertEqual(found?.url, nestedURL)
    }

    /// Tests that a URL absent from the tree returns `nil`.
    func testNodeReturnsNilForUnknownURL() {
        let nodes = [WorkspaceFileNode(url: URL(fileURLWithPath: "/workspace/main.topsy"), isDirectory: false, children: [])]

        let found = FileTreeSidebarView.node(at: URL(fileURLWithPath: "/workspace/missing.topsy"), in: nodes)

        XCTAssertNil(found)
    }
}
