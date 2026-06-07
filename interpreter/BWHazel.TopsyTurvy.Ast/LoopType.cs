namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Specifies the type of loop.
/// </summary>
/// <remarks>
/// <para>
/// All loops in Topsy Turvy are declared with <c>BY A LEGAL FICTION</c> and closed with <c>THE TERM EXPIRES.</c>  The loop type
/// is determined by the modifier clause that immediately follows the optional label:
/// </para>
/// <para>
/// * <see cref="LoopType"/>.<c>Infinite</c>: No modifier repeats indefinitely as an infinite loop until a <c>THAT WILL DO.</c> break.
/// * <see cref="LoopType"/>.<c>Ascending</c>: Increments a counter variable until an expression returns <c>VERITY</c>, e.g. <c>ASCENDING lords UNTIL 10</c> increments <i>lords</i> by 1 each iteration, exiting when <i>lords</i> is 10 or greater.
/// * <see cref="LoopType"/>.<c>Descending</c>: Decrements a counter variable until an expression returns <c>VERITY</c>, e.g. <c>DESCENDING lords UNTIL 0</c> decrements <i>lords</i> by 1 each iteration, exiting when <i>lords</i> is 0 or less.
/// * <see cref="LoopType"/>.<c>Whilst</c>: Continues while an expression evaluates to <c>VERITY</c>, checked before each iteration, e.g. <c>WHILST LOWER DEGREE x AND 10</c> continues looping while <i>lords</i> is less than 10.
/// </para>
/// </remarks>
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
