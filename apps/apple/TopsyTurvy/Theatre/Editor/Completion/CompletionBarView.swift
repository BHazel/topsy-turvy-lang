import SwiftUI

/// A horizontally scrolling row of completion candidate chips, always present even with no candidates.
struct CompletionBarView: View {
    /// The completion candidates.
    let candidates: [CompletionItemInfo]
    
    /// The handler for when a completion candidate is selected.
    let onSelect: (CompletionItemInfo) -> Void

    /// The view body.
    var body: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                ForEach(self.candidates.indices, id: \.self) { index in
                    self.chip(for: self.candidates[index])
                }
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 6)
        }
        .frame(height: 40)
        .background(.bar)
        .accessibilityIdentifier("CompletionBarView")
    }

    /// Creates a chip for a completion candidate.
    ///
    /// - Parameters:
    ///   - completionItem: The completion candidate.
    ///
    /// - Returns: A chip for a completion item.
    private func chip(for completionItem: CompletionItemInfo) -> some View {
        Button {
            self.onSelect(completionItem)
        } label: {
            Text(completionItem.Label)
                .font(.system(.footnote, design: .monospaced))
                .fontWeight(.medium)
                .padding(.horizontal, 10)
                .padding(.vertical, 4)
                .foregroundStyle(Self.colour(for: completionItem.Kind))
                .background(Self.colour(for: completionItem.Kind).opacity(0.18), in: Capsule())
        }
        .buttonStyle(.plain)
    }
    
    /// Gets the colour for the kind of completion item.
    ///
    /// - Parameters:
    ///   - kind: The kind of completion item.
    ///
    /// - Returns: The colour for the kind of completion item.
    private static func colour(for kind: String) -> Color {
        switch kind {
            case "Keyword": .purple
            case "Function": .blue
            case "Variable": .yellow
            case "Parameter": .secondary
            default: .secondary
        }
    }
}
