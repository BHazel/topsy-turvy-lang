using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Tests that the <see cref="DocumentStateManager.Update"/> method records <c>PRAY ADMIT</c> import paths on the document state.
    /// </summary>
    [Fact]
    public void Update_WithImportStatement_RecordsImportPath()
    {
        string source = "HARK! \"Test\"\nPRINCIPALS\nTHE CURTAIN RISES.\nPRAY ADMIT \"other.topsy\"\nFINALE.\n";
        DocumentStateManager manager = new();

        manager.Update(this.testUri, source, this.parser.TryParse(source));

        manager.Get(this.testUri)!.ImportPaths.ShouldContain("other.topsy");
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.GetImportConnectedDocuments"/> returns an empty collection for an untracked document.
    /// </summary>
    [Fact]
    public void GetImportConnectedDocuments_ForUnknownUri_ReturnsEmpty()
    {
        DocumentStateManager manager = new();

        IReadOnlyList<(DocumentUri, DocumentState)> result = manager.GetImportConnectedDocuments(this.testUri);

        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.GetImportConnectedDocuments"/> includes a document the current document imports via <c>PRAY ADMIT</c>.
    /// </summary>
    [Fact]
    public void GetImportConnectedDocuments_WithCurrentDocumentImportingOther_IncludesOther()
    {
        string mainSource = "HARK! \"Test\"\nPRINCIPALS\nTHE CURTAIN RISES.\nPRAY ADMIT \"other.topsy\"\nFINALE.\n";
        DocumentStateManager manager = new();
        manager.Update(this.testUri, mainSource, this.parser.TryParse(mainSource));
        manager.Update(this.otherUri, this.functionSource, this.parser.TryParse(this.functionSource));

        IReadOnlyList<(DocumentUri Uri, DocumentState State)> result = manager.GetImportConnectedDocuments(this.testUri);

        result.ShouldContain(entry => entry.Uri.ToString() == this.otherUri.ToString());
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.GetImportConnectedDocuments"/> includes a document that imports the
    /// current document, even though the current document does not import it back.
    /// </summary>
    [Fact]
    public void GetImportConnectedDocuments_WithOtherDocumentImportingCurrent_IncludesOther()
    {
        string otherSource = "HARK! \"Other\"\nPRINCIPALS\nTHE CURTAIN RISES.\nPRAY ADMIT \"test.topsy\"\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(this.source);
        manager.Update(this.otherUri, otherSource, this.parser.TryParse(otherSource));

        IReadOnlyList<(DocumentUri Uri, DocumentState State)> result = manager.GetImportConnectedDocuments(this.testUri);

        result.ShouldContain(entry => entry.Uri.ToString() == this.otherUri.ToString());
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.GetImportConnectedDocuments"/> excludes an open document that has
    /// no <c>PRAY ADMIT</c> connection to the current document in either direction.
    /// </summary>
    [Fact]
    public void GetImportConnectedDocuments_WithUnrelatedDocument_ExcludesUnrelatedDocument()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.source);
        manager.Update(this.otherUri, this.functionSource, this.parser.TryParse(this.functionSource));

        IReadOnlyList<(DocumentUri Uri, DocumentState State)> result = manager.GetImportConnectedDocuments(this.testUri);

        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Update"/> method records the namespace path declared by the document.
    /// </summary>
    [Fact]
    public void Update_WithNamespaceDeclaration_RecordsNamespacePath()
    {
        string source = "HARK! \"Test\"\nTOWN Accounts WITH DISTRICT Payroll\nFINALE.\n";
        DocumentStateManager manager = new();

        manager.Update(this.testUri, source, this.parser.TryParse(source));

        manager.Get(this.testUri)!.NamespacePath.ShouldBe(["Accounts", "Payroll"]);
    }

    /// <summary>
    /// Tests that the <see cref="DocumentStateManager.Update"/> method leaves <see cref="DocumentState.NamespacePath"/> empty for a document without a namespace declaration.
    /// </summary>
    [Fact]
    public void Update_WithoutNamespaceDeclaration_LeavesNamespacePathEmpty()
    {
        DocumentStateManager manager = new();

        manager.Update(this.testUri, this.source, this.SuccessfulParse(this.source));

        manager.Get(this.testUri)!.NamespacePath.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.GetKnownNamespacePaths"/> returns every distinct namespace path
    /// declared across all open documents.
    /// </summary>
    [Fact]
    public void GetKnownNamespacePaths_WithMultipleDocuments_ReturnsAllDistinctPaths()
    {
        string firstSource = "HARK! \"First\"\nTOWN Accounts\nFINALE.\n";
        string secondSource = "HARK! \"Second\"\nTOWN Marketing\nFINALE.\n";
        DocumentStateManager manager = new();
        manager.Update(this.testUri, firstSource, this.parser.TryParse(firstSource));
        manager.Update(this.otherUri, secondSource, this.parser.TryParse(secondSource));

        IReadOnlyList<IReadOnlyList<string>> result = manager.GetKnownNamespacePaths();

        result.ShouldContain(path => path.SequenceEqual(new[] { "Accounts" }));
        result.ShouldContain(path => path.SequenceEqual(new[] { "Marketing" }));
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.GetKnownNamespacePaths"/> excludes documents without a namespace declaration.
    /// </summary>
    [Fact]
    public void GetKnownNamespacePaths_WithDocumentWithoutNamespace_ExcludesIt()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.source);

        IReadOnlyList<IReadOnlyList<string>> result = manager.GetKnownNamespacePaths();

        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.GetFunctionsInNamespace"/> returns only functions declared in a
    /// document whose own namespace path exactly matches the requested path.
    /// </summary>
    [Fact]
    public void GetFunctionsInNamespace_WithMatchingAndNonMatchingDocuments_ReturnsOnlyMatchingFunctions()
    {
        string accountsSource = "HARK! \"Accounts\"\nTOWN Accounts\nPRINCIPALS\nTHE CURTAIN RISES.\nIT IS MY DUTY TO PERFORM CalculateTax UNDER NO OBLIGATION\n  BEHOLD \"tax\"\nMY DUTY IS DISCHARGED.\nFINALE.\n";
        string marketingSource = "HARK! \"Marketing\"\nTOWN Marketing\nPRINCIPALS\nTHE CURTAIN RISES.\nIT IS MY DUTY TO PERFORM SendCampaign UNDER NO OBLIGATION\n  BEHOLD \"sent\"\nMY DUTY IS DISCHARGED.\nFINALE.\n";
        DocumentStateManager manager = new();
        manager.Update(this.testUri, accountsSource, this.parser.TryParse(accountsSource));
        manager.Update(this.otherUri, marketingSource, this.parser.TryParse(marketingSource));

        IEnumerable<SymbolInfo> result = manager.GetFunctionsInNamespace(["Accounts"]);

        result.ShouldContain(symbol => symbol.Name == "CalculateTax");
        result.ShouldNotContain(symbol => symbol.Name == "SendCampaign");
    }

    /// <summary>
    /// Tests that <see cref="DocumentStateManager.GetFunctionsInNamespace"/> returns an empty sequence for a
    /// namespace path that no open document declares.
    /// </summary>
    [Fact]
    public void GetFunctionsInNamespace_WithUnknownNamespace_ReturnsEmpty()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.functionSource);

        IEnumerable<SymbolInfo> result = manager.GetFunctionsInNamespace(["DoesNotExist"]);

        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Performs a successful parse of the provided source text, returning the resulting <see cref="ParseResult"/>.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>A <see cref="ParseResult"/> representing the result of the parse.</returns>
    private ParseResult SuccessfulParse(string source) =>
        this.parser.TryParse(source);
}
