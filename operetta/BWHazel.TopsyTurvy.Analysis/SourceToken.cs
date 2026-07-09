using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Represents one categorised token span produced by <see cref="SourceTokeniser.Tokenise"/>.
/// </summary>
/// <param name="Category">The category of the token.</param>
/// <param name="Span">The source span the token occupies.</param>
/// <remarks>
/// A token can have 1 of the following categories:
/// * <c>comment</c>
/// * <c>string</c>
/// * <c>number</c>
/// * <c>variable</c>
/// * <c>keyword</c>
/// * <c>type</c>
/// * <c>keywordOther</c>
/// * <c>identifier</c>
/// </remarks>
public record SourceToken(string Category, SourceSpan Span);
