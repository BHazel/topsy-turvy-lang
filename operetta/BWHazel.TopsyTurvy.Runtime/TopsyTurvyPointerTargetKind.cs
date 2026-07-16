namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Defines constants to identify the kind of location a <see cref="TopsyTurvyPointerTarget"/> refers to.
/// </summary>
internal enum TopsyTurvyPointerTargetKind
{
    /// <summary>A named scalar variable.</summary>
    Variable,

    /// <summary>A position within a shared array element list.</summary>
    ArrayElement,

    /// <summary>A character position within a named string variable.</summary>
    StringElement
}
