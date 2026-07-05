import SwiftUI

/// Shared default and presets for the editor's user-adjustable font size, persisted via `@AppStorage`.
enum EditorFontSize {
    /// The default point size — larger than `CodeEditorView`'s own default, which reads very small on iPhone/iPad.
    static let `default`: Double = 16

    static let presets: [Double] = [12, 13, 14, 16, 18, 20, 24, 28]
}

/// A native `Menu` for the shared, `@AppStorage`-persisted editor font size — a stock control, unlike a
/// custom-drawn stepper or zoom bar, and a familiar idiom for this exact setting (e.g. Safari's own "Zoom
/// Text Only" size menu).
struct EditorFontSizeMenu: View {
    @Binding var fontSize: Double

    var body: some View {
        Menu {
            ForEach(EditorFontSize.presets, id: \.self) { size in
                Button {
                    fontSize = size
                } label: {
                    if size == fontSize {
                        Label("\(Int(size))pt", systemImage: "checkmark")
                    } else {
                        Text("\(Int(size))pt")
                    }
                }
            }
        } label: {
            Label("Font Size", systemImage: "textformat.size")
        }
    }
}
