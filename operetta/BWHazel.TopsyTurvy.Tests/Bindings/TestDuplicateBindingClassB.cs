using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A binding class contributing one half of a name collision with <see cref="TestDuplicateBindingClassA"/>, used to
/// test <c>BindingCatalogue</c> collision detection.
/// </summary>
public static class TestDuplicateBindingClassB
{
    /// <summary>
    /// A bound method whose name collides with <see cref="TestDuplicateBindingClassA.DuplicateFunctionA"/>.
    /// </summary>
    [TopsyTurvyFunction(Name = "DuplicateFunction")]
    public static void DuplicateFunctionOther()
    {
    }
}
