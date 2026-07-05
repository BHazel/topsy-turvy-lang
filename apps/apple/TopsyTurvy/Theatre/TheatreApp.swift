import SwiftUI

/// The entry point for the app.
@main
struct TheatreApp: App {
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
