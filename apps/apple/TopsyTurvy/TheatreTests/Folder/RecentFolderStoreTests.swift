import XCTest
@testable import Theatre

/// Tests for the `RecentFolderStore`.
final class RecentFolderStoreTests: XCTestCase {
    private var userDefaults: UserDefaults!
    private var suiteName: String!
    private var recentFolderStore: RecentFolderStore!
    private var folderURL: URL!

    /// Sets up the fixture with error.
    override func setUpWithError() throws {
        try super.setUpWithError()
        self.suiteName = "RecentFolderStoreTests-\(UUID().uuidString)"
        self.userDefaults = try XCTUnwrap(UserDefaults(suiteName: self.suiteName))
        self.recentFolderStore = RecentFolderStore(defaults: self.userDefaults)

        self.folderURL = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: self.folderURL, withIntermediateDirectories: true)
    }

    /// Tears down the fixture with error.
    override func tearDownWithError() throws {
        try FileManager.default.removeItem(at: self.folderURL)
        self.userDefaults.removePersistentDomain(forName: self.suiteName)
        try super.tearDownWithError()
    }

    /// Tests that adding a recent stores an entry retrievable via `recents`, with the folder name captured.
    func testAddRecentStoresRetrievableEntry() throws {
        let recentFolder = try self.recentFolderStore.addRecentFolder(for: self.folderURL)

        XCTAssertEqual(self.recentFolderStore.recents.map(\.id), [recentFolder.id])
        XCTAssertEqual(recentFolder.displayName, self.folderURL.lastPathComponent)
    }

    /// Tests that `resolve(_:)` recovers a URL pointing at the same folder that was bookmarked.
    func testResolveRecoversTheBookmarkedURL() throws {
        let recentFolder = try self.recentFolderStore.addRecentFolder(for: self.folderURL)

        let resolved = self.recentFolderStore.resolveRecentFolderURL(recentFolder)

        XCTAssertEqual(resolved?.standardizedFileURL.path, self.folderURL.standardizedFileURL.path)
    }

    /// Tests that `recents` orders entries most-recently-opened first.
    func testRecentsOrdersMostRecentlyOpenedFirst() throws {
        let olderFolder = try self.recentFolderStore.addRecentFolder(for: self.folderURL)
        let newerFolder = self.folderURL.appendingPathComponent("nested", isDirectory: true)
        try FileManager.default.createDirectory(at: newerFolder, withIntermediateDirectories: true)
        let newer = try self.recentFolderStore.addRecentFolder(for: newerFolder)
        self.recentFolderStore.updateRecentFolder(newer)

        XCTAssertEqual(self.recentFolderStore.recents.first?.id, newer.id)
        XCTAssertEqual(self.recentFolderStore.recents.last?.id, olderFolder.id)
    }

    /// Tests that `touch(_:)` moves a previously-older entry to the front.
    func testTouchMovesEntryToFront() throws {
        let firstFolder = try self.recentFolderStore.addRecentFolder(for: self.folderURL)
        let secondFolder = self.folderURL.appendingPathComponent("nested", isDirectory: true)
        try FileManager.default.createDirectory(at: secondFolder, withIntermediateDirectories: true)
        _ = try self.recentFolderStore.addRecentFolder(for: secondFolder)

        self.recentFolderStore.updateRecentFolder(firstFolder)

        XCTAssertEqual(self.recentFolderStore.recents.first?.id, firstFolder.id)
    }

    /// Tests that `remove(_:)` deletes the entry from `recents`.
    func testRemoveDeletesEntry() throws {
        let entry = try self.recentFolderStore.addRecentFolder(for: self.folderURL)

        self.recentFolderStore.removeRecentFolder(entry)

        XCTAssertTrue(self.recentFolderStore.recents.isEmpty)
    }
}
