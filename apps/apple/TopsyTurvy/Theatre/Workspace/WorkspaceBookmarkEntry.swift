import Foundation

/// A persisted record of a previously opened workspace folder, resolvable back into a security-scoped URL
/// via `WorkspaceBookmarkStore.resolve(_:)`.
struct WorkspaceBookmarkEntry: Codable, Identifiable, Hashable {
    /// This entry's stable identity, independent of the underlying bookmark data.
    let id: UUID

    /// The folder's display name, captured at bookmark-creation time.
    let displayName: String

    /// The security-scoped bookmark data for the folder.
    let bookmarkData: Data

    /// When this workspace was last opened, used to order recents.
    var lastOpened: Date
}
