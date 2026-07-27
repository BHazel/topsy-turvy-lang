using System;
using System.Collections.Generic;
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
    /// Tests that the <c>fathom</c> type (64-bit floating-point) is correctly declared and stored but the value is truncated toward zero to <c>int32</c> on return.
    /// </summary>
    [Fact]
    public void Emit_Fathom_StoresAndReturnsTruncated()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("x"), UtopIRType.Fathom),
            new AppointInstruction(new("x"), new LiteralOperand(42.9)),
            new FindInstruction(new VariableOperand(new("x")))
        ]);

        RunProgram(program).ShouldBe(42);
    }

    /// <summary>
    /// Tests that the <c>foot</c> type (32-bit floating-point) is correctly declared and stored but the value is truncated toward zero to <c>int32</c> on return.
    /// </summary>
    [Fact]
    public void Emit_Foot_StoresAndReturnsTruncated()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("x"), UtopIRType.Foot),
            new AppointInstruction(new("x"), new LiteralOperand(8.5f)),
            new FindInstruction(new VariableOperand(new("x")))
        ]);

        RunProgram(program).ShouldBe(8);
    }

    /// <summary>
    /// Tests that <c>sum.f</c> adds two <c>fathom</c> operands.
    /// </summary>
    [Fact]
    public void Emit_SumFloat_AddsOperands()
    {
        // 1.5 + 2.75 = 4.25, truncated to 4 on return.
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Fathom),
            new WelcomeInstruction(new("b"), UtopIRType.Fathom),
            new WelcomeInstruction(new("r"), UtopIRType.Fathom),
            new AppointInstruction(new("a"), new LiteralOperand(1.5)),
            new AppointInstruction(new("b"), new LiteralOperand(2.75)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.SumFloat, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(4);
    }

    /// <summary>
    /// Tests that <c>diff.f</c> subtracts one <c>fathom</c> operand from another.
    /// </summary>
    [Fact]
    public void Emit_DiffFloat_SubtractsOperands()
    {
        // 10.0 - 2.5 = 7.5, truncated to 7 on return.
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Fathom),
            new WelcomeInstruction(new("b"), UtopIRType.Fathom),
            new WelcomeInstruction(new("r"), UtopIRType.Fathom),
            new AppointInstruction(new("a"), new LiteralOperand(10.0)),
            new AppointInstruction(new("b"), new LiteralOperand(2.5)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.DiffFloat, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(7);
    }

    /// <summary>
    /// Tests that <c>prod.f</c> multiplies two <c>fathom</c> operands.
    /// </summary>
    [Fact]
    public void Emit_ProdFloat_MultipliesOperands()
    {
        // 1.25 * 3.5 = 4.375, truncated to 4 on return.
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Fathom),
            new WelcomeInstruction(new("b"), UtopIRType.Fathom),
            new WelcomeInstruction(new("r"), UtopIRType.Fathom),
            new AppointInstruction(new("a"), new LiteralOperand(1.25)),
            new AppointInstruction(new("b"), new LiteralOperand(3.5)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.ProdFloat, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(4);
    }

    /// <summary>
    /// Tests that <c>quot.f</c> divides one <c>fathom</c> operand by another without integer truncation of the intermediate result.
    /// </summary>
    [Fact]
    public void Emit_QuotFloat_DividesOperands()
    {
        // 15.0 / 2.0 = 7.5, truncated to 7 on return; integer division of 15 / 2 would also give 7,
        // so scale by ten first (7.5 * 10 = 75) to prove the division was floating-point.
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Fathom),
            new WelcomeInstruction(new("b"), UtopIRType.Fathom),
            new WelcomeInstruction(new("scale"), UtopIRType.Fathom),
            new WelcomeInstruction(new("q"), UtopIRType.Fathom),
            new WelcomeInstruction(new("r"), UtopIRType.Fathom),
            new AppointInstruction(new("a"), new LiteralOperand(15.0)),
            new AppointInstruction(new("b"), new LiteralOperand(2.0)),
            new AppointInstruction(new("scale"), new LiteralOperand(10.0)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.QuotFloat, new("q"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new ArithmeticInstruction(UtopIRArithmeticOperation.ProdFloat, new("r"), new VariableOperand(new("q")), new VariableOperand(new("scale"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(75);
    }

    /// <summary>
    /// Tests that <c>rem.f</c> computes the floating-point remainder of two <c>fathom</c> operands.
    /// </summary>
    [Fact]
    public void Emit_RemFloat_ComputesRemainder()
    {
        // 7.5 rem 2.0 = 1.5, scaled by ten (15) to prove the fractional part survived.
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Fathom),
            new WelcomeInstruction(new("b"), UtopIRType.Fathom),
            new WelcomeInstruction(new("scale"), UtopIRType.Fathom),
            new WelcomeInstruction(new("m"), UtopIRType.Fathom),
            new WelcomeInstruction(new("r"), UtopIRType.Fathom),
            new AppointInstruction(new("a"), new LiteralOperand(7.5)),
            new AppointInstruction(new("b"), new LiteralOperand(2.0)),
            new AppointInstruction(new("scale"), new LiteralOperand(10.0)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.RemFloat, new("m"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new ArithmeticInstruction(UtopIRArithmeticOperation.ProdFloat, new("r"), new VariableOperand(new("m")), new VariableOperand(new("scale"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(15);
    }

    /// <summary>
    /// Tests that <c>max.f</c> returns the larger of two <c>fathom</c> operands.
    /// </summary>
    [Fact]
    public void Emit_MaxFloat_ReturnsLargerOperand()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Fathom),
            new WelcomeInstruction(new("b"), UtopIRType.Fathom),
            new WelcomeInstruction(new("r"), UtopIRType.Fathom),
            new AppointInstruction(new("a"), new LiteralOperand(10.5)),
            new AppointInstruction(new("b"), new LiteralOperand(5.25)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.MaxFloat, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(10);
    }

    /// <summary>
    /// Tests that <c>min.f</c> returns the smaller of two <c>fathom</c> operands.
    /// </summary>
    [Fact]
    public void Emit_MinFloat_ReturnsSmallerOperand()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Fathom),
            new WelcomeInstruction(new("b"), UtopIRType.Fathom),
            new WelcomeInstruction(new("r"), UtopIRType.Fathom),
            new AppointInstruction(new("a"), new LiteralOperand(10.5)),
            new AppointInstruction(new("b"), new LiteralOperand(5.25)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.MinFloat, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(5);
    }

    /// <summary>
    /// Tests that floating-point arithmetic on <c>foot</c> operands resolves the <c>float</c> overload of <see cref="Math.Max(float, float)"/> correctly.
    /// </summary>
    [Fact]
    public void Emit_MaxFloat_OnFootOperands_ReturnsLargerOperand()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Foot),
            new WelcomeInstruction(new("b"), UtopIRType.Foot),
            new WelcomeInstruction(new("r"), UtopIRType.Foot),
            new AppointInstruction(new("a"), new LiteralOperand(2.5f)),
            new AppointInstruction(new("b"), new LiteralOperand(9.75f)),
            new ArithmeticInstruction(UtopIRArithmeticOperation.MaxFloat, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(9);
    }

    /// <summary>
    /// Tests that a floating-point arithmetic instruction with integer operands throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Emit_FloatOperationOnIntegerOperands_ThrowsInvalidOperationException()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        UtopIRProgram program = new([
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new ArithmeticInstruction(UtopIRArithmeticOperation.SumFloat, new("r"), new LiteralOperand(1), new LiteralOperand(2))
        ]);

        try
        {
            Should.Throw<InvalidOperationException>(() => new CilEmitter().Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library)));
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
    /// Tests that an integer arithmetic instruction with floating-point operands throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Emit_IntegerOperationOnFloatOperands_ThrowsInvalidOperationException()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        UtopIRProgram program = new([
            new WelcomeInstruction(new("r"), UtopIRType.Fathom),
            new ArithmeticInstruction(UtopIRArithmeticOperation.Sum, new("r"), new LiteralOperand(1.5), new LiteralOperand(2.5))
        ]);

        try
        {
            Should.Throw<InvalidOperationException>(() => new CilEmitter().Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library)));
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
    /// Tests that each binary <see cref="BitwiseInstruction"/> operation computes the correct result on <c>peer</c> operands.
    /// </summary>
    /// <param name="operation">The <see cref="UtopIRBitwiseOperation"/> to test.</param>
    /// <param name="operand1">The first operand value.</param>
    /// <param name="operand2">The second operand value.</param>
    /// <param name="expectedResult">The expected exit code.</param>
    [Theory]
    [InlineData(UtopIRBitwiseOperation.Chord, 9, 3, 1)]
    [InlineData(UtopIRBitwiseOperation.Harmony, 9, 3, 11)]
    [InlineData(UtopIRBitwiseOperation.Discord, 9, 3, 10)]
    [InlineData(UtopIRBitwiseOperation.TransUp, 10, 1, 20)]
    [InlineData(UtopIRBitwiseOperation.TransDown, 10, 1, 5)]
    [InlineData(UtopIRBitwiseOperation.TransUp, 1, 5, 32)]
    public void Emit_BitwiseOperation_ComputesCorrectResult(UtopIRBitwiseOperation operation, int operand1, int operand2, int expectedResult)
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(operand1)),
            new AppointInstruction(new("b"), new LiteralOperand(operand2)),
            new BitwiseInstruction(operation, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that an <see cref="InvInstruction"/> computes the bitwise complement of its operand.
    /// </summary>
    [Fact]
    public void Emit_Inv_ComputesBitwiseComplement()
    {
        // ~10 = -11 in two's complement.
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(10)),
            new InvInstruction(new("r"), new VariableOperand(new("a"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(-11);
    }

    /// <summary>
    /// Tests that <c>transdown</c> on an unsigned type uses <c>shr.un</c> so the vacated high bit is zero-filled.
    /// </summary>
    [Fact]
    public void Emit_TransDown_UnsignedPeer_UsesShrUn()
    {
        // 0x80000000 >> 1 must be 0x40000000, not sign-extended.
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.StandingPeer),
            new WelcomeInstruction(new("b"), UtopIRType.StandingPeer),
            new WelcomeInstruction(new("r"), UtopIRType.StandingPeer),
            new AppointInstruction(new("a"), new LiteralOperand(0x80000000u)),
            new AppointInstruction(new("b"), new LiteralOperand(1u)),
            new BitwiseInstruction(UtopIRBitwiseOperation.TransDown, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        (int exitCode, string ilSource) = RunProgramWithIlSource(program);

        ilSource.ShouldContain("shr.un");
        exitCode.ShouldBe(0x40000000);
    }

    /// <summary>
    /// Tests that a shift on a 64-bit type converts the shift amount to <c>int32</c> so the emitted IL remains valid.
    /// </summary>
    [Fact]
    public void Emit_TransUp_OnChancellor_ConvertsShiftAmountAndComputesCorrectResult()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Chancellor),
            new WelcomeInstruction(new("b"), UtopIRType.Chancellor),
            new WelcomeInstruction(new("r"), UtopIRType.Chancellor),
            new AppointInstruction(new("a"), new LiteralOperand(5L)),
            new AppointInstruction(new("b"), new LiteralOperand(1L)),
            new BitwiseInstruction(UtopIRBitwiseOperation.TransUp, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(10);
    }

    /// <summary>
    /// Tests that a <see cref="BitwiseInstruction"/> with floating-point operands throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Emit_BitwiseOperationOnFloatOperands_ThrowsInvalidOperationException()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        UtopIRProgram program = new([
            new WelcomeInstruction(new("r"), UtopIRType.Fathom),
            new BitwiseInstruction(UtopIRBitwiseOperation.Chord, new("r"), new LiteralOperand(1.5), new LiteralOperand(2.5))
        ]);

        try
        {
            Should.Throw<InvalidOperationException>(() => new CilEmitter().Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library)));
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
    /// Tests that an <see cref="InvInstruction"/> with a floating-point operand throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Emit_InvOnFloatOperand_ThrowsInvalidOperationException()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        UtopIRProgram program = new([
            new WelcomeInstruction(new("r"), UtopIRType.Fathom),
            new InvInstruction(new("r"), new LiteralOperand(1.5))
        ]);

        try
        {
            Should.Throw<InvalidOperationException>(() => new CilEmitter().Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library)));
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
    /// Tests that each <see cref="UtopIRComparisonOperation"/> computes the correct <c>decree</c> result, returned as <c>0</c> or <c>1</c>, on <c>peer</c> operands.
    /// </summary>
    /// <param name="operation">The <see cref="UtopIRComparisonOperation"/> to test.</param>
    /// <param name="operand1">The first operand value.</param>
    /// <param name="operand2">The second operand value.</param>
    /// <param name="expectedResult">The expected exit code, <c>1</c> for <c>verity</c> or <c>0</c> for <c>nay</c>.</param>
    [Theory]
    [InlineData(UtopIRComparisonOperation.Alike, 5, 5, 1)]
    [InlineData(UtopIRComparisonOperation.Alike, 5, 6, 0)]
    [InlineData(UtopIRComparisonOperation.Unlike, 5, 6, 1)]
    [InlineData(UtopIRComparisonOperation.Unlike, 5, 5, 0)]
    [InlineData(UtopIRComparisonOperation.PreAdam, 5, 3, 1)]
    [InlineData(UtopIRComparisonOperation.PreAdam, 3, 5, 0)]
    [InlineData(UtopIRComparisonOperation.LowerDeg, 3, 5, 1)]
    [InlineData(UtopIRComparisonOperation.LowerDeg, 5, 3, 0)]
    public void Emit_Comparison_ComputesCorrectResult(UtopIRComparisonOperation operation, int operand1, int operand2, int expectedResult)
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new WelcomeInstruction(new("r"), UtopIRType.Decree),
            new AppointInstruction(new("a"), new LiteralOperand(operand1)),
            new AppointInstruction(new("b"), new LiteralOperand(operand2)),
            new ComparisonInstruction(operation, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that each floating-point <see cref="UtopIRComparisonOperation"/> computes the correct <c>decree</c> result on <c>fathom</c> operands.
    /// </summary>
    /// <param name="operation">The floating-point <see cref="UtopIRComparisonOperation"/> to test.</param>
    /// <param name="operand1">The first operand value.</param>
    /// <param name="operand2">The second operand value.</param>
    /// <param name="expectedResult">The expected exit code, <c>1</c> for <c>verity</c> or <c>0</c> for <c>nay</c>.</param>
    [Theory]
    [InlineData(UtopIRComparisonOperation.AlikeFloat, 1.5, 1.5, 1)]
    [InlineData(UtopIRComparisonOperation.AlikeFloat, 1.5, 2.5, 0)]
    [InlineData(UtopIRComparisonOperation.PreAdamFloat, 2.5, 1.5, 1)]
    [InlineData(UtopIRComparisonOperation.LowerDegFloat, 1.5, 2.5, 1)]
    public void Emit_ComparisonFloat_ComputesCorrectResult(UtopIRComparisonOperation operation, double operand1, double operand2, int expectedResult)
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Fathom),
            new WelcomeInstruction(new("b"), UtopIRType.Fathom),
            new WelcomeInstruction(new("r"), UtopIRType.Decree),
            new AppointInstruction(new("a"), new LiteralOperand(operand1)),
            new AppointInstruction(new("b"), new LiteralOperand(operand2)),
            new ComparisonInstruction(operation, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that <c>preadam</c> on <c>standingpeer</c> operands uses <c>cgt.un</c> so a value whose top bit is
    /// set is compared as a large positive magnitude rather than a negative signed value.
    /// </summary>
    [Fact]
    public void Emit_PreAdam_UnsignedStandingPeer_UsesCgtUn()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.StandingPeer),
            new WelcomeInstruction(new("b"), UtopIRType.StandingPeer),
            new WelcomeInstruction(new("r"), UtopIRType.Decree),
            new AppointInstruction(new("a"), new LiteralOperand(4_000_000_000u)),
            new AppointInstruction(new("b"), new LiteralOperand(1u)),
            new ComparisonInstruction(UtopIRComparisonOperation.PreAdam, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        (int exitCode, string ilSource) = RunProgramWithIlSource(program);

        ilSource.ShouldContain("cgt.un");
        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that <c>lowerdeg</c> on <c>standingpeer</c> operands uses <c>clt.un</c> so a value whose top bit is
    /// set is compared as a large positive magnitude rather than a negative signed value.
    /// </summary>
    [Fact]
    public void Emit_LowerDeg_UnsignedStandingPeer_UsesCltUn()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.StandingPeer),
            new WelcomeInstruction(new("b"), UtopIRType.StandingPeer),
            new WelcomeInstruction(new("r"), UtopIRType.Decree),
            new AppointInstruction(new("a"), new LiteralOperand(1u)),
            new AppointInstruction(new("b"), new LiteralOperand(4_000_000_000u)),
            new ComparisonInstruction(UtopIRComparisonOperation.LowerDeg, new("r"), new VariableOperand(new("a")), new VariableOperand(new("b"))),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        (int exitCode, string ilSource) = RunProgramWithIlSource(program);

        ilSource.ShouldContain("clt.un");
        exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <see cref="ComparisonInstruction"/> with mismatched operand types throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Emit_ComparisonWithMismatchedOperandTypes_ThrowsInvalidOperationException()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        UtopIRProgram program = new([
            new WelcomeInstruction(new("r"), UtopIRType.Decree),
            new ComparisonInstruction(UtopIRComparisonOperation.Alike, new("r"), new LiteralOperand(1), new LiteralOperand(1.5))
        ]);

        try
        {
            Should.Throw<InvalidOperationException>(() => new CilEmitter().Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library)));
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
    /// Tests that each <see cref="UtopIRLogicalOperation"/> computes the correct <c>decree</c> result, returned as <c>0</c> or <c>1</c>, with an undeclared target auto-declared as <see cref="bool"/>.
    /// </summary>
    /// <param name="operation">The <see cref="UtopIRLogicalOperation"/> to test.</param>
    /// <param name="operand1">The first operand value.</param>
    /// <param name="operand2">The second operand value.</param>
    /// <param name="expectedResult">The expected exit code, <c>1</c> for <c>verity</c> or <c>0</c> for <c>nay</c>.</param>
    [Theory]
    [InlineData(UtopIRLogicalOperation.Both, true, true, 1)]
    [InlineData(UtopIRLogicalOperation.Both, true, false, 0)]
    [InlineData(UtopIRLogicalOperation.Either, false, false, 0)]
    [InlineData(UtopIRLogicalOperation.Either, true, false, 1)]
    public void Emit_Logical_ComputesCorrectResult(UtopIRLogicalOperation operation, bool operand1, bool operand2, int expectedResult)
    {
        UtopIRProgram program = new([
            new LogicalInstruction(operation, new("r"), new LiteralOperand(operand1), new LiteralOperand(operand2)),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that a <see cref="LogicalInstruction"/> with a non-<c>decree</c> operand throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Emit_LogicalWithNonDecreeOperand_ThrowsInvalidOperationException()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        UtopIRProgram program = new([
            new WelcomeInstruction(new("r"), UtopIRType.Decree),
            new LogicalInstruction(UtopIRLogicalOperation.Both, new("r"), new LiteralOperand(1), new LiteralOperand(2))
        ]);

        try
        {
            Should.Throw<InvalidOperationException>(() => new CilEmitter().Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library)));
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
    /// Tests that a <see cref="HardlyInstruction"/> negates its <c>decree</c> operand.
    /// </summary>
    /// <param name="operand">The operand value.</param>
    /// <param name="expectedResult">The expected exit code, the negation of <paramref name="operand"/>.</param>
    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public void Emit_Hardly_NegatesOperand(bool operand, int expectedResult)
    {
        UtopIRProgram program = new([
            new HardlyInstruction(new("r"), new LiteralOperand(operand)),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that a <see cref="HardlyInstruction"/> with a non-<c>decree</c> operand throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Emit_HardlyWithNonDecreeOperand_ThrowsInvalidOperationException()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        UtopIRProgram program = new([
            new WelcomeInstruction(new("r"), UtopIRType.Decree),
            new HardlyInstruction(new("r"), new LiteralOperand(1))
        ]);

        try
        {
            Should.Throw<InvalidOperationException>(() => new CilEmitter().Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library)));
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
    /// Tests that a <see cref="VictimYarnInstruction"/> selects the 4th (1-based) character of
    /// <c>"Hollow"</c>, <c>'l'</c>, returning its code point 108 as the exit code.
    /// </summary>
    [Fact]
    public void Emit_VictimYarn_SelectsCorrectCharacter()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("PoemSubject"), UtopIRType.Yarn),
            new AppointInstruction(new("PoemSubject"), new LiteralOperand("Hollow")),
            new VictimYarnInstruction(new("r"), new VariableOperand(new("PoemSubject")), new LiteralOperand(4)),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(108);
    }

    /// <summary>
    /// Tests that a <see cref="WelcomeListInstruction"/> followed by <see cref="AppointVictimInstruction"/>s
    /// and a <see cref="VictimListInstruction"/> allocates a real CLR array, writes each element via
    /// <c>stelem</c> and reads one back via <c>ldelem</c>.
    /// </summary>
    [Fact]
    public void Emit_WelcomeListAppointVictimVictimList_StoresAndReadsElement()
    {
        UtopIRProgram program = new([
            new WelcomeListInstruction(new("Numbers"), UtopIRType.Peer, new LiteralOperand(3)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(1), new LiteralOperand(10)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(2), new LiteralOperand(20)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(3), new LiteralOperand(30)),
            new VictimListInstruction(new("r"), new("Numbers"), new LiteralOperand(2)),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(20);
    }

    /// <summary>
    /// Tests that a <see cref="WelcomeListInstruction"/> whose size is a <see cref="VariableOperand"/> rather than
    /// a <see cref="LiteralOperand"/> allocates a CLR array sized to the variable current value.
    /// </summary>
    [Fact]
    public void Emit_WelcomeListWithVariableSize_AllocatesArraySizedToVariable()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("Count"), UtopIRType.Peer),
            new AppointInstruction(new("Count"), new LiteralOperand(3)),
            new WelcomeListInstruction(new("Numbers"), UtopIRType.Peer, new VariableOperand(new("Count"))),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(3), new LiteralOperand(30)),
            new VictimListInstruction(new("r"), new("Numbers"), new LiteralOperand(3)),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(30);
    }

    /// <summary>
    /// Tests that a <see cref="WelcomeListInstruction"/> declared with a bare size and no
    /// <see cref="AppointVictimInstruction"/>s produces a <c>newarr</c>-zero-initialised element when
    /// read back, matching the Topsy Turvy "pre-allocated with default values" semantics.
    /// </summary>
    [Fact]
    public void Emit_WelcomeListWithBareSizeAndNoAppointVictim_ReadsZeroInitialisedElement()
    {
        UtopIRProgram program = new([
            new WelcomeListInstruction(new("Numbers"), UtopIRType.Peer, new LiteralOperand(3)),
            new VictimListInstruction(new("r"), new("Numbers"), new LiteralOperand(1)),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(0);
    }

    /// <summary>
    /// Tests that <see cref="PicturetoInstruction"/> and <see cref="ViewfromInstruction"/> allocate a
    /// real <c>PointerHandle</c> referring to an array first element and read it back.
    /// </summary>
    [Fact]
    public void Emit_PicturetoAndViewfromOnArray_ReadsFirstElement()
    {
        UtopIRProgram program = new([
            new WelcomeListInstruction(new("Numbers"), UtopIRType.Peer, new LiteralOperand(3)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(1), new LiteralOperand(10)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(2), new LiteralOperand(20)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(3), new LiteralOperand(30)),
            new WelcomeGallerypicInstruction(new("NumbersPointer"), UtopIRType.Peer),
            new PicturetoInstruction(new("NumbersPointer"), new("Numbers")),
            new ViewfromInstruction(new("r"), new("NumbersPointer")),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(10);
    }

    /// <summary>
    /// Tests that <see cref="ViewtoInstruction"/> genuinely writes through the pointer into the
    /// referenced array. Verified by reading the element back directly via
    /// <see cref="VictimListInstruction"/> afterward, not just through the pointer again.
    /// </summary>
    [Fact]
    public void Emit_Viewto_WritesThroughIntoArrayElement()
    {
        UtopIRProgram program = new([
            new WelcomeListInstruction(new("Numbers"), UtopIRType.Peer, new LiteralOperand(3)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(1), new LiteralOperand(10)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(2), new LiteralOperand(20)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(3), new LiteralOperand(30)),
            new WelcomeGallerypicInstruction(new("NumbersPointer"), UtopIRType.Peer),
            new PicturetoInstruction(new("NumbersPointer"), new("Numbers")),
            new ViewtoInstruction(new("NumbersPointer"), new LiteralOperand(99)),
            new VictimListInstruction(new("r"), new("Numbers"), new LiteralOperand(1)),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(99);
    }

    /// <summary>
    /// Tests that a <see cref="PointerArithmeticInstruction"/> (<c>sum.g</c>) advances the index of a
    /// <c>PointerHandle</c> without touching its <c>Container</c>.
    /// </summary>
    [Fact]
    public void Emit_PointerArithmeticSum_AdvancesToCorrectElement()
    {
        UtopIRProgram program = new([
            new WelcomeListInstruction(new("Numbers"), UtopIRType.Peer, new LiteralOperand(3)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(1), new LiteralOperand(10)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(2), new LiteralOperand(20)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(3), new LiteralOperand(30)),
            new WelcomeGallerypicInstruction(new("NumbersPointer"), UtopIRType.Peer),
            new PicturetoInstruction(new("NumbersPointer"), new("Numbers")),
            new PointerArithmeticInstruction(UtopIRPointerArithmeticOperation.Sum, new("NumbersPointerOffset"), new("NumbersPointer"), new LiteralOperand(2)),
            new ViewfromInstruction(new("r"), new("NumbersPointerOffset")),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(30);
    }

    /// <summary>
    /// Tests that a <see cref="PointerArithmeticInstruction"/> on a pointer into a
    /// <c>yarn</c> advances the index of a <c>PointerHandle</c> the same way it does for an array,
    /// since <c>sum.g</c>/<c>diff.g</c> only ever adjust the index field, regardless of the runtime
    /// type of the container.
    /// </summary>
    [Fact]
    public void Emit_PointerArithmeticSumOnYarnPointer_AdvancesToCorrectCharacter()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("PoemSubject"), UtopIRType.Yarn),
            new AppointInstruction(new("PoemSubject"), new LiteralOperand("Hollow")),
            new WelcomeGallerypicInstruction(new("PoemSubjectPointer"), UtopIRType.Stitch),
            new PicturetoInstruction(new("PoemSubjectPointer"), new("PoemSubject")),
            new PointerArithmeticInstruction(UtopIRPointerArithmeticOperation.Sum, new("PoemSubjectPointerOffset"), new("PoemSubjectPointer"), new LiteralOperand(2)),
            new ViewfromInstruction(new("r"), new("PoemSubjectPointerOffset")),
            new FindInstruction(new VariableOperand(new("r")))
        ]);

        RunProgram(program).ShouldBe(108);
    }

    /// <summary>
    /// Tests that <see cref="ViewtoInstruction"/> writing through a pointer into a <c>yarn</c>
    /// character throws <see cref="InvalidOperationException"/> at runtime, since strings are immutable.
    /// </summary>
    [Fact]
    public void Emit_ViewtoOnYarnPointer_ThrowsInvalidOperationExceptionAtRuntime()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("PoemSubject"), UtopIRType.Yarn),
            new AppointInstruction(new("PoemSubject"), new LiteralOperand("Hollow")),
            new WelcomeGallerypicInstruction(new("PoemSubjectPointer"), UtopIRType.Stitch),
            new PicturetoInstruction(new("PoemSubjectPointer"), new("PoemSubject")),
            new ViewtoInstruction(new("PoemSubjectPointer"), new LiteralOperand('X')),
            new FindInstruction(new LiteralOperand(0))
        ]);

        TargetInvocationException exception = Should.Throw<TargetInvocationException>(() => RunProgram(program));
        exception.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that a <see cref="PicturetoInstruction"/> targeting a plain scalar variable (not an
    /// array or <c>yarn</c>) throws <see cref="NotSupportedException"/> at emit time.
    /// </summary>
    [Fact]
    public void Emit_PicturetoOnScalarVariable_ThrowsNotSupportedException()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        UtopIRProgram program = new([
            new WelcomeInstruction(new("Number"), UtopIRType.Peer),
            new AppointInstruction(new("Number"), new LiteralOperand(42)),
            new WelcomeGallerypicInstruction(new("NumberPointer"), UtopIRType.Peer),
            new PicturetoInstruction(new("NumberPointer"), new("Number"))
        ]);

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
    /// Tests that appointing <c>naught</c> to a pointer previously assigned via <see cref="PicturetoInstruction"/>
    /// re-zeroes the <c>PointerHandle</c> via <c>initobj</c> without throwing, even though the local
    /// already held a real <c>Container</c> reference.
    /// </summary>
    [Fact]
    public void Emit_AppointNaughtToPointer_ReZeroesHandleWithoutThrowing()
    {
        UtopIRProgram program = new([
            new WelcomeListInstruction(new("Numbers"), UtopIRType.Peer, new LiteralOperand(1)),
            new AppointVictimInstruction(new("Numbers"), new LiteralOperand(1), new LiteralOperand(10)),
            new WelcomeGallerypicInstruction(new("NumbersPointer"), UtopIRType.Peer),
            new PicturetoInstruction(new("NumbersPointer"), new("Numbers")),
            new AppointInstruction(new("NumbersPointer"), new LiteralOperand(NaughtLiteral.Instance)),
            new FindInstruction(new LiteralOperand(1))
        ]);

        RunProgram(program).ShouldBe(1);
    }

    /// <summary>
    /// Tests that appointing <c>naught</c> to an array variable emits <c>ldnull</c>/<c>stloc</c> via
    /// the generic <see cref="LiteralOperand"/> path, since a CLR array is a reference type.
    /// </summary>
    [Fact]
    public void Emit_AppointNaughtToArray_SetsArrayReferenceToNullWithoutThrowing()
    {
        UtopIRProgram program = new([
            new WelcomeListInstruction(new("Numbers"), UtopIRType.Peer, new LiteralOperand(1)),
            new AppointInstruction(new("Numbers"), new LiteralOperand(NaughtLiteral.Instance)),
            new FindInstruction(new LiteralOperand(1))
        ]);

        RunProgram(program).ShouldBe(1);
    }

    /// <summary>
    /// Tests that appointing <c>naught</c> to a <c>yarn</c> variable emits <c>ldnull</c>/<c>stloc</c>
    /// via the generic <see cref="LiteralOperand"/> path, since a CLR <see cref="string"/> is a
    /// reference type.
    /// </summary>
    [Fact]
    public void Emit_AppointNaughtToYarn_SetsStringReferenceToNullWithoutThrowing()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("PoemSubject"), UtopIRType.Yarn),
            new AppointInstruction(new("PoemSubject"), new LiteralOperand(NaughtLiteral.Instance)),
            new FindInstruction(new LiteralOperand(1))
        ]);

        RunProgram(program).ShouldBe(1);
    }

    /// <summary>
    /// Tests that an unconditional <see cref="SailInstruction"/> skips the instruction immediately following it.
    /// </summary>
    [Fact]
    public void Emit_Sail_UnconditionalBranch_SkipsIntermediateInstruction()
    {
        UtopIRProgram program = new([
            new SailInstruction(new UtopIRLabel("LOGIC")),
            new FindInstruction(new LiteralOperand(0)),
            new LabelInstruction(new UtopIRLabel("LOGIC")),
            new FindInstruction(new LiteralOperand(1))
        ]);

        RunProgram(program).ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <see cref="LabelInstruction"/> reached without a preceding branch does not disrupt normal
    /// sequential execution, matching its flat-marker semantics.
    /// </summary>
    [Fact]
    public void Emit_Label_WithNoPrecedingBranch_ExecutesInstructionsInOrder()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("x"), UtopIRType.Peer),
            new AppointInstruction(new("x"), new LiteralOperand(1)),
            new LabelInstruction(new UtopIRLabel("MARK")),
            new AppointInstruction(new("x"), new LiteralOperand(2)),
            new FindInstruction(new VariableOperand(new("x")))
        ]);

        RunProgram(program).ShouldBe(2);
    }

    /// <summary>
    /// Tests that <see cref="SailAlikeInstruction"/> branches to the label when the <c>decree</c> value is
    /// <c>verity</c>.
    /// </summary>
    [Fact]
    public void Emit_SailAlike_ValueVerity_BranchesToLabel()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("Boolean"), UtopIRType.Decree),
            new AppointInstruction(new("Boolean"), new LiteralOperand(true)),
            new SailAlikeInstruction(new VariableOperand(new("Boolean")), new UtopIRLabel("IS_ALIKE")),
            new FindInstruction(new LiteralOperand(0)),
            new LabelInstruction(new UtopIRLabel("IS_ALIKE")),
            new FindInstruction(new LiteralOperand(1))
        ]);

        RunProgram(program).ShouldBe(1);
    }

    /// <summary>
    /// Tests that <see cref="SailAlikeInstruction"/> falls through without branching when the <c>decree</c> value
    /// is <c>nay</c>.
    /// </summary>
    [Fact]
    public void Emit_SailAlike_ValueNay_FallsThroughWithoutBranching()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("Boolean"), UtopIRType.Decree),
            new AppointInstruction(new("Boolean"), new LiteralOperand(false)),
            new SailAlikeInstruction(new VariableOperand(new("Boolean")), new UtopIRLabel("IS_ALIKE")),
            new FindInstruction(new LiteralOperand(0)),
            new LabelInstruction(new UtopIRLabel("IS_ALIKE")),
            new FindInstruction(new LiteralOperand(1))
        ]);

        RunProgram(program).ShouldBe(0);
    }

    /// <summary>
    /// Tests that <see cref="SailUnlikeInstruction"/> branches to the label when the <c>decree</c> value is
    /// <c>nay</c>.
    /// </summary>
    [Fact]
    public void Emit_SailUnlike_ValueNay_BranchesToLabel()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("Boolean"), UtopIRType.Decree),
            new AppointInstruction(new("Boolean"), new LiteralOperand(false)),
            new SailUnlikeInstruction(new VariableOperand(new("Boolean")), new UtopIRLabel("IS_UNLIKE")),
            new FindInstruction(new LiteralOperand(0)),
            new LabelInstruction(new UtopIRLabel("IS_UNLIKE")),
            new FindInstruction(new LiteralOperand(1))
        ]);

        RunProgram(program).ShouldBe(1);
    }

    /// <summary>
    /// Tests that <see cref="SailUnlikeInstruction"/> falls through without branching when the <c>decree</c>
    /// value is <c>verity</c>.
    /// </summary>
    [Fact]
    public void Emit_SailUnlike_ValueVerity_FallsThroughWithoutBranching()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("Boolean"), UtopIRType.Decree),
            new AppointInstruction(new("Boolean"), new LiteralOperand(true)),
            new SailUnlikeInstruction(new VariableOperand(new("Boolean")), new UtopIRLabel("IS_UNLIKE")),
            new FindInstruction(new LiteralOperand(0)),
            new LabelInstruction(new UtopIRLabel("IS_UNLIKE")),
            new FindInstruction(new LiteralOperand(1))
        ]);

        RunProgram(program).ShouldBe(0);
    }

    /// <summary>
    /// Tests that a branch instruction referencing a label with no matching <see cref="LabelInstruction"/>
    /// anywhere in the programme throws <see cref="InvalidOperationException"/> naming the missing label,
    /// rather than surfacing an opaque CLR metadata exception.
    /// </summary>
    [Fact]
    public void Emit_SailToUndeclaredLabel_ThrowsInvalidOperationException()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        UtopIRProgram program = new([
            new SailInstruction(new UtopIRLabel("NOWHERE")),
            new FindInstruction(new LiteralOperand(0))
        ]);

        try
        {
            Should.Throw<InvalidOperationException>(() => new CilEmitter().Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library)));
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
    /// <remarks>
    /// <see cref="UtopIRType.Array"/> and <see cref="UtopIRType.Pointer"/> are marker values only and are
    /// never valid operands to the bare <c>welcome</c> instruction (arrays and pointers are declared via
    /// the dedicated <c>welcome.list</c>/<c>welcome.gallerypic</c> instructions instead), so they remain
    /// genuinely unsupported here even though <c>yarn</c> (formerly the only case) is now supported.
    /// </remarks>
    /// <param name="type">The unsupported <see cref="UtopIRType"/> to declare.</param>
    [Theory]
    [InlineData(UtopIRType.Array)]
    [InlineData(UtopIRType.Pointer)]
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
    /// Tests that a standalone <c>summon</c> calls the injected external function with the
    /// <c>prentice</c>-pushed argument.
    /// </summary>
    [Fact]
    public void Emit_Summon_CallsExternalVoidFunction()
    {
        TestHostService.ClearLog();
        MethodInfo method = typeof(TestSummonTargets).GetMethod(nameof(TestSummonTargets.WriteText))!;
        CilExternalFunction function = new("WriteText", method, [typeof(string)], null, [typeof(TestHostService)]);
        CilHostInjectedService service = new(typeof(TestHostService), typeof(TestHostService).GetConstructor(Type.EmptyTypes)!);

        UtopIRProgram program = new([
            new PrenticeInstruction(new LiteralOperand("hello")),
            new SummonInstruction(new FunctionReference("WriteText")),
            new FindInstruction(null)
        ]);

        int exitCode = RunProgramWithExternalFunctions(program, [function], [service]);

        exitCode.ShouldBe(0);
        TestHostService.Log.ShouldBe(["hello"]);
    }

    /// <summary>
    /// Tests that <c>summon.find</c> stores the external function return value into the target
    /// register, usable by a subsequent <see cref="FindInstruction"/>.
    /// </summary>
    [Fact]
    public void Emit_SummonFind_StoresReturnValueUsableByFind()
    {
        MethodInfo method = typeof(TestSummonTargets).GetMethod(nameof(TestSummonTargets.Double))!;
        CilExternalFunction function = new("Double", method, [typeof(int)], typeof(int), []);

        UtopIRProgram program = new([
            new PrenticeInstruction(new LiteralOperand(21)),
            new SummonFindInstruction(new("result"), new FunctionReference("Double")),
            new FindInstruction(new VariableOperand(new("result")))
        ]);

        RunProgramWithExternalFunctions(program, [function], []).ShouldBe(42);
    }

    /// <summary>
    /// Tests that a bare <c>summon</c> of a value-returning function pops the unused result rather than
    /// leaving it on the evaluation stack.
    /// </summary>
    [Fact]
    public void Emit_Summon_OfValueReturningFunctionAsVoidStatement_PopsResultAndDoesNotCrash()
    {
        MethodInfo method = typeof(TestSummonTargets).GetMethod(nameof(TestSummonTargets.Double))!;
        CilExternalFunction function = new("Double", method, [typeof(int)], typeof(int), []);

        UtopIRProgram program = new([
            new PrenticeInstruction(new LiteralOperand(21)),
            new SummonInstruction(new FunctionReference("Double")),
            new FindInstruction(new LiteralOperand(7))
        ]);

        RunProgramWithExternalFunctions(program, [function], []).ShouldBe(7);
    }

    /// <summary>
    /// Tests that a host-injected service is constructed once and reused across every subsequent
    /// <c>summon</c> in the same programme that needs it.
    /// </summary>
    [Fact]
    public void Emit_MultipleSummonCallsNeedingHostInjectedService_ConstructsServiceOnlyOnce()
    {
        TestHostService.ClearLog();
        MethodInfo method = typeof(TestSummonTargets).GetMethod(nameof(TestSummonTargets.WriteText))!;
        CilExternalFunction function = new("WriteText", method, [typeof(string)], null, [typeof(TestHostService)]);
        CilHostInjectedService service = new(typeof(TestHostService), typeof(TestHostService).GetConstructor(Type.EmptyTypes)!);

        UtopIRProgram program = new([
            new PrenticeInstruction(new LiteralOperand("first")),
            new SummonInstruction(new FunctionReference("WriteText")),
            new PrenticeInstruction(new LiteralOperand("second")),
            new SummonInstruction(new FunctionReference("WriteText")),
            new FindInstruction(null)
        ]);

        RunProgramWithExternalFunctions(program, [function], [service]);

        TestHostService.Log.ShouldBe(["first", "second"]);
        TestHostService.ConstructionCount.ShouldBe(1);
    }

    /// <summary>
    /// Tests that <c>summon</c> of a function absent from the injected external function set throws a
    /// clear <see cref="InvalidOperationException"/> naming the offending function, rather than an
    /// opaque failure deep inside emission.
    /// </summary>
    [Fact]
    public void Emit_SummonUnknownFunction_ThrowsInvalidOperationException()
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        UtopIRProgram program = new([
            new SummonInstruction(new FunctionReference("DoesNotExist")),
            new FindInstruction(null)
        ]);

        try
        {
            Should.Throw<InvalidOperationException>(() => new CilEmitter().Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library)));
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
    /// Emits and runs a programme with the given external functions and host-injected services,
    /// returning the exit code.
    /// </summary>
    /// <param name="program">The UtopIR programme to emit.</param>
    /// <param name="externalFunctions">The external functions callable via <c>summon</c>/<c>summon.find</c>.</param>
    /// <param name="hostInjectedServices">The host-injected services available to those functions.</param>
    /// <returns>The exit code returned by the programme.</returns>
    private static int RunProgramWithExternalFunctions(
        UtopIRProgram program,
        IReadOnlyList<CilExternalFunction> externalFunctions,
        IReadOnlyList<CilHostInjectedService> hostInjectedServices)
    {
        string assemblyName = $"TopsyTurvyCilTest_{Guid.NewGuid():N}";
        string outputPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        try
        {
            CilEmitter emitter = new();
            emitter.Emit(program, new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Library, externalFunctions, hostInjectedServices));

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
    /// Tests that <c>were</c> converts <c>peer</c> to <c>fathom</c> by emitting <c>conv.r8</c>.
    /// </summary>
    [Fact]
    public void Emit_Were_ConvertsPeerToFathom_EmitsConvR8()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Peer),
            new WelcomeInstruction(new("b"), UtopIRType.Fathom),
            new AppointInstruction(new("a"), new LiteralOperand(7)),
            new WereInstruction(new("b"), new VariableOperand(new("a")), UtopIRType.Fathom),
            new FindInstruction(new VariableOperand(new("b")))
        ]);

        (int exitCode, string ilSource) = RunProgramWithIlSource(program);

        ilSource.ShouldContain("conv.r8");
        exitCode.ShouldBe(7);
    }

    /// <summary>
    /// Tests that <c>were</c> narrows <c>fathom</c> to <c>peer</c> by truncating toward zero via <c>conv.i4</c>, matching the Topsy Turvy runtime cast behaviour.
    /// </summary>
    [Fact]
    public void Emit_Were_NarrowsFathomToPeer_TruncatesTowardZero()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Fathom),
            new WelcomeInstruction(new("b"), UtopIRType.Peer),
            new AppointInstruction(new("a"), new LiteralOperand(42.9)),
            new WereInstruction(new("b"), new VariableOperand(new("a")), UtopIRType.Peer),
            new FindInstruction(new VariableOperand(new("b")))
        ]);

        (int exitCode, string ilSource) = RunProgramWithIlSource(program);

        ilSource.ShouldContain("conv.i4");
        exitCode.ShouldBe(42);
    }

    /// <summary>
    /// Tests that <c>were</c> narrows <c>fathom</c> to <c>foot</c> by emitting <c>conv.r4</c>.
    /// </summary>
    [Fact]
    public void Emit_Were_NarrowsFathomToFoot_EmitsConvR4()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("a"), UtopIRType.Fathom),
            new WelcomeInstruction(new("b"), UtopIRType.Foot),
            new AppointInstruction(new("a"), new LiteralOperand(8.5)),
            new WereInstruction(new("b"), new VariableOperand(new("a")), UtopIRType.Foot),
            new FindInstruction(new VariableOperand(new("b")))
        ]);

        (int exitCode, string ilSource) = RunProgramWithIlSource(program);

        ilSource.ShouldContain("conv.r4");
        exitCode.ShouldBe(8);
    }

    /// <summary>
    /// Tests that the <c>stitch</c> type stores a character literal and returns its code point as the exit code.
    /// </summary>
    [Fact]
    public void Emit_Stitch_StoresCharacterAndReturnsCodePoint()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("c"), UtopIRType.Stitch),
            new AppointInstruction(new("c"), new LiteralOperand('A')),
            new FindInstruction(new VariableOperand(new("c")))
        ]);

        RunProgram(program).ShouldBe(65);
    }

    /// <summary>
    /// Tests that <c>were</c> converts <c>peer</c> to <c>stitch</c> through the code point via <c>conv.u2</c>.
    /// </summary>
    [Fact]
    public void Emit_Were_ConvertsPeerToStitch_EmitsConvU2()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("n"), UtopIRType.Peer),
            new WelcomeInstruction(new("c"), UtopIRType.Stitch),
            new AppointInstruction(new("n"), new LiteralOperand(66)),
            new WereInstruction(new("c"), new VariableOperand(new("n")), UtopIRType.Stitch),
            new FindInstruction(new VariableOperand(new("c")))
        ]);

        (int exitCode, string ilSource) = RunProgramWithIlSource(program);

        ilSource.ShouldContain("conv.u2");
        exitCode.ShouldBe(66);
    }

    /// <summary>
    /// Tests that <c>were</c> converts <c>stitch</c> to <c>peer</c> yielding the character code point.
    /// </summary>
    [Fact]
    public void Emit_Were_ConvertsStitchToPeer_YieldsCodePoint()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("c"), UtopIRType.Stitch),
            new WelcomeInstruction(new("n"), UtopIRType.Peer),
            new AppointInstruction(new("c"), new LiteralOperand('Z')),
            new WereInstruction(new("n"), new VariableOperand(new("c")), UtopIRType.Peer),
            new FindInstruction(new VariableOperand(new("n")))
        ]);

        RunProgram(program).ShouldBe(90);
    }

    /// <summary>
    /// Tests that <c>were</c> converts <c>stitch</c> to <c>fathom</c> through the code point.
    /// </summary>
    [Fact]
    public void Emit_Were_ConvertsStitchToFathom_YieldsCodePoint()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("c"), UtopIRType.Stitch),
            new WelcomeInstruction(new("d"), UtopIRType.Fathom),
            new AppointInstruction(new("c"), new LiteralOperand('A')),
            new WereInstruction(new("d"), new VariableOperand(new("c")), UtopIRType.Fathom),
            new FindInstruction(new VariableOperand(new("d")))
        ]);

        RunProgram(program).ShouldBe(65);
    }

    /// <summary>
    /// Tests that <c>were</c> converts <c>fathom</c> to <c>stitch</c> by truncating toward zero then narrowing to the code point.
    /// </summary>
    [Fact]
    public void Emit_Were_ConvertsFathomToStitch_TruncatesToCodePoint()
    {
        UtopIRProgram program = new([
            new WelcomeInstruction(new("d"), UtopIRType.Fathom),
            new WelcomeInstruction(new("c"), UtopIRType.Stitch),
            new AppointInstruction(new("d"), new LiteralOperand(65.9)),
            new WereInstruction(new("c"), new VariableOperand(new("d")), UtopIRType.Stitch),
            new FindInstruction(new VariableOperand(new("c")))
        ]);

        RunProgram(program).ShouldBe(65);
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
