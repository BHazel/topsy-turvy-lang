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

    /// <summary>
    /// A function flagged as preview with a keyword analogue, used to test that both fold into hover documentation
    /// correctly without depending on any particular Standard Library function.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <param name="io">The host-injected input/output implementation.</param>
    [TopsyTurvyFunction(Name = "TestPreviewWrite", IsPreview = true, KeywordAnalogue = "BEHOLD")]
    public static void TestPreviewWrite([TopsyTurvyParameter("Text")] string text, ITopsyTurvyIO io) => io.WriteLine(text);

    /// <summary>
    /// A function taking an array parameter, used to test array argument marshalling.
    /// </summary>
    /// <param name="values">The array of values.</param>
    /// <returns>The sum of every element in <paramref name="values"/>.</returns>
    [TopsyTurvyFunction(Name = "TestArraySum")]
    public static int TestArraySum([TopsyTurvyParameter("Values")] int[] values)
    {
        int sum = 0;
        foreach (int value in values)
        {
            sum += value;
        }

        return sum;
    }

    /// <summary>
    /// A function returning an array, used to test array return value marshalling.
    /// </summary>
    /// <param name="count">The number of elements to return.</param>
    /// <returns>An array of <paramref name="count"/> ascending integers, starting at 1.</returns>
    [TopsyTurvyFunction(Name = "TestArrayRange")]
    public static int[] TestArrayRange([TopsyTurvyParameter("Count")] int count)
    {
        int[] result = new int[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = i + 1;
        }

        return result;
    }
}
