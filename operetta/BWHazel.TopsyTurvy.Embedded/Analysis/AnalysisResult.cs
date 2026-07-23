using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Embedded.Analysis;

/// <summary>
/// Represents the result of a parse and type-check pass, returned as JSON by <see cref="NativeExports.ToolchainExports.AnalyseSource"/>.
/// </summary>
/// <param name="Success">A value indicating whether parsing and type-checking both succeeded with no error-severity diagnostics.</param>
/// <param name="Diagnostics">The diagnostics produced by parsing and type-checking, in the order produced.</param>
public record AnalysisResult(bool Success, IReadOnlyList<DiagnosticInfo> Diagnostics);
