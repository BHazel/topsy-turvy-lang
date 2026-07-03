using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

/// <summary>
/// Emits a .NET assembly from a <see cref="UtopIRProgram"/> using <see cref="PersistedAssemblyBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// ### Assembly Structure
/// The emitter wraps the UtopIR instruction sequence in the minimum required .NET assembly structure:
/// * <c>AssemblyBuilder</c>: Sets the CLR assembly identity.
/// * <c>ModuleBuilder</c>: Creates a single module inside the assembly.
/// * <c>TypeBuilder</c>: Defines a public static class named "Opera" to hold the entry point.  A C#
///   <c>static class</c> compiles to exactly <c>TypeAttributes.Abstract | TypeAttributes.Sealed</c>,
///   the CLR way of forbidding instantiation, which is correct here since <c>Main</c> is static
///   and never needs an instance.
/// * <c>MethodBuilder</c>: Defines the public static <c>int Main(string[] args)</c> method serving as
///   the programme entry point.
/// </para>
/// <para>
/// ### Local Variables
/// UtopIR virtual registers (<c>£name</c>) are lowered to CIL <see cref="LocalBuilder"/> locals tracked
/// in a <c>Dictionary&lt;string, LocalBuilder&gt;</c> keyed on the variable name without the <c>£</c> prefix.
/// Every <see cref="ILGenerator.DeclareLocal(Type)"/> call is paired with a
/// <see cref="CilGenerator.RegisterLocalName"/> call so the name for a UtopIR local can be recovered later
/// when producing IL source text: .NET metadata alone does not retain local names.
/// </para>
/// <para>
/// ### Output Kinds
/// The emitter can be configured to produce different kinds of outputs, set in <see cref="CilEmitOptions"/> to constants
/// defined in <see cref="CilOutputKind"/>:
/// </para>
/// <para>
/// * <see cref="CilOutputKind"/><c>.Library</c>: Saves the assembly with no CLR entry point set, via
///   <see cref="PersistedAssemblyBuilder.Save(string)"/>, suitable for loading with reflection.
/// * <see cref="CilOutputKind"/><c>.Executable</c>: Generates metadata explicitly and builds the PE with
///   <see cref="ManagedPEBuilder"/>, registering <c>Opera.Main</c> as the CLR entry point and writing
///   a companion <c>runtimeconfig.json</c> so the output can be launched with <c>dotnet &lt;path&gt;</c>.
/// * <see cref="CilOutputKind"/><c>.IlSourceOnly</c>: Nothing is written to <see cref="CilEmitOptions.OutputPath"/>,
///   which is ignored, and only the IL text is returned in <see cref="CilEmitResult.IlSource"/>.
/// </para>
/// <para>
/// ### IL Source
/// Regardless of <see cref="CilOutputKind"/>, the type is always finalised and the assembly is always
/// serialized to an in-memory stream.  This in-memory copy is what feeds <see cref="CilGenerator.ToIlText"/>,
/// which disassembles the real, already-persisted bytes using _AsmResolver_ rather than reconstructing text
/// by hand.  This means an assembly is always built internally, even in <c>IlSourceOnly</c> mode: it is
/// simply never written to disk or returned to the caller in that mode.
/// </para>
/// <para>
/// ### Supported Types
/// All integer types are supported in v0.0.1-preview1.  Floating-point, boolean, character and
/// string types are not currently supported.
/// </para>
/// </remarks>
public sealed class CilEmitter
{
    /// <summary>
    /// Emits the given UtopIR programme as a .NET assembly using the provided options.
    /// </summary>
    /// <param name="program">The UtopIR programme to emit.</param>
    /// <param name="options">The CLI emitter options.</param>
    /// <returns>A <see cref="CilEmitResult"/> carrying the readable IL listing for the emitted method body.</returns>
    /// <exception cref="NotSupportedException">Thrown when the programme contains a type or literal that is not supported by the emitter in this version.</exception>
    public CilEmitResult Emit(UtopIRProgram program, CilEmitOptions options)
    {
        AssemblyName assemblyName = new(options.AssemblyName);
        PersistedAssemblyBuilder assemblyBuilder = new(assemblyName, typeof(object).Assembly);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(options.AssemblyName);

        TypeBuilder typeBuilder = moduleBuilder.DefineType(
            name: "Opera",
            attr: TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);

        MethodBuilder mainMethod = typeBuilder.DefineMethod(
            name: "Main",
            attributes: MethodAttributes.Public | MethodAttributes.Static,
            returnType: typeof(int),
            parameterTypes: [typeof(string[])]);

        // .NET metadata does not retain parameter names by default (mirroring the same gap for locals
        // — see CilGenerator.RegisterLocalName) — DefineParameter records it so CilGenerator.ToIlText
        // can recover "args" instead of falling back to a generic name when disassembling.
        mainMethod.DefineParameter(1, ParameterAttributes.None, "args");

        ILGenerator ilGenerator = mainMethod.GetILGenerator();
        CilGenerator cilGenerator = new();

        Dictionary<string, LocalBuilder> locals = [];
        Dictionary<string, UtopIRType> localTypes = [];

        bool hasReturn = false;
        foreach (UtopIRInstruction instruction in program.Instructions)
        {
            if (instruction is FindInstruction)
            {
                hasReturn = true;
            }

            this.EmitInstruction(instruction, ilGenerator, cilGenerator, locals, localTypes);
        }

        if (!hasReturn)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Ret);
        }

        typeBuilder.CreateType();

        // PersistedAssemblyBuilder throws "Cannot populate assembly metadata multiple times" if its
        // metadata is generated more than once, so the PE bytes must be produced exactly once here
        // and reused both for disassembly, via CilGenerator.ToIlText, and for the on-disk output,
        // never a second, separate Save()/GenerateMetadata() call.
        byte[] assemblyBytes = options.OutputKind == CilOutputKind.Executable
            ? this.BuildExecutablePeBytes(assemblyBuilder, mainMethod)
            : this.BuildLibraryPeBytes(assemblyBuilder);

        if (options.OutputKind == CilOutputKind.Executable)
        {
            File.WriteAllBytes(options.OutputPath, assemblyBytes);
            this.WriteRuntimeConfig(options.OutputPath);
        }
        else if (options.OutputKind == CilOutputKind.Library)
        {
            File.WriteAllBytes(options.OutputPath, assemblyBytes);
        }

        return new(cilGenerator.ToIlText(assemblyBytes));
    }

    /// <summary>
    /// Saves the assembly as a .NET library with no entry point.
    /// </summary>
    /// <param name="assemblyBuilder">The assembly builder to serialise.</param>
    /// <returns>The full PE as a byte array.</returns>
    private byte[] BuildLibraryPeBytes(PersistedAssemblyBuilder assemblyBuilder)
    {
        using MemoryStream stream = new();
        assemblyBuilder.Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Saves the assembly as a .NET executable with an entry point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The PE cannot be executed directly and must be run via the .NET toolchain, such as:
    /// </para>
    /// <code>
    /// dotnet programme.dll
    /// </code>
    /// <para>
    /// The process of setting the entry point follows:
    /// * Metadata is explicitly generated by calling <see cref="PersistedAssemblyBuilder.GenerateMetadata(out BlobBuilder, out BlobBuilder)"/>.
    /// * The <see cref="ManagedPEBuilder"/> is constructed with the entry point token obtained from the <c>Main</c> method <see cref="MethodBuilder.MetadataToken"/>.
    /// </para>
    /// </remarks>
    /// <param name="assemblyBuilder">The assembly builder to serialise.</param>
    /// <param name="mainMethod">The method to register as the CLR entry point.</param>
    /// <returns>The full PE as a byte array.</returns>
    private byte[] BuildExecutablePeBytes(PersistedAssemblyBuilder assemblyBuilder, MethodBuilder mainMethod)
    {
        MetadataBuilder metadataBuilder = assemblyBuilder.GenerateMetadata(out BlobBuilder ilStream, out BlobBuilder fieldData);

        ManagedPEBuilder peBuilder = new(
            header: PEHeaderBuilder.CreateExecutableHeader(),
            metadataRootBuilder: new(metadataBuilder),
            ilStream: ilStream,
            mappedFieldData: fieldData,
            entryPoint: MetadataTokens.MethodDefinitionHandle(mainMethod.MetadataToken));

        BlobBuilder peBlob = new();
        peBuilder.Serialize(peBlob);

        using MemoryStream stream = new();
        peBlob.WriteContentTo(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Writes a minimal <c>runtimeconfig.json</c> file alongside the saved assembly so the emitted
    /// executable can be launched with <c>dotnet &lt;path&gt;</c> as a framework-dependent app.
    /// </summary>
    /// <remarks>
    /// The file is saved with the same name as the assembly with a <c>.runtimeconfig.json</c> extension.
    /// </remarks>
    /// <param name="outputPath">The path of the emitted PE.</param>
    private void WriteRuntimeConfig(string outputPath)
    {
        string runtimeConfigurationPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
        string frameworkVersion = Environment.Version.ToString(3);
        string json = $$"""
            {
              "runtimeOptions": {
                "tfm": "net10.0",
                "framework": {
                  "name": "Microsoft.NETCore.App",
                  "version": "{{frameworkVersion}}"
                }
              }
            }
            """;

        File.WriteAllText(runtimeConfigurationPath, json);
    }

    /// <summary>
    /// Dispatches a single UtopIR instruction to the appropriate emit method.
    /// </summary>
    /// <param name="instruction">The instruction to emit.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record UtopIR names for locals declared while emitting this instruction.</param>
    /// <param name="locals">The map from variable name (without <c>£</c>) to its <see cref="LocalBuilder"/> slot.</param>
    /// <param name="localTypes">The map from variable name to its <see cref="UtopIRType"/> for type-context lookups.</param>
    private void EmitInstruction(
        UtopIRInstruction instruction,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes)
    {
        switch (instruction)
        {
            case WelcomeInstruction welcome:
                this.EmitWelcome(welcome, ilGenerator, cilGenerator, locals, localTypes);
                break;
            case AppointInstruction appoint:
                this.EmitAppoint(appoint, ilGenerator, locals);
                break;
            case ArithmeticInstruction arithmetic:
                this.EmitArithmetic(arithmetic, ilGenerator, cilGenerator, locals, localTypes);
                break;
            case PrenticeInstruction prentice:
                this.EmitPrentice(prentice, ilGenerator, locals);
                break;
            case LeaveInstruction leave:
                this.EmitLeave(leave, ilGenerator, locals);
                break;
            case FindInstruction find:
                this.EmitFind(find, ilGenerator, locals, localTypes);
                break;
            case WereInstruction were:
                this.EmitWere(were, ilGenerator, cilGenerator, locals, localTypes);
                break;
        }
    }

    /// <summary>
    /// Declares a CIL local variable for a <see cref="WelcomeInstruction"/>.
    /// </summary>
    /// <param name="welcome">The welcome instruction declaring the variable.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record the UtopIR name for the newly declared local.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    private void EmitWelcome(
        WelcomeInstruction welcome,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes)
    {
        Type clrType = this.MapToClrType(welcome.Type);
        LocalBuilder local = ilGenerator.DeclareLocal(clrType);
        cilGenerator.RegisterLocalName(local, welcome.Target.Name);
        locals[welcome.Target.Name] = local;
        localTypes[welcome.Target.Name] = welcome.Type;
    }

    /// <summary>
    /// Emits CIL for an <see cref="AppointInstruction"/> by loading the value operand then storing it
    /// into the target local.
    /// </summary>
    /// <param name="appoint">The appoint instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    private void EmitAppoint(
        AppointInstruction appoint,
        ILGenerator ilGenerator,
        Dictionary<string, LocalBuilder> locals)
    {
        LocalBuilder target = locals[appoint.Target.Name];
        this.EmitStackLoadOperand(appoint.Value, ilGenerator, locals);
        ilGenerator.Emit(OpCodes.Stloc, target);
    }

    /// <summary>
    /// Emits CIL for an <see cref="ArithmeticInstruction"/>.
    /// </summary>
    /// <remarks>
    /// The Transformer guarantees both operands always share the same type by this point by inserting
    /// a <see cref="WereInstruction"/> to widen the narrower operand beforehand when they originally
    /// differ: this is validated defensively here rather than trusted blindly, in case a future or
    /// hand-built <see cref="ArithmeticInstruction"/> reaches the emitter without going through the
    /// Transformer.
    /// </remarks>
    /// <param name="arithmeticInstruction">The arithmetic instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record the UtopIR name for the newly declared local.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when the two operands have different inferred types.</exception>
    private void EmitArithmetic(
        ArithmeticInstruction arithmeticInstruction,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes)
    {
        UtopIRType operand1Type = this.InferOperandType(arithmeticInstruction.Operand1, localTypes);
        UtopIRType operand2Type = this.InferOperandType(arithmeticInstruction.Operand2, localTypes);
        if (operand1Type != operand2Type)
        {
            throw new InvalidOperationException(
                $"Arithmetic instruction targeting '£{arithmeticInstruction.Target.Name}' has mismatched " +
                $"operand types ('{operand1Type}' and '{operand2Type}').");
        }

        // If the target local has not yet been declared, declare it using the operand type.
        // Arithmetic instructions in UtopIR can be thought of as "auto"-declaring the
        // target register.
        if (!locals.TryGetValue(arithmeticInstruction.Target.Name, out LocalBuilder? declaredLocal))
        {
            declaredLocal = ilGenerator.DeclareLocal(this.MapToClrType(operand1Type));
            cilGenerator.RegisterLocalName(declaredLocal, arithmeticInstruction.Target.Name);
            locals[arithmeticInstruction.Target.Name] = declaredLocal;
            localTypes[arithmeticInstruction.Target.Name] = operand1Type;
        }

        LocalBuilder target = declaredLocal;
        UtopIRType targetType = localTypes[arithmeticInstruction.Target.Name];

        if (arithmeticInstruction.Operation is UtopIRArithmeticOperation.Max or UtopIRArithmeticOperation.Min)
        {
            this.EmitMaxMin(arithmeticInstruction.Operation, arithmeticInstruction.Operand1, arithmeticInstruction.Operand2, targetType, ilGenerator, locals);
        }
        else
        {
            this.EmitStackLoadOperand(arithmeticInstruction.Operand1, ilGenerator, locals);
            this.EmitStackLoadOperand(arithmeticInstruction.Operand2, ilGenerator, locals);
            this.EmitArithmeticOpcode(arithmeticInstruction.Operation, targetType, ilGenerator);
        }

        ilGenerator.Emit(OpCodes.Stloc, target);
    }

    /// <summary>
    /// Emits CIL for <c>max</c> or <c>min</c> by calling <c>Math.Max</c> or
    /// <c>Math.Min</c> respectively.
    /// </summary>
    /// <remarks>
    /// Narrow integer types (<c>short</c>, <c>sbyte</c>, <c>ushort</c>, <c>byte</c>) require an
    /// explicit narrowing conversion before the call because the CIL evaluation stack always widens
    /// them to <c>int32</c>.
    /// </remarks>
    /// <param name="arithmeticOperations">The arithmetic operations; must be <see cref="UtopIRArithmeticOperation.Max"/> or <see cref="UtopIRArithmeticOperation.Min"/>.</param>
    /// <param name="operand1">The first operand.</param>
    /// <param name="operand2">The second operand.</param>
    /// <param name="type">The type of the target register, used to resolve the correct <see cref="Math"/> overload.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    private void EmitMaxMin(
        UtopIRArithmeticOperation arithmeticOperations,
        UtopIROperand operand1,
        UtopIROperand operand2,
        UtopIRType type,
        ILGenerator ilGenerator,
        Dictionary<string, LocalBuilder> locals)
    {
        Type clrType = this.MapToClrType(type);
        string methodName = arithmeticOperations == UtopIRArithmeticOperation.Max ? "Max" : "Min";

        this.EmitStackLoadOperand(operand1, ilGenerator, locals);
        this.EmitConversion(type, ilGenerator);
        this.EmitStackLoadOperand(operand2, ilGenerator, locals);
        this.EmitConversion(type, ilGenerator);

        MethodInfo mathMethod = typeof(Math).GetMethod(methodName, [clrType, clrType])
            ?? throw new InvalidOperationException(
                $"Math.{methodName}({clrType.Name}, {clrType.Name}) could not be resolved.");

        ilGenerator.Emit(OpCodes.Call, mathMethod);
    }

    /// <summary>
    /// Emits CIL for a <see cref="PrenticeInstruction"/> pushing the operand value onto the
    /// evaluation stack.
    /// </summary>
    /// <param name="prentice">The prentice instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    private void EmitPrentice(
        PrenticeInstruction prentice,
        ILGenerator ilGenerator,
        Dictionary<string, LocalBuilder> locals)
    {
        this.EmitStackLoadOperand(prentice.Value, ilGenerator, locals);
    }

    /// <summary>
    /// Emits CIL for a <see cref="LeaveInstruction"/> popping the top of the evaluation stack into
    /// the target local.
    /// </summary>
    /// <param name="leave">The leave instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    private void EmitLeave(
        LeaveInstruction leave,
        ILGenerator ilGenerator,
        Dictionary<string, LocalBuilder> locals)
    {
        ilGenerator.Emit(OpCodes.Stloc, locals[leave.Target.Name]);
    }

    /// <summary>
    /// Emits CIL for a <see cref="WereInstruction"/> by loading the source operand, converting it to
    /// the declared destination type and storing it into the target local.
    /// </summary>
    /// <param name="were">The were instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record the UtopIR name for an auto-declared target local.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    private void EmitWere(
        WereInstruction were,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes)
    {
        this.EmitStackLoadOperand(were.Value, ilGenerator, locals);
        this.EmitConversion(were.Type, ilGenerator);

        if (!locals.TryGetValue(were.Target.Name, out LocalBuilder? target))
        {
            target = ilGenerator.DeclareLocal(this.MapToClrType(were.Type));
            cilGenerator.RegisterLocalName(target, were.Target.Name);
            locals[were.Target.Name] = target;
        }

        localTypes[were.Target.Name] = were.Type;
        ilGenerator.Emit(OpCodes.Stloc, target);
    }

    /// <summary>
    /// Emits CIL for a <see cref="FindInstruction"/> loading the return value or 0 for a void return.
    /// </summary>
    /// <remarks>
    /// <c>Main</c> always returns <c>int32</c>.  64-bit values are narrowed to <c>int32</c> via <c>conv.i4</c>.
    /// </remarks>
    /// <param name="find">The find instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    private void EmitFind(
        FindInstruction find,
        ILGenerator ilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes)
    {
        if (find.Value is null)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
        }
        else if (find.Value is VariableOperand variableOperand)
        {
            ilGenerator.Emit(OpCodes.Ldloc, locals[variableOperand.Variable.Name]);
            if (this.Is64BitType(localTypes[variableOperand.Variable.Name]))
            {
                ilGenerator.Emit(OpCodes.Conv_I4);
            }
        }
        else if (find.Value is LiteralOperand literalOperand)
        {
            this.EmitLoadLiteralValue(literalOperand.Value, ilGenerator);
            if (literalOperand.Value is long or ulong)
            {
                ilGenerator.Emit(OpCodes.Conv_I4);
            }
        }

        ilGenerator.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// Loads a <see cref="UtopIROperand"/> onto the CIL evaluation stack.
    /// </summary>
    /// <remarks>
    /// For a <see cref="LiteralOperand"/>, the <c>ldc</c> opcode is inferred directly from the
    /// literal own CLR runtime type (see <see cref="EmitLoadLiteralValue"/>): the target
    /// register declared <see cref="UtopIRType"/> is not needed here.
    /// </remarks>
    /// <param name="operand">The operand to load.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    private void EmitStackLoadOperand(
        UtopIROperand operand,
        ILGenerator ilGenerator,
        Dictionary<string, LocalBuilder> locals)
    {
        switch (operand)
        {
            case VariableOperand variableOperand:
                ilGenerator.Emit(OpCodes.Ldloc, locals[variableOperand.Variable.Name]);
                break;
            case LiteralOperand literalOperand:
                this.EmitLoadLiteralValue(literalOperand.Value, ilGenerator);
                break;
        }
    }

    /// <summary>
    /// Emits the appropriate <c>ldc</c> opcode for a compile-time literal value, inferring the
    /// opcode from the CLR type of the value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The CIL evaluation stack has no distinct unsigned <c>int32</c>/<c>int64</c> type: <c>ldc.i4</c>
    /// and <c>ldc.i8</c> simply push a raw 4-/8-byte bit pattern.  For <c>byte</c> and <c>ushort</c>,
    /// the <c>(int)</c> cast is a genuine zero-extending widen always producing a positive, correct
    /// value, e.g. <c>byte</c> <c>255</c> becomes <c>int</c> <c>255</c>.  For <c>uint</c> and
    /// <c>ulong</c>, the cast is a same-width bit-reinterpret, e.g. <c>uint</c> <c>4000000000</c>
    /// becomes the negative-looking <c>int</c> <c>-294967296</c>.  This looks alarming printed as a
    /// signed number, but the bit pattern is preserved exactly and is reinterpreted correctly once
    /// <c>stloc</c> stores it into a local declared with the true unsigned CLR type (see
    /// <see cref="MapToClrType"/>).  This mirrors the same principle documented on
    /// <see cref="EmitArithmeticOpcode"/> for <c>div.un</c>/<c>rem.un</c>.
    /// </para>
    /// </remarks>
    /// <param name="value">The literal value to load.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <exception cref="NotSupportedException">Thrown when the value CLR type is not supported in this version.</exception>
    private void EmitLoadLiteralValue(object value, ILGenerator ilGenerator)
    {
        switch (value)
        {
            case int int32:
                ilGenerator.Emit(OpCodes.Ldc_I4, int32);
                break;
            case long int64:
                ilGenerator.Emit(OpCodes.Ldc_I8, int64);
                break;
            case short int16:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)int16);
                break;
            case sbyte int8:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)int8);
                break;
            case uint uint32:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)uint32);
                break;
            case ulong uint64:
                ilGenerator.Emit(OpCodes.Ldc_I8, (long)uint64);
                break;
            case ushort uint16:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)uint16);
                break;
            case byte uint8:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)uint8);
                break;
            default:
                throw new NotSupportedException(
                    $"Literal value of CLR type '{value.GetType().Name}' is not supported by the CIL emitter in this version.");
        }
    }

    /// <summary>
    /// Emits the CIL arithmetic opcode corresponding to the given <see cref="UtopIRArithmeticOperation"/>.
    /// </summary>
    /// <remarks>
    /// Division and remainder use <c>div.un</c> and <c>rem.un</c> for unsigned types to preserve
    /// correct semantics.  All other operations use their signed equivalents which are bit-identical
    /// for two's-complement addition, subtraction and multiplication.
    /// </remarks>
    /// <param name="operation">The arithmetic operation.</param>
    /// <param name="type">The target variable type used to select signed vs unsigned division and remainder opcodes.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <exception cref="InvalidOperationException">Thrown when called with <c>Max</c> or <c>Min</c> as those are handled separately via <see cref="EmitMaxMin"/>.</exception>
    private void EmitArithmeticOpcode(UtopIRArithmeticOperation operation, UtopIRType type, ILGenerator ilGenerator)
    {
        bool isUnsigned = this.IsUnsignedType(type);
        switch (operation)
        {
            case UtopIRArithmeticOperation.Sum:
                ilGenerator.Emit(OpCodes.Add);
                break;
            case UtopIRArithmeticOperation.Diff:
                ilGenerator.Emit(OpCodes.Sub);
                break;
            case UtopIRArithmeticOperation.Prod:
                ilGenerator.Emit(OpCodes.Mul);
                break;
            case UtopIRArithmeticOperation.Quot:
                ilGenerator.Emit(isUnsigned
                    ? OpCodes.Div_Un
                    : OpCodes.Div);
                break;
            case UtopIRArithmeticOperation.Rem:
                ilGenerator.Emit(isUnsigned
                    ? OpCodes.Rem_Un
                    : OpCodes.Rem);
                break;
            case UtopIRArithmeticOperation.Max or UtopIRArithmeticOperation.Min:
                throw new InvalidOperationException($"Operation '{operation}' must be handled by EmitMaxMin.");
            default:
                throw new InvalidOperationException($"Operation '{operation}' not supported.");
        }
    }

    /// <summary>
    /// Emits the <c>conv.*</c> opcode that converts whatever is currently on the CIL evaluation stack
    /// to the given target type true CLR width and signedness.
    /// </summary>
    /// <remarks>
    /// Covers all integer <see cref="UtopIRType"/> variants.  A single target-only conversion is
    /// sufficient regardless of the original type of the value.  <c>conv.*</c> opcodes convert whatever is on
    /// the stack so this also serves the <see cref="EmitWere"/> general widen/narrow cast between any
    /// pair of integer types, not just the narrowing case <see cref="EmitMaxMin"/> uses.
    /// </remarks>
    /// <param name="targetType">The UtopIR type to convert the top-of-stack value to.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="targetType"/> has no supported conversion in this version.</exception>
    private void EmitConversion(UtopIRType targetType, ILGenerator ilGenerator)
    {
        OpCode opcode = targetType switch
        {
            UtopIRType.Chancellor => OpCodes.Conv_I8,
            UtopIRType.Peer => OpCodes.Conv_I4,
            UtopIRType.Pirate => OpCodes.Conv_I2,
            UtopIRType.SausageRoll => OpCodes.Conv_I1,
            UtopIRType.StandingChancellor => OpCodes.Conv_U8,
            UtopIRType.StandingPeer => OpCodes.Conv_U4,
            UtopIRType.StandingPirate => OpCodes.Conv_U2,
            UtopIRType.StandingSausageRoll => OpCodes.Conv_U1,
            _ => throw new NotSupportedException($"UtopIR type '{targetType}' is not supported by the CIL emitter conversion in this version.")
        };

        ilGenerator.Emit(opcode);
    }

    /// <summary>
    /// Determines if the <see cref="UtopIRType"/> is an unsigned integer type.
    /// </summary>
    /// <param name="type">The UtopIR type to test.</param>
    /// <returns><c>true</c> for unsigned integer types, otherwise <c>false</c>.</returns>
    private bool IsUnsignedType(UtopIRType type) =>
        type is UtopIRType.StandingChancellor
            or UtopIRType.StandingPeer
            or UtopIRType.StandingPirate
            or UtopIRType.StandingSausageRoll;

    /// <summary>
    /// Determines if the <see cref="UtopIRType"/> is a 64-bit integer type.
    /// Returns <c>true</c> when the given <see cref="UtopIRType"/> occupies 64 bits on the CIL
    /// evaluation stack and therefore requires <c>conv.i4</c> before a <c>ret</c> from <c>Main</c>.
    /// </summary>
    /// <param name="type">The UtopIR type to test.</param>
    /// <remarks>
    /// 64-bit integer types occupy 64 bits on the CIL evaluation stack and therefore require
    /// conversion to 32-bit via the <c>conv.i4</c> IL opcode before returning from the <c>Main</c> method.
    /// </remarks>
    /// <returns><c>true</c> for 64-bit integer types, otherwise <c>false</c>.</returns>
    private bool Is64BitType(UtopIRType type) =>
        type is UtopIRType.Chancellor or UtopIRType.StandingChancellor;

    /// <summary>
    /// Maps a <see cref="UtopIRType"/> to the corresponding CLR <see cref="Type"/>.
    /// </summary>
    /// <param name="utopirType">The UtopIR type to map.</param>
    /// <returns>The CLR <see cref="Type"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="utopirType"/> has no supported CLR equivalent in this version.</exception>
    private Type MapToClrType(UtopIRType utopirType) => utopirType switch
    {
        UtopIRType.Chancellor => typeof(long),
        UtopIRType.Peer => typeof(int),
        UtopIRType.Pirate => typeof(short),
        UtopIRType.SausageRoll => typeof(sbyte),
        UtopIRType.StandingChancellor => typeof(ulong),
        UtopIRType.StandingPeer => typeof(uint),
        UtopIRType.StandingPirate => typeof(ushort),
        UtopIRType.StandingSausageRoll => typeof(byte),
        _ => throw new NotSupportedException(
            $"UtopIR type '{utopirType}' is not supported by the CIL emitter in this version.")
    };

    /// <summary>
    /// Infers the <see cref="UtopIRType"/> of a <see cref="UtopIROperand"/> used to declare a CIL
    /// local for a UtopIR temporary register that was never given an explicit
    /// <see cref="WelcomeInstruction"/> (please see the remarks on <see cref="EmitArithmetic"/>).
    /// </summary>
    /// <param name="operand">The operand to infer a type for.</param>
    /// <param name="localTypes">The map from variable name to its already-known <see cref="UtopIRType"/>.</param>
    /// <returns>
    /// For a <see cref="VariableOperand"/>, the already-recorded type of the referenced variable.
    /// For a <see cref="LiteralOperand"/>, the type inferred from the literal own CLR runtime type
    /// (the reverse of <see cref="MapToClrType"/>).
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when a <see cref="LiteralOperand"/> CLR runtime type has no corresponding <see cref="UtopIRType"/> in this version.</exception>
    private UtopIRType InferOperandType(UtopIROperand operand, Dictionary<string, UtopIRType> localTypes) => operand switch
    {
        VariableOperand variableOperand => localTypes[variableOperand.Variable.Name],
        LiteralOperand literalOperand => literalOperand.Value switch
        {
            long => UtopIRType.Chancellor,
            int => UtopIRType.Peer,
            short => UtopIRType.Pirate,
            sbyte => UtopIRType.SausageRoll,
            ulong => UtopIRType.StandingChancellor,
            uint => UtopIRType.StandingPeer,
            ushort => UtopIRType.StandingPirate,
            byte => UtopIRType.StandingSausageRoll,
            _ => throw new NotSupportedException(
                $"Literal value of CLR type '{literalOperand.Value.GetType().Name}' has no corresponding UtopIR type in this version.")
        },
        _ => throw new NotSupportedException($"Operand type '{operand.GetType().Name}' is not supported.")
    };
}
