using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="ReferencesHandler"/> class.
/// </summary>
public class ReferencesHandlerTests : LanguageServerTestBase
{
    private readonly string sourceWithFunction = """
        HARK! "Test"
        IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
          BEHOLD "hello"
        MY DUTY IS DISCHARGED.
        PRINCIPALS
        THE CURTAIN RISES.
        SUMMON greet WITH NOTHING IF YOU PLEASE.
        FINALE.
        """;

    private readonly DocumentUri otherUri = DocumentUri.From("file:///other.topsy");

    /// <summary>
    /// Tests that the <see cref="ReferencesHandler.Handle"/> method returns null when no document state is registered.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocumentState_ReturnsNull()
    {
        DocumentStateManager manager = new();
        ReferencesHandler handler = new(manager);

        LocationContainer? result = await handler.Handle(this.MakeRequest(line: 0, character: 0), CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="ReferencesHandler.Handle"/> method returns null when the cursor is not over an identifier.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorNotOnIdentifier_ReturnsNull()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithFunction);
        ReferencesHandler handler = new(manager);

        LocationContainer? result = await handler.Handle(this.MakeRequest(line: 0, character: 0), CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="ReferencesHandler.Handle"/> method finds the declaration and its call site within the same document.
    /// </summary>
    [Fact]
    public async Task Handle_WithFunctionUsedInSameDocument_ReturnsDeclarationAndCallSite()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithFunction);
        ReferencesHandler handler = new(manager);

        LocationContainer? result = await handler.Handle(this.MakeRequest(line: 1, character: 26), CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Count().ShouldBe(2);
    }

    /// <summary>
    /// Tests that the <see cref="ReferencesHandler.Handle"/> method includes occurrences in another open document
    /// that imports the current document via <c>PRAY ADMIT</c>.
    /// </summary>
    [Fact]
    public async Task Handle_WithCallSiteInImportingDocument_ReturnsCrossFileReference()
    {
        string mainSource = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
              BEHOLD "hello"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;
        string importingSource = "HARK! \"Other\"\nPRINCIPALS\nTHE CURTAIN RISES.\nPRAY ADMIT \"test.topsy\"\nSUMMON greet WITH NOTHING IF YOU PLEASE.\nFINALE.\n";

        DocumentStateManager manager = this.CreateManagerWithSource(mainSource);
        manager.Update(this.otherUri, importingSource, this.parser.TryParse(importingSource));
        ReferencesHandler handler = new(manager);

        LocationContainer? result = await handler.Handle(this.MakeRequest(line: 1, character: 26), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldContain(location => location.Uri.ToString() == this.otherUri.ToString());
    }

    /// <summary>
    /// Tests that the <see cref="ReferencesHandler.Handle"/> method excludes a same-named occurrence in another open
    /// document that has no <c>PRAY ADMIT</c> connection to the current document in either direction.
    /// </summary>
    [Fact]
    public async Task Handle_WithSameNameInUnrelatedDocument_ExcludesUnrelatedDocument()
    {
        string unrelatedSource = "HARK! \"Unrelated\"\nIT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION\n  BEHOLD \"hi\"\nMY DUTY IS DISCHARGED.\nFINALE.\n";

        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithFunction);
        manager.Update(this.otherUri, unrelatedSource, this.parser.TryParse(unrelatedSource));
        ReferencesHandler handler = new(manager);

        LocationContainer? result = await handler.Handle(this.MakeRequest(line: 1, character: 26), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldNotContain(location => location.Uri.ToString() == this.otherUri.ToString());
    }

    /// <summary>
    /// Creates a <see cref="ReferenceParams"/> object for the test document at the specified line and character position.
    /// </summary>
    /// <param name="line">The line number for the references request.</param>
    /// <param name="character">The character position within the line for the references request.</param>
    /// <returns>A <see cref="ReferenceParams"/> object for the specified position in the test document.</returns>
    private ReferenceParams MakeRequest(int line, int character) =>
        new()
        {
            TextDocument = new()
            {
                Uri = this.testUri
            },
            Position = new(line, character),
            Context = new() { IncludeDeclaration = true }
        };
}
