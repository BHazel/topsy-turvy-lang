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

        value.LiteralType.ShouldBe(LiteralType.Integer);
        value.RawValue.ShouldBe(7);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.Double"/> method sets the correct type and value.
    /// </summary>
    [Fact]
    public void Float_WithValue_SetsCorrectTypeAndRawValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Double(2.5);

        value.LiteralType.ShouldBe(LiteralType.Double);
        value.RawValue.ShouldBe(2.5);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.String"/> method sets the correct type and value.
    /// </summary>
    [Fact]
    public void String_WithValue_SetsCorrectTypeAndRawValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("hello");

        value.LiteralType.ShouldBe(LiteralType.String);
        value.RawValue.ShouldBe("hello");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.Boolean"/> method sets the correct type and value.
    /// </summary>
    [Fact]
    public void Boolean_WithTrueValue_SetsCorrectTypeAndRawValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(true);

        value.LiteralType.ShouldBe(LiteralType.Boolean);
        value.RawValue.ShouldBe(true);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.Null"/> method sets the correct type and null raw value.
    /// </summary>
    [Fact]
    public void Null_Always_SetsNullTypeAndNullRawValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        value.LiteralType.ShouldBe(LiteralType.Null);
        value.RawValue.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns false for zero integer.
    /// </summary>
    [Fact]
    public void IsTruthy_WithZeroInteger_ReturnsFalse()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(0);

        bool result = value.IsTruthy();

        result.ShouldBeFalse();
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

        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns false for zero float.
    /// </summary>
    [Fact]
    public void IsTruthy_WithZeroFloat_ReturnsFalse()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Double(0.0);

        bool result = value.IsTruthy();

        result.ShouldBeFalse();
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
        TopsyTurvyValue value = TopsyTurvyValue.Double(rawValue);

        bool result = value.IsTruthy();

        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns false for empty string.
    /// </summary>
    [Fact]
    public void IsTruthy_WithEmptyString_ReturnsFalse()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String(string.Empty);

        bool result = value.IsTruthy();

        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns true for non-empty string.
    /// </summary>
    [Fact]
    public void IsTruthy_WithNonEmptyString_ReturnsTrue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("hello");

        bool result = value.IsTruthy();

        result.ShouldBeTrue();
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

        result.ShouldBe(rawValue);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.IsTruthy"/> method returns false for null.
    /// </summary>
    [Fact]
    public void IsTruthy_WithNull_ReturnsFalse()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        bool result = value.IsTruthy();

        result.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method returns "VERITY" for true boolean.
    /// </summary>
    [Fact]
    public void ToString_WithTrueBoolean_ReturnsVERITY()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(true);

        string result = value.ToString();

        result.ShouldBe("VERITY");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method returns "NAY" for false boolean.
    /// </summary>
    [Fact]
    public void ToString_WithFalseBoolean_ReturnsNAY()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(false);

        string result = value.ToString();

        result.ShouldBe("NAY");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method returns "NAUGHT" for null.
    /// </summary>
    [Fact]
    public void ToString_WithNull_ReturnsNAUGHT()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        string result = value.ToString();

        result.ShouldBe("NAUGHT");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method returns the decimal representation of an integer.
    /// </summary>
    [Fact]
    public void ToString_WithInteger_ReturnsDecimalString()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(42);

        string result = value.ToString();

        result.ShouldBe("42");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method delegates to the raw value <c>ToString</c> method for floats.
    /// </summary>
    [Fact]
    public void ToString_WithFloat_DelegatesRawValueToString()
    {
        double rawValue = 1.5;
        TopsyTurvyValue value = TopsyTurvyValue.Double(rawValue);

        string result = value.ToString();

        result.ShouldBe(rawValue.ToString());
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.ToString"/> method returns the raw string value unchanged.
    /// </summary>
    [Fact]
    public void ToString_WithString_ReturnsSameString()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("hello world");

        string result = value.ToString();

        result.ShouldBe("hello world");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns the same instance when the target type matches.
    /// </summary>
    [Fact]
    public void CastTo_WithSameType_ReturnsSameInstance()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(10);

        TopsyTurvyValue result = value.CastTo(LiteralType.Integer);

        result.ShouldBeSameAs(value);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method truncates a float when casting to integer.
    /// </summary>
    [Fact]
    public void CastTo_FloatToInteger_TruncatesDecimalPart()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Double(3.9);

        TopsyTurvyValue result = value.CastTo(LiteralType.Integer);

        result.LiteralType.ShouldBe(LiteralType.Integer);
        result.RawValue.ShouldBe(3);
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

        result.LiteralType.ShouldBe(LiteralType.Integer);
        result.RawValue.ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns zero when casting null to integer.
    /// </summary>
    [Fact]
    public void CastTo_NullToInteger_ReturnsZero()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        TopsyTurvyValue result = value.CastTo(LiteralType.Integer);

        result.LiteralType.ShouldBe(LiteralType.Integer);
        result.RawValue.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method parses a valid numeric string when casting to integer.
    /// </summary>
    [Fact]
    public void CastTo_ParseableStringToInteger_ParsesValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("123");

        TopsyTurvyValue result = value.CastTo(LiteralType.Integer);

        result.LiteralType.ShouldBe(LiteralType.Integer);
        result.RawValue.ShouldBe(123);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method throws when a non-numeric string is cast to integer.
    /// </summary>
    [Fact]
    public void CastTo_UnparseableStringToInteger_ThrowsRuntimeException()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("hello");

        Should.Throw<TopsyTurvyRuntimeException>(() => value.CastTo(LiteralType.Integer));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method widens an integer to a float.
    /// </summary>
    [Fact]
    public void CastTo_IntegerToFloat_WidensToDouble()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(5);

        TopsyTurvyValue result = value.CastTo(LiteralType.Double);

        result.LiteralType.ShouldBe(LiteralType.Double);
        result.RawValue.ShouldBe(5.0);
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

        TopsyTurvyValue result = value.CastTo(LiteralType.Double);

        result.LiteralType.ShouldBe(LiteralType.Double);
        result.RawValue.ShouldBe(expectedResult);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns 0.0 when casting null to float.
    /// </summary>
    [Fact]
    public void CastTo_NullToFloat_ReturnsZeroPointZero()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        TopsyTurvyValue result = value.CastTo(LiteralType.Double);

        result.LiteralType.ShouldBe(LiteralType.Double);
        result.RawValue.ShouldBe(0.0);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method parses a valid float string when casting to float.
    /// </summary>
    [Fact]
    public void CastTo_ParseableStringToFloat_ParsesValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("3.14");

        TopsyTurvyValue result = value.CastTo(LiteralType.Double);

        result.LiteralType.ShouldBe(LiteralType.Double);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method throws when a non-numeric string is cast to float.
    /// </summary>
    [Fact]
    public void CastTo_UnparseableStringToFloat_ThrowsRuntimeException()
    {
        TopsyTurvyValue value = TopsyTurvyValue.String("hello");

        Should.Throw<TopsyTurvyRuntimeException>(() => value.CastTo(LiteralType.Double));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method converts an integer to its decimal string representation.
    /// </summary>
    [Fact]
    public void CastTo_IntegerToString_ReturnsDecimalRepresentation()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(42);

        TopsyTurvyValue result = value.CastTo(LiteralType.String);

        result.LiteralType.ShouldBe(LiteralType.String);
        result.RawValue.ShouldBe("42");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method converts a true boolean to "VERITY" string.
    /// </summary>
    [Fact]
    public void CastTo_TrueBooleanToString_ReturnsVerity()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(true);

        TopsyTurvyValue result = value.CastTo(LiteralType.String);

        result.RawValue.ShouldBe("VERITY");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method converts a false boolean to "NAY" string.
    /// </summary>
    [Fact]
    public void CastTo_FalseBooleanToString_ReturnsNay()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Boolean(false);

        TopsyTurvyValue result = value.CastTo(LiteralType.String);

        result.RawValue.ShouldBe("NAY");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method converts null to "NAUGHT" string.
    /// </summary>
    [Fact]
    public void CastTo_NullToString_ReturnsNAUGHT()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Null();

        TopsyTurvyValue result = value.CastTo(LiteralType.String);

        result.RawValue.ShouldBe("NAUGHT");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns true when a truthy value is cast to boolean.
    /// </summary>
    [Fact]
    public void CastTo_TruthyIntegerToBoolean_ReturnsTrue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(5);

        TopsyTurvyValue result = value.CastTo(LiteralType.Boolean);

        result.LiteralType.ShouldBe(LiteralType.Boolean);
        result.RawValue.ShouldBe(true);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns false when zero is cast to boolean.
    /// </summary>
    [Fact]
    public void CastTo_ZeroIntegerToBoolean_ReturnsFalse()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(0);

        TopsyTurvyValue result = value.CastTo(LiteralType.Boolean);

        result.LiteralType.ShouldBe(LiteralType.Boolean);
        result.RawValue.ShouldBe(false);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyValue.CastTo"/> method returns null when any value is cast to null.
    /// </summary>
    [Fact]
    public void CastTo_IntegerToNull_ReturnsNullValue()
    {
        TopsyTurvyValue value = TopsyTurvyValue.Integer(42);

        TopsyTurvyValue result = value.CastTo(LiteralType.Null);

        result.LiteralType.ShouldBe(LiteralType.Null);
        result.RawValue.ShouldBeNull();
    }
}
