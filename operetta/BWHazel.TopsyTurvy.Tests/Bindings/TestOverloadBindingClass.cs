using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A binding class contributing two functions that share a name but differ in parameter types, used to test
/// <see cref="BindingCatalogue"/> overload support.
/// </summary>
public static class TestOverloadBindingClass
{
    /// <summary>
    /// One overload of <c>Describe</c>, taking a single integer.
    /// </summary>
    /// <param name="value">The integer parameter.</param>
    /// <returns>A description of the integer.</returns>
    [TopsyTurvyFunction(Name = "Describe")]
    public static string DescribeInteger(int value) => value.ToString();

    /// <summary>
    /// The other overload of <c>Describe</c>, taking an array of integers instead.
    /// </summary>
    /// <param name="values">The array parameter.</param>
    /// <returns>A description of the array.</returns>
    [TopsyTurvyFunction(Name = "Describe")]
    public static string DescribeArray(int[] values) => values.Length.ToString();
}
