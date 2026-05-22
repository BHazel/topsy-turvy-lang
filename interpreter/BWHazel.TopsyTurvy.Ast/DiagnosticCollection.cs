using System.Collections.Generic;
using System.Linq;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Collects and manages a set of diagnostics.
/// </summary>
public class DiagnosticCollection
{
    private readonly List<Diagnostic> diagnostics = [];

    /// <summary>
    /// Adds a diagnostic to the collection.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    public void Add(Diagnostic diagnostic) => this.diagnostics.Add(diagnostic);

    /// <summary>
    /// Gets the read-only list of all collected diagnostics.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => this.diagnostics.AsReadOnly();

    /// <summary>
    /// Returns true if the collection contains any diagnostics with Error severity.
    /// </summary>
    public bool HasErrors => this.diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Clears all diagnostics from the collection.
    /// </summary>
    public void Clear() => this.diagnostics.Clear();
}
