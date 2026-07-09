import SwiftUI
import UIKit

/// A toolbar button for opening a folder of code files.
///
/// This presents a dedicated `FolderPickerView` to select a folder to open in a `FolderBrowserView` as a full-screen
/// modal over the `SingleFileEditorHostView`.  A bookmark is created for the folder so it can be accessed again in
/// recent items.
struct OpenFolderButton: View {
    /// A value indicating whether the folder picker sheet is presented.
    @State private var isPickerPresented = false

    /// The store of recently opened folders.
    @State private var recentFolderStore = RecentFolderStore()

    /// The folder currently presented in the full-screen browser, or `nil` if none is open.
    @State private var selectedFolder: RecentFolder?

    /// The token for `TheatreActiveScene` publishing from this instance.
    private let activeSceneToken = UUID()

    /// The scene phase, from the environment.
    @Environment(\.scenePhase) private var scenePhase

    /// The view body.
    var body: some View {
        Button {
            self.isPickerPresented = true
        } label: {
            Label("Open Programme Folder…", systemImage: "folder")
        }
        .sheet(isPresented: self.$isPickerPresented) {
            FolderPickerView { url in
                self.isPickerPresented = false
                self.openFolder(for: url)
            }
        }
        .fullScreenCover(item: self.$selectedFolder) { entry in
            FolderBrowserView(currentFolder: entry)
        }
        .accessibilityIdentifier("OpenFolderButton")
        .onAppear {
            self.publishActiveFolderActions()
        }
        .onDisappear {
            TheatreActiveScene.shared.clearFolderActions(token: self.activeSceneToken)
        }
        .onChange(of: self.scenePhase) { _, newPhase in
            if newPhase == .active {
                self.publishActiveFolderActions()
            }
        }
    }

    /// Publishes this button actions to `TheatreActiveScene` so the menu bar and hardware-keyboard
    /// shortcuts reach whichever single-file document window is currently active.
    private func publishActiveFolderActions() {
        TheatreActiveScene.shared.publishFolderActions(
            TheatreFolderActions(
                openFolder: {
                    self.isPickerPresented = true
                },
                recents: self.recentFolderStore.recents,
                openRecent: {
                    self.selectedFolder = $0
                }
            ),
            token: self.activeSceneToken
        )
    }

    /// Adds the folder to the recents, creates a security-scoped bookmark for it and presents the folder browser cover onto it.
    ///
    /// The `UIDocumentPickerViewController` URL requires `startAccessingSecurityScopedResource()` before any
    /// operation on it, including `bookmarkData()`.  Without it, bookmark creation fails silently.  Access is
    /// only needed transiently here to create the bookmark; `TheatreFolder` starts its own access on the
    /// URL it later resolves from that bookmark.
    ///
    /// - Parameters:
    ///   - url: The folder URL.
    private func openFolder(for url: URL) {
        guard url.startAccessingSecurityScopedResource() else {
            return
        }
        
        defer {
            url.stopAccessingSecurityScopedResource()
        }

        guard let entry = try? self.recentFolderStore.addRecentFolder(for: url) else {
            return
        }
        
        self.selectedFolder = entry
    }
}
