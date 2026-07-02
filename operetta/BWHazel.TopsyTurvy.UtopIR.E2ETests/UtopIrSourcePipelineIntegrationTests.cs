using System;
using System.IO;
using System.Reflection;
using BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;
using BWHazel.TopsyTurvy.UtopIR.Parser;

namespace BWHazel.TopsyTurvy.UtopIR.E2ETests;

/// <summary>
/// End-to-end tests driving UtopIR source text through <see cref="UtopIRParser"/> and <see cref="CilEmitter"/> to a running .NET assembly.
/// </summary>
public sealed class UtopIrSourcePipelineIntegrationTests
{
    /// <summary>
    /// Runs a UtopIR programme through the full pipeline (parse, emit, load, invoke), verifying the compiled assembly produces the correct exit code.
    /// </summary>
    [Fact]
    public void Pipeline_HandWrittenUtopIrSource_ProducesCorrectExitCode()
    {
        const string source =
            """
            £lhs = welcome peer
            £lhs = appoint 10
            £rhs = welcome peer
            £rhs = appoint 3
            £_sum_lhs_rhs = sum £lhs, £rhs
            £result = welcome peer
            £result = appoint £_sum_lhs_rhs
            find £result
            """;

        UtopIRParseResult parseResult = new UtopIRParser().TryParse(source);

        parseResult.Diagnostics.ShouldBeEmpty();
        parseResult.Program.ShouldNotBeNull();

        string assemblyName = $"TopsyTurvyUtopIrSourceTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            new CilEmitter().Emit(
                parseResult.Program!,
                new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library));

            Assembly assembly = Assembly.LoadFrom(outputPath);
            Type operaType = assembly.GetType("Opera")!;
            MethodInfo mainMethod = operaType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;
            int exitCode = (int)mainMethod.Invoke(null, [Array.Empty<string>()])!;

            exitCode.ShouldBe(13);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    /// <summary>
    /// Tests that a UtopIR programme using <c>were</c> to widen a <c>peer</c> value into a <c>chancellor</c> target parses and runs correctly.
    /// </summary>
    [Fact]
    public void Pipeline_UtopIrSourceWithWereCast_WidensAndProducesCorrectExitCode()
    {
        const string source =
            """
            £small = welcome peer
            £small = appoint 200
            £big = welcome chancellor
            £big = were £small, chancellor
            find £big
            """;

        UtopIRParseResult parseResult = new UtopIRParser().TryParse(source);
        
        parseResult.Diagnostics.ShouldBeEmpty();
        parseResult.Program.ShouldNotBeNull();

        string assemblyName = $"TopsyTurvyUtopIrSourceTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            CilEmitResult emitResult = new CilEmitter().Emit(
                parseResult.Program!,
                new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library));

            emitResult.IlSource.ShouldContain("conv.i8");

            Assembly assembly = Assembly.LoadFrom(outputPath);
            Type operaType = assembly.GetType("Opera")!;
            MethodInfo mainMethod = operaType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;
            int exitCode = (int)mainMethod.Invoke(null, [Array.Empty<string>()])!;

            exitCode.ShouldBe(200);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    /// <summary>
    /// Tests that malformed UtopIR source produces a diagnostic rather than an exception, and does not produce a parsed programme.
    /// </summary>
    [Fact]
    public void Pipeline_MalformedUtopIrSource_ReportsDiagnosticWithoutThrowing()
    {
        const string source = "£x = bogus 5";

        UtopIRParseResult parseResult = new UtopIRParser().TryParse(source);

        parseResult.Success.ShouldBeFalse();
        parseResult.Program.ShouldBeNull();
        parseResult.Diagnostics.ShouldNotBeEmpty();
    }
}
