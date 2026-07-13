using System;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="CompletionHandler"/> class.
/// </summary>
public class CompletionHandlerTests : LanguageServerTestBase
{
    private readonly string sourceWithSymbols = """
        HARK! "Test"
        PRINCIPALS
          PRAY WELCOME myVar AS A PEER BEING 1
        THE CURTAIN RISES.
        IT IS MY DUTY TO PERFORM myFunc UNDER THE TERMS OF param AS A PEER TO FIND PEER
          AND SO I FIND param
        MY DUTY IS DISCHARGED.
        BEHOLD myVar
        FINALE.
        """;

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method always returns keyword completions even when no document state exists.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocumentState_ReturnsKeywords()
    {
        DocumentStateManager manager = new();
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 0, character: 0), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldNotBeEmpty();
        result.Items.ShouldContain(item => item.Kind == CompletionItemKind.Keyword);
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method includes declared symbols alongside keywords.
    /// </summary>
    [Fact]
    public async Task Handle_WithDocumentContainingSymbols_IncludesSymbolsInItems()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithSymbols);
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 7, character: 0), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "myVar");
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method includes function symbols with function completion kind.
    /// </summary>
    [Fact]
    public async Task Handle_WithDocumentContainingFunction_IncludesFunctionItemWithCorrectKind()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithSymbols);
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 7, character: 0), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "myFunc" && item.Kind == CompletionItemKind.Function);
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method filters keyword items by the phrase typed before the cursor.
    /// </summary>
    [Fact]
    public async Task Handle_WithPartialKeywordTyped_FiltersKeywordListToMatches()
    {
        string source = "HARK! \"Test\"\nTHE CURTAIN RISES.\nBEHO\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 2, character: 4), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label.StartsWith("BEHOLD", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method filters keyword items to a multi-word namespace keyword when its own first words are typed as the whole phrase.
    /// </summary>
    [Fact]
    public async Task Handle_WithPartialRecogniseKeywordTyped_FiltersKeywordListToMatches()
    {
        string source = "HARK! \"Test\"\nTHE CURTAIN RISES.\nPRAY REC\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 2, character: 8), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "PRAY RECOGNISE");
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method offers the new namespace keywords as candidates when a document has never been parsed, alongside all other keywords.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocumentState_IncludesNamespaceKeywords()
    {
        DocumentStateManager manager = new();
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 0, character: 0), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "TOWN");
        result.Items.ShouldContain(item => item.Label == "WITH DISTRICT");
        result.Items.ShouldContain(item => item.Label == "WITH DUTY");
        result.Items.ShouldContain(item => item.Label == "PRAY RECOGNISE");
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method filters keyword items to <c>WITH DISTRICT</c> and not the shorter, pre-existing bare <c>WITH</c> keyword, confirming the two-word phrase prefix is matched in full rather than truncated to its first word.
    /// </summary>
    [Fact]
    public async Task Handle_WithPartialWithDistrictKeywordTyped_FiltersKeywordListToMatches()
    {
        string source = "HARK! \"Test\"\nTHE CURTAIN RISES.\nWITH DI\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 2, character: 7), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "WITH DISTRICT");
        result.Items.ShouldNotContain(item => item.Label == "WITH");
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method imports function symbols from other open documents.
    /// </summary>
    [Fact]
    public async Task Handle_WithFunctionInOtherDocument_IncludesImportedFunctionItem()
    {
        DocumentUri otherUri = DocumentUri.From("file:///other.topsy");
        string otherSource = """
            HARK! "Other"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM sharedFunc UNDER NO OBLIGATION
              BEHOLD "hello"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string mainSource = "HARK! \"Main\"\nPRINCIPALS\nTHE CURTAIN RISES.\n\nFINALE.\n";
        DocumentStateManager manager = new();
        manager.Update(this.testUri, mainSource, this.parser.TryParse(mainSource));
        manager.Update(otherUri, otherSource, this.parser.TryParse(otherSource));
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 3, character: 0), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "sharedFunc");
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method offers a namespace name declared in another open
    /// document as a completion candidate right after the <c>TOWN</c> keyword.
    /// </summary>
    [Fact]
    public async Task Handle_AfterTownKeyword_OffersKnownNamespaceName()
    {
        DocumentUri otherUri = DocumentUri.From("file:///other.topsy");
        string otherSource = """
            HARK! "Other"
            TOWN Accounts
            PRINCIPALS
            THE CURTAIN RISES.
            FINALE.
            """;
        string mainSource = "HARK! \"Main\"\nTOWN \nFINALE.\n";

        DocumentStateManager manager = new();
        manager.Update(this.testUri, mainSource, this.parser.TryParse(mainSource));
        manager.Update(otherUri, otherSource, this.parser.TryParse(otherSource));
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 1, character: 5), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "Accounts" && item.Kind == CompletionItemKind.Module);
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method offers a namespace name as a completion candidate
    /// right after the <c>PRAY RECOGNISE</c> keyword.
    /// </summary>
    [Fact]
    public async Task Handle_AfterPrayRecogniseKeyword_OffersKnownNamespaceName()
    {
        DocumentUri otherUri = DocumentUri.From("file:///other.topsy");
        string otherSource = """
            HARK! "Other"
            TOWN Accounts
            PRINCIPALS
            THE CURTAIN RISES.
            FINALE.
            """;
        string mainSource = "HARK! \"Main\"\nPRINCIPALS\nTHE CURTAIN RISES.\nPRAY RECOGNISE \nFINALE.\n";

        DocumentStateManager manager = new();
        manager.Update(this.testUri, mainSource, this.parser.TryParse(mainSource));
        manager.Update(otherUri, otherSource, this.parser.TryParse(otherSource));
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 3, character: 15), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "Accounts" && item.Kind == CompletionItemKind.Module);
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method offers the next sub-namespace segment once its parent
    /// segment has already been typed after <c>WITH DISTRICT</c>.
    /// </summary>
    [Fact]
    public async Task Handle_AfterWithDistrictKeyword_OffersNextNamespaceSegment()
    {
        DocumentUri otherUri = DocumentUri.From("file:///other.topsy");
        string otherSource = """
            HARK! "Other"
            TOWN Accounts WITH DISTRICT Payroll
            PRINCIPALS
            THE CURTAIN RISES.
            FINALE.
            """;
        string mainSource = "HARK! \"Main\"\nTOWN Accounts WITH DISTRICT \nFINALE.\n";

        DocumentStateManager manager = new();
        manager.Update(this.testUri, mainSource, this.parser.TryParse(mainSource));
        manager.Update(otherUri, otherSource, this.parser.TryParse(otherSource));
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 1, character: 29), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "Payroll" && item.Kind == CompletionItemKind.Module);
        result.Items.ShouldNotContain(item => item.Label == "Accounts" && item.Kind == CompletionItemKind.Module);
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method offers the next sub-namespace segment after the
    /// short-form <c>*</c> separator, matching the long-form <c>WITH DISTRICT</c> behaviour.
    /// </summary>
    [Fact]
    public async Task Handle_AfterShortFormSeparator_OffersNextNamespaceSegment()
    {
        DocumentUri otherUri = DocumentUri.From("file:///other.topsy");
        string otherSource = """
            HARK! "Other"
            TOWN Accounts*Payroll
            PRINCIPALS
            THE CURTAIN RISES.
            FINALE.
            """;
        string mainSource = "HARK! \"Main\"\nTOWN Accounts*\nFINALE.\n";

        DocumentStateManager manager = new();
        manager.Update(this.testUri, mainSource, this.parser.TryParse(mainSource));
        manager.Update(otherUri, otherSource, this.parser.TryParse(otherSource));
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 1, character: 14), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "Payroll" && item.Kind == CompletionItemKind.Module);
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method scopes function-name completion to the exact namespace
    /// already typed once the <c>WITH DUTY</c> segment of a fully-qualified <c>SUMMON</c> target is being typed,
    /// excluding functions from other namespaces.
    /// </summary>
    [Fact]
    public async Task Handle_AfterWithDutyKeyword_ScopesFunctionCompletionToTypedNamespace()
    {
        DocumentUri accountsUri = DocumentUri.From("file:///accounts.topsy");
        DocumentUri otherUri = DocumentUri.From("file:///other.topsy");
        string accountsSource = """
            HARK! "Accounts"
            TOWN Accounts
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM CalculateTax UNDER NO OBLIGATION
              BEHOLD "tax"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;
        string otherSource = """
            HARK! "Other"
            TOWN Marketing
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM SendCampaign UNDER NO OBLIGATION
              BEHOLD "sent"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        // The in-progress line with an incomplete SUMMON statement fails to parse on its own, exactly as it would
        // while the user is still mid-typing; the completion request relies on the "last good" symbol table from
        // before this edit, so the document is first primed with a syntactically complete source.
        string validMainSource = "HARK! \"Main\"\nFINALE.\n";
        string inProgressMainSource = "HARK! \"Main\"\nSUMMON Accounts WITH DUTY \nFINALE.\n";

        DocumentStateManager manager = new();
        manager.Update(this.testUri, validMainSource, this.parser.TryParse(validMainSource));
        manager.Update(this.testUri, inProgressMainSource, this.parser.TryParse(inProgressMainSource));
        manager.Update(accountsUri, accountsSource, this.parser.TryParse(accountsSource));
        manager.Update(otherUri, otherSource, this.parser.TryParse(otherSource));
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(this.MakeRequest(line: 1, character: 26), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "CalculateTax" && item.Kind == CompletionItemKind.Function);
        result.Items.ShouldNotContain(item => item.Label == "SendCampaign");
    }

    /// <summary>
    /// Tests that the <see cref="CompletionHandler"/> method still scopes function-name completion to the typed
    /// namespace after <c>WITH DUTY</c> even when the current document has never itself successfully parsed, i.e.
    /// there is no "last good" symbol table to fall back on.
    /// </summary>
    [Fact]
    public async Task Handle_AfterWithDutyKeywordInNeverParsedDocument_StillScopesFunctionCompletion()
    {
        DocumentUri accountsUri = DocumentUri.From("file:///accounts.topsy");
        string accountsSource = """
            HARK! "Accounts"
            TOWN Accounts
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM CalculateTax UNDER NO OBLIGATION
              BEHOLD "tax"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        // The main document is updated exactly once, with an incomplete, unparsable source.
        // There is no prior successful parse for it to fall back on.
        string inProgressMainSource = "HARK! \"Main\"\nSUMMON Accounts WITH DUTY \nFINALE.\n";

        DocumentStateManager manager = new();
        manager.Update(this.testUri, inProgressMainSource, this.parser.TryParse(inProgressMainSource));
        manager.Update(accountsUri, accountsSource, this.parser.TryParse(accountsSource));
        CompletionHandler handler = new(manager);

        manager.Get(this.testUri)!.SymbolTable.ShouldBeNull();

        CompletionList result = await handler.Handle(this.MakeRequest(line: 1, character: 26), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldContain(item => item.Label == "CalculateTax" && item.Kind == CompletionItemKind.Function);
    }

    /// <summary>
    /// Creates a <see cref="CompletionParams"/> object for the test document at the specified line and character position.
    /// </summary>
    /// <param name="line">The line number for the completion request.</param>
    /// <param name="character">The character position within the line for the completion request.</param>
    /// <returns>A <see cref="CompletionParams"/> object for the specified position in the test document.</returns>
    private CompletionParams MakeRequest(int line, int character) =>
        new()
        {
            TextDocument = new()
            {
                Uri = this.testUri
            },
            Position = new(line, character)
        };
}
