using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Tests for the <see cref="TopsyTurvyValue"/> class.
/// </summary>
public class TopsyTurvyValueTests
{
    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.Integer"/> method sets the correct type and value.
    /// </summary>
    [Fact]
    public void Integer_WithValue_SetsCorrectTypeAndRawValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(7);

        Assert.Equal(LiteralType.Integer, value.TopsyTurvyType);
        Assert.Equal(7, value.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.Float"/> method sets the correct type and value.
    /// </summary>
    [Fact]
    public void Float_WithValue_SetsCorrectTypeAndRawValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Float(2.5);

        Assert.Equal(LiteralType.Float, value.TopsyTurvyType);
        Assert.Equal(2.5, value.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.String"/> method sets the correct type and value.
    /// </summary>
    [Fact]
    public void String_WithValue_SetsCorrectTypeAndRawValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("hello");

        Assert.Equal(LiteralType.String, value.TopsyTurvyType);
        Assert.Equal("hello", value.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.Boolean"/> method sets the correct type and value.
    /// </summary>
    [Fact]
    public void Boolean_WithTrueValue_SetsCorrectTypeAndRawValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(true);

        Assert.Equal(LiteralType.Boolean, value.TopsyTurvyType);
        Assert.Equal(true, value.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.Null"/> method sets the correct type and null raw value.
    /// </summary>
    [Fact]
    public void Null_Always_SetsNullTypeAndNullRawValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        Assert.Equal(LiteralType.Null, value.TopsyTurvyType);
        Assert.Null(value.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns false for zero integer.
    /// </summary>
    [Fact]
    public void IsTruthy_WithZeroInteger_ReturnsFalse()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(0);

        bool result = value.IsTruthy();

        Assert.False(result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns true for non-zero integer.
    /// </summary>
    /// <param name="rawValue">The integer value to test.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    public void IsTruthy_WithNonZeroInteger_ReturnsTrue(int rawValue)
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(rawValue);

        bool result = value.IsTruthy();

        Assert.True(result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns false for zero float.
    /// </summary>
    [Fact]
    public void IsTruthy_WithZeroFloat_ReturnsFalse()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Float(0.0);

        bool result = value.IsTruthy();

        Assert.False(result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns true for non-zero float.
    /// </summary>
    /// <param name="rawValue">The float value to test.</param>
    [Theory]
    [InlineData(0.1)]
    [InlineData(-1.5)]
    public void IsTruthy_WithNonZeroFloat_ReturnsTrue(double rawValue)
    {
        TopsyTurvyValue value = TopsyTurvyValue.Float(rawValue);

        bool result = value.IsTruthy();

        Assert.True(result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns false for empty string.
    /// </summary>
    [Fact]
    public void IsTruthy_WithEmptyString_ReturnsFalse()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String(string.Empty);

        bool result = value.IsTruthy();

        Assert.False(result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns true for non-empty string.
    /// </summary>
    [Fact]
    public void IsTruthy_WithNonEmptyString_ReturnsTrue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("hello");

        bool result = value.IsTruthy();

        Assert.True(result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns the raw boolean value.
    /// </summary>
    /// <param name="rawValue">The boolean value to test.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsTruthy_WithBoolean_ReturnsSameValue(bool rawValue)
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(rawValue);

        bool result = value.IsTruthy();

        Assert.Equal(rawValue, result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns false for null.
    /// </summary>
    [Fact]
    public void IsTruthy_WithNull_ReturnsFalse()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        bool result = value.IsTruthy();

        Assert.False(result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method returns "VERITY" for true boolean.
    /// </summary>
    [Fact]
    public void ToString_WithTrueBoolean_ReturnsVERITY()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(true);

        string result = value.ToString();

        Assert.Equal("VERITY", result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method returns "NAY" for false boolean.
    /// </summary>
    [Fact]
    public void ToString_WithFalseBoolean_ReturnsNAY()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(false);

        string result = value.ToString();

        Assert.Equal("NAY", result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method returns "NAUGHT" for null.
    /// </summary>
    [Fact]
    public void ToString_WithNull_ReturnsNAUGHT()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        string result = value.ToString();

        Assert.Equal("NAUGHT", result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method returns the decimal representation of an integer.
    /// </summary>
    [Fact]
    public void ToString_WithInteger_ReturnsDecimalString()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(42);

        string result = value.ToString();

        Assert.Equal("42", result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method delegates to the raw value <c>ToString</c> method for floats.
    /// </summary>
    [Fact]
    public void ToString_WithFloat_DelegatesRawValueToString()
    {
        double rawValue = 1.5;
        TopsyTurvyValue value = TopsyTurvyValue.Float(rawValue);

        string result = value.ToString();

        Assert.Equal(rawValue.ToString(), result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method returns the raw string value unchanged.
    /// </summary>
    [Fact]
    public void ToString_WithString_ReturnsSameString()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("hello world");

        string result = value.ToString();

        Assert.Equal("hello world", result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns the same instance when the target type matches.
    /// </summary>
    [Fact]
    public void CastTo_WithSameType_ReturnsSameInstance()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(10);

        TopsyTurvyValue result = value.CastTo(LiteralType.Integer);

        Assert.Same(value, result);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method truncates a float when casting to integer.
    /// </summary>
    [Fact]
    public void CastTo_FloatToInteger_TruncatesDecimalPart()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Float(3.9);

        TopsyTurvyValue result = value.CastTo(LiteralType.Integer);

        Assert.Equal(LiteralType.Integer, result.TopsyTurvyType);
        Assert.Equal(3, result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method converts a boolean to 1 or 0 when casting to integer.
    /// </summary>
    /// <param name="rawValue">The boolean value to cast.</param>
    /// <param name="expectedResult">The expected integer result.</param>
    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void CastTo_BooleanToInteger_ReturnsOneOrZero(bool rawValue, int expectedResult)
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(rawValue);

        TopsyTurvyValue result = value.CastTo(LiteralType.Integer);

        Assert.Equal(LiteralType.Integer, result.TopsyTurvyType);
        Assert.Equal(expectedResult, result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns zero when casting null to integer.
    /// </summary>
    [Fact]
    public void CastTo_NullToInteger_ReturnsZero()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        TopsyTurvyValue result = value.CastTo(LiteralType.Integer);

        Assert.Equal(LiteralType.Integer, result.TopsyTurvyType);
        Assert.Equal(0, result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method parses a valid numeric string when casting to integer.
    /// </summary>
    [Fact]
    public void CastTo_ParseableStringToInteger_ParsesValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("123");

        TopsyTurvyValue result = value.CastTo(LiteralType.Integer);

        Assert.Equal(LiteralType.Integer, result.TopsyTurvyType);
        Assert.Equal(123, result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method throws when a non-numeric string is cast to integer.
    /// </summary>
    [Fact]
    public void CastTo_UnparseableStringToInteger_ThrowsRuntimeException()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("hello");

        Assert.Throws<TopsyTurvyRuntimeException>(() => value.CastTo(LiteralType.Integer));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method widens an integer to a float.
    /// </summary>
    [Fact]
    public void CastTo_IntegerToFloat_WidensToDouble()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(5);

        TopsyTurvyValue result = value.CastTo(LiteralType.Float);

        Assert.Equal(LiteralType.Float, result.TopsyTurvyType);
        Assert.Equal(5.0, result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method converts a boolean to 1.0 or 0.0 when casting to float.
    /// </summary>
    /// <param name="rawValue">The boolean value to cast.</param>
    /// <param name="expectedResult">The expected float result.</param>
    [Theory]
    [InlineData(true, 1.0)]
    [InlineData(false, 0.0)]
    public void CastTo_BooleanToFloat_ReturnsOneOrZeroDouble(bool rawValue, double expectedResult)
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(rawValue);

        TopsyTurvyValue result = value.CastTo(LiteralType.Float);

        Assert.Equal(LiteralType.Float, result.TopsyTurvyType);
        Assert.Equal(expectedResult, result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns 0.0 when casting null to float.
    /// </summary>
    [Fact]
    public void CastTo_NullToFloat_ReturnsZeroPointZero()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        TopsyTurvyValue result = value.CastTo(LiteralType.Float);

        Assert.Equal(LiteralType.Float, result.TopsyTurvyType);
        Assert.Equal(0.0, result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method parses a valid float string when casting to float.
    /// </summary>
    [Fact]
    public void CastTo_ParseableStringToFloat_ParsesValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("3.14");

        TopsyTurvyValue result = value.CastTo(LiteralType.Float);

        Assert.Equal(LiteralType.Float, result.TopsyTurvyType);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method throws when a non-numeric string is cast to float.
    /// </summary>
    [Fact]
    public void CastTo_UnparseableStringToFloat_ThrowsRuntimeException()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("hello");

        Assert.Throws<TopsyTurvyRuntimeException>(() => value.CastTo(LiteralType.Float));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method converts an integer to its decimal string representation.
    /// </summary>
    [Fact]
    public void CastTo_IntegerToString_ReturnsDecimalRepresentation()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(42);

        TopsyTurvyValue result = value.CastTo(LiteralType.String);

        Assert.Equal(LiteralType.String, result.TopsyTurvyType);
        Assert.Equal("42", result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method converts a true boolean to "VERITY" string.
    /// </summary>
    [Fact]
    public void CastTo_TrueBooleanToString_ReturnsVerity()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(true);

        TopsyTurvyValue result = value.CastTo(LiteralType.String);

        Assert.Equal("VERITY", result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method converts a false boolean to "NAY" string.
    /// </summary>
    [Fact]
    public void CastTo_FalseBooleanToString_ReturnsNay()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(false);

        TopsyTurvyValue result = value.CastTo(LiteralType.String);

        Assert.Equal("NAY", result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method converts null to "NAUGHT" string.
    /// </summary>
    [Fact]
    public void CastTo_NullToString_ReturnsNAUGHT()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        TopsyTurvyValue result = value.CastTo(LiteralType.String);

        Assert.Equal("NAUGHT", result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns true when a truthy value is cast to boolean.
    /// </summary>
    [Fact]
    public void CastTo_TruthyIntegerToBoolean_ReturnsTrue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(5);

        TopsyTurvyValue result = value.CastTo(LiteralType.Boolean);

        Assert.Equal(LiteralType.Boolean, result.TopsyTurvyType);
        Assert.Equal(true, result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns false when zero is cast to boolean.
    /// </summary>
    [Fact]
    public void CastTo_ZeroIntegerToBoolean_ReturnsFalse()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(0);

        TopsyTurvyValue result = value.CastTo(LiteralType.Boolean);

        Assert.Equal(LiteralType.Boolean, result.TopsyTurvyType);
        Assert.Equal(false, result.RawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns null when any value is cast to null.
    /// </summary>
    [Fact]
    public void CastTo_IntegerToNull_ReturnsNullValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(42);

        TopsyTurvyValue result = value.CastTo(LiteralType.Null);

        Assert.Equal(LiteralType.Null, result.TopsyTurvyType);
        Assert.Null(result.RawValue);
    }
}
