using System.Collections.Generic;
using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.LanguageServer;
using BWHazel.TopsyTurvy.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="DocumentStateManager"/> class.
/// </summary>
public class DocumentStateManagerTests : LanguageServerTestBase
{
    private readonly string source = """
        HARK! "Test"
        PRINCIPALS
          PRAY WELCOME greeting AS A YARN BEING "Hello"
        THE CURTAIN RISES.
        BEHOLD greeting
        FINALE.
        """;

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Get"/> method returns null for an unknown URI.
    /// </summary>
    [Fact]
    public void Get_ForUnknownUri_ReturnsNull()
    {
        DocumentStateManager manager = new();

        DocumentState? result = manager.Get(this.testUri);

        Assert.Null(result);
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Update"/> method creates a new state entry when the URI is first seen.
    /// </summary>
    [Fact]
    public void Update_ForNewUri_CreatesRetrievableState()
    {
        DocumentStateManager manager = new();
        ParseResult result = this.SuccessfulParse(this.source);

        manager.Update(this.testUri, this.source, result);

        Assert.NotNull(manager.Get(this.testUri));
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Update"/> method stores the raw source text on the state.
    /// </summary>
    [Fact]
    public void Update_WithSource_StoresSourceOnState()
    {
        DocumentStateManager manager = new();
        ParseResult result = this.SuccessfulParse(this.source);

        manager.Update(this.testUri, this.source, result);

        Assert.Equal(this.source, manager.Get(this.testUri)!.Source);
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Update"/> method builds a symbol table when parsing succeeds.
    /// </summary>
    [Fact]
    public void Update_WithSuccessfulParse_BuildsSymbolTable()
    {
        DocumentStateManager manager = new();
        ParseResult result = this.SuccessfulParse(this.source);

        manager.Update(this.testUri, this.source, result);

        Assert.NotNull(manager.Get(this.testUri)!.SymbolTable);
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Update"/> method preserves the last good symbol table when the new parse fails.
    /// </summary>
    [Fact]
    public void Update_WithFailedParse_PreservesLastGoodSymbolTable()
    {
        DocumentStateManager manager = new();
        ParseResult goodResult = this.SuccessfulParse(this.source);
        manager.Update(this.testUri, this.source, goodResult);
        SymbolTable? lastGoodTable = manager.Get(this.testUri)!.SymbolTable;

        ParseResult badResult = new(null, []);
        manager.Update(this.testUri, "NOT VALID", badResult);

        Assert.Same(lastGoodTable, manager.Get(this.testUri)!.SymbolTable);
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Update"/> method still updates the source text when parsing fails.
    /// </summary>
    [Fact]
    public void Update_WithFailedParse_UpdatesSourceText()
    {
        DocumentStateManager manager = new();
        ParseResult goodResult = this.SuccessfulParse(this.source);
        manager.Update(this.testUri, this.source, goodResult);

        ParseResult badResult = new(null, []);
        manager.Update(this.testUri, "NEW SOURCE", badResult);

        Assert.Equal("NEW SOURCE", manager.Get(this.testUri)!.Source);
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Remove"/> method removes the state for a tracked URI.
    /// </summary>
    [Fact]
    public void Remove_ForTrackedUri_RemovesState()
    {
        DocumentStateManager manager = new();
        manager.Update(this.testUri, this.source, this.SuccessfulParse(this.source));

        manager.Remove(this.testUri);

        Assert.Null(manager.Get(this.testUri));
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Remove"/> method does not throw when called for an unknown URI.
    /// </summary>
    [Fact]
    public void Remove_ForUnknownUri_DoesNotThrow()
    {
        DocumentStateManager manager = new();

        manager.Remove(DocumentUri.From("file:///does-not-exist.topsy"));

        Assert.Empty(manager.AllDocuments());
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.AllDocuments"/> method returns an empty collection when no documents are tracked.
    /// </summary>
    [Fact]
    public void AllDocuments_WithNoDocuments_ReturnsEmptyCollection()
    {
        DocumentStateManager manager = new();

        IReadOnlyList<(DocumentUri, DocumentState)> result = manager.AllDocuments();

        Assert.Empty(result);
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.AllDocuments"/> method returns an entry for each tracked document.
    /// </summary>
    [Fact]
    public void AllDocuments_WithMultipleDocuments_ReturnsAllEntries()
    {
        DocumentStateManager manager = new();
        DocumentUri uri1 = DocumentUri.From("file:///a.topsy");
        DocumentUri uri2 = DocumentUri.From("file:///b.topsy");
        ParseResult result = this.SuccessfulParse(this.source);

        manager.Update(uri1, this.source, result);
        manager.Update(uri2, this.source, result);

        Assert.Equal(2, manager.AllDocuments().Count);
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.AllDocuments"/> method returns a snapshot that is unaffected by subsequent updates.
    /// </summary>
    [Fact]
    public void AllDocuments_AfterRemoval_DoesNotIncludeRemovedDocument()
    {
        DocumentStateManager manager = new();
        manager.Update(this.testUri, this.source, this.SuccessfulParse(this.source));
        manager.Remove(this.testUri);

        IReadOnlyList<(DocumentUri, DocumentState)> result = manager.AllDocuments();

        Assert.Empty(result);
    }

    /// <summary>
    /// Performs a successful parse of the provided source text, returning the resulting <see cref="ParseResult"/>.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>A <see cref="ParseResult"/> representing the result of the parse.</returns>
    private ParseResult SuccessfulParse(string source) =>
        this.parser.TryParse(source);
}
