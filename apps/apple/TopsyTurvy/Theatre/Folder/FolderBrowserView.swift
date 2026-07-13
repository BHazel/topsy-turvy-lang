import SwiftUI

/// The view for working on a whole folder of Topsy Turvy files.
struct FolderBrowserView: View {
    /// The opened folder, once resolved from `currentFolder`.
    @State private var theatreFolder: TheatreFolder?

    /// The URL of the file currently selected in the sidebar.
    @State private var selectedFileURL: URL?

    /// The store of recently opened folders.
    @State private var recentFoldersStore = RecentFolderStore()

    /// The visibility of the sidebar and detail columns.
    @State private var columnVisibility: NavigationSplitViewVisibility = .all

    /// The currently open folder.
    let currentFolder: RecentFolder

    /// The dismiss action, from the environment.
    @Environment(\.dismiss) private var dismiss

    /// The session for interacting with the Topsy Turvy toolchain.
    @State private var session = TopsyTurvySession()

    /// The view body.
    var body: some View {
        NavigationSplitView(columnVisibility: self.$columnVisibility) {
            self.sidebar
                .navigationBarBackButtonHidden(true)
                .toolbar {
                    self.closeButton
                }
        } detail: {
            self.detail
                .toolbar {
                    self.closeButton
                }
        }
        .toolbarRole(.automatic)
        .task {
            await self.session.open()
            self.openFolder()
        }
        .onDisappear {
            self.theatreFolder?.stopAccess()
            Task {
                await self.session.close()
            }
        }
    }

    /// View builder for the sidebar view for `FolderBrowserView`.
    @ViewBuilder
    private var sidebar: some View {
        if let theatreFolder {
            FileTreeSidebarView(nodes: theatreFolder.fileTree, selectedFileURL: self.$selectedFileURL) { node in
                self.selectedFileURL = node.url
            }
            .navigationTitle(theatreFolder.rootURL.lastPathComponent)
        } else {
            ContentUnavailableView("Folder Unavailable", systemImage: "folder.badge.questionmark")
        }
    }

    /// Builds the detail view for `FolderBrowserView`.
    @ViewBuilder
    private var detail: some View {
        if let selectedFileURL {
            FolderFileEditorView(fileURL: selectedFileURL, session: self.session)
                .id(selectedFileURL)
        } else {
            ContentUnavailableView("Select a File", systemImage: "doc.text")
        }
    }

    /// The close button view used in `FolderBrowserView`.
    ///
    /// - Returns: The close button toolbar item.
    private var closeButton: ToolbarItem<(), Button<Text>> {
        ToolbarItem(placement: .topBarTrailing) {
            Button("Close") {
                self.dismiss()
            }
        }
    }

    /// Opens the folder retrieved from recent items.
    ///
    /// This demonstrates how callers are responsible for requesting secuity-scoped access by calling
    /// `TheatreFolder.startAccess()`.
    private func openFolder() {
        guard self.theatreFolder == nil else {
            return
        }

        guard let url = self.recentFoldersStore.resolveRecentFolderURL(self.currentFolder) else {
            return
        }

        let folder = TheatreFolder(rootURL: url)
        folder.startAccess()
        self.theatreFolder = folder
        self.recentFoldersStore.updateRecentFolder(self.currentFolder)
    }
}
