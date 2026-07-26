using System;
using System.Diagnostics;
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

    private const string ValidUtopIrSource =
        """
        £result = welcome peer
        £result = appoint 13
        find £result
        """;

    private const string ExternalLibraryCallSource =
        """
        HARK! "Test"
        PRINCIPALS
          PRAY WELCOME greeting AS A YARN
        THE CURTAIN RISES.
        greeting IS APPOINTED SUMMON Greet WITH "Ko-Ko" IF YOU PLEASE.
        BEHOLD greeting
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
    /// Tests that a <c>.utopir</c> file provided to <c>--emit utopir-ast</c> is parsed and its AST emitted as JSON.
    /// </summary>
    [Fact]
    public async Task Sorcerer_UtopIrFileForUtopIrAst_WritesJsonToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.utopir"), ValidUtopIrSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("sorcerer prog.utopir --emit utopir-ast --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("WelcomeInstruction");
    }

    /// <summary>
    /// Tests that a <c>.utopir</c> file provided to <c>--emit dotnet-cil</c> is parsed and its CIL disassembly emitted.
    /// </summary>
    [Fact]
    public async Task Sorcerer_UtopIrFileForDotNetCil_WritesIlTextToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.utopir"), ValidUtopIrSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("sorcerer prog.utopir --emit dotnet-cil --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("ldc.i4");
        stdout.ShouldContain("ret");
    }

    /// <summary>
    /// Tests that a <c>.utopir</c> file provided to <c>--target dotnet</c> is parsed and compiled to a runnable assembly.
    /// </summary>
    [Fact]
    public async Task Sorcerer_UtopIrFileForTargetDotNet_BuildsDotNetExecutable()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.utopir"), ValidUtopIrSource);

        (int exitCode, string _, string _) = await this.RunAsync("sorcerer prog.utopir --target dotnet --tiptoe");

        exitCode.ShouldBe(0);
        File.Exists(Path.Combine(this.WorkingDirectory, "prog.dll")).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a malformed <c>.utopir</c> file reports a syntax error diagnostic rather than crashing.
    /// </summary>
    [Fact]
    public async Task Sorcerer_MalformedUtopIrFile_ReportsSyntaxError()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.utopir"), "£x = bogus 5");

        (int exitCode, string _, string stderr) = await this.RunAsync("sorcerer prog.utopir --emit utopir-ast --tiptoe");

        exitCode.ShouldBe(1);
        stderr.ShouldNotBeEmpty();
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

    /// <summary>
    /// Tests that <c>--config varFormat:numeric</c> (the default) produces incrementing temporary variable names.
    /// </summary>
    [Fact]
    public async Task Sorcerer_VarFormatNumeric_ProducesIncrementingTempNames()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("sorcerer prog.topsy --emit utopir --config varFormat:numeric --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("£_0");
    }

    /// <summary>
    /// Tests that <c>--config varFormat:verbose</c> produces descriptive temporary variable names.
    /// </summary>
    [Fact]
    public async Task Sorcerer_VarFormatVerbose_ProducesDescriptiveTempNames()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string stdout, string _) = await this.RunAsync("sorcerer prog.topsy --emit utopir --config varFormat:verbose --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("£_sum_10_3");
    }

    /// <summary>
    /// Tests that an invalid <c>varFormat</c> value is rejected as a real error.
    /// </summary>
    [Fact]
    public async Task Sorcerer_VarFormatInvalidValue_ReturnsExitCode1()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string stderr) = await this.RunAsync("sorcerer prog.topsy --emit utopir --config varFormat:bogus --tiptoe");

        exitCode.ShouldBe(1);
        stderr.ShouldContain("varFormat");
    }

    /// <summary>
    /// Tests that <c>varFormat</c> is reported as ignored, but does not fail the operation, for <c>.utopir</c> input.
    /// </summary>
    [Fact]
    public async Task Sorcerer_VarFormatVerboseWithUtopIrInput_WarnsButStillSucceeds()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.utopir"), ValidUtopIrSource);

        (int exitCode, string _, string stderr) = await this.RunAsync("sorcerer prog.utopir --emit utopir-ast --config varFormat:verbose --tiptoe");

        exitCode.ShouldBe(0);
        stderr.ShouldContain("ignored for .utopir input");
    }

    /// <summary>
    /// Tests that a compiled programme calling Standard Library functions can run in a fresh directory
    /// containing only the emitted assembly, its <c>runtimeconfig.json</c>, and its two dependencies.
    /// </summary>
    [Fact]
    public async Task Sorcerer_TargetDotNetWithExternalCalls_RunsInIsolatedDirectoryWithOnlyItsDependencies()
    {
        const string source = """
            HARK! "Isolated Execution"

            PRINCIPALS
              PRAY WELCOME Greeting AS A YARN
            THE CURTAIN RISES.

            BEHOLD "before"
            PRAY TELL Greeting
            BEHOLD Greeting

            FINALE.
            """;

        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), source);

        (int compileExitCode, string _, string _) = await this.RunAsync("sorcerer prog.topsy --target dotnet --tiptoe");

        compileExitCode.ShouldBe(0);

        string[] requiredFiles =
        [
            "prog.dll",
            "prog.runtimeconfig.json",
            "BWHazel.TopsyTurvy.StandardLibrary.dll",
            "BWHazel.TopsyTurvy.Sdk.Interop.dll"
        ];

        foreach (string requiredFile in requiredFiles)
        {
            File.Exists(Path.Combine(this.WorkingDirectory, requiredFile))
                .ShouldBeTrue($"'{requiredFile}' should have been copied alongside the compiled assembly.");
        }

        string isolatedDirectory = Path.Combine(this.WorkingDirectory, "isolated");
        Directory.CreateDirectory(isolatedDirectory);
        foreach (string requiredFile in requiredFiles)
        {
            File.Copy(Path.Combine(this.WorkingDirectory, requiredFile), Path.Combine(isolatedDirectory, requiredFile));
        }

        ProcessStartInfo startInfo = new("dotnet", "prog.dll")
        {
            WorkingDirectory = isolatedDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = new() { StartInfo = startInfo };
        process.Start();
        await process.StandardInput.WriteLineAsync("hello back");
        process.StandardInput.Close();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(TimeSpan.FromSeconds(10));
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        stderr.ShouldBeEmpty();
        process.ExitCode.ShouldBe(0);
        stdout.ShouldContain("before");
        stdout.ShouldContain("hello back");
    }

    /// <summary>
    /// Tests that <c>--emit dotnet-cil</c> without <c>--admit</c> fails type-checking when the source calls a
    /// function only available in an external library.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitDotNetCilCallingExternalFunctionWithoutAdmitting_ReturnsExitCode1()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ExternalLibraryCallSource);

        (int exitCode, string _, string _) = await this.RunAsync("sorcerer prog.topsy --emit dotnet-cil --tiptoe");

        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that <c>--emit dotnet-cil --admit</c> type-checks and emits CIL for a call into an admitted external library.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitDotNetCilWithAdmittedExternalLibrary_WritesIlTextToStdout()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ExternalLibraryCallSource);

        (int exitCode, string stdout, string _) = await this.RunAsync($"sorcerer prog.topsy --emit dotnet-cil --admit \"{FixtureLibraryPath}\" --tiptoe");

        exitCode.ShouldBe(0);
        stdout.ShouldContain("call");
    }

    /// <summary>
    /// Tests that <c>--target dotnet --admit</c> reports a clean error when the admitted external library path does not exist.
    /// </summary>
    [Fact]
    public async Task Sorcerer_TargetDotNetWithMissingExternalLibrary_ReturnsExitCode1()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ValidSource);

        (int exitCode, string _, string stderr) = await this.RunAsync("sorcerer prog.topsy --target dotnet --admit nonexistent.dll --tiptoe");

        exitCode.ShouldBe(1);
        stderr.ShouldContain("not found");
    }

    /// <summary>
    /// Tests that a compiled programme calling an admitted external library function can run in a fresh directory
    /// containing only the emitted assembly, its <c>runtimeconfig.json</c>, its Standard Library dependencies,
    /// and the admitted external library.
    /// </summary>
    [Fact]
    public async Task Sorcerer_TargetDotNetWithAdmittedExternalLibrary_RunsInIsolatedDirectoryWithOnlyItsDependencies()
    {
        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), ExternalLibraryCallSource);

        (int compileExitCode, string _, string _) = await this.RunAsync($"sorcerer prog.topsy --target dotnet --admit \"{FixtureLibraryPath}\" --tiptoe");

        compileExitCode.ShouldBe(0);

        string fixtureLibraryFileName = Path.GetFileName(FixtureLibraryPath);
        string[] requiredFiles =
        [
            "prog.dll",
            "prog.runtimeconfig.json",
            "BWHazel.TopsyTurvy.StandardLibrary.dll",
            "BWHazel.TopsyTurvy.Sdk.Interop.dll",
            fixtureLibraryFileName
        ];

        foreach (string requiredFile in requiredFiles)
        {
            File.Exists(Path.Combine(this.WorkingDirectory, requiredFile))
                .ShouldBeTrue($"'{requiredFile}' should have been copied alongside the compiled assembly.");
        }

        string isolatedDirectory = Path.Combine(this.WorkingDirectory, "isolated");
        Directory.CreateDirectory(isolatedDirectory);
        foreach (string requiredFile in requiredFiles)
        {
            File.Copy(Path.Combine(this.WorkingDirectory, requiredFile), Path.Combine(isolatedDirectory, requiredFile));
        }

        ProcessStartInfo startInfo = new("dotnet", "prog.dll")
        {
            WorkingDirectory = isolatedDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = new()
        {
            StartInfo = startInfo
        };
        
        process.Start();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(TimeSpan.FromSeconds(10));
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        stderr.ShouldBeEmpty();
        process.ExitCode.ShouldBe(0);
        stdout.ShouldContain("Hello, Ko-Ko!");
    }

    /// <summary>
    /// Tests that a Topsy Turvy construct the UtopIR transformer does not support reports a clean error message
    /// rather than an unhandled exception stack trace.
    /// </summary>
    [Fact]
    public async Task Sorcerer_EmitUtopIrWithTransformerUnsupportedConstruct_ReturnsCleanErrorMessage()
    {
        const string source =
            """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            BEHOLD SUMMON PreviewBehold WITH "Ko-Ko" AND VERITY IF YOU PLEASE.
            FINALE.
            """;

        File.WriteAllText(Path.Combine(this.WorkingDirectory, "prog.topsy"), source);

        (int exitCode, string _, string stderr) = await this.RunAsync("sorcerer prog.topsy --emit utopir --tiptoe");

        exitCode.ShouldBe(1);
        stderr.ShouldContain("BEHOLD is only supported");
        stderr.ShouldNotContain("at BWHazel.TopsyTurvy");
    }
}
