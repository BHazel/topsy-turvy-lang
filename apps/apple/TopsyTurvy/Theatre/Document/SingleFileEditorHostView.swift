import SwiftUI

/// Hosts a single Topsy Turvy source file opened via `DocumentGroup`.
///
/// Owns its own `TopsyTurvySession` for as long as this document stays open. The actual editing UI
/// comes from the shared `EditorHostView`, which this view feeds directly from the text of the
/// document.  That text already autosaves itself, so no extra saving code is needed here.
///
/// Also adds an "Open Folder" button to the `EditorHostView` toolbar as a way to jump from one open file
/// into the fuller folder-browsing experience.
struct SingleFileEditorHostView: View {
    /// The Topsy Turvy toolchain session owned by this document.
    @State private var session = TopsyTurvySession()

    /// The source text.
    @Binding var text: String

    /// The source file URL.
    let documentURL: URL?

    /// The view body.
    var body: some View {
        EditorHostView(text: self.$text, documentURL: self.documentURL, session: self.session) {
            ToolbarItemGroup {
                OpenFolderButton()
            }
        }
        .task {
            await self.session.open()
        }
        .onDisappear {
            Task {
                await self.session.close()
            }
        }
    }
}
