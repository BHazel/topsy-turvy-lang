namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a function parameter with its declared name, type and source location.
/// </summary>
/// <remarks>
/// The <see cref="Type"/> corresponds to the <c>AS A &lt;type&gt;</c> annotation
///  that is required for every parameter in a <c>UNDER THE TERMS OF</c> clause.
/// </remarks>
/// <param name="Name">The parameter identifier as written.</param>
/// <param name="Type">The declared <see cref="LiteralType"/> for this parameter.</param>
/// <param name="Span">The source span covering the parameter identifier token.</param>
public record TypedParameter(string Name, LiteralType Type, SourceSpan Span);
