/// Represents the result of a hover request, returned as JSON by `topsyturvy_tc_hover` from the Topsy Turvy toolchain.
struct HoverResult: Decodable {
    /// A value indicating whether a symbol was found at the requested position.
    let Found: Bool

    /// The Markdown hover content for the symbol, or `nil` when `Found` is `false`.
    let MarkdownContent: String?
}
