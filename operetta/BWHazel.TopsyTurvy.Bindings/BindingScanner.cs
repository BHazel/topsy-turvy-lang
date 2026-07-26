using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Sdk.Interop.Bindings;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.Bindings;

/// <summary>
/// Discovers <see cref="BoundFunctionDescriptor"/> instances from <see cref="TopsyTurvyFunctionAttribute"/>-decorated methods.
/// </summary>
/// <remarks>
/// The scanner looks for methods carrying <see cref="TopsyTurvyFunctionAttribute"/> and turns each one into a
/// <see cref="BoundFunctionDescriptor"/> that the rest of the toolchain can work with, without needing to touch
/// reflection itself.  A method without the attribute is simply not a Topsy Turvy function and is skipped; a
/// method that has the attribute but is shaped wrongly, for example an instance method, or a parameter type with
/// no Topsy Turvy equivalent, fails immediately with a <see cref="BindingCatalogueException"/> naming the
/// problem, rather than surfacing later as a confusing failure somewhere else in the toolchain.  <see cref="Scan(Type)"/>
/// and <see cref="ScanAssembly(Assembly)"/> are the two ways to point the scanner at some code: one at a single
/// known class, one across everything in an assembly.
/// </remarks>
public static class BindingScanner
{
    /// <summary>
    /// The CLR types the host injects into trailing bound-method parameters, invisible to Topsy Turvy code.
    /// </summary>
    private static readonly IReadOnlyList<Type> HostInjectedServiceTypes = [typeof(ITopsyTurvyIO)];

    /// <summary>
    /// Scans a binding class for <see cref="TopsyTurvyFunctionAttribute"/>-decorated methods.
    /// </summary>
    /// <param name="bindingClass">The binding class to scan, typically a <c>typeof(SomeClass)</c> literal.</param>
    /// <returns>The descriptors discovered on the class.</returns>
    /// <remarks>
    /// This is the path used for the Standard Library and any other binding class known at compile time. It is
    /// safe under Native AOT trimming, a build step that can delete code it cannot prove is used, because a
    /// <c>typeof</c> expression is always recognised and kept regardless of that step, and the attribute on the
    /// parameter of this method tells the trimmer to also keep the methods of that class, so reflecting over them
    /// later still works even after trimming has run.
    /// </remarks>
    /// <exception cref="BindingCatalogueException">Thrown when a bound method violates one of the scanner rules.</exception>
    public static IReadOnlyList<BoundFunctionDescriptor> Scan(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type bindingClass) =>
        ScanType(bindingClass);

    /// <summary>
    /// Scans every type in an assembly for <see cref="TopsyTurvyFunctionAttribute"/>-decorated methods.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The descriptors discovered across every type in the assembly.</returns>
    /// <exception cref="BindingCatalogueException">Thrown when a bound method violates one of the scanner rules.</exception>
    /// <remarks>
    /// This is the path used to discover functions in a dynamically loaded external library assembly. Unlike
    /// <see cref="Scan(Type)"/>, the assembly is not known until runtime, so the trimmer has no way to know its
    /// methods are needed; this must never be called from a Native AOT host.
    /// </remarks>
    [RequiresUnreferencedCode("Scans the types of an assembly reflectively; those types are only known at runtime, so the trimmer has no way to know in advance that they are used and may remove them as unused.")]
    public static IReadOnlyList<BoundFunctionDescriptor> ScanAssembly(Assembly assembly)
    {
        List<BoundFunctionDescriptor> descriptors = [];
        foreach (Type type in assembly.GetTypes())
        {
            descriptors.AddRange(ScanType(type));
        }

        return descriptors;
    }

