import SwiftUI

#if os(iOS)
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
#elseif os(macOS)
import AppKit

private enum FolderPicker {
    /// Runs a modal `NSOpenPanel` restricted to picking a single directory.
    static func pick() -> URL? {
        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.allowsMultipleSelection = false
        return panel.runModal() == .OK ? panel.url : nil
    }
}
#endif

/// A toolbar button that opens a folder picker for multi-file `PRAY ADMIT` programmes, unifying the
/// `NSOpenPanel` (macOS) and `UIDocumentPickerViewController` (iOS/iPadOS) presentation behind one call site.
struct OpenFolderButton: View {
    var onPick: (URL) -> Void

    #if os(iOS)
    @State private var isPickerPresented = false
    #endif

    var body: some View {
        Button {
            #if os(macOS)
            if let url = FolderPicker.pick() {
                onPick(url)
            }
            #else
            isPickerPresented = true
            #endif
        } label: {
            Label("Open Programme Folder…", systemImage: "folder")
        }
        #if os(iOS)
        .sheet(isPresented: $isPickerPresented) {
            FolderPickerView { url in
                isPickerPresented = false
                onPick(url)
            }
        }
        #endif
    }
}
