using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a namespace recognition directive.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>PRAY RECOGNISE</c> directive in Topsy Turvy.  It opens a namespace so
/// its functions may thereafter be called by bare name, without a fully-qualified <c>SUMMON</c>
/// target, for the remainder of the file.  A file may contain more than one <see cref="RecogniseNode"/>,
/// and it is valid only as a top-level statement, never nested inside a function, loop, conditional,
/// or other block body.  Recognising a namespace has no dependency on that namespace file already
/// having been admitted via <see cref="ImportNode"/> in the same file: it affects only bare-name
/// resolution, not which functions exist.
/// </para>
/// <para>
/// Every recognition directive consists of an ordered <see cref="Path"/> of one or more segments.
/// Sub-namespaces are written either with the long-hand <c>WITH DISTRICT</c> phrase or the short-hand
/// <c>*</c> character.  Both forms produce the same <see cref="Path"/> shape.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy opening a nested namespace:
/// </para>
/// <code>
/// PRAY RECOGNISE Accounts WITH DISTRICT Payroll
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="RecogniseNode"/> with:
/// * The <see cref="Path"/> property set to <c>["Accounts", "Payroll"]</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new RecogniseNode()
/// {
///     Path = ["Accounts", "Payroll"],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class RecogniseNode : Statement
{
    /// <summary>
    /// Gets or initialises the ordered namespace path segments.
    /// </summary>
    public required IReadOnlyList<string> Path { get; init; }
}
