namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Specifies the type of loop.
/// </summary>
public enum LoopType
{
    /// <summary>A loop that repeats indefinitely until broken.</summary>
    Infinite,

    /// <summary>A loop that increments a counter until a condition is met.</summary>
    Ascending,

    /// <summary>A loop that decrements a counter until a condition is met.</summary>
    Descending,

    /// <summary>A loop that continues while a condition is true.</summary>
    Whilst
}
