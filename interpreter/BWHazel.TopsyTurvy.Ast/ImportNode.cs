namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an import directive.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>PRAY ADMIT</c> import directive in Topsy Turvy.  It imports another <c>.topsy</c> file into
/// the current programme, making all functions defined in that file available to the current programme.  Every import
/// directive consists of a single <see cref="FilePath"/> string identifying the file to import.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to import a file:
/// </para>
/// <code>
/// PRAY ADMIT "mikado-punishments.topsy"
/// </code>
/// <para>
/// would be represented in the AST as an <see cref="ImportNode"/> with:
/// * The <see cref="FilePath"/> property set to <c>mikado-utils.topsy</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new ImportNode()
/// {
///     FilePath = "mikado-utils.topsy",
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class ImportNode : Statement
{
    /// <summary>
    /// Gets or initialises the path to the file to import.
    /// </summary>
    public required string FilePath { get; init; }
}
