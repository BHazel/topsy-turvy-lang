namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Represents the depth adjustment applied to a line during formatting.
/// </summary>
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
