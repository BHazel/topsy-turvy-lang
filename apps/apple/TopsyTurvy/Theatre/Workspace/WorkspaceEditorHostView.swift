import CodeEditorView
import Combine
import LanguageSupport
import SwiftUI

/// Hosts one workspace file's editing surface: editor, run controls, output drawer, and issues inspector —
/// the workspace-window counterpart to `TheatreDocumentView`, reading and writing a plain on-disk buffer
/// instead of a `FileDocument`-bound one, since workspace files are real files on disk rather than the
/// document model's in-memory-then-saved buffers.
///
/// `WorkspaceScene` applies `.id(fileURL)` at the call site, so this view (and its `@State`) is recreated
/// whenever the user selects a different file — the same per-document lifecycle `TheatreDocumentView` gets
/// for free from `DocumentGroup`.
struct WorkspaceEditorHostView: View {
    let fileURL: URL

    /// Owned by the caller (`WorkspaceScene`), not this view: a `TopsyTurvySession` is a reusable engine
    /// handle, not tied to one file, and must outlive any single file's editing surface — see
    /// `WorkspaceScene`'s own session property for why per-file session churn is unsafe.
    let session: TopsyTurvySession

    @State private var text = ""
    @State private var languageService: TopsyTurvyLanguageService?
    @State private var runViewModel = RunSessionViewModel()
    @State private var args: [String] = []
    @State private var stdinLines: [String] = []
    @State private var isInputsPresented = false
    @State private var isOutputPresented = false
    @State private var isIssuesPresented = false
    @State private var position = CodeEditor.Position()
    @State private var diagnostics: Set<TextLocated<Message>> = []
    @AppStorage("editorFontSize") private var fontSize: Double = EditorFontSize.default
    @StateObject private var keyboardObserver = KeyboardObserver()

    var body: some View {
        editor
            .toolbar {
                if keyboardObserver.isKeyboardVisible {
                    ToolbarItemGroup {
                        Button {
                            keyboardObserver.dismiss()
                        } label: {
                            Label("Dismiss Keyboard", systemImage: "keyboard.chevron.compact.down")
                        }
                    }
                }
                ToolbarItemGroup {
                    RunToolbarButtons(isRunning: runViewModel.isRunning, onRun: runProgramme, onStop: stopProgramme)
                }
                ToolbarItemGroup {
                    inputsButton
                }
                ToolbarItemGroup {
                    formatButton
                }
                ToolbarItemGroup {
                    issuesToggleButton
                }
                ToolbarItemGroup {
                    EditorFontSizeMenu(fontSize: $fontSize)
                }
            }
            .safeAreaInset(edge: .bottom) {
                OutputDrawerView(lines: runViewModel.outputLines, statusMessage: runViewModel.statusMessage, isPresented: $isOutputPresented)
            }
            .issuesInspector(diagnostics: diagnostics, isPresented: $isIssuesPresented, onSelect: navigateToDiagnostic)
            .focusedSceneValue(
                \.theatreDocumentActions,
                TheatreDocumentActions(
                    isRunning: runViewModel.isRunning,
                    perform: runProgramme,
                    stop: stopProgramme,
                    format: formatSource
                )
            )
            .navigationTitle(fileURL.lastPathComponent)
            // Large titles are for root/browsing screens (Mail's inbox, Settings); an editing surface's own
            // file name reads more naturally compact, the way Pages/Xcode show the current document's title.
            .navigationBarTitleDisplayMode(.inline)
            .task {
                text = (try? String(contentsOf: fileURL, encoding: .utf8)) ?? ""
                // Re-registers the import resolver for this file's directory on the shared session — the
                // session itself is already open, owned by `WorkspaceScene` for the workspace's whole lifetime.
                await session.setImportResolver { [fileURL] filename in
                    Self.resolveSiblingImport(filename, relativeTo: fileURL)
                }
                languageService = TopsyTurvyLanguageService(session: session)
            }
            .onDisappear {
                save()
            }
            // `languageService` starts `nil` until the `.task` above populates it; `Empty` stands in until then.
            .onReceive(languageService?.diagnostics.eraseToAnyPublisher() ?? Empty().eraseToAnyPublisher()) { newDiagnostics in
                diagnostics = newDiagnostics
            }
    }