    /// <summary>
    /// Scans a single type for <see cref="TopsyTurvyFunctionAttribute"/>-decorated methods.
    /// </summary>
    /// <param name="type">The type to scan.</param>
    /// <returns>The descriptors discovered on the type.</returns>
    /// <remarks>
    /// Methods are enumerated regardless of accessibility or whether they are static, so that a bound method violating the rule
    /// that methods must be public static on a public class is caught as a scan-time error rather than being silently skipped by a
    /// visibility filter on the reflection query itself.
    /// </remarks>
    /// <exception cref="BindingCatalogueException">Thrown when a bound method violates one of the scanner rules.</exception>
    private static IReadOnlyList<BoundFunctionDescriptor> ScanType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type)
    {
        List<BoundFunctionDescriptor> descriptors = [];
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            TopsyTurvyFunctionAttribute? binding = method.GetCustomAttribute<TopsyTurvyFunctionAttribute>();
            if (binding is null)
            {
                continue;
            }

            if (!type.IsPublic && !type.IsNestedPublic)
            {
                throw new BindingCatalogueException($"Binding class '{type.FullName}' must be public.");
            }

            descriptors.Add(DescribeMethod(method, binding));
        }

        return descriptors;
    }

    /// <summary>
    /// Builds a <see cref="BoundFunctionDescriptor"/> from a bound method and its attribute, applying the normative
    /// rules of the scanner.
    /// </summary>
    /// <param name="method">The bound method.</param>
    /// <param name="binding">The <see cref="TopsyTurvyFunctionAttribute"/> of the method.</param>
    /// <returns>The resulting descriptor.</returns>
    /// <exception cref="BindingCatalogueException">Thrown when the method violates one of the scanner rules.</exception>
    private static BoundFunctionDescriptor DescribeMethod(MethodInfo method, TopsyTurvyFunctionAttribute binding)
    {
        if (!method.IsStatic || !method.IsPublic)
        {
            throw new BindingCatalogueException($"Bound method '{method.DeclaringType?.FullName}.{method.Name}' must be public static.");
        }

        string name = binding.Name ?? method.Name;
        if (!IsValidIdentifier(name))
        {
            throw new BindingCatalogueException($"Bound method '{method.DeclaringType?.FullName}.{method.Name}' has an invalid Topsy Turvy name '{name}'.");
        }

        ParameterInfo[] clrParameters = method.GetParameters();
        int hostInjectedParameterCount = 0;
        List<BoundParameter> parameters = [];
        bool seenHostInjected = false;

        foreach (ParameterInfo clrParameter in clrParameters)
        {
            bool isHostInjected = HostInjectedServiceTypes.Contains(clrParameter.ParameterType);
            if (isHostInjected)
            {
                seenHostInjected = true;
                hostInjectedParameterCount++;
                continue;
            }

            if (seenHostInjected)
            {
                throw new BindingCatalogueException(
                    $"Bound method '{method.DeclaringType?.FullName}.{method.Name}' has a bound parameter '{clrParameter.Name}' after a host-injected parameter; host-injected parameters must trail.");
            }

            LiteralType parameterType;
            LiteralType? parameterElementType = null;
            if (ClrTypeMap.TryGetArrayElementLiteralType(clrParameter.ParameterType, out LiteralType arrayElementType))
            {
                parameterType = LiteralType.Array;
                parameterElementType = arrayElementType;
            }
            else if (ClrTypeMap.TryGetLiteralType(clrParameter.ParameterType, out LiteralType mappedParameterType))
            {
                parameterType = mappedParameterType;
            }
            else
            {
                throw new BindingCatalogueException(
                    $"Bound method '{method.DeclaringType?.FullName}.{method.Name}' has a parameter '{clrParameter.Name}' of unmapped type '{clrParameter.ParameterType}'.");
            }

            string parameterName = clrParameter.GetCustomAttribute<TopsyTurvyParameterAttribute>()?.ParameterName ?? clrParameter.Name!;
            if (!IsValidIdentifier(parameterName))
            {
                throw new BindingCatalogueException(
                    $"Bound method '{method.DeclaringType?.FullName}.{method.Name}' has an invalid parameter name '{parameterName}'.");
            }

            parameters.Add(new(parameterName, parameterType, clrParameter.ParameterType, parameterElementType));
        }

        LiteralType? returnType = null;
        LiteralType? returnElementType = null;
        if (method.ReturnType != typeof(void))
        {
            if (ClrTypeMap.TryGetArrayElementLiteralType(method.ReturnType, out LiteralType arrayReturnElementType))
            {
                returnType = LiteralType.Array;
                returnElementType = arrayReturnElementType;
            }
            else if (ClrTypeMap.TryGetLiteralType(method.ReturnType, out LiteralType mappedReturnType))
            {
                returnType = mappedReturnType;
            }
            else
            {
                throw new BindingCatalogueException(
                    $"Bound method '{method.DeclaringType?.FullName}.{method.Name}' has an unmapped return type '{method.ReturnType}'.");
            }
        }

        string? qualifiedNamespace = JoinNamespace(method, binding.Namespace);

        return new(
            method,
            name,
            qualifiedNamespace,
            binding.IsPreview,
            binding.KeywordAnalogue,
            hostInjectedParameterCount,
            parameters,
            returnType,
            returnElementType);
    }

    /// <summary>
    /// Joins a bound method namespace path segments into the toolchain internal dot-joined form.
    /// </summary>
    /// <param name="method">The bound method, used only to name the offending method in a validation error.</param>
    /// <param name="segments">The namespace path segments from <see cref="TopsyTurvyFunctionAttribute.Namespace"/>.</param>
    /// <returns>The dot-joined namespace, or <c>null</c> for the global namespace.</returns>
    /// <remarks>
    /// This is the one place a segment list becomes the internal joined string <see cref="BindingCatalogue"/> keys
    /// on; a binding author never constructs that string directly.  <c>null</c> and an empty array are treated
    /// identically, both meaning the global namespace.
    /// </remarks>
    /// <exception cref="BindingCatalogueException">Thrown when a segment is not a valid Topsy Turvy identifier.</exception>
    private static string? JoinNamespace(MethodInfo method, string[]? segments)
    {
        if (segments is null || segments.Length == 0)
        {
            return null;
        }

        foreach (string segment in segments)
        {
            if (!IsValidIdentifier(segment))
            {
                throw new BindingCatalogueException(
                    $"Bound method '{method.DeclaringType?.FullName}.{method.Name}' has an invalid namespace segment '{segment}'.");
            }
        }

        return string.Join('.', segments);
    }

    /// <summary>
    /// Checks whether a name is lexically valid as a Topsy Turvy identifier.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns><c>true</c> if the name is a valid identifier shape, otherwise <c>false</c>.</returns>
    /// <remarks>
    /// This checks lexical form only (a leading letter, followed by letters, digits, hyphens or underscores).
    /// Reserved-keyword collision is checked at catalogue level by callers that can see <c>KeywordData</c>.
    /// </remarks>
    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsLetter(name[0]))
        {
            return false;
        }

        return name.Skip(1).All(character => char.IsLetterOrDigit(character) || character == '-' || character == '_');
    }
}
