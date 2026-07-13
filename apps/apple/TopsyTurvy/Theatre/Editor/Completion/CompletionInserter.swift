import Foundation

/// Splices a completion candidate text into a buffer at a given range.
enum CompletionInserter {
    /// Replaces the specified range in the provided text with the completion item `InsertText`.
    ///
    /// The caller decides what `range` to replace, usually the word being completed, not just the cursor
    /// position since `InsertText` is meant to fully replace that word, not be appended after it.
    ///
    /// - Parameters:
    ///   - completionItem: The completion candidate to insert.
    ///   - text: The buffer to splice into.
    ///   - range: The range, in UTF-16 code units, to replace.
    ///
    /// - Returns: The updated text and the cursor position immediately after the inserted text.
    static func insert(_ completionItem: CompletionItemInfo, into text: String, at range: NSRange) -> (text: String, selection: NSRange) {
        let buffer = text as NSString
        let updatedBuffer = buffer.replacingCharacters(in: range, with: completionItem.InsertText)
        let cursorLocation = range.location + (completionItem.InsertText as NSString).length
        return (updatedBuffer, NSRange(location: cursorLocation, length: 0))
    }
}
