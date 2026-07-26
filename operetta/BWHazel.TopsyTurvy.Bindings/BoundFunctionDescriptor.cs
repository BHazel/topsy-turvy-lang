using System.Collections.Generic;
using System.Reflection;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Bindings;

/// <summary>
/// Describes a bound function discovered by the <see cref="BindingScanner"/>.
/// </summary>
/// <remarks>
/// This is the one shape every downstream consumer of an external function agrees on:
/// <list type="bullet">
/// <item><description>The interpreter uses it to invoke the method and marshal arguments.</description></item>
/// <item><description>The type checker uses it to seed a signature.</description></item>
/// <item><description>Hover, completion and signature help use it to describe the function to a user.</description></item>
/// </list>
/// Building this record once, at scan time, means none of those consumers ever need to touch <see cref="Method"/> or its
/// attributes directly, so a change to how bindings are declared, for example a new attribute, only ever has one place to update.
/// </remarks>
/// <param name="Method">The implementing CLR method.</param>
/// <param name="Name">The name called from Topsy Turvy code.</param>
/// <param name="Namespace">
/// The dot-joined namespace path, or <c>null</c> for the global namespace. This is the
/// toolchain internal lookup key, already joined by <see cref="BindingScanner"/> from the
/// <c>TopsyTurvyFunctionAttribute.Namespace</c> segment list; nothing outside the scanner
/// constructs this string by hand.
/// </param>
/// <param name="IsPreview">A value indicating whether the function is a preview function.</param>
/// <param name="KeywordAnalogue">The keyword this function mirrors, if any.</param>
/// <param name="HostInjectedParameterCount">The number of trailing host-injected parameters.</param>
/// <param name="Parameters">The Topsy Turvy-visible parameters, host-injected services excluded.</param>
/// <param name="ReturnType">The inferred return type, or <c>null</c> for a void function.</param>
/// <param name="ReturnElementType">The element type when <paramref name="ReturnType"/> is <see cref="LiteralType.Array"/>, otherwise <c>null</c>.</param>
public sealed record BoundFunctionDescriptor(
    MethodInfo Method,
    string Name,
    string? Namespace,
    bool IsPreview,
    string? KeywordAnalogue,
    int HostInjectedParameterCount,
    IReadOnlyList<BoundParameter> Parameters,
    LiteralType? ReturnType,
    LiteralType? ReturnElementType = null);
