using Blazor.Diagrams.Core.Geometry;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Tests for the <see cref="NodeLayoutContext"/> class.
/// </summary>
public class NodeLayoutContextTests
{
    /// <summary>
    /// Tests that <see cref="NodeLayoutContext.NextPrimaryPosition"/> starts at the configured coordinates
    /// and advances the cursor by the row-spacing amount on each call.
    /// </summary>
    [Fact]
    public void NextPrimaryPosition_StartsAtConfiguredCoordinatesAndAdvancesByRowSpacing()
    {
        NodeLayoutContext layout = new(primaryX: 700, secondaryX: 500, startY: 60);

        Point first = layout.NextPrimaryPosition();
        Point second = layout.NextPrimaryPosition();

        first.X.ShouldBe(700);
        first.Y.ShouldBe(60);
        second.X.ShouldBe(700);
        second.Y.ShouldBe(220);
    }

    /// <summary>
    /// Tests that <see cref="NodeLayoutContext.NextPrimaryPosition"/> resets the secondary cursor Y
    /// to match the Y of the position it just emitted.
    /// </summary>
    [Fact]
    public void NextPrimaryPosition_ResetsSecondaryYToMatchTheJustEmittedPrimaryY()
    {
        NodeLayoutContext layout = new(primaryX: 700, secondaryX: 500, startY: 60);
        layout.NextSecondaryPosition();
        layout.NextSecondaryPosition();

        layout.NextPrimaryPosition();
        Point secondary = layout.NextSecondaryPosition();

        secondary.Y.ShouldBe(60);
    }

    /// <summary>
    /// Tests that <see cref="NodeLayoutContext.NextSecondaryPosition"/> advances its own cursor
    /// independently of the primary cursor.
    /// </summary>
    [Fact]
    public void NextSecondaryPosition_AdvancesIndependentlyOfPrimaryCursor()
    {
        NodeLayoutContext layout = new(primaryX: 700, secondaryX: 500, startY: 60);

        Point first = layout.NextSecondaryPosition();
        Point second = layout.NextSecondaryPosition();

        first.X.ShouldBe(500);
        first.Y.ShouldBe(60);
        second.X.ShouldBe(500);
        second.Y.ShouldBe(220);
    }

    /// <summary>
    /// Tests that <see cref="NodeLayoutContext.AdvancePrimaryYTo"/> only moves the cursor forward
    /// and never rewinds it to an earlier position.
    /// </summary>
    [Theory]
    [InlineData(30, 60)]
    [InlineData(500, 500)]
    public void AdvancePrimaryYTo_OnlyMovesCursorForward(double advanceTo, double expectedY)
    {
        NodeLayoutContext layout = new(primaryX: 700, secondaryX: 500, startY: 60);

        layout.AdvancePrimaryYTo(advanceTo);

        layout.CurrentPrimaryY.ShouldBe(expectedY);
    }

    /// <summary>
    /// Tests that <see cref="NodeLayoutContext.CurrentPrimaryY"/> does not advance the cursor on read.
    /// </summary>
    [Fact]
    public void CurrentPrimaryY_DoesNotAdvanceOnRead()
    {
        NodeLayoutContext layout = new(primaryX: 700, secondaryX: 500, startY: 60);

        double firstRead = layout.CurrentPrimaryY;
        double secondRead = layout.CurrentPrimaryY;

        firstRead.ShouldBe(60);
        secondRead.ShouldBe(60);
    }

    /// <summary>
    /// Tests that the default constructor parameters match the documented defaults.
    /// </summary>
    [Fact]
    public void Constructor_DefaultParameters_MatchDocumentedDefaults()
    {
        NodeLayoutContext layout = new();

        Point primary = layout.NextPrimaryPosition();
        Point secondary = layout.NextSecondaryPosition();

        primary.X.ShouldBe(300);
        primary.Y.ShouldBe(60);
        secondary.X.ShouldBe(60);
    }
}
