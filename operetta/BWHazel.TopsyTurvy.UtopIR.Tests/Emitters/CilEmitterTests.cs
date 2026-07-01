using System;
using System.IO;
using System.Reflection;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

namespace BWHazel.TopsyTurvy.UtopIR.Tests.Emitters;

/// <summary>
/// Tests for <see cref="CilEmitter"/>.
/// </summary>
/// <remarks>
/// Each test that verifies runtime behaviour emits a <c>.dll</c> to a unique temporary path,
/// loads it with <c>Assembly.LoadFrom</c>, invokes <c>Opera.Main</c>, and asserts the
/// returned exit code.  The temporary file is deleted after the test regardless of outcome.
/// </remarks>
public class CilEmitterTests
{
    /// <summary>
    /// Tests that a void <see cref="FindInstruction"/> (null value) causes <c>Main</c> to return exit code 0.
    /// </summary>
    [Fact]
    public void Emit_FindNull_ReturnsZero()
    {
        UtopIRProgram program = new([new FindInstruction(Value: null)]);

        RunProgram(program).ShouldBe(0);
    }

    /// <summary>
    /// Tests that a <see cref="FindInstruction"/> with a <see cref="LiteralOperand"/> causes <c>Main</c> to return the literal value as the exit code.
    /// </summary>
    [Fact]
    public void Emit_FindWithLiteral_ReturnsLiteralAsExitCode()
    {
        UtopIRProgram program = new([new FindInstruction(new LiteralOperand(7))]);

        RunProgram(program).ShouldBe(7);
    }

