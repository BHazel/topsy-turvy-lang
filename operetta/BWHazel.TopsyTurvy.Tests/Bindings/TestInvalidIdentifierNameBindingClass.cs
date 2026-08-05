using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A public binding class whose bound method has a Topsy Turvy name that is not a valid identifier.
/// </summary>
public static class TestInvalidIdentifierNameBindingClass
{
    /// <summary>
    /// A bound method with an invalid, digit-leading Topsy Turvy name.
    /// </summary>
    [TopsyTurvyFunction(Name = "1InvalidName")]
    public static void Function()
    {
    }
}
