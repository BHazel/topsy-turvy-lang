namespace BWHazel.TopsyTurvy.Embedded.Analysis;

/// <summary>
/// Represents a single diagnostic, as serialised across the native export boundary.
/// </summary>
/// <remarks>
/// The four location fields (<see cref="StartLine"/>, <see cref="StartColumn"/>, <see cref="EndLine"/>, <see cref="EndColumn"/>)
/// are 1-indexed, matching <see cref="BWHazel.TopsyTurvy.Ast.SourceSpan"/>/<see cref="BWHazel.TopsyTurvy.Ast.SourceLocation"/>
/// therefore no conversion is applied. The span is half-open: the end location is one column past the final character of the span.
/// This differs from <see cref="NativeExports.ToolchainExports.GetHover"/>/<see cref="NativeExports.ToolchainExports.GetCompletions"/>, whose <c>line</c>
/// and <c>column</c> parameters are 0-indexed to match the LSP convention already used elsewhere in the toolchain, therefore,
/// callers must not combine the two.
/// </remarks>
/// <param name="Message">The descriptive diagnostic message.</param>
/// <param name="Severity">The severity, one of <c>"Info"</c>, <c>"Warning"</c> or <c>"Error"</c>.</param>
/// <param name="StartLine">The 1-indexed line number of the span start.</param>
/// <param name="StartColumn">The 1-indexed column number of the span start.</param>
/// <param name="EndLine">The 1-indexed line number of the span end.</param>
/// <param name="EndColumn">The 1-indexed column number of the span end.</param>
public record DiagnosticInfo(
    string Message,
    string Severity,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn);
