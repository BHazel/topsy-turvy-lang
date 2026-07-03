using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Cli;

namespace BWHazel.TopsyTurvy.Tests.Cli;

/// <summary>
/// Tests for the <see cref="ProgramExecutionResult"/> class.
/// </summary>
public class ProgramExecutionResultTests
{
    /// <summary>
    /// Tests that the <see cref="ProgramExecutionResult.Success"/> method returns a result with <see cref="ProgramExecutionResult.IsSuccess"/> set to <c>true</c>.
    /// </summary>
    [Fact]
    public void Success_Always_ReturnsSuccessResult()
    {
        ProgramExecutionResult result = ProgramExecutionResult.Success();

        result.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="ProgramExecutionResult.Failure"/> method sets <see cref="ProgramExecutionResult.IsSuccess"/> to <c>false</c>.
    /// </summary>
    [Fact]
    public void Failure_WithMessage_SetsIsSuccessFalse()
    {
        ProgramExecutionResult result = ProgramExecutionResult.Failure("Something went wrong.");

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Something went wrong.");
    }

    /// <summary>
    /// Tests that the <see cref="ProgramExecutionResult.SyntaxError"/> method sets <see cref="ProgramExecutionResult.IsSuccess"/> to <c>false</c>.
    /// </summary>
    [Fact]
    public void SyntaxError_WithErrors_SetsIsSuccessFalse()
    {
        List<string> errors = ["[1:1] Unexpected token"];

        ProgramExecutionResult result = ProgramExecutionResult.SyntaxError(errors);

        result.IsSuccess.ShouldBeFalse();
        result.SyntaxErrors!.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="ProgramExecutionResult.RuntimeError"/> method sets <see cref="ProgramExecutionResult.IsSuccess"/> to <c>false</c>.
    /// </summary>
    [Fact]
    public void RuntimeError_WithDiagnostics_SetsIsSuccessFalse()
    {
        Diagnostic diagnostic = new(
            "Runtime failure",
            DiagnosticSeverity.Error,
            new(new(Line: 1, Column: 1), new(Line: 1, Column: 5)));

        ProgramExecutionResult result = ProgramExecutionResult.RuntimeError([diagnostic]);

        result.IsSuccess.ShouldBeFalse();
        result.RuntimeDiagnostics!.ShouldNotBeEmpty();
    }
}
