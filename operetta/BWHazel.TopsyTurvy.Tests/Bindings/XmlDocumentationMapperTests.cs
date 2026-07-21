using System.IO;
using System.Linq;
using System.Text;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// Tests for the <see cref="XmlDocumentationMapper"/> class.
/// </summary>
public class XmlDocumentationMapperTests
{
    /// <summary>
    /// The documentation ID of <see cref="TestValidBindingClass.VoidFunction"/>, matching the mechanical construction
    /// rule (<c>"M:" + declaringType.FullName + "." + method.Name + "(" + parameter type full names + ")"</c>).
    /// </summary>
    private const string VoidFunctionDocumentationId =
        "M:BWHazel.TopsyTurvy.Tests.Bindings.TestValidBindingClass.VoidFunction(System.String,BWHazel.TopsyTurvy.Sdk.Interop.IO.ITopsyTurvyIO)";

    /// <summary>
    /// Tests that <see cref="XmlDocumentationMapper.Map"/> maps every documented field for a fully-documented function.
    /// </summary>
    [Fact]
    public void Map_WithFullyDocumentedFunction_MapsEveryField()
    {
        string xml = $"""
            <?xml version="1.0"?>
            <doc>
              <members>
                <member name="{VoidFunctionDocumentationId}">
                  <summary>Prints text.</summary>
                  <remarks>
                    Additional remarks.
                    <example>SUMMON TestVoidFunction WITH "Hello" IF YOU PLEASE.</example>
                  </remarks>
                  <param name="text">The text parameter.</param>
                  <param name="io">The host-injected parameter, not part of the language-level signature.</param>
                  <exception cref="T:System.ArgumentException">Thrown if the text is invalid.</exception>
                  <seealso cref="M:BWHazel.TopsyTurvy.Tests.Bindings.TestValidBindingClass.ValueFunction"/>
                </member>
              </members>
            </doc>
            """;

        XmlDocumentationIndex index = XmlDocumentationMapper.Load(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        BoundFunctionDescriptor descriptor = BindingScanner.Scan(typeof(TestValidBindingClass))
            .Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.VoidFunction));

        DocumentationComment result = XmlDocumentationMapper.Map(index, descriptor);

        result.Summary.ShouldBe("Prints text.");
        result.Remarks.ShouldNotBeNull();
        result.Remarks.ShouldContain("Additional remarks.");
        result.Examples.ShouldNotBeNull();
        result.Examples!.ShouldHaveSingleItem();
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions!.ShouldHaveSingleItem();
        result.Exceptions[0].Name.ShouldBe("ArgumentException");
        result.SeeAlso.ShouldNotBeNull();
        result.SeeAlso!.ShouldHaveSingleItem();
        result.SeeAlso[0].ShouldBe("ValueFunction");
    }

    /// <summary>
    /// Tests that <see cref="XmlDocumentationMapper.Map"/> excludes the host-injected parameter from the mapped <c>Parameters</c>.
    /// </summary>
    [Fact]
    public void Map_WithHostInjectedParameter_ExcludesItFromMappedParameters()
    {
        string xml = $"""
            <?xml version="1.0"?>
            <doc>
              <members>
                <member name="{VoidFunctionDocumentationId}">
                  <summary>Prints text.</summary>
                  <param name="text">The text parameter.</param>
                  <param name="io">The host-injected parameter.</param>
                </member>
              </members>
            </doc>
            """;

        XmlDocumentationIndex index = XmlDocumentationMapper.Load(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        BoundFunctionDescriptor descriptor = BindingScanner.Scan(typeof(TestValidBindingClass))
            .Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.VoidFunction));

        DocumentationComment result = XmlDocumentationMapper.Map(index, descriptor);

        result.Parameters.ShouldNotBeNull();
        result.Parameters!.Count.ShouldBe(1);
        result.Parameters.ShouldContainKey("Text");
    }

    /// <summary>
    /// Tests that <see cref="XmlDocumentationMapper.Map"/> maps a bound parameter under its Topsy Turvy-visible name.
    /// </summary>
    [Fact]
    public void Map_WithBoundParameter_MapsUnderBoundName()
    {
        string xml = $"""
            <?xml version="1.0"?>
            <doc>
              <members>
                <member name="{VoidFunctionDocumentationId}">
                  <summary>Prints text.</summary>
                  <param name="text">The text to print.</param>
                  <param name="io">The host-injected parameter.</param>
                </member>
              </members>
            </doc>
            """;

        XmlDocumentationIndex index = XmlDocumentationMapper.Load(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        BoundFunctionDescriptor descriptor = BindingScanner.Scan(typeof(TestValidBindingClass))
            .Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.VoidFunction));

        DocumentationComment result = XmlDocumentationMapper.Map(index, descriptor);

        result.Parameters!["Text"].Type.ShouldBe(LiteralTypeNames.ToDisplayName(LiteralType.String));
        result.Parameters["Text"].Description.ShouldBe("The text to print.");
    }

    /// <summary>
    /// Tests that <see cref="XmlDocumentationMapper.Map"/> throws when no <c>&lt;member&gt;</c> element exists for the function.
    /// </summary>
    [Fact]
    public void Map_WithNoMatchingMember_Throws()
    {
        string xml = """
            <?xml version="1.0"?>
            <doc>
              <members>
              </members>
            </doc>
            """;

        XmlDocumentationIndex index = XmlDocumentationMapper.Load(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        BoundFunctionDescriptor descriptor = BindingScanner.Scan(typeof(TestValidBindingClass))
            .Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.VoidFunction));

        Should.Throw<XmlDocumentationMappingException>(() => XmlDocumentationMapper.Map(index, descriptor));
    }

    /// <summary>
    /// Tests that <see cref="XmlDocumentationMapper.Map"/> throws when a bound parameter has no corresponding
    /// <c>&lt;param&gt;</c> entry.
    /// </summary>
    [Fact]
    public void Map_WithMissingParameterEntry_Throws()
    {
        string xml = $"""
            <?xml version="1.0"?>
            <doc>
              <members>
                <member name="{VoidFunctionDocumentationId}">
                  <summary>Prints text.</summary>
                </member>
              </members>
            </doc>
            """;

        XmlDocumentationIndex index = XmlDocumentationMapper.Load(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
        BoundFunctionDescriptor descriptor = BindingScanner.Scan(typeof(TestValidBindingClass))
            .Single(descriptor => descriptor.Method.Name == nameof(TestValidBindingClass.VoidFunction));

        Should.Throw<XmlDocumentationMappingException>(() => XmlDocumentationMapper.Map(index, descriptor));
    }
}
