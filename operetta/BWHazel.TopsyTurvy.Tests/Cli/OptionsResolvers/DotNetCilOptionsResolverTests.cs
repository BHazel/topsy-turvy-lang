using System;
using System.Collections.Generic;
using System.IO;
using BWHazel.TopsyTurvy.Cli.OptionsResolvers;
using BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

namespace BWHazel.TopsyTurvy.Tests.Cli.OptionsResolvers;

/// <summary>
/// Tests for the <see cref="DotNetCilOptionsResolver"/> class.
/// </summary>
[Collection("ConsoleCapture")]
public class DotNetCilOptionsResolverTests
{
    private const string SampleUtopIrSource =
        """
        £result = welcome peer
        £result = appoint 21
        find £result
        """;

    private const string SampleTopsyTurvySource =
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
    /// Tests that <see cref="DotNetCilOptionsResolver.GetAllowedExtensions"/> and the default <see cref="IEmitterOptionsResolver{TOptions}.CanEmit"/> implementation accept both <c>.topsy</c> and <c>.utopir</c> files.
    /// </summary>
    [Theory]
    [InlineData("prog.topsy", true)]
    [InlineData("prog.utopir", true)]
    [InlineData("prog.txt", false)]
    public void CanEmit_WithVariousExtensions_ReturnsExpectedResult(string filename, bool expectedResult)
    {
        IEmitterOptionsResolver<CilTargetOptions> resolver = new DotNetCilOptionsResolver();

        resolver.CanEmit(filename).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that <see cref="DotNetCilOptionsResolver.Apply"/> returns the base options unchanged when no <c>--config</c> values are given.
    /// </summary>
    [Fact]
    public void Apply_WithEmptyConfig_ReturnsBaseOptionsUnchanged()
    {
        DotNetCilOptionsResolver resolver = new();
        CilTargetOptions baseOptions = new(new CilEmitOptions("prog", "prog.dll", CilOutputKind.Executable));

        CilTargetOptions result = resolver.Apply(baseOptions, new Dictionary<string, string>(), tiptoe: true);

        result.ShouldBe(baseOptions);
    }

    /// <summary>
    /// Tests that <see cref="DotNetCilOptionsResolver.Apply"/> resolves a valid <c>varFormat</c> value.
    /// </summary>
    [Theory]
    [InlineData("numeric", VariableNameFormat.Numeric)]
    [InlineData("verbose", VariableNameFormat.Verbose)]
    public void Apply_WithValidVarFormat_ReturnsResolvedFormat(string value, VariableNameFormat expected)
    {
        DotNetCilOptionsResolver resolver = new();
        CilTargetOptions baseOptions = new(new CilEmitOptions("prog", "prog.dll", CilOutputKind.Executable));
        Dictionary<string, string> config = new() { ["varFormat"] = value };

        CilTargetOptions result = resolver.Apply(baseOptions, config, tiptoe: true);

        result.Format.ShouldBe(expected);
        result.EmitOptions.ShouldBe(baseOptions.EmitOptions);
    }

    /// <summary>
    /// Tests that <see cref="DotNetCilOptionsResolver.Apply"/> throws <see cref="ToolchainConfigException"/> for an invalid <c>varFormat</c> value.
    /// </summary>
    [Fact]
    public void Apply_WithInvalidVarFormatValue_ThrowsToolchainConfigException()
    {
        DotNetCilOptionsResolver resolver = new();
        CilTargetOptions baseOptions = new(new CilEmitOptions("prog", "prog.dll", CilOutputKind.Executable));
        Dictionary<string, string> config = new() { ["varFormat"] = "bogus" };

        Should.Throw<ToolchainConfigException>(() => resolver.Apply(baseOptions, config, tiptoe: true));
    }

    /// <summary>
    /// Tests that <see cref="DotNetCilOptionsResolver.Apply"/> warns and returns the base options unchanged when an unrecognised <c>--config</c> key is given, rather than throwing.
    /// </summary>
    [Fact]
    public void Apply_WithUnrecognisedConfigKey_WarnsAndReturnsBaseOptionsUnchanged()
    {
        DotNetCilOptionsResolver resolver = new();
        CilTargetOptions baseOptions = new(new CilEmitOptions("prog", "prog.dll", CilOutputKind.Executable));
        Dictionary<string, string> config = new()
        {
            ["optimise"] = "true"
        };

        TextWriter originalError = Console.Error;
        StringWriter capturedError = new();
        Console.SetError(capturedError);
        CilTargetOptions result;
        try
        {
            result = resolver.Apply(baseOptions, config, tiptoe: true);
        }
        finally
        {
            Console.SetError(originalError);
        }

        result.ShouldBe(baseOptions);
        capturedError.ToString().ShouldContain("optimise");
    }

    /// <summary>
    /// Tests that <see cref="DotNetCilOptionsResolver.Emit"/> reads a UtopIR source file and writes the IL disassembly to STDOUT for <see cref="CilOutputKind.IlSourceOnly"/>.
    /// </summary>
    [Fact]
    public void Emit_WithIlSourceOnly_WritesIlToStdout()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"DotNetCilOptionsResolverTests_{Guid.NewGuid():N}.utopir");
        File.WriteAllText(sourcePath, SampleUtopIrSource);

