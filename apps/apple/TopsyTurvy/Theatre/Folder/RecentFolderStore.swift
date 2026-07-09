import Foundation

/// Manages recently opened folders for recent items.
///
/// Recently opened folders are stored as security-scoped bookmarks in `UserDefaults` so they can be
/// reopened across relaunches without re-prompting the user via the document picker.
final class RecentFolderStore {
    /// The `UserDefaults` key the recent folders are stored under.
    private static let defaultsKey = "recentFolders"

    /// The user defaults backing this store.
    private let defaults: UserDefaults

    /// Creates a store from user defaults.
    ///
    /// - Parameters:
    ///   - defaults: The user defaults, with the standard by default.
    init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
    }

    /// The recently opened folders, most recent first.
    var recents: [RecentFolder] {
        self.loadRecentFolders().sorted {
            $0.lastOpened > $1.lastOpened
        }
    }

    /// Adds a recent folder to the store.
    ///
    /// Creates a security-scoped entry for the provided folder URL which must currently have picker-granted
    /// access.
    ///
    /// - Parameters:
    ///   - url: The folder URL as returned from the folder picker.
    ///
    /// - Returns: The added recent folder entry.
    @discardableResult
    func addRecentFolder(for url: URL) throws -> RecentFolder {
        let bookmarkData = try url.bookmarkData()
        let recentFolder = RecentFolder(
            id: UUID(),
            displayName: url.lastPathComponent,
            bookmarkData: bookmarkData,
            lastOpened: Date()
        )

        self.saveRecentFolders(self.loadRecentFolders() + [recentFolder])
        return recentFolder
    }

    /// Resolves the URL for a recent folder.
    ///
    /// The caller is responsible for the security-scoped access lifecycle by calling the
    /// `TheatreFolder.startAccess()` and `TheatreFolder.stopAccess()` methods.
    ///
    /// - Parameters:
    ///   - recentFolder: The recent folder.
    ///
    /// - Returns: The URL of the recent folder, or `nil` if it cannot be resolved.
    func resolveRecentFolderURL(_ recentFolder: RecentFolder) -> URL? {
        var isStale = false
        return try? URL(resolvingBookmarkData: recentFolder.bookmarkData, bookmarkDataIsStale: &isStale)
    }

    /// Updates the recent folder including the last opened timestamp to the current date and time.
    ///
    /// - Parameters:
    ///   - recentFolder: The recent folder.
    func updateRecentFolder(_ recentFolder: RecentFolder) {
        var recentFolders = self.loadRecentFolders()
        guard let recentFolderIndex = recentFolders.firstIndex(where: { $0.id == recentFolder.id }) else {
            return
        }

        recentFolders[recentFolderIndex].lastOpened = Date()
        self.saveRecentFolders(recentFolders)
    }

    /// Removes a recent folder.
    ///
    /// - Parameters:
    ///   - recentFolder: The recent folder to remove.
    func removeRecentFolder(_ recentFolder: RecentFolder) {
        self.saveRecentFolders(self.loadRecentFolders().filter {
            $0.id != recentFolder.id
        })
    }

    /// Loads recent folders from user defaults.
    ///
    /// - Returns: The recent folders, otherwise an empty array.
    private func loadRecentFolders() -> [RecentFolder] {
        guard let data = self.defaults.data(forKey: Self.defaultsKey) else {
            return []
        }

        return (try? JSONDecoder().decode([RecentFolder].self, from: data)) ?? []
    }

    /// Saves recent folders to user defaults.
    ///
    /// - Parameters:
    ///   - recentFolders: The recent folders, including all existing and new folders to add.
    private func saveRecentFolders(_ recentFolders: [RecentFolder]) {
        guard let data = try? JSONEncoder().encode(recentFolders) else {
            return
        }

        self.defaults.set(data, forKey: Self.defaultsKey)
    }
}
