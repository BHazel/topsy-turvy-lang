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

        Assert.Null(result);
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

        Assert.Null(result);
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

        Assert.NotNull(result);
        Assert.NotNull(result.Contents);
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

        Assert.Null(result);
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

        Assert.NotNull(result);
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
