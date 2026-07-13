import CodeEditorView
import Combine
import LanguageSupport
import SwiftUI

/// Hosts the editing surface for one file including the editor, run controls, output drawer and issues inspector.
///
/// Shared by both single-file `DocumentGroup` documents (`SingleFileEditorHostView`) and folder files
/// (`FolderFileEditorView`).  The two differ only in how `text` is backed and saved, and how the navigation
/// title is set, which is the responsibility of the caller, not of this view.
struct EditorHostView<ExtraToolbarContent: ToolbarContent>: View {
    /// The source text.
    @Binding var text: String

    /// The location of the file.
    let documentURL: URL?

    /// The Topsy Turvy toolchain session.
    ///
    /// This is owned by the caller: a `FolderBrowserView` when a folder is open, or `SingleFileEditorHostView`
    /// when only a single file is being edited.
    let session: TopsyTurvySession

    /// The handler for a save.
    ///
    /// Called before executing a programme.  Only required when a folder is open in `FolderBrowserView` as the single-file
    /// `SingleFileEditorHostView` already auto-saves.
    var onSave: (() -> Void)?

    /// The extra toolbar items from the caller, merged into the one `.toolbar` in the view body.
    ///
    /// Only one `.toolbar` modifier should ever apply to this view otherwise duplication occurs.
    var extraToolbarContent: () -> ExtraToolbarContent

    /// The `CodeEditorView` language service, created once the session is open.
    @State private var languageService: TopsyTurvyLanguageService?

    /// The orchestrator for programme runs.
    @State private var runOrchestrator = TopsyTurvyRunOrchestrator()

    /// The command-line arguments exposed as `THE PROPS`.
    @State private var arguments: [String] = []

    /// The preset standard input lines.
    @State private var stdinLines: [String] = []

    /// A value indicating whether the arguments and input sheet is presented.
    @State private var isInputsPresented = false

    /// A value indicating whether the output pane is presented.
    @State private var isOutputPresented = false

    /// A value indicating whether the issues inspector is presented.
    @State private var isIssuesPresented = false

    /// The current cursor and selection position in the editor.
    @State private var position = CodeEditor.Position()

    /// The current diagnostics shown in the issues inspector.
    @State private var diagnostics: Set<TextLocated<Message>> = []

    /// The controller for the completions bar.
    @State private var completionBarController = CompletionBarController()

    /// The scene phase, from the environment.
    @Environment(\.scenePhase) private var scenePhase

    /// The user-adjustable editor font size, persisted via `@AppStorage`.
    @AppStorage("editorFontSize") private var fontSize: Double = EditorFontSize.default

    /// Observes the software keyboard so the toolbar can offer a "Dismiss Keyboard" action while it is shown.
    @StateObject private var keyboardObserver = KeyboardObserver()

    /// The token for `TheatreActiveScene` publishing from this instance.
    private let activeSceneToken = UUID()

    /// The view body.
    var body: some View {
        self.editor
            .safeAreaInset(edge: .bottom) {
                VStack(spacing: 0) {
                    CompletionBarView(candidates: self.completionBarController.candidates, onSelect: self.insertCompletion)
                    OutputDrawerView(lines: self.runOrchestrator.outputLines, statusMessage: self.runOrchestrator.statusMessage, isPresented: self.$isOutputPresented)
                }
            }
            .issuesInspector(diagnostics: diagnostics, isPresented: $isIssuesPresented, onSelect: navigateToDiagnostic)
            .toolbar {
                self.toolbarContent
                self.extraToolbarContent()
            }
            .onAppear {
                self.publishActiveDocumentActions()
            }
            .onDisappear {
                TheatreActiveScene.shared.clearDocumentActions(token: self.activeSceneToken)
            }
            .onChange(of: scenePhase) { _, newPhase in
                if newPhase == .active {
                    self.publishActiveDocumentActions()
                }
            }
            .onChange(of: runOrchestrator.isRunning) {
                self.publishActiveDocumentActions()
            }
            .onChange(of: text) {
                self.scheduleCompletionUpdate()
            }
            .onChange(of: position.selections) {
                self.scheduleCompletionUpdate()
            }
            .task {
                await session.setImportResolver { [documentURL] filename in
                    guard let documentURL else {
                        return nil
                    }
                    
                    return Self.resolveImport(filename, relativeTo: documentURL)
                }
                
                self.languageService = TopsyTurvyLanguageService(session: session)
            }
            .onReceive(self.languageService?.diagnostics.eraseToAnyPublisher() ?? Empty().eraseToAnyPublisher()) { newDiagnostics in
                self.diagnostics = newDiagnostics
            }
    }

