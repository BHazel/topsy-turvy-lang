import SwiftUI

/// Loads the source for a file into the editor when a folder is open.
///
/// Auto-save is unavailable therefore `FolderFileEditorView` implements its own logic.
struct FolderFileEditorView: View {
    /// The file URL.
    let fileURL: URL
    
    /// The Topsy Turvy toolchain session.
    let session: TopsyTurvySession

    /// The current text of the file.
    @State private var text = ""

    /// The debounced save task, if a save is currently pending.
    @State private var pendingSaveTask: Task<Void, Never>?

    /// The view body.
    var body: some View {
        EditorHostView(text: self.$text, documentURL: self.fileURL, session: self.session, onSave: self.save)
            .navigationTitle(self.fileURL.lastPathComponent)
            .navigationBarTitleDisplayMode(.inline)
            .task {
                self.text = (try? String(contentsOf: self.fileURL, encoding: .utf8)) ?? ""
            }
            .onChange(of: self.text) {
                self.scheduleSave()
            }
            .onDisappear {
                self.save()
            }
    }

    /// Restarts the ~1s idle debounce, discarding any previously scheduled save.
    private func scheduleSave() {
        self.pendingSaveTask?.cancel()
        self.pendingSaveTask = Task {
            try? await Task.sleep(for: .seconds(1))
            guard !Task.isCancelled else { return }
            self.save()
        }
    }

    /// Saves the text to the file.
    private func save() {
        self.pendingSaveTask?.cancel()
        self.pendingSaveTask = nil
        try? self.text.write(to: self.fileURL, atomically: true, encoding: .utf8)
    }
}
