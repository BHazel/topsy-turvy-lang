import SwiftUI

/// Renders hover Markdown content in a compact popover, for a long-press hover gesture on iOS/iPadOS.
///
/// `CodeEditorView` only automates hover on macOS hence this custom functionality to support iOS/iPadOS.
struct TopsyTurvyHoverPopoverView: View {
    /// The Mardown content.
    let markdown: String

    /// The view body.
    var body: some View {
        ScrollView {
            Text(.init(self.markdown))
                .padding()
                .frame(minWidth: 200, maxWidth: 320, alignment: .leading)
                .fixedSize(horizontal: false, vertical: true)
        }
        .frame(maxHeight: 240)
    }
}