    /// The editor view.
    private var editor: some View {
        TopsyTurvyCodeEditorView(text: self.$text, position: self.$position, session: self.session, languageService: self.languageService, fontSize: self.fontSize)
    }

    /// The toolbar items owned by this view.
    ///
    /// This is merged with `extraToolbarContent` into the one `.toolbar` call in the view body.
    @ToolbarContentBuilder
    private var toolbarContent: some ToolbarContent {
        if self.keyboardObserver.isKeyboardVisible {
            ToolbarItemGroup {
                Button {
                    self.keyboardObserver.dismiss()
                } label: {
                    Label("Dismiss Keyboard", systemImage: "keyboard.chevron.compact.down")
                }
            }
        }
        ToolbarItemGroup {
            RunToolbarButtons(isRunning: self.runOrchestrator.isRunning, onRun: self.runProgramme, onStop: self.stopProgramme)
        }
        ToolbarItemGroup {
            self.inputsButton
        }
        ToolbarItemGroup {
            self.formatButton
        }
        ToolbarItemGroup {
            self.issuesToggleButton
        }
        ToolbarItemGroup {
            EditorFontSizeMenu(fontSize: self.$fontSize)
        }
    }

    /// The Inputs button.
    private var inputsButton: some View {
        Button {
            self.isInputsPresented = true
        } label: {
            Label("Arguments & Input", systemImage: "text.append")
        }
        .sheet(isPresented: self.$isInputsPresented) {
            RunInputsView(commandLineArguments: self.$arguments, stdinLines: self.$stdinLines)
                .presentationDetents([.medium, .large])
        }
    }

    /// The Format button.
    private var formatButton: some View {
        Button {
            self.formatSource()
        } label: {
            Label("Format", systemImage: "text.alignleft")
        }
        .accessibilityIdentifier("EditorHostView.formatButton")
    }

    /// Toggles the Issues inspector/sheet.
    private var issuesToggleButton: some View {
        Button {
            self.isIssuesPresented.toggle()
        } label: {
            Label("Issues", systemImage: "exclamationmark.triangle")
        }
        .accessibilityIdentifier("EditorHostView.issuesToggleButton")
    }

    /// Resolves an imported filename (via `PRAY ADMIT`) against the directory containing this file.
    ///
    /// - Parameters:
    ///   - filename: The filename to import.
    ///   - documentURL: The path of the file.
    ///
    /// - Returns: The source text of the imported filename.
    private static func resolveImport(_ filename: String, relativeTo documentURL: URL) -> String? {
        let fileToImportURL = documentURL.deletingLastPathComponent().appendingPathComponent(filename)
        return try? String(contentsOf: fileToImportURL, encoding: .utf8)
    }

    /// Publishes the current actions to `TheatreActiveScene`, so the menu bar and hardware-keyboard shortcuts
    /// reach whichever document or folder window is currently active.
    private func publishActiveDocumentActions() {
        TheatreActiveScene.shared.publishDocumentActions(
            TheatreDocumentActions(isRunning: self.runOrchestrator.isRunning, perform: self.runProgramme, stop: self.stopProgramme, format: self.formatSource),
            token: self.activeSceneToken
        )
    }

