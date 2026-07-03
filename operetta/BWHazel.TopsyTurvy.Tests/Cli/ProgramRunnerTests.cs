using System.Collections.Generic;
using BWHazel.TopsyTurvy.Cli;
using BWHazel.TopsyTurvy.Tests.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Cli;

/// <summary>
/// Tests for the <see cref="ProgramRunner"/> class.
/// </summary>
public class ProgramRunnerTests : CliTestBase
{
    private const string FilePath = "/programme.topsy";

    private const string InvalidSource = "THIS IS NOT VALID TOPSY TURVY";

    private const string ValidSource = """
        HARK! "Test"
        PRINCIPALS
        THE CURTAIN RISES.
        BEHOLD "OK"
        FINALE.
        """;

    /// <summary>
    /// Tests that the <see cref="ProgramRunner.Check"/> method returns a failure result when the file does not exist.
    /// </summary>
    [Fact]
    public void Check_WhenFileDoesNotExist_ReturnsFailureResult()
    {
        ProgramExecutionResult result = ProgramRunner.Check(FilePath, this.fileSystem);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="ProgramRunner.Check"/> method returns a syntax error result when the file has syntax errors.
    /// </summary>
    [Fact]
    public void Check_WhenFileHasSyntaxErrors_ReturnsSyntaxErrorResult()
    {
        this.fileSystem.File.WriteAllText(FilePath, InvalidSource);

        ProgramExecutionResult result = ProgramRunner.Check(FilePath, this.fileSystem);

        result.IsSuccess.ShouldBeFalse();
        result.SyntaxErrors.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="ProgramRunner.Check"/> method returns a success result when the file is valid.
    /// </summary>
    [Fact]
    public void Check_WhenFileIsValid_ReturnsSuccessResult()
    {
        this.fileSystem.File.WriteAllText(FilePath, ValidSource);

        ProgramExecutionResult result = ProgramRunner.Check(FilePath, this.fileSystem);

        result.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="ProgramRunner.ParseFile"/> method returns a failure result when the file does not exist.
    /// </summary>
    [Fact]
    public void ParseFile_WhenFileDoesNotExist_ReturnsFailureResult()
    {
        (ProgramExecutionResult result, _) = ProgramRunner.ParseFile(FilePath, this.fileSystem);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="ProgramRunner.ParseFile"/> method returns a syntax error result when the file has syntax errors.
    /// </summary>
    [Fact]
    public void ParseFile_WhenFileHasSyntaxErrors_ReturnsSyntaxErrorResult()
    {
        this.fileSystem.File.WriteAllText(FilePath, InvalidSource);

        (ProgramExecutionResult result, _) = ProgramRunner.ParseFile(FilePath, this.fileSystem);

        result.IsSuccess.ShouldBeFalse();
        result.SyntaxErrors.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="ProgramRunner.ParseFile"/> method returns a success result and non-null parse data when the file is valid.
    /// </summary>
    [Fact]
    public void ParseFile_WhenFileIsValid_ReturnsSuccessAndNonNullProgram()
    {
        this.fileSystem.File.WriteAllText(FilePath, ValidSource);

        (ProgramExecutionResult result, var parseData) = ProgramRunner.ParseFile(FilePath, this.fileSystem);

        result.IsSuccess.ShouldBeTrue();
        parseData.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="ProgramRunner.Run"/> method returns a failure result when the file does not exist.
    /// </summary>
    [Fact]
    public void Run_WhenFileDoesNotExist_ReturnsFailureResult()
    {
        List<string> output = [];
        Queue<string> input = new();
        TestIO io = new(output, input);

        ProgramExecutionResult result = ProgramRunner.Run(FilePath, io, this.fileSystem);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="ProgramRunner.Run"/> method returns a syntax error result when the file has syntax errors.
    /// </summary>
    [Fact]
    public void Run_WhenFileHasSyntaxErrors_ReturnsSyntaxErrorResult()
    {
        this.fileSystem.File.WriteAllText(FilePath, InvalidSource);
        List<string> output = [];
        Queue<string> input = new();
        TestIO io = new(output, input);

        ProgramExecutionResult result = ProgramRunner.Run(FilePath, io, this.fileSystem);

        result.IsSuccess.ShouldBeFalse();
        result.SyntaxErrors.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="ProgramRunner.Run"/> method returns a success result and produces output when the file is valid.
    /// </summary>
    [Fact]
    public void Run_WhenFileIsValid_ReturnsSuccessAndProducesOutput()
    {
        this.fileSystem.File.WriteAllText(FilePath, ValidSource);
        List<string> output = [];
        Queue<string> input = new();
        TestIO io = new(output, input);

        ProgramExecutionResult result = ProgramRunner.Run(FilePath, io, this.fileSystem);

        result.IsSuccess.ShouldBeTrue();
        output.ShouldContain("OK");
    }
}
