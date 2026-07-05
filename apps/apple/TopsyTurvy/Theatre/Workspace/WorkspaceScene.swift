import SwiftUI

/// The root content of a workspace, presented as a `.fullScreenCover` over the document it was opened from
/// (see `OpenFolderButton`): a file-tree sidebar over a bookmarked folder, with the selected file's editing
/// surface (`WorkspaceEditorHostView`) as the detail column.
struct WorkspaceScene: View {
    let entry: WorkspaceBookmarkEntry

    @State private var workspaceModel: WorkspaceModel?
    @State private var selectedFileURL: URL?
    @State private var bookmarkStore = WorkspaceBookmarkStore()
    @State private var columnVisibility: NavigationSplitViewVisibility = .all
    @Environment(\.dismiss) private var dismiss

    // Owned once for the whole workspace, not per file: `TopsyTurvySession` is a reusable engine handle (its
    // `execute`/`analyse`/`format` calls all take `source` as a parameter — nothing ties a session to one
    // file). A previous per-file design created a fresh session in `WorkspaceEditorHostView`, torn down and
    // recreated via `.id(selectedFileURL)` on every switch; the old session's teardown was a fire-and-forget
    // `Task { await session.close() }`, unsynchronised with the new session's `open()` starting almost
    // simultaneously — a race that left the native engine in a state where every subsequent run failed with
    // "An unexpected error occurred.", for every file, for the rest of that app run. Reusing one session and
    // only re-registering the import resolver per file removes the race entirely.
    @State private var session = TopsyTurvySession()

    var body: some View {
        NavigationSplitView(columnVisibility: $columnVisibility) {
            // No back button: the file tree expands in place via disclosure triangles (`OutlineGroup`), not
            // push-navigation, so there's never anything for a back chevron to return from. On compact width
            // (iPhone) this is also the first screen shown, where a bare "<" would be an ambiguous stand-in
            // for dismissing the whole cover — the explicit "Close" button below replaces it.
            sidebar
                .navigationBarBackButtonHidden(true)
                .toolbar {
                    closeButton
                }
        } detail: {
            // The close button is duplicated here rather than shared with the sidebar's via some higher
            // attachment point: a button placed only on the sidebar's own toolbar disappears along with the
            // sidebar when it collapses on regular width — the same mistake already fixed once for the
            // sidebar-reopen toggle — so both toolbars carry their own copy instead.
            detail
                .toolbar {
                    closeButton
                }
        }
        .toolbarRole(.automatic)
        .task {
            await session.open()
            openWorkspace()
        }
        .onDisappear {
            workspaceModel?.stopAccess()
            Task { await session.close() }
        }
    }

    @ViewBuilder
    private var sidebar: some View {
        if let workspaceModel {
            FileTreeSidebarView(nodes: workspaceModel.fileTree, selectedFileURL: $selectedFileURL) { node in
                selectedFileURL = node.url
            }
            .navigationTitle(workspaceModel.rootURL.lastPathComponent)
        } else {
            ContentUnavailableView("Folder Unavailable", systemImage: "folder.badge.questionmark")
        }
    }

    // Binding `columnVisibility` on `NavigationSplitView` is sufficient for its own automatic sidebar-toggle
    // button to appear reliably and migrate correctly between the sidebar's and detail's toolbars as the
    // column collapses/expands — no explicit toggle button is needed alongside it. This is distinct from the
    // back button hidden above: the toggle re-expands a collapsed sidebar and must stay.
    @ViewBuilder
    private var detail: some View {
        if let selectedFileURL {
            WorkspaceEditorHostView(fileURL: selectedFileURL, session: session)
                .id(selectedFileURL)
        } else {
            ContentUnavailableView("Select a File", systemImage: "doc.text")
        }
    }

    private var closeButton: ToolbarItem<(), Button<Text>> {
        ToolbarItem(placement: .topBarTrailing) {
            Button("Close") {
                dismiss()
            }
        }
    }

    /// Resolves `entry`'s bookmark, starts security-scoped access to the resulting folder, and builds its
    /// file tree.
    private func openWorkspace() {
        guard workspaceModel == nil else { return }
        guard let url = bookmarkStore.resolve(entry) else { return }

        let model = WorkspaceModel(rootURL: url)
        model.startAccess()
        workspaceModel = model
        bookmarkStore.touch(entry)
    }
}
