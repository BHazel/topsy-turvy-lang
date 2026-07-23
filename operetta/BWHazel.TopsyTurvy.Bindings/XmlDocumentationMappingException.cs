using System;

namespace BWHazel.TopsyTurvy.Bindings;

/// <summary>
/// Represents an error mapping the compiler-generated XML documentation of a bound function into a <c>DocumentationComment</c>.
/// </summary>
/// <remarks>
/// Thrown by <see cref="XmlDocumentationMapper"/> when a documentation ID cannot be built, when no
/// <c>&lt;member&gt;</c> element exists for a bound function, or when a bound parameter has no corresponding
/// <c>&lt;param&gt;</c> entry.  Stale or missing library documentation is treated as an error rather than being
/// silently tolerated, matching the documentation discipline of the project.
/// </remarks>
/// <param name="message">A description of the error.</param>
public sealed class XmlDocumentationMappingException(string message) : Exception(message);
