import CodeEditorView
import Combine
import LanguageSupport
import SwiftUI

/// The main per-document window: editor above, output below. The divider is a plain dark hairline that can
/// be dragged to resize the output pane — visually just a separator, with a generous invisible hit area,
/// rather than a drawn "handle". On compact-width screens (iPhone, or a narrow iPad multitasking pane) the
/// output pane also hides itself while the keyboard is visible, since the docked pane and the keyboard would
/// otherwise compete for the limited space above it, leaving very little room to actually type; regular-width
/// iPad keeps output visible while typing, since there's enough headroom above the keyboard there for it not
/// to matter.
///
/// `PRAY ADMIT` resolves a sibling filename directly from this document's own on-disk directory, so a second
/// file never needs to be explicitly "opened into" this window to be importable — only present alongside it
/// on disk.
struct TheatreDocumentView: View {
    @Binding var text: String
    let documentURL: URL?

    @State private var session = TopsyTurvySession()
    @State private var languageService: TopsyTurvyLanguageService?
    @State private var runViewModel = RunSessionViewModel()
    @State private var args: [String] = []
    @State private var stdinLines: [String] = []
    @State private var isInputsPresented = false
    @State private var position = CodeEditor.Position()
    @State private var diagnostics: Set<TextLocated<Message>> = []
    @AppStorage("editorFontSize") private var fontSize: Double = EditorFontSize.default

    @State private var isIssuesListVisible = false
    @State private var outputHeight: CGFloat = 200
    @State private var outputHeightAtDragStart: CGFloat?
    @StateObject private var keyboardObserver = KeyboardObserver()
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass

    /// Whether the output pane should be hidden for the keyboard: only when the keyboard is visible *and* the
    /// window is compact-width (iPhone, or an iPad multitasking pane narrow enough to behave like one — Slide
    /// Over, or a narrow Split View/Stage Manager tile). Regular-width iPad (full screen or a wide Split View
    /// pane) has enough vertical headroom above the keyboard that hiding output there sacrifices screen space
    /// for no layout benefit, unlike the genuinely cramped compact-width case.
    private var shouldHideOutputForKeyboard: Bool {
        keyboardObserver.isKeyboardVisible && horizontalSizeClass == .compact
    }

    var body: some View {
        content
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
                    issuesListToggleButton
                }
                ToolbarItemGroup {
                    EditorFontSizeMenu(fontSize: $fontSize)
                }
                ToolbarItemGroup {
                    OpenFolderButton()
                }
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
                await session.open()
                await session.setImportResolver { [documentURL] filename in
                    Self.resolveSiblingImport(filename, relativeTo: documentURL)
                }
                languageService = TopsyTurvyLanguageService(session: session)
            }
            .onDisappear {
                Task { await session.close() }
            }
            // `languageService` starts `nil` until the `.task` above populates it; `Empty` stands in until then.
            .onReceive(languageService?.diagnostics.eraseToAnyPublisher() ?? Empty().eraseToAnyPublisher()) { newDiagnostics in
                diagnostics = newDiagnostics
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
        .accessibilityIdentifier("TheatreDocumentView.formatButton")
    }

    /// Toggles the issues pane's visibility. Hidden by default, since the output pane already fights the
    /// keyboard for space — a third always-visible pane would leave even less room to type.
    private var issuesListToggleButton: some View {
        Button {
            isIssuesListVisible.toggle()
        } label: {
            Label("Issues", systemImage: "exclamationmark.triangle")
        }
        .accessibilityIdentifier("TheatreDocumentView.issuesListToggleButton")
    }

    private var content: some View {
        VStack(spacing: 0) {
            editor
                .frame(maxHeight: .infinity)

            // Hidden while the keyboard is up: on iPhone especially, the docked output pane otherwise leaves
            // very little room to actually type once the keyboard and output are both competing for the
            // remaining space above it. Reappears as soon as the keyboard is dismissed.
            if !shouldHideOutputForKeyboard {
                outputDivider
                OutputPaneView(lines: runViewModel.outputLines)
                    .frame(height: outputHeight)
                    .transition(.move(edge: .bottom).combined(with: .opacity))

                if isIssuesListVisible {
                    Divider()
                    IssuesListView(diagnostics: diagnostics, onSelect: navigateToDiagnostic)
                        .frame(height: 160)
                        .transition(.move(edge: .bottom).combined(with: .opacity))
                }
            }
        }
        .animation(.easeInOut(duration: 0.2), value: shouldHideOutputForKeyboard)
        .animation(.easeInOut(duration: 0.2), value: isIssuesListVisible)
    }

    /// A dark hairline separating editor and output, draggable to resize the output pane.
    private var outputDivider: some View {
        Rectangle()
            .fill(Color(.opaqueSeparator))
            .frame(height: 2)
            .frame(height: 16)
            .contentShape(Rectangle())
            .gesture(
                DragGesture()
                    .onChanged { value in
                        let startHeight = outputHeightAtDragStart ?? outputHeight
                        outputHeightAtDragStart = startHeight
                        outputHeight = min(max(startHeight - value.translation.height, 80), 600)
                    }
                    .onEnded { _ in
                        outputHeightAtDragStart = nil
                    }
            )
    }

    private var editor: some View {
        TopsyTurvyCodeEditorView(text: $text, position: $position, session: session, languageService: languageService, fontSize: fontSize)
    }

    /// Resolves a `PRAY ADMIT` filename against the directory containing this window's own document, mirroring
    /// how the interpreter itself resolves imports when no custom resolver is supplied.
    private static func resolveSiblingImport(_ filename: String, relativeTo documentURL: URL?) -> String? {
        guard let documentURL else { return nil }
        let siblingURL = documentURL.deletingLastPathComponent().appendingPathComponent(filename)
        return try? String(contentsOf: siblingURL, encoding: .utf8)
    }

    private func runProgramme() {
        // So the output pane (hidden while typing) reappears without an extra tap to dismiss the keyboard.
        keyboardObserver.dismiss()
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
