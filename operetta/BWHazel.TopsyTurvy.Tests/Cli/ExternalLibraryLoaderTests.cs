using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Cli;
using BWHazel.TopsyTurvy.Tests.ExternalLibraryFixture;

namespace BWHazel.TopsyTurvy.Tests.Cli;

/// <summary>
/// Tests for the <see cref="ExternalLibraryLoader"/> class.
/// </summary>
public class ExternalLibraryLoaderTests
{
    /// <summary>
    /// Tests that the <see cref="ExternalLibraryLoader.Load"/> method returns the default catalogue when given no paths.
    /// </summary>
    [Fact]
    public void Load_WithNoPaths_ReturnsDefaultCatalogue()
    {
        ExternalLibraryLoadResult result = ExternalLibraryLoader.Load([]);

        result.Catalogue.ShouldBe(BindingCatalogue.Default);
        result.ErrorMessage.ShouldBeNull();
        result.ExternalLibraryAssemblyPaths.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="ExternalLibraryLoader.Load"/> method merges a valid external library into the catalogue
    /// alongside the Standard Library.
    /// </summary>
    [Fact]
    public void Load_WithValidExternalLibrary_ReturnsMergedCatalogue()
    {
        string fixtureLibraryPath = typeof(FixtureLibrary).Assembly.Location;

        ExternalLibraryLoadResult result = ExternalLibraryLoader.Load([fixtureLibraryPath]);

        result.ErrorMessage.ShouldBeNull();
        result.Catalogue.ShouldNotBeNull();
        result.Catalogue.Find("Greet").ShouldNotBeNull();
        result.Catalogue.Find("PreviewBehold").ShouldNotBeNull();
        result.ExternalLibraryAssemblyPaths.ShouldHaveSingleItem();
    }

    /// <summary>
    /// Tests that the <see cref="ExternalLibraryLoader.Load"/> method reports a clean error when a path does not exist.
    /// </summary>
    [Fact]
    public void Load_WithMissingPath_ReturnsErrorMentioningNotFound()
    {
        ExternalLibraryLoadResult result = ExternalLibraryLoader.Load(["/no/such/library.dll"]);

        result.Catalogue.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("not found");
    }

    /// <summary>
    /// Tests that the <see cref="ExternalLibraryLoader.Load"/> method reports a clean error when an assembly contains a malformed binding.
    /// </summary>
    /// <remarks>
    /// Scans the <c>Tests</c> assembly location, which already contains several deliberately malformed
    /// <c>Test*BindingClass</c> fixtures used to test <see cref="BindingScanner"/> itself, so no dedicated
    /// malformed-binding fixture is needed here.
    /// </remarks>
    [Fact]
    public void Load_WithMalformedBinding_ReturnsError()
    {
        string testsAssemblyPath = typeof(ExternalLibraryLoaderTests).Assembly.Location;

        ExternalLibraryLoadResult result = ExternalLibraryLoader.Load([testsAssemblyPath]);

        result.Catalogue.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="ExternalLibraryLoader.Load"/> method reports a clean error on a name collision.
    /// </summary>
    /// <remarks>
    /// Admitting the same fixture library twice scans it twice, producing two catalogues sharing every key, so
    /// merging them collides deterministically without needing a dedicated colliding fixture.
    /// </remarks>
    [Fact]
    public void Load_WithSameLibraryAdmittedTwice_ReturnsCollisionError()
    {
        string fixtureLibraryPath = typeof(FixtureLibrary).Assembly.Location;

        ExternalLibraryLoadResult result = ExternalLibraryLoader.Load([fixtureLibraryPath, fixtureLibraryPath]);

        result.Catalogue.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNull();
    }
}
