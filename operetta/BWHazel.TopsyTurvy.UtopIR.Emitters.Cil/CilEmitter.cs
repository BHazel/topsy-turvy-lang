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
/// All integer, floating-point and character types are supported as of v0.0.1-preview2.  As of
/// v0.0.1-preview3, the <c>decree</c> (boolean) type is supported as the result of a comparison or
/// logical instruction, and as the operand of a conditional branch but not yet as a <c>were</c>
/// cast source or target (<see cref="EmitConversion"/> has no <c>decree</c> case).  The <c>yarn</c>
/// (string) type is supported for declaration, assignment and <see cref="VictimYarnInstruction"/>
/// character access (see <see cref="EmitVictimYarn"/>).
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
        Dictionary<string, UtopIRType> arrayElementTypes = [];
        Dictionary<string, Label> labels = [];

        this.ValidateLabelReferences(program);

        bool hasReturn = false;
        foreach (UtopIRInstruction instruction in program.Instructions)
        {
            if (instruction is FindInstruction)
            {
                hasReturn = true;
            }

            this.EmitInstruction(instruction, ilGenerator, cilGenerator, locals, localTypes, arrayElementTypes, labels);
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
    /// Validates that every label referenced by a <see cref="SailInstruction"/>,
    /// <see cref="SailAlikeInstruction"/> or <see cref="SailUnlikeInstruction"/> has a matching
    /// <see cref="LabelInstruction"/> declared somewhere in the programme.
    /// </summary>
    /// <remarks>
    /// Without this check, a mistyped label name would surface as an opaque CLR metadata exception
    /// when <see cref="ILGenerator"/> finalises a branch to a <see cref="Label"/> that was never
    /// marked, rather than a clear UtopIR-level error naming the offending instruction.
    /// </remarks>
    /// <param name="program">The UtopIR programme to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when a branch instruction references a label with no matching declaration.</exception>
    private void ValidateLabelReferences(UtopIRProgram program)
    {
        HashSet<string> declaredLabels = [];
        foreach (UtopIRInstruction instruction in program.Instructions)
        {
            if (instruction is LabelInstruction label)
            {
                declaredLabels.Add(label.Name.Name);
            }
        }

        foreach (UtopIRInstruction instruction in program.Instructions)
        {
            string? referencedLabel = instruction switch
            {
                SailInstruction sail => sail.Label.Name,
                SailAlikeInstruction sailAlike => sailAlike.Label.Name,
                SailUnlikeInstruction sailUnlike => sailUnlike.Label.Name,
                _ => null
            };

            if (referencedLabel is not null && !declaredLabels.Contains(referencedLabel))
            {
                throw new InvalidOperationException(
                    $"Branch instruction references undeclared label '!{referencedLabel}'.");
            }
        }
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
    /// <param name="arrayElementTypes">The map from array variable name to its declared element <see cref="UtopIRType"/>.</param>
    /// <param name="labels">The map from label name (without <c>!</c>) to its <see cref="Label"/>, lazily populated via <see cref="GetOrDefineLabel"/>.</param>
    private void EmitInstruction(
        UtopIRInstruction instruction,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes,
        Dictionary<string, UtopIRType> arrayElementTypes,
        Dictionary<string, Label> labels)
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
            case BitwiseInstruction bitwise:
                this.EmitBitwise(bitwise, ilGenerator, cilGenerator, locals, localTypes);
                break;
            case InvInstruction inv:
                this.EmitInv(inv, ilGenerator, cilGenerator, locals, localTypes);
                break;
            case ComparisonInstruction comparison:
                this.EmitComparison(comparison, ilGenerator, cilGenerator, locals, localTypes);
                break;
            case LogicalInstruction logical:
                this.EmitLogical(logical, ilGenerator, cilGenerator, locals, localTypes);
                break;
            case HardlyInstruction hardly:
                this.EmitHardly(hardly, ilGenerator, cilGenerator, locals, localTypes);
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
            case LabelInstruction label:
                this.EmitLabel(label, ilGenerator, labels);
                break;
            case SailInstruction sail:
                this.EmitSail(sail, ilGenerator, labels);
                break;
            case SailAlikeInstruction sailAlike:
                this.EmitSailAlike(sailAlike, ilGenerator, locals, labels);
                break;
            case SailUnlikeInstruction sailUnlike:
                this.EmitSailUnlike(sailUnlike, ilGenerator, locals, labels);
                break;
            case VictimYarnInstruction victimYarn:
                this.EmitVictimYarn(victimYarn, ilGenerator, cilGenerator, locals, localTypes);
                break;
            case WelcomeListInstruction welcomeList:
                this.EmitWelcomeList(welcomeList, ilGenerator, cilGenerator, locals, localTypes, arrayElementTypes);
                break;
            case AppointVictimInstruction appointVictim:
                this.EmitAppointVictim(appointVictim, ilGenerator, locals, arrayElementTypes);
                break;
            case VictimListInstruction victimList:
                this.EmitVictimList(victimList, ilGenerator, cilGenerator, locals, localTypes, arrayElementTypes);
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

        bool isFloatOperation = this.IsFloatOperation(arithmeticInstruction.Operation);
        if (isFloatOperation && !this.IsFloatType(operand1Type))
        {
            throw new InvalidOperationException(
                $"Floating-point arithmetic instruction targeting '£{arithmeticInstruction.Target.Name}' " +
                $"requires floating-point operands but was given '{operand1Type}'.");
        }

        if (!isFloatOperation && this.IsFloatType(operand1Type))
        {
            throw new InvalidOperationException(
                $"Integer arithmetic instruction targeting '£{arithmeticInstruction.Target.Name}' " +
                $"requires integer operands but was given '{operand1Type}'.");
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

        if (arithmeticInstruction.Operation
            is UtopIRArithmeticOperation.Max
            or UtopIRArithmeticOperation.Min
            or UtopIRArithmeticOperation.MaxFloat
            or UtopIRArithmeticOperation.MinFloat)
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
    /// <param name="arithmeticOperation">The arithmetic operations; must be <see cref="UtopIRArithmeticOperation.Max"/>, <see cref="UtopIRArithmeticOperation.Min"/>, <see cref="UtopIRArithmeticOperation.MaxFloat"/> or <see cref="UtopIRArithmeticOperation.MinFloat"/>.</param>
    /// <param name="operand1">The first operand.</param>
    /// <param name="operand2">The second operand.</param>
    /// <param name="type">The type of the target register, used to resolve the correct <see cref="Math"/> overload.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    private void EmitMaxMin(
        UtopIRArithmeticOperation arithmeticOperation,
        UtopIROperand operand1,
        UtopIROperand operand2,
        UtopIRType type,
        ILGenerator ilGenerator,
        Dictionary<string, LocalBuilder> locals)
    {
        Type clrType = this.MapToClrType(type);
        string methodName = arithmeticOperation is UtopIRArithmeticOperation.Max or UtopIRArithmeticOperation.MaxFloat
            ? "Max"
            : "Min";

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
    /// Emits CIL for a <see cref="BitwiseInstruction"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both operands must share the same integer type.  An undeclared target register is
    /// auto-declared with the operand type.
    /// </para>
    /// <para>
    /// For the shift operations, CIL requires the shift amount on the stack as an <c>int32</c>
    /// or native int regardless of the value width, so a <c>conv.i4</c> is emitted after
    /// loading the second operand when the operand type is 64-bit.
    /// </para>
    /// </remarks>
    /// <param name="bitwiseInstruction">The bitwise instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record the UtopIR name for the newly declared local.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when the operands have different inferred types or are not integer types.</exception>
    private void EmitBitwise(
        BitwiseInstruction bitwiseInstruction,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes)
    {
        UtopIRType operand1Type = this.InferOperandType(bitwiseInstruction.Operand1, localTypes);
        UtopIRType operand2Type = this.InferOperandType(bitwiseInstruction.Operand2, localTypes);
        if (operand1Type != operand2Type)
        {
            throw new InvalidOperationException(
                $"Bitwise instruction targeting '£{bitwiseInstruction.Target.Name}' has mismatched " +
                $"operand types ('{operand1Type}' and '{operand2Type}').");
        }

        if (!this.IsIntegerType(operand1Type))
        {
            throw new InvalidOperationException(
                $"Bitwise instruction targeting '£{bitwiseInstruction.Target.Name}' " +
                $"requires integer operands but was given '{operand1Type}'.");
        }

        if (!locals.TryGetValue(bitwiseInstruction.Target.Name, out LocalBuilder? declaredLocal))
        {
            declaredLocal = ilGenerator.DeclareLocal(this.MapToClrType(operand1Type));
            cilGenerator.RegisterLocalName(declaredLocal, bitwiseInstruction.Target.Name);
            locals[bitwiseInstruction.Target.Name] = declaredLocal;
            localTypes[bitwiseInstruction.Target.Name] = operand1Type;
        }

        LocalBuilder target = declaredLocal;
        UtopIRType targetType = localTypes[bitwiseInstruction.Target.Name];
        bool isShift = bitwiseInstruction.Operation is UtopIRBitwiseOperation.TransUp or UtopIRBitwiseOperation.TransDown;

        this.EmitStackLoadOperand(bitwiseInstruction.Operand1, ilGenerator, locals);
        this.EmitStackLoadOperand(bitwiseInstruction.Operand2, ilGenerator, locals);
        if (isShift && this.Is64BitType(operand1Type))
        {
            ilGenerator.Emit(OpCodes.Conv_I4);
        }

        this.EmitBitwiseOpcode(bitwiseInstruction.Operation, targetType, ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, target);
    }

    /// <summary>
    /// Emits CIL for an <see cref="InvInstruction"/> by loading the operand, applying <c>not</c>
    /// and storing the result into the target local.
    /// </summary>
    /// <remarks>
    /// An undeclared target register is auto-declared with the operand type.
    /// </remarks>
    /// <param name="invInstruction">The inv instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record the UtopIR name for the newly declared local.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when the operand is not an integer type.</exception>
    private void EmitInv(
        InvInstruction invInstruction,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes)
    {
        UtopIRType operandType = this.InferOperandType(invInstruction.Operand, localTypes);
        if (!this.IsIntegerType(operandType))
        {
            throw new InvalidOperationException(
                $"Bitwise NOT instruction targeting '£{invInstruction.Target.Name}' " +
                $"requires an integer operand but was given '{operandType}'.");
        }

        if (!locals.TryGetValue(invInstruction.Target.Name, out LocalBuilder? declaredLocal))
        {
            declaredLocal = ilGenerator.DeclareLocal(this.MapToClrType(operandType));
            cilGenerator.RegisterLocalName(declaredLocal, invInstruction.Target.Name);
            locals[invInstruction.Target.Name] = declaredLocal;
            localTypes[invInstruction.Target.Name] = operandType;
        }

        this.EmitStackLoadOperand(invInstruction.Operand, ilGenerator, locals);
        ilGenerator.Emit(OpCodes.Not);
        ilGenerator.Emit(OpCodes.Stloc, declaredLocal);
    }

    /// <summary>
    /// Emits CIL for a <see cref="ComparisonInstruction"/>.
    /// </summary>
    /// <remarks>
    /// The target local is always declared as <see cref="bool"/> regardless of the
    /// operand type, since a comparison result is always <c>decree</c>.
    /// </remarks>
    /// <param name="comparisonInstruction">The comparison instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record the UtopIR name for the newly declared local.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when the two operands have different inferred types.</exception>
    private void EmitComparison(
        ComparisonInstruction comparisonInstruction,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes)
    {
        UtopIRType operand1Type = this.InferOperandType(comparisonInstruction.Operand1, localTypes);
        UtopIRType operand2Type = this.InferOperandType(comparisonInstruction.Operand2, localTypes);
        if (operand1Type != operand2Type)
        {
            throw new InvalidOperationException(
                $"Comparison instruction targeting '£{comparisonInstruction.Target.Name}' has mismatched " +
                $"operand types ('{operand1Type}' and '{operand2Type}').");
        }

        if (!locals.TryGetValue(comparisonInstruction.Target.Name, out LocalBuilder? declaredLocal))
        {
            declaredLocal = ilGenerator.DeclareLocal(this.MapToClrType(UtopIRType.Decree));
            cilGenerator.RegisterLocalName(declaredLocal, comparisonInstruction.Target.Name);
            locals[comparisonInstruction.Target.Name] = declaredLocal;
            localTypes[comparisonInstruction.Target.Name] = UtopIRType.Decree;
        }

        this.EmitStackLoadOperand(comparisonInstruction.Operand1, ilGenerator, locals);
        this.EmitStackLoadOperand(comparisonInstruction.Operand2, ilGenerator, locals);
        this.EmitComparisonOpcode(comparisonInstruction.Operation, operand1Type, ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, declaredLocal);
    }

    /// <summary>
    /// Emits CIL for a <see cref="LogicalInstruction"/>.
    /// </summary>
    /// <remarks>
    /// A CIL <see cref="bool"/> is always a 0/1 <c>int32</c> on the evaluation stack, so bitwise
    /// <c>and</c>/<c>or</c> on the two operands is equivalent to logical <c>and</c>/<c>or</c>.
    /// </remarks>
    /// <param name="logicalInstruction">The logical instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record the UtopIR name for the newly declared local.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when either operand is not of the <c>decree</c> type.</exception>
    private void EmitLogical(
        LogicalInstruction logicalInstruction,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes)
    {
        UtopIRType operand1Type = this.InferOperandType(logicalInstruction.Operand1, localTypes);
        UtopIRType operand2Type = this.InferOperandType(logicalInstruction.Operand2, localTypes);
        if (!this.IsDecreeType(operand1Type) || !this.IsDecreeType(operand2Type))
        {
            throw new InvalidOperationException(
                $"Logical instruction targeting '£{logicalInstruction.Target.Name}' requires " +
                $"decree operands but was given '{operand1Type}' and '{operand2Type}'.");
        }

        if (!locals.TryGetValue(logicalInstruction.Target.Name, out LocalBuilder? declaredLocal))
        {
            declaredLocal = ilGenerator.DeclareLocal(this.MapToClrType(UtopIRType.Decree));
            cilGenerator.RegisterLocalName(declaredLocal, logicalInstruction.Target.Name);
            locals[logicalInstruction.Target.Name] = declaredLocal;
            localTypes[logicalInstruction.Target.Name] = UtopIRType.Decree;
        }

        this.EmitStackLoadOperand(logicalInstruction.Operand1, ilGenerator, locals);
        this.EmitStackLoadOperand(logicalInstruction.Operand2, ilGenerator, locals);
        ilGenerator.Emit(logicalInstruction.Operation == UtopIRLogicalOperation.Both
            ? OpCodes.And
            : OpCodes.Or);
        ilGenerator.Emit(OpCodes.Stloc, declaredLocal);
    }

    /// <summary>
    /// Emits CIL for a <see cref="HardlyInstruction"/> by loading the operand, negating it via the
    /// <c>x == false</c> idiom, and storing the result into the target local.
    /// </summary>
    /// <param name="hardlyInstruction">The hardly instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record the UtopIR name for the newly declared local.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when the operand is not of the <c>decree</c> type.</exception>
    private void EmitHardly(
        HardlyInstruction hardlyInstruction,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes)
    {
        UtopIRType operandType = this.InferOperandType(hardlyInstruction.Operand, localTypes);
        if (!this.IsDecreeType(operandType))
        {
            throw new InvalidOperationException(
                $"Hardly instruction targeting '£{hardlyInstruction.Target.Name}' " +
                $"requires a decree operand but was given '{operandType}'.");
        }

        if (!locals.TryGetValue(hardlyInstruction.Target.Name, out LocalBuilder? declaredLocal))
        {
            declaredLocal = ilGenerator.DeclareLocal(this.MapToClrType(UtopIRType.Decree));
            cilGenerator.RegisterLocalName(declaredLocal, hardlyInstruction.Target.Name);
            locals[hardlyInstruction.Target.Name] = declaredLocal;
            localTypes[hardlyInstruction.Target.Name] = UtopIRType.Decree;
        }

        this.EmitStackLoadOperand(hardlyInstruction.Operand, ilGenerator, locals);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Ceq);
        ilGenerator.Emit(OpCodes.Stloc, declaredLocal);
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
    /// Emits CIL for a <see cref="VictimYarnInstruction"/> by loading the <c>yarn</c> string value and the
    /// 1-based index converted to 0-based, then calling <c>string.get_Chars(int)</c> via <c>callvirt</c>.
    /// </summary>
    /// <remarks>
    /// An undeclared target register is auto-declared as <see cref="UtopIRType.Stitch"/>.
    /// </remarks>
    /// <param name="victimYarn">The victim.yarn instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record the UtopIR name for an auto-declared target local.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when <c>string.get_Chars(int)</c> cannot be resolved via reflection.</exception>
    private void EmitVictimYarn(
        VictimYarnInstruction victimYarn,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes)
    {
        if (!locals.TryGetValue(victimYarn.Target.Name, out LocalBuilder? declaredLocal))
        {
            declaredLocal = ilGenerator.DeclareLocal(this.MapToClrType(UtopIRType.Stitch));
            cilGenerator.RegisterLocalName(declaredLocal, victimYarn.Target.Name);
            locals[victimYarn.Target.Name] = declaredLocal;
            localTypes[victimYarn.Target.Name] = UtopIRType.Stitch;
        }

        this.EmitStackLoadOperand(victimYarn.YarnString, ilGenerator, locals);
        this.EmitStackLoadOperand(victimYarn.Index, ilGenerator, locals);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Sub);

        MethodInfo getCharsMethod = typeof(string).GetMethod("get_Chars", [typeof(int)])
            ?? throw new InvalidOperationException("'string.get_Chars(int)' could not be resolved.");
        ilGenerator.Emit(OpCodes.Callvirt, getCharsMethod);
        ilGenerator.Emit(OpCodes.Stloc, declaredLocal);
    }

    /// <summary>
    /// Declares a CIL local variable for a <see cref="WelcomeListInstruction"/> and allocates a CLR
    /// array of the declared element type and size via <c>newarr</c>.
    /// </summary>
    /// <param name="welcomeList">The welcome.list instruction declaring the array variable.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record the UtopIR name for the newly declared local.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    /// <param name="arrayElementTypes">The map from array variable name to its declared element <see cref="UtopIRType"/>.</param>
    private void EmitWelcomeList(
        WelcomeListInstruction welcomeList,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes,
        Dictionary<string, UtopIRType> arrayElementTypes)
    {
        Type elementClrType = this.MapToClrType(welcomeList.ElementType);

        ilGenerator.Emit(OpCodes.Ldc_I4, welcomeList.Size);
        ilGenerator.Emit(OpCodes.Newarr, elementClrType);

        LocalBuilder local = ilGenerator.DeclareLocal(elementClrType.MakeArrayType());
        cilGenerator.RegisterLocalName(local, welcomeList.Target.Name);
        ilGenerator.Emit(OpCodes.Stloc, local);

        locals[welcomeList.Target.Name] = local;
        localTypes[welcomeList.Target.Name] = UtopIRType.Array;
        arrayElementTypes[welcomeList.Target.Name] = welcomeList.ElementType;
    }

    /// <summary>
    /// Emits CIL for an <see cref="AppointVictimInstruction"/> by loading the array, the 1-based index
    /// converted to 0-based, and the value, then storing via the element type <c>stelem.*</c> opcode.
    /// </summary>
    /// <param name="appointVictim">The appoint.victim instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="arrayElementTypes">The map from array variable name to its declared element <see cref="UtopIRType"/>.</param>
    private void EmitAppointVictim(
        AppointVictimInstruction appointVictim,
        ILGenerator ilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> arrayElementTypes)
    {
        UtopIRType elementType = arrayElementTypes[appointVictim.Array.Name];

        ilGenerator.Emit(OpCodes.Ldloc, locals[appointVictim.Array.Name]);
        this.EmitStackLoadOperand(appointVictim.Index, ilGenerator, locals);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Sub);
        this.EmitStackLoadOperand(appointVictim.Value, ilGenerator, locals);
        this.EmitArrayElementStoreOpcode(elementType, ilGenerator);
    }

    /// <summary>
    /// Emits CIL for a <see cref="VictimListInstruction"/> by loading the array and the 1-based index
    /// converted to 0-based, then loading via the element type <c>ldelem.*</c> opcode.
    /// </summary>
    /// <remarks>
    /// An undeclared target register is auto-declared with the array element type.
    /// </remarks>
    /// <param name="victimList">The victim.list instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="cilGenerator">The CIL generator to record the UtopIR name for an auto-declared target local.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="localTypes">The map from variable name to <see cref="UtopIRType"/>.</param>
    /// <param name="arrayElementTypes">The map from array variable name to its declared element <see cref="UtopIRType"/>.</param>
    private void EmitVictimList(
        VictimListInstruction victimList,
        ILGenerator ilGenerator,
        CilGenerator cilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, UtopIRType> localTypes,
        Dictionary<string, UtopIRType> arrayElementTypes)
    {
        UtopIRType elementType = arrayElementTypes[victimList.Array.Name];

        if (!locals.TryGetValue(victimList.Target.Name, out LocalBuilder? declaredLocal))
        {
            declaredLocal = ilGenerator.DeclareLocal(this.MapToClrType(elementType));
            cilGenerator.RegisterLocalName(declaredLocal, victimList.Target.Name);
            locals[victimList.Target.Name] = declaredLocal;
            localTypes[victimList.Target.Name] = elementType;
        }

        ilGenerator.Emit(OpCodes.Ldloc, locals[victimList.Array.Name]);
        this.EmitStackLoadOperand(victimList.Index, ilGenerator, locals);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Sub);
        this.EmitArrayElementLoadOpcode(elementType, ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, declaredLocal);
    }

    /// <summary>
    /// Retrieves the <see cref="Label"/> for the given name, lazily calling
    /// <see cref="ILGenerator.DefineLabel"/> the first time the name is seen.
    /// </summary>
    /// <remarks>
    /// <c>sail</c>/<c>sailalike</c>/<c>sailunlike</c> can forward-reference a label not yet marked, which
    /// <see cref="ILGenerator"/> supports natively: a <see cref="Label"/> may be referenced before
    /// <see cref="ILGenerator.MarkLabel"/> is called on it.
    /// </remarks>
    /// <param name="name">The label name, without the <c>!</c> prefix.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="labels">The map from label name to <see cref="Label"/>.</param>
    /// <returns>The existing or newly-defined <see cref="Label"/> for <paramref name="name"/>.</returns>
    private Label GetOrDefineLabel(string name, ILGenerator ilGenerator, Dictionary<string, Label> labels)
    {
        if (!labels.TryGetValue(name, out Label label))
        {
            label = ilGenerator.DefineLabel();
            labels[name] = label;
        }

        return label;
    }

    /// <summary>
    /// Emits CIL for a <see cref="LabelInstruction"/> by marking the current position with its
    /// <see cref="Label"/>.
    /// </summary>
    /// <param name="labelInstruction">The label instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="labels">The map from label name to <see cref="Label"/>.</param>
    private void EmitLabel(LabelInstruction labelInstruction, ILGenerator ilGenerator, Dictionary<string, Label> labels)
    {
        ilGenerator.MarkLabel(this.GetOrDefineLabel(labelInstruction.Name.Name, ilGenerator, labels));
    }

    /// <summary>
    /// Emits CIL for a <see cref="SailInstruction"/> as an unconditional branch.
    /// </summary>
    /// <param name="sailInstruction">The sail instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="labels">The map from label name to <see cref="Label"/>.</param>
    private void EmitSail(SailInstruction sailInstruction, ILGenerator ilGenerator, Dictionary<string, Label> labels)
    {
        ilGenerator.Emit(OpCodes.Br, this.GetOrDefineLabel(sailInstruction.Label.Name, ilGenerator, labels));
    }

    /// <summary>
    /// Emits CIL for a <see cref="SailAlikeInstruction"/> as a branch taken when the <c>decree</c>
    /// value is <c>verity</c>.
    /// </summary>
    /// <param name="sailAlikeInstruction">The sailalike instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="labels">The map from label name to <see cref="Label"/>.</param>
    private void EmitSailAlike(
        SailAlikeInstruction sailAlikeInstruction,
        ILGenerator ilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, Label> labels)
    {
        this.EmitStackLoadOperand(sailAlikeInstruction.Value, ilGenerator, locals);
        ilGenerator.Emit(OpCodes.Brtrue, this.GetOrDefineLabel(sailAlikeInstruction.Label.Name, ilGenerator, labels));
    }

    /// <summary>
    /// Emits CIL for a <see cref="SailUnlikeInstruction"/> as a branch taken when the <c>decree</c>
    /// value is <c>nay</c>.
    /// </summary>
    /// <param name="sailUnlikeInstruction">The sailunlike instruction.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <param name="locals">The map from variable name to <see cref="LocalBuilder"/>.</param>
    /// <param name="labels">The map from label name to <see cref="Label"/>.</param>
    private void EmitSailUnlike(
        SailUnlikeInstruction sailUnlikeInstruction,
        ILGenerator ilGenerator,
        Dictionary<string, LocalBuilder> locals,
        Dictionary<string, Label> labels)
    {
        this.EmitStackLoadOperand(sailUnlikeInstruction.Value, ilGenerator, locals);
        ilGenerator.Emit(OpCodes.Brfalse, this.GetOrDefineLabel(sailUnlikeInstruction.Label.Name, ilGenerator, labels));
    }

    /// <summary>
    /// Emits CIL for a <see cref="FindInstruction"/> loading the return value or 0 for a void return.
    /// </summary>
    /// <remarks>
    /// <c>Main</c> always returns <c>int32</c>.  64-bit values are narrowed to <c>int32</c> via
    /// <c>conv.i4</c>, as are floating-point values, which are truncated in the process.  The type
    /// used to decide whether a conversion is needed is derived via <see cref="InferOperandType"/>
    /// for both a <see cref="VariableOperand"/> and a <see cref="LiteralOperand"/> alike, rather than
    /// switching on the operand CLR runtime type directly, so this stays correct automatically if a
    /// future <see cref="UtopIRType"/> is added to <see cref="Is64BitType"/> or <see cref="IsFloatType"/>.
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
        else
        {
            this.EmitStackLoadOperand(find.Value, ilGenerator, locals);
            UtopIRType valueType = this.InferOperandType(find.Value, localTypes);
            if (this.Is64BitType(valueType) || this.IsFloatType(valueType))
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
    /// <para>
    /// <c>char</c> has no dedicated shape on the CIL evaluation stack, so it is loaded the same way
    /// as <c>byte</c> and <c>ushort</c>: its UTF-16 code unit is widened to <c>int32</c> via <c>ldc.i4</c>.
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
            case double float64:
                ilGenerator.Emit(OpCodes.Ldc_R8, float64);
                break;
            case float float32:
                ilGenerator.Emit(OpCodes.Ldc_R4, float32);
                break;
            case char character:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)character);
                break;
            case bool boolean:
                ilGenerator.Emit(OpCodes.Ldc_I4, boolean ? 1 : 0);
                break;
            case string stringValue:
                ilGenerator.Emit(OpCodes.Ldstr, stringValue);
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
            case UtopIRArithmeticOperation.Sum or UtopIRArithmeticOperation.SumFloat:
                ilGenerator.Emit(OpCodes.Add);
                break;
            case UtopIRArithmeticOperation.Diff or UtopIRArithmeticOperation.DiffFloat:
                ilGenerator.Emit(OpCodes.Sub);
                break;
            case UtopIRArithmeticOperation.Prod or UtopIRArithmeticOperation.ProdFloat:
                ilGenerator.Emit(OpCodes.Mul);
                break;
            case UtopIRArithmeticOperation.Quot or UtopIRArithmeticOperation.QuotFloat:
                ilGenerator.Emit(isUnsigned
                    ? OpCodes.Div_Un
                    : OpCodes.Div);
                break;
            case UtopIRArithmeticOperation.Rem or UtopIRArithmeticOperation.RemFloat:
                ilGenerator.Emit(isUnsigned
                    ? OpCodes.Rem_Un
                    : OpCodes.Rem);
                break;
            case UtopIRArithmeticOperation.Max
                or UtopIRArithmeticOperation.Min
                or UtopIRArithmeticOperation.MaxFloat
                or UtopIRArithmeticOperation.MinFloat:
                throw new InvalidOperationException($"Operation '{operation}' must be handled by EmitMaxMin.");
            default:
                throw new InvalidOperationException($"Operation '{operation}' not supported.");
        }
    }

    /// <summary>
    /// Emits the CIL bitwise opcode corresponding to the given <see cref="UtopIRBitwiseOperation"/>.
    /// </summary>
    /// <remarks>
    /// The right shift uses <c>shr.un</c> for unsigned types so vacated high bits are zero-filled
    /// rather than sign-extended.  All other operations are bit-pattern identical for signed and
    /// unsigned types and use a single opcode.
    /// </remarks>
    /// <param name="operation">The bitwise operation.</param>
    /// <param name="type">The target variable type used to select the signed or unsigned right-shift opcode.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="operation"/> is not a recognised bitwise operation.</exception>
    private void EmitBitwiseOpcode(UtopIRBitwiseOperation operation, UtopIRType type, ILGenerator ilGenerator)
    {
        switch (operation)
        {
            case UtopIRBitwiseOperation.Chord:
                ilGenerator.Emit(OpCodes.And);
                break;
            case UtopIRBitwiseOperation.Harmony:
                ilGenerator.Emit(OpCodes.Or);
                break;
            case UtopIRBitwiseOperation.Discord:
                ilGenerator.Emit(OpCodes.Xor);
                break;
            case UtopIRBitwiseOperation.TransUp:
                ilGenerator.Emit(OpCodes.Shl);
                break;
            case UtopIRBitwiseOperation.TransDown:
                ilGenerator.Emit(this.IsUnsignedType(type)
                    ? OpCodes.Shr_Un
                    : OpCodes.Shr);
                break;
            default:
                throw new InvalidOperationException($"Operation '{operation}' not supported.");
        }
    }

    /// <summary>
    /// Emits the CIL comparison opcode(s) corresponding to the given <see cref="UtopIRComparisonOperation"/>.
    /// </summary>
    /// <remarks>
    /// * <c>alike</c> uses <c>ceq</c> directly.
    /// * <c>unlike</c> has no direct CIL opcode, so it is synthesised as <c>ceq</c> followed by <c>ldc.i4.0</c>/<c>ceq</c> (the standard "not equal" idiom: negate an equality result).
    /// * <c>preadam</c> and <c>lowerdeg</c> use <c>cgt</c> and <c>clt</c> respectively, selecting the unsigned variant for unsigned operand types for the same reason <see cref="EmitArithmeticOpcode"/> selects <c>div.un</c>/<c>rem.un</c>.
    /// </remarks>
    /// <param name="operation">The comparison operation.</param>
    /// <param name="operandType">The operand type used to select the signed or unsigned relational opcode.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="operation"/> is not a recognised comparison operation.</exception>
    private void EmitComparisonOpcode(UtopIRComparisonOperation operation, UtopIRType operandType, ILGenerator ilGenerator)
    {
        bool isUnsigned = this.IsUnsignedType(operandType);
        switch (operation)
        {
            case UtopIRComparisonOperation.Alike or UtopIRComparisonOperation.AlikeFloat:
                ilGenerator.Emit(OpCodes.Ceq);
                break;
            case UtopIRComparisonOperation.Unlike or UtopIRComparisonOperation.UnlikeFloat:
                ilGenerator.Emit(OpCodes.Ceq);
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Ceq);
                break;
            case UtopIRComparisonOperation.PreAdam or UtopIRComparisonOperation.PreAdamFloat:
                ilGenerator.Emit(isUnsigned
                    ? OpCodes.Cgt_Un
                    : OpCodes.Cgt);
                break;
            case UtopIRComparisonOperation.LowerDeg or UtopIRComparisonOperation.LowerDegFloat:
                ilGenerator.Emit(isUnsigned
                    ? OpCodes.Clt_Un
                    : OpCodes.Clt);
                break;
            default:
                throw new InvalidOperationException($"Operation '{operation}' not supported.");
        }
    }

    /// <summary>
    /// Emits the CIL <c>ldelem.*</c> opcode that loads an array element of the given
    /// <see cref="UtopIRType"/> onto the evaluation stack.
    /// </summary>
    /// <remarks>
    /// Unsigned narrow integer types (<see cref="UtopIRType.StandingPeer"/>,
    /// <see cref="UtopIRType.StandingPirate"/>, <see cref="UtopIRType.StandingSausageRoll"/>) and
    /// <see cref="UtopIRType.Stitch"/> (a 16-bit unsigned code unit) use the <c>u*</c>-suffixed opcode
    /// so the loaded value is zero-extended rather than sign-extended.  <see cref="UtopIRType.Yarn"/>
    /// is a CLR reference type and uses <c>ldelem.ref</c>.
    /// </remarks>
    /// <param name="elementType">The array declared element type.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="elementType"/> has no supported array element opcode in this version.</exception>
    private void EmitArrayElementLoadOpcode(UtopIRType elementType, ILGenerator ilGenerator)
    {
        OpCode opcode = elementType switch
        {
            UtopIRType.Chancellor or UtopIRType.StandingChancellor => OpCodes.Ldelem_I8,
            UtopIRType.Peer => OpCodes.Ldelem_I4,
            UtopIRType.StandingPeer => OpCodes.Ldelem_U4,
            UtopIRType.Pirate => OpCodes.Ldelem_I2,
            UtopIRType.StandingPirate or UtopIRType.Stitch => OpCodes.Ldelem_U2,
            UtopIRType.SausageRoll => OpCodes.Ldelem_I1,
            UtopIRType.StandingSausageRoll or UtopIRType.Decree => OpCodes.Ldelem_U1,
            UtopIRType.Fathom => OpCodes.Ldelem_R8,
            UtopIRType.Foot => OpCodes.Ldelem_R4,
            UtopIRType.Yarn => OpCodes.Ldelem_Ref,
            _ => throw new NotSupportedException(
                $"UtopIR type '{elementType}' is not supported as an array element type by the CIL emitter in this version.")
        };

        ilGenerator.Emit(opcode);
    }

    /// <summary>
    /// Emits the CIL <c>stelem.*</c> opcode that stores a value from the evaluation stack into an
    /// array element of the given <see cref="UtopIRType"/>.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="EmitArrayElementLoadOpcode"/>, storing has no signed/unsigned distinction:
    /// a <c>stelem.*</c> opcode simply narrows and writes the bit pattern, so one opcode per width
    /// covers both a type and its unsigned counterpart. <see cref="UtopIRType.Yarn"/> is a CLR
    /// reference type and uses <c>stelem.ref</c>.
    /// </remarks>
    /// <param name="elementType">The array declared element type.</param>
    /// <param name="ilGenerator">The IL generator for the current method body.</param>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="elementType"/> has no supported array element opcode in this version.</exception>
    private void EmitArrayElementStoreOpcode(UtopIRType elementType, ILGenerator ilGenerator)
    {
        OpCode opcode = elementType switch
        {
            UtopIRType.Chancellor or UtopIRType.StandingChancellor => OpCodes.Stelem_I8,
            UtopIRType.Peer or UtopIRType.StandingPeer => OpCodes.Stelem_I4,
            UtopIRType.Pirate or UtopIRType.StandingPirate or UtopIRType.Stitch => OpCodes.Stelem_I2,
            UtopIRType.SausageRoll or UtopIRType.StandingSausageRoll or UtopIRType.Decree => OpCodes.Stelem_I1,
            UtopIRType.Fathom => OpCodes.Stelem_R8,
            UtopIRType.Foot => OpCodes.Stelem_R4,
            UtopIRType.Yarn => OpCodes.Stelem_Ref,
            _ => throw new NotSupportedException(
                $"UtopIR type '{elementType}' is not supported as an array element type by the CIL emitter in this version.")
        };

        ilGenerator.Emit(opcode);
    }

    /// <summary>
    /// Emits the <c>conv.*</c> opcode that converts whatever is currently on the CIL evaluation stack
    /// to the given target type true CLR width and signedness.
    /// </summary>
    /// <remarks>
    /// Covers all integer and floating-point <see cref="UtopIRType"/> variants.  A single target-only
    /// conversion is sufficient regardless of the original type of the value.  <c>conv.*</c> opcodes
    /// convert whatever is on the stack so this also serves the <see cref="EmitWere"/> general
    /// widen/narrow cast between any pair of numeric types, not just the narrowing case
    /// <see cref="EmitMaxMin"/> uses.  Floating-point to integer conversion truncates toward zero,
    /// matching the Topsy Turvy runtime cast behaviour.  <see cref="UtopIRType.Stitch"/> uses
    /// <c>conv.u2</c>, the same opcode as <see cref="UtopIRType.StandingPirate"/> (<c>ushort</c>),
    /// since a .NET <c>char</c> is a 16-bit unsigned code unit on the CIL stack.
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
            UtopIRType.Fathom => OpCodes.Conv_R8,
            UtopIRType.Foot => OpCodes.Conv_R4,
            UtopIRType.Stitch => OpCodes.Conv_U2,
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
    /// Determines if the <see cref="UtopIRType"/> is a floating-point type.
    /// </summary>
    /// <param name="type">The UtopIR type to test.</param>
    /// <returns><c>true</c> for floating-point types, otherwise <c>false</c>.</returns>
    private bool IsFloatType(UtopIRType type) =>
        type is UtopIRType.Fathom
            or UtopIRType.Foot;

    /// <summary>
    /// Determines if the <see cref="UtopIRType"/> is an integer type of any width or signed-ness.
    /// </summary>
    /// <param name="type">The UtopIR type to test.</param>
    /// <returns><c>true</c> for integer types, otherwise <c>false</c>.</returns>
    private bool IsIntegerType(UtopIRType type) =>
        type is UtopIRType.Chancellor
            or UtopIRType.Peer
            or UtopIRType.Pirate
            or UtopIRType.SausageRoll
            or UtopIRType.StandingChancellor
            or UtopIRType.StandingPeer
            or UtopIRType.StandingPirate
            or UtopIRType.StandingSausageRoll;

    /// <summary>
    /// Determines if the <see cref="UtopIRType"/> is the <c>decree</c> (boolean) type.
    /// </summary>
    /// <param name="type">The UtopIR type to test.</param>
    /// <returns><c>true</c> for <see cref="UtopIRType.Decree"/>, otherwise <c>false</c>.</returns>
    private bool IsDecreeType(UtopIRType type) =>
        type is UtopIRType.Decree;

    /// <summary>
    /// Determines if the <see cref="UtopIRArithmeticOperation"/> is a floating-point operation.
    /// </summary>
    /// <param name="operation">The arithmetic operation to test.</param>
    /// <returns><c>true</c> for floating-point operations, otherwise <c>false</c>.</returns>
    private bool IsFloatOperation(UtopIRArithmeticOperation operation) =>
        operation is UtopIRArithmeticOperation.SumFloat
            or UtopIRArithmeticOperation.DiffFloat
            or UtopIRArithmeticOperation.ProdFloat
            or UtopIRArithmeticOperation.QuotFloat
            or UtopIRArithmeticOperation.RemFloat
            or UtopIRArithmeticOperation.MaxFloat
            or UtopIRArithmeticOperation.MinFloat;

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
        UtopIRType.Fathom => typeof(double),
        UtopIRType.Foot => typeof(float),
        UtopIRType.Stitch => typeof(char),
        UtopIRType.Decree => typeof(bool),
        UtopIRType.Yarn => typeof(string),
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
            double => UtopIRType.Fathom,
            float => UtopIRType.Foot,
            char => UtopIRType.Stitch,
            bool => UtopIRType.Decree,
            string => UtopIRType.Yarn,
            _ => throw new NotSupportedException(
                $"Literal value of CLR type '{literalOperand.Value.GetType().Name}' has no corresponding UtopIR type in this version.")
        },
        _ => throw new NotSupportedException($"Operand type '{operand.GetType().Name}' is not supported.")
    };
}
