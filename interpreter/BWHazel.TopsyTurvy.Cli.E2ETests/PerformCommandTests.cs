using System.IO;
using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Tests for the CLI perform (run, stage) command.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="PerformCommandTests"/> class.
/// </remarks>
/// <param name="fixture">The shared CLI fixture.</param>
[Collection("Cli")]
public sealed class PerformCommandTests(CliFixture fixture)
    : CliTestBase(fixture)
{
    private const string ValidSource =
        """
        HARK! "Test"
        PRINCIPALS
        THE CURTAIN RISES.
        BEHOLD "OK"
        FINALE.
        """;

    private const string InvalidSource = "FINALE.";

    private const string RuntimeErrorSource =
        """
        HARK! "Test"
        PRINCIPALS
        THE CURTAIN RISES.
        A HIDEOUS CURSE ON "runtime error"
        FINALE.
        """;

    /// <summary>
    /// Tests that the perform command returns exit code 1 when the specified file does not exist.
    /// </summary>
    [Fact]
    public async Task Perform_WhenFileDoesNotExist_ReturnsExitCode1()
    {
        (int exitCode, string _, string _) = await this.RunAsync("perform nonexistent.topsy --tiptoe");

        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the perform command writes an error message to stderr when the specified file does not exist.
    /// </summary>
    [Fact]
    public async Task Perform_WhenFileDoesNotExist_WritesErrorToStderr()
    {
        (int _, string _, string stderr) = await this.RunAsync("perform nonexistent.topsy --tiptoe");

        stderr.ShouldContain("File not found");
    }

    /// <summary>
    /// Tests that the perform command returns exit code 1 when the specified file contains syntax errors.
    /// </summary>
    [Fact]
    public async Task Perform_WhenFileHasSyntaxErrors_ReturnsExitCode1()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), InvalidSource);

        (int exitCode, string _, string _) = await this.RunAsync("perform prog.topsy --tiptoe");

        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the perform command returns exit code 1 when the specified file causes a runtime error.
    /// </summary>
    [Fact]
    public async Task Perform_WhenFileHasRuntimeError_ReturnsExitCode1()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), RuntimeErrorSource);

        (int exitCode, string _, string _) = await this.RunAsync("perform prog.topsy --tiptoe");

        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the perform command returns exit code 0 when the specified file is valid.
    /// </summary>
    [Fact]
    public async Task Perform_WhenFileIsValid_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string _) = await this.RunAsync("perform prog.topsy --tiptoe");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the perform command writes the program output to stdout when the file is valid.
    /// </summary>
    [Fact]
    public async Task Perform_WhenFileIsValid_WritesOutputToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int _, string stdout, string _) = await this.RunAsync("perform prog.topsy --tiptoe");

        stdout.ShouldContain("OK");
    }

    /// <summary>
    /// Tests that the perform command writes nothing to stderr when the file is valid.
    /// </summary>
    [Fact]
    public async Task Perform_WhenFileIsValid_WritesNothingToStderr()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int _, string _, string stderr) = await this.RunAsync("perform prog.topsy --tiptoe");

        stderr.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the perform command produces output when invoked via the run alias with a valid file.
    /// </summary>
    [Fact]
    public async Task Perform_UsingRunAlias_ProducesOutput()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("run prog.topsy --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("OK");
    }

    /// <summary>
    /// Tests that the perform command returns exit code 0 when invoked via the stage alias with a valid file.
    /// </summary>
    [Fact]
    public async Task Perform_UsingStageAlias_ProducesOutput()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("stage prog.topsy --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("OK");
    }
}
