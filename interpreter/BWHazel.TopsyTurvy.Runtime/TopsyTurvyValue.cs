using System.Globalization;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Represents a runtime value in the Topsy Turvy interpreter.
/// </summary>
/// <remarks>
/// Wraps a .NET primitive together with a <see cref="LiteralType"/> tag as the language is
/// dynamically typed at runtime.
/// </remarks>
public sealed class TopsyTurvyValue
{
    /// <summary>
    /// Initialises a new instance of the <see cref="TopsyTurvyValue"/> class with the specified raw value and type.
    /// </summary>
    /// <param name="raw">The underlying .NET value.</param>
    /// <param name="type">The Topsy Turvy type of the value.</param>
    private TopsyTurvyValue(object? raw, LiteralType type)
    {
        this.RawValue = raw;
        this.TopsyTurvyType = type;
    }

    /// <summary>
    /// Gets the underlying .NET value.
    /// </summary>
    public object? RawValue { get; }

    /// <summary>
    /// Gets the Topsy Turvy type of this value.
    /// </summary>
    public LiteralType TopsyTurvyType { get; }

    /// <summary>
    /// Creates an integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the integer value.</returns>
    public static TopsyTurvyValue Integer(int value) => new(value, LiteralType.Integer);

    /// <summary>
    /// Creates a floating-point value.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the floating-point value.</returns>
    public static TopsyTurvyValue Float(double value) => new(value, LiteralType.Float);

    /// <summary>
    /// Creates a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the string.</returns>
    public static TopsyTurvyValue String(string value) => new(value, LiteralType.String);

    /// <summary>
    /// Creates a boolean value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing the boolean.</returns>
    public static TopsyTurvyValue Boolean(bool value) => new(value, LiteralType.Boolean);

    /// <summary>
    /// Creates a null value.
    /// </summary>
    /// <returns>A new <see cref="TopsyTurvyValue"/> representing null.</returns>
    public static TopsyTurvyValue Null() => new(null, LiteralType.Null);

    /// <summary>
    /// Evaluates the truthiness of this value in a boolean context.
    /// </summary>
    /// <returns><c>true</c> for "truthy" values, otherwise <c>false</c>.</returns>
    public bool IsTruthy() => TopsyTurvyType switch
    {
        LiteralType.Integer => (int)RawValue! != 0,
        LiteralType.Float => (double)RawValue! != 0.0,
        LiteralType.String => !string.IsNullOrEmpty((string)RawValue!),
        LiteralType.Boolean => (bool)RawValue!,
        LiteralType.Null => false,
        _ => true
    };

    /// <summary>
    /// Returns a new value coerced to the specified target type.
    /// </summary>
    /// <param name="target">The target Topsy Turvy type.</param>
    /// <returns>A new <see cref="TopsyTurvyValue"/> of the target type.</returns>
    /// <exception cref="TopsyTurvyRuntimeException">Thrown when the value cannot be coerced.</exception>
    public TopsyTurvyValue CastTo(LiteralType target)
    {
        if (TopsyTurvyType == target)
        {
            return this;
        }

        return target switch
        {
            LiteralType.Integer => TopsyTurvyType switch
            {
                LiteralType.Float => Integer((int)(double)RawValue!),
                LiteralType.Boolean => Integer((bool)RawValue! ? 1 : 0),
                LiteralType.Null => Integer(0),
                LiteralType.String =>
                    int.TryParse((string)RawValue!, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)
                        ? Integer(i)
                        : throw new TopsyTurvyRuntimeException($"Cannot cast '{RawValue}' to PEER."),
                _ => this
            },
            LiteralType.Float => TopsyTurvyType switch
            {
                LiteralType.Integer => Float((double)(int)RawValue!),
                LiteralType.Boolean => Float((bool)RawValue! ? 1.0 : 0.0),
                LiteralType.Null => Float(0.0),
                LiteralType.String =>
                    double.TryParse((string)RawValue!, NumberStyles.Float, CultureInfo.InvariantCulture, out double f)
                        ? Float(f)
                        : throw new TopsyTurvyRuntimeException($"Cannot cast '{RawValue}' to FATHOM."),
                _ => this
            },
            LiteralType.String => String(ToString()),
            LiteralType.Boolean => Boolean(IsTruthy()),
            LiteralType.Null => Null(),
            _ => this
        };
    }

    /// <summary>
    /// Returns the string representation of this value.
    /// </summary>
    /// <remarks>
    /// Booleans render as <c>VERITY</c> or <c>NAY</c>; null renders as <c>NAUGHT</c>.
    /// all other types use their default .NET string representation.
    /// </remarks>
    /// <returns>A string representation of this value.</returns>
    public override string ToString() => TopsyTurvyType switch
    {
        LiteralType.Boolean => (bool)RawValue! ? "VERITY" : "NAY",
        LiteralType.Null => "NAUGHT",
        _ => RawValue?.ToString() ?? "NAUGHT"
    };
}
