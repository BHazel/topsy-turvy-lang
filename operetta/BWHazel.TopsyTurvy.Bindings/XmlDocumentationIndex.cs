using System.Collections.Generic;
using System.Xml.Linq;

namespace BWHazel.TopsyTurvy.Bindings;

/// <summary>
/// Holds a compiler-generated XML documentation file indexed by member documentation ID.
/// </summary>
/// <remarks>
/// A host, such as the language server or the Web Editor, has one compiled documentation file to read but many
/// bound functions to describe over its lifetime, so parsing the file is separated from looking a member up in it:
/// <see cref="XmlDocumentationMapper.Load(System.IO.Stream)"/> pays the XML parsing cost once and builds this
/// index and <see cref="XmlDocumentationMapper.Map(XmlDocumentationIndex, BoundFunctionDescriptor)"/> then does a
/// dictionary lookup per function rather than re-scanning the document.  Members are keyed by their raw
/// <c>&lt;member name="..."&gt;</c> attribute value, for example
/// <c>M:BWHazel.TopsyTurvy.StandardLibrary.Global.PreviewBehold(System.String,System.Boolean,BWHazel.TopsyTurvy.Sdk.Interop.IO.ITopsyTurvyIO)</c>.
/// </remarks>
public sealed class XmlDocumentationIndex
{
    private readonly IReadOnlyDictionary<string, XElement> membersById;

    /// <summary>
    /// Initialises a new instance of the <see cref="XmlDocumentationIndex"/> class.
    /// </summary>
    /// <param name="membersById">The indexed <c>&lt;member&gt;</c> elements, keyed by documentation ID.</param>
    internal XmlDocumentationIndex(IReadOnlyDictionary<string, XElement> membersById)
    {
        this.membersById = membersById;
    }

    /// <summary>
    /// Attempts to retrieve the <c>&lt;member&gt;</c> element for the given documentation ID.
    /// </summary>
    /// <param name="documentationId">The documentation ID of the member.</param>
    /// <param name="member">The matching <c>&lt;member&gt;</c> element, when this method returns <c>true</c>.</param>
    /// <returns><c>true</c> if a member with that ID was found, otherwise <c>false</c>.</returns>
    internal bool TryGetMember(string documentationId, out XElement? member) =>
        this.membersById.TryGetValue(documentationId, out member);
}
