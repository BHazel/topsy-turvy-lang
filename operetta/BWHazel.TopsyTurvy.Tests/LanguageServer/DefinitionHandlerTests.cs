using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="DefinitionHandler"/> class.
/// </summary>
public class DefinitionHandlerTests : LanguageServerTestBase
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
    /// Tests that the <see cref="DefinitionHandler.Handle"/> method returns an empty result when no document state is registered.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocumentState_ReturnsEmpty()
    {
        DocumentStateManager manager = new();
        DefinitionHandler handler = new(manager);

        LocationOrLocationLinks? result = await handler.Handle(this.MakeRequest(line: 0, character: 0), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="DefinitionHandler.Handle"/> method returns an empty result when the cursor is not over an identifier.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorNotOnIdentifier_ReturnsEmpty()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithVariable);
        DefinitionHandler handler = new(manager);

        LocationOrLocationLinks? result = await handler.Handle(this.MakeRequest(line: 0, character: 0), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="DefinitionHandler.Handle"/> method returns a location when the cursor is over a declared variable.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorOnDeclaredVariable_ReturnsDefinitionLocation()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithVariable);
        DefinitionHandler handler = new(manager);

        LocationOrLocationLinks? result = await handler.Handle(this.MakeRequest(line: 4, character: 7), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="DefinitionHandler.Handle"/> method returns an empty result when the cursor is over an unknown word.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorOnUnknownWord_ReturnsEmpty()
    {
        string source = "HARK! \"Test\"\nTHE CURTAIN RISES.\nBEHOLD unknownVar\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        DefinitionHandler handler = new(manager);

        LocationOrLocationLinks? result = await handler.Handle(this.MakeRequest(line: 2, character: 8), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="DefinitionHandler.Handle"/> method navigates to a symbol declared in a different open document.
    /// </summary>
    [Fact]
    public async Task Handle_WithSymbolInOtherDocument_ReturnsOtherDocumentLocation()
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
        DefinitionHandler handler = new(manager);

        LocationOrLocationLinks? result = await handler.Handle(this.MakeRequest(line: 3, character: 8), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Creates a <see cref="DefinitionParams"/> object for the test document at the specified line and character position.
    /// </summary>
    /// <param name="line">The line number for the definition request.</param>
    /// <param name="character">The character position within the line for the definition request.</param>
    /// <returns>A <see cref="DefinitionParams"/> object for the specified position in the test document.</returns>
    private DefinitionParams MakeRequest(int line, int character) =>
        new()
        {
            TextDocument = new()
            {
                Uri = this.testUri
            },
            Position = new(line, character)
        };
}
