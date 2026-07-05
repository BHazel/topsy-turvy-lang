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
    @AppStorage("editorFontSize") private var fontSize: Double = EditorFontSize.default
    @StateObject private var keyboardObserver = KeyboardObserver()

    /// Backs an invisible focus proxy (below) — see its comment for why one is needed at all.
    @FocusState private var isFocusProxyFocused: Bool

    var body: some View {
        editor
            // `CodeEditorView` wraps its own `UITextView`, which never participates in SwiftUI's focus
            // system — nothing in this scene ever becomes a SwiftUI-recognised focused element. Without one,
            // iPadOS never considers this scene "focused" for `@FocusedValue` purposes, so `.focusedSceneValue`
            // below never activates and `TheatreCommands`' menu items stay permanently disabled. This
            // invisible, zero-size proxy grabs focus as soon as the view appears purely to give the scene
            // something to recognise.
            .background {
                Color.clear
                    .focusable()
                    .focused($isFocusProxyFocused)
                    .frame(width: 0, height: 0)
            }
            .onAppear {
                isFocusProxyFocused = true
            }
            .safeAreaInset(edge: .bottom) {
                OutputDrawerView(lines: runViewModel.outputLines, statusMessage: runViewModel.statusMessage, isPresented: $isOutputPresented)
            }
            .issuesInspector(diagnostics: diagnostics, isPresented: $isIssuesPresented, onSelect: navigateToDiagnostic)
            .toolbar {
                toolbarContent
                extraToolbarContent()
            }
            .focusedSceneValue(
                \.theatreDocumentActions,
                TheatreDocumentActions(
                    isRunning: runViewModel.isRunning,
                    perform: runProgramme,
                    stop: stopProgramme,
                    format: formatSource
                )
            )
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
