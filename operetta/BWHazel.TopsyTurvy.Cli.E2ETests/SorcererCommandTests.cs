using System.IO;
using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Tests for the CLI <c>sorcerer</c> command.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="SorcererCommandTests"/> class.
/// </remarks>
/// <param name="fixture">The shared CLI fixture.</param>
[Collection("Cli")]
public sealed class SorcererCommandTests(CliFixture fixture)
    : CliTestBase(fixture)
{
    private const string ValidSource =
        """
        HARK! "Test"
        PRINCIPALS
          PRAY WELCOME result AS A PEER
        THE CURTAIN RISES.
        ASIDE: This is a comment.
        result IS APPOINTED SUM OF 10 AND 3
        AND SO I FIND result
        FINALE.
        """;

    /// <summary>
    /// Tests that <c>sorcerer</c> returns exit code 1 when the specified file does not exist.
    /// </summary>
    [Fact]
    public async Task Sorcerer_WhenFileDoesNotExist_ReturnsExitCode1()
    {
        (int exitCode, string _, string _) = await this.RunAsync("sorcerer nonexistent.topsy --emit preprocess --tiptoe");

        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that <c>sorcerer</c> writes an error message to stderr when the specified file does not exist.
    /// </summary>
    [Fact]
    public async Task Sorcerer_WhenFileDoesNotExist_WritesErrorToStderr()
    {
        (int _, string _, string stderr) = await this.RunAsync("sorcerer nonexistent.topsy --emit preprocess --tiptoe");

        stderr.ShouldContain("File not found");
    }

    /// <summary>
    /// Tests that <c>--emit preprocess</c> writes the pre-processed source to stdout with comments stripped.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitPreprocess_WritesPreprocessedOutputToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("sorcerer prog.topsy --emit preprocess --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("HARK!");
        stdout.ShouldNotContain("This is a comment");
    }

    /// <summary>
    /// Tests that <c>--emit</c> accepts the <c>cue</c> alias for the preprocess format.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitCueAlias_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string _) = await this.RunAsync("sorcerer prog.topsy --emit cue --tiptoe");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that <c>--emit</c> accepts the <c>p</c> alias for the preprocess format.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitShortAliasP_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string _) = await this.RunAsync("sorcerer prog.topsy -e p --tiptoe");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that <c>--emit ast</c> writes the Topsy Turvy AST as JSON to stdout.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitAst_WritesJsonToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("sorcerer prog.topsy --emit ast --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("\"$type\"");
        stdout.ShouldContain("ProgramNode");
    }

    /// <summary>
    /// Tests that <c>--emit</c> accepts the <c>promptbook</c> alias for the AST format.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitPromptbookAlias_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string _) = await this.RunAsync("sorcerer prog.topsy --emit promptbook --tiptoe");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that <c>--emit ast --abridged</c> removes whitespace from the JSON output.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitAstAbridged_ProducesCompactJson()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int _, string stdout, string _) = await this.RunAsync("sorcerer prog.topsy --emit ast --abridged --tiptoe");

        stdout.ShouldNotContain("\n  ");
    }

    /// <summary>
    /// Tests that <c>--emit utopir</c> writes UtopIR source text to stdout.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitUtopIr_WritesUtopIrTextToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("sorcerer prog.topsy --emit utopir --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("welcome");
        stdout.ShouldContain("£result");
    }

    /// <summary>
    /// Tests that <c>--emit utopir-ast</c> writes the UtopIR AST as JSON to stdout.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitUtopIrAst_WritesJsonToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("sorcerer prog.topsy --emit utopir-ast --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("\"$type\"");
        stdout.ShouldContain("WelcomeInstruction");
    }

    /// <summary>
    /// Tests that <c>--emit utopir-ast --chromatic</c> exits successfully with syntax-highlighted JSON.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitUtopIrAstChromatic_ReturnsExitCode0()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string _) = await this.RunAsync("sorcerer prog.topsy --emit utopir-ast --chromatic --tiptoe");

        exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Tests that <c>--emit dotnet-cil</c> writes CIL disassembly text to stdout.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitDotNetCil_WritesIlTextToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("sorcerer prog.topsy --emit dotnet-cil --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("ldc.i4");
        stdout.ShouldContain("ret");
    }

    /// <summary>
    /// Tests that <c>sorcerer</c> with no <c>--emit</c>/<c>--target</c> flags defaults to building a .NET executable.
    /// </summary>
    [Fact]
    public async Task Sorcerer_NoFlags_BuildsDotNetExecutableByDefault()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string _) = await this.RunAsync("sorcerer prog.topsy --tiptoe");

        exitCode.ShouldBe(0);
        File.Exists(Path.Combine(this.WorkingDirectory, "prog.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(this.WorkingDirectory, "prog.runtimeconfig.json")).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that <c>--target dotnet --output</c> writes the built assembly to the given path.
    /// </summary>
    [Fact]
    public async Task Sorcerer_TargetDotNetWithOutput_WritesToRequestedPath()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string _) = await this.RunAsync("sorcerer prog.topsy --target dotnet --output built.dll --tiptoe");

        exitCode.ShouldBe(0);
        File.Exists(Path.Combine(this.WorkingDirectory, "built.dll")).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that when both <c>--emit</c> and <c>--target</c> are given, <c>--emit</c> wins and no assembly is built.
    /// </summary>
    [Fact]
    public async Task Sorcerer_BothEmitAndTarget_EmitWinsAndNoAssemblyIsBuilt()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("sorcerer prog.topsy --emit preprocess --target dotnet --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("HARK!");
        File.Exists(Path.Combine(this.WorkingDirectory, "prog.dll")).ShouldBeFalse();
    }

    /// <summary>
    /// Tests that an unrecognised file extension is rejected for <c>--emit ast</c>.
    /// </summary>
    [Fact]
    public async Task Sorcerer_UnrecognisedExtension_ReturnsExitCode1()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.txt"), ValidSource);

        (int exitCode, string _, string stderr) = await this.RunAsync("sorcerer prog.txt --emit ast --tiptoe");

        exitCode.ShouldBe(1);
        stderr.ShouldContain("Invalid file extension");
    }

    /// <summary>
    /// Tests that a <c>.utopir</c> file is rejected for <c>--emit preprocess</c>, which only accepts <c>.topsy</c>.
    /// </summary>
    [Fact]
    public async Task Sorcerer_UtopIrFileForPreprocess_ReturnsExitCode1()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.utopir"), "£x = welcome peer");

        (int exitCode, string _, string stderr) = await this.RunAsync("sorcerer prog.utopir --emit preprocess --tiptoe");

        exitCode.ShouldBe(1);
        stderr.ShouldContain("Invalid file extension");
    }

    /// <summary>
    /// Tests that a <c>.utopir</c> file provided to <c>--emit utopir-ast</c> reports "not yet supported" rather than attempting to parse it.
    /// </summary>
    [Fact]
    public async Task Sorcerer_UtopIrFileForUtopIrAst_ReturnsNotYetSupportedError()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.utopir"), "£x = welcome peer");

        (int exitCode, string _, string stderr) = await this.RunAsync("sorcerer prog.utopir --emit utopir-ast --tiptoe");

        exitCode.ShouldBe(1);
        stderr.ShouldContain("not yet supported");
    }

    /// <summary>
    /// Tests that a <c>.utopir</c> file provided to <c>--emit dotnet-cil</c> reports "not yet supported".
    /// </summary>
    [Fact]
    public async Task Sorcerer_UtopIrFileForDotNetCil_ReturnsNotYetSupportedError()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.utopir"), "£x = welcome peer");

        (int exitCode, string _, string stderr) = await this.RunAsync("sorcerer prog.utopir --emit dotnet-cil --tiptoe");

        exitCode.ShouldBe(1);
        stderr.ShouldContain("not yet supported");
    }

    /// <summary>
    /// Tests that a <c>.utopir</c> file provided to <c>--target dotnet</c> reports "not yet supported".
    /// </summary>
    [Fact]
    public async Task Sorcerer_UtopIrFileForTargetDotNet_ReturnsNotYetSupportedError()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.utopir"), "£x = welcome peer");

        (int exitCode, string _, string stderr) = await this.RunAsync("sorcerer prog.utopir --target dotnet --tiptoe");

        exitCode.ShouldBe(1);
        stderr.ShouldContain("not yet supported");
    }

    /// <summary>
    /// Tests that an unknown <c>--emit</c> format value is rejected.
    /// </summary>
    [Fact]
    public async Task Sorcerer_UnknownEmitFormat_ReturnsExitCode1()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string stderr) = await this.RunAsync("sorcerer prog.topsy --emit nonsense --tiptoe");

        exitCode.ShouldBe(1);
        stderr.ShouldContain("Unknown --emit format");
    }

    /// <summary>
    /// Tests that the <c>compile</c> alias invokes the same behaviour as <c>sorcerer</c>.
    /// </summary>
    [Fact]
    public async Task Compile_Alias_BehavesLikeSorcerer()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("compile prog.topsy --emit preprocess --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("HARK!");
    }
}
