import SwiftUI

/// A horizontally scrolling row of completion candidate chips, always present (even with no candidates) so
/// its place in the layout stays fixed rather than popping in and out — an always-visible fixture, iOS
/// QuickType/Pythonista style, not a popup.
struct CompletionBarView: View {
    let candidates: [CompletionItemPayload]
    let onSelect: (CompletionItemPayload) -> Void

    var body: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                ForEach(candidates.indices, id: \.self) { index in
                    chip(for: candidates[index])
                }
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 6)
        }
        .frame(height: 40)
        .background(.bar)
        .accessibilityIdentifier("CompletionBarView")
    }

    private func chip(for item: CompletionItemPayload) -> some View {
        Button {
            onSelect(item)
        } label: {
            Text(item.Label)
                .font(.system(.footnote, design: .monospaced))
                .fontWeight(.medium)
                .padding(.horizontal, 10)
                .padding(.vertical, 4)
                .foregroundStyle(Self.color(for: item.Kind))
                .background(Self.color(for: item.Kind).opacity(0.18), in: Capsule())
        }
        .buttonStyle(.plain)
    }

    /// Kind-to-colour mapping, reusing `IssuesListView`'s curated category-colour palette rather than
    /// inventing new hues — red is deliberately skipped here, since it reads as "error" elsewhere in this app.
    private static func color(for kind: String) -> Color {
        switch kind {
        case "Keyword": .purple
        case "Function": .blue
        case "Variable": .yellow
        case "Parameter": .secondary
        default: .secondary
        }
    }
}
