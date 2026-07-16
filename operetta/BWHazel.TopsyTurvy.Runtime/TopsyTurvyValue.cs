using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Represents a runtime value in the Topsy Turvy interpreter.
/// </summary>
/// <remarks>
/// <para>
/// Instances of <see cref="TopsyTurvyValue"/> only exist at runtime and are not part of the AST.  They are created using static
/// factory methods for each supported literal type to ensure consistency between the underlying .NET value and the Topsy Turvy
/// literal type.
/// </para>
/// <para>
/// The supported types and their .NET representations are:
/// </para>
/// <para>
/// | Topsy Turvy keyword     | <see cref="LiteralType"/>  | .NET type                    |
/// |-------------------------|----------------------------|------------------------------|
/// | `PEER`                  | `Integer`                  | `int`                        |
/// | `CHANCELLOR`            | `Long`                     | `long`                       |
/// | `PIRATE`                | `Short`                    | `short`                      |
/// | `SAUSAGE-ROLL`          | `SignedByte`               | `sbyte`                      |
/// | `STANDING PEER`         | `UnsignedInteger`          | `uint`                       |
/// | `STANDING CHANCELLOR`   | `UnsignedLong`             | `ulong`                      |
/// | `STANDING PIRATE`       | `UnsignedShort`            | `ushort`                     |
/// | `STANDING SAUSAGE-ROLL` | `Byte`                     | `byte`                       |
/// | `FATHOM`                | `Double`                   | `double`                     |
/// | `FOOT`                  | `Single`                   | `float`                      |
/// | `YARN`                  | `String`                   | `string`                     |
/// | `STITCH`                | `Char`                     | `char`                       |
/// | `DECREE`                | `Boolean`                  | `bool`                       |
/// | `NAUGHT`                | `Null`                     | `null`                       |
/// | `A LITTLE LIST OF`      | `Array`                    | `List&lt;TopsyTurvyValue&gt;`|
/// | `A GALLERY PICTURE OF`  | `Pointer`                  | `TopsyTurvyPointerTarget`    |
///
/// </para>
/// </remarks>
public sealed class TopsyTurvyValue
{
    /// <summary>
    /// Initialises a new instance of the <see cref="TopsyTurvyValue"/> class with the specified raw value and type.
    /// </summary>
    /// <param name="rawValue">The underlying .NET value.</param>
    /// <param name="literalType">The Topsy Turvy literal type of the value.</param>
    private TopsyTurvyValue(object? rawValue, LiteralType literalType)
    {
        this.RawValue = rawValue;
        this.LiteralType = literalType;
    }

    /// <summary>
    /// Gets the underlying .NET value.
    /// </summary>
    public object? RawValue { get; }

    /// <summary>
    /// Gets the Topsy Turvy literal type of this value.
    /// </summary>
    public LiteralType LiteralType { get; }

    /// <summary>
    /// Creates a 32-bit signed integer value (<c>PEER</c>).
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the integer value.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.Integer(42);
    /// </code>
    public static TopsyTurvyValue Integer(int value) => new(value, LiteralType.Integer);

    /// <summary>
    /// Creates a 64-bit signed integer value (<c>CHANCELLOR</c>).
    /// </summary>
    /// <param name="value">The long value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the 64-bit signed integer.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.Long(1234567890123456789L);
    /// </code>
    public static TopsyTurvyValue Long(long value) => new(value, LiteralType.Long);

    /// <summary>
    /// Creates a 16-bit signed integer value (<c>PIRATE</c>).
    /// </summary>
    /// <param name="value">The short value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the 16-bit signed integer.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.Short(30000);
    /// </code>
    public static TopsyTurvyValue Short(short value) => new(value, LiteralType.Short);

    /// <summary>
    /// Creates an 8-bit signed integer value (<c>SAUSAGE-ROLL</c>).
    /// </summary>
    /// <param name="value">The signed byte value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the 8-bit signed integer.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.SignedByte(127);
    /// </code>
    public static TopsyTurvyValue SignedByte(sbyte value) => new(value, LiteralType.SignedByte);

