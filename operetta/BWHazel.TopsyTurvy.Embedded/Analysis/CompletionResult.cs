using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Embedded.Analysis;

/// <summary>
/// Represents the result of a completion request, returned as JSON by <see cref="NativeExports.GetCompletions"/>.
/// </summary>
/// <param name="Items">The completion candidates, combining matching keywords and symbols.</param>
public record CompletionResult(IReadOnlyList<CompletionItemPayload> Items);
