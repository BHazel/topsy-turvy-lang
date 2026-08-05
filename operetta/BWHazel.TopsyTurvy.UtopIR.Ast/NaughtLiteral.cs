namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Represents the <c>naught</c> null literal value.
/// </summary>
/// <remarks>
/// A singleton used as the <see cref="LiteralOperand.Value"/> of a <see cref="LiteralOperand"/>
/// when appointing no pointee to a pointer, array or <c>yarn</c> string variable.  Kept as a distinct type,
/// rather than a bare CLR <c>null</c>, so every switch over the value of a <see cref="LiteralOperand"/>
/// can pattern-match <c>naught</c> by type rather than needing a separate null check.
/// <see cref="NaughtLiteral"/> is a C# implementation detail only: <c>naught</c> is not itself a
/// <see cref="UtopIRType"/>, and no type-checking layer treats it as one.
/// </remarks>
public sealed class NaughtLiteral
{
    /// <summary>
    /// Gets the sole <see cref="NaughtLiteral"/> instance.
    /// </summary>
    public static readonly NaughtLiteral Instance = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NaughtLiteral"/> class.
    /// </summary>
    private NaughtLiteral()
    {
    }
}
