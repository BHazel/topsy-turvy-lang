import SwiftUI

/// Renders hover Markdown content in a compact popover, for `TopsyTurvyCodeEditorView`'s iOS/iPadOS long-press
/// hover gesture.
///
/// On macOS, hover is handled automatically by `CodeEditorView`'s own `InfoPopover` (`CodeActions.swift`,
/// AppKit-only) via `TopsyTurvyLanguageService.info(at:)` — this view exists purely for the platforms that
/// mechanism doesn't reach, since the package's iOS/visionOS `CodeActions` branch is an unimplemented stub.
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
