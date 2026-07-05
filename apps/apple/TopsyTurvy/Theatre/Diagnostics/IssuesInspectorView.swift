import LanguageSupport
import SwiftUI

/// Presents `IssuesListView`, adapting to size class per `THEATRE_DESIGN.md` §4: a trailing `.inspector` on
/// regular-width screens (iPad), or a detented sheet on compact-width screens (iPhone).
private struct IssuesInspectorModifier: ViewModifier {
    let diagnostics: Set<TextLocated<Message>>
    @Binding var isPresented: Bool
    let onSelect: (TextLocation) -> Void

    @Environment(\.horizontalSizeClass) private var horizontalSizeClass

    func body(content: Content) -> some View {
        if horizontalSizeClass == .compact {
            content
                .sheet(isPresented: $isPresented) {
                    NavigationStack {
                        IssuesListView(diagnostics: diagnostics, onSelect: onSelect)
                            .navigationTitle("Issues")
                            .navigationBarTitleDisplayMode(.inline)
                    }
                    .presentationDetents([.medium, .large])
                }
        } else {
            content
                .inspector(isPresented: $isPresented) {
                    IssuesListView(diagnostics: diagnostics, onSelect: onSelect)
                        .inspectorColumnWidth(min: 220, ideal: 280, max: 400)
                }
        }
    }
}

extension View {
    /// Presents `diagnostics` as an issues list, adapting to size class — a trailing inspector on regular
    /// width, a detented sheet on compact width.
    /// - Parameter diagnostics: The diagnostics to list.
    /// - Parameter isPresented: Whether the inspector/sheet is shown.
    /// - Parameter onSelect: Called with a diagnostic's location when its row is tapped.
    func issuesInspector(
        diagnostics: Set<TextLocated<Message>>,
        isPresented: Binding<Bool>,
        onSelect: @escaping (TextLocation) -> Void
    ) -> some View {
        modifier(IssuesInspectorModifier(diagnostics: diagnostics, isPresented: isPresented, onSelect: onSelect))
    }
}
