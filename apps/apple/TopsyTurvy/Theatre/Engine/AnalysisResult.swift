/// Represents an analysis result returned as JSON by `topsyturvy_analyse` from the Topsy Turvy toolchain.
struct AnalysisResult: Decodable {
    /// A value indicating whether the analysis was successful.
    let Success: Bool

    /// The diagnostics reported by the analysis.
    let Diagnostics: [DiagnosticInfo]
}
