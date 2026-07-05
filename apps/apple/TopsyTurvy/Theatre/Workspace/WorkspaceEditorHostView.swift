import CodeEditorView
import Combine
import LanguageSupport
import SwiftUI

/// Hosts one file's editing surface: editor, run controls, output drawer, and issues inspector. The single
/// shared shell for both single-file `DocumentGroup` documents (see `SingleFileEditorHostView`) and workspace
/// files (see `WorkspaceFileEditorView`) — the two contexts differ only in how `text` is backed (a
/// `FileDocument` binding vs. a disk-backed one with its own debounced autosave) and how persistence and the
/// navigation title are wired, both of which are the caller's responsibility, not this view's.
struct WorkspaceEditorHostView<ExtraToolbarContent: ToolbarContent>: View {
    @Binding var text: String

    /// The file's location, for `PRAY ADMIT` sibling resolution — `nil` for a brand-new, not-yet-saved
    /// single-file document, in which case sibling imports simply have nothing to resolve against yet.
    let documentURL: URL?

    /// Owned by the caller (`WorkspaceScene` or `SingleFileEditorHostView`), not this view: a
    /// `TopsyTurvySession` is a reusable engine handle, not tied to one file, and must outlive any single
    /// file's editing surface — see `WorkspaceScene`'s own session property for why per-file session churn is
    /// unsafe.
    let session: TopsyTurvySession

    /// Called before running the programme, so the caller can flush a pending disk write first (workspace
    /// files) — `nil` for single-file documents, whose `FileDocument` binding already has its own autosave.
    var onSave: (() -> Void)?

    /// Caller-specific items merged into this view's own single `.toolbar` call — e.g. `SingleFileEditorHostView`
    /// passes `OpenFolderButton()` here. Declaring a *second*, separate `.toolbar` at the caller's level (tried
    /// previously) produced duplicated buttons in single-file mode, live-tested on both iPhone and iPad
    /// (2026-07-05): exactly one `.toolbar` modifier must ever apply to this view, so callers merge in rather
    /// than layering their own alongside it.
    var extraToolbarContent: () -> ExtraToolbarContent

    @State private var languageService: TopsyTurvyLanguageService?
    @State private var runViewModel = RunSessionViewModel()
    @State private var args: [String] = []
    @State private var stdinLines: [String] = []
    @State private var isInputsPresented = false
    @State private var isOutputPresented = false
    @State private var isIssuesPresented = false
    @State private var position = CodeEditor.Position()
    @State private var diagnostics: Set<TextLocated<Message>> = []
    @State private var completionBarController = CompletionBarController()
    @AppStorage("editorFontSize") private var fontSize: Double = EditorFontSize.default
    @StateObject private var keyboardObserver = KeyboardObserver()

    /// This instance's token for `TheatreActiveScene` publishing — see that type's doc comment.
    private let activeSceneToken = UUID()

    @Environment(\.scenePhase) private var scenePhase

    var body: some View {
        editor
            // Both the completion bar and the output drawer sit in one `.safeAreaInset` — the same mechanism
            // `OutputDrawerView` already relies on correctly riding above the software keyboard on its own; a
            // separate `.overlay` with manual `keyboardHeight` padding was tried first and confirmed live
            // (2026-07-05, iPhone and detached-keyboard iPad) to get covered by the keyboard/drawer instead of
            // riding above them. Not `ToolbarItemGroup(placement: .keyboard)`, which is unreliable over
            // `CodeEditorView`'s wrapped `UITextView` (a `UIViewRepresentable`).
            .safeAreaInset(edge: .bottom) {
                VStack(spacing: 0) {
                    CompletionBarView(candidates: completionBarController.candidates, onSelect: insertCompletion)
                    OutputDrawerView(lines: runViewModel.outputLines, statusMessage: runViewModel.statusMessage, isPresented: $isOutputPresented)
                }
            }
            .issuesInspector(diagnostics: diagnostics, isPresented: $isIssuesPresented, onSelect: navigateToDiagnostic)
            .toolbar {
                toolbarContent
                extraToolbarContent()
            }
            .onAppear {
                publishActiveDocumentActions()
            }
            .onDisappear {
                TheatreActiveScene.shared.clearDocumentActions(token: activeSceneToken)
            }
            .onChange(of: scenePhase) { _, newPhase in
                if newPhase == .active {
                    publishActiveDocumentActions()
                }
            }
            .onChange(of: runViewModel.isRunning) {
                publishActiveDocumentActions()
            }
            .onChange(of: text) {
                scheduleCompletionUpdate()
            }
            .onChange(of: position.selections) {
                scheduleCompletionUpdate()
            }
            .task {
                // Re-registers the import resolver for this file's directory on the shared session — the
                // session itself is already open, owned by the caller for its whole lifetime.
                await session.setImportResolver { [documentURL] filename in
                    guard let documentURL else { return nil }
                    return Self.resolveSiblingImport(filename, relativeTo: documentURL)
                }
                languageService = TopsyTurvyLanguageService(session: session)
            }
            // `languageService` starts `nil` until the `.task` above populates it; `Empty` stands in until then.
            .onReceive(languageService?.diagnostics.eraseToAnyPublisher() ?? Empty().eraseToAnyPublisher()) { newDiagnostics in
                diagnostics = newDiagnostics
            }
    }

