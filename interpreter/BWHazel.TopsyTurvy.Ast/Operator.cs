namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Defines the operators supported by the Topsy Turvy language.
/// </summary>
/// <remarks>
/// <para>
/// Operators are used in <see cref="PrefixExpressionNode"/> to identify the operation being performed.  The different operators
/// are grouped by category:
/// </para>
/// <para>
/// **Arithmetic Operators** are binary operators that take two operands and produce a numeric result:
/// * <see cref="Operator"/>.<c>Sum</c>: Addition of 2 numbers, <c>x + y</c>: <c>SUM OF x AND y</c>.
/// * <see cref="Operator"/>.<c>Difference</c>: Subtraction of 2 numbers, <c>x - y</c>: <c>DIFFERENCE OF x AND y</c>.
/// * <see cref="Operator"/>.<c>Product</c>: Multiplication of 2 numbers, <c>x * y</c>: <c>PRODUCT OF x AND y</c>.
/// * <see cref="Operator"/>.<c>Quotient</c>: Division of 2 numbers, <c>x / y</c>: <c>QUOTIENT OF x AND y</c>.
/// * <see cref="Operator"/>.<c>Remainder</c>: Remainder of 2 numbers, <c>x % y</c>: <c>REMAINDER OF x AND y</c>.
/// * <see cref="Operator"/>.<c>Larger</c>: Maximum of 2 numbers, <c>max(x, y)</c>: <c>LARGER OF x AND y</c>.
/// * <see cref="Operator"/>.<c>Smaller</c>: Minimum of 2 numbers, <c>min(x, y)</c>: <c>SMALLER OF x AND y</c>.
/// </para>
/// <para>
/// **Comparison Operators** are binary operators that take two operands and return a <c>DECREE</c> (boolean):
/// * <see cref="Operator"/>.<c>Alike</c>: Equality, <c>x == y</c>: <c>ALIKE x AND y</c>.
/// * <see cref="Operator"/>.<c>Unlike</c>: Inequality, <c>x != y</c>: <c>UNLIKE x AND y</c>.
/// * <see cref="Operator"/>.<c>PreAdamite</c>: Greater-Than, <c>x &gt; y</c>: <c>PRE-ADAMITE x AND y</c>.
/// * <see cref="Operator"/>.<c>LowerDegree</c>: Less-Than, <c>x &lt; y</c>: <c>LOWER DEGREE x AND y</c>.
/// </para>
/// <para>
/// **Logical Operators** operate on <c>DECREE</c> (boolean) values:
/// * <see cref="Operator"/>.<c>Both</c>: Logical And, <c>x &amp;&amp; y</c> <c>BOTH x AND y</c>.
/// * <see cref="Operator"/>.<c>Either</c>: Logical Or, <c>x || y</c> <c>EITHER x OR y</c>.
/// * <see cref="Operator"/>.<c>HardlyEver</c>: Logical Not, <c>!x</c> <c>HARDLY EVER x</c>.
/// </para>
/// <para>
/// **Variadic Operators** accept two or more operands, closed by <c>IF YOU PLEASE.</c>:
/// * <see cref="Operator"/>.<c>WovenOf</c>: String Concatenation, casting each operand to <c>YARN</c> (string): <c>WOVEN OF x AND y [AND z ...] IF YOU PLEASE.</c>.
/// * <see cref="Operator"/>.<c>AllOf</c>: Variadic Logic And, where all operands must be <c>VERITY</c> (true) for the whole expression to be <c>VERITY</c>: <c>ALL OF x AND y [AND z ...] IF YOU PLEASE.</c>.
/// * <see cref="Operator"/>.<c>AnyOf</c>: Variadic Logic Or, where at least one operand must be <c>VERITY</c> (true) for the whole expression to be <c>VERITY</c>: <c>ANY OF x AND y [AND z ...] IF YOU PLEASE.</c>.
/// </para>
/// <para>
/// **Function Call** accepts a function name and zero or more arguments, closed by <c>IF YOU PLEASE.</c>:
/// * <see cref="Operator"/>.<c>Summon</c>: Function Call: <c>SUMMON name WITH arg [AND arg ...] IF YOU PLEASE.</c>.
/// </para>
/// </remarks>
public enum Operator
{
    /// <summary>SUM OF</summary>
    Sum,

    /// <summary>DIFFERENCE OF</summary>
    Difference,

    /// <summary>PRODUCT OF</summary>
    Product,

    /// <summary>QUOTIENT OF</summary>
    Quotient,

    /// <summary>REMAINDER OF</summary>
    Remainder,

    /// <summary>LARGER OF</summary>
    Larger,

    /// <summary>SMALLER OF</summary>
    Smaller,

    /// <summary>BOTH</summary>
    Both,

    /// <summary>EITHER</summary>
    Either,

    /// <summary>HARDLY EVER</summary>
    HardlyEver,

    /// <summary>ALIKE</summary>
    Alike,

    /// <summary>UNLIKE</summary>
    Unlike,

    /// <summary>PRE-ADAMITE</summary>
    PreAdamite,

    /// <summary>LOWER DEGREE</summary>
    LowerDegree,

    /// <summary>WOVEN OF</summary>
    WovenOf,

    /// <summary>SUMMON</summary>
    Summon,

    /// <summary>ALL OF</summary>
    AllOf,

    /// <summary>ANY OF</summary>
    AnyOf
}
