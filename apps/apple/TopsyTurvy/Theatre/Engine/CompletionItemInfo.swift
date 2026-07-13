/// Represents a single completion candidate, as serialised across the native export boundary.
struct CompletionItemInfo: Decodable {
    /// The display label for the candidate.
    let Label: String

    /// The candidate kind.
    ///
    /// This can be one of `"Keyword"`, `"Variable"`, `"Function"` or `"Parameter"`.
    let Kind: String

    /// The additional detail text.
    ///
    /// This can include type name or parameter list, or `nil` when none applies.
    let Detail: String?

    /// The text to insert, which may be a suffix of `Label` when part of it has already been typed.
    let InsertText: String
}
