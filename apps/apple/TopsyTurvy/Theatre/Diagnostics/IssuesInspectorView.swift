import LanguageSupport
import SwiftUI

/// Defines a modifier to present a list view for diagnostic issues.
struct IssuesInspectorModifier: ViewModifier {
    /// The diagnostics.
    let diagnostics: Set<TextLocated<Message>>
    
    /// A value indicating whether the view is currently presented.
    @Binding var isPresented: Bool
    
    /// The handler for selecting an issue in the list.
    let onSelect: (TextLocation) -> Void

    /// The horizontal size class, from the environment.
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass

    /// Returns the view modifier body.
    ///
    /// - Parameters:
    ///   - content: The view content.
    ///
    /// - Returns: The view modifier body.
    func body(content: Content) -> some View {
        if horizontalSizeClass == .compact {
            content
                .sheet(isPresented: self.$isPresented) {
                    NavigationStack {
                        IssuesListView(diagnostics: self.diagnostics, onSelect: self.onSelect)
                            .navigationTitle("Issues")
                            .navigationBarTitleDisplayMode(.inline)
                            .toolbar {
                                ToolbarItem(placement: .topBarTrailing) {
                                    Button("Close") {
                                        isPresented = false
                                    }
                                }
                            }
                    }
                    .presentationDetents([.medium, .large])
                }
        } else {
            content
                .inspector(isPresented: self.$isPresented) {
                    IssuesListView(diagnostics: self.diagnostics, onSelect: self.onSelect)
                        .inspectorColumnWidth(min: 220, ideal: 280, max: 400)
                }
        }
    }
}