    /// <summary>
    /// Creates a 32-bit unsigned integer value (<c>STANDING PEER</c>).
    /// </summary>
    /// <param name="value">The unsigned integer value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the 32-bit unsigned integer.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.UnsignedInteger(4000000000u);
    /// </code>
    public static TopsyTurvyValue UnsignedInteger(uint value) => new(value, LiteralType.UnsignedInteger);

    /// <summary>
    /// Creates a 64-bit unsigned integer value (<c>STANDING CHANCELLOR</c>).
    /// </summary>
    /// <param name="value">The unsigned long value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the 64-bit unsigned integer.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.UnsignedLong(18000000000000000000UL);
    /// </code>
    public static TopsyTurvyValue UnsignedLong(ulong value) => new(value, LiteralType.UnsignedLong);

    /// <summary>
    /// Creates a 16-bit unsigned integer value (<c>STANDING PIRATE</c>).
    /// </summary>
    /// <param name="value">The unsigned short value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the 16-bit unsigned integer.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.UnsignedShort(60000);
    /// </code>
    public static TopsyTurvyValue UnsignedShort(ushort value) => new(value, LiteralType.UnsignedShort);

    /// <summary>
    /// Creates an 8-bit unsigned integer value (<c>STANDING SAUSAGE-ROLL</c>).
    /// </summary>
    /// <param name="value">The byte value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the 8-bit unsigned integer.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.Byte(255);
    /// </code>
    public static TopsyTurvyValue Byte(byte value) => new(value, LiteralType.Byte);

    /// <summary>
    /// Creates a 64-bit double-precision floating-point value (<c>FATHOM</c>).
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the double-precision float.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.Double(3.14159265358979);
    /// </code>
    public static TopsyTurvyValue Double(double value) => new(value, LiteralType.Double);

    /// <summary>
    /// Creates a 32-bit single-precision floating-point value (<c>FOOT</c>).
    /// </summary>
    /// <param name="value">The single-precision float value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the single-precision float.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.Single(3.14f);
    /// </code>
    public static TopsyTurvyValue Single(float value) => new(value, LiteralType.Single);

    /// <summary>
    /// Creates a string value (<c>YARN</c>).
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the string.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.String("I am the very model of a modern Major-General!");
    /// </code>
    public static TopsyTurvyValue String(string value) => new(value, LiteralType.String);

    /// <summary>
    /// Creates a single-character value (<c>STITCH</c>).
    /// </summary>
    /// <param name="value">The character value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the character.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.Char('G');
    /// </code>
    public static TopsyTurvyValue Char(char value) => new(value, LiteralType.Char);

    /// <summary>
    /// Creates a boolean value (<c>DECREE</c>).
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the boolean.</returns>
    /// <code>
    /// TopsyTurvyValue yes = TopsyTurvyValue.Boolean(true);
    /// TopsyTurvyValue no = TopsyTurvyValue.Boolean(false);
    /// </code>
    public static TopsyTurvyValue Boolean(bool value) => new(value, LiteralType.Boolean);

    /// <summary>
    /// Creates a null value (<c>NAUGHT</c>).
    /// </summary>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing null.</returns>
    /// <code>
    /// TopsyTurvyValue value = TopsyTurvyValue.Null();
    /// </code>
    public static TopsyTurvyValue Null() => new(null, LiteralType.Null);

    /// <summary>
    /// Creates an array value from a list of elements (<c>A LITTLE LIST OF</c>).
    /// </summary>
    /// <param name="elements">The ordered list of elements.</param>
    /// <remarks>
    /// The list is stored by reference; two array variables assigned to the same list share the same underlying storage,
    /// implementing reference semantics for arrays.
    /// </remarks>
    /// <returns>A new <see cref="TopsyTurvyValue"/> wrapping the element list.</returns>
    /// <code>
    /// List&lt;TopsyTurvyValue&gt; elements =
    /// [
    ///     TopsyTurvyValue.Integer(1),
    ///     TopsyTurvyValue.Integer(2),
    ///     TopsyTurvyValue.Integer(3),
    /// ];
    /// 
    /// TopsyTurvyValue value = TopsyTurvyValue.Array(elements);
    /// </code>
    public static TopsyTurvyValue Array(List<TopsyTurvyValue> elements) => new(elements, LiteralType.Array);

