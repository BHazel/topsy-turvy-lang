namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Holds the most recent parsed state for a single open document.
/// </summary>
public class DocumentState
{
    /// <summary>
    /// Gets or sets the current raw source text of the document.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol table built from the last successful parse.
    /// </summary>
    /// <remarks>
    /// This will be <c>null</c> until the document has been successfully parsed at least once.
    /// </remarks>
    public SymbolTable? SymbolTable { get; set; }
}
