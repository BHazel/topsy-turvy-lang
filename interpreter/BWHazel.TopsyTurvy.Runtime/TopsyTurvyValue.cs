using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Represents a runtime value in the Topsy Turvy interpreter.
/// </summary>
/// <remarks>
/// Instances of <see cref="TopsyTurvyValue"/> only exist at runtime and are not part of the AST.  They are created using static
/// factory methods for each supported literl type to ensure consistency between the underlying .NET value and Topsy Turvy literal
/// type.
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
    /// Creates an integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <remarks>
    /// <code>
    /// TopsyTurvyValue integerValue = TopsyTurvyValue.Integer(20);
    /// </code>
    /// </remarks>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the integer value.</returns>
    public static TopsyTurvyValue Integer(int value) => new(value, LiteralType.Integer);

    /// <summary>
    /// Creates a floating-point value.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    /// <remarks>
    /// <code>
    /// TopsyTurvyValue floatValue = TopsyTurvyValue.Float(3.14);
    /// </code>
    /// </remarks>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the floating-point value.</returns>
    public static TopsyTurvyValue Float(double value) => new(value, LiteralType.Float);

    /// <summary>
    /// Creates a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <remarks>
    /// <code>
    /// TopsyTurvyValue stringValue = TopsyTurvyValue.String("Hello, World!");
    /// </code>
    /// </remarks>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the string.</returns>
    public static TopsyTurvyValue String(string value) => new(value, LiteralType.String);

    /// <summary>
    /// Creates a boolean value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <remarks>
    /// <code>
    /// TopsyTurvyValue booleanValue = TopsyTurvyValue.Boolean(true);
    /// </code>
    /// </remarks>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the boolean.</returns>
    public static TopsyTurvyValue Boolean(bool value) => new(value, LiteralType.Boolean);

    /// <summary>
    /// Creates a null value.
    /// </summary>
    /// <remarks>
    /// <code>
    /// TopsyTurvyValue nullValue = TopsyTurvyValue.Null();
    /// </code>
    /// </remarks>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing null.</returns>
    public static TopsyTurvyValue Null() => new(null, LiteralType.Null);

    /// <summary>
    /// Creates an array value from a list of elements.
    /// </summary>
    /// <param name="elements">The ordered list of elements.</param>
    /// <remarks>
    /// <para>
    /// The list is stored by reference; two array variables assigned to the same list share the same underlying storage.
    /// This implements reference semantics for arrays.
    /// </para>
    /// <code>
    /// TopsyTurvyValue arrayValue = TopsyTurvyValue.Array(new List&lt;TopsyTurvyValue&gt; { TopsyTurvyValue.Integer(1) });
    /// </code>
    /// </remarks>
    /// <returns>A new <see cref="TopsyTurvyValue"/> wrapping the element list.</returns>
    public static TopsyTurvyValue Array(List<TopsyTurvyValue> elements) => new(elements, LiteralType.Array);

    /// <summary>
    /// Evaluates the truthiness of this value in a Boolean context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For each supported type, a value to be considered "truthy" is:
    /// * **Integer**: Any non-zero value.
    /// * **Float**: Any non-zero value.
    /// * **String**: Any non-empty string.
    /// * **Boolean**: <c>true</c>
    /// * **Null**: Never "truthy".
    /// </para>
    /// <code>
    /// // "Truthy" Value
    /// TopsyTurvyValue value = TopsyTurvyValue.Integer(42);
    /// bool isTruthy = value.IsTruthy();
    /// 
    /// // Non-"Truthy" Value
    /// TopsyTurvyValue stringValue = TopsyTurvyValue.String(string.Empty);
    /// bool isStringTruthy = stringValue.IsTruthy();
    /// </code>
    /// </remarks>
    /// <returns><c>true</c> for "truthy" values, otherwise <c>false</c>.</returns>
    public bool IsTruthy() => LiteralType switch
    {
        LiteralType.Integer => (int)this.RawValue! != 0,
        LiteralType.Float => (double)this.RawValue! != 0.0,
        LiteralType.String => !string.IsNullOrEmpty((string)this.RawValue!),
        LiteralType.Boolean => (bool)this.RawValue!,
        LiteralType.Null => false,
        LiteralType.Array => ((List<TopsyTurvyValue>)this.RawValue!).Count > 0,
        _ => true
    };

    /// <summary>
    /// Casts this value to the specified target type
    /// </summary>
    /// <param name="targetType">The target Topsy Turvy type.</param>
    /// <remarks>
    /// <para>
    /// Types in Topsy Turvy can be cast to other target types:
    /// * **Integer** casts from other types:
    ///     * Floating-point values are truncated to their integer part.
    ///     * Boolean values are converted to 1 for <c>true</c> and 0 for <c>false</c>.
    ///     * Null values are converted to 0.
    ///     * Strings are parsed as integers if possible, otherwise an exception is thrown.
    /// * **Float** casts from other types:
    ///     * Integer values are converted to their floating-point representation.
    ///     * Boolean values are converted to 1.0 for <c>true</c> and 0.0 for <c>false</c>.
    ///     * Null values are converted to 0.0.
    ///     * Strings are parsed as floating-point numbers if possible, otherwise an exception is thrown.
    /// * **String** casts from other types:
    ///     * All types are converted to their string representation.
    /// * **Boolean** casts from other types:
    ///     * All types are evaluated for truthiness using the <see cref="IsTruthy"/> method.
    /// * **Null** casts from other types:
    ///     * All types are converted to null.
    /// * **Array** casts:
    ///     * Arrays can only be cast to <c>String</c> (via <see cref="ToString"/>) or <c>Boolean</c> (via <see cref="IsTruthy"/>).
    ///     * Casting an array to any other type throws a <see cref="TopsyTurvyRuntimeException"/>.
    ///     * Without this guard the <c>targetType</c> switch would fall to its <c>_ =&gt; this</c> arm and silently
    ///       return the array value labelled with the wrong <see cref="LiteralType"/>.
    /// </para>
    /// The following example demonstrates casting an integer value to a floating-point value:
    /// <code>
    /// TopsyTurvyValue integerValue = TopsyTurvyValue.Integer(42);
    /// TopsyTurvyValue floatValue = integerValue.CastTo(LiteralType.Float);
    /// </code>
    /// Whereas, the following example would throw an exception when attempting to cast a string that cannot be parsed as an integer:
    /// <code>
    /// TopsyTurvyValue stringValue = TopsyTurvyValue.String("Hello, World!");
    /// TopsyTurvyValue invalidCast = stringValue.CastTo(LiteralType.Integer);
    /// </code>
    /// </remarks>
    /// <returns>A new <see cref="TopsyTurvyValue"/> of the target type.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the value cannot be cast to the target type.</exception>
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

        return targetType switch
        {
            LiteralType.Integer => this.LiteralType switch
            {
                LiteralType.Float => Integer((int)(double)this.RawValue!),
                LiteralType.Boolean => Integer((bool)this.RawValue! ? 1 : 0),
                LiteralType.Null => Integer(0),
                LiteralType.String =>
                    int.TryParse((string)this.RawValue!, NumberStyles.Integer, CultureInfo.InvariantCulture, out int valueAsInt)
                        ? Integer(valueAsInt)
                        : throw new TopsyTurvyRuntimeException(this.GetInvalidCastErrorMessage(Keywords.TypeNames.Peer)),
                _ => this
            },
            LiteralType.Float => this.LiteralType switch
            {
                LiteralType.Integer => Float((double)(int)this.RawValue!),
                LiteralType.Boolean => Float((bool)this.RawValue! ? 1.0 : 0.0),
                LiteralType.Null => Float(0.0),
                LiteralType.String =>
                    double.TryParse((string)this.RawValue!, NumberStyles.Float, CultureInfo.InvariantCulture, out double valueAsFloat)
                        ? Float(valueAsFloat)
                        : throw new TopsyTurvyRuntimeException(this.GetInvalidCastErrorMessage(Keywords.TypeNames.Fathom)),
                _ => this
            },
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
    /// <para>
    /// With the exception of Boolean and Null types, the string representation is the default .NET string representation of the
    /// underlying value.  Boolean types render as <c>VERITY</c> for <c>true</c> or <c>NAY</c> for <c>false</c> and Null types
    /// render as <c>NAUGHT</c>.
    /// </para>
    /// In the following example various TopsyTurvy values are converted to strings:
    /// <code>
    /// TopsyTurvyValue.Integer(42).ToString();
    /// TopsyTurvyValue.Float(3.14).ToString();
    /// TopsyTurvyValue.String("Hello, World!").ToString();
    /// TopsyTurvyValue.Boolean(true).ToString();
    /// TopsyTurvyValue.Null().ToString();
    /// </code>
    /// and will be rendered as:
    /// <code>
    /// "42"
    /// "3.14"
    /// "Hello, World!"
    /// "VERITY"
    /// "NAUGHT"
    /// </code>
    /// </remarks>
    /// <returns>A string representation of this value.</returns>
    public override string ToString() => LiteralType switch
    {
        LiteralType.Boolean => (bool)this.RawValue!
            ? Keywords.Literals.Verity
            : Keywords.Literals.Nay,
        LiteralType.Null => Keywords.Literals.Naught,
        LiteralType.Array => "[" + string.Join(", ", ((List<TopsyTurvyValue>)this.RawValue!).Select(v => v.ToString())) + "]",
        _ => this.RawValue?.ToString() ?? Keywords.Literals.Naught
    };

    /// <summary>
    /// Builds an error message for an invalid cast operation.
    /// </summary>
    /// <param name="targetType">The target Topsy Turvy type.</param>
    /// <returns>An error message indicating the invalid cast.</returns>
    private string GetInvalidCastErrorMessage(string targetType) => $"Cannot cast '{this.RawValue}' to {targetType}.";
}
