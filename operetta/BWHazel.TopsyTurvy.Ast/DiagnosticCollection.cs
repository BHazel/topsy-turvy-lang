using System.Collections.Generic;
using System.Linq;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Collects and manages a set of diagnostics.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="DiagnosticCollection"/> accumulates <see cref="Diagnostic"/>s produced during parsing or
/// execution and exposes them as a read-only list.  The <see cref="HasErrors"/> property provides a quick check for
/// whether any <see cref="DiagnosticSeverity"/><c>.Error</c> diagnostics are present which callers use to decide whether
/// to abort further processing.  The collection is returned by interpreter execution and is also populated by
/// the parser when syntax errors are encountered.
/// </para>
/// <para>
/// There is no Topsy Turvy syntax that maps directly to <see cref="DiagnosticCollection"/>.  It is an infrastructure
/// type used for error reporting and is never directly instantiated by user code.
/// </para>
/// </remarks>
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
