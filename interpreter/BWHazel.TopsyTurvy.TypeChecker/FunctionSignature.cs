using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.TypeChecker;

/// <summary>
/// Represents the compile-time signature of a Topsy Turvy function.
/// </summary>
/// <param name="ParameterTypes">The declared type of each parameter, in declaration order.</param>
/// <param name="ReturnType">The declared return type, or <c>null</c> for void functions.</param>
public record FunctionSignature(
    IReadOnlyList<LiteralType> ParameterTypes,
    LiteralType? ReturnType);
