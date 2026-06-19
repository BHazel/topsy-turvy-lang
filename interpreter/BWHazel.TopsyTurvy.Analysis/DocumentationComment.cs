using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Holds the parsed content of a Topsy Turvy documentation comment.
/// </summary>
/// <remarks>
/// <para>
/// A documentation comment is a standard block comment placed immediately before a variable
/// declaration or function definition.  Its content is organised by keyword tags with the
/// intended purposes:
/// * <c>LEGEND:</c> A brief summary.
/// * <c>RECITATIVE:</c> Additional remarks.
/// * <c>ARTICLE:</c> Parameter descriptions, with the format <c>&lt;parameter name&gt; (&lt;type&gt;): &lt;description&gt;</c>.
/// * <c>CONSEQUENCE:</c> Return value description, with the format <c>(&lt;type&gt;): &lt;description&gt;</c>.
/// * <c>CURSES:</c> Values that may be thrown, with the format <c>&lt;name&gt; (&lt;type&gt;): &lt;description&gt;</c>.
/// * <c>CHORUS:</c> Code examples.
/// * <c>ENSEMBLE:</c> See-also references.
/// * <c>STATUTORY:</c> A deprecation message.
/// </para>
/// <para>
/// All tags support multi-line content which is terminated by the next tag or the end of the
/// comment.  <c>LEGEND</c>, <c>RECITATIVE</c>, <c>CONSEQUENCE</c> and <c>STATUTORY</c> tags can appear once per block comment
/// while <c>ARTICLE</c> and <c>CURSES</c> are intended to appear multiple times, once per parameter or thrown value respectively.
/// The <c>CHORUS</c> and <c>ENSEMBLE</c> tags can also appear multiple times, once per example or "see also" reference respectively.
/// </para>
/// </remarks>
public record DocumentationComment
{
    /// <summary>
    /// Gets or initialises the one-line summary.
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Gets or initialises additional remarks.
    /// </summary>
    public string? Remarks { get; init; }

    /// <summary>
    /// Gets or initialises the parameter descriptions keyed by parameter name.
    /// </summary>
    /// <remarks>
    /// Each entry maps the parameter name to a tuple of the declared type and a description.
    /// Only populated for <see cref="SymbolKind.Function"/> symbols.
    /// </remarks>
    public IReadOnlyDictionary<string, (string Type, string Description)>? Parameters { get; init; }

    /// <summary>
    /// Gets or initialises the return value description.
    /// </summary>
    /// <remarks>
    /// Only populated for <see cref="SymbolKind.Function"/> symbols.
    /// </remarks>
    public (string Type, string Description)? ReturnValue { get; init; }

    /// <summary>
    /// Gets or initialises the list of documented thrown values.
    /// </summary>
    public IReadOnlyList<(string Name, string Type, string Description)>? Exceptions { get; init; }

    /// <summary>
    /// Gets or initialises code examples.
    /// </summary>
    public IReadOnlyList<string>? Examples { get; init; }

    /// <summary>
    /// Gets or initialises see-also references.
    /// </summary>
    public IReadOnlyList<string>? SeeAlso { get; init; }

    /// <summary>
    /// Gets or initialises a value indicating whether the symbol is deprecated.
    /// </summary>
    public bool IsDeprecated { get; init; }

    /// <summary>
    /// Gets or initialises the deprecation message.
    /// </summary>
    public string? DeprecationMessage { get; init; }
}
