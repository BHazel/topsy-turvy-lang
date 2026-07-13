import LanguageSupport

/// Row-identity support for diagnostics.
extension TextLocated<Message> {
    /// A row identity derived from content, not `entity.id`.
    ///
    /// `Message.id` is a fresh `UUID()` on every construction, so two diagnostics with identical content get
    /// different IDs on every re-analysis pass, which made SwiftUI treat every row in the `List` as freshly
    /// inserted instead of diffing them.
    ///
    /// - Returns: The row identity.
    var rowIdentity: String {
        "\(location.zeroBasedLine):\(location.zeroBasedColumn):\(entity.category):\(entity.summary)"
    }
}
