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
