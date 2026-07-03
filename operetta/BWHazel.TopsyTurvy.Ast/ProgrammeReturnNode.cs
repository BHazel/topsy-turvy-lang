namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a top-level programme return statement that sets the OS exit code.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to <c>AND SO I FIND &lt;expr&gt;</c> appearing directly in the programme body, not inside a
/// function.  The expression must evaluate to a <c>PEER</c> value and the runtime uses it as the process exit
/// code.  Omitting this statement from the programme body implicitly returns exit code <c>0</c>.
/// </para>
/// <para>
/// For example, the following programme ends with exit code 42:
/// </para>
/// <code>
/// HARK! "Exit code demo"
/// AND SO I FIND 42
/// FINALE.
/// </code>
/// <para>
/// and would be represented in the AST as a <see cref="ProgrammeReturnNode"/> with:
/// * The <see cref="Value"/> property set to a <see cref="LiteralNode"/> of <c>42</c>.
/// </para>
/// </remarks>
public class ProgrammeReturnNode : Statement
{
    /// <summary>
    /// Gets or initialises the expression whose value is used as the OS exit code.
    /// </summary>
    public required Expression Value { get; init; }
}
