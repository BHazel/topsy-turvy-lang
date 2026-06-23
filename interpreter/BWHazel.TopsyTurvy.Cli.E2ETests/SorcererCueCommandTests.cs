using System.IO;
using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Tests for the CLI sorcerer cue (preprocess) subcommand.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="SorcererCueCommandTests"/> class.
/// </remarks>
/// <param name="fixture">The shared CLI fixture.</param>
[Collection("Cli")]
public sealed class SorcererCueCommandTests(CliFixture fixture)
    : CliTestBase(fixture)
{
    private const string ValidSource =
        """
        HARK! "Test"
        PRINCIPALS
        THE CURTAIN RISES.
        ASIDE: This is a comment.
        BEHOLD "OK"
        FINALE.
        """;

    /// <summary>
    /// Tests that the sorcerer cue subcommand returns exit code 1 when the specified file does not exist.
    /// </summary>
    [Fact]
    public async Task SorcererCue_WhenFileDoesNotExist_ReturnsExitCode1()
    {
        (int exitCode, string _, string _) = await this.RunAsync("sorcerer cue nonexistent.topsy --tiptoe");

        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the sorcerer cue subcommand writes an error message to stderr when the specified file does not exist.
    /// </summary>
    [Fact]
    public async Task SorcererCue_WhenFileDoesNotExist_WritesErrorToStderr()
    {
        (int _, string _, string stderr) = await this.RunAsync("sorcerer cue nonexistent.topsy --tiptoe");

        stderr.ShouldContain("File not found");
    }

    /// <summary>
    /// Tests that the sorcerer cue subcommand returns exit code 0 when the specified file is valid.
    /// </summary>
    [Fact]
    public async Task SorcererCue_WhenFileIsValid_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string _) = await this.RunAsync("sorcerer cue prog.topsy --tiptoe");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the sorcerer cue subcommand writes the pre-processed source to stdout with comments stripped.
    /// </summary>
    [Fact]
    public async Task SorcererCue_WhenFileIsValid_WritesPreprocessedOutputToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int _, string stdout, string _) = await this.RunAsync("sorcerer cue prog.topsy --tiptoe");

        stdout.ShouldContain("BEHOLD");
        stdout.ShouldNotContain("This is a comment");
    }

    /// <summary>
    /// Tests that the sorcerer cue subcommand returns exit code 0 when invoked via the preprocess alias with a valid file.
    /// </summary>
    [Fact]
    public async Task SorcererCue_UsingPreprocessAlias_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string _) = await this.RunAsync("sorcerer preprocess prog.topsy --tiptoe");

        exitCode.ShouldBe(0);
    }
}
