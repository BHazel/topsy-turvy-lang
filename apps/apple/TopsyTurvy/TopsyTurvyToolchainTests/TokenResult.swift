/// Represents the result of a `topsyturvy_tokens` request from the Topsy Turvy toolchain.
///
/// Mirrors `TokenResult` in `operetta/BWHazel.TopsyTurvy.Embedded/Analysis/TokenResult.cs`. Property names
/// match the JSON contract's PascalCase keys exactly, since `EmbeddedJsonContext` uses no naming policy.
struct TokenResult: Decodable {
    /// The recognised token spans, in source order.
    let Tokens: [TokenInfo]
}
