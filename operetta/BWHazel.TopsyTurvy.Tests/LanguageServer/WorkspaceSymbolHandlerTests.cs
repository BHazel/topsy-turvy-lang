using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="WorkspaceSymbolHandler"/> class.
/// </summary>
public class WorkspaceSymbolHandlerTests : LanguageServerTestBase
{
    private readonly string sourceWithSymbols = """
        HARK! "Test"
        PRINCIPALS
          PRAY WELCOME myVariable AS A PEER BEING 1
        THE CURTAIN RISES.
        IT IS MY DUTY TO PERFORM myFunction UNDER NO OBLIGATION
          BEHOLD "hello"
        MY DUTY IS DISCHARGED.
        FINALE.
        """;

    /// <summary>
    /// Tests that the <see cref="WorkspaceSymbolHandler.Handle"/> method returns an empty collection when no documents are tracked.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocuments_ReturnsEmptyCollection()
    {
        DocumentStateManager manager = new();
        WorkspaceSymbolHandler handler = new(manager);

        Container<WorkspaceSymbol>? result = await handler.Handle(this.MakeRequest(string.Empty), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="WorkspaceSymbolHandler.Handle"/> method returns all symbols when the query is empty.
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyQuery_ReturnsAllSymbols()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithSymbols);
        WorkspaceSymbolHandler handler = new(manager);

        Container<WorkspaceSymbol>? result = await handler.Handle(this.MakeRequest(string.Empty), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="WorkspaceSymbolHandler.Handle"/> method filters symbols when a non-empty query is supplied.
    /// </summary>
    [Fact]
    public async Task Handle_WithMatchingQuery_ReturnsOnlyMatchingSymbols()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithSymbols);
        WorkspaceSymbolHandler handler = new(manager);

        Container<WorkspaceSymbol>? result = await handler.Handle(this.MakeRequest("myVar"), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldAllBe(symbol => symbol.Name.Contains("myVar", System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Tests that the <see cref="WorkspaceSymbolHandler.Handle"/> method returns an empty collection when no symbols match the query.
    /// </summary>
    [Fact]
    public async Task Handle_WithNonMatchingQuery_ReturnsEmptyCollection()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithSymbols);
        WorkspaceSymbolHandler handler = new(manager);

        Container<WorkspaceSymbol>? result = await handler.Handle(this.MakeRequest("zzz_no_match_zzz"), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="WorkspaceSymbolHandler.Handle"/> method aggregates symbols across all open documents.
    /// </summary>
    [Fact]
    public async Task Handle_WithMultipleDocuments_AggregatesSymbolsFromAll()
    {
        DocumentUri otherUri = DocumentUri.From("file:///other.topsy");
        string otherSource = """
            HARK! "Other"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM otherFunc UNDER NO OBLIGATION
              BEHOLD "x"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceWithSymbols);
        manager.Update(otherUri, otherSource, this.parser.TryParse(otherSource));
        WorkspaceSymbolHandler handler = new(manager);

        Container<WorkspaceSymbol>? result = await handler.Handle(this.MakeRequest(string.Empty), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldContain(symbol => symbol.Name == "otherFunc");
    }

    /// <summary>
    /// Creates a <see cref="WorkspaceSymbolParams"/> object with the specified query string.
    /// </summary>
    /// <param name="query">The query string to filter workspace symbols.</param>
    /// <returns>A <see cref="WorkspaceSymbolParams"/> object with the specified query string.</returns>
    private WorkspaceSymbolParams MakeRequest(string query) =>
        new()
        {
            Query = query
        };
}
