using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A public binding class whose bound method has a parameter of a CLR type with no <c>LiteralType</c> mapping.
/// </summary>
public static class TestUnmappedParameterTypeBindingClass
{
    /// <summary>
    /// A bound method with an unmapped <see cref="object"/> parameter.
    /// </summary>
    /// <param name="value">A parameter of an unmapped type.</param>
    [TopsyTurvyFunction]
    public static void Function(object value)
    {
    }
}
