using System;

namespace BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

/// <summary>
/// Marks a static method as a Topsy Turvy library function.
/// </summary>
/// <remarks>
/// This is the sole discovery mechanism for external functions: the <c>BindingScanner</c> finds a
/// callable function purely by looking for this attribute.  So an author of an external library only has to add
/// the attribute to a method for it to become callable from Topsy Turvy code.  The method must be <c>public static</c>
/// on a <c>public</c> class.  Trailing parameters of host-injected service types, such as <see cref="IO.ITopsyTurvyIO"/>, are injected by
/// the host and are invisible to Topsy Turvy code.  All preceding parameters form the language-level signature.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TopsyTurvyFunctionAttribute : Attribute
{
    /// <summary>
    /// Gets or initialises the function name as called from Topsy Turvy code.
    /// </summary>
    /// <remarks>
    /// Defaults to the CLR method name when not set.  Must be a valid Topsy Turvy identifier
    /// and must not be a reserved keyword.
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or initialises the ordered namespace path segments, or <c>null</c>/empty for the global namespace.
    /// </summary>
    /// <remarks>
    /// For example <c>["Accounts", "Payroll"]</c> would be used for a namespace declared in Tospy Turvy as
    /// <c>TOWN Accounts WITH DISTRICT Payroll</c>, or the <c>*</c> short-hand, matching the
    /// <c>NamespaceDeclarationNode.Path</c> shape exactly.
    /// </remarks>
    public string[]? Namespace { get; init; }

    /// <summary>
    /// Gets or initialises a value indicating whether the function is a preview function that may change without warning.
    /// </summary>
    public bool IsPreview { get; init; }

    /// <summary>
    /// Gets or initialises the name of the language keyword this function is the library counterpart of, if any.
    /// </summary>
    /// <remarks>Documentation metadata only, surfaced in hover text.  It has no effect on binding or resolution.</remarks>
    public string? KeywordAnalogue { get; init; }
}
