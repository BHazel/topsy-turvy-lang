using System.Globalization;
using BWHazel.TopsyTurvy.Ast;
using Superpower;
using Superpower.Parsers;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Provides atomic <see cref="TextParser{T}"/> combinators for Topsy Turvy terminal symbols.
/// </summary>
/// <remarks>
/// <para>
/// The lexer is the lowest-level component of the parser which converts the raw text of the source code into tokens, which can be
/// described as the "words" of the language.  Statements and expressions are not handled here: this is purely at the literal level
/// and answers the question "what does this one word look like?".  The lexer itself is made up of parsers for each type of literal,
/// all of which are built up by smaller building blocks called combinators.  Most parsers work on individual characters at a time
/// to build the token, moving a cursor through the source text as they consume each character.  However, some parsers match on a
/// complete word (span) in a single step.  Some parsers are designed to back-track if they fail, moving the cursor back to the start
/// of the attempted parse so that other parsers can try to match the same text.  The supported parsers in the lexer are outlined
/// below.
/// </para>
/// <para>
/// ### Whitespace
/// 2 parsers are included for whitespace, both returning a <c>char[]</c> of the whitespace characters.
/// * <c>Whitespace</c> matches on any length of whitespace.
/// * <c>WhitespaceRequired</c> matches on at least one whitespace character, and is used to separate tokens where necessary.
/// 
/// It should be noted the resulting array is always discarded by higher-level parsers and is only run to enforce spacing rules
/// between tokens.
/// </para>
/// <para>
/// ### String Literals
/// The <c>StringLiteral</c> parser matches on a double-quoted string, returning the content with escape sequences resolved as a <c>string</c>.
/// * It first matches an initial single <c>"</c> character.
/// * It then matches the content of the string, which can be either:
///     * An escape sequence, which starts with a <c>~</c> character followed by a single supported escape character code.
///     * Any character except a double quote, which is treated as a literal character.
/// * Finally, it matches a closing <c>"</c> character and returns the concatenated content of the string.
/// </para>
/// <para>
/// ### Numeric Literals
/// Two parsers are included for numeric literals, which are distinguished by the presence of a decimal point.
/// #### Floating-Point Literals
/// The <c>FloatLiteral</c> parser matches on a floating-point number, returning it as a <c>double</c>.
/// * It first matches an integer part using the built-in <c>IntegerInt32</c> parser.
/// * It then matches a decimal point character <c>.</c>.
/// * Finally, it matches the fractional part as a sequence of digits and combines it with the integer part to produce the final value.
/// 
/// This parser supports back-tracking on failure.
/// #### Integer Literals
/// The <c>IntegerLiteral</c> parser matches on a signed integer, returning it as a <c>long</c> so
/// literals wider than <c>Int32</c> parse without wrapping.  Callers decide the final <c>LiteralType</c>
/// (<c>Integer</c> vs <c>Long</c>) from the parsed magnitude.
/// * It matches on any signed integer.
///
/// The <c>BoxIntegerLiteralValue</c> helper turns the raw <c>long</c> into the boxed value callers
/// actually store: a boxed <c>int</c> if it fits, otherwise a boxed <c>long</c>.  Every caller of
/// <c>IntegerLiteral</c> that needs a CLR-typed value (rather than the raw <c>long</c>) goes through this
/// helper, so an integer literal boxed runtime type is resolved identically everywhere.
/// </para>
/// <para>
/// ### Boolean Literals
/// The <c>BooleanLiteral</c> parser matches on the keywords for <c>true</c> and <c>false</c>, returning the corresponding <c>bool</c> value.
/// * It first matches on the <c>VERITY</c> keyword (case-insensitive) and, if matched, returns <c>true</c>.
/// * If that does not match, it then matches on the <c>NAY</c> keyword (case-insensitive) and, if matched, returns <c>false</c>.
/// 
/// This parser supports back-tracking on failure.
/// </para>
/// <para>
/// ### Null Literal
/// The <c>NullLiteral</c> parser matches on the keyword for <c>null</c>, returning a <c>null</c> value.
/// * It matches on the <c>NAUGHT</c> keyword (case-insensitive) and, if matched, returns <c>null</c>.
/// 
/// This parser supports back-tracking on failure.
/// </para>
/// <para>
/// ### Identifiers
/// The <c>Identifier</c> parser matches on any valid Topsy Turvy identifiers for variables, functions and parameters, returning the identifier name as a <c>string</c>.
/// * It first matches a leading character which must be a letter to match.
/// * It then matches zero or more subsequent characters which can be letters, digits, hyphens or underscores.
/// * Finally, it checks that the resulting identifier is not one of a small set of reserved keywords that would cause parsing ambiguity.
///
/// Full reserved word enforcement for declared names is applied after the parsing process.
/// </para>
/// <para>
/// ### Character Literals
/// The <c>CharacterLiteral</c> parser matches on a single-quoted character literal, returning the content as a <c>char</c>.
/// * It first matches an opening <c>'</c> character.
/// * It then matches exactly one character, which can be either:
///     * An escape sequence, which starts with a <c>~</c> character followed by a supported escape code
///       (<c>~n</c>, <c>~t</c>, <c>~'</c>, <c>~~</c>).
///     * Any character except a single quote or tilde.
/// * Finally, it matches a closing <c>'</c> character and returns the single resolved character.
///
/// This parser supports back-tracking on failure.
/// </para>
/// <para>
/// ### Keywords
/// The <c>Keyword</c> parser is a helper function that generates a parser for a specific keyword string, returning the upper-case version of the keyword on a successful match as a <c>string</c>.
/// * It matches on the exact keyword text, ignoring case, converting the result to upper-case.
///
/// This parser supports back-tracking on failure.
/// </para>
/// </remarks>
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
        (from openingQuote in Character.EqualTo('"')
         from stringContent in
             (
                 from escapeCharacter in Character.EqualTo('~')
                 from escapedCode in
                     Character.EqualTo('n').Select(_ => "\n")
                         .Or(Character.EqualTo('t').Select(_ => "\t"))
                         .Or(Character.EqualTo('"').Select(_ => "\""))
                         .Or(Character.EqualTo('~').Select(_ => "~"))
                 select escapedCode
             )
             .Or(Character.Except('"').Select(c => c.ToString()))
             .Many()
         from closingQuote in Character.EqualTo('"')
         select string.Concat(stringContent)).Named("string literal");

    /// <summary>
    /// Parses a single-quoted character literal and returns the resolved <see cref="char"/>.
    /// </summary>
    public static readonly TextParser<char> CharacterLiteral =
        (from openingQuote in Character.EqualTo('\'')
         from content in
             (from escapeCharacter in Character.EqualTo('~')
              from escapedCode in
                  Character.EqualTo('n').Select(_ => '\n')
                      .Or(Character.EqualTo('t').Select(_ => '\t'))
                      .Or(Character.EqualTo('\'').Select(_ => '\''))
                      .Or(Character.EqualTo('~').Select(_ => '~'))
              select escapedCode)
             .Or(Character.Except('\''))
         from closingQuote in Character.EqualTo('\'')
         select content)
            .Try().Named("character literal");

    /// <summary>
    /// Parses a floating-point numeric literal.
    /// </summary>
    /// <remarks>
    /// Requires a decimal point to disambiguate from integers.
    /// </remarks>
    public static readonly TextParser<double> FloatLiteral =
        (from integerPart in Numerics.IntegerInt32
         from dot in Character.EqualTo('.')
         from fractionalPart in Character.Digit.AtLeastOnce()
         select double.Parse(
             $"{integerPart}.{new string(fractionalPart)}",
             CultureInfo.InvariantCulture)).Try();

    /// <summary>
    /// Parses a signed integer literal.
    /// </summary>
    public static readonly TextParser<long> IntegerLiteral =
        Numerics.IntegerInt64;

    /// <summary>
    /// Boxes a value parsed by <see cref="IntegerLiteral"/> as an <see cref="int"/> when it fits in
    /// the <see cref="int"/> range, otherwise as a <see cref="long"/>.
    /// </summary>
    /// <remarks>
    /// Shared by every caller of <see cref="IntegerLiteral"/> that needs a CLR-typed literal value as
    /// opposed to the raw parsed <see cref="long"/>.  It keeps the same magnitude-based
    /// <c>Integer</c>-vs-<c>Long</c> resolution consistent wherever an integer literal boxed runtime
    /// type matters, e.g. for <c>object.Equals</c> comparisons against other boxed integer literals.
    /// </remarks>
    /// <param name="value">The raw value parsed by <see cref="IntegerLiteral"/>.</param>
    /// <returns>A boxed <see cref="int"/> if <paramref name="value"/> fits, otherwise a boxed <see cref="long"/>.</returns>
    public static object BoxIntegerLiteralValue(long value) =>
        value is >= int.MinValue and <= int.MaxValue
            ? (object)(int)value
            : value;

    /// <summary>
    /// Parses a boolean literal.
    /// </summary>
    public static readonly TextParser<bool> BooleanLiteral =
        Span.EqualToIgnoreCase(Keywords.Literals.Verity)
            .Try()
            .Select(_ => true)
            .Or(Span.EqualToIgnoreCase(Keywords.Literals.Nay)
                .Try()
                .Select(_ => false));

    /// <summary>
    /// Parses the null literal.
    /// </summary>
    public static readonly TextParser<object?> NullLiteral =
        Span.EqualToIgnoreCase(Keywords.Literals.Naught)
            .Try()
            .Select(_ => (object?)null);

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
                  && !name.Equals("NOTHING", System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals("VICTIM", System.StringComparison.OrdinalIgnoreCase)
                  && !name.Equals("ON", System.StringComparison.OrdinalIgnoreCase),
            "identifier (not a reserved keyword)")
        .Named("identifier");

    /// <summary>
    /// Returns a parser that matches the exact <paramref name="keyword"/> text.
    /// </summary>
    /// <param name="keyword">The keyword string to match.</param>
    /// <returns>A parser that returns the upper-case keyword on match.</returns>
    public static TextParser<string> Keyword(string keyword) =>
        Span.EqualToIgnoreCase(keyword)
            .Try()
            .Select(_ => keyword.ToUpperInvariant());
}
