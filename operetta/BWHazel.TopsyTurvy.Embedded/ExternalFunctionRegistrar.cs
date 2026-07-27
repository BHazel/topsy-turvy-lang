using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;

namespace BWHazel.TopsyTurvy.Embedded;

/// <summary>
/// Adds Standard Library and external library functions to a file symbol table.
/// </summary>
/// <remarks>
/// <para>
/// A symbol table only knows about the functions actually written in the file it was built from. This class
/// supplements it with every function described by a <see cref="BindingCatalogue"/>, so hover and completion
/// treat any Standard Library or external library function the same way they treat a function the user wrote
/// themselves. Each function is added under its own name; a name already taken by a real Topsy Turvy function
/// is left alone, since a function declared in the file always takes priority over one of the same name from
/// a library.
/// </para>
/// <code>
/// SymbolTable symbolTable = SymbolTable.Build(program, source);
/// IReadOnlyList&lt;string&gt; shadowedFunctions = ExternalFunctionRegistrar.Register(symbolTable);
/// </code>
/// </remarks>
internal static class ExternalFunctionRegistrar
{
    private const string DocumentationResourceName = "BWHazel.TopsyTurvy.StandardLibrary.xml";

    private static readonly XmlDocumentationIndex? DocumentationIndex = LoadDocumentationIndex();

    /// <summary>
    /// Adds every function in <paramref name="externalFunctions"/> to the given symbol table.
    /// </summary>
    /// <param name="symbolTable">The symbol table to seed.</param>
    /// <param name="externalFunctions">The catalogue of external functions to add, defaulting to <see cref="BindingCatalogue.Default"/>.</param>
    /// <returns>The names of any external functions shadowed by an existing symbol of the same name.</returns>
    internal static IReadOnlyList<string> Register(SymbolTable symbolTable, BindingCatalogue? externalFunctions = null)
    {
        if (DocumentationIndex is null)
        {
            return [];
        }

        List<string> shadowedFunctions = [];
        foreach (BoundFunctionDescriptor descriptor in (externalFunctions ?? BindingCatalogue.Default).Functions)
        {
            DocumentationComment documentation = XmlDocumentationMapper.Map(DocumentationIndex, descriptor);
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
    /// Loads the Standard Library XML documentation embedded in this assembly.
    /// </summary>
    /// <returns>The loaded index, or <c>null</c> if the resource could not be found.</returns>
    /// <remarks>
    /// A Native AOT binary has no reliable path to read a file beside its own assembly at runtime,
    /// therefore the documentation is embedded as a resource in this project instead and read back via
    /// <see cref="Assembly.GetManifestResourceStream(string)"/>.
    /// </remarks>
    private static XmlDocumentationIndex? LoadDocumentationIndex()
    {
        using Stream? stream = typeof(ExternalFunctionRegistrar).Assembly.GetManifestResourceStream(DocumentationResourceName);
        return stream is null
            ? null
            : XmlDocumentationMapper.Load(stream);
    }
}
