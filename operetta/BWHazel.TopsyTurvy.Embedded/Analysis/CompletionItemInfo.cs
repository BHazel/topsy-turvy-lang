namespace BWHazel.TopsyTurvy.Embedded.Analysis;

/// <summary>
/// Represents a single completion candidate, as serialised across the native export boundary.
/// </summary>
/// <param name="Label">The display label for the candidate.</param>
/// <param name="Kind">The candidate kind, one of <c>"Keyword"</c>, <c>"Variable"</c>, <c>"Function"</c> or <c>"Parameter"</c>.</param>
/// <param name="Detail">The additional detail text, such as a type name or parameter list, or <see langword="null"/> when none applies.</param>
/// <param name="InsertText">The text to insert, which may be a suffix of <see cref="Label"/> when part of it has already been typed.</param>
public record CompletionItemInfo(string Label, string Kind, string? Detail, string InsertText);
