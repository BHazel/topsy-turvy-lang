using System;
using System.Collections.Generic;
using System.Linq;

namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Represents a function definition block: the <c>duty</c> opener, its body, and the <c>discharged</c> closer.
/// </summary>
/// <remarks>
/// <para>
/// A function definition is not itself a <see cref="UtopIRInstruction"/> but is a programme-level
/// member, the same tier as <see cref="UtopIRProgram"/> itself.  Every <see cref="UtopIRProgram"/>
/// contains at least one <see cref="UtopIRFunctionDefinition"/> named <c>Opera</c>, the implicit entry
/// point wrapping every top-level Topsy Turvy statement.  User-declared functions
/// (the Topsy Turvy <c>IT IS MY DUTY TO PERFORM</c> ... <c>MY DUTY IS DISCHARGED.</c>) appear
/// alongside it as additional function declarations.
/// </para>
/// <para>
/// <see cref="Name"/> is the fully-qualified function name, without the <c>&amp;</c> prefix: for a
/// function declared under a <c>TOWN</c> namespace, this includes the namespace path joined with
/// <see cref="UtopIRKeywords.NamespaceDelimiter"/>, e.g. <c>Aesthetic*Writing*Greet</c>.
/// </para>
/// <para>
/// <see cref="ReturnType"/> is <c>null</c> for a void function, where there is no <c>finds</c> clause.
/// </para>
/// <para>
/// The format is:
/// </para>
/// <code>
/// duty &amp;&lt;fn-name&gt;, [term &lt;type&gt; %&lt;param-name&gt;, ...] [finds &lt;type&gt;]
///     @ Function instructions here.
/// discharged
/// </code>
/// <para>
/// For example, the following Topsy Turvy function:
/// </para>
/// <code>
/// IT IS MY DUTY TO PERFORM Add UNDER THE TERMS OF Num1 AS A PEER AND Num2 AS A PEER TO FIND PEER
///     AND SO I FIND SUM OF Num1 AND Num2
/// MY DUTY IS DISCHARGED.
/// </code>
/// <para>
/// would be represented in the AST as:
/// </para>
/// <code>
/// new UtopIRFunctionDefinition(
///     Name: "Add",
///     Parameters:
///     [
///         new UtopIRFunctionParameter(new UtopIRTermType(UtopIRType.Peer), "Num1"),
///         new UtopIRFunctionParameter(new UtopIRTermType(UtopIRType.Peer), "Num2")
///     ],
///     ReturnType: new UtopIRTermType(UtopIRType.Peer),
///     Body:
///     [
///         new ArithmeticInstruction(
///             UtopIRArithmeticOperation.Sum,
///             new UtopIRVariable("_sum_Num1_Num2"),
///             new ParameterOperand(new UtopIRParameter("Num1")),
///             new ParameterOperand(new UtopIRParameter("Num2"))),
///         new FindInstruction(new VariableOperand(new UtopIRVariable("_sum_Num1_Num2")))
///     ]);
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// duty &amp;Add, term peer %Num1, term peer %Num2, finds peer
///     £_sum_Num1_Num2 = sum %Num1, %Num2
///     find £_sum_Num1_Num2
/// discharged
/// </code>
/// </remarks>
/// <param name="Name">The fully-qualified function name, without the <c>&amp;</c> prefix.</param>
/// <param name="Parameters">The ordered list of parameters declared in the <c>duty</c> header.</param>
/// <param name="ReturnType">The declared return type, or <c>null</c> for a void function.</param>
/// <param name="Body">The ordered list of instructions in the function body.</param>
public sealed record UtopIRFunctionDefinition(
    string Name,
    IReadOnlyList<UtopIRFunctionParameter> Parameters,
    UtopIRTermType? ReturnType,
    IReadOnlyList<UtopIRInstruction> Body)
{
    /// <summary>
    /// Determines whether this instance and <paramref name="other"/> represent the same function definition.
    /// </summary>
    /// <remarks>
    /// The compiler-generated record equality for <see cref="Parameters"/> and <see cref="Body"/> would
    /// otherwise compare by reference rather than by sequence, since <see cref="IReadOnlyList{T}"/> has
    /// no built-in value equality.
    /// </remarks>
    /// <param name="other">The instance to compare against.</param>
    /// <returns><c>true</c> if both instances have an equal <see cref="Name"/> and <see cref="ReturnType"/>, and sequence-equal <see cref="Parameters"/> and <see cref="Body"/>, otherwise <c>false</c>.</returns>
    public bool Equals(UtopIRFunctionDefinition? other) =>
        other is not null
            && this.Name == other.Name
            && this.ReturnType == other.ReturnType
            && this.Parameters.SequenceEqual(other.Parameters)
            && this.Body.SequenceEqual(other.Body);

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(UtopIRFunctionDefinition)"/>.
    /// </summary>
    /// <returns>The computed hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(this.Name);
        hash.Add(this.ReturnType);
        foreach (UtopIRFunctionParameter parameter in this.Parameters)
        {
            hash.Add(parameter);
        }

        foreach (UtopIRInstruction instruction in this.Body)
        {
            hash.Add(instruction);
        }

        return hash.ToHashCode();
    }
}
