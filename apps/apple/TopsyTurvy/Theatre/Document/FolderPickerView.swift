import SwiftUI
import UIKit

/// Wraps `UIDocumentPickerViewController` in directory-open mode, for picking a folder on
/// iOS/iPadOS.
struct FolderPickerView: UIViewControllerRepresentable {
    /// The handler for when a folder is picked.
    var onPick: (URL) -> Void

    /// Builds the `UIDocumentPickerViewController`.
    ///
    /// - Parameters:
    ///   - context: The contextual information about the system state.
    ///
    /// - Returns: The `UIDocumentPickerViewController` configured in directory-open mode.
    func makeUIViewController(context: Context) -> UIDocumentPickerViewController {
        let picker = UIDocumentPickerViewController(forOpeningContentTypes: [.folder])
        picker.delegate = context.coordinator
        return picker
    }

    /// Updates the `UIDocumentPickerViewController`.
    ///
    /// This is not required and is therefore a no-op.
    ///
    /// - Parameters:
    ///   - uiViewController: The `UIDocumentPickerViewController`.
    ///   - context: The contextual information about the system state.
    func updateUIViewController(_ uiViewController: UIDocumentPickerViewController, context: Context) {}

    /// Makes a coordinator.
    ///
    /// Called directly by SwiftUI.
    ///
    /// - Returns: A coordinator.
    func makeCoordinator() -> Coordinator {
        Coordinator(onPick: onPick)
    }

    /// Coordinates between the delegate-based UIKit and SwiftUI.
    ///
    /// To follow convention this class is nested and only used to support `FolderPickerView`.
    final class Coordinator: NSObject, UIDocumentPickerDelegate {
        /// The handler for when a folder is picked.
        let onPick: (URL) -> Void

        /// Creates a `Coordinator` with the handler for when a folder is picked.
        ///
        /// - Parameters:
        ///   - onPick: The handler for when a folder is picked.
        init(onPick: @escaping (URL) -> Void) {
            self.onPick = onPick
        }

        /// The delegate for when an item is picked.
        ///
        /// - Parameters:
        ///   - controller: The `UIDocumentPickerViewController`.
        ///   - urls: The selected item URLs.
        func documentPicker(_ controller: UIDocumentPickerViewController, didPickDocumentsAt urls: [URL]) {
            if let url = urls.first {
                self.onPick(url)
            }
        }
    }
}
