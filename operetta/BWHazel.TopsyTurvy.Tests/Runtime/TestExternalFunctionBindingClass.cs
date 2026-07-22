using System;
using BWHazel.TopsyTurvy.Runtime;
using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// A binding class used to test <see cref="ExternalFunctionInvoker"/> and <c>SUMMON</c> dispatch in <see cref="Interpreter"/>.
/// </summary>
public static class TestExternalFunctionBindingClass
{
    /// <summary>
    /// A function taking a narrow integer parameter, used to test a non-widening argument mismatch.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value, unchanged.</returns>
    [TopsyTurvyFunction(Name = "TestNarrow")]
    public static int TestNarrow([TopsyTurvyParameter("Value")] int value) => value;

    /// <summary>
    /// A function taking a wide integer parameter, used to test a widening argument conversion.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value, unchanged.</returns>
    [TopsyTurvyFunction(Name = "TestWiden")]
    public static long TestWiden([TopsyTurvyParameter("Value")] long value) => value;

    /// <summary>
    /// A function that always throws, used to test that an invoked function exception is wrapped in a <see cref="TopsyTurvyRuntimeException"/>.
    /// </summary>
    [TopsyTurvyFunction(Name = "TestThrows")]
    public static void TestThrows() => throw new InvalidOperationException("boom");

    /// <summary>
    /// A function writing to the host-injected <see cref="ITopsyTurvyIO"/>, used to test <c>SUMMON</c> dispatch to an
    /// external function without depending on any particular Standard Library function.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <param name="io">The host-injected input/output implementation.</param>
    [TopsyTurvyFunction(Name = "TestWrite")]
    public static void TestWrite([TopsyTurvyParameter("Text")] string text, ITopsyTurvyIO io) => io.WriteLine(text);

    /// <summary>
    /// A function reading from the host-injected <see cref="ITopsyTurvyIO"/>, used to test <c>SUMMON</c> dispatch to an
    /// external function without depending on any particular Standard Library function.
    /// </summary>
    /// <param name="io">The host-injected input/output implementation.</param>
    /// <returns>The line read.</returns>
    [TopsyTurvyFunction(Name = "TestRead")]
    public static string TestRead(ITopsyTurvyIO io) => io.ReadLine();
}
