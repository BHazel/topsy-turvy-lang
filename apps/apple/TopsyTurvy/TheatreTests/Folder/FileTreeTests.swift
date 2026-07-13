import XCTest
@testable import Theatre

/// Tests for the `FileTree`.
final class FileTreeTests: XCTestCase {
    private var rootURL: URL!

    /// Sets up the fixture with error.
    override func setUpWithError() throws {
        try super.setUpWithError()
        self.rootURL = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: self.rootURL, withIntermediateDirectories: true)
    }

    /// Tears down the fixture with error.
    override func tearDownWithError() throws {
        try FileManager.default.removeItem(at: self.rootURL)
        try super.tearDownWithError()
    }

    /// Tests that directories sort before files at the same level, regardless of name.
    func testBuildOrdersDirectoriesBeforeFiles() throws {
        try write("z.topsy", in: self.rootURL)
        try makeDirectory("a-folder", in: self.rootURL)

        let tree = FileTree.build(at: self.rootURL)

        XCTAssertEqual(tree.map(\.name), ["a-folder", "z.topsy"])
    }

    /// Tests that entries at the same directory level sort alphabetically by localised name.
    func testBuildSortsAlphabeticallyWithinTheSameKind() throws {
        try write("banana.topsy", in: self.rootURL)
        try write("apple.topsy", in: self.rootURL)

        let tree = FileTree.build(at: self.rootURL)

        XCTAssertEqual(tree.map(\.name), ["apple.topsy", "banana.topsy"])
    }

    /// Tests that a Topsy Turvy `.topsy` file is identified as such, and others are not.
    func testBuildIdentifiesTopsyFilesByExtension() throws {
        try write("programme.topsy", in: self.rootURL)
        try write("notes.txt", in: self.rootURL)

        let tree = FileTree.build(at: self.rootURL)

        let topsyNode = try XCTUnwrap(tree.first { $0.name == "programme.topsy" })
        let textNode = try XCTUnwrap(tree.first { $0.name == "notes.txt" })
        XCTAssertTrue(topsyNode.isTopsyFile)
        XCTAssertFalse(textNode.isTopsyFile)
    }

    /// Tests that subfolders are enumerated recursively, populating `children`.
    func testBuildRecursesIntoSubfolders() throws {
        let subfolder = try makeDirectory("sub", in: self.rootURL)
        try write("nested.topsy", in: subfolder)

        let tree = FileTree.build(at: self.rootURL)

        let subNode = try XCTUnwrap(tree.first { $0.name == "sub" })
        XCTAssertEqual(subNode.children.map(\.name), ["nested.topsy"])
    }

    /// Tests that hidden files (dotfiles) are skipped.
    func testBuildSkipsHiddenFiles() throws {
        try write(".hidden", in: self.rootURL)
        try write("visible.topsy", in: self.rootURL)

        let tree = FileTree.build(at: self.rootURL)

        XCTAssertEqual(tree.map(\.name), ["visible.topsy"])
    }

    /// Tests that an empty folder produces an empty tree.
    func testBuildOnEmptyFolderReturnsEmptyArray() {
        XCTAssertTrue(FileTree.build(at: self.rootURL).isEmpty)
    }

    /// Writes a file returning its URL.
    ///
    /// - Parameters:
    ///   - name: The file name.
    ///   - directory: The directory to save the file in.
    ///
    /// - Returns: The URL of the saved file.
    @discardableResult
    private func write(_ name: String, in directory: URL) throws -> URL {
        let url = directory.appendingPathComponent(name)
        try Data().write(to: url)
        return url
    }

    /// Creates a directory returning its URL.
    ///
    /// - Parameters:
    ///   - name: The directory name.
    ///   - directory: The parent directory to create the directory in.
    ///
    /// - Returns: The URL of the created directory.
    @discardableResult
    private func makeDirectory(_ name: String, in directory: URL) throws -> URL {
        let url = directory.appendingPathComponent(name, isDirectory: true)
        try FileManager.default.createDirectory(at: url, withIntermediateDirectories: true)
        return url
    }
}
