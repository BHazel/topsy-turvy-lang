import SwiftUI

/// Hosts a single `.topsy` document opened via `DocumentGroup`: owns that document window's own
/// `TopsyTurvySession` for the window's whole lifetime, and renders the shared `WorkspaceEditorHostView` shell
/// bound directly to the document's own `FileDocument` text binding — its autosave already covers persistence,
/// so no disk-backed wrapper (`WorkspaceFileEditorView`'s workspace-file equivalent) is needed here.
///
/// Passes "Open Folder" (a single-file-specific escape hatch into the fuller workspace experience, not
/// something a workspace file itself would offer) as `WorkspaceEditorHostView`'s `extraToolbarContent`, merging
/// it into that view's own single `.toolbar` call rather than layering a second, separate one here — declaring
/// two `.toolbar` calls (one here, one inside `WorkspaceEditorHostView`'s own `body`) was tried and produced
/// visibly duplicated toolbar buttons, live-tested on both iPhone and iPad (2026-07-05).
struct SingleFileEditorHostView: View {
    @Binding var text: String
    let documentURL: URL?

    @State private var session = TopsyTurvySession()

    var body: some View {
        WorkspaceEditorHostView(text: $text, documentURL: documentURL, session: session) {
            ToolbarItemGroup {
                OpenFolderButton()
            }
        }
        .task {
            await session.open()
        }
        .onDisappear {
            Task { await session.close() }
        }
    }
}
