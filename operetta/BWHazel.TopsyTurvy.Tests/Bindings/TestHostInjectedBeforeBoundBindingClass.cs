using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// A public binding class whose bound method has a host-injected parameter preceding a bound parameter..
/// </summary>
public static class TestHostInjectedBeforeBoundBindingClass
{
    /// <summary>
    /// A bound method with the host-injected <see cref="ITopsyTurvyIO"/> parameter listed before a bound parameter.
    /// </summary>
    /// <param name="io">The host-injected parameter, listed first.</param>
    /// <param name="text">A bound parameter, listed after the host-injected parameter.</param>
    [TopsyTurvyFunction]
    public static void Function(ITopsyTurvyIO io, string text) =>
        io.WriteLine(text);
}