    private var editor: some View {
        TopsyTurvyCodeEditorView(text: $text, position: $position, session: session, languageService: languageService, fontSize: fontSize)
    }

    /// This view's own toolbar items, merged with `extraToolbarContent` into the one `.toolbar` call above.
    @ToolbarContentBuilder
    private var toolbarContent: some ToolbarContent {
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
    /// interpreter itself resolves imports when no custom resolver is supplied. For a workspace file, siblings
    /// anywhere in the folder tree are reachable, since the root folder's security-scoped access
    /// (`WorkspaceModel.startAccess()`) covers its whole subtree, not just the selected file.
    private static func resolveSiblingImport(_ filename: String, relativeTo documentURL: URL) -> String? {
        let siblingURL = documentURL.deletingLastPathComponent().appendingPathComponent(filename)
        return try? String(contentsOf: siblingURL, encoding: .utf8)
    }

    /// Publishes this file's actions to `TheatreActiveScene`, so the menu bar and hardware-keyboard shortcuts
    /// reach whichever document/workspace scene is currently active.
    private func publishActiveDocumentActions() {
        TheatreActiveScene.shared.publishDocumentActions(
            TheatreDocumentActions(isRunning: runViewModel.isRunning, perform: runProgramme, stop: stopProgramme, format: formatSource),
            token: activeSceneToken
        )
    }

    /// Recomputes the completion bar's candidates for the current cursor position, after `CompletionBarController`'s
    /// own debounce.
    private func scheduleCompletionUpdate() {
        guard let location = position.selections.first?.location else { return }
        let (line, column) = Self.lineColumn(forOffset: location, in: text)
        completionBarController.scheduleUpdate(session: session, source: text, line: Int32(line), column: Int32(column))
    }

    /// Splices a tapped completion chip into `text`, replacing the already-typed word it completes (not
    /// appending after it — `InsertText` is always the *full* replacement text, e.g. a symbol's is
    /// unconditionally its whole name per `NativeExports.BuildSymbolItem`, never trimmed by what's already
    /// typed), and advances the cursor past the inserted text.
    private func insertCompletion(_ item: CompletionItemPayload) {
        guard let cursor = position.selections.first else { return }
        let lastWordLength = completionBarController.lastWord.utf16.count
        let replaceRange = NSRange(location: cursor.location - lastWordLength, length: lastWordLength)
        let result = TopsyTurvyCompletionInserter.insert(item, into: text, at: replaceRange)
        text = result.text
        position.selections = [result.selection]
    }

    private func runProgramme() {
        keyboardObserver.dismiss()
        onSave?()
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

    /// Converts an absolute UTF-16 character offset into `text` into a 0-indexed line/column — the inverse of
    /// `characterOffset(forZeroBasedLine:column:in:)`, used to feed the completion bar's cursor position
    /// directly from `text`/`position`, without depending on `TopsyTurvyLanguageService`'s own asynchronously
    /// populated `locationService`.
    private static func lineColumn(forOffset offset: Int, in text: String) -> (line: Int, column: Int) {
        let lines = text.components(separatedBy: "\n")
        var remaining = offset
        for (index, line) in lines.enumerated() {
            let lineLength = line.utf16.count + 1
            if remaining < lineLength || index == lines.count - 1 {
                return (index, remaining)
            }
            remaining -= lineLength
        }
        return (max(0, lines.count - 1), 0)
    }
}

/// A no-op `ToolbarContent`, standing in for callers with no extra toolbar items of their own.
struct NoExtraToolbarContent: ToolbarContent {
    var body: some ToolbarContent {
        ToolbarItemGroup {}
    }
}

extension WorkspaceEditorHostView where ExtraToolbarContent == NoExtraToolbarContent {
    /// Convenience for callers with no extra toolbar items of their own (`WorkspaceFileEditorView`).
    init(text: Binding<String>, documentURL: URL?, session: TopsyTurvySession, onSave: (() -> Void)? = nil) {
        self.init(text: text, documentURL: documentURL, session: session, onSave: onSave) { NoExtraToolbarContent() }
    }
}
