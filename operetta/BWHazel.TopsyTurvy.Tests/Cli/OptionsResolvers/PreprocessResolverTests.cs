using System;
using System.Collections.Generic;
using System.IO;
using BWHazel.TopsyTurvy.Cli.OptionsResolvers;

namespace BWHazel.TopsyTurvy.Tests.Cli.OptionsResolvers;

/// <summary>
/// Tests for the <see cref="PreprocessResolver"/> class.
/// </summary>
[Collection("ConsoleCapture")]
public class PreprocessResolverTests
{
    /// <summary>
    /// Tests that <see cref="PreprocessResolver.GetAllowedExtensions"/> only accepts <c>.topsy</c> files.
    /// </summary>
    [Theory]
    [InlineData("prog.topsy", true)]
    [InlineData("prog.utopir", false)]
    public void CanEmit_WithVariousExtensions_ReturnsExpectedResult(string filename, bool expectedResult)
    {
        IEmitterOptionsResolver<NoOptions> resolver = new PreprocessResolver();

        resolver.CanEmit(filename).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that <see cref="PreprocessResolver.Apply"/> warns and returns the base options unchanged when any <c>--config</c> value is given, rather than throwing.
    /// </summary>
    [Fact]
    public void Apply_WithAnyConfigKey_WarnsAndReturnsBaseOptionsUnchanged()
    {
        PreprocessResolver resolver = new();
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
    /// Tests that <see cref="PreprocessResolver.Emit"/> writes the pre-processed source to STDOUT with comments stripped.
    /// </summary>
    [Fact]
    public void Emit_WithValidSource_WritesPreprocessedTextToStdout()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"PreprocessResolverTests_{Guid.NewGuid():N}.topsy");
        File.WriteAllText(sourcePath, "HARK! \"Test\"\nASIDE: comment\nFINALE.");

        PreprocessResolver resolver = new();

        TextWriter originalOut = Console.Out;
        StringWriter capturedOut = new();
        Console.SetOut(capturedOut);
        try
        {
            int exitCode = resolver.Emit(sourcePath, NoOptions.Default, tiptoe: true);

            exitCode.ShouldBe(0);
            capturedOut.ToString().ShouldContain("HARK!");
            capturedOut.ToString().ShouldNotContain("comment");
        }
        finally
        {
            Console.SetOut(originalOut);
            File.Delete(sourcePath);
        }
    }
}
