using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A public binding class whose bound method has an invalid namespace path segment.
/// </summary>
public static class TestInvalidNamespaceSegmentBindingClass
{
    /// <summary>
    /// A bound method with an invalid, digit-leading namespace segment.
    /// </summary>
    [TopsyTurvyFunction(Namespace = ["1InvalidSegment"])]
    public static void Function()
    {
    }
}
