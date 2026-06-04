using System;
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
    
    private readonly string functionSource = """
        HARK! "Test"
        IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
          BEHOLD "hello"
        MY DUTY IS DISCHARGED.
        THE CURTAIN RISES.
        SUMMON greet WITH NOTHING IF YOU PLEASE.
        FINALE.
        """;

    private readonly DocumentUri otherUri = DocumentUri.From("file:///other.topsy");

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Get"/> method returns null for an unknown URI.
    /// </summary>
    [Fact]
    public void Get_ForUnknownUri_ReturnsNull()
    {
        DocumentStateManager manager = new();

        DocumentState? result = manager.Get(this.testUri);

        result.ShouldBeNull();
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

        manager.Get(this.testUri).ShouldNotBeNull();
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

        manager.Get(this.testUri)!.Source.ShouldBe(this.source);
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

        manager.Get(this.testUri)!.SymbolTable.ShouldNotBeNull();
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

        manager.Get(this.testUri)!.SymbolTable.ShouldBeSameAs(lastGoodTable);
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

        manager.Get(this.testUri)!.Source.ShouldBe("NEW SOURCE");
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

        manager.Get(this.testUri).ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Remove"/> method does not throw when called for an unknown URI.
    /// </summary>
    [Fact]
    public void Remove_ForUnknownUri_DoesNotThrow()
    {
        DocumentStateManager manager = new();

        manager.Remove(DocumentUri.From("file:///does-not-exist.topsy"));

        manager.AllDocuments().ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.AllDocuments"/> method returns an empty collection when no documents are tracked.
    /// </summary>
    [Fact]
    public void AllDocuments_WithNoDocuments_ReturnsEmptyCollection()
    {
        DocumentStateManager manager = new();

        IReadOnlyList<(DocumentUri, DocumentState)> result = manager.AllDocuments();

        result.ShouldBeEmpty();
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

        manager.AllDocuments().Count.ShouldBe(2);
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

        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.FindSymbolInOtherDocuments"/> returns null when no other documents are open.
    /// </summary>
    [Fact]
    public void FindSymbolInOtherDocuments_WithNoOtherDocuments_ReturnsNull()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.source);

        SymbolInfo? result = manager.FindSymbolInOtherDocuments("greeting", this.testUri);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.FindSymbolInOtherDocuments"/> finds a symbol declared in a different open document.
    /// </summary>
    [Fact]
    public void FindSymbolInOtherDocuments_WithSymbolInOtherDocument_ReturnsSymbolInfo()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.source);
        manager.Update(this.otherUri, this.source, this.parser.TryParse(this.source));

        SymbolInfo? result = manager.FindSymbolInOtherDocuments("greeting", this.testUri);

        result.ShouldNotBeNull();
        string.Equals(result.Name, "greeting", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.FindSymbolInOtherDocuments"/> excludes the current document from the search.
    /// </summary>
    [Fact]
    public void FindSymbolInOtherDocuments_WithSymbolOnlyInCurrentDocument_ReturnsNull()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.source);

        SymbolInfo? result = manager.FindSymbolInOtherDocuments("greeting", this.testUri);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.FindSymbolWithUriInOtherDocuments"/> returns the current URI and null info when no symbol is found.
    /// </summary>
    [Fact]
    public void FindSymbolWithUriInOtherDocuments_WithNoMatch_ReturnsCurrentUriAndNullInfo()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.source);

        (DocumentUri uri, SymbolInfo? info) = manager.FindSymbolWithUriInOtherDocuments("nonexistent", this.testUri);

        uri.ToString().ShouldBe(this.testUri.ToString());
        info.ShouldBeNull();
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.FindSymbolWithUriInOtherDocuments"/> returns the correct URI and symbol info when the symbol is found in another document.
    /// </summary>
    [Fact]
    public void FindSymbolWithUriInOtherDocuments_WithSymbolInOtherDocument_ReturnsOtherUriAndInfo()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.source);
        manager.Update(this.otherUri, this.source, this.parser.TryParse(this.source));

        (DocumentUri uri, SymbolInfo? info) = manager.FindSymbolWithUriInOtherDocuments("greeting", this.testUri);

        uri.ToString().ShouldBe(this.otherUri.ToString());
        info.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.GetImportedFunctionSymbols"/> returns an empty sequence when no other documents are open.
    /// </summary>
    [Fact]
    public void GetImportedFunctionSymbols_WithNoOtherDocuments_ReturnsEmpty()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.functionSource);

        IEnumerable<SymbolInfo> result = manager.GetImportedFunctionSymbols(this.testUri);

        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.GetImportedFunctionSymbols"/> returns function symbols from other open documents.
    /// </summary>
    [Fact]
    public void GetImportedFunctionSymbols_WithFunctionInOtherDocument_ReturnsFunctionSymbol()
    {
        string otherSource = "HARK! \"Other\"\nPRINCIPALS\nTHE CURTAIN RISES.\nIT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION\n  BEHOLD \"hello\"\nMY DUTY IS DISCHARGED.\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(this.source);
        manager.Update(this.otherUri, otherSource, this.parser.TryParse(otherSource));

        IEnumerable<SymbolInfo> result = manager.GetImportedFunctionSymbols(this.testUri);

        result.ShouldContain(symbol => symbol.Name.Equals("greet", StringComparison.OrdinalIgnoreCase) && symbol.Kind == SymbolKind.Function);
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.GetImportedFunctionSymbols"/> excludes non-function symbols from other documents.
    /// </summary>
    [Fact]
    public void GetImportedFunctionSymbols_WithVariableInOtherDocument_DoesNotReturnVariable()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.source);
        manager.Update(this.otherUri, this.source, this.parser.TryParse(this.source));

        IEnumerable<SymbolInfo> result = manager.GetImportedFunctionSymbols(this.testUri);

        result.ShouldNotContain(s => s.Name.Equals("greeting", System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Performs a successful parse of the provided source text, returning the resulting <see cref="ParseResult"/>.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>A <see cref="ParseResult"/> representing the result of the parse.</returns>
    private ParseResult SuccessfulParse(string source) =>
        this.parser.TryParse(source);
}
