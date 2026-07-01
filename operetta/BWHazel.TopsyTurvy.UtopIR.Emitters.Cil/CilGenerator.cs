using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

/// <summary>
/// Produces the readable, bare IL instruction listing returned via <see cref="CilEmitResult.IlSource"/>,
/// by disassembling the assembly bytes <see cref="CilEmitter"/> already built and restoring the
/// UtopIR variable names that .NET metadata does not retain for locals.
/// </summary>
/// <remarks>
/// <para>
/// ### Two-Phase Contract
/// This type has exactly two responsibilities, used in a fixed order by <see cref="CilEmitter"/>:
/// </para>
/// <para>
/// * **During Emission:** <see cref="CilEmitter"/> calls <see cref="RegisterLocalName"/> immediately
/// after every <see cref="ILGenerator.DeclareLocal(System.Type)"/>, recording the UtopIR name
/// and CLR type against that local index.  No IL text exists yet at this point: this step only remembers
/// names for later.
/// * **After Emission:** Once the assembly has been fully built and serialised to bytes,
/// <see cref="CilEmitter"/> calls <see cref="ToIlText"/> once, passing those bytes.  This
/// disassembles the real, already-persisted method body and produces the final text, substituting
/// each local reference with its registered UtopIR name.
/// </para>
/// <para>
/// ### Local Variable Names
/// .NET metadata does not store local variable names: only a portable PDB does, which this project
/// does not currently generate, so AsmResolver alone cannot recover UtopIR names.  This is
/// exactly what phase 1 above exists to compensate for.  See <see cref="UtopIrCilInstructionFormatter"/>
/// for how the registered names are substituted back into the disassembled instruction text.
/// </para>
/// </remarks>
internal sealed class CilGenerator
{
    /// <summary>
    /// Maps each declared local index to its original UtopIR name.
    /// </summary>
    private readonly Dictionary<int, string> localNamesByIndex = [];

    /// <summary>
    /// Records the UtopIR name for a local, keyed on its index, for later substitution by <see cref="ToIlText"/>.
    /// </summary>
    /// <param name="local">The local declared via <see cref="ILGenerator.DeclareLocal(System.Type)"/>.</param>
    /// <param name="name">The UtopIR variable name, without the <c>£</c> prefix, to record for this local.</param>
    public void RegisterLocalName(LocalBuilder local, string name)
    {
        this.localNamesByIndex[local.LocalIndex] = name;
    }

    /// <summary>
    /// Disassembles the given assembly bytes entry type and method into a bare IL instruction
    /// listing, substituting registered UtopIR names for local variable references.
    /// </summary>
    /// <remarks>
    /// Returns nothing but the disassembled instruction lines since AsmResolver provides no facility
    /// to render either as text and no platform currently builds executable IL from source text.
    /// Local names/types are still visible inline on each <c>ldloc</c>/<c>stloc</c> line via
    /// <see cref="UtopIrCilInstructionFormatter"/>.
    /// </remarks>
    /// <param name="assemblyBytes">The full bytes of the assembly previously built by <see cref="CilEmitter"/>.</param>
    /// <returns>The bare, formatted IL instruction listing.</returns>
    public string ToIlText(byte[] assemblyBytes)
    {
        AssemblyDefinition assembly = AssemblyDefinition.FromBytes(assemblyBytes);
        ModuleDefinition module = assembly.ManifestModule!;
        TypeDefinition operaType = module.TopLevelTypes.Single(type => type.Name == "Opera");
        MethodDefinition mainMethod = operaType.Methods.Single(method => method.Name == "Main");
        CilMethodBody methodBody = mainMethod.CilMethodBody!;

        StringBuilder builder = new();
        UtopIrCilInstructionFormatter formatter = new(this.localNamesByIndex);
        foreach (CilInstruction instruction in methodBody.Instructions)
        {
            builder.AppendLine(formatter.Format(instruction));
        }

        return builder.ToString().TrimEnd('\r', '\n');
    }
}
