/// Represents a single completion candidate, as serialised across the native export boundary.
///
/// Mirrors `CompletionItemPayload` in `operetta/BWHazel.TopsyTurvy.Embedded/Analysis/CompletionItemPayload.cs`.
struct CompletionItemPayload: Decodable {
    /// The display label for the candidate.
    let Label: String

    /// The candidate kind, one of `"Keyword"`, `"Variable"`, `"Function"` or `"Parameter"`.
    let Kind: String

    /// Additional detail text, such as a type name or parameter list, or `nil` when none applies.
    let Detail: String?

    /// The text to insert, which may be a suffix of `Label` when part of it has already been typed.
    let InsertText: String
}
