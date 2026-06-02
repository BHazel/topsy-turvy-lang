using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="FoldingRangeHandler"/> class.
/// </summary>
public class FoldingRangeHandlerTests : LanguageServerTestBase
{
    /// <summary>
    /// Tests that the <see cref="FoldingRangeHandler.Handle"/> method returns null when no document state is registered.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocumentState_ReturnsNull()
    {
        DocumentStateManager manager = new();
        FoldingRangeHandler handler = new(manager);

        Container<FoldingRange>? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Tests that the <see cref="FoldingRangeHandler.Handle"/> method returns at least one range for a well-formed programme.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidSource_ReturnsFoldingRanges()
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
        FoldingRangeHandler handler = new(manager);

        Container<FoldingRange>? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Tests that the <see cref="FoldingRangeHandler.Handle"/> method produces a comment region fold for a multi-line comment.
    /// </summary>
    [Fact]
    public async Task Handle_WithMultiLineComment_ProducesCommentKindRange()
    {
        string source = """
            HARK! "Test"
            (ASIDE, AT SOME LENGTH:
              This is a long comment.
              It spans multiple lines.
            END OF ASIDE.)
            THE CURTAIN RISES.
            FINALE.
            """;

        DocumentStateManager manager = this.CreateManagerWithSource(source);
        FoldingRangeHandler handler = new(manager);

        Container<FoldingRange>? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        Assert.NotNull(result);
        bool hasComment = false;
        foreach (FoldingRange range in result)
        {
            if (range.Kind == FoldingRangeKind.Comment)
            {
                hasComment = true;
                break;
            }
        }

        Assert.True(hasComment);
    }

    /// <summary>
    /// Tests that the <see cref="FoldingRangeHandler.Handle"/> method produces a region fold for a function definition.
    /// </summary>
    [Fact]
    public async Task Handle_WithFunctionDefinition_ProducesRegionKindRange()
    {
        string source = """
            HARK! "Test"
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
              BEHOLD "Hello"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        DocumentStateManager manager = this.CreateManagerWithSource(source);
        FoldingRangeHandler handler = new(manager);

        Container<FoldingRange>? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        Assert.NotNull(result);
        bool hasRegion = false;
        foreach (FoldingRange range in result)
        {
            if (range.Kind == FoldingRangeKind.Region)
            {
                hasRegion = true;
                break;
            }
        }

        Assert.True(hasRegion);
    }

    /// <summary>
    /// Tests that the <see cref="FoldingRangeHandler.Handle"/> method does not produce a range when the block spans only a single line.
    /// </summary>
    [Fact]
    public async Task Handle_WithSingleLineBlock_ProducesNoRange()
    {
        string source = "HARK! \"Test\"\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        FoldingRangeHandler handler = new(manager);

        Container<FoldingRange>? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Tests that the <see cref="FoldingRangeHandler.Handle"/> method produces a range for a loop construct.
    /// </summary>
    [Fact]
    public async Task Handle_WithLoopConstruct_ProducesLoopFoldRange()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
              PRAY WELCOME i AS A PEER BEING 0
            THE CURTAIN RISES.
            BY A LEGAL FICTION WHILST LOWER DEGREE i AND 3
              i IS APPOINTED SUM OF i AND 1
            THE TERM EXPIRES.
            FINALE.
            """;

        DocumentStateManager manager = this.CreateManagerWithSource(source);
        FoldingRangeHandler handler = new(manager);

        Container<FoldingRange>? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Creates a <see cref="FoldingRangeRequestParam"/> for the test document URI.
    /// </summary>
    /// <returns>A <see cref="FoldingRangeRequestParam"/> for the test document URI.</returns>
    private FoldingRangeRequestParam MakeRequest() =>
        new()
        {
            TextDocument = new()
            {
                Uri = this.testUri
            }
        };
}
