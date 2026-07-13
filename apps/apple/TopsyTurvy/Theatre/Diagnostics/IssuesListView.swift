import LanguageSupport
import SwiftUI

/// Lists diagnostics for a Topsy Turvy source file sorted top-to-bottom by location.
///
/// Issues are reported by `TopsyTurvyLanguageService.diagnostics`; tapping a row moves the editor selection
/// to that location.
struct IssuesListView: View {
    /// The reported diagnostics.
    let diagnostics: Set<TextLocated<Message>>
    
    /// The handler for selecting an issue in the list.
    let onSelect: (TextLocation) -> Void

    /// The view body.
    var body: some View {
        List(self.sortedDiagnostics, id: \.self.rowIdentity) { diagnostic in
            Button {
                self.onSelect(diagnostic.location)
            } label: {
                HStack(alignment: .top, spacing: 8) {
                    Image(systemName: Self.iconName(for: diagnostic.entity.category))
                        .foregroundStyle(Self.color(for: diagnostic.entity.category))
                    
                    VStack(alignment: .leading, spacing: 2) {
                        Text(diagnostic.entity.summary)
                        Text("Line \(diagnostic.location.oneBasedLine)")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }
            .buttonStyle(.plain)
        }
        .overlay {
            if self.diagnostics.isEmpty {
                ContentUnavailableView("No Issues", systemImage: "checkmark.circle")
            }
        }
    }

    /// Sorts the diagnostics.
    ///
    /// - Returns: The sorted diagnostics.
    private var sortedDiagnostics: [TextLocated<Message>] {
        Self.sorted(diagnostics)
    }

    /// Orders diagnostics by line and column.
    ///
    /// `Set<TextLocated<Message>>` has no stable order of its own.
    ///
    /// - Parameters:
    ///   - diagnostics: The diagnostics.
    ///
    /// - Returns: The sorted diagnostics.
    static func sorted(_ diagnostics: Set<TextLocated<Message>>) -> [TextLocated<Message>] {
        diagnostics.sorted { lhs, rhs in
            (lhs.location.zeroBasedLine, lhs.location.zeroBasedColumn) < (rhs.location.zeroBasedLine, rhs.location.zeroBasedColumn)
        }
    }

    /// Gets the system icon for a diagnostic category.
    ///
    /// - Parameters:
    ///   - category: The diagnostic category.
    ///
    /// - Returns: The system icon name.
    private static func iconName(for category: Message.Category) -> String {
        switch category {
        case .error: "xmark.octagon.fill"
        case .warning: "exclamationmark.triangle.fill"
        case .hole: "circle.dashed"
        case .live: "bolt.fill"
        case .informational: "info.circle"
        }
    }

    /// Gets the colour for a diagnostic category.
    ///
    /// - Parameters:
    ///   - category: The diagnostic category.
    ///
    /// - Returns: The colour.
    private static func color(for category: Message.Category) -> Color {
        switch category {
        case .error: .red
        case .warning: .yellow
        case .hole: .purple
        case .live: .blue
        case .informational: .secondary
        }
    }
}
