import SwiftUI

/// The Theatre app's menu bar commands, sourced from the active document window's `TheatreDocumentActions`
/// and `TheatreFolderActions` (via `TheatreActiveScene`, not `@FocusedValue` — see that type's doc comment
/// for why) — each item is disabled when no document window is active.
struct TheatreCommands: Commands {
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
            .disabled(folderActions?.recents.isEmpty != false)
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
