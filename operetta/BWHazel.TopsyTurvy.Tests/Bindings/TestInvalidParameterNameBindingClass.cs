using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A public binding class whose bound method has a parameter renamed to an invalid Topsy Turvy identifier.
/// </summary>
public static class TestInvalidParameterNameBindingClass
{
    /// <summary>
    /// A bound method with an invalid, digit-leading parameter name.
    /// </summary>
    /// <param name="text">A parameter renamed to an invalid identifier.</param>
    [TopsyTurvyFunction]
    public static void Function([TopsyTurvyParameter("1InvalidName")] string text)
    {
    }
}
