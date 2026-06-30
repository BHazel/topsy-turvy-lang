using System.IO;
using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Tests the CLI panel rendering.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="PanelOutputTests"/> class.
/// All assertions target stdout.  Without --tiptoe, the <c>PanelHelper</c> class routes all output
/// through <c>AnsiConsole.Write</c>, which writes to standard output. When stdout
/// is redirected, ANSI escape codes are stripped but panel header text and body text remain in the captured
/// output.
/// </remarks>
/// <param name="fixture">The shared CLI fixture.</param>
[Collection("Cli")]
public sealed class PanelOutputTests(CliFixture fixture) : CliTestBase(fixture)
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
    /// Tests that the commission command writes the Commissioned! panel to stdout on success.
    /// </summary>
    [Fact]
    public async Task Commission_OnSuccess_WritesCommissionedPanel()
    {
        (int _, string stdout, string _) = await this.RunAsync("commission my-topsy-turvy-programme.topsy");

        stdout.ShouldContain("Commissioned!");
        stdout.ShouldContain("my-topsy-turvy-programme.topsy");
    }

    /// <summary>
    /// Tests that the commission command writes the Why, Damme! error panel to stdout when the extension is invalid.
    /// </summary>
    [Fact]
    public async Task Commission_WithInvalidExtension_WritesWhyDammePanel()
    {
        (int _, string stdout, string _) = await this.RunAsync("commission myfile.txt");

        stdout.ShouldContain("Why, Damme!");
        stdout.ShouldContain("Only .topsy");
    }

    /// <summary>
    /// Tests that the commission command writes the Why, Damme! error panel to stdout when the file already exists.
    /// </summary>
    [Fact]
    public async Task Commission_WhenFileExists_WritesWhyDammePanel()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), string.Empty);

        (int _, string stdout, string _) = await this.RunAsync("commission prog.topsy");

        stdout.ShouldContain("Why, Damme!");
        stdout.ShouldContain("File already exists");
    }

    /// <summary>
    /// Tests that the mount command writes the Mounted! panel to stdout on success.
    /// </summary>
    [Fact]
    public async Task Mount_OnSuccess_WritesMountedPanel()
    {
        (int _, string stdout, string _) = await this.RunAsync("mount myproject");

        stdout.ShouldContain("Mounted!");
        stdout.ShouldContain("myproject/");
    }

    /// <summary>
    /// Tests that the mount command writes the Why, Damme! error panel to stdout when the directory already exists.
    /// </summary>
    [Fact]
    public async Task Mount_WhenDirectoryExists_WritesWhyDammePanel()
    {
        Directory.CreateDirectory(Path.Combine(this.WorkingDirectory, "myproject"));

        (int _, string stdout, string _) = await this.RunAsync("mount myproject");

        stdout.ShouldContain("Why, Damme!");
        stdout.ShouldContain("Directory already exists");
    }

    /// <summary>
    /// Tests that the rehearse command writes the Now Rehearsing... panel to stdout when starting.
    /// </summary>
    [Fact]
    public async Task Rehearse_OnStart_WritesNowRehearsingPanel()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "my-topsy-turvy-programme.topsy"), ValidSource);

        (int _, string stdout, string _) = await this.RunAsync("rehearse my-topsy-turvy-programme.topsy");

        stdout.ShouldContain("Now Rehearsing...");
        stdout.ShouldContain("my-topsy-turvy-programme.topsy");
    }

    /// <summary>
    /// Tests that the rehearse command writes the Rehearsal Over! panel to stdout on success.
    /// </summary>
    [Fact]
    public async Task Rehearse_OnSuccess_WritesRehearsalOverPanel()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int _, string stdout, string _) = await this.RunAsync("rehearse prog.topsy");

        stdout.ShouldContain("Rehearsal Over!");
    }

    /// <summary>
    /// Tests that the rehearse command writes the Crushed Again! error panel to stdout when the file has syntax errors.
    /// </summary>
    [Fact]
    public async Task Rehearse_WhenSyntaxError_WritesCrushedAgainPanel()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), InvalidSource);

        (int _, string stdout, string _) = await this.RunAsync("rehearse prog.topsy");

        stdout.ShouldContain("Crushed Again!");
        stdout.ShouldContain("Syntax Error");
    }

    /// <summary>
    /// Tests that the rehearse command writes the Why, Damme! error panel to stdout when the file is not found.
    /// </summary>
    [Fact]
    public async Task Rehearse_WhenFileNotFound_WritesWhyDammePanel()
    {
        (int _, string stdout, string _) = await this.RunAsync("rehearse nonexistent.topsy");

        stdout.ShouldContain("Why, Damme!");
        stdout.ShouldContain("File not found");
    }

    /// <summary>
    /// Tests that the perform command writes the Now Performing... panel to stdout when starting.
    /// </summary>
    [Fact]
    public async Task Perform_OnStart_WritesNowPerformingPanel()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "my-topsy-turvy-programme.topsy"), ValidSource);

        (int _, string stdout, string _) = await this.RunAsync("perform my-topsy-turvy-programme.topsy");

        stdout.ShouldContain("Now Performing...");
        stdout.ShouldContain("my-topsy-turvy-programme.topsy");
    }

    /// <summary>
    /// Tests that the perform command writes the Performance Over! panel to stdout on success.
    /// </summary>
    [Fact]
    public async Task Perform_OnSuccess_WritesPerformanceOverPanel()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int _, string stdout, string _) = await this.RunAsync("perform prog.topsy");

        stdout.ShouldContain("Performance Over!");
    }

    /// <summary>
    /// Tests that the perform command writes the Crushed Again! error panel to stdout when the file has syntax errors.
    /// </summary>
    [Fact]
    public async Task Perform_WhenSyntaxError_WritesCrushedAgainPanel()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), InvalidSource);

        (int _, string stdout, string _) = await this.RunAsync("perform prog.topsy");

        stdout.ShouldContain("Crushed Again!");
        stdout.ShouldContain("Syntax Error");
    }

    /// <summary>
    /// Tests that the perform command writes the A Hideous Curse! error panel to stdout when the file causes a runtime error.
    /// </summary>
    [Fact]
    public async Task Perform_WhenRuntimeError_WritesAHideousCursePanel()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), RuntimeErrorSource);

        (int _, string stdout, string _) = await this.RunAsync("perform prog.topsy");

        stdout.ShouldContain("A Hideous Curse!");
        stdout.ShouldContain("Runtime Error");
    }
}
