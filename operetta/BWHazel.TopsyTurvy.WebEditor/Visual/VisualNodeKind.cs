namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Defines constants for the semantic colour category of a visual node header.
/// </summary>
/// <remarks>
/// Each value maps to a <c>vn-header-{kind}</c> CSS class defined in <c>app.css</c>.
/// </remarks>
public enum VisualNodeKind
{
    /// <summary>Programme root node (<c>HARK!</c> … <c>FINALE.</c>).</summary>
    Program,

    /// <summary>Variable or constant declaration (<c>PRAY WELCOME</c>).</summary>
    Declaration,

    /// <summary>Variable or array-element assignment (<c>IS APPOINTED</c>).</summary>
    Assignment,

    /// <summary>Print statement (<c>BEHOLD</c>).</summary>
    Print,

    /// <summary>Input statement (<c>PRAY TELL</c>).</summary>
    Input,

    /// <summary>Conditional statement (<c>SHOULD IT TRANSPIRE THAT</c>).</summary>
    Conditional,

    /// <summary>Loop statement (<c>BY A LEGAL FICTION</c>).</summary>
    Loop,

    /// <summary>Function definition or return (<c>IT IS MY DUTY TO PERFORM</c> / <c>MY DUTY IS DISCHARGED.</c>).</summary>
    Function,

    /// <summary>Operator expression node.</summary>
    Operator,

    /// <summary>Literal value node.</summary>
    Literal,

    /// <summary>Identifier (variable reference) node.</summary>
    Identifier,

    /// <summary>Function parameter reference node.</summary>
    Parameter,

    /// <summary>Control-flow node (break, continue, guard).</summary>
    ControlFlow,

    /// <summary>Error-handling node (throw, try-catch, assert).</summary>
    ErrorHandling,

    /// <summary>Catch-all for placeholder and deferred node types.</summary>
    Other,
}
