using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.TypeChecker;

/// <summary>
/// Entry point for type-checking a parsed Topsy Turvy programme.
/// </summary>
/// <remarks>
/// <para>
/// The type checker runs after a successful parse and before execution.  It performs a two-pass walk
/// over the AST: the first pass collects all function signatures (enabling forward references), and
/// the second checks every expression and statement for type correctness.
/// </para>
/// <para>
/// Errors stop execution but warnings do not.  The result includes a <see cref="SemanticModel"/> that
/// downstream components can query for inferred expression types, declared symbol types and function
/// signatures.
/// </para>
/// <para>
/// The type checker defines a single <see cref="Check"/> method that accepts a populated <see cref="ProgramNode"/>
/// and returns a <see cref="TypeCheckResult"/>.  The result contains any diagnostics and the semantic model.
/// </para>
/// <code>
/// string sourceText = File.ReadAllText("programme.topsy");
/// TopsyTurvyParser parser = new();
/// ProgramNode program = parser.Parse(sourceText);
///
/// TopsyTurvyTypeChecker typeChecker = new();
/// TypeCheckResult result = typeChecker.Check(program);
/// </code>
/// </remarks>
public sealed class TopsyTurvyTypeChecker
{
    /// <summary>
    /// Type-checks the given programme and returns the result.
    /// </summary>
    /// <param name="program">The parsed programme to check.</param>
    /// <returns>A <see cref="TypeCheckResult"/> containing any diagnostics and the semantic model.</returns>
    public TypeCheckResult Check(ProgramNode program)
    {
        TypeCheckVisitor visitor = new();
        return visitor.Visit(program);
    }
}
