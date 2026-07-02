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
        IEmitterOptionsResolver<VariableNameFormat> resolver = new UtopIrResolver();

        resolver.CanEmit(filename).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that <see cref="UtopIrResolver.Apply"/> resolves a valid <c>varFormat</c> value.
    /// </summary>
    [Theory]
    [InlineData("numeric", VariableNameFormat.Numeric)]
    [InlineData("verbose", VariableNameFormat.Verbose)]
    [InlineData("VERBOSE", VariableNameFormat.Verbose)]
    public void Apply_WithValidVarFormat_ReturnsResolvedFormat(string value, VariableNameFormat expected)
    {
        UtopIrResolver resolver = new();
        Dictionary<string, string> config = new() { ["varFormat"] = value };

        VariableNameFormat result = resolver.Apply(VariableNameFormat.Numeric, config, tiptoe: true);

        result.ShouldBe(expected);
    }

    /// <summary>
    /// Tests that <see cref="UtopIrResolver.Apply"/> throws <see cref="ToolchainConfigException"/> for an invalid <c>varFormat</c> value.
    /// </summary>
    [Fact]
    public void Apply_WithInvalidVarFormatValue_ThrowsToolchainConfigException()
    {
        UtopIrResolver resolver = new();
        Dictionary<string, string> config = new() { ["varFormat"] = "bogus" };

        Should.Throw<ToolchainConfigException>(() => resolver.Apply(VariableNameFormat.Numeric, config, tiptoe: true));
    }

    /// <summary>
    /// Tests that <see cref="UtopIrResolver.Apply"/> warns and returns the default format unchanged when an unrecognized <c>--config</c> key is given, rather than throwing.
    /// </summary>
    [Fact]
    public void Apply_WithUnrecognizedConfigKey_WarnsAndReturnsDefaultFormat()
    {
        UtopIrResolver resolver = new();
        Dictionary<string, string> config = new() { ["foo"] = "bar" };

        TextWriter originalError = Console.Error;
        StringWriter capturedError = new();
        Console.SetError(capturedError);
        VariableNameFormat result;
        try
        {
            result = resolver.Apply(VariableNameFormat.Numeric, config, tiptoe: true);
        }
        finally
        {
            Console.SetError(originalError);
        }

        result.ShouldBe(VariableNameFormat.Numeric);
        capturedError.ToString().ShouldContain("foo");
    }

    /// <summary>
    /// Tests that <see cref="UtopIrResolver.Emit"/> writes the transformed UtopIR source text to STDOUT using numeric temporary names by default.
    /// </summary>
    [Fact]
    public void Emit_WithNumericFormat_WritesUtopIrTextWithNumericTempNames()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"UtopIrResolverTests_{Guid.NewGuid():N}.topsy");
        File.WriteAllText(sourcePath, SampleSource);

        UtopIrResolver resolver = new();

        TextWriter originalOut = Console.Out;
        StringWriter capturedOut = new();
        Console.SetOut(capturedOut);
        try
        {
            int exitCode = resolver.Emit(sourcePath, VariableNameFormat.Numeric, tiptoe: true);

            exitCode.ShouldBe(0);
            capturedOut.ToString().ShouldContain("£result");
            capturedOut.ToString().ShouldContain("£_0");
        }
        finally
        {
            Console.SetOut(originalOut);
            File.Delete(sourcePath);
        }
    }

    /// <summary>
    /// Tests that <see cref="UtopIrResolver.Emit"/> writes the transformed UtopIR source text to STDOUT using descriptive temporary names when <see cref="VariableNameFormat.Verbose"/> is requested.
    /// </summary>
    [Fact]
    public void Emit_WithVerboseFormat_WritesUtopIrTextWithDescriptiveTempNames()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"UtopIrResolverTests_{Guid.NewGuid():N}.topsy");
        File.WriteAllText(sourcePath, SampleSource);

        UtopIrResolver resolver = new();

        TextWriter originalOut = Console.Out;
        StringWriter capturedOut = new();
        Console.SetOut(capturedOut);
        try
        {
            int exitCode = resolver.Emit(sourcePath, VariableNameFormat.Verbose, tiptoe: true);

            exitCode.ShouldBe(0);
            capturedOut.ToString().ShouldContain("£_sum_10_3");
        }
        finally
        {
            Console.SetOut(originalOut);
            File.Delete(sourcePath);
        }
    }
}