        DotNetCilOptionsResolver resolver = new();
        CilTargetOptions options = new(new CilEmitOptions("prog", string.Empty, CilOutputKind.IlSourceOnly));

        TextWriter originalOut = Console.Out;
        StringWriter capturedOut = new();
        Console.SetOut(capturedOut);
        try
        {
            int exitCode = resolver.Emit(sourcePath, options, tiptoe: true);

            exitCode.ShouldBe(0);
            capturedOut.ToString().ShouldContain("ldc.i4");
        }
        finally
        {
            Console.SetOut(originalOut);
            File.Delete(sourcePath);
        }
    }

    /// <summary>
    /// Tests that <see cref="DotNetCilOptionsResolver.Emit"/> reads a UtopIR source file and writes a runnable assembly to disk for <see cref="CilOutputKind.Executable"/>.
    /// </summary>
    [Fact]
    public void Emit_WithExecutable_WritesAssemblyToOutputPath()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"DotNetCilOptionsResolverTests_{Guid.NewGuid():N}.utopir");
        File.WriteAllText(sourcePath, SampleUtopIrSource);

        string outputPath = Path.Combine(Path.GetTempPath(), $"DotNetCilOptionsResolverTests_{Guid.NewGuid():N}.dll");
        DotNetCilOptionsResolver resolver = new();
        CilTargetOptions options = new(new CilEmitOptions("prog", outputPath, CilOutputKind.Executable));

        try
        {
            int exitCode = resolver.Emit(sourcePath, options, tiptoe: true);

            exitCode.ShouldBe(0);
            File.Exists(outputPath).ShouldBeTrue();
        }
        finally
        {
            File.Delete(sourcePath);
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    /// <summary>
    /// Tests that <see cref="DotNetCilOptionsResolver.Emit"/> uses the requested <see cref="VariableNameFormat"/> when transforming Topsy Turvy source.
    /// </summary>
    [Fact]
    public void Emit_WithVerboseFormatAndTopsyTurvySource_UsesDescriptiveTempNames()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"DotNetCilOptionsResolverTests_{Guid.NewGuid():N}.topsy");
        File.WriteAllText(sourcePath, SampleTopsyTurvySource);

        DotNetCilOptionsResolver resolver = new();
        CilTargetOptions options = new(new CilEmitOptions("prog", string.Empty, CilOutputKind.IlSourceOnly), VariableNameFormat.Verbose);

        TextWriter originalOut = Console.Out;
        StringWriter capturedOut = new();
        Console.SetOut(capturedOut);
        try
        {
            int exitCode = resolver.Emit(sourcePath, options, tiptoe: true);

            exitCode.ShouldBe(0);
            capturedOut.ToString().ShouldContain("'£_sum_10_3'");
        }
        finally
        {
            Console.SetOut(originalOut);
            File.Delete(sourcePath);
        }
    }
}
