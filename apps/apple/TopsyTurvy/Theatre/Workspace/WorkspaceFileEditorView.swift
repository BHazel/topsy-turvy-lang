import SwiftUI

/// Loads one workspace file's contents from disk into a plain `Binding<String>` for `WorkspaceEditorHostView`,
/// and writes it back continuously — debounced ~1s after the last edit, flushed immediately before Perform
/// and when this file's editing surface disappears (file switch or workspace close). Workspace files are
/// plain on-disk buffers, unlike a single-file `DocumentGroup` document's `FileDocument`, which has its own
/// autosave.
///
/// `WorkspaceScene` applies `.id(fileURL)` at the call site, so this view (and its `@State`) is recreated
/// whenever the user selects a different file.
struct WorkspaceFileEditorView: View {
    let fileURL: URL
    let session: TopsyTurvySession

    @State private var text = ""
    @State private var pendingSaveTask: Task<Void, Never>?

    var body: some View {
        WorkspaceEditorHostView(text: $text, documentURL: fileURL, session: session, onSave: flush)
            .navigationTitle(fileURL.lastPathComponent)
            // Large titles are for root/browsing screens (Mail's inbox, Settings); an editing surface's own
            // file name reads more naturally compact, the way Pages/Xcode show the current document's title.
            .navigationBarTitleDisplayMode(.inline)
            .task {
                text = (try? String(contentsOf: fileURL, encoding: .utf8)) ?? ""
            }
            .onChange(of: text) {
                scheduleSave()
            }
            .onDisappear {
                flush()
            }
    }

    /// Restarts the ~1s idle debounce, discarding any previously scheduled save.
    private func scheduleSave() {
        pendingSaveTask?.cancel()
        pendingSaveTask = Task {
            try? await Task.sleep(for: .seconds(1))
            guard !Task.isCancelled else { return }
            flush()
        }
    }

    /// Writes `text` back to `fileURL` immediately, best-effort, cancelling any pending debounced save.
    private func flush() {
        pendingSaveTask?.cancel()
        pendingSaveTask = nil
        try? text.write(to: fileURL, atomically: true, encoding: .utf8)
    }
}
