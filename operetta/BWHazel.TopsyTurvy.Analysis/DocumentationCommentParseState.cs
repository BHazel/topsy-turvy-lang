using System.Collections.Generic;
using System.Text;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Holds all mutable state accumulated during a single <see cref="DocumentationCommentParser.Parse"/> pass.
/// </summary>
public sealed class DocumentationCommentParseState
{
    /// <summary>
    /// Gets or sets the name of the tag currently being accumulated, e.g. <c>LEGEND</c>, or <c>null</c> between tags.
    /// </summary>
    public string? CurrentSection { get; set; }

    /// <summary>
    /// Gets or sets the accumulator for the text content of <see cref="CurrentSection"/> across continuation lines.
    /// </summary>
    public StringBuilder? CurrentAccumulator { get; set; }

    /// <summary>
    /// Gets or sets the parameter name captured from the most recent <c>ARTICLE</c> tag header.
    /// </summary>
    public string? CurrentArticleName { get; set; }

    /// <summary>
    /// Gets or sets the type captured from the most recent <c>ARTICLE</c> or <c>CONSEQUENCE</c> tag header.
    /// </summary>
    public string? CurrentArticleType { get; set; }

    /// <summary>
    /// Gets or sets the exception name captured from the most recent <c>CURSES</c> tag header.
    /// </summary>
    public string? CurrentCursesName { get; set; }

    /// <summary>
    /// Gets or sets the exception type captured from the most recent <c>CURSES</c> tag header.
    /// </summary>
    public string? CurrentCursesType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether at least one recognised tag has been encountered.
    /// </summary>
    public bool AnyTagFound { get; set; }

    /// <summary>
    /// Gets or sets the accumulated text of the <c>LEGEND:</c> tag.
    /// </summary>
    /// <remarks>
    /// This maps to <see cref="DocumentationComment.Summary"/>.
    /// </remarks>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the accumulator for the text of the <c>RECITATIVE:</c> tag.
    /// </summary>
    /// <remarks>
    /// This maps to <see cref="DocumentationComment.Remarks"/>.
    /// </remarks>
    public StringBuilder? RemarksBuilder { get; set; }

    /// <summary>
    /// Gets or sets the accumulated parameter descriptions keyed by name from all <c>ARTICLE</c> tags.
    /// </summary>
    /// <remarks>
    /// This maps to <see cref="DocumentationComment.Parameters"/>.
    /// </remarks>
    public Dictionary<string, (string Type, string Description)>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the accumulated return-value description from the <c>CONSEQUENCE</c> tag.
    /// </summary>
    /// <remarks>
    /// This maps to <see cref="DocumentationComment.ReturnValue"/>.
    /// </remarks>
    public (string Type, string Description)? ReturnValue { get; set; }

    /// <summary>
    /// Gets or sets the accumulated thrown-value entries from all <c>CURSES</c> tags.
    /// </summary>
    /// <remarks>
    /// This maps to <see cref="DocumentationComment.Exceptions"/>.
    /// </remarks>
    public List<(string Name, string Type, string Description)>? Exceptions { get; set; }

    /// <summary>
    /// Gets or sets the accumulated code examples from all <c>CHORUS:</c> tags.
    /// </summary>
    /// <remarks>
    /// This maps to <see cref="DocumentationComment.Examples"/>.
    /// </remarks>
    public List<string>? Examples { get; set; }

    /// <summary>
    /// Gets or sets the accumulated see-also references from all <c>ENSEMBLE:</c> tags.
    /// </summary>
    /// <remarks>
    /// This maps to <see cref="DocumentationComment.SeeAlso"/>.
    /// </remarks>
    public List<string>? SeeAlso { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a <c>STATUTORY:</c> tag has been encountered.
    /// </summary>
    /// <remarks>
    /// This maps to <see cref="DocumentationComment.IsDeprecated"/>.
    /// </remarks>
    public bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets or sets the deprecation message from the <c>STATUTORY:</c> tag.
    /// </summary>
    /// <remarks>
    /// This maps to <see cref="DocumentationComment.DeprecationMessage"/>.
    /// </remarks>
    public string? DeprecationMessage { get; set; }
}
