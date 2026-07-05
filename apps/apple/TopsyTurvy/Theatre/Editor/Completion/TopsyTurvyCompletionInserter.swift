import Foundation

/// Splices a completion candidate's insert text into a buffer at the current cursor position — a pure
/// function, directly unit-tested, with no dependency on the editor or the native engine.
enum TopsyTurvyCompletionInserter {
    /// Replaces `range` in `text` with `item.InsertText`, since `InsertText` may be only the not-yet-typed
    /// suffix of `item.Label` (the caller's `range` is expected to be the current zero-length cursor position,
    /// not a selection spanning already-typed characters).
    /// - Parameters:
    ///   - item: The completion candidate to insert.
    ///   - text: The buffer to splice into.
    ///   - range: The `NSRange` (UTF-16 code units, matching `NSString`/`NSRange` convention) to replace.
    /// - Returns: The updated text, and the zero-length selection the cursor should advance to, immediately
    ///   after the inserted text.
    static func insert(_ item: CompletionItemPayload, into text: String, at range: NSRange) -> (text: String, selection: NSRange) {
        let buffer = text as NSString
        let updated = buffer.replacingCharacters(in: range, with: item.InsertText)
        let cursorLocation = range.location + (item.InsertText as NSString).length
        return (updated, NSRange(location: cursorLocation, length: 0))
    }
}
