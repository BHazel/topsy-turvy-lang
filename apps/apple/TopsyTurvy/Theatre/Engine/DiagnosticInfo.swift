/// Represents a single diagnostic, as serialised across the native export boundary.
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
