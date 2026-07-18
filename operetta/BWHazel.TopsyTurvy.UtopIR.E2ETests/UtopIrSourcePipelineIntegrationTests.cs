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
    /// Runs a hand-written UtopIR programme using <c>sum.f</c> on <c>fathom</c> values through the full pipeline, verifying the truncated exit code.
    /// </summary>
    [Fact]
    public void Pipeline_HandWrittenUtopIrSourceWithFloatArithmetic_ProducesCorrectExitCode()
    {
        const string source =
            """
            £lhs = welcome fathom
            £lhs = appoint 1.5
            £rhs = welcome fathom
            £rhs = appoint 2.75
            £_sumf_lhs_rhs = sum.f £lhs, £rhs
            £result = welcome fathom
            £result = appoint £_sumf_lhs_rhs
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

            exitCode.ShouldBe(4);
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
    /// Runs a hand-written UtopIR programme containing a <c>sailalike</c> instruction through the
    /// full pipeline, verifying it branches to the labelled <c>find 1</c> when the <c>decree</c>
    /// value is <c>verity</c>.
    /// </summary>
    [Fact]
    public void Pipeline_SailAlikeWithVerity_BranchesAndReturnsOne()
    {
        const string source =
            """
            £Boolean = welcome decree
            £Boolean = appoint verity

            sailalike £Boolean, !IS_ALIKE
            find 0

            !IS_ALIKE
              find 1
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

            exitCode.ShouldBe(1);
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
    /// Runs the same programme as <see cref="Pipeline_SailAlikeWithVerity_BranchesAndReturnsOne"/>
    /// with the <c>decree</c> value set to <c>nay</c>, verifying the <c>sailalike</c> instruction
    /// falls through to <c>find 0</c> without branching.
    /// </summary>
    [Fact]
    public void Pipeline_SailAlikeWithNay_FallsThroughAndReturnsZero()
    {
        const string source =
            """
            £Boolean = welcome decree
            £Boolean = appoint nay

            sailalike £Boolean, !IS_ALIKE
            find 0

            !IS_ALIKE
              find 1
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

            exitCode.ShouldBe(0);
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
