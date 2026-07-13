using System.Collections.Generic;

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

    /// <summary>
    /// Gets or sets the raw <c>PRAY ADMIT</c> import path strings declared by the document, exactly as written
    /// (not resolved to an absolute path). Populated from the last successful parse, alongside <see cref="SymbolTable"/>.
    /// </summary>
    public IReadOnlyList<string> ImportPaths { get; set; } = [];

    /// <summary>
    /// Gets or sets the namespace path declared by the document, e.g. <c>["Accounts", "Payroll"]</c>, or empty
    /// if it declares none.  Populated from the last successful parse, alongside <see cref="SymbolTable"/>.
    /// </summary>
    public IReadOnlyList<string> NamespacePath { get; set; } = [];
}
