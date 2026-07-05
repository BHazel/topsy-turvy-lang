/// The active single-file document window's "open a folder" actions, exposed to menu bar commands via
/// `TheatreActiveScene` (not `FocusedValue` — see that type's doc comment for why) — separate from
/// `TheatreDocumentActions` since only a single-file window (not an already-open workspace) offers this.
struct TheatreFolderActions {
    /// Presents the folder picker.
    let openFolder: () -> Void

    /// Recently opened workspace folders, most recently opened first.
    let recents: [WorkspaceBookmarkEntry]

    /// Reopens a recent workspace folder directly, bypassing the picker.
    let openRecent: (WorkspaceBookmarkEntry) -> Void
}
