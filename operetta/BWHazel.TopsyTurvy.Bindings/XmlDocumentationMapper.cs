using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Bindings;

/// <summary>
/// Maps compiler-generated XML documentation onto <see cref="BoundFunctionDescriptor"/> instances.
/// </summary>
/// <remarks>
/// The documentation ID for a bound method is built mechanically from its <see cref="MethodInfo"/>:
/// <c>"M:" + declaringType.FullName + "." + method.Name + "(" + comma-joined parameter type full names + ")"</c>,
/// omitting the parentheses entirely for a parameterless method.  Host-injected parameters are included in that ID,
/// since it reflects the real CLR signature, but are left out of the mapped <c>Parameters</c>, since a Topsy Turvy
/// caller never sees them either.
/// </remarks>
public static class XmlDocumentationMapper
{
    /// <summary>
    /// Loads and indexes a compiler-generated XML documentation file.
    /// </summary>
    /// <param name="documentationXml">The XML documentation content.  The location is host-dependent, so the mapper
    /// takes a stream, never a path.</param>
    /// <returns>The indexed documentation.</returns>
    public static XmlDocumentationIndex Load(Stream documentationXml)
    {
        XDocument document = XDocument.Load(documentationXml);
        Dictionary<string, XElement> membersById = new(StringComparer.Ordinal);
        foreach (XElement member in document.Descendants("member"))
        {
            string? name = member.Attribute("name")?.Value;
            if (name is not null)
            {
                membersById[name] = member;
            }
        }

        return new(membersById);
    }

    /// <summary>
    /// Maps the documentation for one bound function.
    /// </summary>
    /// <param name="index">The loaded documentation index.</param>
    /// <param name="descriptor">The bound function to map documentation for.</param>
    /// <returns>The mapped documentation comment.</returns>
    /// <exception cref="XmlDocumentationMappingException">
    /// Thrown when no documentation ID could be built for the method, no <c>&lt;member&gt;</c> element was found for it, or a
    /// bound parameter has no corresponding <c>&lt;param&gt;</c> entry.
    /// </exception>
    public static DocumentationComment Map(XmlDocumentationIndex index, BoundFunctionDescriptor descriptor)
    {
        string documentationId = BuildDocumentationId(descriptor.Method);
        if (!index.TryGetMember(documentationId, out XElement? member) || member is null)
        {
            throw new XmlDocumentationMappingException($"No XML documentation entry found for '{documentationId}'.");
        }

        Dictionary<string, (string Type, string Description)> parameters = new(StringComparer.OrdinalIgnoreCase);
        ParameterInfo[] clrParameters = descriptor.Method.GetParameters();
        int visibleParameterCount = clrParameters.Length - descriptor.HostInjectedParameterCount;
        for (int i = 0; i < visibleParameterCount; i++)
        {
            BoundParameter boundParameter = descriptor.Parameters[i];
            string clrParameterName = clrParameters[i].Name!;
            XElement? paramElement = member.Elements("param")
                .FirstOrDefault(element => element.Attribute("name")?.Value == clrParameterName) ?? throw new XmlDocumentationMappingException(
                    $"No <param> XML documentation entry found for parameter '{clrParameterName}' of '{documentationId}'.");

            parameters[boundParameter.Name] = (LiteralTypeNames.ToDisplayName(boundParameter.Type), Trim(paramElement.Value));
        }

        (string Type, string Description)? returnValue = null;
        if (descriptor.ReturnType is not null)
        {
            string? returnsText = member.Element("returns")?.Value;
            returnValue = (LiteralTypeNames.ToDisplayName(descriptor.ReturnType.Value), Trim(returnsText ?? string.Empty));
        }

        List<(string Name, string Type, string Description)> exceptions = [.. member.Elements("exception")
            .Select(element => (
                Name: CrefTail(element.Attribute("cref")?.Value),
                Type: "YARN",
                Description: Trim(element.Value)))];

        List<string> examples = [.. member.Elements("example")
            .Concat(member.Element("remarks")?.Elements("example") ?? [])
            .Select(element => Trim(element.Value))];

        List<string> seeAlso = [.. member.Elements("seealso")
            .Select(element => CrefTail(element.Attribute("cref")?.Value))];

        ObsoleteAttribute? obsolete = descriptor.Method.GetCustomAttribute<ObsoleteAttribute>();

        return new()
        {
            Summary = TrimOrNull(member.Element("summary")?.Value),
            Remarks = FoldPreviewAndKeywordAnalogue(TrimOrNull(member.Element("remarks")?.Value), descriptor),
            Parameters = parameters.Count > 0 ? parameters : null,
            ReturnValue = returnValue,
            Exceptions = exceptions.Count > 0 ? exceptions : null,
            Examples = examples.Count > 0 ? examples : null,
            SeeAlso = seeAlso.Count > 0 ? seeAlso : null,
            IsDeprecated = obsolete is not null,
            DeprecationMessage = obsolete?.Message
        };
    }

