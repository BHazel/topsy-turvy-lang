namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an import statement.
/// </summary>
public class ImportNode : TopsyTurvyStatement
{
    /// <summary>
    /// Gets or initialises the path to the file to import.
    /// </summary>
    public required string FilePath { get; init; }
}
