import SwiftUI

/// Renders hover Markdown content in a compact popover, for `TopsyTurvyCodeEditorView`'s iOS/iPadOS long-press
/// hover gesture.
///
/// `CodeEditorView` only automates hover via its own `InfoPopover` (`CodeActions.swift`) on macOS, which this
/// app does not target — its iOS/visionOS `CodeActions` branch is an unimplemented upstream stub — so this
/// view builds the equivalent by hand for iOS/iPadOS.
struct TopsyTurvyHoverPopoverView: View {
    let markdown: String

    var body: some View {
        ScrollView {
            Text(.init(markdown))
                .padding()
                .frame(minWidth: 200, maxWidth: 320, alignment: .leading)
                .fixedSize(horizontal: false, vertical: true)
        }
        .frame(maxHeight: 240)
    }
}
