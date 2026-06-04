using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="DocumentFormattingHandler"/> class.
/// </summary>
public class DocumentFormattingHandlerTests : LanguageServerTestBase
{
    /// <summary>
    /// Tests that the <see cref="DocumentFormattingHandler.Handle"/> method returns null when no document state is registered.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocumentState_ReturnsNull()
    {
        DocumentStateManager manager = new();
        DocumentFormattingHandler handler = new(manager);

        TextEditContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="DocumentFormattingHandler.Handle"/> method returns a single text edit replacing the full document.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidSource_ReturnsSingleFullDocumentEdit()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
              PRAY WELCOME x AS A PEER BEING 1
            THE CURTAIN RISES.
            BEHOLD x
            FINALE.
            """;

        DocumentStateManager manager = this.CreateManagerWithSource(source);
        DocumentFormattingHandler handler = new(manager);

        TextEditContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldHaveSingleItem();
    }

    /// <summary>
    /// Tests that the <see cref="DocumentFormattingHandler.Handle"/> method produces an edit whose range starts at the first character.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidSource_EditRangeStartsAtDocumentBeginning()
    {
        string source = "HARK! \"Test\"\nTHE CURTAIN RISES.\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        DocumentFormattingHandler handler = new(manager);

        TextEditContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        result.ShouldNotBeNull();
        TextEdit edit = result.First();
        edit.Range.Start.Line.ShouldBe(0);
        edit.Range.Start.Character.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the <see cref="DocumentFormattingHandler.Handle"/> method normalises keyword casing in the formatted output.
    /// </summary>
    [Fact]
    public async Task Handle_WithLowercaseKeywords_NormalisesToCanonicalCase()
    {
        string source = "hark! \"Test\"\nthe curtain rises.\nfinale.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        DocumentFormattingHandler handler = new(manager);

        TextEditContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        result.ShouldNotBeNull();
        TextEdit edit = result.First();
        edit.NewText.ShouldContain("HARK!");
        edit.NewText.ShouldContain("FINALE.");
    }

    /// <summary>
    /// Creates a <see cref="DocumentFormattingParams"/> object for the test document.
    /// </summary>
    /// <returns>A <see cref="DocumentFormattingParams"/> object for the test document.</returns>
    private DocumentFormattingParams MakeRequest() =>
        new()
        {
            TextDocument = new()
            {
                Uri = this.testUri
            },
            Options = new()
            {
                TabSize = 2,
                InsertSpaces = true
            }
        };
}
