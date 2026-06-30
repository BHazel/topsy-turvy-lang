namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Carries the output of a single pre-processing pass.
/// </summary>
/// <remarks>
/// When the pre-processing pipeline is executed, each pre-processor transforms the source code storing the result in a
/// <see cref="PreProcessResult"/>.  Each ccontains the transformed text and a <see cref="SourceMap"/> that records how positions
/// in the transformed text correspond to positions in the original source code.
/// </remarks>
/// <param name="TransformedText">The transformed text.</param>
/// <param name="SourceMap">The source map that tracks offsets in the transformed text back to the original source.</param>
public record PreProcessResult(string TransformedText, SourceMap SourceMap);
