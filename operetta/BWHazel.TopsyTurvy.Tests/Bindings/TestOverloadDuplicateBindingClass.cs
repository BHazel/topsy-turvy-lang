using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A binding class contributing a function whose name and parameter types both match one already declared in
/// <see cref="TestOverloadBindingClass"/>, used to test that a true duplicate is still rejected.
/// </summary>
public static class TestOverloadDuplicateBindingClass
{
    /// <summary>
    /// A bound method whose name and parameter type both match <see cref="TestOverloadBindingClass.DescribeInteger"/>.
    /// </summary>
    /// <param name="value">The integer parameter.</param>
    /// <returns>A description of the integer.</returns>
    [TopsyTurvyFunction(Name = "Describe")]
    public static string DescribeIntegerAgain(int value) => value.ToString();
}