using System;
using System.Collections.Generic;
using System.IO;
using BWHazel.TopsyTurvy.Cli.OptionsResolvers;

namespace BWHazel.TopsyTurvy.Tests.Cli.OptionsResolvers;

/// <summary>
/// Tests for the <see cref="UtopIrAstResolver"/> class.
/// </summary>
[Collection("ConsoleCapture")]
public class UtopIrAstResolverTests
{
    private const string SampleUtopIrSource =
        """
        £result = welcome peer
        £result = appoint 21
        find £result
        """;

    /// <summary>
    /// Tests that <see cref="UtopIrAstResolver.GetAllowedExtensions"/> accepts both <c>.topsy</c> and <c>.utopir</c> files.
    /// </summary>
    [Theory]
    [InlineData("prog.topsy", true)]
    [InlineData("prog.utopir", true)]
    [InlineData("prog.txt", false)]
    public void CanEmit_WithVariousExtensions_ReturnsExpectedResult(string filename, bool expectedResult)
    {
        IEmitterOptionsResolver<JsonEmitOptions> resolver = new UtopIrAstResolver();

        resolver.CanEmit(filename).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that <see cref="UtopIrAstResolver.Apply"/> warns and returns the base options unchanged when any <c>--config</c> value is given, rather than throwing.
    /// </summary>
    [Fact]
    public void Apply_WithAnyConfigKey_WarnsAndReturnsBaseOptionsUnchanged()
    {
        UtopIrAstResolver resolver = new();
        JsonEmitOptions baseOptions = new(false, false);
        Dictionary<string, string> config = new()
        {
            ["foo"] = "bar"
        };

        TextWriter originalError = Console.Error;
        StringWriter capturedError = new();
        Console.SetError(capturedError);
        JsonEmitOptions result;
        try
        {
            result = resolver.Apply(baseOptions, config, tiptoe: true);
        }
        finally
        {
            Console.SetError(originalError);
        }

        result.ShouldBe(baseOptions);
        capturedError.ToString().ShouldContain("foo");
    }

    /// <summary>
    /// Tests that <see cref="UtopIrAstResolver.Emit"/> reads a UtopIR source file and writes the UtopIR AST as JSON to STDOUT.
    /// </summary>
    [Fact]
    public void Emit_WithUtopIrSource_WritesUtopIrAstJsonToStdout()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"UtopIrAstResolverTests_{Guid.NewGuid():N}.utopir");
        File.WriteAllText(sourcePath, SampleUtopIrSource);

        UtopIrAstResolver resolver = new();

        TextWriter originalOut = Console.Out;
        StringWriter capturedOut = new();
        Console.SetOut(capturedOut);
        try
        {
            int exitCode = resolver.Emit(sourcePath, new JsonEmitOptions(false, false), tiptoe: true);

            exitCode.ShouldBe(0);
            capturedOut.ToString().ShouldContain("WelcomeInstruction");
        }
        finally
        {
            Console.SetOut(originalOut);
            File.Delete(sourcePath);
        }
    }
}
