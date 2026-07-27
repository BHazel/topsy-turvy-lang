using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.TypeChecker;

/// <summary>
/// Holds the type information derived from a successful type-check pass.
/// </summary>
/// <remarks>
/// <para>
/// The semantic model is populated by <see cref="TypeCheckVisitor"/> and exposed as part of
/// <see cref="TypeCheckResult"/>.  It maps:
/// * Expression node identity to inferred <see cref="LiteralType"/>.
/// * Symbol names to their declared types.
/// * Function names to their compiled-time signatures, grouped into overload sets.
/// All lookups return <c>null</c>, or an empty list for an overload set, when the requested item
/// is unknown, e.g. the expression has no inferred type, the symbol was not declared or the
/// function was not found.
/// </para>
/// </remarks>
public sealed class SemanticModel
{
    private readonly ConditionalWeakTable<Expression, StrongBox<LiteralType?>> expressionTypes = [];
    private readonly Dictionary<string, LiteralType> symbolTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<FunctionSignature>> functionSignatures = new(StringComparer.Ordinal);

    /// <summary>
    /// Records the inferred type for an expression node.
    /// </summary>
    /// <param name="node">The expression node.</param>
    /// <param name="type">The inferred type, or <c>null</c> for void/unresolvable.</param>
    internal void SetExpressionType(Expression node, LiteralType? type) =>
        this.expressionTypes.AddOrUpdate(node, new(type));

    /// <summary>
    /// Records the declared type for a named symbol (variable or parameter).
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <param name="type">The declared type.</param>
    internal void SetSymbolType(string name, LiteralType type) =>
        this.symbolTypes[name] = type;

    /// <summary>
    /// Adds a signature to a function overload set.
    /// </summary>
    /// <param name="name">The function name.</param>
    /// <param name="signature">The function signature to add.</param>
    /// <remarks>
    /// Two or more signatures may share a name; whether that is a legitimate overload or a duplicate
    /// declaration is a call the caller must have already made before adding a second signature under
    /// the same name, since this method itself has no way to distinguish the two.
    /// </remarks>
    internal void AddFunctionSignature(string name, FunctionSignature signature)
    {
        if (!this.functionSignatures.TryGetValue(name, out List<FunctionSignature>? overloads))
        {
            overloads = [];
            this.functionSignatures[name] = overloads;
        }

        overloads.Add(signature);
    }

    /// <summary>
    /// Gets the inferred type for an expression node, or <c>null</c> if not recorded.
    /// </summary>
    /// <param name="node">The expression node to look up.</param>
    /// <returns>The inferred <see cref="LiteralType"/>, or <c>null</c>.</returns>
    public LiteralType? GetExpressionType(Expression node) =>
        this.expressionTypes.TryGetValue(node, out StrongBox<LiteralType?>? box)
            ? box.Value
            : null;

    /// <summary>
    /// Gets the declared type for a named symbol, or <c>null</c> if not recorded.
    /// </summary>
    /// <param name="name">The symbol name to look up.</param>
    /// <returns>The declared <see cref="LiteralType"/>, or <c>null</c>.</returns>
    public LiteralType? GetSymbolType(string name) =>
        this.symbolTypes.TryGetValue(name, out LiteralType type)
            ? type
            : null;

    /// <summary>
    /// Gets every overload bound under a function name.
    /// </summary>
    /// <param name="name">The function name to look up.</param>
    /// <returns>Every <see cref="FunctionSignature"/> declared under that name, or an empty list if none exist.</returns>
    public IReadOnlyList<FunctionSignature> GetFunctionSignatures(string name) =>
        this.functionSignatures.TryGetValue(name, out List<FunctionSignature>? overloads)
            ? overloads
            : [];

    /// <summary>
    /// Gets the signature for a function, or <c>null</c> if not found or if more than one overload is declared under that name.
    /// </summary>
    /// <param name="name">The function name to look up.</param>
    /// <returns>The <see cref="FunctionSignature"/>, or <c>null</c>.</returns>
    /// <remarks>
    /// Callers that need to resolve a specific call site against a possible overload set should use
    /// <see cref="GetFunctionSignatures"/> instead; this method only ever answers unambiguously for a
    /// name with exactly one declared signature.
    /// </remarks>
    public FunctionSignature? GetFunctionSignature(string name)
    {
        IReadOnlyList<FunctionSignature> overloads = this.GetFunctionSignatures(name);
        return overloads.Count == 1
            ? overloads[0]
            : null;
    }
}
