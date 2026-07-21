using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A non-public binding class carrying an otherwise well-formed bound method.
/// </summary>
internal static class TestNonPublicBindingClass
{
    /// <summary>
    /// A bound method that cannot be discovered because its declaring class is not public.
    /// </summary>
    [TopsyTurvyFunction]
    public static void Function()
    {
    }
}
