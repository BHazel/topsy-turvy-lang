using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="RenameHandler"/> class.
/// </summary>
public class RenameHandlerTests : LanguageServerTestBase
{
    private readonly string sourceWithVariable = """
        HARK! "Test"
        PRINCIPALS
          PRAY WELCOME counter AS A PEER BEING 0
        THE CURTAIN RISES.
        counter IS APPOINTED SUM OF counter AND 1
        BEHOLD counter
        FINALE.
        """;

    /// <summary>
    /// Tests that the <see cref="RenameHandler.Handle"/> method returns null when no document state is registered.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocumentState_ReturnsNull()
    {
        DocumentStateManager manager = new();
        RenameHandler handler = new(manager);

        WorkspaceEdit? result = await handler.Handle(this.MakeRequest(line: 0, character: 0, newName: "newName"), CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Tests that the <see cref="RenameHandler.Handle"/> method returns null when the cursor is not over an identifier.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorNotOnIdentifier_ReturnsNull()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithVariable);
        RenameHandler handler = new(manager);

        WorkspaceEdit? result = await handler.Handle(this.MakeRequest(line: 0, character: 0, newName: "newName"), CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Tests that the <see cref="RenameHandler.Handle"/> method returns a workspace edit covering all occurrences of the symbol.
    /// </summary>
    [Fact]
    public async Task Handle_WithCursorOnDeclaredVariable_ReturnsEditsForAllOccurrences()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithVariable);
        RenameHandler handler = new(manager);

        WorkspaceEdit? result = await handler.Handle(this.MakeRequest(line: 4, character: 1, newName: "tally"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Changes);
        Assert.True(result.Changes.ContainsKey(this.testUri));
        Assert.True(result.Changes[this.testUri].Count() > 1);
    }

    /// <summary>
    /// Tests that the <see cref="RenameHandler.Handle"/> method replaces every occurrence with the new name.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidRename_ReplacesOccurrencesWithNewName()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithVariable);
        RenameHandler handler = new(manager);

        WorkspaceEdit? result = await handler.Handle(this.MakeRequest(line: 4, character: 1, newName: "tally"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Changes);
        foreach (TextEdit edit in result.Changes[this.testUri])
        {
            Assert.Equal("tally", edit.NewText);
        }
    }

    /// <summary>
    /// Tests that the <see cref="RenameHandler.Handle"/> method returns null when the symbol name contains a space, i.e. JUST SO.
    /// </summary>
    [Fact]
    public async Task Handle_WithJustSoSymbol_ReturnsNullDueToSpaceInName()
    {
        string source = "HARK! \"Test\"\nTHE CURTAIN RISES.\nBEHOLD JUST SO\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        RenameHandler handler = new(manager);

        WorkspaceEdit? result = await handler.Handle(this.MakeRequest(line: 2, character: 7, newName: "newName"), CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Tests that the <see cref="RenameHandler.Handle"/> method skips occurrences inside string literals.
    /// </summary>
    [Fact]
    public async Task Handle_WithVariableNameInsideStringLiteral_DoesNotRenameStringContent()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
              PRAY WELCOME value AS A YARN BEING "value is here"
            THE CURTAIN RISES.
            BEHOLD value
            FINALE.
            """;

        DocumentStateManager manager = this.CreateManagerWithSource(source);
        RenameHandler handler = new(manager);

        WorkspaceEdit? result = await handler.Handle(this.MakeRequest(line: 4, character: 7, newName: "renamed"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Changes);
        foreach (TextEdit edit in result.Changes[this.testUri])
        {
            Assert.DoesNotContain("value is here", edit.NewText);
        }
    }

    /// <summary>
    /// Creates a <see cref="RenameParams"/> request with the specified line, character, and new name.
    /// </summary>
    /// <param name="line">The line number of the position.</param>
    /// <param name="character">The character offset of the position.</param>
    /// <param name="newName">The new name for the symbol.</param>
    /// <returns>A <see cref="RenameParams"/> for the test document at the specified position with the new name.</returns>
    private RenameParams MakeRequest(int line, int character, string newName) =>
        new()
        {
            TextDocument = new()
            {
                Uri = this.testUri
            },
            Position = new(line, character),
            NewName = newName
        };
}
