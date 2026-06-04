using BWHazel.TopsyTurvy.Ast;
using Superpower;
using Superpower.Parsers;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Provides atomic <see cref="TextParser{T}"/> combinators for Topsy Turvy terminal symbols.
/// </summary>
public static class Lexer
{
    /// <summary>
    /// Parses and discards one or more whitespace characters.
    /// </summary>
    public static readonly TextParser<char[]> Whitespace =
        Character.WhiteSpace.Many();

    /// <summary>
    /// Parses and discards at least one whitespace character.
    /// </summary>
    public static readonly TextParser<char[]> WhitespaceRequired =
        Character.WhiteSpace.AtLeastOnce();

    /// <summary>
    /// Parses a double-quoted string literal.
    /// </summary>
    public static readonly TextParser<string> StringLiteral =
        (from open in Character.EqualTo('"')
         from content in
             (
                 from escape in Character.EqualTo('~')
                 from code in
                     Character.EqualTo('n').Select(_ => "\n")
                         .Or(Character.EqualTo('t').Select(_ => "\t"))
                         .Or(Character.EqualTo('"').Select(_ => "\""))
                         .Or(Character.EqualTo('~').Select(_ => "~"))
                 select code
             )
             .Or(Character.Except('"').Select(c => c.ToString()))
             .Many()
         from close in Character.EqualTo('"')
         select string.Concat(content)).Named("string literal");

    /// <summary>
    /// Parses a floating-point numeric literal.
    /// </summary>
    /// <remarks>
    /// Requires a decimal point to disambiguate from integers.
    /// </remarks>
    public static readonly TextParser<double> FloatLiteral =
        (from intPart in Numerics.IntegerInt32
         from _dot in Character.EqualTo('.')
         from fracPart in Character.Digit.AtLeastOnce()
         select double.Parse(
             $"{intPart}.{new string(fracPart)}",
             System.Globalization.CultureInfo.InvariantCulture)).Try();

    /// <summary>
    /// Parses a signed integer literal.
    /// </summary>
    public static readonly TextParser<int> IntegerLiteral =
        Numerics.IntegerInt32;

    /// <summary>
    /// Parses a boolean literal.
    /// </summary>
    public static readonly TextParser<bool> BooleanLiteral =
        Span.EqualToIgnoreCase(Keywords.Literals.Verity).Try().Select(_ => true)
            .Or(Span.EqualToIgnoreCase(Keywords.Literals.Nay).Try().Select(_ => false));

    /// <summary>
    /// Parses the null literal.
    /// </summary>
    public static readonly TextParser<object?> NullLiteral =
        Span.EqualToIgnoreCase(Keywords.Literals.Naught).Try().Select(_ => (object?)null);

    /// <summary>
    /// Parses a valid Topsy Turvy identifier.
    /// </summary>
    public static readonly TextParser<string> Identifier =
        (from first in Character.Letter
         from rest in Character.Matching(
                            c => char.IsLetterOrDigit(c) || c == '-' || c == '_',
                            "letter, digit, hyphen or underscore")
                           .Many()
         select first + new string(rest))
        .Where(
            name => !name.Equals("HARK", System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals("FINALE", System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals("PRINCIPALS", System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals("BEHOLD", System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals(Keywords.Literals.Naught, System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals(Keywords.Literals.Verity, System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals(Keywords.Literals.Nay, System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals("SO", System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals("QUITE", System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals("WHEN", System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals("NOTHING", System.StringComparison.OrdinalIgnoreCase),
            "identifier (not a reserved keyword)")
        .Named("identifier");

    /// <summary>
    /// Returns a parser that matches the exact <paramref name="keyword"/> text.
    /// </summary>
    /// <param name="keyword">The keyword string to match.</param>
    /// <returns>A parser that returns the upper-case keyword on match.</returns>
    public static TextParser<string> Keyword(string keyword) =>
        Span.EqualToIgnoreCase(keyword).Try().Select(_ => keyword.ToUpperInvariant());
}
