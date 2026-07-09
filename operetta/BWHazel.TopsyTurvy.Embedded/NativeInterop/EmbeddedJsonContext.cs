using System.Text.Json.Serialization;
using BWHazel.TopsyTurvy.Embedded.Analysis;

namespace BWHazel.TopsyTurvy.Embedded.NativeInterop;

/// <summary>
/// Provides source-generated JSON serialisation for native export payloads.
/// </summary>
/// <remarks>
/// Reflection-based <see cref="System.Text.Json.JsonSerializer"/> usage is a trim and Native AOT hazard,
/// so every type serialised across the native export boundary must be registered here rather than relying
/// on the reflection-based default.
/// </remarks>
[JsonSerializable(typeof(AnalysisResult))]
[JsonSerializable(typeof(DiagnosticInfo))]
[JsonSerializable(typeof(HoverResult))]
[JsonSerializable(typeof(CompletionResult))]
[JsonSerializable(typeof(CompletionItemInfo))]
[JsonSerializable(typeof(TokenResult))]
[JsonSerializable(typeof(TokenInfo))]
[JsonSerializable(typeof(string[]))]
public sealed partial class EmbeddedJsonContext : JsonSerializerContext
{
}
