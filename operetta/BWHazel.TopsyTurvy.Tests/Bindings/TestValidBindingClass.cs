using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A well-formed binding class exercising the happy path of the scanner.
/// </summary>
public static class TestValidBindingClass
{
    /// <summary>
    /// A void function with a renamed parameter and a trailing host-injected parameter.
    /// </summary>
    /// <param name="text">The text parameter, renamed from <c>text</c> to <c>Text</c>.</param>
    /// <param name="io">The host-injected I/O parameter, invisible to Topsy Turvy code.</param>
    [TopsyTurvyFunction(Name = "TestVoidFunction", IsPreview = true, KeywordAnalogue = "BEHOLD")]
    public static void VoidFunction([TopsyTurvyParameter("Text")] string text, ITopsyTurvyIO io) =>
        io.WriteLine(text);

    /// <summary>
    /// A value-returning function with no parameters, defaulting its Topsy Turvy name to the CLR method name.
    /// </summary>
    /// <returns>Always <c>42</c>.</returns>
    [TopsyTurvyFunction]
    public static int ValueFunction() => 42;

    /// <summary>
    /// A namespaced function, used to test catalogue namespace-qualified keys.
    /// </summary>
    [TopsyTurvyFunction(Name = "NamespacedFunction", Namespace = ["Test", "Namespace"])]
    public static void NamespacedFunction()
    {
    }

    /// <summary>
    /// A public static method with no <see cref="TopsyTurvyFunctionAttribute"/>, which the scanner must ignore.
    /// </summary>
    public static void UnattributedFunction()
    {
    }
}
