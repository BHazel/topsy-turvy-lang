using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A public binding class with an array-typed parameter and an array-typed return value.
/// </summary>
public static class TestArrayBindingClass
{
    /// <summary>
    /// A bound method taking an array parameter and returning its length.
    /// </summary>
    /// <param name="values">The array parameter.</param>
    /// <returns>The number of elements in <paramref name="values"/>.</returns>
    [TopsyTurvyFunction]
    public static int Count(int[] values) => values.Length;

    /// <summary>
    /// A bound method returning an array.
    /// </summary>
    /// <returns>An array of three integers.</returns>
    [TopsyTurvyFunction]
    public static int[] Range() => [1, 2, 3];
}
