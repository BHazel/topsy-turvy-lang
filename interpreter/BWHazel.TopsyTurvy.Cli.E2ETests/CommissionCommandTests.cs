using System.IO;
using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Tests for the CLI commission (new) command.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="CommissionCommandTests"/> class.
/// </remarks>
/// <param name="fixture">The shared CLI fixture.</param>
[Collection("Cli")]
public sealed class CommissionCommandTests(CliFixture fixture)
    : CliTestBase(fixture)
{

    /// <summary>
    /// Tests that the commission command returns exit code 1 when the filename has an invalid extension.
    /// </summary>
    [Fact]
    public async Task Commission_WithInvalidExtension_ReturnsExitCode1()
    {
        (int exitCode, string _, string _) = await this.RunAsync("commission myfile.txt --tiptoe");

        Assert.Equal(1, exitCode);
    }

    /// <summary>
    /// Tests that the commission command writes an error message to stderr when the filename has an invalid extension.
    /// </summary>
    [Fact]
    public async Task Commission_WithInvalidExtension_WritesErrorToStderr()
    {
        (int _, string _, string stderr) = await this.RunAsync("commission myfile.txt --tiptoe");

        Assert.Contains("Only .topsy files are supported", stderr);
    }

    /// <summary>
    /// Tests that the commission command returns exit code 1 when the target file already exists.
    /// </summary>
    [Fact]
    public async Task Commission_WhenFileAlreadyExists_ReturnsExitCode1()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), string.Empty);

        (int exitCode, string _, string _) = await this.RunAsync("commission prog.topsy --tiptoe");

        Assert.Equal(1, exitCode);
    }

    /// <summary>
    /// Tests that the commission command writes an error message to stderr when the target file already exists.
    /// </summary>
    [Fact]
    public async Task Commission_WhenFileAlreadyExists_WritesErrorToStderr()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), string.Empty);

        (int _, string _, string stderr) = await this.RunAsync("commission prog.topsy --tiptoe");

        Assert.Contains("File already exists", stderr);
    }

    /// <summary>
    /// Tests that the commission command returns exit code 0 when the target file does not exist.
    /// </summary>
    [Fact]
    public async Task Commission_WhenFileDoesNotExist_ReturnsExitCode0()
    {
        (int exitCode, string _, string _) = await this.RunAsync("commission prog.topsy --tiptoe");

        Assert.Equal(0, exitCode);
    }

    /// <summary>
    /// Tests that the commission command creates the target file when it does not already exist.
    /// </summary>
    [Fact]
    public async Task Commission_WhenFileDoesNotExist_CreatesFile()
    {
        await this.RunAsync("commission prog.topsy --tiptoe");

        Assert.True(File.Exists(Path.Combine(this.WorkingDirectory, "prog.topsy")));
    }

    /// <summary>
    /// Tests that the commission command writes the default HARK! title to the file when no title is specified.
    /// </summary>
    [Fact]
    public async Task Commission_WithNoTitle_WritesDefaultTitleToFile()
    {
        await this.RunAsync("commission prog.topsy --tiptoe");

        string content = File.ReadAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"));
        Assert.Contains("HARK! \"Programme\"", content);
    }

    /// <summary>
    /// Tests that the commission command writes the specified HARK! title to the file.
    /// </summary>
    [Fact]
    public async Task Commission_WithTitle_WritesHarkTitleToFile()
    {
        await this.RunAsync("commission prog.topsy --title \"My Show\" --tiptoe");

        string content = File.ReadAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"));
        Assert.Contains("HARK! \"My Show\"", content);
    }

    /// <summary>
    /// Tests that the commission command writes the specified subtitle to the file.
    /// </summary>
    [Fact]
    public async Task Commission_WithSubtitle_WritesSubtitleToFile()
    {
        await this.RunAsync("commission prog.topsy --or \"A Subtitle\" --tiptoe");

        string content = File.ReadAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"));
        Assert.Contains("or, \"A Subtitle\"", content);
    }

    /// <summary>
    /// Tests that the commission command writes both the title and subtitle to the file when both are specified.
    /// </summary>
    [Fact]
    public async Task Commission_WithTitleAndSubtitle_WritesBothToFile()
    {
        await this.RunAsync("commission prog.topsy --title \"My Show\" --or \"A Subtitle\" --tiptoe");

        string content = File.ReadAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"));
        Assert.Contains("HARK! \"My Show\"", content);
        Assert.Contains("or, \"A Subtitle\"", content);
    }

    /// <summary>
    /// Tests that the commission command creates the target file when invoked via the new alias.
    /// </summary>
    [Fact]
    public async Task Commission_UsingNewAlias_CreatesFile()
    {
        (int exitCode, string _, string _) = await this.RunAsync("new prog.topsy --tiptoe");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(this.WorkingDirectory, "prog.topsy")));
    }
}
