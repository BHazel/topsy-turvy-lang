/// Represents the result of a completion request, returned as JSON by `topsyturvy_tc_complete` from the Topsy Turvy toolchain.
struct CompletionResult: Decodable {
    /// The completion candidates, combining matching keywords and symbols.
    let Items: [CompletionItemInfo]
}
