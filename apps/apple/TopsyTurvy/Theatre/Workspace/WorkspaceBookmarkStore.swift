import Foundation

/// Persists recently opened workspace folders as security-scoped bookmarks in `UserDefaults`, so they can be
/// reopened across relaunches without re-prompting the user via the document picker.
final class WorkspaceBookmarkStore {
    private static let defaultsKey = "workspaceRecents"

    private let defaults: UserDefaults

    /// Creates a store backed by the given defaults, `.standard` unless overridden for testing.
    /// - Parameter defaults: The `UserDefaults` suite to persist recents in.
    init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
    }

    /// The stored recents, most recently opened first.
    var recents: [WorkspaceBookmarkEntry] {
        load().sorted { $0.lastOpened > $1.lastOpened }
    }

    /// Creates a security-scoped bookmark for `url` (which must currently have picker-granted access) and
    /// stores it as a new recent entry.
    /// - Parameter url: The folder URL returned by the document picker.
    /// - Returns: The newly stored entry.
    @discardableResult
    func addRecent(for url: URL) throws -> WorkspaceBookmarkEntry {
        let bookmarkData = try url.bookmarkData()
        let entry = WorkspaceBookmarkEntry(
            id: UUID(),
            displayName: url.lastPathComponent,
            bookmarkData: bookmarkData,
            lastOpened: Date()
        )
        save(load() + [entry])
        return entry
    }

    /// Resolves a stored entry back into a URL. The caller is responsible for the security-scoped access
    /// lifecycle (`WorkspaceModel.startAccess()`/`stopAccess()` do this).
    /// - Parameter entry: The entry to resolve.
    /// - Returns: The resolved URL, or `nil` if the bookmark can no longer be resolved.
    func resolve(_ entry: WorkspaceBookmarkEntry) -> URL? {
        var isStale = false
        return try? URL(resolvingBookmarkData: entry.bookmarkData, bookmarkDataIsStale: &isStale)
    }

    /// Updates an entry's `lastOpened` timestamp to now, moving it to the front of `recents`.
    /// - Parameter entry: The entry to touch.
    func touch(_ entry: WorkspaceBookmarkEntry) {
        var entries = load()
        guard let index = entries.firstIndex(where: { $0.id == entry.id }) else { return }
        entries[index].lastOpened = Date()
        save(entries)
    }

    /// Removes a stored entry, for example when its bookmark can no longer be resolved.
    /// - Parameter entry: The entry to remove.
    func remove(_ entry: WorkspaceBookmarkEntry) {
        save(load().filter { $0.id != entry.id })
    }

    private func load() -> [WorkspaceBookmarkEntry] {
        guard let data = defaults.data(forKey: Self.defaultsKey) else { return [] }
        return (try? JSONDecoder().decode([WorkspaceBookmarkEntry].self, from: data)) ?? []
    }

    private func save(_ entries: [WorkspaceBookmarkEntry]) {
        guard let data = try? JSONEncoder().encode(entries) else { return }
        defaults.set(data, forKey: Self.defaultsKey)
    }
}