    private var editor: some View {
        TopsyTurvyCodeEditorView(text: $text, position: $position, session: session, languageService: languageService, fontSize: fontSize)
    }

    /// The inputs button, presenting `RunInputsView` as a medium-detent sheet.
    private var inputsButton: some View {
        Button {
            isInputsPresented = true
        } label: {
            Label("Arguments & Input", systemImage: "text.append")
        }
        .sheet(isPresented: $isInputsPresented) {
            RunInputsView(args: $args, stdinLines: $stdinLines)
                .presentationDetents([.medium, .large])
        }
    }

    /// The format command button: replaces the buffer with `topsyturvy_format`'s canonically cased, correctly
    /// indented output.
    private var formatButton: some View {
        Button {
            formatSource()
        } label: {
            Label("Format", systemImage: "text.alignleft")
        }
        .accessibilityIdentifier("WorkspaceEditorHostView.formatButton")
    }

    /// Toggles the issues inspector/sheet.
    private var issuesToggleButton: some View {
        Button {
            isIssuesPresented.toggle()
        } label: {
            Label("Issues", systemImage: "exclamationmark.triangle")
        }
        .accessibilityIdentifier("WorkspaceEditorHostView.issuesToggleButton")
    }

    /// Resolves a `PRAY ADMIT` filename against the directory containing this file, mirroring how the
    /// interpreter itself resolves imports when no custom resolver is supplied. Siblings anywhere in the
    /// workspace's folder tree are reachable, since the root folder's security-scoped access
    /// (`WorkspaceModel.startAccess()`) covers its whole subtree, not just the selected file.
    private static func resolveSiblingImport(_ filename: String, relativeTo fileURL: URL) -> String? {
        let siblingURL = fileURL.deletingLastPathComponent().appendingPathComponent(filename)
        return try? String(contentsOf: siblingURL, encoding: .utf8)
    }

    private func runProgramme() {
        keyboardObserver.dismiss()
        save()
        isOutputPresented = true
        Task {
            await runViewModel.run(session: session, source: text, args: args, stdin: stdinLines.joined(separator: "\n"))
        }
    }

    private func stopProgramme() {
        runViewModel.stop(session: session)
    }

    /// Replaces `text` with `session.format(source:)`'s canonically cased, correctly indented output.
    private func formatSource() {
        Task {
            if let formatted = await session.format(source: text) {
                text = formatted
            }
        }
    }

    /// Moves the editor's selection to a diagnostic's location, invoked by tapping a row in `IssuesListView`.
    private func navigateToDiagnostic(_ location: TextLocation) {
        let offset = Self.characterOffset(forZeroBasedLine: location.zeroBasedLine, column: location.zeroBasedColumn, in: text)
        position.selections = [NSRange(location: offset, length: 0)]
    }

    /// Converts a 0-indexed line/column into an absolute UTF-16 character offset into `text`.
    private static func characterOffset(forZeroBasedLine line: Int, column: Int, in text: String) -> Int {
        let lines = text.components(separatedBy: "\n")
        var offset = 0
        for index in 0..<line where index < lines.count {
            offset += lines[index].utf16.count + 1
        }
        return offset + column
    }

    /// Writes `text` back to `fileURL`, best-effort — called before running (autosave-on-Perform) and when
    /// this file's editing surface disappears (file switch or window close), since workspace files are plain
    /// on-disk buffers rather than a `FileDocument` with its own autosave.
    private func save() {
        try? text.write(to: fileURL, atomically: true, encoding: .utf8)
    }
}
