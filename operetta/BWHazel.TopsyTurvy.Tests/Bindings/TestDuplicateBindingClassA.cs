using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A binding class contributing one half of a name collision with <see cref="TestDuplicateBindingClassB"/>, used to
/// test <c>BindingCatalogue</c> collision detection.
/// </summary>
public static class TestDuplicateBindingClassA
{
    /// <summary>
    /// A bound method whose name collides with <see cref="TestDuplicateBindingClassB.DuplicateFunctionOther"/>.
    /// </summary>
    [TopsyTurvyFunction(Name = "DuplicateFunction")]
    public static void DuplicateFunctionA()
    {
    }
}
