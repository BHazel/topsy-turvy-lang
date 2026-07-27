using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="HoverHandler"/> class.
/// </summary>
public class HoverHandlerTests : LanguageServerTestBase
{
    private readonly string sourceWithVariable = """
        HARK! "Test"
        PRINCIPALS
          PRAY WELCOME greeting AS A YARN BEING "Hello"
        THE CURTAIN RISES.
        BEHOLD greeting
        FINALE.
        """;

    /// <summary>
    /// Tests that the <see cref="HoverHandler.Handle"/> method returns null when no document state is registered.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocumentState_ReturnsNull()
    {
        DocumentStateManager manager = new();
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(this.MakeRequest(line: 0, character: 0), CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="HoverHandler.Handle"/> method returns null when the cursor is not over an identifier.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorNotOnIdentifier_ReturnsNull()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithVariable);
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(this.MakeRequest(line: 0, character: 2), CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="HoverHandler.Handle"/> method returns Markdown content when hovering over a known variable.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorOnKnownVariable_ReturnsMarkdownHover()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithVariable);
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(this.MakeRequest(line: 4, character: 7), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Contents.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="HoverHandler.Handle"/> method returns null when hovering over an unknown symbol.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorOnUnknownWord_ReturnsNull()
    {
        string source = "HARK! \"Test\"\nTHE CURTAIN RISES.\nBEHOLD unknownVar\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(this.MakeRequest(line: 2, character: 8), CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="HoverHandler.Handle"/> method finds a symbol declared in a different open document.
    /// </summary>
    [Fact]
    public async Task Handle_WithSymbolDeclaredInOtherDocument_ReturnsHoverFromOtherDocument()
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

        string mainSource = "HARK! \"Main\"\nPRINCIPALS\nTHE CURTAIN RISES.\nSUMMON sharedFunc WITH NOTHING IF YOU PLEASE.\nFINALE.\n";
        DocumentStateManager manager = new();
        manager.Update(this.testUri, mainSource, this.parser.TryParse(mainSource));
        manager.Update(otherUri, otherSource, this.parser.TryParse(otherSource));
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(this.MakeRequest(line: 3, character: 8), CancellationToken.None);

        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="HoverHandler.Handle"/> method includes the documentation summary when a documentation comment precedes the declaration.
    /// </summary>
    [Fact]
    public async Task Handle_WithDocumentationComment_ReturnsHoverIncludingSummary()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
              (ASIDE, AT SOME LENGTH:
                LEGEND: Holds the numbers.
              END OF ASIDE.)
              PRAY WELCOME Numbers AS A PEER
            THE CURTAIN RISES.
            BEHOLD Numbers
            FINALE.
            """;
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(this.MakeRequest(line: 7, character: 7), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Contents.HasMarkupContent.ShouldBeTrue();
        result.Contents.MarkupContent!.Value.ShouldContain("Holds the numbers.");
    }

    /// <summary>
    /// Tests that the <see cref="HoverHandler.Handle"/> method includes every overload signature when hovering
    /// over a function name with more than one declared overload, not just the first-declared one.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorOnOverloadedFunctionName_ReturnsAllOverloadSignatures()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "a whole number"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A YARN TO FIND YARN
              AND SO I FIND "a piece of text"
            MY DUTY IS DISCHARGED.
            SUMMON describe WITH 1 IF YOU PLEASE.
            FINALE.
            """;
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(this.MakeRequest(line: 10, character: 8), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Contents.HasMarkupContent.ShouldBeTrue();
        string markdown = result.Contents.MarkupContent!.Value;
        markdown.ShouldContain("PEER");
        markdown.ShouldContain("YARN");
    }

    /// <summary>
    /// Tests that the <see cref="HoverHandler.Handle"/> method returns only the one overload declared on the
    /// hovered line when the cursor is directly on a specific overload declaration, not every overload.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorOnSpecificOverloadDeclaration_ReturnsOnlyThatOverload()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A PEER TO FIND YARN
              AND SO I FIND "a whole number"
            MY DUTY IS DISCHARGED.

            IT IS MY DUTY TO PERFORM describe UNDER THE TERMS OF value AS A YARN TO FIND YARN
              AND SO I FIND "a piece of text"
            MY DUTY IS DISCHARGED.
            SUMMON describe WITH 1 IF YOU PLEASE.
            FINALE.
            """;
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(this.MakeRequest(line: 3, character: 27), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Contents.HasMarkupContent.ShouldBeTrue();
        string markdown = result.Contents.MarkupContent!.Value;
        markdown.ShouldContain("value AS A PEER");
        markdown.ShouldNotContain("value AS A YARN");
    }

    /// <summary>
    /// Tests that the <see cref="HoverHandler.Handle"/> method returns a namespace hover card when hovering over a
    /// namespace segment declared in the document, listing the functions it declares.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorOnNamespaceSegment_ReturnsNamespaceHoverListingFunctions()
    {
        string source = """
            HARK! "Accounts"
            TOWN Accounts
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM CalculateTax UNDER NO OBLIGATION
              BEHOLD "tax"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(this.MakeRequest(line: 1, character: 6), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Contents.HasMarkupContent.ShouldBeTrue();
        result.Contents.MarkupContent!.Value.ShouldContain("namespace");
        result.Contents.MarkupContent!.Value.ShouldContain("Accounts");
        result.Contents.MarkupContent!.Value.ShouldContain("CalculateTax");
    }

    /// <summary>
    /// Tests that the <see cref="HoverHandler.Handle"/> method returns a namespace hover card when hovering over a
    /// namespace segment that is declared only in another open document, not the current one.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorOnNamespaceSegmentDeclaredInOtherDocument_ReturnsNamespaceHover()
    {
        DocumentUri otherUri = DocumentUri.From("file:///other.topsy");
        string otherSource = """
            HARK! "Accounts"
            TOWN Accounts
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM CalculateTax UNDER NO OBLIGATION
              BEHOLD "tax"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;
        string mainSource = "HARK! \"Main\"\nSUMMON Accounts WITH DUTY CalculateTax WITH NOTHING IF YOU PLEASE.\nFINALE.\n";

        DocumentStateManager manager = new();
        manager.Update(this.testUri, mainSource, this.parser.TryParse(mainSource));
        manager.Update(otherUri, otherSource, this.parser.TryParse(otherSource));
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(this.MakeRequest(line: 1, character: 8), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Contents.HasMarkupContent.ShouldBeTrue();
        result.Contents.MarkupContent!.Value.ShouldContain("Accounts");
    }

    /// <summary>
    /// Create a <see cref="HoverParams"/> request for the test document at a specific position.
    /// </summary>
    /// <param name="line">The line number of the position.</param>
    /// <param name="character">The character offset of the position.</param>
    /// <returns>A <see cref="HoverParams"/> for the test document at the specified position.</returns>
    private HoverParams MakeRequest(int line, int character) =>
        new()
        {
            TextDocument = new()
            {
                Uri = this.testUri
            },
            Position = new(line, character)
        };
}
