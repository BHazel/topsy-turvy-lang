using System;

namespace BWHazel.TopsyTurvy.Bindings;

/// <summary>
/// Represents an error discovered while scanning binding classes or constructing a <see cref="BindingCatalogue"/>.
/// </summary>
/// <remarks>
/// Thrown by <see cref="BindingScanner"/> for a malformed binding, such as a non-public binding class, an
/// unmapped parameter type, or a host-injected parameter preceding a bound parameter; and by <see cref="BindingCatalogue"/>
/// for a name collision between two descriptors.  Both classes of error surface at construction time rather than
/// being silently skipped, matching the declare-before-use strictness of the project.
/// </remarks>
/// <param name="message">A description of the error.</param>
public sealed class BindingCatalogueException(string message) : Exception(message);
