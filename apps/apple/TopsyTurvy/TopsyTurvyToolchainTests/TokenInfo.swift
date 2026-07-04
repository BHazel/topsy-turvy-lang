/// Represents a categorised token span returned by `topsyturvy_tokens` from the Topsy Turvy toolchain.
///
/// Mirrors `TokenInfo` in `operetta/BWHazel.TopsyTurvy.Embedded/Analysis/TokenInfo.cs`. Property names match
/// the JSON contract's PascalCase keys exactly, since `EmbeddedJsonContext` uses no naming policy. The four
/// location fields are 1-indexed and half-open, matching `DiagnosticInfo`'s convention rather than
/// `topsyturvy_hover`/`topsyturvy_complete`'s 0-indexed LSP convention.
struct TokenInfo: Decodable {
    /// One of "comment", "string", "number", "variable", "keyword", "type", "keywordOther" or "identifier".
    let Category: String

    /// The 1-indexed line number of the span start.
    let StartLine: Int

    /// The 1-indexed column number of the span start.
    let StartColumn: Int

    /// The 1-indexed line number of the span end.
    let EndLine: Int

    /// The 1-indexed column number of the span end.
    let EndColumn: Int
}
