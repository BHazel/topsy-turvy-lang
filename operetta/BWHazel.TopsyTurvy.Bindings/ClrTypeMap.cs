using System;
using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Bindings;

/// <summary>
/// Maps between CLR types and Topsy Turvy <see cref="LiteralType"/> values.
/// </summary>
/// <remarks>
/// <para>
/// A bound method parameters and return type are CLR types, but everything downstream, the type checker, the
/// interpreter, and hover text, needs a Topsy Turvy <see cref="LiteralType"/> instead.  This map is the one place
/// that conversion happens, so an unmapped type is caught once, when a binding is scanned, rather than failing
/// confusingly somewhere later.
/// </para>
/// </remarks>
public static class ClrTypeMap
{
    private static readonly IReadOnlyDictionary<Type, LiteralType> ClrToLiteral = new Dictionary<Type, LiteralType>()
    {
        [typeof(int)] = LiteralType.Integer,
        [typeof(long)] = LiteralType.Long,
        [typeof(short)] = LiteralType.Short,
        [typeof(sbyte)] = LiteralType.SignedByte,
        [typeof(uint)] = LiteralType.UnsignedInteger,
        [typeof(ulong)] = LiteralType.UnsignedLong,
        [typeof(ushort)] = LiteralType.UnsignedShort,
        [typeof(byte)] = LiteralType.Byte,
        [typeof(double)] = LiteralType.Double,
        [typeof(float)] = LiteralType.Single,
        [typeof(string)] = LiteralType.String,
        [typeof(char)] = LiteralType.Char,
        [typeof(bool)] = LiteralType.Boolean,
    };

    private static readonly IReadOnlyDictionary<LiteralType, Type> LiteralToClr =
        ClrToLiteral.ToDictionary(pair => pair.Value, pair => pair.Key);

    /// <summary>
    /// Attempts to map a CLR type to its Topsy Turvy <see cref="LiteralType"/>.
    /// </summary>
    /// <param name="clrType">The CLR type to map.</param>
    /// <param name="literalType">The mapped <see cref="LiteralType"/> when this method returns <c>true</c>.</param>
    /// <returns><c>true</c> if the type is mapped, otherwise <c>false</c>.</returns>
    public static bool TryGetLiteralType(Type clrType, out LiteralType literalType) =>
        ClrToLiteral.TryGetValue(clrType, out literalType);

    /// <summary>
    /// Attempts to map a Topsy Turvy <see cref="LiteralType"/> to its CLR type.
    /// </summary>
    /// <param name="literalType">The literal type to map.</param>
    /// <param name="clrType">The mapped CLR type when this method returns <c>true</c>.</param>
    /// <returns><c>true</c> if the type is mapped, otherwise <c>false</c>.</returns>
    public static bool TryGetClrType(LiteralType literalType, out Type? clrType) =>
        LiteralToClr.TryGetValue(literalType, out clrType);
}
