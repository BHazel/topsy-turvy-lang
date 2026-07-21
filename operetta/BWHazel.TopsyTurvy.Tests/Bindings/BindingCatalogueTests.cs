using BWHazel.TopsyTurvy.Bindings;
using Shouldly;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// Tests for the <see cref="BindingCatalogue"/> class.
/// </summary>
public class BindingCatalogueTests
{
    /// <summary>
    /// Tests that <see cref="BindingCatalogue.Default"/> scans the <c>Global</c> class and finds both preview functions.
    /// </summary>
    [Fact]
    public void Default_WhenAccessed_ContainsBothPreviewFunctions()
    {
        BindingCatalogue catalogue = BindingCatalogue.Default;

        catalogue.Find("PreviewBehold").ShouldNotBeNull();
        catalogue.Find("PreviewPrayTell").ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that <see cref="BindingCatalogue.Empty"/> contains no functions.
    /// </summary>
    [Fact]
    public void Empty_WhenAccessed_ContainsNoFunctions()
    {
        BindingCatalogue catalogue = BindingCatalogue.Empty;

        catalogue.Functions.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="BindingCatalogue.Create"/> throws when two descriptors from different binding classes share a key.
    /// </summary>
    [Fact]
    public void Create_WithCollidingBindingClasses_Throws()
    {
        Should.Throw<BindingCatalogueException>(() => BindingCatalogue.Create(typeof(TestDuplicateBindingClassA), typeof(TestDuplicateBindingClassB)));
    }

    /// <summary>
    /// Tests that <see cref="BindingCatalogue.Merge"/> throws when two descriptors from different catalogues share a key.
    /// </summary>
    [Fact]
    public void Merge_WithCollidingCatalogs_Throws()
    {
        BindingCatalogue catalogueA = BindingCatalogue.Create(typeof(TestDuplicateBindingClassA));
        BindingCatalogue catalogueB = BindingCatalogue.Create(typeof(TestDuplicateBindingClassB));

        Should.Throw<BindingCatalogueException>(() => BindingCatalogue.Merge(catalogueA, catalogueB));
    }

    /// <summary>
    /// Tests that <see cref="BindingCatalogue.Merge"/> combines descriptors from non-colliding catalogues.
    /// </summary>
    [Fact]
    public void Merge_WithNonCollidingCatalogs_CombinesDescriptors()
    {
        BindingCatalogue catalogueA = BindingCatalogue.Create(typeof(TestDuplicateBindingClassA));
        BindingCatalogue merged = BindingCatalogue.Merge(BindingCatalogue.Default, catalogueA);

        merged.Find("PreviewBehold").ShouldNotBeNull();
        merged.Find("DuplicateFunction").ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that <see cref="BindingCatalogue.Find"/> is case-insensitive.
    /// </summary>
    [Fact]
    public void Find_WithDifferentCase_FindsTheSameDescriptor()
    {
        BindingCatalogue catalogue = BindingCatalogue.Default;

        catalogue.Find("previewbehold").ShouldNotBeNull();
        catalogue.Find("PREVIEWBEHOLD").ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that <see cref="BindingCatalogue.Find"/> returns <c>null</c> for an unknown name.
    /// </summary>
    [Fact]
    public void Find_WithUnknownName_ReturnsNull()
    {
        BindingCatalogue catalogue = BindingCatalogue.Default;

        catalogue.Find("NoSuchFunction").ShouldBeNull();
    }

    /// <summary>
    /// Tests that <see cref="BindingCatalogue.Find"/> resolves a namespace function by its dot-joined qualified name.
    /// </summary>
    [Fact]
    public void Find_WithNamespacedFunction_ResolvesByQualifiedName()
    {
        BindingCatalogue catalogue = BindingCatalogue.Create(typeof(TestValidBindingClass));

        catalogue.Find("Test.Namespace.NamespacedFunction").ShouldNotBeNull();
    }
}
