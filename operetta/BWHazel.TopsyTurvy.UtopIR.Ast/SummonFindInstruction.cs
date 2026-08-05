using System;
using System.Collections.Generic;
using System.Linq;

namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Calls a returning function and stores its result in a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>summon.find</c> instruction with the format
/// <c>£&lt;name&gt; = summon.find &lt;function&gt;, [term &lt;type&gt;, ...]</c>.  Only a returning
/// function can be called this way.  Each argument is pushed onto the stack with a
/// <see cref="PrenticeInstruction"/>, in the order declared by the function, before the
/// <see cref="SummonFindInstruction"/> itself. <see cref="ParameterTypes"/> carries one entry per
/// parameter, in declaration order, allowing overload resolution but is empty for a parameterless
/// function.
/// </para>
/// <para>
/// For example, the Topsy Turvy call:
/// </para>
/// <code>
/// Poem IS APPOINTED SUMMON GetPoem WITH Name IF YOU PLEASE.
/// </code>
/// <para>
/// lowers to a <see cref="PrenticeInstruction"/> followed by a <see cref="SummonFindInstruction"/>,
/// represented in the AST as:
/// </para>
/// <code>
/// new PrenticeInstruction(Operand: new VariableOperand(new UtopIRVariable("Name")));
/// new SummonFindInstruction(
///     Target: new UtopIRVariable("Poem"),
///     Function: new FunctionReference("GetPoem"),
///     ParameterTypes: [new UtopIRTermType(UtopIRType.Yarn)]);
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// prentice £Name
/// £Poem = summon.find &amp;GetPoem, term yarn
/// </code>
/// <para>
/// A function taking an array parameter renders its <c>term</c> operand using the
/// <c>list.&lt;type&gt;</c> form, e.g. <c>summon &amp;Sort, term list.peer</c> for a function whose sole
/// parameter is declared <c>LITTLE LIST OF PEER</c>.
/// </para>
/// </remarks>
/// <param name="Target">The virtual register that will hold the returned value.</param>
/// <param name="Function">The function to call.</param>
/// <param name="ParameterTypes">The declared type of each parameter, in declaration order.</param>
public sealed record SummonFindInstruction(UtopIRVariable Target, FunctionReference Function, IReadOnlyList<UtopIRTermType> ParameterTypes) : UtopIRInstruction
{
    /// <summary>
    /// Determines whether this instance and <paramref name="other"/> represent the same instruction.
    /// </summary>
    /// <remarks>
    /// The compiler-generated record equality for <see cref="ParameterTypes"/> would otherwise compare
    /// by reference rather than by sequence, since <see cref="IReadOnlyList{T}"/> has no built-in value
    /// equality.
    /// </remarks>
    /// <param name="other">The instance to compare against.</param>
    /// <returns><c>true</c> if both instances have an equal <see cref="Target"/> and <see cref="Function"/> and sequence-equal <see cref="ParameterTypes"/>, otherwise <c>false</c>.</returns>
    public bool Equals(SummonFindInstruction? other) =>
        other is not null
            && this.Target == other.Target
            && this.Function == other.Function
            && this.ParameterTypes.SequenceEqual(other.ParameterTypes);

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(SummonFindInstruction)"/>.
    /// </summary>
    /// <returns>The computed hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(this.Target);
        hash.Add(this.Function);
        foreach (UtopIRTermType parameterType in this.ParameterTypes)
        {
            hash.Add(parameterType);
        }

        return hash.ToHashCode();
    }
}
