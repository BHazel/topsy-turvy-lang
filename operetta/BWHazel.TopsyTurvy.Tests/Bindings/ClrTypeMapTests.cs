using System;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;
using Shouldly;

namespace BWHazel.TopsyTurvy.Tests.Bindings;

/// <summary>
/// Tests for the <see cref="ClrTypeMap"/> class.
/// </summary>
public class ClrTypeMapTests
{
    /// <summary>
    /// Tests that <see cref="ClrTypeMap.TryGetLiteralType"/> maps every specified CLR type to its <see cref="LiteralType"/>.
    /// </summary>
    /// <param name="clrType">The CLR type to map.</param>
    /// <param name="expected">The expected <see cref="LiteralType"/>.</param>
    [Theory]
    [InlineData(typeof(int), LiteralType.Integer)]
    [InlineData(typeof(long), LiteralType.Long)]
    [InlineData(typeof(short), LiteralType.Short)]
    [InlineData(typeof(sbyte), LiteralType.SignedByte)]
    [InlineData(typeof(uint), LiteralType.UnsignedInteger)]
    [InlineData(typeof(ulong), LiteralType.UnsignedLong)]
    [InlineData(typeof(ushort), LiteralType.UnsignedShort)]
    [InlineData(typeof(byte), LiteralType.Byte)]
    [InlineData(typeof(double), LiteralType.Double)]
    [InlineData(typeof(float), LiteralType.Single)]
    [InlineData(typeof(string), LiteralType.String)]
    [InlineData(typeof(char), LiteralType.Char)]
    [InlineData(typeof(bool), LiteralType.Boolean)]
    public void TryGetLiteralType_WithMappedClrType_ReturnsExpectedLiteralType(Type clrType, LiteralType expected)
    {
        bool found = ClrTypeMap.TryGetLiteralType(clrType, out LiteralType literalType);

        found.ShouldBeTrue();
        literalType.ShouldBe(expected);
    }

    /// <summary>
    /// Tests that <see cref="ClrTypeMap.TryGetLiteralType"/> returns <c>false</c> for an unmapped CLR type, including
    /// CLR array types, which are deliberately unmappable.
    /// </summary>
    /// <param name="clrType">The CLR type to map.</param>
    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(int[]))]
    public void TryGetLiteralType_WithUnmappedClrType_ReturnsFalse(Type clrType)
    {
        bool found = ClrTypeMap.TryGetLiteralType(clrType, out _);

        found.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that <see cref="ClrTypeMap.TryGetClrType"/> maps every specified <see cref="LiteralType"/> back to its CLR type.
    /// </summary>
    /// <param name="literalType">The literal type to map.</param>
    /// <param name="expected">The expected CLR type.</param>
    [Theory]
    [InlineData(LiteralType.Integer, typeof(int))]
    [InlineData(LiteralType.String, typeof(string))]
    [InlineData(LiteralType.Boolean, typeof(bool))]
    public void TryGetClrType_WithMappedLiteralType_ReturnsExpectedClrType(LiteralType literalType, Type expected)
    {
        bool found = ClrTypeMap.TryGetClrType(literalType, out Type? clrType);

        found.ShouldBeTrue();
        clrType.ShouldBe(expected);
    }

    /// <summary>
    /// Tests that <see cref="ClrTypeMap.TryGetClrType"/> returns <c>false</c> for a <see cref="LiteralType"/> with no CLR mapping.
    /// </summary>
    /// <param name="literalType">The literal type to map.</param>
    [Theory]
    [InlineData(LiteralType.Null)]
    [InlineData(LiteralType.Array)]
    [InlineData(LiteralType.Pointer)]
    public void TryGetClrType_WithUnmappedLiteralType_ReturnsFalse(LiteralType literalType)
    {
        bool found = ClrTypeMap.TryGetClrType(literalType, out _);

        found.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that <see cref="ClrTypeMap.TryGetArrayElementLiteralType"/> maps a one-dimensional array type to the
    /// <see cref="LiteralType"/> of its element.
    /// </summary>
    [Fact]
    public void TryGetArrayElementLiteralType_WithMappedElementType_ReturnsExpectedElementType()
    {
        bool arrayElementTypeFound = ClrTypeMap.TryGetArrayElementLiteralType(typeof(int[]), out LiteralType elementType);

        arrayElementTypeFound.ShouldBeTrue();
        elementType.ShouldBe(LiteralType.Integer);
    }

    /// <summary>
    /// Tests that <see cref="ClrTypeMap.TryGetArrayElementLiteralType"/> returns <c>false</c> for a non-array type.
    /// </summary>
    [Fact]
    public void TryGetArrayElementLiteralType_WithNonArrayType_ReturnsFalse()
    {
        bool arrayElementTypeFound = ClrTypeMap.TryGetArrayElementLiteralType(typeof(int), out _);

        arrayElementTypeFound.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that <see cref="ClrTypeMap.TryGetArrayElementLiteralType"/> returns <c>false</c> for an array whose
    /// element type has no <see cref="LiteralType"/> mapping, including a nested array (an array of arrays).
    /// </summary>
    [Theory]
    [InlineData(typeof(object[]))]
    [InlineData(typeof(int[][]))]
    public void TryGetArrayElementLiteralType_WithUnmappedElementType_ReturnsFalse(Type clrType)
    {
        bool found = ClrTypeMap.TryGetArrayElementLiteralType(clrType, out _);

        found.ShouldBeFalse();
    }
}
