using System.IO;
using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Tests for the CLI rehearse (check) command.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="RehearseCommandTests"/> class.
/// </remarks>
/// <param name="fixture">The shared CLI fixture.</param>
[Collection("Cli")]
public sealed class RehearseCommandTests(CliFixture fixture)
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

    /// <summary>
    /// Tests that the rehearse command returns exit code 1 when the specified file does not exist.
    /// </summary>
    [Fact]
    public async Task Rehearse_WhenFileDoesNotExist_ReturnsExitCode1()
    {
        (int exitCode, string _, string _) = await this.RunAsync("rehearse nonexistent.topsy --tiptoe");

        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the rehearse command writes an error message to stderr when the specified file does not exist.
    /// </summary>
    [Fact]
    public async Task Rehearse_WhenFileDoesNotExist_WritesErrorToStderr()
    {
        (int _, string _, string stderr) = await this.RunAsync("rehearse nonexistent.topsy --tiptoe");

        stderr.ShouldContain("File not found");
    }

    /// <summary>
    /// Tests that the rehearse command returns exit code 1 when the specified file contains syntax errors.
    /// </summary>
    [Fact]
    public async Task Rehearse_WhenFileHasSyntaxErrors_ReturnsExitCode1()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), InvalidSource);

        (int exitCode, string _, string _) = await this.RunAsync("rehearse prog.topsy --tiptoe");

        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the rehearse command returns exit code 0 when the specified file is valid.
    /// </summary>
    [Fact]
    public async Task Rehearse_WhenFileIsValid_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string _) = await this.RunAsync("rehearse prog.topsy --tiptoe");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the rehearse command writes nothing to stderr when the specified file is valid.
    /// </summary>
    [Fact]
    public async Task Rehearse_WhenFileIsValid_WritesNothingToStderr()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int _, string _, string stderr) = await this.RunAsync("rehearse prog.topsy --tiptoe");

        stderr.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the rehearse command returns exit code 0 when invoked via the check alias with a valid file.
    /// </summary>
    [Fact]
    public async Task Rehearse_UsingCheckAlias_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string _) = await this.RunAsync("check prog.topsy --tiptoe");

        exitCode.ShouldBe(0);
    }
}
