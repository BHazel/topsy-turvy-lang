using System;
using System.Collections.Generic;
using System.IO;
using BWHazel.TopsyTurvy.Cli.OptionsResolvers;

namespace BWHazel.TopsyTurvy.Tests.Cli.OptionsResolvers;

/// <summary>
/// Tests for the <see cref="AstResolver"/> class.
/// </summary>
[Collection("ConsoleCapture")]
public class AstResolverTests
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
    /// Tests that <see cref="AstResolver.GetAllowedExtensions"/> only accepts <c>.topsy</c> files.
    /// </summary>
    [Theory]
    [InlineData("prog.topsy", true)]
    [InlineData("prog.utopir", false)]
    public void CanEmit_WithVariousExtensions_ReturnsExpectedResult(string filename, bool expectedResult)
    {
        IEmitterOptionsResolver<JsonEmitOptions> resolver = new AstResolver();

        resolver.CanEmit(filename).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that <see cref="AstResolver.Apply"/> warns and returns the base options unchanged when any <c>--config</c> value is given, rather than throwing.
    /// </summary>
    [Fact]
    public void Apply_WithAnyConfigKey_WarnsAndReturnsBaseOptionsUnchanged()
    {
        AstResolver resolver = new();
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
    /// Tests that <see cref="AstResolver.Emit"/> writes the Topsy Turvy AST as JSON to STDOUT.
    /// </summary>
    [Fact]
    public void Emit_WithValidSource_WritesAstJsonToStdout()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"AstResolverTests_{Guid.NewGuid():N}.topsy");
        File.WriteAllText(sourcePath, SampleSource);

        AstResolver resolver = new();

        TextWriter originalOut = Console.Out;
        StringWriter capturedOut = new();
        Console.SetOut(capturedOut);
        try
        {
            int exitCode = resolver.Emit(sourcePath, new JsonEmitOptions(false, false), tiptoe: true);

            exitCode.ShouldBe(0);
            capturedOut.ToString().ShouldContain("ProgramNode");
        }
        finally
        {
            Console.SetOut(originalOut);
            File.Delete(sourcePath);
        }
    }
}