    /// <summary>
    /// Creates a pointer value referring to the given target (<c>A GALLERY PICTURE OF</c>).
    /// </summary>
    /// <param name="target">The location this pointer refers to.</param>
    /// <remarks>
    /// An unassigned pointer is represented by <see cref="Null"/>, not by this factory: a
    /// <see cref="LiteralType"/><c>.Pointer</c> value always carries a real target.
    /// </remarks>
    /// <returns>A new <see cref="TopsyTurvyValue"/> wrapping the pointer target.</returns>
    /// <code>
    /// TopsyTurvyPointerTarget target = TopsyTurvyPointerTarget.ForVariable(environment, "Number");
    /// TopsyTurvyValue value = TopsyTurvyValue.Pointer(target);
    /// </code>
    public static TopsyTurvyValue Pointer(TopsyTurvyPointerTarget target) => new(target, LiteralType.Pointer);

    /// <summary>
    /// Evaluates the truthiness of this value in a boolean context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For each supported type, a value is considered truthy when:
    /// * All integer types (<c>PEER</c>, <c>CHANCELLOR</c>, <c>PIRATE</c>, <c>SAUSAGE-ROLL</c> and their unsigned variants): Any non-zero value.
    /// * Floating-point types (<c>FATHOM</c>, <c>FOOT</c>): Any non-zero value.
    /// * <c>YARN</c>: Any non-empty string.
    /// * <c>STITCH</c>: Any character other than the null character (<c>'\0'</c>).
    /// * <c>DECREE</c>: <c>true</c>.
    /// * <c>NAUGHT</c>: Never truthy.
    /// * Arrays: Non-empty.
    /// * Pointers: Always truthy, since an unassigned pointer is represented by <c>NAUGHT</c> rather than a pointer value.
    /// </para>
    /// </remarks>
    /// <returns><c>true</c> for truthy values; otherwise <c>false</c>.</returns>
    /// <code>
    /// TopsyTurvyValue.Integer(1).IsTruthy();       // true
    /// TopsyTurvyValue.Integer(0).IsTruthy();       // false
    /// TopsyTurvyValue.String("hello").IsTruthy();  // true
    /// TopsyTurvyValue.String("").IsTruthy();       // false
    /// TopsyTurvyValue.Null().IsTruthy();           // false
    /// </code>
    public bool IsTruthy() => LiteralType switch
    {
        LiteralType.Integer => (int)this.RawValue! != 0,
        LiteralType.Long => (long)this.RawValue! != 0L,
        LiteralType.Short => (short)this.RawValue! != 0,
        LiteralType.SignedByte => (sbyte)this.RawValue! != 0,
        LiteralType.UnsignedInteger => (uint)this.RawValue! != 0u,
        LiteralType.UnsignedLong => (ulong)this.RawValue! != 0UL,
        LiteralType.UnsignedShort => (ushort)this.RawValue! != 0,
        LiteralType.Byte => (byte)this.RawValue! != 0,
        LiteralType.Double => (double)this.RawValue! != 0.0,
        LiteralType.Single => (float)this.RawValue! != 0.0f,
        LiteralType.String => !string.IsNullOrEmpty((string)this.RawValue!),
        LiteralType.Char => (char)this.RawValue! != '\0',
        LiteralType.Boolean => (bool)this.RawValue!,
        LiteralType.Null => false,
        LiteralType.Array => ((List<TopsyTurvyValue>)this.RawValue!).Count > 0,
        LiteralType.Pointer => true,
        _ => true
    };

