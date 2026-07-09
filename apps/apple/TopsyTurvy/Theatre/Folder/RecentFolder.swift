import Foundation

/// A recently opened folder.
struct RecentFolder: Codable, Identifiable, Hashable {
    /// The entry ID, independent of underlying bookmark data.
    let id: UUID

    /// The folder display name, captured when creating the bookmark.
    let displayName: String

    /// The security-scoped bookmark data for the folder.
    let bookmarkData: Data

    /// The timestamp the folder was last opened, for ordering of recent items.
    var lastOpened: Date
}
