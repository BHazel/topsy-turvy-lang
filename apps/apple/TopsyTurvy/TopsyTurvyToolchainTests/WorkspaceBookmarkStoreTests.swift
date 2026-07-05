import XCTest
@testable import Theatre

/// Tests for `WorkspaceBookmarkStore`, the workspace recents persistence layer.
final class WorkspaceBookmarkStoreTests: XCTestCase {
    private var defaults: UserDefaults!
    private var suiteName: String!
    private var store: WorkspaceBookmarkStore!
    private var folderURL: URL!

    override func setUpWithError() throws {
        try super.setUpWithError()
        suiteName = "WorkspaceBookmarkStoreTests-\(UUID().uuidString)"
        defaults = try XCTUnwrap(UserDefaults(suiteName: suiteName))
        store = WorkspaceBookmarkStore(defaults: defaults)

        folderURL = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: folderURL, withIntermediateDirectories: true)
    }

    override func tearDownWithError() throws {
        try FileManager.default.removeItem(at: folderURL)
        defaults.removePersistentDomain(forName: suiteName)
        try super.tearDownWithError()
    }

    /// Tests that adding a recent stores an entry retrievable via `recents`, with the folder's name captured.
    func testAddRecentStoresRetrievableEntry() throws {
        let entry = try store.addRecent(for: folderURL)

        XCTAssertEqual(store.recents.map(\.id), [entry.id])
        XCTAssertEqual(entry.displayName, folderURL.lastPathComponent)
    }

    /// Tests that `resolve(_:)` recovers a URL pointing at the same folder that was bookmarked.
    func testResolveRecoversTheBookmarkedURL() throws {
        let entry = try store.addRecent(for: folderURL)

        let resolved = store.resolve(entry)

        XCTAssertEqual(resolved?.standardizedFileURL.path, folderURL.standardizedFileURL.path)
    }

    /// Tests that `recents` orders entries most-recently-opened first.
    func testRecentsOrdersMostRecentlyOpenedFirst() throws {
        let older = try store.addRecent(for: folderURL)
        let newerFolder = folderURL.appendingPathComponent("nested", isDirectory: true)
        try FileManager.default.createDirectory(at: newerFolder, withIntermediateDirectories: true)
        let newer = try store.addRecent(for: newerFolder)
        store.touch(newer)

        XCTAssertEqual(store.recents.first?.id, newer.id)
        XCTAssertEqual(store.recents.last?.id, older.id)
    }

    /// Tests that `touch(_:)` moves a previously-older entry to the front.
    func testTouchMovesEntryToFront() throws {
        let first = try store.addRecent(for: folderURL)
        let secondFolder = folderURL.appendingPathComponent("nested", isDirectory: true)
        try FileManager.default.createDirectory(at: secondFolder, withIntermediateDirectories: true)
        _ = try store.addRecent(for: secondFolder)

        store.touch(first)

        XCTAssertEqual(store.recents.first?.id, first.id)
    }

    /// Tests that `remove(_:)` deletes the entry from `recents`.
    func testRemoveDeletesEntry() throws {
        let entry = try store.addRecent(for: folderURL)

        store.remove(entry)

        XCTAssertTrue(store.recents.isEmpty)
    }
}
