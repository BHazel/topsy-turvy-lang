using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A public binding class whose bound method is an instance method.
/// </summary>
public sealed class TestInstanceMethodBindingClass
{
    /// <summary>
    /// A bound method that is not static.
    /// </summary>
    [TopsyTurvyFunction]
    public void Function()
    {
    }
}