    /// <summary>
    /// Folds the preview and keyword-analogue markers of a bound function into its remarks text, since
    /// <see cref="DocumentationComment"/> has no dedicated fields for either.
    /// </summary>
    /// <param name="remarks">The remarks text mapped from the XML documentation, or <c>null</c> if there was none.</param>
    /// <param name="descriptor">The bound function to fold markers from.</param>
    /// <returns>The combined remarks text, or <c>null</c> if there is nothing to show.</returns>
    private static string? FoldPreviewAndKeywordAnalogue(string? remarks, BoundFunctionDescriptor descriptor)
    {
        List<string> parts = [];
        if (remarks is not null)
        {
            parts.Add(remarks);
        }

        if (descriptor.IsPreview)
        {
            parts.Add("> **Preview**<br />\n> This is a preview function and may change without warning.");
        }

        if (descriptor.KeywordAnalogue is not null)
        {
            parts.Add($"Library counterpart of `{descriptor.KeywordAnalogue}`.");
        }

        return parts.Count > 0
            ? string.Join("\n\n", parts)
            : null;
    }

    /// <summary>
    /// Builds the mechanical XML documentation ID for a method.
    /// </summary>
    /// <param name="method">The method to build the ID for.</param>
    /// <returns>The documentation ID.</returns>
    /// <exception cref="XmlDocumentationMappingException">Thrown when the declaring type of the method has no <see cref="Type.FullName"/>.</exception>
    private static string BuildDocumentationId(MethodInfo method)
    {
        string? declaringTypeName = (method.DeclaringType?.FullName)
            ?? throw new XmlDocumentationMappingException($"Cannot build a documentation ID for method '{method.Name}': its declaring type has no full name.");

        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return $"M:{declaringTypeName}.{method.Name}";
        }

        string parameterList = string.Join(",", parameters.Select(parameter => parameter.ParameterType.FullName));
        return $"M:{declaringTypeName}.{method.Name}({parameterList})";
    }

    /// <summary>
    /// Extracts the tail segment of a <c>cref</c> attribute value, dropping its documentation ID prefix.
    /// </summary>
    /// <param name="cref">The raw <c>cref</c> attribute value, for example <c>T:System.ArgumentException</c>.</param>
    /// <returns>The tail segment, for example <c>ArgumentException</c>, or the original value if it has no colon prefix.</returns>
    private static string CrefTail(string? cref)
    {
        if (string.IsNullOrEmpty(cref))
        {
            return string.Empty;
        }

        int colonIndex = cref.IndexOf(':');
        string qualifiedName = colonIndex >= 0
            ? cref[(colonIndex + 1)..]
            : cref;

        int lastDotIndex = qualifiedName.LastIndexOf('.');
        return lastDotIndex >= 0
            ? qualifiedName[(lastDotIndex + 1)..]
            : qualifiedName;
    }

    /// <summary>
    /// Trims XML documentation text content, collapsing the indentation the compiler preserves verbatim.
    /// </summary>
    /// <param name="text">The raw element text.</param>
    /// <returns>The trimmed text.</returns>
    private static string Trim(string text) => text.Trim();

    /// <summary>
    /// Trims XML documentation text content, returning <c>null</c> for missing or empty content.
    /// </summary>
    /// <param name="text">The raw element text, or <c>null</c> if the element is absent.</param>
    /// <returns>The trimmed text, or <c>null</c>.</returns>
    private static string? TrimOrNull(string? text)
    {
        string? trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed)
            ? null
            : trimmed;
    }
}
