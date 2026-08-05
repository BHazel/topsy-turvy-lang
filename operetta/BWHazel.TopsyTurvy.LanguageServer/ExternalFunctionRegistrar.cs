using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Adds Standard Library and external library functions to a document symbol table.
/// </summary>
/// <remarks>
/// <para>
/// A symbol table only knows about the functions actually written in the document it was built from. This class
/// supplements it with every function described by a <see cref="BindingCatalogue"/>, so hover, completion and
/// signature help treat any Standard Library or external library function, the same way they
/// treat a function the user wrote themselves.  Each function is added under its own name; a name
/// already taken by a real Topsy Turvy function is left alone, since a function declared in the document always
/// takes priority over one of the same name from a library.
/// </para>
/// <code>
/// SymbolTable symbolTable = SymbolTable.Build(program, source);
/// IReadOnlyList&lt;string&gt; shadowedFunctions = ExternalFunctionRegistrar.Register(symbolTable);
/// </code>
/// </remarks>
internal static class ExternalFunctionRegistrar
{
    private static readonly ConcurrentDictionary<Assembly, XmlDocumentationIndex?> DocumentationIndexesByAssembly = new();

    /// <summary>
    /// Adds every function in <paramref name="externalFunctions"/> to the given symbol table.
    /// </summary>
    /// <param name="symbolTable">The symbol table to seed.</param>
    /// <param name="externalFunctions">The catalogue of external functions to add, defaulting to <see cref="BindingCatalogue.Default"/>.</param>
    /// <returns>The names of any external functions shadowed by an existing symbol of the same name.</returns>
    internal static IReadOnlyList<string> Register(SymbolTable symbolTable, BindingCatalogue? externalFunctions = null)
    {
        List<string> shadowedFunctions = [];
        foreach (BoundFunctionDescriptor descriptor in (externalFunctions ?? BindingCatalogue.Default).Functions)
        {
            DocumentationComment documentation = MapDocumentation(descriptor);
            IReadOnlyList<(string Name, LiteralType Type, LiteralType? ArrayElementType)> parameters = [.. descriptor.Parameters
                .Select(parameter => (parameter.Name, parameter.Type, parameter.ArrayElementType))];

            if (!symbolTable.AddExternalFunction(descriptor.Name, parameters, descriptor.ReturnType, descriptor.ReturnElementType, documentation))
            {
                shadowedFunctions.Add(descriptor.Name);
            }
        }

        return shadowedFunctions;
    }

    /// <summary>
    /// Maps a bound function documentation, loading the XML documentation of its own declaring assembly rather
    /// than assuming every function comes from the Standard Library.
    /// </summary>
    /// <param name="descriptor">The bound function to map documentation for.</param>
    /// <returns>The mapped documentation, or an empty one if its declaring assembly has no XML documentation available.</returns>
    private static DocumentationComment MapDocumentation(BoundFunctionDescriptor descriptor)
    {
        Assembly declaringAssembly = descriptor.Method.DeclaringType!.Assembly;
        XmlDocumentationIndex? index = DocumentationIndexesByAssembly.GetOrAdd(declaringAssembly, LoadDocumentationIndex);
        return index is null
            ? new()
            : XmlDocumentationMapper.Map(index, descriptor);
    }

    /// <summary>
    /// Loads the XML documentation file of a bound function assembly from beside it on disk.
    /// </summary>
    /// <param name="assembly">The assembly to load documentation for.</param>
    /// <returns>The loaded index, or <c>null</c> if the file could not be found or read.</returns>
    /// <remarks>
    /// The language server is a normal desktop process, so any assembly it references has a real path on disk, and
    /// the compiler-generated XML documentation file sits right beside it under the same name with a <c>.xml</c>
    /// extension. When the language server itself is published as a single file, an assembly bundled inside it
    /// reports an empty <see cref="Assembly.Location"/>, so the path is instead built from
    /// <see cref="AppContext.BaseDirectory"/> plus the assembly simple name, matching where the documentation file
    /// is copied alongside the published executable.
    /// </remarks>
    private static XmlDocumentationIndex? LoadDocumentationIndex(Assembly assembly)
    {
        string documentationPath = string.IsNullOrEmpty(assembly.Location)
            ? Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml")
            : Path.ChangeExtension(assembly.Location, ".xml");

        if (!File.Exists(documentationPath))
        {
            return null;
        }

        using FileStream stream = File.OpenRead(documentationPath);
        return XmlDocumentationMapper.Load(stream);
    }
}
