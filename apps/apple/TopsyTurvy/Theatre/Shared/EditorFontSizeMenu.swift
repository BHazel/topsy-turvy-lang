import SwiftUI

/// A menu to display the font size presets for the `CodeEditorView`-powered code editor.
///
/// The font size is persisted in `@AppStorage`, as defined in `EditorHostView`.
struct EditorFontSizeMenu: View {
    /// The selected font size.
    @Binding var fontSize: Double

    /// The view body.
    var body: some View {
        Menu {
            ForEach(EditorFontSize.presets, id: \.self) { size in
                Button {
                    self.fontSize = size
                } label: {
                    if size == self.fontSize {
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
