using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Represents the outcome of resolving signature help at a cursor position.
/// </summary>
/// <param name="Signatures">The declared overloads of the called function, in declaration order.</param>
/// <param name="ActiveSignature">The index, within <paramref name="Signatures"/>, of the overload to highlight.</param>
/// <param name="ActiveParameter">The index of the parameter currently being typed, within the active signature.</param>
public sealed record SignatureHelpResult(IReadOnlyList<SignatureCandidate> Signatures, int ActiveSignature, int ActiveParameter);
