using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using BWHazel.TopsyTurvy.UtopIR.Ast;

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
/// * **During Emission:** <see cref="CilEmitter"/> sets <see cref="CurrentMethodName"/> before emitting
/// the body of each function, then calls <see cref="RegisterLocalName"/> immediately after every
/// <see cref="ILGenerator.DeclareLocal(System.Type)"/>, recording the UtopIR name against the index
/// of that local within the current method.  No IL text exists yet at this point: this step only
/// remembers names for later.  A local index is only unique within one method, since every function
/// restarts its locals at index 0, so names are keyed on the method name and local index together,
/// not the index alone.
/// * **After Emission:** Once the assembly has been fully built and serialised to bytes,
/// <see cref="CilEmitter"/> calls <see cref="ToIlText"/> once, passing those bytes.  This disassembles
/// every method defined on the entry point type, in declaration order, producing the final text with a
/// <c>// &lt;name&gt;</c> header per method and each local reference substituted with its registered
/// UtopIR name.
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
    /// Maps each declared local, identified by method name and local index, to its original UtopIR name.
    /// </summary>
    private readonly Dictionary<(string MethodName, int LocalIndex), string> localNamesByMethodAndIndex = [];

    /// <summary>
    /// Gets or sets the name of the function currently being emitted, used to scope
    /// <see cref="RegisterLocalName"/> registrations to the right method.
    /// </summary>
    /// <remarks>
    /// <see cref="CilEmitter"/> sets this once per function before emitting its body, since a fresh
    /// <see cref="ILGenerator"/> always restarts its local indices at <c>0</c>, regardless of which
    /// function it belongs to.
    /// </remarks>
    public string CurrentMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Records the UtopIR name for a local, keyed on <see cref="CurrentMethodName"/> and its index,
    /// for later substitution by <see cref="ToIlText"/>.
    /// </summary>
    /// <param name="local">The local declared via <see cref="ILGenerator.DeclareLocal(System.Type)"/>.</param>
    /// <param name="name">The UtopIR variable name, without the <c>£</c> prefix, to record for this local.</param>
    public void RegisterLocalName(LocalBuilder local, string name)
    {
        this.localNamesByMethodAndIndex[(this.CurrentMethodName, local.LocalIndex)] = name;
    }

    /// <summary>
    /// Disassembles every method defined on the entry point type of the assembly into a bare IL
    /// instruction listing, substituting registered UtopIR names for local variable references.
    /// </summary>
    /// <remarks>
    /// Methods are listed in declaration order: every <see cref="UtopIRFunctionDefinition"/> in
    /// <see cref="UtopIRProgram.Functions"/> order, then the real CLR entry point <c>Main</c> last,
    /// each preceded by a <c>// &lt;name&gt;</c> header line. Local names are substituted inline on
    /// each <c>ldloc</c>/<c>stloc</c> line via <see cref="UtopIrCilInstructionFormatter"/>.
    /// </remarks>
    /// <param name="assemblyBytes">The full bytes of the assembly previously built by <see cref="CilEmitter"/>.</param>
    /// <returns>The bare, formatted IL instruction listing for every method.</returns>
    public string ToIlText(byte[] assemblyBytes)
    {
        AssemblyDefinition assembly = AssemblyDefinition.FromBytes(assemblyBytes);
        ModuleDefinition module = assembly.ManifestModule!;
        TypeDefinition operaType = module.TopLevelTypes.Single(type => type.Name == "Opera");

        StringBuilder builder = new();
        bool isFirstMethod = true;
        foreach (MethodDefinition method in operaType.Methods)
        {
            if (method.CilMethodBody is null)
            {
                continue;
            }

            if (!isFirstMethod)
            {
                builder.AppendLine();
            }

            isFirstMethod = false;
            builder.AppendLine($"// {method.Name}");

            Dictionary<int, string> localNames = this.localNamesByMethodAndIndex
                .Where(entry => entry.Key.MethodName == method.Name)
                .ToDictionary(entry => entry.Key.LocalIndex, entry => entry.Value);

            UtopIrCilInstructionFormatter formatter = new(localNames);
            foreach (CilInstruction instruction in method.CilMethodBody.Instructions)
            {
                builder.AppendLine(formatter.Format(instruction));
            }
        }

        return builder.ToString().TrimEnd('\r', '\n');
    }
}