    /// <summary>
    /// Casts this value to the specified target type.
    /// </summary>
    /// <param name="targetType">The target Topsy Turvy type.</param>
    /// <remarks>
    /// <para>
    /// All numeric types (<c>PEER</c>, <c>CHANCELLOR</c>, <c>PIRATE</c>, <c>SAUSAGE-ROLL</c> and their unsigned
    /// variants, <c>FATHOM</c>, <c>FOOT</c>) can be cast to any other numeric type.  Integer values are truncated or
    /// widened as needed and floating-point values lose the fractional part when cast to an integer type.
    /// </para>
    /// <para>
    /// <c>STITCH</c> casts to integer types as the Unicode code-point of the character, whereas integer types cast to
    /// <c>STITCH</c> using the code-point as a character.  Casting a <c>YARN</c> to <c>STITCH</c> takes the first
    /// character of the string.  Casting an empty string is a runtime error.
    /// </para>
    /// <para>
    /// Arrays can only be cast to <c>YARN</c> (via <see cref="ToString"/>) or <c>DECREE</c> (via <see cref="IsTruthy"/>).
    /// Casting an array to any other type throws a <see cref="TopsyTurvyRuntimeException"/>.  Pointers follow the
    /// same restriction: a pointer can only be cast to <c>YARN</c>, which renders its synthetic display address, or
    /// <c>DECREE</c>.
    /// </para>
    /// </remarks>
    /// <returns>A new <see cref="TopsyTurvyValue"/> of the target type.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the value cannot be cast to the target type.</exception>
    /// <code>
    /// // Casting to Long
    /// TopsyTurvyValue peer = TopsyTurvyValue.Integer(42);
    /// TopsyTurvyValue chancellor = peer.CastTo(LiteralType.Long);
    ///
    /// // Casting to Integer
    /// TopsyTurvyValue fathom = TopsyTurvyValue.Double(3.9);
    /// TopsyTurvyValue truncated = fathom.CastTo(LiteralType.Integer);
    /// </code>
    public TopsyTurvyValue CastTo(LiteralType targetType)
    {
        if (LiteralType == targetType)
        {
            return this;
        }

        if (this.LiteralType == LiteralType.Array && targetType is not (LiteralType.String or LiteralType.Boolean))
        {
            throw new TopsyTurvyRuntimeException($"Cannot cast an array to {targetType}.");
        }

        if (this.LiteralType == LiteralType.Pointer && targetType is not (LiteralType.String or LiteralType.Boolean))
        {
            throw new TopsyTurvyRuntimeException($"Cannot cast a pointer to {targetType}.");
        }

        return targetType switch
        {
            LiteralType.Integer => Integer((int)this.ToInt64()),
            LiteralType.Long => Long(this.ToInt64()),
            LiteralType.Short => Short((short)this.ToInt64()),
            LiteralType.SignedByte => SignedByte((sbyte)this.ToInt64()),
            LiteralType.UnsignedInteger => UnsignedInteger((uint)this.ToUInt64()),
            LiteralType.UnsignedLong => UnsignedLong(this.ToUInt64()),
            LiteralType.UnsignedShort => UnsignedShort((ushort)this.ToUInt64()),
            LiteralType.Byte => Byte((byte)this.ToUInt64()),
            LiteralType.Double => Double(this.ToDouble()),
            LiteralType.Single => Single((float)this.ToDouble()),
            LiteralType.Char => Char(this.ToChar()),
            LiteralType.String => String(this.ToString()),
            LiteralType.Boolean => Boolean(this.IsTruthy()),
            LiteralType.Null => Null(),
            _ => this
        };
    }

    /// <summary>
    /// Returns the string representation of this value.
    /// </summary>
    /// <remarks>
    /// Values render according to the following rules:
    /// * Numeric values render as their decimal representation.
    /// * String values render as the raw string, not quoted.
    /// * Boolean values render as <c>VERITY</c> (<c>true</c>) or <c>NAY</c> (<c>false</c>).
    /// * Character values render as the bare character, not quoted.
    /// * Null renders as <c>NAUGHT</c>.
    /// * Arrays render as a comma-separated, bracket-enclosed list.
    /// * Pointers render as a synthetic, address-shaped hexadecimal string derived from the target handle identifier, with no relation to any real memory location.
    /// * All other types use the default .NET <see cref="object.ToString"/> of their underlying value.
    /// </remarks>
    /// <returns>A string representation of this value.</returns>
    /// <code>
    /// TopsyTurvyValue.Boolean(true).ToString();   // "VERITY"
    /// TopsyTurvyValue.Boolean(false).ToString();  // "NAY"
    /// TopsyTurvyValue.Null().ToString();          // "NAUGHT"
    /// TopsyTurvyValue.Integer(42).ToString();     // "42"
    /// TopsyTurvyValue.Char('G').ToString();       // "G"
    /// </code>
    public override string ToString() => LiteralType switch
    {
        LiteralType.Boolean => (bool)this.RawValue!
            ? Keywords.Literals.Verity
            : Keywords.Literals.Nay,
        LiteralType.Null => Keywords.Literals.Naught,
        LiteralType.Array => "[" + string.Join(", ", ((List<TopsyTurvyValue>)this.RawValue!).Select(v => v.ToString())) + "]",
        LiteralType.Pointer => ((TopsyTurvyPointerTarget)this.RawValue!).ToString(),
        LiteralType.Char => ((char)this.RawValue!).ToString(),
        _ => this.RawValue?.ToString() ?? Keywords.Literals.Naught
    };

