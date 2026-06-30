using System.IO;
using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Tests for the CLI playbill (docs) command.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="PlaybillCommandTests"/> class.
/// </remarks>
/// <param name="fixture">The shared CLI fixture.</param>
[Collection("Cli")]
public sealed class PlaybillCommandTests(CliFixture fixture)
    : CliTestBase(fixture)
{
    private const string SourceWithoutDocComments =
        """
        HARK! "Test"
        PRINCIPALS
        THE CURTAIN RISES.
        BEHOLD "OK"
        FINALE.
        """;

    private const string SourceWithDocComments =
        """
        HARK! "Test"
        PRINCIPALS
        (ASIDE, AT SOME LENGTH:
          LEGEND: Holds the count.
        END OF ASIDE.)
        PRAY WELCOME Count AS A PEER BEING 42
        THE CURTAIN RISES.
        BEHOLD Count
        FINALE.
        """;

    private const string InvalidSource = "FINALE.";

    /// <summary>
    /// Tests that the playbill command returns exit code 1 when the specified file does not exist.
    /// </summary>
    [Fact]
    public async Task Playbill_WhenFileDoesNotExist_ReturnsExitCode1()
    {
        (int exitCode, string _, string _) = await this.RunAsync("playbill nonexistent.topsy --tiptoe");

        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the playbill command writes an error message to stderr when the specified file does not exist.
    /// </summary>
    [Fact]
    public async Task Playbill_WhenFileDoesNotExist_WritesErrorToStderr()
    {
        (int _, string _, string stderr) = await this.RunAsync("playbill nonexistent.topsy --tiptoe");

        stderr.ShouldContain("File not found");
    }

    /// <summary>
    /// Tests that the playbill command returns exit code 1 when the specified file contains syntax errors.
    /// </summary>
    [Fact]
    public async Task Playbill_WhenFileHasSyntaxErrors_ReturnsExitCode1()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), InvalidSource);

        (int exitCode, string _, string _) = await this.RunAsync("playbill prog.topsy --tiptoe");

        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the playbill command returns exit code 0 when the specified file has no documentation comments.
    /// </summary>
    [Fact]
    public async Task Playbill_WhenFileHasNoDocComments_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), SourceWithoutDocComments);

        (int exitCode, string _, string _) = await this.RunAsync("playbill prog.topsy --tiptoe");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the playbill command writes Markdown documentation to stdout when the file has documentation comments.
    /// </summary>
    [Fact]
    public async Task Playbill_WhenFileHasDocComments_WritesMarkdownToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), SourceWithDocComments);

        (int _, string stdout, string _) = await this.RunAsync("playbill prog.topsy --tiptoe");

        stdout.ShouldContain("Holds the count");
    }

    /// <summary>
    /// Tests that the playbill command returns exit code 0 when invoked via the docs alias with a valid file.
    /// </summary>
    [Fact]
    public async Task Playbill_UsingDocsAlias_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), SourceWithDocComments);

        (int exitCode, string _, string _) = await this.RunAsync("docs prog.topsy --tiptoe");

        exitCode.ShouldBe(0);
    }
}
