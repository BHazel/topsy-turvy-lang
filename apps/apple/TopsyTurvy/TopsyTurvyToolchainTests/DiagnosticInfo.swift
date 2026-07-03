/// Represents a diagnostic reported by `topsyturvy_analyse` from the Topsy Turvy toolchain.
///
/// Mirrors `DiagnosticInfo` in `operetta/BWHazel.TopsyTurvy.Embedded/Analysis/DiagnosticInfo.cs`.
/// Property names match the JSON contract's PascalCase keys exactly, since `EmbeddedJsonContext` uses
/// no naming policy.
struct DiagnosticInfo: Decodable {
    /// The diagnostic message.
    let Message: String

    /// The severity of the diagnostic, one of "Info", "Warning", or "Error".
    let Severity: String

    /// The 1-indexed line number of the span start.
    let StartLine: Int

    /// The 1-indexed column number of the span start.
    let StartColumn: Int

    /// The 1-indexed line number of the span end.
    let EndLine: Int

    /// The 1-indexed column number of the span end.
    let EndColumn: Int
}