    /// <summary>
    /// Converts this numeric value to a <see cref="long"/> for use in integer cast operations.
    /// </summary>
    /// <remarks>
    /// Unsigned values larger than <see cref="long.MaxValue"/> are truncated.  Floating-point values lose their
    /// fractional part.  String values are parsed but an unparseable string throws a <see cref="TopsyTurvyRuntimeException"/>.
    /// </remarks>
    /// <returns>The value represented as a <see cref="long"/>.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the source type cannot be converted to an integer.</exception>
    private long ToInt64() => this.LiteralType switch
    {
        LiteralType.Integer => (long)(int)this.RawValue!,
        LiteralType.Long => (long)this.RawValue!,
        LiteralType.Short => (long)(short)this.RawValue!,
        LiteralType.SignedByte => (long)(sbyte)this.RawValue!,
        LiteralType.UnsignedInteger => (long)(uint)this.RawValue!,
        LiteralType.UnsignedLong => (long)(ulong)this.RawValue!,
        LiteralType.UnsignedShort => (long)(ushort)this.RawValue!,
        LiteralType.Byte => (long)(byte)this.RawValue!,
        LiteralType.Double => (long)(double)this.RawValue!,
        LiteralType.Single => (long)(float)this.RawValue!,
        LiteralType.Boolean => (bool)this.RawValue! ? 1L : 0L,
        LiteralType.Null => 0L,
        LiteralType.Char => (long)(char)this.RawValue!,
        LiteralType.String =>
            long.TryParse((string)this.RawValue!, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedLong)
                ? parsedLong
                : int.TryParse((string)this.RawValue!, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedInt)
                    ? (long)parsedInt
                    : throw new TopsyTurvyRuntimeException(this.GetInvalidCastErrorMessage(Keywords.TypeNames.Peer)),
        _ => throw new TopsyTurvyRuntimeException($"Cannot convert {this.LiteralType} to an integer.")
    };

    /// <summary>
    /// Converts this numeric value to a <see cref="ulong"/> for use in unsigned integer cast operations.
    /// </summary>
    /// <remarks>
    /// Negative signed values wrap according to two's-complement truncation.  String values are parsed as
    /// <see cref="ulong"/> but an unparseable string throws a <see cref="TopsyTurvyRuntimeException"/>.
    /// </remarks>
    /// <returns>The value represented as a <see cref="ulong"/>.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the source type cannot be converted to an unsigned integer.</exception>
    private ulong ToUInt64() => this.LiteralType switch
    {
        LiteralType.Integer => (ulong)(int)this.RawValue!,
        LiteralType.Long => (ulong)(long)this.RawValue!,
        LiteralType.Short => (ulong)(short)this.RawValue!,
        LiteralType.SignedByte => (ulong)(sbyte)this.RawValue!,
        LiteralType.UnsignedInteger => (ulong)(uint)this.RawValue!,
        LiteralType.UnsignedLong => (ulong)this.RawValue!,
        LiteralType.UnsignedShort => (ulong)(ushort)this.RawValue!,
        LiteralType.Byte => (ulong)(byte)this.RawValue!,
        LiteralType.Double => (ulong)(double)this.RawValue!,
        LiteralType.Single => (ulong)(float)this.RawValue!,
        LiteralType.Boolean => (bool)this.RawValue! ? 1UL : 0UL,
        LiteralType.Null => 0UL,
        LiteralType.Char => (ulong)(char)this.RawValue!,
        LiteralType.String =>
            ulong.TryParse((string)this.RawValue!, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsed)
                ? parsed
                : throw new TopsyTurvyRuntimeException(this.GetInvalidCastErrorMessage(Keywords.TypeNames.Standing + " " + Keywords.TypeNames.Peer)),
        _ => throw new TopsyTurvyRuntimeException($"Cannot convert {this.LiteralType} to an unsigned integer.")
    };

