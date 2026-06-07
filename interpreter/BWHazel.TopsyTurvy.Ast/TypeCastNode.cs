namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Base class for type casting operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TypeCastNode"/> is the common base for the two forms of type casting in Topsy Turvy:
/// * <see cref="InPlaceCastNode"/> (<c>IS HENCEFORTH A</c>): Mutates a variable directly in-place.
/// * <see cref="ExpressionCastNode"/> (<c>AS IT WERE ... AS A</c>): Produces a cast value and stores it in the implicit <c>JUST SO</c> variable without modifying the original.
/// </para>
/// <para>
/// There is no Topsy Turvy syntax that maps directly to <see cref="TypeCastNode"/>.  It is a base class only and is
/// never directly instantiated.
/// </para>
/// </remarks>
public abstract class TypeCastNode : Statement
{
}
