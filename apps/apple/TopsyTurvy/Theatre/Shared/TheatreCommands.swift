import SwiftUI

/// The Theatre app's menu bar commands, sourced from the focused document window's `TheatreDocumentActions` —
/// each item is disabled when no document window has focus.
struct TheatreCommands: Commands {
    @FocusedValue(\.theatreDocumentActions) private var actions

    var body: some Commands {
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
