/// Shared default and presets for the user-adjustable font size of the editor, persisted via `@AppStorage`.
enum EditorFontSize {
    /// The default font size.
    ///
    /// This is larger than the `CodeEditorView` default, which reads very small on the iPhone and iPad.
    static let `default`: Double = 16

    /// The font size option presets.
    static let presets: [Double] = [12, 13, 14, 16, 18, 20, 24, 28]
}
