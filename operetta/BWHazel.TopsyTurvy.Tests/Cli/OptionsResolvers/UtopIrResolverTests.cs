using System;
using System.Collections.Generic;
using System.IO;
using BWHazel.TopsyTurvy.Cli.OptionsResolvers;

namespace BWHazel.TopsyTurvy.Tests.Cli.OptionsResolvers;

/// <summary>
/// Tests for the <see cref="UtopIrResolver"/> class.
/// </summary>
[Collection("ConsoleCapture")]
public class UtopIrResolverTests
{
    private const string SampleSource =
        """
        HARK! "Test"
        PRINCIPALS
          PRAY WELCOME result AS A PEER
        THE CURTAIN RISES.
        result IS APPOINTED SUM OF 10 AND 3
        AND SO I FIND result
        FINALE.
        """;

    /// <summary>
    /// Tests that <see cref="UtopIrResolver.GetAllowedExtensions"/> only accepts <c>.topsy</c> files.
    /// </summary>
    [Theory]
    [InlineData("prog.topsy", true)]
    [InlineData("prog.utopir", false)]
    public void CanEmit_WithVariousExtensions_ReturnsExpectedResult(string filename, bool expectedResult)
    {
        IEmitterOptionsResolver<NoOptions> resolver = new UtopIrResolver();

        resolver.CanEmit(filename).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that <see cref="UtopIrResolver.Apply"/> warns and returns the base options unchanged when any <c>--config</c> value is given, rather than throwing.
    /// </summary>
    [Fact]
    public void Apply_WithAnyConfigKey_WarnsAndReturnsBaseOptionsUnchanged()
    {
        UtopIrResolver resolver = new();
        Dictionary<string, string> config = new()
        {
            ["foo"] = "bar"
        };

        TextWriter originalError = Console.Error;
        StringWriter capturedError = new();
        Console.SetError(capturedError);
        NoOptions result;
        try
        {
            result = resolver.Apply(NoOptions.Default, config, tiptoe: true);
        }
        finally
        {
            Console.SetError(originalError);
        }

        result.ShouldBe(NoOptions.Default);
        capturedError.ToString().ShouldContain("foo");
    }

    /// <summary>
    /// Tests that <see cref="UtopIrResolver.Emit"/> writes the transformed UtopIR source text to STDOUT.
    /// </summary>
    [Fact]
    public void Emit_WithValidSource_WritesUtopIrTextToStdout()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"UtopIrResolverTests_{Guid.NewGuid():N}.topsy");
        File.WriteAllText(sourcePath, SampleSource);

        UtopIrResolver resolver = new();

        TextWriter originalOut = Console.Out;
        StringWriter capturedOut = new();
        Console.SetOut(capturedOut);
        try
        {
            int exitCode = resolver.Emit(sourcePath, NoOptions.Default, tiptoe: true);

            exitCode.ShouldBe(0);
            capturedOut.ToString().ShouldContain("£result");
        }
        finally
        {
            Console.SetOut(originalOut);
            File.Delete(sourcePath);
        }
    }
}
