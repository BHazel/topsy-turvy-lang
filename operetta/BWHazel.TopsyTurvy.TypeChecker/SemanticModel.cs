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
/// * Function names to their compiled-time signatures.
/// All lookups return <c>null</c> when the requested item is unknown, e.g. the expression has no
/// inferred type, the symbol was not declared or the function was not found.
/// </para>
/// </remarks>
public sealed class SemanticModel
{
    private readonly ConditionalWeakTable<Expression, StrongBox<LiteralType?>> expressionTypes = [];
    private readonly Dictionary<string, LiteralType> symbolTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FunctionSignature> functionSignatures = new(StringComparer.Ordinal);

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
    /// Records the signature for a function.
    /// </summary>
    /// <param name="name">The function name.</param>
    /// <param name="signature">The function signature.</param>
    internal void SetFunctionSignature(string name, FunctionSignature signature) =>
        this.functionSignatures[name] = signature;

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
    /// Gets the signature for a function, or <c>null</c> if not found.
    /// </summary>
    /// <param name="name">The function name to look up.</param>
    /// <returns>The <see cref="FunctionSignature"/>, or <c>null</c>.</returns>
    public FunctionSignature? GetFunctionSignature(string name) =>
        this.functionSignatures.TryGetValue(name, out FunctionSignature? signature)
            ? signature
            : null;
}
