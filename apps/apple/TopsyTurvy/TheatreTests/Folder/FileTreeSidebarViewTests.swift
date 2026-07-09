import XCTest
@testable import Theatre

/// Tests for the `FileTreeSidebarView`.
final class FileTreeSidebarViewTests: XCTestCase {
    /// Tests that a top-level node is found by its URL.
    func testNodeFindsTopLevelFile() {
        let fileURL = URL(fileURLWithPath: "/workspace/main.topsy")
        let nodes = [FileNode(url: fileURL, isDirectory: false, children: [])]

        let found = FileTreeSidebarView.node(at: fileURL, in: nodes)

        XCTAssertEqual(found?.url, fileURL)
    }

    /// Tests that a node nested inside a subfolder is found by searching recursively.
    func testNodeFindsNestedFile() {
        let nestedURL = URL(fileURLWithPath: "/workspace/sub/nested.topsy")
        let nested = FileNode(url: nestedURL, isDirectory: false, children: [])
        let folder = FileNode(url: URL(fileURLWithPath: "/workspace/sub"), isDirectory: true, children: [nested])

        let found = FileTreeSidebarView.node(at: nestedURL, in: [folder])

        XCTAssertEqual(found?.url, nestedURL)
    }

    /// Tests that a URL absent from the tree returns `nil`.
    func testNodeReturnsNilForUnknownURL() {
        let nodes = [FileNode(url: URL(fileURLWithPath: "/workspace/main.topsy"), isDirectory: false, children: [])]

        let found = FileTreeSidebarView.node(at: URL(fileURLWithPath: "/workspace/missing.topsy"), in: nodes)

        XCTAssertNil(found)
    }
}
