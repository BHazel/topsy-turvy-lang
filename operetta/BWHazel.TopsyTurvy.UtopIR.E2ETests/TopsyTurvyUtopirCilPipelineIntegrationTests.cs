using System;
using System.IO;
using System.Reflection;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.UtopIR.Analysis;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;
using BWHazel.TopsyTurvy.UtopIR.Transformer;
using BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

namespace BWHazel.TopsyTurvy.UtopIR.E2ETests;

/// <summary>
/// End-to-end tests driving the full UtopIR pipeline from Topsy Turvy source text through to a
/// running .NET assembly.
/// </summary>
public class TopsyTurvyUtopirCilPipelineIntegrationTests
{
    /// <summary>
    /// Runs a Topsy Turvy programme through the complete pipeline, verifying the UtopIR source text and
    /// the final OS exit code from the compiled assembly.
    /// </summary>
    [Fact]
    public void Pipeline_SimpleArithmeticProgramme_ProducesCorrectExitCode()
    {
        const string source = """
            HARK! "The Pirate King's Arithmetical Engine"

            PRINCIPALS
              PRAY WELCOME lhs    AS A PEER
              PRAY WELCOME rhs    AS A PEER
              PRAY WELCOME result AS A PEER
            THE CURTAIN RISES.

            lhs IS APPOINTED 10
            rhs IS APPOINTED 3
            result IS APPOINTED SUM OF lhs AND rhs
            AND SO I FIND result

            FINALE.
            """;

        TopsyTurvyParser parser = new();
        ParseResult parseResult = parser.TryParse(source);

        parseResult.Diagnostics.ShouldBeEmpty();
        parseResult.Program.ShouldNotBeNull();

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer(new InstructionDetailVariableFormatter()).Transform(parseResult.Program!);

        string utopIrSource = new UtopIRCodeGenerator().Generate(utopIrProgram);
        utopIrSource.ShouldContain("£lhs = welcome peer");
        utopIrSource.ShouldContain("£rhs = welcome peer");
        utopIrSource.ShouldContain("£result = welcome peer");
        utopIrSource.ShouldContain("£lhs = appoint 10");
        utopIrSource.ShouldContain("£rhs = appoint 3");
        utopIrSource.ShouldContain("£_sum_lhs_rhs = sum £lhs, £rhs");
        utopIrSource.ShouldContain("£result = appoint £_sum_lhs_rhs");
        utopIrSource.ShouldContain("find £result");

        string assemblyName = $"TopsyTurvyPipelineTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            CilEmitResult emitResult = new CilEmitter().Emit(
                utopIrProgram,
                new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library));

            emitResult.IlSource.ShouldContain("ldc.i4.s 10");
            emitResult.IlSource.ShouldContain("add");

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
    /// Runs a Topsy Turvy programme with floating-point arithmetic through the complete pipeline,
    /// verifying the <c>.f</c>-suffixed UtopIR instruction, the floating-point IL opcodes and the
    /// truncated final OS exit code.
    /// </summary>
    [Fact]
    public void Pipeline_FloatingPointArithmeticProgramme_ProducesCorrectExitCode()
    {
        const string source = """
            HARK! "The Fathomless Deep"

            PRINCIPALS
              PRAY WELCOME depth  AS A FATHOM
              PRAY WELCOME scale  AS A FATHOM
              PRAY WELCOME result AS A FATHOM
            THE CURTAIN RISES.

            depth IS APPOINTED 4.5
            scale IS APPOINTED 10.0
            result IS APPOINTED PRODUCT OF depth AND scale
            AND SO I FIND result

            FINALE.
            """;

        TopsyTurvyParser parser = new();
        ParseResult parseResult = parser.TryParse(source);

        parseResult.Diagnostics.ShouldBeEmpty();
        parseResult.Program.ShouldNotBeNull();

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer(new InstructionDetailVariableFormatter()).Transform(parseResult.Program!);

        string utopIrSource = new UtopIRCodeGenerator().Generate(utopIrProgram);
        utopIrSource.ShouldContain("£depth = welcome fathom");
        utopIrSource.ShouldContain("£depth = appoint 4.5");
        utopIrSource.ShouldContain("£scale = appoint 10.0");
        utopIrSource.ShouldContain("£_prodf_depth_scale = prod.f £depth, £scale");
        utopIrSource.ShouldContain("£result = appoint £_prodf_depth_scale");
        utopIrSource.ShouldContain("find £result");

        string assemblyName = $"TopsyTurvyPipelineTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            CilEmitResult emitResult = new CilEmitter().Emit(
                utopIrProgram,
                new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library));

            emitResult.IlSource.ShouldContain("ldc.r8 4.5");
            emitResult.IlSource.ShouldContain("mul");

            Assembly assembly = Assembly.LoadFrom(outputPath);
            Type operaType = assembly.GetType("Opera")!;
            MethodInfo mainMethod = operaType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;
            int exitCode = (int)mainMethod.Invoke(null, [Array.Empty<string>()])!;

            exitCode.ShouldBe(45);
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
    /// Runs a Topsy Turvy programme with bitwise operators through the complete pipeline, verifying
    /// the lowered UtopIR instructions and the final OS exit code.
    /// </summary>
    [Fact]
    public void Pipeline_BitwiseProgramme_ProducesCorrectExitCode()
    {
        // (9 & 3) = 1, then 1 << 1 = 2.
        const string source = """
            HARK! "A Most Ingenious Paradox of Bits"

            PRINCIPALS
              PRAY WELCOME mask   AS A PEER
              PRAY WELCOME value  AS A PEER
              PRAY WELCOME result AS A PEER
            THE CURTAIN RISES.

            mask IS APPOINTED 9
            value IS APPOINTED 3
            result IS APPOINTED TRANSPOSITION UP CHORD OF mask AND value
            AND SO I FIND result

            FINALE.
            """;

        TopsyTurvyParser parser = new();
        ParseResult parseResult = parser.TryParse(source);

        parseResult.Diagnostics.ShouldBeEmpty();
        parseResult.Program.ShouldNotBeNull();

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer(new InstructionDetailVariableFormatter()).Transform(parseResult.Program!);

        string utopIrSource = new UtopIRCodeGenerator().Generate(utopIrProgram);
        utopIrSource.ShouldContain("£_chord_mask_value = chord £mask, £value");
        utopIrSource.ShouldContain("£_transup__chord_mask_value_1 = transup £_chord_mask_value, 1");
        utopIrSource.ShouldContain("£result = appoint £_transup__chord_mask_value_1");

        string assemblyName = $"TopsyTurvyPipelineTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            CilEmitResult emitResult = new CilEmitter().Emit(
                utopIrProgram,
                new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library));

            emitResult.IlSource.ShouldContain("and");
            emitResult.IlSource.ShouldContain("shl");

            Assembly assembly = Assembly.LoadFrom(outputPath);
            Type operaType = assembly.GetType("Opera")!;
            MethodInfo mainMethod = operaType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;
            int exitCode = (int)mainMethod.Invoke(null, [Array.Empty<string>()])!;

            exitCode.ShouldBe(2);
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
    /// Runs a Topsy Turvy programme casting a <c>STITCH</c> to a <c>PEER</c> through the complete
    /// pipeline, verifying the <c>were</c> instruction and the code-point exit code.
    /// </summary>
    [Fact]
    public void Pipeline_StitchCastProgramme_ReturnsCodePointExitCode()
    {
        const string source = """
            HARK! "A Stitch In Time"

            PRINCIPALS
              PRAY WELCOME letter AS A STITCH
              PRAY WELCOME result AS A PEER
            THE CURTAIN RISES.

            letter IS APPOINTED 'A'
            result IS APPOINTED AS IT WERE letter AS A PEER
            AND SO I FIND result

            FINALE.
            """;

        TopsyTurvyParser parser = new();
        ParseResult parseResult = parser.TryParse(source);

        parseResult.Diagnostics.ShouldBeEmpty();
        parseResult.Program.ShouldNotBeNull();

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer(new InstructionDetailVariableFormatter()).Transform(parseResult.Program!);

        string utopIrSource = new UtopIRCodeGenerator().Generate(utopIrProgram);
        utopIrSource.ShouldContain("£letter = welcome stitch");
        utopIrSource.ShouldContain("£letter = appoint 'A'");
        utopIrSource.ShouldContain("£_were_letter_peer = were £letter, peer");
        utopIrSource.ShouldContain("£result = appoint £_were_letter_peer");

        string assemblyName = $"TopsyTurvyPipelineTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            new CilEmitter().Emit(
                utopIrProgram,
                new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library));

            Assembly assembly = Assembly.LoadFrom(outputPath);
            Type operaType = assembly.GetType("Opera")!;
            MethodInfo mainMethod = operaType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;
            int exitCode = (int)mainMethod.Invoke(null, [Array.Empty<string>()])!;

            exitCode.ShouldBe(65);
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
    /// Runs a Topsy Turvy programme with a nested arithmetic expression through the complete pipeline,
    /// verifying the transformer flattens the logic into temporary registers and the final OS exit code.
    /// </summary>
    [Fact]
    public void Pipeline_NestedArithmeticProgramme_FlattensAndProducesCorrectExitCode()
    {
        const string source = """
            HARK! "Nested Arithmetic"

            PRINCIPALS
              PRAY WELCOME first_number  AS A PEER
              PRAY WELCOME second_number AS A PEER
              PRAY WELCOME third_number  AS A PEER
            THE CURTAIN RISES.

            first_number  IS APPOINTED 3
            second_number IS APPOINTED 4
            third_number  IS APPOINTED 5
            AND SO I FIND SUM OF PRODUCT OF first_number AND second_number AND third_number

            FINALE.
            """;

        TopsyTurvyParser parser = new();
        ParseResult parseResult = parser.TryParse(source);

        parseResult.Diagnostics.ShouldBeEmpty();
        parseResult.Program.ShouldNotBeNull();

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer().Transform(parseResult.Program!);

        string utopIrSource = new UtopIRCodeGenerator().Generate(utopIrProgram);
        utopIrSource.ShouldContain("prod £first_number, £second_number");
        utopIrSource.ShouldContain("sum ");

        string assemblyName = $"TopsyTurvyPipelineTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            new CilEmitter().Emit(utopIrProgram, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library));

            Assembly assembly = Assembly.LoadFrom(outputPath);
            Type operaType = assembly.GetType("Opera")!;
            MethodInfo mainMethod = operaType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;
            int exitCode = (int)mainMethod.Invoke(null, [Array.Empty<string>()])!;

            // (3 * 4) + 5 = 17
            exitCode.ShouldBe(17);
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
    /// Runs a Topsy Turvy programme with mixed-type arithmetic (<c>peer</c> + <c>chancellor</c>) through the complete pipeline, verifying the transformer widens the narrower operand and the compiled assembly produces the correct exit code.
    /// </summary>
    [Fact]
    public void Pipeline_MixedTypeArithmeticProgramme_WidensAndProducesCorrectExitCode()
    {
        const string source = """
            HARK! "The Chancellor's Arithmetical Engine"

            PRINCIPALS
              PRAY WELCOME small_number AS A PEER
              PRAY WELCOME large_number AS A CHANCELLOR
            THE CURTAIN RISES.

            small_number IS APPOINTED 10
            large_number IS APPOINTED AS IT WERE 200 AS A CHANCELLOR
            AND SO I FIND SUM OF small_number AND large_number

            FINALE.
            """;

        TopsyTurvyParser parser = new();
        ParseResult parseResult = parser.TryParse(source);

        parseResult.Diagnostics.ShouldBeEmpty();
        parseResult.Program.ShouldNotBeNull();

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer().Transform(parseResult.Program!);

        string utopIrSource = new UtopIRCodeGenerator().Generate(utopIrProgram);
        utopIrSource.ShouldContain("were £small_number, chancellor");
        utopIrSource.ShouldContain("sum ");

        string assemblyName = $"TopsyTurvyMixedTypeTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            CilEmitResult emitResult = new CilEmitter().Emit(
                utopIrProgram,
                new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library));

            emitResult.IlSource.ShouldContain("conv.i8");

            Assembly assembly = Assembly.LoadFrom(outputPath);
            Type operaType = assembly.GetType("Opera")!;
            MethodInfo mainMethod = operaType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;
            int exitCode = (int)mainMethod.Invoke(null, [Array.Empty<string>()])!;

            exitCode.ShouldBe(210);
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
    /// Runs a Topsy Turvy programme that appoints a bare in-range integer literal to a <c>chancellor</c> variable, verifying the transformer inserts a widening cast rather than storing a mismatched-width value.
    /// </summary>
    [Fact]
    public void Pipeline_LiteralAppointedToWiderDeclaredType_WidensAndProducesCorrectExitCode()
    {
        const string source = """
            HARK! "The Widening Appointment"

            PRINCIPALS
              PRAY WELCOME big_number AS A CHANCELLOR
            THE CURTAIN RISES.

            big_number IS APPOINTED 200
            AND SO I FIND big_number

            FINALE.
            """;

        TopsyTurvyParser parser = new();
        ParseResult parseResult = parser.TryParse(source);

        parseResult.Diagnostics.ShouldBeEmpty();
        parseResult.Program.ShouldNotBeNull();

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer().Transform(parseResult.Program!);

        string utopIrSource = new UtopIRCodeGenerator().Generate(utopIrProgram);
        utopIrSource.ShouldContain("were 200, chancellor");

        string assemblyName = $"TopsyTurvyAppointWidenTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            CilEmitResult emitResult = new CilEmitter().Emit(
                utopIrProgram,
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
}
