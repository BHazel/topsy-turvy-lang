namespace BWHazel.TopsyTurvy.Embedded.Analysis;

/// <summary>
/// Represents the result of a hover request, returned as JSON by <see cref="NativeExports.ToolchainExports.GetHover"/>.
/// </summary>
/// <param name="Found">A value indicating whether a symbol was found at the requested position.</param>
/// <param name="MarkdownContent">The Markdown hover content for the symbol, or <see langword="null"/> when <paramref name="Found"/> is <c>false</c>.</param>
public record HoverResult(bool Found, string? MarkdownContent);
