import XCTest
@testable import Theatre

/// Tests for `WorkspaceFileTree.build(at:)`, the workspace sidebar's file-tree enumeration.
final class WorkspaceFileTreeTests: XCTestCase {
    private var rootURL: URL!

    override func setUpWithError() throws {
        try super.setUpWithError()
        rootURL = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: rootURL, withIntermediateDirectories: true)
    }

    override func tearDownWithError() throws {
        try FileManager.default.removeItem(at: rootURL)
        try super.tearDownWithError()
    }

    /// Tests that directories sort before files at the same level, regardless of name.
    func testBuildOrdersDirectoriesBeforeFiles() throws {
        try write("z.topsy", in: rootURL)
        try makeDirectory("a-folder", in: rootURL)

        let tree = WorkspaceFileTree.build(at: rootURL)

        XCTAssertEqual(tree.map(\.name), ["a-folder", "z.topsy"])
    }

    /// Tests that entries at the same directory-ness level sort alphabetically by localized name.
    func testBuildSortsAlphabeticallyWithinTheSameKind() throws {
        try write("banana.topsy", in: rootURL)
        try write("apple.topsy", in: rootURL)

        let tree = WorkspaceFileTree.build(at: rootURL)

        XCTAssertEqual(tree.map(\.name), ["apple.topsy", "banana.topsy"])
    }

    /// Tests that a `.topsy` file is identified as such, and a non-`.topsy` file is not.
    func testBuildIdentifiesTopsyFilesByExtension() throws {
        try write("programme.topsy", in: rootURL)
        try write("notes.txt", in: rootURL)

        let tree = WorkspaceFileTree.build(at: rootURL)

        let topsyNode = try XCTUnwrap(tree.first { $0.name == "programme.topsy" })
        let textNode = try XCTUnwrap(tree.first { $0.name == "notes.txt" })
        XCTAssertTrue(topsyNode.isTopsyFile)
        XCTAssertFalse(textNode.isTopsyFile)
    }

    /// Tests that subfolders are enumerated recursively, populating `children`.
    func testBuildRecursesIntoSubfolders() throws {
        let subfolder = try makeDirectory("sub", in: rootURL)
        try write("nested.topsy", in: subfolder)

        let tree = WorkspaceFileTree.build(at: rootURL)

        let subNode = try XCTUnwrap(tree.first { $0.name == "sub" })
        XCTAssertEqual(subNode.children.map(\.name), ["nested.topsy"])
    }

    /// Tests that hidden files (dotfiles) are skipped.
    func testBuildSkipsHiddenFiles() throws {
        try write(".hidden", in: rootURL)
        try write("visible.topsy", in: rootURL)

        let tree = WorkspaceFileTree.build(at: rootURL)

        XCTAssertEqual(tree.map(\.name), ["visible.topsy"])
    }

    /// Tests that an empty folder produces an empty tree.
    func testBuildOnEmptyFolderReturnsEmptyArray() {
        XCTAssertTrue(WorkspaceFileTree.build(at: rootURL).isEmpty)
    }

    @discardableResult
    private func write(_ name: String, in directory: URL) throws -> URL {
        let url = directory.appendingPathComponent(name)
        try Data().write(to: url)
        return url
    }

    @discardableResult
    private func makeDirectory(_ name: String, in directory: URL) throws -> URL {
        let url = directory.appendingPathComponent(name, isDirectory: true)
        try FileManager.default.createDirectory(at: url, withIntermediateDirectories: true)
        return url
    }
}
