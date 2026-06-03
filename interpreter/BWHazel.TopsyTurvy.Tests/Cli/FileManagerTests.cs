using BWHazel.TopsyTurvy.Cli;
using Testably.Abstractions.Testing;

namespace BWHazel.TopsyTurvy.Tests.Cli;

/// <summary>
/// Tests for the <see cref="FileManager"/> class.
/// </summary>
public class FileManagerTests : CliTestBase
{
    private const string TestFilePath = "/prog.topsy";
    private const string TestDirectoryPath = "/myproject";
    private const string TestTitle = "My Programme";
    private const string TestSubtitle = "A Subtitle";

    /// <summary>
    /// Tests that the <see cref="FileManager.BuildFileContent"/> method returns content containing the title.
    /// </summary>
    [Fact]
    public void BuildFileContent_WithTitleOnly_ReturnsContentWithTitle()
    {
        string content = FileManager.BuildFileContent(TestTitle, subtitle: null);

        Assert.Contains(TestTitle, content);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.BuildFileContent"/> method does not include a subtitle line when no subtitle is provided.
    /// </summary>
    [Fact]
    public void BuildFileContent_WithTitleOnly_DoesNotIncludeSubtitleLine()
    {
        string content = FileManager.BuildFileContent(TestTitle, subtitle: null);

        Assert.DoesNotContain("or,", content);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.BuildFileContent"/> method includes a subtitle line when a subtitle is provided.
    /// </summary>
    [Fact]
    public void BuildFileContent_WithSubtitle_IncludesSubtitleLine()
    {
        string content = FileManager.BuildFileContent(TestTitle, TestSubtitle);

        Assert.Contains("or,", content);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.BuildFileContent"/> method formats the subtitle correctly.
    /// </summary>
    [Fact]
    public void BuildFileContent_WithSubtitle_FormatsSubtitleCorrectly()
    {
        string content = FileManager.BuildFileContent(TestTitle, TestSubtitle);

        Assert.Contains($"  or, \"{TestSubtitle}\"", content);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.TryReadSource"/> method returns a failure when the file does not exist.
    /// </summary>
    [Fact]
    public void TryReadSource_WhenFileDoesNotExist_ReturnsFailure()
    {
        (bool success, string? _) = FileManager.TryReadSource(TestFilePath, out _, this.fileSystem);

        Assert.False(success);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.TryReadSource"/> method returns success and the file content when the file exists.
    /// </summary>
    [Fact]
    public void TryReadSource_WhenFileExists_ReturnsSuccessAndContent()
    {
        this.fileSystem.Initialize()
            .WithFile(TestFilePath)
            .Which(fileManipulator => fileManipulator.HasStringContent("HARK!"));

        (bool success, string? _) = FileManager.TryReadSource(TestFilePath, out string source, this.fileSystem);

        Assert.True(success);
        Assert.Contains("HARK!", source);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.TryCreateProgrammeFile"/> method returns a failure when the file already exists.
    /// </summary>
    [Fact]
    public void TryCreateProgrammeFile_WhenFileAlreadyExists_ReturnsFailure()
    {
        this.fileSystem.Initialize()
            .WithFile(TestFilePath);

        (bool success, string? _) = FileManager.TryCreateProgrammeFile(TestFilePath, TestTitle, subtitle: null, this.fileSystem);

        Assert.False(success);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.TryCreateProgrammeFile"/> method does not overwrite an existing file.
    /// </summary>
    [Fact]
    public void TryCreateProgrammeFile_WhenFileAlreadyExists_DoesNotOverwriteContent()
    {
        const string originalContent = "ORIGINAL CONTENT";
        this.fileSystem.Initialize()
            .WithFile(TestFilePath)
            .Which(fileManipulator => fileManipulator.HasStringContent(originalContent));

        FileManager.TryCreateProgrammeFile(TestFilePath, TestTitle, subtitle: null, this.fileSystem);

        string remainingContent = this.fileSystem.File.ReadAllText(TestFilePath);
        Assert.Equal(originalContent, remainingContent);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.TryCreateProgrammeFile"/> method returns success when the file does not exist.
    /// </summary>
    [Fact]
    public void TryCreateProgrammeFile_WhenFileDoesNotExist_ReturnsSuccess()
    {
        (bool success, string? _) = FileManager.TryCreateProgrammeFile(TestFilePath, TestTitle, subtitle: null, this.fileSystem);

        Assert.True(success);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.TryCreateProgrammeFile"/> method creates a file beginning with the HARK! header.
    /// </summary>
    [Fact]
    public void TryCreateProgrammeFile_WhenFileDoesNotExist_CreatesFileWithHarkTitle()
    {
        FileManager.TryCreateProgrammeFile(TestFilePath, TestTitle, subtitle: null, this.fileSystem);

        string content = this.fileSystem.File.ReadAllText(TestFilePath);

        Assert.StartsWith($"HARK! \"{TestTitle}\"", content);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.TryMountProject"/> method returns a failure when the directory already exists.
    /// </summary>
    [Fact]
    public void TryMountProject_WhenDirectoryAlreadyExists_ReturnsFailure()
    {
        this.fileSystem.Initialize()
            .WithSubdirectory(TestDirectoryPath);

        (bool success, string? _) = FileManager.TryMountProject(TestDirectoryPath, TestTitle, subtitle: null, hollow: false, this.fileSystem);

        Assert.False(success);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.TryMountProject"/> method returns success when the directory does not exist.
    /// </summary>
    [Fact]
    public void TryMountProject_WhenDirectoryDoesNotExist_ReturnsSuccess()
    {
        (bool success, string? _) = FileManager.TryMountProject(TestDirectoryPath, TestTitle, subtitle: null, hollow: false, this.fileSystem);

        Assert.True(success);
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.TryMountProject"/> method creates the project directory.
    /// </summary>
    [Fact]
    public void TryMountProject_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        FileManager.TryMountProject(TestDirectoryPath, TestTitle, subtitle: null, hollow: false, this.fileSystem);

        Assert.True(this.fileSystem.Directory.Exists(TestDirectoryPath));
    }

    /// <summary>
    /// Tests that the <see cref="FileManager.TryMountProject"/> method creates a .topsy entry-point file when hollow not provided.
    /// </summary>
    [Fact]
    public void TryMountProject_WhenNotHollow_CreatesEntryPointFile()
    {
        FileManager.TryMountProject(TestDirectoryPath, TestTitle, subtitle: null, hollow: false, this.fileSystem);

        bool topsyFileExists = this.fileSystem.File.Exists(
            this.fileSystem.Path.Combine(TestDirectoryPath, "myproject.topsy"));

        Assert.True(topsyFileExists);
    }
}
