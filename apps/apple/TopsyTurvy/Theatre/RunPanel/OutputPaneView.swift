import SwiftUI

/// Displays the outputfrom a programme execution, in a monospaced, scrolling pane.
struct OutputPaneView: View {
    /// The output lines.
    let lines: [String]

    /// The view body.
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 2) {
                ForEach(Array(self.lines.enumerated()), id: \.offset) { _, line in
                    Text(line)
                        .font(.system(.body, design: .monospaced))
                        .frame(maxWidth: .infinity, alignment: .leading)
                }
            }
            .padding(8)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
        .background(Color(.secondarySystemBackground))
        .accessibilityIdentifier("OutputPaneView.scrollView")
    }
}
