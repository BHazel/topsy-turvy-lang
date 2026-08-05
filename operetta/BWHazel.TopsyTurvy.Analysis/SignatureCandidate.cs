using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Represents one function overload as a signature-help candidate.
/// </summary>
/// <param name="Label">The full display label, e.g. <c>"describe(value AS A PEER) TO FIND YARN"</c>.</param>
/// <param name="ParameterLabels">The display label of each parameter, in declaration order, each a substring of <paramref name="Label"/>.</param>
public sealed record SignatureCandidate(string Label, IReadOnlyList<string> ParameterLabels);
