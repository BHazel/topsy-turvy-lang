using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Embedded.Analysis;

/// <summary>
/// Represents the result of a tokenise request, returned as JSON by <see cref="NativeExports.ToolchainExports.GetTokens"/>.
/// </summary>
/// <param name="Tokens">The recognised token spans, in source order.</param>
public record TokenResult(IReadOnlyList<TokenInfo> Tokens);
