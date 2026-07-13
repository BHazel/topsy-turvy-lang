import CodeEditorView
import Combine
import LanguageSupport
import SwiftUI

/// Wraps `CodeEditorView.CodeEditor` bound to the text of a source file, configured for Topsy Turvy highlighting,
/// live diagnostics and semantic tokens via the custom Topsy Turvy language service.
///
/// The minimap is disabled: on an iPhone-width screen, every point of editing width matters more than a
/// birds-eye overview of the programme.
struct TopsyTurvyCodeEditorView: View {
    /// The current diagnostics shown as inline messages in the editor.
    @State private var messages: Set<TextLocated<Message>> = []

    /// The Markdown content of the currently presented hover popover.
    @State private var hoverContent: String?

    /// A value indicating whether the hover popover is presented.
    @State private var isHoverPresented = false

    /// The colour scheme, from the environment.
    @Environment(\.colorScheme) private var colorScheme
    
    /// The source text.
    @Binding var text: String
    
    /// The position in the source text.
    @Binding var position: CodeEditor.Position
    
    /// The session to interact with the Topsy Turvy toolchain.
    let session: TopsyTurvySession
    
    /// The `CodeEditorView` Topsy Turvy language service.
    let languageService: TopsyTurvyLanguageService?
    
    /// The font size.
    ///
    /// This is set to `14` by default.
    var fontSize: CGFloat = 14

    /// The view body.
    var body: some View {
        CodeEditor(text: self.$text, position: self.$position, messages: self.$messages, language: .topsyTurvy(languageService: languageService))
            .environment(\.codeEditorTheme, self.theme)
            .environment(\.codeEditorLayoutConfiguration, CodeEditor.LayoutConfiguration(showMinimap: false, wrapText: true))
            .onReceive(self.languageService?.diagnostics.eraseToAnyPublisher() ?? Empty().eraseToAnyPublisher()) { newMessages in
                self.messages = newMessages
            }
            .onChange(of: self.text) { _, newValue in
                Task {
                    await self.languageService?.updateText(newValue)
                }
            }
            .simultaneousGesture(LongPressGesture().onEnded { _ in
                self.performHover()
            })
            .popover(isPresented: self.$isHoverPresented) {
                TopsyTurvyHoverPopoverView(markdown: self.hoverContent ?? "")
            }
    }

    /// The active highlighting theme derived from the system theme.
    private var theme: Theme {
        var theme = self.colorScheme == .dark
            ? Theme.defaultDark
            : Theme.defaultLight
        
        theme.fontSize = fontSize
        return theme
    }

    /// Fetches hover content for the current selection location and presents it in a popover.
    private func performHover() {
        guard let languageService, let location = position.selections.first?.location else {
            return
        }
        
        Task {
            if let content = await languageService.hoverContent(at: location) {
                self.hoverContent = content
                self.isHoverPresented = true
            }
        }
    }
}
