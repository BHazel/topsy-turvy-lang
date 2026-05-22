using System.Collections.Generic;
using System.Linq;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Collects and manages a set of diagnostics.
/// </summary>
public class TopsyTurvyDiagnosticCollection
{
    private readonly List<TopsyTurvyDiagnostic> diagnostics = [];

    /// <summary>
    /// Adds a diagnostic to the collection.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    public void Add(TopsyTurvyDiagnostic diagnostic)
    {
        this.diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Gets the read-only list of all collected diagnostics.
    /// </summary>
    public IReadOnlyList<TopsyTurvyDiagnostic> Diagnostics => this.diagnostics.AsReadOnly();

    /// <summary>
    /// Returns true if the collection contains any diagnostics with Error severity.
    /// </summary>
    /// <returns><c>true</c> if there are error diagnostics, otherwise <c>false</c>.</returns>
    public bool HasErrors => this.diagnostics.Any(d => d.Severity == TopsyTurvyDiagnosticSeverity.Error);

    /// <summary>
    /// Clears all diagnostics from the collection.
    /// </summary>
    public void Clear()
    {
        this.diagnostics.Clear();
    }
}
