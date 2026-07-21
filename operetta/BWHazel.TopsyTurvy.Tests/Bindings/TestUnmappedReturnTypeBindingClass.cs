using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A public binding class whose bound method returns a CLR type with no <c>LiteralType</c> mapping.
/// </summary>
public static class TestUnmappedReturnTypeBindingClass
{
    /// <summary>
    /// A bound method returning an unmapped <see cref="object"/> value.
    /// </summary>
    /// <returns>An unmapped return value.</returns>
    [TopsyTurvyFunction]
    public static object Function() => new();
}
