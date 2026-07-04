/// Represents an analysis result returned by `topsyturvy_analyse` from the Topsy Turvy toolchain.
///
/// Mirrors `AnalysisResult` in `operetta/BWHazel.TopsyTurvy.Embedded/Analysis/AnalysisResult.cs`.
/// Property names match the JSON contract's PascalCase keys exactly, since `EmbeddedJsonContext` uses
/// no naming policy.
struct AnalysisResult: Decodable {
    /// A value indicating whether the analysis was successful.
    let Success: Bool

    /// The diagnostics reported by the analysis.
    let Diagnostics: [DiagnosticInfo]
}