    /// <summary>
    /// Tests that a <see cref="FindInstruction"/> with a <see cref="VariableOperand"/> causes <c>Main</c> to return the variable stored value as the exit code.
    /// </summary>
    [Fact]
    public void Emit_FindWithVariable_ReturnsVariableValueAsExitCode()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("x"), UtopIRType.Peer),
            new AppointInstruction(new("x"), new LiteralOperand(42)),
            new FindInstruction(new VariableOperand(new("x")))
        ]);

        RunProgram(program).ShouldBe(42);
    }

    /// <summary>
    /// Tests that <c>Main</c> returns 0 implicitly when the programme contains no <see cref="FindInstruction"/>.
    /// </summary>
    [Fact]
    public void Emit_NoFindInstruction_ImplicitlyReturnsZero()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("x"), UtopIRType.Peer),
            new AppointInstruction(new("x"), new LiteralOperand(99))
        ]);

        RunProgram(program).ShouldBe(0);
    }

    /// <summary>
    /// Tests that <see cref="AppointInstruction"/> with a <see cref="LiteralOperand"/> stores the literal value in the target register.
    /// </summary>
    [Fact]
    public void Emit_AppointLiteral_StoresValueInRegister()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("n"), UtopIRType.Peer),
            new AppointInstruction(new("n"), new LiteralOperand(55)),
            new FindInstruction(new VariableOperand(new("n")))
        ]);

        RunProgram(program).ShouldBe(55);
    }

    /// <summary>
    /// Tests that <see cref="AppointInstruction"/> with a <see cref="VariableOperand"/> copies the source register value into the target register.
    /// </summary>
    [Fact]
    public void Emit_AppointVariable_CopiesValueToTarget()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("src"), UtopIRType.Peer),
            new WelcomeInstruction(new("dst"), UtopIRType.Peer),
            new AppointInstruction(new("src"), new LiteralOperand(33)),
            new AppointInstruction(new("dst"), new VariableOperand(new("src"))),
            new FindInstruction(new VariableOperand(new("dst")))
        ]);

        RunProgram(program).ShouldBe(33);
    }

    /// <summary>
    /// Tests that the <c>chancellor</c> type (64-bit signed integer) is correctly declared and stored but the value is narrowed to <c>int32</c> on return.
    /// </summary>
    [Fact]
    public void Emit_Chancellor_StoresAndReturnsCorrectly()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("x"), UtopIRType.Chancellor),
            new AppointInstruction(new("x"), new LiteralOperand(100L)),
            new FindInstruction(new VariableOperand(new("x")))
        ]);

        RunProgram(program).ShouldBe(100);
    }

    /// <summary>
    /// Tests that the <c>pirate</c> type (16-bit signed integer) is correctly declared and stored.
    /// </summary>
    [Fact]
    public void Emit_Pirate_StoresAndReturnsCorrectly()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("x"), UtopIRType.Pirate),
            new AppointInstruction(new("x"), new LiteralOperand((short)20)),
            new FindInstruction(new VariableOperand(new("x")))
        ]);

        RunProgram(program).ShouldBe(20);
    }

    /// <summary>
    /// Tests that the <c>sausageroll</c> type (8-bit signed integer) is correctly declared and stored.
    /// </summary>
    [Fact]
    public void Emit_SausageRoll_StoresAndReturnsCorrectly()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("x"), UtopIRType.SausageRoll),
            new AppointInstruction(new("x"), new LiteralOperand((sbyte)10)),
            new FindInstruction(new VariableOperand(new("x")))
        ]);

        RunProgram(program).ShouldBe(10);
    }

    /// <summary>
    /// Tests that the <c>standingpeer</c> type (32-bit unsigned integer) is correctly declared and stored.
    /// </summary>
    [Fact]
    public void Emit_StandingPeer_StoresAndReturnsCorrectly()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("x"), UtopIRType.StandingPeer),
            new AppointInstruction(new("x"), new LiteralOperand(200u)),
            new FindInstruction(new VariableOperand(new("x")))
        ]);

        RunProgram(program).ShouldBe(200);
    }

    /// <summary>
    /// Tests that the <c>standingchancellor</c> type (64-bit unsigned integer) is correctly declared and stored but is narrowed to <c>int32</c> on return.
    /// </summary>
    [Fact]
    public void Emit_StandingChancellor_StoresAndReturnsCorrectly()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("x"), UtopIRType.StandingChancellor),
            new AppointInstruction(new("x"), new LiteralOperand(150UL)),
            new FindInstruction(new VariableOperand(new("x")))
        ]);

        RunProgram(program).ShouldBe(150);
    }

    /// <summary>
    /// Tests that the <c>sum</c> operation adds two operands correctly.
    /// </summary>
    [Fact]
    public void Emit_Sum_AddsOperands()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(10)),
            new AppointInstruction(new("b"), new LiteralOperand(3)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Sum, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(13);
    }

    /// <summary>
    /// Tests that the <c>diff</c> operation subtracts the second operand from the first.
    /// </summary>
    [Fact]
    public void Emit_Diff_SubtractsOperands()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(10)),
            new AppointInstruction(new("b"), new LiteralOperand(3)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Diff, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(7);
    }

    /// <summary>
    /// Tests that the <c>prod</c> operation multiplies two operands correctly.
    /// </summary>
    [Fact]
    public void Emit_Prod_MultipliesOperands()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(6)),
            new AppointInstruction(new("b"), new LiteralOperand(7)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Prod, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(42);
    }

    /// <summary>
    /// Tests that the <c>quot</c> operation performs integer division correctly.
    /// </summary>
    [Fact]
    public void Emit_Quot_DividesOperands()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(20)),
            new AppointInstruction(new("b"), new LiteralOperand(4)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Quot, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(5);
    }

    /// <summary>
    /// Tests that the <c>rem</c> operation computes the remainder correctly.
    /// </summary>
    [Fact]
    public void Emit_Rem_ComputesRemainder()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(17)),
            new AppointInstruction(new("b"), new LiteralOperand(5)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Rem, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(2);
    }

    /// <summary>
    /// Tests that the <c>max</c> operation returns the larger of two operands.
    /// </summary>
    [Fact]
    public void Emit_Max_ReturnsLargerOperand()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(8)),
            new AppointInstruction(new("b"), new LiteralOperand(15)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Max, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(15);
    }

    /// <summary>
    /// Tests that the <c>min</c> operation returns the smaller of two operands.
    /// </summary>
    [Fact]
    public void Emit_Min_ReturnsSmallerOperand()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(8)),
            new AppointInstruction(new("b"), new LiteralOperand(15)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Min, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(8);
    }

    /// <summary>
    /// Tests that arithmetic works correctly when both operands are <see cref="LiteralOperand"/>s rather than <see cref="VariableOperand"/>s.
    /// </summary>
    [Fact]
    public void Emit_Sum_WithLiteralOperands_AddsCorrectly()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Sum, new("r"), new LiteralOperand(11), new LiteralOperand(22)),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(33);
    }

    /// <summary>
    /// Tests that <c>quot</c> on an unsigned type uses unsigned division semantics.
    /// </summary>
    [Fact]
    public void Emit_Quot_UnsignedPeer_UsesDivUn()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.StandingPeer),
            new WelcomeInstruction(new("b"), UtopIRType.StandingPeer),
            new WelcomeInstruction(new("r"), UtopIRType.StandingPeer),
            new AppointInstruction(new("a"), new LiteralOperand(21u)),
            new AppointInstruction(new("b"), new LiteralOperand(3u)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Quot, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(7);
    }

    /// <summary>
    /// Tests that a <see cref="PrenticeInstruction"/> pushes a value onto the stack and a subsequent <see cref="LeaveInstruction"/> stores it in the target register.
    /// </summary>
    [Fact]
    public void Emit_PrenticeAndLeave_MovesValueToTargetRegister()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("target"), UtopIRType.Peer),
            new PrenticeInstruction(new LiteralOperand(77)),
            new LeaveInstruction(new("target")),
            new FindInstruction(new VariableOperand(new("target")))
        ]);

        RunProgram(program).ShouldBe(77);
    }

    /// <summary>
    /// Tests a flattened nested expression: <c>_prod_a_b = prod a, b; r = sum _prod_a_b, c</c>.
    /// </summary>
    [Fact]
    public void Emit_NestedArithmetic_ProducesCorrectResult()
    {
        // (3 * 4) + 5 = 17
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("c"), UtopIRType.Peer),
            new WelcomeInstruction(new("_prod_a_b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(3)),
            new AppointInstruction(new("b"), new LiteralOperand(4)),
            new AppointInstruction(new("c"), new LiteralOperand(5)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Prod, new("_prod_a_b"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Sum, new("r"), new VariableOperand(new("_prod_a_b")), new VariableOperand(new("c"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(17);
    }

    /// <summary>
    /// Tests that an undeclared arithmetic target infers its CLR type from a <see cref="VariableOperand"/>.
    /// </summary>
    [Fact]
    public void Emit_ArithmeticWithUndeclaredTarget_InfersTypeFromVariableOperandAndSucceeds()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(6)),
            new AppointInstruction(new("b"), new LiteralOperand(7)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Sum, new("_sum_a_b"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("_sum_a_b")))
        ]);

        RunProgram(program).ShouldBe(13);
    }

    /// <summary>
    /// Tests that an undeclared arithmetic target infers its CLR type from a <see cref="LiteralOperand"/>.
    /// </summary>
    [Fact]
    public void Emit_ArithmeticWithUndeclaredTarget_InfersTypeFromLiteralOperandAndSucceeds()
    {
        UtopIRProgram program = new([
            new ArithmeticInstruction(UtopIRArithmeticOperation.Sum, new("_sum_11_22"), new LiteralOperand(11), new LiteralOperand(22)),
            new FindInstruction(new VariableOperand(new("_sum_11_22")))
        ]);

        RunProgram(program).ShouldBe(33);
    }

    /// <summary>
    /// Tests that declaring a variable of an unsupported type throws <see cref="NotSupportedException"/>.
    /// </summary>
    [Theory]
    [InlineData(UtopIRType.Fathom)]
    [InlineData(UtopIRType.Foot)]
    [InlineData(UtopIRType.Decree)]
    [InlineData(UtopIRType.Stitch)]
    [InlineData(UtopIRType.Yarn)]
    public void Emit_UnsupportedType_ThrowsNotSupportedException(UtopIRType type)
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        UtopIRProgram program = new([new WelcomeInstruction(new("x"), type)]);

        try
        {
            Should.Throw<NotSupportedException>(() => new CilEmitter().Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library)));
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
    /// Tests that the maximum <c>byte</c> value (255) round-trips correctly through the <c>(int)</c> widening cast used internally by <c>EmitLoadLiteralValue</c>.
    /// </summary>
    [Fact]
    public void Emit_StandingSausageRollMaxValue_RoundTripsCorrectly()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("x"), UtopIRType.StandingSausageRoll),
            new AppointInstruction(new("x"), new LiteralOperand((byte)255)),
            new FindInstruction(new VariableOperand(new("x")))
        ]);

        RunProgram(program).ShouldBe(255);
    }

    /// <summary>
    /// Tests that a <c>uint</c> above <see cref="int.MaxValue"/> round-trips through the bit-reinterpreting <c>(int)</c> cast.
    /// </summary>
    /// <remarks>
    /// The bit pattern pushed by <c>ldc.i4</c> is stored into a genuinely <c>uint</c>-typed
    ///  local, so the value read back is the original unsigned value, not the negative-looking
    /// intermediate.
    /// </remarks>
    [Fact]
    public void Emit_StandingPeerValueAboveIntMaxValue_RoundTripsCorrectly()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.StandingPeer),
            new WelcomeInstruction(new("b"), UtopIRType.StandingPeer),
            new WelcomeInstruction(new("r"), UtopIRType.StandingPeer),
            new AppointInstruction(new("a"), new LiteralOperand(4_000_000_000u)),
            new AppointInstruction(new("b"), new LiteralOperand(1u)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Diff, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        // 4_000_000_000 - 1 = 3_999_999_999; narrowed to Int32 via conv.i4 on return wraps
        // to the same bit pattern as an Int32, which we recover here via unchecked cast.
        RunProgram(program).ShouldBe(unchecked((int)3_999_999_999u));
    }

    /// <summary>
    /// Tests that <see cref="CilOutputKind.Executable"/> registers <c>Opera.Main</c> as the CLR entry point.
    /// </summary>
    [Fact]
    public void Emit_ExecutableOutputKind_SetsEntryPointAndRunsCorrectly()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");
        string runtimeConfigurationPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");

        UtopIRProgram program = new([new FindInstruction(new LiteralOperand(9))]);
        try
        {
            CilEmitter emitter = new();
            emitter.Emit(program, new(assemblyName, outputPath, CilOutputKind.Executable));

            File.Exists(runtimeConfigurationPath).ShouldBeTrue();

            Assembly assembly = Assembly.LoadFrom(outputPath);
            assembly.EntryPoint.ShouldNotBeNull();
            assembly.EntryPoint!.Name.ShouldBe("Main");

            Type operaType = assembly.GetType("Opera")!;
            MethodInfo mainMethod = operaType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;
            ((int)mainMethod.Invoke(null, [Array.Empty<string>()])!).ShouldBe(9);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            if (File.Exists(runtimeConfigurationPath))
            {
                File.Delete(runtimeConfigurationPath);
            }
        }
    }

    /// <summary>
    /// Tests that <see cref="CilEmitResult.IlSource"/> reflects the emitted instructions and that no <c>.il</c> file is written automatically.
    /// </summary>
    [Fact]
    public void Emit_ReturnsIlSource_ReflectingEmittedInstructionsWithoutWritingToDisk()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");
        string ilSourcePath = Path.ChangeExtension(outputPath, ".il");

        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(10)),
            new AppointInstruction(new("b"), new LiteralOperand(3)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Sum, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        try
        {
            CilEmitter emitter = new();
            CilEmitResult result = emitter.Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library));

            result.IlSource.ShouldContain("ldc.i4.s 10");
            result.IlSource.ShouldContain("stloc '£a'");
            result.IlSource.ShouldContain("ldc.i4.3");
            result.IlSource.ShouldContain("stloc '£b'");
            result.IlSource.ShouldContain("ldloc '£a'");
            result.IlSource.ShouldContain("ldloc '£b'");
            result.IlSource.ShouldContain("add");
            result.IlSource.ShouldContain("stloc '£r'");
            result.IlSource.ShouldContain("ldloc '£r'");
            result.IlSource.ShouldContain("ret");

            File.Exists(ilSourcePath).ShouldBeFalse();
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
    /// Emits a program, loads it, invokes <c>Main</c> and returns the exit code.
    /// </summary>
    /// <param name="program">The UtopIR programme to emit and run.</param>
    /// <returns>The integer exit code returned by the <c>Main</c> method.</returns>
    private static int RunProgram(UtopIRProgram program)
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            CilEmitter emitter = new();
            emitter.Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library));

            Assembly assembly = Assembly.LoadFrom(outputPath);
            Type operaType = assembly.GetType("Opera")!;
            MethodInfo mainMethod = operaType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;
            return (int)mainMethod.Invoke(null, [Array.Empty<string>()])!;
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
    /// Tests that <see cref="CilOutputKind.IlSourceOnly"/> produces IL text without writing any assembly.
    /// </summary>
    [Fact]
    public void Emit_IlSourceOnlyOutputKind_ProducesIlTextAndWritesNoAssembly()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(10)),
            new AppointInstruction(new("b"), new LiteralOperand(3)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Sum, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        string assemblyName = $"TopsyTurvyIlOnlyTest_{Guid.NewGuid():N}";
        string nonExistentDirectoryOutputPath = Path.Combine(
            Path.GetTempPath(),
            $"TopsyTurvyIlOnlyTest_NoSuchDirectory_{Guid.NewGuid():N}",
            assemblyName + ".dll");

        CilEmitResult result = new CilEmitter().Emit(
            program,
            new CilEmitOptions(assemblyName, nonExistentDirectoryOutputPath, CilOutputKind.IlSourceOnly));

        result.IlSource.ShouldContain("ldc.i4.s 10");
        result.IlSource.ShouldContain("stloc '£a'");
        result.IlSource.ShouldContain("add");
        result.IlSource.ShouldContain("ret");

        // Directory.Exists is false, so if Emit had attempted to write anything to OutputPath, it
        // would have thrown DirectoryNotFoundException before reaching this point.
        Directory.Exists(Path.GetDirectoryName(nonExistentDirectoryOutputPath)).ShouldBeFalse();
    }

    /// <summary>
    /// Tests that <c>were</c> widens <c>peer</c> to <c>chancellor</c> by emitting <c>conv.i8</c>.
    /// </summary>
    [Fact]
    public void Emit_Were_WidensPeerToChancellor_EmitsConvI8()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Chancellor),
            new AppointInstruction(new("a"), new LiteralOperand(-5)),
            new WereInstruction(new("b"), new VariableOperand(new("a")), UtopIRType.Chancellor),
            new FindInstruction(new VariableOperand(new("b")))
        ]);

        (int exitCode, string ilSource) = RunProgramWithIlSource(program);

        ilSource.ShouldContain("conv.i8");
        exitCode.ShouldBe(-5);
    }

    /// <summary>
    /// Tests that <c>were</c> narrows <c>chancellor</c> to <c>peer</c> by truncating via <c>conv.i4</c>.
    /// </summary>
    [Fact]
    public void Emit_Were_NarrowsChancellorToPeer_TruncatesToLow32Bits()
    {
        const long outOfRangeValue = (1L << 32) + 100_000;
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Chancellor),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(outOfRangeValue)),
            new WereInstruction(new("b"), new VariableOperand(new("a")), UtopIRType.Peer),
            new FindInstruction(new VariableOperand(new("b")))
        ]);

        (int exitCode, string ilSource) = RunProgramWithIlSource(program);

        ilSource.ShouldContain("conv.i4");
        exitCode.ShouldBe(100_000);
    }

    /// <summary>
    /// Tests that <c>were</c> to <c>standingpeer</c> emits <c>conv.u4</c>.
    /// </summary>
    [Fact]
    public void Emit_Were_ConvertsPeerToStandingPeer_EmitsConvU4()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.StandingPeer),
            new AppointInstruction(new("a"), new LiteralOperand(7)),
            new WereInstruction(new("b"), new VariableOperand(new("a")), UtopIRType.StandingPeer),
            new FindInstruction(new VariableOperand(new("b")))
        ]);

        (int exitCode, string ilSource) = RunProgramWithIlSource(program);

        ilSource.ShouldContain("conv.u4");
        exitCode.ShouldBe(7);
    }

    /// <summary>
    /// Tests that <c>were</c> narrows a <c>chancellor</c> value above <see cref="int.MaxValue"/> into <c>standingpeer</c> correctly.
    /// </summary>
    [Fact]
    public void Emit_Were_NarrowsChancellorToStandingPeer_RoundTripsValueAboveIntMaxValue()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Chancellor),
            new WelcomeInstruction(new("b"), UtopIRType.StandingPeer),
            new AppointInstruction(new("a"), new LiteralOperand(4_000_000_000L)),
            new WereInstruction(new("b"), new VariableOperand(new("a")), UtopIRType.StandingPeer),
            new FindInstruction(new VariableOperand(new("b")))
        ]);

        RunProgram(program).ShouldBe(unchecked((int)4_000_000_000u));
    }

    /// <summary>
    /// Tests that a <c>were</c> instruction whose target has no prior <see cref="WelcomeInstruction"/> auto-declares it with the destination type.
    /// </summary>
    [Fact]
    public void Emit_Were_UndeclaredTarget_AutoDeclaresWithDestinationType()
    {
        UtopIRProgram program = new([
            new WereInstruction(new("_were_11_chancellor"), new LiteralOperand(11), UtopIRType.Chancellor),
            new FindInstruction(new VariableOperand(new("_were_11_chancellor")))
        ]);

        RunProgram(program).ShouldBe(11);
    }

    /// <summary>
    /// Emits, runs and returns both the exit code and the emitted IL source text.
    /// </summary>
    /// <param name="program">The UtopIR programme to emit and run.</param>
    /// <returns>The integer exit code and the <see cref="CilEmitResult.IlSource"/> text.</returns>
    private static (int ExitCode, string IlSource) RunProgramWithIlSource(UtopIRProgram program)
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            CilEmitter emitter = new();
            CilEmitResult result = emitter.Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library));

            Assembly assembly = Assembly.LoadFrom(outputPath);
            Type operaType = assembly.GetType("Opera")!;
            MethodInfo mainMethod = operaType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!;
            int exitCode = (int)mainMethod.Invoke(null, [Array.Empty<string>()])!;
            return (exitCode, result.IlSource);
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
