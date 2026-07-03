using System.Collections.Generic;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

/// <summary>
/// A <see cref="CilInstructionFormatter"/> that substitutes registered UtopIR names for local
/// variable operands, instead of the AsmResolver default numeric rendering.
/// </summary>
/// <param name="localNames">Maps each declared local index to its UtopIR name.</param>
internal sealed class UtopIrCilInstructionFormatter(IReadOnlyDictionary<int, string> localNames) : CilInstructionFormatter
{
    /// <inheritdoc />
    protected override string FormatVariable(object? operand)
    {
        if (operand is CilLocalVariable local)
        {
            return $"'£{this.ResolveName(local.Index)}'";
        }

        return base.FormatVariable(operand);
    }

    /// <summary>
    /// Formats a single disassembled instruction, substituting a registered UtopIR local name for
    /// <c>ldloc</c>/<c>stloc</c> instructions and otherwise deferring to the inherited
    /// <c>FormatInstruction</c> which, via <see cref="FormatVariable"/> above, already resolves
    /// full-form local references, and via the unmodified base formatting, resolves method/field/type
    /// references correctly too.
    /// </summary>
    /// <param name="instruction">The instruction to format.</param>
    /// <returns>The formatted instruction line, including its real <c>IL_XXXX:</c> byte offset.</returns>
    public string Format(CilInstruction instruction)
    {
        if ((instruction.IsLdloc() || instruction.IsStloc()) && instruction.Operand is null)
        {
            int? index = TryGetMacroLocalIndex(instruction);
            string mnemonic = instruction.IsLdloc()
                ? "ldloc"
                : "stloc";

            string name = index.HasValue
                ? this.ResolveName(index.Value)
                : $"V_{index}";

            return $"{this.FormatLabel(instruction.Offset)}: {mnemonic} '£{name}'";
        }

        return this.FormatInstruction(instruction);
    }

    /// <summary>
    /// Looks up the registered UtopIR name for a local index, falling back to the AsmResolver own
    /// <c>V_&lt;index&gt;</c> convention if the index was never registered although this should
    /// not happen for any local <see cref="CilEmitter"/> itself declares.
    /// </summary>
    /// <param name="index">The local index.</param>
    /// <returns>The UtopIR name, or a fallback if unregistered.</returns>
    private string ResolveName(int index) =>
        localNames.TryGetValue(index, out string? name)
            ? name
            : $"V_{index}";

    /// <summary>
    /// Resolves the local variable index implied by a macro-form <c>ldloc</c>/<c>stloc</c> opcode,
    /// e.g. <see cref="CilCode.Ldloc_0"/>, which carries no operand of its own.
    /// </summary>
    /// <param name="instruction">The macro-form <c>ldloc</c>/<c>stloc</c> instruction to resolve.</param>
    /// <returns>The resolved local index, or <c>null</c> if it could not be determined.</returns>
    private static int? TryGetMacroLocalIndex(CilInstruction instruction) => instruction.OpCode.Code switch
    {
        CilCode.Ldloc_0 or CilCode.Stloc_0 => 0,
        CilCode.Ldloc_1 or CilCode.Stloc_1 => 1,
        CilCode.Ldloc_2 or CilCode.Stloc_2 => 2,
        CilCode.Ldloc_3 or CilCode.Stloc_3 => 3,
        _ => null
    };
}
