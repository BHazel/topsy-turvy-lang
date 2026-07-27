using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.ExternalLibraryFixture;

/// <summary>
/// A minimal, external library used for testing toolchain operations.
/// </summary>
/// <remarks>
/// Deliberately kept separate from the internal <c>Test*BindingClass</c> fixtures in
/// <c>BWHazel.TopsyTurvy.Tests/Bindings</c>, which are used to test <c>BindingScanner</c> itself and include several
/// classes malformed on purpose. Scanning the assembly of this project is what a real admitted external library assembly
/// looks like: a project referencing only <c>BWHazel.TopsyTurvy.Sdk.Interop</c>.
/// </remarks>
public static class FixtureLibrary
{
    /// <summary>
    /// Greets the given name.
    /// </summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A greeting containing the <paramref name="name"/> to greet.</returns>
    [TopsyTurvyFunction(Name = "Greet")]
    public static string Greet(string name) => $"Hello, {name}!";

    /// <summary>
    /// Sums the given array of integers.
    /// </summary>
    /// <param name="values">The array of values to sum.</param>
    /// <returns>The sum of every element in <paramref name="values"/>.</returns>
    [TopsyTurvyFunction(Name = "Sum")]
    public static int Sum(int[] values)
    {
        int total = 0;
        foreach (int value in values)
        {
            total += value;
        }

        return total;
    }
}
