/// Represents the result of a completion request, returned as JSON by `topsyturvy_complete`.
///
/// Mirrors `CompletionResult` in `operetta/BWHazel.TopsyTurvy.Embedded/Analysis/CompletionResult.cs`.
struct CompletionResult: Decodable {
    /// The completion candidates, combining matching keywords and symbols.
    let Items: [CompletionItemPayload]
}
