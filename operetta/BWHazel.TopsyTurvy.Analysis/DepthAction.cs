namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Represents the depth adjustment applied to a line during formatting.
/// </summary>
/// <remarks>
/// This is intended to be used by formatters to determine how to adjust the indentation level of a line based on its context:
/// * <see cref="DepthAction"/><c>.None</c>: No change in depth.
/// * <see cref="DepthAction"/><c>.PostIncrease1</c>: Increase depth by 1 after writing the line.
/// * <see cref="DepthAction"/><c>.PostIncrease2</c>: Increase depth by 2 after writing the line.
/// * <see cref="DepthAction"/><c>.PreDecrease1</c>: Decrease depth by 1 before writing the line.
/// * <see cref="DepthAction"/><c>.PreDecrease2</c>: Decrease depth by 2 before writing the line.
/// * <see cref="DepthAction"/><c>.MidBlock</c>: Decrease by 1 before writing, then increase by 1 after as used in mid-block transitions.
/// </remarks>
public enum DepthAction
{
    /// <summary>No depth change.</summary>
    None,
    
    /// <summary>Increase depth by 1 after writing the line.</summary>
    PostIncrease1,
    
    /// <summary>Increase depth by 2 after writing the line.</summary>
    PostIncrease2,
    
    /// <summary>Decrease depth by 1 before writing the line.</summary>
    PreDecrease1,
    
    /// <summary>Decrease depth by 2 before writing the line.</summary>
    PreDecrease2,
    
    /// <summary>Decrease by 1 before writing, then increase by 1 after (mid-block transition).</summary>
    MidBlock
}
