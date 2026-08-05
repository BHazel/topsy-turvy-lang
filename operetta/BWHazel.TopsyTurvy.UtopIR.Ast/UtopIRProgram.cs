using System;
using System.Collections.Generic;
using System.Linq;

namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// The root node of a UtopIR programme.
/// </summary>
/// <remarks>
/// A <see cref="UtopIRProgram"/> is the result of transforming a Topsy Turvy AST or parsing a
/// UtopIR source file. Every programme contains at least one <see cref="UtopIRFunctionDefinition"/>
/// named <c>Opera</c>, the implicit entry point wrapping every top-level Topsy Turvy statement.  All
/// user-declared functions appear alongside it.  Within each function, its
/// <see cref="UtopIRFunctionDefinition.Body"/> is executed top-to-bottom.
/// </remarks>
/// <param name="Functions">The ordered list of function definitions that make up the programme, including the implicit <c>Opera</c> entry point.</param>
public sealed record UtopIRProgram(IReadOnlyList<UtopIRFunctionDefinition> Functions)
{
    /// <summary>
    /// Determines whether this instance and <paramref name="other"/> represent the same programme.
    /// </summary>
    /// <remarks>
    /// The compiler-generated record equality for <see cref="Functions"/> would otherwise compare by
    /// reference rather than by sequence, since <see cref="IReadOnlyList{T}"/> has no built-in value
    /// equality.
    /// </remarks>
    /// <param name="other">The UtopIR programme instance to compare against.</param>
    /// <returns><c>true</c> if both instances have sequence-equal <see cref="Functions"/>, otherwise <c>false</c>.</returns>
    public bool Equals(UtopIRProgram? other) =>
        other is not null && this.Functions.SequenceEqual(other.Functions);

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(UtopIRProgram)"/>.
    /// </summary>
    /// <returns>The computed hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (UtopIRFunctionDefinition function in this.Functions)
        {
            hash.Add(function);
        }

        return hash.ToHashCode();
    }
}