    /// Recomputes candidates for the completion bar at the current cursor position, after the debounce in
    /// `CompletionBarController`.
    private func scheduleCompletionUpdate() {
        guard let location = self.position.selections.first?.location else {
            return
        }
        
        let (line, column) = Self.lineColumn(forOffset: location, in: self.text)
        self.completionBarController.scheduleUpdate(session: self.session, source: self.text, line: Int32(line), column: Int32(column))
    }

    /// Replaces the word being completed with the text of a tapped completion chip and moves the cursor to
    /// just after it.
    ///
    /// - Parameters:
    ///   - item: The completion candidate.
    private func insertCompletion(_ item: CompletionItemInfo) {
        guard let cursor = self.position.selections.first else {
            return
        }
        
        let lastWordLength = self.completionBarController.lastWord.utf16.count
        let replaceRange = NSRange(location: cursor.location - lastWordLength, length: lastWordLength)
        let result = CompletionInserter.insert(item, into: self.text, at: replaceRange)
        self.text = result.text
        self.position.selections = [result.selection]
    }

    /// Executes a programme.
    private func runProgramme() {
        self.keyboardObserver.dismiss()
        self.onSave?()
        self.isOutputPresented = true
        Task {
            await self.runOrchestrator.run(session: self.session, source: self.text, args: self.arguments, stdin: self.stdinLines.joined(separator: "\n"))
        }
    }

    /// Stops a running programme.
    private func stopProgramme() {
        self.runOrchestrator.stop(session: self.session)
    }

    /// Formats the source text.
    private func formatSource() {
        Task {
            if let formattedText = await self.session.format(source: self.text) {
                self.text = formattedText
            }
        }
    }

    /// Moves the editor selection to a diagnostic location.
    ///
    /// Invoked by tapping a row in `IssuesListView`.
    ///
    /// - Parameters:
    ///   - location: The diagnostic location.
    private func navigateToDiagnostic(_ location: TextLocation) {
        let offset = Self.characterOffset(forZeroBasedLine: location.zeroBasedLine, column: location.zeroBasedColumn, in: self.text)
        self.position.selections = [NSRange(location: offset, length: 0)]
    }

    /// Converts a 0-indexed line/column into an absolute UTF-16 character offset in the `text`.
    ///
    /// - Parameters:
    ///   - line: The 0-indexed line.
    ///   - column: The 0-indexed column.
    ///   - text: The source text.
    ///
    /// - Returns: The absolute offset within the text.
    private static func characterOffset(forZeroBasedLine line: Int, column: Int, in text: String) -> Int {
        let lines = text.components(separatedBy: "\n")
        var offset = 0
        for index in 0..<line where index < lines.count {
            offset += lines[index].utf16.count + 1
        }
        
        return offset + column
    }

    /// Converts an absolute UTF-16 offset in `text` into a 0-indexed line/column.
    ///
    /// - Parameters:
    ///   - offset: The offset in the text.
    ///   - text: The source text.
    ///
    /// - Returns: The 0-indexed line and column.
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

/// Toolbar context used for callers with no extra toolbar items to add.
///
/// This is intentionally included in this file as it only relates to `EditorHostView`.
struct NoExtraToolbarContent: ToolbarContent {
    /// The view body.
    var body: some ToolbarContent {
        ToolbarItemGroup {}
    }
}

/// Extends `EditorHostView` when no extra toolbar content is required.
///
/// This is intentionally included in this file as it only relates to `EditorHostView`.
extension EditorHostView where ExtraToolbarContent == NoExtraToolbarContent {
    /// Creates the `EditorHostView` when no extra toolbar content is needed.
    ///
    /// - Parameters:
    ///   - text: The source text.
    ///   - documentURL: The file URL.
    ///   - session: The Topsy Turvy toolchain session.
    ///   - onSave: The handler for a save.
    init(text: Binding<String>, documentURL: URL?, session: TopsyTurvySession, onSave: (() -> Void)? = nil) {
        self.init(text: text, documentURL: documentURL, session: session, onSave: onSave) { NoExtraToolbarContent() }
    }
}
