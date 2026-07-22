using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.UtopIR.Tests.Transformer;

/// <summary>
/// A binding class used to test the <see cref="BWHazel.TopsyTurvy.UtopIR.Transformer.TopsyTurvyToUtopIRTransformer"/>
/// <c>summon</c>/<c>summon.find</c>/<c>BEHOLD</c>/<c>PRAY TELL</c> lowering, without depending on the real
/// Standard Library.
/// </summary>
public static class TestExternalFunctionBindingClass
{
    /// <summary>
    /// A void function taking one integer parameter, used to test a standalone <c>SUMMON</c>.
    /// </summary>
    /// <param name="value">The value.</param>
    [TopsyTurvyFunction(Name = "TestVoid")]
    public static void TestVoid([TopsyTurvyParameter("Value")] int value)
    {
    }

    /// <summary>
    /// A function returning its integer parameter unchanged, used to test <c>summon.find</c>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value, unchanged.</returns>
    [TopsyTurvyFunction(Name = "TestValue")]
    public static int TestValue([TopsyTurvyParameter("Value")] int value) => value;

    /// <summary>
    /// A function taking a wide integer parameter, used to test a widening argument conversion.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value, unchanged.</returns>
    [TopsyTurvyFunction(Name = "TestWiden")]
    public static long TestWiden([TopsyTurvyParameter("Value")] long value) => value;

    /// <summary>
    /// A stand-in for the Standard Library <c>PreviewBehold</c>, matching its name and signature exactly,
    /// since <c>TopsyTurvyToUtopIRTransformer.TransformPrint</c> looks the function up by that literal name.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <param name="withCeremony">A value indicating whether a trailing newline is applied.</param>
    /// <param name="io">The host-injected input/output implementation.</param>
    [TopsyTurvyFunction(Name = "PreviewBehold")]
    public static void PreviewBehold(
        [TopsyTurvyParameter("Text")] string text,
        [TopsyTurvyParameter("WithCeremony")] bool withCeremony,
        ITopsyTurvyIO io)
    {
    }

    /// <summary>
    /// A stand-in for the Standard Library <c>PreviewPrayTell</c>, matching its name and signature exactly,
    /// since <c>TopsyTurvyToUtopIRTransformer.TransformInput</c> looks the function up by that literal name.
    /// </summary>
    /// <param name="io">The host-injected input/output implementation.</param>
    /// <returns>An empty line.</returns>
    [TopsyTurvyFunction(Name = "PreviewPrayTell")]
    public static string PreviewPrayTell(ITopsyTurvyIO io) => string.Empty;
}
