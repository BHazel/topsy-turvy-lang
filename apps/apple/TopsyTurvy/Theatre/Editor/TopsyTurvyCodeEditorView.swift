import CodeEditorView
import Combine
import LanguageSupport
import SwiftUI

/// Wraps `CodeEditorView.CodeEditor`, bound to a document's text, configured for Topsy Turvy highlighting,
/// live diagnostics and semantic tokens via `languageService`. The minimap is disabled: it claims meaningful
/// width on its own even before accounting for the gutter, which is disproportionately costly on an
/// iPhone-width screen where every point of editing width matters more than a birds-eye overview of a
/// typically short programme.
///
/// `position` is owned by the caller (`WorkspaceEditorHostView`), not this view, so that external UI — the
/// iOS on-demand completion toolbar button and the issues list's row-tap-to-navigate — can both read the
/// current selection and move it.
struct TopsyTurvyCodeEditorView: View {
    @Binding var text: String
    @Binding var position: CodeEditor.Position
    let session: TopsyTurvySession
    let languageService: TopsyTurvyLanguageService?
    var fontSize: CGFloat = 14

    @State private var messages: Set<TextLocated<Message>> = []
    @Environment(\.colorScheme) private var colorScheme

    @State private var hoverContent: String?
    @State private var isHoverPresented = false

    var body: some View {
        CodeEditor(text: $text, position: $position, messages: $messages, language: .topsyTurvy(languageService: languageService))
            .environment(\.codeEditorTheme, theme)
            .environment(\.codeEditorLayoutConfiguration, CodeEditor.LayoutConfiguration(showMinimap: false, wrapText: true))
            // `CodeEditorView` already sinks `languageService.diagnostics` into its own internal `messages`
            // state once a language service is attached (`CodeView.startLanguageService`), which should make
            // this subscription redundant. It's kept as a belt-and-suspenders safeguard pending a live visual
            // check (no duplicate/flickering squiggles) that the internal wiring alone is sufficient — see
            // DEVELOPMENT.md. A harmless no-op `Empty` publisher stands in before `languageService` exists.
            .onReceive(languageService?.diagnostics.eraseToAnyPublisher() ?? Empty().eraseToAnyPublisher()) { newMessages in
                messages = newMessages
            }
            // The single source of truth for re-analysis: `LanguageService.documentDidChange`'s own `newText`
            // parameter is only the edited-range fragment, not the whole document (confirmed by reading
            // `CodeStorageDelegate.swift`), so `TopsyTurvyLanguageService` cannot reliably use it. `text` here
            // is guaranteed to already be the correct, full post-edit document whenever this fires.
            .onChange(of: text) { _, newValue in
                Task { await languageService?.updateText(newValue) }
            }
            // Hover has no automatic path on iOS/iPadOS: `CodeEditorView`'s `info(at:)` popover is AppKit-only
            // (`CodeActions.swift`'s iOS/visionOS branch is an unimplemented upstream stub), so this gesture
            // builds the equivalent by hand. A tap already moves the underlying `UITextView`'s selection
            // before a long-press gesture fires, so `position.selections.first?.location` is already the
            // tapped character index — no custom point-to-index hit-testing is needed.
            .onLongPressGesture {
                performHover()
            }
            .popover(isPresented: $isHoverPresented) {
                TopsyTurvyHoverPopoverView(markdown: hoverContent ?? "")
            }
    }

    /// The active highlighting theme, derived from the current colour scheme with `fontSize` applied — lets
    /// the user adjust text size (particularly useful on iPhone, where the default is small).
    private var theme: Theme {
        var theme = colorScheme == .dark ? Theme.defaultDark : Theme.defaultLight
        theme.fontSize = fontSize
        return theme
    }

    /// Fetches hover content for the current selection's location and presents it in a popover.
    private func performHover() {
        guard let languageService, let location = position.selections.first?.location else { return }
        Task {
            if let content = await languageService.hoverContent(at: location) {
                hoverContent = content
                isHoverPresented = true
            }
        }
    }
}
