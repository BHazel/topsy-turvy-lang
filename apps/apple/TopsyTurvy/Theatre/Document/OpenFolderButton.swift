import SwiftUI
import UIKit

/// Wraps `UIDocumentPickerViewController` in directory-open mode, for picking a programme folder on
/// iOS/iPadOS.
private struct FolderPickerView: UIViewControllerRepresentable {
    var onPick: (URL) -> Void

    func makeUIViewController(context: Context) -> UIDocumentPickerViewController {
        let picker = UIDocumentPickerViewController(forOpeningContentTypes: [.folder])
        picker.delegate = context.coordinator
        return picker
    }

    func updateUIViewController(_ uiViewController: UIDocumentPickerViewController, context: Context) {}

    func makeCoordinator() -> Coordinator { Coordinator(onPick: onPick) }

    final class Coordinator: NSObject, UIDocumentPickerDelegate {
        let onPick: (URL) -> Void

        init(onPick: @escaping (URL) -> Void) {
            self.onPick = onPick
        }

        func documentPicker(_ controller: UIDocumentPickerViewController, didPickDocumentsAt urls: [URL]) {
            if let url = urls.first {
                onPick(url)
            }
        }
    }
}

/// A toolbar button that opens a folder picker for multi-file `PRAY ADMIT` programmes, via
/// `UIDocumentPickerViewController`, then bookmarks the picked folder and presents `WorkspaceScene` as a
/// full-screen cover onto it. A cover, not a second scene: an earlier `WindowGroup`-based attempt had no
/// reliable way back on iPhone (dismissing it could exit the app entirely, since the workspace and
/// single-file scenes had no relationship for iOS to fall back to); a full-screen cover's dismissal is
/// standard and reliable on both iPhone and iPad.
struct OpenFolderButton: View {
    @State private var isPickerPresented = false
    @State private var bookmarkStore = WorkspaceBookmarkStore()
    @State private var presentedEntry: WorkspaceBookmarkEntry?

    var body: some View {
        Button {
            isPickerPresented = true
        } label: {
            Label("Open Programme Folder…", systemImage: "folder")
        }
        .sheet(isPresented: $isPickerPresented) {
            FolderPickerView { url in
                isPickerPresented = false
                openWorkspace(for: url)
            }
        }
        .fullScreenCover(item: $presentedEntry) { entry in
            WorkspaceScene(entry: entry)
        }
        .accessibilityIdentifier("OpenFolderButton")
    }

    /// Creates a security-scoped bookmark for the picked folder and presents the workspace cover onto it.
    ///
    /// `UIDocumentPickerViewController`'s URL requires `startAccessingSecurityScopedResource()` before any
    /// operation on it, including `bookmarkData()` — without it, bookmark creation fails silently. Access is
    /// only needed transiently here, to create the bookmark; `WorkspaceModel` starts its own access on the
    /// URL it later resolves from that bookmark.
    private func openWorkspace(for url: URL) {
        guard url.startAccessingSecurityScopedResource() else { return }
        defer { url.stopAccessingSecurityScopedResource() }

        guard let entry = try? bookmarkStore.addRecent(for: url) else { return }
        presentedEntry = entry
    }
}
