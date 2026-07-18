namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Defines the kinds of symbols that can appear in a Topsy Turvy program.
/// </summary>
/// <remarks>
/// A symbol represents a named entity in a program, of which the following are supported:
/// * <see cref="SymbolKind"/><c>.Variable</c>: A variable as used in a declaration, assignment or reference.
/// * <see cref="SymbolKind"/><c>.Function</c>: A function as used in a definition or reference.
/// * <see cref="SymbolKind"/><c>.Parameter</c>: A function parameter as used in a function definition or reference within the function.
/// * <see cref="SymbolKind"/><c>.Namespace</c>: A namespace declaration.
/// </remarks>
public enum SymbolKind
{
    /// <summary>A variable.</summary>
    Variable,

    /// <summary>A function.</summary>
    Function,

    /// <summary>A function parameter.</summary>
    Parameter,

    /// <summary>A namespace declaration.</summary>
    Namespace
}
