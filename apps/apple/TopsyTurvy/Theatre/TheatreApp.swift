import SwiftUI

/// Entry point for the Theatre app.
///
/// The entire app is wrapped by a `DocumentGroup` to support creating, opening and saving Topsy Turvy files.
/// Documents are represents by `TheatreDocument`
@main
struct TheatreApp: App {
    /// The main app scene.
    var body: some Scene {
        DocumentGroup(newDocument: TheatreDocument()) { configuration in
            SingleFileEditorHostView(text: configuration.$document.text, documentURL: configuration.fileURL)
        }
        .defaultSize(width: 1100, height: 750)
        .commands {
            TheatreCommands()
        }
    }
}
