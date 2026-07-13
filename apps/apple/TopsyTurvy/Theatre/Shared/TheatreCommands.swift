import SwiftUI

/// Defines commands for the menu bar and keyboard shortcuts.
///
/// Menu bar commands are available on iPadOS, and macOS if supported by the app.
struct TheatreCommands: Commands {
    /// The commands.
    var body: some Commands {
        let actions = TheatreActiveScene.shared.documentActions
        let folderActions = TheatreActiveScene.shared.folderActions
        
        CommandGroup(after: .newItem) {
            Button("Open Folder…") {
                folderActions?.openFolder()
            }
            .keyboardShortcut("o", modifiers: [.command, .shift])
            .disabled(folderActions == nil)

            Menu("Open Recent") {
                if let recents = folderActions?.recents, !recents.isEmpty {
                    ForEach(recents) { entry in
                        Button(entry.displayName) {
                            folderActions?.openRecent(entry)
                        }
                    }
                }
            }
            .disabled(folderActions?.recents.isEmpty == true)
        }

        CommandMenu("Performance") {
            Button("Perform") {
                actions?.perform()
            }
            .keyboardShortcut("r", modifiers: .command)
            .disabled(actions == nil || actions?.isRunning == true)

            Button("Stop") {
                actions?.stop()
            }
            .keyboardShortcut(".", modifiers: .command)
            .disabled(actions == nil || actions?.isRunning == false)
        }

        CommandGroup(after: .pasteboard) {
            Button("Format Programme") {
                actions?.format()
            }
            .keyboardShortcut("f", modifiers: [.command, .shift])
            .disabled(actions == nil)
        }
    }
}