    /// <summary>
    /// Converts this value to a <see cref="double"/> for use in floating-point cast and arithmetic operations.
    /// </summary>
    /// <remarks>
    /// String values are parsed but an unparseable string throws a <see cref="TopsyTurvyRuntimeException"/>.
    /// </remarks>
    /// <returns>The value represented as a <see cref="double"/>.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the source type cannot be converted to a floating-point value.</exception>
    private double ToDouble() => this.LiteralType switch
    {
        LiteralType.Integer => (double)(int)this.RawValue!,
        LiteralType.Long => (double)(long)this.RawValue!,
        LiteralType.Short => (double)(short)this.RawValue!,
        LiteralType.SignedByte => (double)(sbyte)this.RawValue!,
        LiteralType.UnsignedInteger => (double)(uint)this.RawValue!,
        LiteralType.UnsignedLong => (double)(ulong)this.RawValue!,
        LiteralType.UnsignedShort => (double)(ushort)this.RawValue!,
        LiteralType.Byte => (double)(byte)this.RawValue!,
        LiteralType.Double => (double)this.RawValue!,
        LiteralType.Single => (double)(float)this.RawValue!,
        LiteralType.Boolean => (bool)this.RawValue! ? 1.0 : 0.0,
        LiteralType.Null => 0.0,
        LiteralType.Char => (double)(char)this.RawValue!,
        LiteralType.String =>
            double.TryParse((string)this.RawValue!, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                ? parsed
                : throw new TopsyTurvyRuntimeException(this.GetInvalidCastErrorMessage(Keywords.TypeNames.Fathom)),
        _ => throw new TopsyTurvyRuntimeException($"Cannot convert {this.LiteralType} to a floating-point value.")
    };

    /// <summary>
    /// Converts this value to a <see cref="char"/> for use in character cast operations.
    /// </summary>
    /// <remarks>
    /// Integer types cast using the Unicode code-point.  String values take the first character but casting an empty
    /// string throws a <see cref="TopsyTurvyRuntimeException"/>.
    /// </remarks>
    /// <returns>The value represented as a <see cref="char"/>.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the source type cannot be converted to a character.</exception>
    private char ToChar() => this.LiteralType switch
    {
        LiteralType.Integer => (char)(int)this.RawValue!,
        LiteralType.Long => (char)(long)this.RawValue!,
        LiteralType.Short => (char)(short)this.RawValue!,
        LiteralType.SignedByte => (char)(sbyte)this.RawValue!,
        LiteralType.UnsignedInteger => (char)(uint)this.RawValue!,
        LiteralType.UnsignedLong => (char)(ulong)this.RawValue!,
        LiteralType.UnsignedShort => (char)(ushort)this.RawValue!,
        LiteralType.Byte => (char)(byte)this.RawValue!,
        LiteralType.Double => (char)(double)this.RawValue!,
        LiteralType.Single => (char)(float)this.RawValue!,
        LiteralType.Boolean => (bool)this.RawValue! ? '\x01' : '\0',
        LiteralType.Null => '\0',
        LiteralType.Char => (char)this.RawValue!,
        LiteralType.String when !string.IsNullOrEmpty((string)this.RawValue!) => ((string)this.RawValue!)[0],
        LiteralType.String => throw new TopsyTurvyRuntimeException("Cannot cast an empty YARN to STITCH."),
        _ => throw new TopsyTurvyRuntimeException($"Cannot convert {this.LiteralType} to a character.")
    };

    /// <summary>
    /// Builds an error message for an invalid cast operation.
    /// </summary>
    /// <param name="targetType">The target Topsy Turvy type keyword.</param>
    /// <returns>An error message indicating the invalid cast.</returns>
    private string GetInvalidCastErrorMessage(string targetType) => $"Cannot cast '{this.RawValue}' to {targetType}.";
}
