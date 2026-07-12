using Blazor.Diagrams.Core.Geometry;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Tracks auto-layout cursors while <see cref="VisualGraphBuilder"/> builds a diagram.
/// </summary>
/// <remarks>
/// Statement nodes occupy the primary column whereas expression nodes occupy the secondary column and are placed
/// beside the statement that consumes them.  Calling <see cref="NextPrimaryPosition"/> advances the primary
/// cursor and resets the secondary cursor to the same Y coordinate so that expressions always start level with their
/// owning statement.
/// </remarks>
/// <param name="primaryX">X coordinate of the main (statement) column.</param>
/// <param name="secondaryX">X coordinate of the expression column, to the left of primary.</param>
/// <param name="startY">Initial Y for both cursors.</param>
public sealed class NodeLayoutContext(double primaryX = 300, double secondaryX = 60, double startY = 60)
{
    private const double RowSpacingY = 160;

    private readonly double primaryX = primaryX;
    private readonly double secondaryX = secondaryX;
    private double primaryY = startY;
    private double secondaryY = startY;

    /// <summary>
    /// Gets the vertical distance between successive rows.
    /// </summary>
    /// <remarks>
    /// Exposed so callers can offset child layouts.
    /// </remarks>
    internal static double RowSpacing => RowSpacingY;

    /// <summary>
    /// Gets the current primary Y cursor without advancing it, for use when spawning a child layout.
    /// </summary>
    public double CurrentPrimaryY => this.primaryY;

    /// <summary>
    /// Gets the next position for a statement node and resets the secondary column to the same Y.
    /// </summary>
    /// <returns>The next position for a statement node.</returns>
    public Point NextPrimaryPosition()
    {
        Point position = new(this.primaryX, this.primaryY);
        this.secondaryY = this.primaryY;
        this.primaryY += RowSpacingY;
        return position;
    }

    /// <summary>
    /// Gets the next position for an expression node in the secondary column.
    /// </summary>
    /// <returns>The next position for an expression node.</returns>
    public Point NextSecondaryPosition()
    {
        Point position = new(this.secondaryX, this.secondaryY);
        this.secondaryY += RowSpacingY;
        return position;
    }

    /// <summary>
    /// Advances the primary Y cursor to at least <paramref name="y"/>.
    /// </summary>
    /// <param name="y">Minimum Y value for the cursor.</param>
    /// <remarks>
    /// This enables the next <see cref="NextPrimaryPosition"/> call to land below a block
    /// whose branches extend deeper than the default row spacing would place it.
    /// </remarks>
    public void AdvancePrimaryYTo(double y)
    {
        if (y > this.primaryY)
        {
            this.primaryY = y;
        }
    }
}
