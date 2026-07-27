using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BWHazel.TopsyTurvy.StandardLibrary;

namespace BWHazel.TopsyTurvy.Bindings;

/// <summary>
/// An immutable, collision-checked set of bound function descriptors, grouped into overload sets by fully-qualified name.
/// </summary>
/// <remarks>
/// Each descriptor is grouped under its fully-qualified name; two descriptors under the same name coexist as
/// overloads as long as their parameter types differ. If two descriptors end up with the same name **and** the
/// same parameter types, whether from the same binding class, different classes or two merged catalogues,
/// construction throws a <see cref="BindingCatalogueException"/> naming both sources rather than silently keeping
/// one. Lookup is case-insensitive.
/// </remarks>
public sealed class BindingCatalogue
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<BoundFunctionDescriptor>> descriptorsByKey;

    /// <summary>
    /// Initialises a new instance of the <see cref="BindingCatalogue"/> class.
    /// </summary>
    /// <param name="functions">The descriptors held by the catalogue.</param>
    /// <param name="descriptorsByKey">The descriptors indexed by their resolver-convention key, grouped into overload sets.</param>
    private BindingCatalogue(IReadOnlyList<BoundFunctionDescriptor> functions, IReadOnlyDictionary<string, IReadOnlyList<BoundFunctionDescriptor>> descriptorsByKey)
    {
        this.Functions = functions;
        this.descriptorsByKey = descriptorsByKey;
    }

    /// <summary>
    /// Gets the default catalogue containing the Standard Library binding classes.
    /// </summary>
    /// <remarks>
    /// This is built once and cached.  Adding a binding class to the library means adding one more
    /// <see cref="BindingScanner.Scan(Type)"/> call to the <see cref="Merge"/> call below.
    /// <c>typeof</c> literals are scanned this way, rather than via <see cref="Create(Type[])"/>, because a
    /// Native AOT publish trims away every Standard Library function reachable via
    /// <c>SUMMON</c>: routing a <c>typeof</c> literal through the <see cref="Create(Type[])"/> <c>params
    /// Type[]</c> parameter breaks the trimmer ability to verify <see cref="BindingScanner.Scan(Type)"/>
    /// single-<see cref="Type"/> parameter is safe to reflect over, so it silently scanned nothing.  Calling
    /// <see cref="BindingScanner.Scan(Type)"/> directly here keeps that guarantee intact.
    /// </remarks>
    public static BindingCatalogue Default { get; } = Merge(FromDescriptors(BindingScanner.Scan(typeof(Global))));

    /// <summary>
    /// Builds a catalogue directly from an already-scanned descriptor list.
    /// </summary>
    /// <param name="functions">The descriptors to build the catalogue from.</param>
    /// <returns>The resulting catalogue.</returns>
    /// <exception cref="BindingCatalogueException">Thrown when two descriptors share a key.</exception>
    /// <remarks>
    /// Lets <see cref="Default"/> build from one or more direct, trim-safe <see cref="BindingScanner.Scan(Type)"/>
    /// calls and combine them with <see cref="Merge"/>, instead of going through <see cref="Create(Type[])"/>.
    /// Also used by <c>ExternalLibraryLoader</c> to build a catalogue from a third-party assembly
    /// <see cref="BindingScanner.ScanAssembly(System.Reflection.Assembly)"/> result.
    /// </remarks>
    public static BindingCatalogue FromDescriptors(IReadOnlyList<BoundFunctionDescriptor> functions) => new(functions, IndexByKey(functions));

    /// <summary>
    /// Gets an empty catalogue.
    /// </summary>
    /// <remarks>
    /// This is intended for hosts that need no external functions.
    /// </remarks>
    public static BindingCatalogue Empty { get; } = new([], new Dictionary<string, IReadOnlyList<BoundFunctionDescriptor>>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all descriptors held by the catalogue.
    /// </summary>
    public IReadOnlyList<BoundFunctionDescriptor> Functions { get; }

    /// <summary>
    /// Builds a catalogue by scanning the given binding classes.
    /// </summary>
    /// <param name="bindingClasses">The binding classes to scan.</param>
    /// <returns>The resulting catalogue.</returns>
    /// <exception cref="BindingCatalogueException">Thrown when a scan error occurred, or two descriptors share a key.</exception>
    /// <remarks>
    /// The suppression below exists because this method takes several classes at once as a plain <c>Type[]</c>,
    /// which cannot carry the same Native AOT trimming guarantee <see cref="BindingScanner.Scan(Type)"/> gets from
    /// annotating its single <c>Type</c> parameter.  In practice this is safe: every call site in this codebase
    /// passes <c>typeof(SomeClass)</c> literals directly and those are always kept by the trimmer regardless of
    /// any attribute.
    /// </remarks>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072",
        Justification = "Type[] cannot carry DynamicallyAccessedMembers annotations; every call site in this codebase passes typeof(...) literals directly, and the trimmer always keeps a type referenced this way regardless of any annotation.")]
    public static BindingCatalogue Create(params Type[] bindingClasses)
    {
        List<BoundFunctionDescriptor> functions = [];
        foreach (Type bindingClass in bindingClasses)
        {
            functions.AddRange(BindingScanner.Scan(bindingClass));
        }

        return new(functions, IndexByKey(functions));
    }

    /// <summary>
    /// Combines catalogues, for example the default catalogue plus external library catalogues.
    /// </summary>
    /// <param name="catalogues">The catalogues to combine.</param>
    /// <returns>A catalogue containing every descriptor from every source catalogue.</returns>
    /// <exception cref="BindingCatalogueException">Thrown when two descriptors from different catalogues share a key.</exception>
    public static BindingCatalogue Merge(params BindingCatalogue[] catalogues)
    {
        List<BoundFunctionDescriptor> functions = [.. catalogues.SelectMany(catalogue => catalogue.Functions)];
        return new(functions, IndexByKey(functions));
    }

    /// <summary>
    /// Looks up every overload bound under a resolved name.
    /// </summary>
    /// <param name="qualifiedName">The fully-qualified name, or bare for global namespace, for a function, matching the convention of the resolver.</param>
    /// <returns>Every descriptor bound under that name, or an empty list if none exist.</returns>
    /// <remarks>
    /// A caller resolving an actual call site should filter this list by the call argument types; this method
    /// itself does not know what arguments, if any, the caller has.
    /// </remarks>
    public IReadOnlyList<BoundFunctionDescriptor> FindAll(string qualifiedName) =>
        this.descriptorsByKey.TryGetValue(qualifiedName, out IReadOnlyList<BoundFunctionDescriptor>? descriptors)
            ? descriptors
            : [];

    /// <summary>
    /// Looks up the single descriptor bound under a resolved name.
    /// </summary>
    /// <param name="qualifiedName">The fully-qualified name, or bare for global namespace, for
    /// a function, matching the convention of the resolver.</param>
    /// <returns>The matching descriptor, or <c>null</c> if no descriptor has that key.</returns>
    /// <exception cref="BindingCatalogueException">Thrown when more than one overload is bound under that name; call <see cref="FindAll"/> and resolve by argument type instead.</exception>
    public BoundFunctionDescriptor? Find(string qualifiedName)
    {
        IReadOnlyList<BoundFunctionDescriptor> candidates = this.FindAll(qualifiedName);
        return candidates.Count switch
        {
            0 => null,
            1 => candidates[0],
            _ => throw new BindingCatalogueException(
                $"Function '{qualifiedName}' has {candidates.Count} overloads; resolving by name alone is ambiguous. Use {nameof(FindAll)} and resolve by argument type instead.")
        };
    }

    /// <summary>
    /// Builds a lookup dictionary from a set of descriptors, grouped into overload sets by qualified name,
    /// detecting a same-name, same-parameter-types collision.
    /// </summary>
    /// <param name="functions">The descriptors to build the lookup from.</param>
    /// <returns>A dictionary mapping each qualified name to its overload set.</returns>
    /// <remarks>
    /// The key is <see cref="BoundFunctionDescriptor.Namespace"/> plus <see cref="BoundFunctionDescriptor.Name"/>,
    /// for example <c>Accounts.Payroll.CalculateTax</c>, matching how the interpreter and type checker already
    /// identify a qualified function internally. <see cref="BindingScanner"/> builds this dot-joined form before
    /// a descriptor ever reaches this method, so nothing here needs to construct it.
    /// </remarks>
    /// <exception cref="BindingCatalogueException">Thrown when two descriptors share both a key and parameter types.</exception>
    private static IReadOnlyDictionary<string, IReadOnlyList<BoundFunctionDescriptor>> IndexByKey(IReadOnlyList<BoundFunctionDescriptor> functions)
    {
        Dictionary<string, List<BoundFunctionDescriptor>> overloadsByKey = new(StringComparer.OrdinalIgnoreCase);
        foreach (BoundFunctionDescriptor function in functions)
        {
            string key = function.Namespace is null
                ? function.Name
                : $"{function.Namespace}.{function.Name}";

            if (!overloadsByKey.TryGetValue(key, out List<BoundFunctionDescriptor>? overloads))
            {
                overloads = [];
                overloadsByKey[key] = overloads;
            }

            BoundFunctionDescriptor? duplicate = overloads.Find(existing => HasSameParameterTypes(existing, function));
            if (duplicate is not null)
            {
                throw new BindingCatalogueException(
                    $"Function '{key}' is bound more than once with the same parameter types: '{duplicate.Method.DeclaringType?.FullName}.{duplicate.Method.Name}' and '{function.Method.DeclaringType?.FullName}.{function.Method.Name}'.");
            }

            overloads.Add(function);
        }

        return overloadsByKey.ToDictionary(
            pair => pair.Key,
            IReadOnlyList<BoundFunctionDescriptor> (pair) => pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether two descriptors have the same parameter list, comparing both the parameter type and,
    /// for an array parameter, its element type.
    /// </summary>
    /// <param name="first">The first descriptor.</param>
    /// <param name="second">The second descriptor.</param>
    /// <returns><c>true</c> if every parameter matches in order, otherwise <c>false</c>.</returns>
    private static bool HasSameParameterTypes(BoundFunctionDescriptor first, BoundFunctionDescriptor second)
    {
        if (first.Parameters.Count != second.Parameters.Count)
        {
            return false;
        }

        for (int i = 0; i < first.Parameters.Count; i++)
        {
            BoundParameter firstParameter = first.Parameters[i];
            BoundParameter secondParameter = second.Parameters[i];
            if (firstParameter.Type != secondParameter.Type || firstParameter.ArrayElementType != secondParameter.ArrayElementType)
            {
                return false;
            }
        }

        return true;
    }
}
