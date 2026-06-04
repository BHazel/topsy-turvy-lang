using System.IO;
using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Tests for the CLI mount (init) command.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="MountCommandTests"/> class.
/// </remarks>
/// <param name="fixture">The shared CLI fixture.</param>
[Collection("Cli")]
public sealed class MountCommandTests(CliFixture fixture)
    : CliTestBase(fixture)
{

    /// <summary>
    /// Tests that the mount command returns exit code 1 when the target directory already exists.
    /// </summary>
    [Fact]
    public async Task Mount_WhenDirectoryAlreadyExists_ReturnsExitCode1()
    {
        Directory.CreateDirectory(Path.Combine(this.WorkingDirectory, "myproject"));

        (int exitCode, string _, string _) = await this.RunAsync("mount myproject --tiptoe");

        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the mount command writes an error message to stderr when the target directory already exists.
    /// </summary>
    [Fact]
    public async Task Mount_WhenDirectoryAlreadyExists_WritesErrorToStderr()
    {
        Directory.CreateDirectory(Path.Combine(this.WorkingDirectory, "myproject"));

        (int _, string _, string stderr) = await this.RunAsync("mount myproject --tiptoe");

        stderr.ShouldContain("Directory already exists");
    }

    /// <summary>
    /// Tests that the mount command returns exit code 0 when the target directory does not exist.
    /// </summary>
    [Fact]
    public async Task Mount_WhenDirectoryDoesNotExist_ReturnsExitCode0()
    {
        (int exitCode, string _, string _) = await this.RunAsync("mount myproject --tiptoe");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the mount command creates the target directory when it does not already exist.
    /// </summary>
    [Fact]
    public async Task Mount_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        await this.RunAsync("mount myproject --tiptoe");

        Directory.Exists(Path.Combine(this.WorkingDirectory, "myproject")).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the mount command creates the entry-point file inside the project directory when not hollow.
    /// </summary>
    [Fact]
    public async Task Mount_WhenNotHollow_CreatesEntryPointFile()
    {
        await this.RunAsync("mount myproject --tiptoe");

        File.Exists(Path.Combine(this.WorkingDirectory, "myproject", "myproject.topsy")).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the mount command does not create the entry-point file when the hollow flag is specified.
    /// </summary>
    [Fact]
    public async Task Mount_WhenHollow_DoesNotCreateEntryPointFile()
    {
        await this.RunAsync("mount myproject --hollow --tiptoe");

        File.Exists(Path.Combine(this.WorkingDirectory, "myproject", "myproject.topsy")).ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the mount command writes the default HARK! title to the entry-point file when no title is specified.
    /// </summary>
    [Fact]
    public async Task Mount_WithNoTitle_WritesDefaultTitleToEntryPointFile()
    {
        await this.RunAsync("mount myproject --tiptoe");

        string content = File.ReadAllText(Path.Combine(this.WorkingDirectory, "myproject", "myproject.topsy"));
        content.ShouldContain("HARK! \"Programme\"");
    }

    /// <summary>
    /// Tests that the mount command writes the specified HARK! title to the entry-point file.
    /// </summary>
    [Fact]
    public async Task Mount_WithTitle_WritesHarkTitleToEntryPointFile()
    {
        await this.RunAsync("mount myproject --title \"Grand Opera\" --tiptoe");

        string content = File.ReadAllText(Path.Combine(this.WorkingDirectory, "myproject", "myproject.topsy"));
        content.ShouldContain("HARK! \"Grand Opera\"");
    }

    /// <summary>
    /// Tests that the mount command writes the specified subtitle to the entry-point file.
    /// </summary>
    [Fact]
    public async Task Mount_WithSubtitle_WritesSubtitleToEntryPointFile()
    {
        await this.RunAsync("mount myproject --or \"A Subtitle\" --tiptoe");

        string content = File.ReadAllText(Path.Combine(this.WorkingDirectory, "myproject", "myproject.topsy"));
        content.ShouldContain("or, \"A Subtitle\"");
    }

    /// <summary>
    /// Tests that the mount command writes both the title and subtitle to the entry-point file when both are specified.
    /// </summary>
    [Fact]
    public async Task Mount_WithTitleAndSubtitle_WritesBothToEntryPointFile()
    {
        await this.RunAsync("mount myproject --title \"Grand Opera\" --or \"A Subtitle\" --tiptoe");

        string content = File.ReadAllText(Path.Combine(this.WorkingDirectory, "myproject", "myproject.topsy"));
        content.ShouldContain("HARK! \"Grand Opera\"");
        content.ShouldContain("or, \"A Subtitle\"");
    }

    /// <summary>
    /// Tests that the mount command creates the target directory when invoked via the init alias.
    /// </summary>
    [Fact]
    public async Task Mount_UsingInitAlias_CreatesDirectory()
    {
        (int exitCode, string _, string _) = await this.RunAsync("init myproject --tiptoe");

        exitCode.ShouldBe(0);
        Directory.Exists(Path.Combine(this.WorkingDirectory, "myproject")).ShouldBeTrue();
    }
}
