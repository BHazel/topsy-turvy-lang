using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A public binding class whose bound method is private.
/// </summary>
public static class TestPrivateMethodBindingClass
{
    /// <summary>
    /// A bound method that is not public.
    /// </summary>
    [TopsyTurvyFunction]
    private static void Function()
    {
    }
}
