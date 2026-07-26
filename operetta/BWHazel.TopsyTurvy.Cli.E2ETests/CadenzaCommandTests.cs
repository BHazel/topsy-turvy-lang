using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Tests for the CLI cadenza (interactive) REPL command.
/// </summary>
/// <remarks>
/// All tests pipe commands via standard input and rely on the REPL non-interactive detection
/// via <see cref="System.Console.IsInputRedirected"/> to suppress panels and styled prompts,
/// making standard output and error assertions straightforward.
/// </remarks>
/// <param name="fixture">The shared CLI fixture.</param>
[Collection("Cli")]
public sealed class CadenzaCommandTests(CliFixture fixture)
    : CliTestBase(fixture)
{
    /// <summary>
    /// Tests that the cadenza command exits cleanly with exit code 0 when the exit command is sent.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithExitCommand_ReturnsExitCode0()
    {
        (int exitCode, string _, string _) = await this.RunWithStdinAsync("cadenza", ":exit\n");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the cadenza command prints programme output to stdout.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithValidStatement_PrintsOutputToStdout()
    {
        (int _, string stdout, string _) = await this.RunWithStdinAsync("cadenza", "BEHOLD \"Hello\"\n:exit\n");

        stdout.ShouldContain("Hello");
    }

    /// <summary>
    /// Tests that the cadenza command writes an error message when given invalid syntax.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithSyntaxError_WritesError()
    {
        (int _, string stdout, string stderr) = await this.RunWithStdinAsync("cadenza", "BROKEN SYNTAX\n:exit\n");

        (stdout + stderr).ShouldNotBe(string.Empty);
    }

    /// <summary>
    /// Tests that a variable declared in one call is accessible in a subsequent call.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithDeclaration_PersistsAcrossSubsequentCalls()
    {
        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            "cadenza",
            "PRAY WELCOME x AS A PEER BEING 42\nBEHOLD x\n:exit\n");

        stdout.ShouldContain("42");
    }

    /// <summary>
    /// Tests that the :begone command does not cause a non-zero exit code.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithBegoneCommand_ReturnsExitCode0()
    {
        (int exitCode, string _, string _) = await this.RunWithStdinAsync("cadenza", ":begone\n:exit\n");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the :entracte command writes the REPL command table to stdout.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithEntracteCommand_WritesHelpToStdout()
    {
        (int _, string stdout, string _) = await this.RunWithStdinAsync("cadenza", ":entracte\n:exit\n");

        stdout.ShouldContain(":exit");
    }

    /// <summary>
    /// Tests that the cadenza command accepts the interactive alias and exits cleanly.
    /// </summary>
    [Fact]
    public async Task Cadenza_UsingInteractiveAlias_ReturnsExitCode0()
    {
        (int exitCode, string _, string _) = await this.RunWithStdinAsync("interactive", ":exit\n");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the cadenza command with the tiptoe flag exits cleanly.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithTiptoeFlag_ReturnsExitCode0()
    {
        (int exitCode, string _, string _) = await this.RunWithStdinAsync("cadenza --tiptoe", ":exit\n");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the :quit alias for :exit also exits cleanly.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithQuitAlias_ReturnsExitCode0()
    {
        (int exitCode, string _, string _) = await this.RunWithStdinAsync("cadenza", ":quit\n");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the :madrigal alias switches to multi-line mode, allowing multiple statements
    /// to be executed when a blank line is submitted.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithMadrigalAlias_ExecutesMultipleLines()
    {
        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            "cadenza",
            ":madrigal\nBEHOLD \"A\"\nBEHOLD \"B\"\n\n:exit\n");

        stdout.ShouldContain("A");
        stdout.ShouldContain("B");
    }

    /// <summary>
    /// Tests that a runtime error (assigning to a CONSERVATIVE constant) writes a diagnostic to stderr.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithRuntimeError_WritesErrorToStderr()
    {
        (int _, string _, string stderr) = await this.RunWithStdinAsync(
            "cadenza",
            "PRAY WELCOME x AS A CONSERVATIVE PEER BEING 10\nx IS APPOINTED 20\n:exit\n");

        stderr.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Tests that the session continues after a syntax error and a subsequent valid call still produces output.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithSyntaxError_SessionContinues()
    {
        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            "cadenza",
            "BROKEN SYNTAX\nBEHOLD \"recovered\"\n:exit\n");

        stdout.ShouldContain("recovered");
    }

    /// <summary>
    /// Tests that PRAY TELL reads the next line from stdin and the assigned value is
    /// accessible in a subsequent REPL turn.
    /// </summary>
    /// <remarks>
    /// Stdin Layout: Call 1 declares the variable, call 2 executes PRAY TELL consuming
    /// the following stdin line as the user answer and call 3 prints the variable.
    /// </remarks>
    [Fact]
    public async Task Cadenza_WithPrayTell_ReadsStdinAndPersistsValue()
    {
        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            "cadenza",
            "PRAY WELCOME name AS A YARN\nPRAY TELL name\nKo-Ko\nBEHOLD name\n:exit\n");

        stdout.ShouldContain("Ko-Ko");
    }

    /// <summary>
    /// Tests that the --tiptoe flag produces programme output to stdout.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithTiptoeFlag_PrintsOutputToStdout()
    {
        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            "cadenza --tiptoe",
            "BEHOLD \"tiptoe output\"\n:exit\n");

        stdout.ShouldContain("tiptoe output");
    }

    /// <summary>
    /// Tests that a variable mutated in one call retains its updated value in a subsequent call
    /// exercising state persistence across three consecutive calls.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithVariableMutation_PersistsUpdatedValueAcrossCalls()
    {
        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            "cadenza",
            "PRAY WELCOME x AS A PEER BEING 10\n" +
            "x IS APPOINTED SUM OF x AND 5\n" +
            "BEHOLD x\n" +
            ":exit\n");

        stdout.ShouldContain("15");
    }

    /// <summary>
    /// Tests that the :armoury command lists a function parameter as a readable "name: TYPE" pair.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithArmouryCommand_ListsFunctionParameterAsReadablePair()
    {
        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            "cadenza",
            "IT IS MY DUTY TO PERFORM Greet UNDER THE TERMS OF name AS A YARN TO FIND YARN AND SO I FIND name MY DUTY IS DISCHARGED.\n" +
            ":armoury\n" +
            ":exit\n");

        stdout.ShouldContain("name: YARN");
        stdout.ShouldNotContain("TypedParameter");
    }

    /// <summary>
    /// Tests that the cadenza command calls a function from an external library admitted at startup.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithAdmittedExternalLibrary_CallsExternalFunction()
    {
        (int _, string stdout, string _) = await this.RunWithStdinAsync(
            $"cadenza --admit \"{FixtureLibraryPath}\"",
            "BEHOLD SUMMON Greet WITH \"Ko-Ko\" IF YOU PLEASE.\n:exit\n");

        stdout.ShouldContain("Hello, Ko-Ko!");
    }

    /// <summary>
    /// Tests that the cadenza command returns exit code 1 and reports a clean error when the admitted external
    /// library path does not exist, without ever starting the REPL loop.
    /// </summary>
    [Fact]
    public async Task Cadenza_WithMissingExternalLibrary_ReturnsExitCode1()
    {
        (int exitCode, string _, string stderr) = await this.RunWithStdinAsync("cadenza --admit nonexistent.dll --tiptoe", ":exit\n");

        exitCode.ShouldBe(1);
        stderr.ShouldContain("not found");
    }
}
