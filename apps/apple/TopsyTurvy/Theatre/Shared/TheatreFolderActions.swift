/// The "open a folder" actions of the active single-file document window, exposed to menu bar commands via
/// `TheatreActiveScene`, separate from `TheatreDocumentActions` since only a single-file view
/// (not an already-open folder) offers this.
///
/// Actions available for opening folders.
struct TheatreFolderActions {
    /// Presents the folder picker.
    let openFolder: () -> Void

    /// The recently opened folders, most recent first.
    let recents: [RecentFolder]

    /// Reopens a recent folder, bypassing the picker.
    let openRecent: (RecentFolder) -> Void
}
