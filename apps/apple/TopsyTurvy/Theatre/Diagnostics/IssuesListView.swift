import LanguageSupport
import SwiftUI

/// Lists diagnostics reported by `TopsyTurvyLanguageService.diagnostics`, sorted top-to-bottom by location.
/// Tapping a row moves the editor's selection to that location.
struct IssuesListView: View {
    let diagnostics: Set<TextLocated<Message>>
    let onSelect: (TextLocation) -> Void

    var body: some View {
        List(sortedDiagnostics, id: \.rowIdentity) { item in
            Button {
                onSelect(item.location)
            } label: {
                HStack(alignment: .top, spacing: 8) {
                    Image(systemName: Self.iconName(for: item.entity.category))
                        .foregroundStyle(Self.color(for: item.entity.category))
                    VStack(alignment: .leading, spacing: 2) {
                        Text(item.entity.summary)
                        Text("Line \(item.location.oneBasedLine)")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }
            .buttonStyle(.plain)
        }
        .overlay {
            if diagnostics.isEmpty {
                ContentUnavailableView("No Issues", systemImage: "checkmark.circle")
            }
        }
    }

    private var sortedDiagnostics: [TextLocated<Message>] {
        Self.sorted(diagnostics)
    }

    /// Orders diagnostics top-to-bottom, left-to-right for a sane display — `Set<TextLocated<Message>>` has no
    /// stable order of its own. A free function, kept separate from the view body for direct unit testing.
    static func sorted(_ diagnostics: Set<TextLocated<Message>>) -> [TextLocated<Message>] {
        diagnostics.sorted { lhs, rhs in
            (lhs.location.zeroBasedLine, lhs.location.zeroBasedColumn) < (rhs.location.zeroBasedLine, rhs.location.zeroBasedColumn)
        }
    }

    private static func iconName(for category: Message.Category) -> String {
        switch category {
        case .error: "xmark.octagon.fill"
        case .warning: "exclamationmark.triangle.fill"
        case .hole: "circle.dashed"
        case .live: "bolt.fill"
        case .informational: "info.circle"
        }
    }

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

extension TextLocated<Message> {
    /// A row identity built from content, not `entity.id`.
    ///
    /// `Message.id` (`UUID()`, no memberwise override) is assigned fresh on every construction, so a
    /// logically-unchanged diagnostic gets a brand new, unrelated `id` on every debounced re-analysis pass —
    /// confirmed by reading `Message.swift` directly. Using it as `List`'s row identity made SwiftUI treat
    /// every row as freshly inserted on every edit rather than diffing in place, which on macOS specifically
    /// (unlike iOS) was severe enough to repeatedly rebuild the backing `NSTableView` while typing and steal
    /// first responder from the editor — the reported "cursor jumps to the end of the file" bug. A stable,
    /// content-derived identity means an unchanged diagnostic keeps the same identity across re-renders.
    var rowIdentity: String {
        "\(location.zeroBasedLine):\(location.zeroBasedColumn):\(entity.category):\(entity.summary)"
    }
}
