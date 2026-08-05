using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Tests for the CLI <c>incantation</c> (LSP) command.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="IncantationCommandTests"/> class.
/// </remarks>
/// <param name="fixture">The shared CLI fixture.</param>
[Collection("Cli")]
public sealed class IncantationCommandTests(CliFixture fixture)
    : CliTestBase(fixture)
{
    /// <summary>
    /// Tests that a missing admitted external library reports a clean, single-line error to stderr and exits
    /// non-zero before the language server starts, rather than corrupting the JSON-RPC stdout stream.
    /// </summary>
    [Fact]
    public async Task Incantation_WithMissingExternalLibrary_ReturnsExitCode1AndCleanStderr()
    {
        (int exitCode, string stdout, string stderr) = await this.RunAsync("incantation --admit nonexistent.dll");

        exitCode.ShouldBe(1);
        stdout.ShouldBeEmpty();
        stderr.ShouldContain("not found");
    }

    /// <summary>
    /// Tests that a missing admitted external library is also rejected when specified via the include alias.
    /// </summary>
    [Fact]
    public async Task Incantation_WithMissingExternalLibraryUsingIncludeAlias_ReturnsExitCode1()
    {
        (int exitCode, string _, string stderr) = await this.RunAsync("incantation --include nonexistent.dll");

        exitCode.ShouldBe(1);
        stderr.ShouldContain("not found");
    }
}
