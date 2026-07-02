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

    /// <summary>
    /// Tests that <see cref="DotNetCilOptionsResolver.GetAllowedExtensions"/> and the default <see cref="IEmitterOptionsResolver{TOptions}.CanEmit"/> implementation accept both <c>.topsy</c> and <c>.utopir</c> files.
    /// </summary>
    [Theory]
    [InlineData("prog.topsy", true)]
    [InlineData("prog.utopir", true)]
    [InlineData("prog.txt", false)]
    public void CanEmit_WithVariousExtensions_ReturnsExpectedResult(string filename, bool expectedResult)
    {
        IEmitterOptionsResolver<CilEmitOptions> resolver = new DotNetCilOptionsResolver();

        resolver.CanEmit(filename).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that <see cref="DotNetCilOptionsResolver.Apply"/> returns the base options unchanged when no <c>--config</c> values are given.
    /// </summary>
    [Fact]
    public void Apply_WithEmptyConfig_ReturnsBaseOptionsUnchanged()
    {
        DotNetCilOptionsResolver resolver = new();
        CilEmitOptions baseOptions = new("prog", "prog.dll", CilOutputKind.Executable);

        CilEmitOptions result = resolver.Apply(baseOptions, new Dictionary<string, string>(), tiptoe: true);

        result.ShouldBe(baseOptions);
    }

    /// <summary>
    /// Tests that <see cref="DotNetCilOptionsResolver.Apply"/> warns and returns the base options unchanged when any <c>--config</c> value is given, rather than throwing.
    /// </summary>
    [Fact]
    public void Apply_WithAnyConfigKey_WarnsAndReturnsBaseOptionsUnchanged()
    {
        DotNetCilOptionsResolver resolver = new();
        CilEmitOptions baseOptions = new("prog", "prog.dll", CilOutputKind.Executable);
        Dictionary<string, string> config = new()
        {
            ["optimise"] = "true"
        };

        TextWriter originalError = Console.Error;
        StringWriter capturedError = new();
        Console.SetError(capturedError);
        CilEmitOptions result;
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
        CilEmitOptions options = new("prog", string.Empty, CilOutputKind.IlSourceOnly);

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
        CilEmitOptions options = new("prog", outputPath, CilOutputKind.Executable);

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
}
