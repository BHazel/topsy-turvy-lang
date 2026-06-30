namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Holds the most recent parsed state for a single open document.
/// </summary>
/// <remarks>
/// This establishes the "last good parse" pattern for a document where the symbol table from the most recent successful parse is
/// stored alongside the current source text, as used in the analysis system.
/// </remarks>
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
