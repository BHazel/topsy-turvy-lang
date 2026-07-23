using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BWHazel.TopsyTurvy.StandardLibrary;

namespace BWHazel.TopsyTurvy.Bindings;

/// <summary>
/// An immutable, collision-checked set of bound function descriptors.
/// </summary>
/// <remarks>
/// Each descriptor is keyed by its fully-qualified name.  If two descriptors end up with the same key,
/// whether from the same binding class, different classes or two merged catalogues, construction throws a
/// <see cref="BindingCatalogueException"/> naming both sources rather than silently keeping one.  Lookup is
/// case-insensitive.
/// </remarks>
public sealed class BindingCatalogue
{
    private readonly IReadOnlyDictionary<string, BoundFunctionDescriptor> descriptorsByKey;

    /// <summary>
    /// Initialises a new instance of the <see cref="BindingCatalogue"/> class.
    /// </summary>
    /// <param name="functions">The descriptors held by the catalogue.</param>
    /// <param name="descriptorsByKey">The descriptors indexed by their resolver-convention key.</param>
    private BindingCatalogue(IReadOnlyList<BoundFunctionDescriptor> functions, IReadOnlyDictionary<string, BoundFunctionDescriptor> descriptorsByKey)
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
    public static BindingCatalogue Default { get; } = Merge(FromScan(BindingScanner.Scan(typeof(Global))));

    /// <summary>
    /// Builds a catalogue directly from an already-scanned descriptor list.
    /// </summary>
    /// <param name="functions">The descriptors to build the catalogue from.</param>
    /// <returns>The resulting catalogue.</returns>
    /// <exception cref="BindingCatalogueException">Thrown when two descriptors share a key.</exception>
    /// <remarks>
    /// Lets <see cref="Default"/> build from one or more direct, trim-safe <see cref="BindingScanner.Scan(Type)"/>
    /// calls and combine them with <see cref="Merge"/>, instead of going through <see cref="Create(Type[])"/>.
    /// </remarks>
    private static BindingCatalogue FromScan(IReadOnlyList<BoundFunctionDescriptor> functions) => new(functions, IndexByKey(functions));

    /// <summary>
    /// Gets an empty catalogue.
    /// </summary>
    /// <remarks>
    /// This is intended for hosts that need no external functions.
    /// </remarks>
    public static BindingCatalogue Empty { get; } = new([], new Dictionary<string, BoundFunctionDescriptor>(StringComparer.OrdinalIgnoreCase));

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
    /// Looks up a descriptor by resolved name.
    /// </summary>
    /// <param name="qualifiedName">The fully-qualified name, or bare for global namespace, for
    /// a function, matching the convention of the resolver.</param>
    /// <returns>The matching descriptor, or <c>null</c> if no descriptor has that key.</returns>
    public BoundFunctionDescriptor? Find(string qualifiedName) =>
        this.descriptorsByKey.TryGetValue(qualifiedName, out BoundFunctionDescriptor? descriptor)
            ? descriptor
            : null;

    /// <summary>
    /// Builds a lookup dictionary from a set of descriptors, keyed by qualified name, detecting collisions.
    /// </summary>
    /// <param name="functions">The descriptors to build the lookup from.</param>
    /// <returns>A dictionary mapping each descriptor qualified name to the descriptor itself.</returns>
    /// <remarks>
    /// The key is <see cref="BoundFunctionDescriptor.Namespace"/> plus <see cref="BoundFunctionDescriptor.Name"/>,
    /// for example <c>Accounts.Payroll.CalculateTax</c>, matching how the interpreter and type checker already
    /// identify a qualified function internally. <see cref="BindingScanner"/> builds this dot-joined form before
    /// a descriptor ever reaches this method, so nothing here needs to construct it.
    /// </remarks>
    /// <exception cref="BindingCatalogueException">Thrown when descriptors share a key.</exception>
    private static IReadOnlyDictionary<string, BoundFunctionDescriptor> IndexByKey(IReadOnlyList<BoundFunctionDescriptor> functions)
    {
        Dictionary<string, BoundFunctionDescriptor> descriptorsByKey = new(StringComparer.OrdinalIgnoreCase);
        foreach (BoundFunctionDescriptor function in functions)
        {
            string key = function.Namespace is null
                ? function.Name
                : $"{function.Namespace}.{function.Name}";

            if (descriptorsByKey.TryGetValue(key, out BoundFunctionDescriptor? existing))
            {
                throw new BindingCatalogueException(
                    $"Function '{key}' is bound more than once: '{existing.Method.DeclaringType?.FullName}.{existing.Method.Name}' and '{function.Method.DeclaringType?.FullName}.{function.Method.Name}'.");
            }

            descriptorsByKey[key] = function;
        }

        return descriptorsByKey;
    }
}
