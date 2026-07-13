import LanguageSupport
import SwiftUI

extension View {
    /// Presents diagnostics as an issues list, adapting to size class: a trailing inspector on regular
    /// width or a detented sheet on compact width.
    /// - Parameters:
    ///   - diagnostics: The diagnostics to display in the list.
    ///   - isPresented: A value indicating whether the inspector or sheet is shown.
    ///   - onSelect: Handler for when a diagnostic in the list is tapped.
    ///
    /// - Returns: A view.
    func issuesInspector(
        diagnostics: Set<TextLocated<Message>>,
        isPresented: Binding<Bool>,
        onSelect: @escaping (TextLocation) -> Void
    ) -> some View {
        modifier(IssuesInspectorModifier(diagnostics: diagnostics, isPresented: isPresented, onSelect: onSelect))
    }
}
