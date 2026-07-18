using System.Globalization;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using Superpower;
using Superpower.Parsers;

namespace BWHazel.TopsyTurvy.UtopIR.Parser;

/// <summary>
/// Provides atomic <see cref="TextParser{T}"/> combinators for UtopIR terminal symbols.
/// </summary>
/// <remarks>
/// <para>
/// The lexer is the lowest-level component of the parser, converting raw UtopIR source text into
/// tokens.  Statements and instructions are not handled here, only the literal level.
/// </para>
/// <para>
/// ### Whitespace
/// * <see cref="Whitespace"/>/<see cref="WhitespaceRequired"/> match zero-or-more and at-least-one
/// non-newline whitespace characters respectively, discarding what they match.  They deliberately exclude
/// <c>\r</c> and <c>\n</c>, unlike the Topsy Turvy lexer equivalents as UtopIR is line-oriented and
/// newlines are structurally significant, consumed explicitly by <see cref="InstructionParser.Newline"/>.
/// 
/// ### Variables
/// * <see cref="Variable"/> matches a <c>£</c> character followed by an <see cref="Identifier"/>,
/// returning just the identifier text with the <c>£</c> stripped.
///
/// ### Labels
/// * <see cref="Label"/> matches a <c>!</c> character followed by an <see cref="Identifier"/>,
/// returning just the identifier text with the <c>!</c> stripped.  Mirrors <see cref="Variable"/>
/// exactly, but for branch targets rather than virtual registers.
///
/// ### Literals
/// * <see cref="StringLiteral"/> and <see cref="CharacterLiteral"/> match on an individual string or
/// character respectively.  The use the same <c>~</c>-escape "Victoria Flourish" convention as Topsy
/// Turvy (<c>~n</c>, <c>~t</c>, <c>~"</c>/<c>~'</c>, <c>~~</c>).
/// * <see cref="FloatLiteral"/> returns a <see cref="double"/> and requires a decimal point to
/// disambiguate from <see cref="IntegerLiteral"/>.
/// * <see cref="IntegerLiteral"/> returns a <see cref="long"/> so literals wider than
/// <see cref="int"/> parse without wrapping. The <see cref="BoxIntegerLiteralValue(long)"/> parser
/// boxes a parsed value as an <see cref="int"/> if it fits, otherwise as a <see cref="long"/>.  This
/// follows the convention used in the Topsy Turvy lexer.
/// * <see cref="BooleanLiteral"/> matches the UtopIR lowercase <c>verity</c>/<c>nay</c> keywords and return
/// <c>true</c> and <c>false</c> respectively.
/// 
/// ### Comments
/// * <see cref="Comment"/> matches an <c>@</c> character followed by the rest of the line, not
/// including the terminating newline, which <see cref="InstructionParser"/> consumes separately.
/// 
/// ### Identifiers and Keywords
/// * <see cref="Identifier"/> matches a leading letter or underscore followed by any number of
/// letters, digits, hyphens or underscores.  The leading underscore allowance is needed because
/// <c>TopsyTurvyToUtopIRTransformer</c> auto-generated temporary register names, start with one
/// by convention.  Unlike the Topsy Turvy lexer, no reserved-word filtering is needed here since
/// UtopIR identifiers only ever appear immediately after <c>£</c>.
/// * <see cref="Keyword(string)"/> matches a keyword case-insensitively and returns its lower-case
/// canonical form.
/// </para>
/// </remarks>
public static class Lexer
{
    /// <summary>
    /// Parses and discards zero or more non-newline whitespace characters.
    /// </summary>
    /// <remarks>
    /// Deliberately excludes <c>\r</c> and <c>\n</c>, unlike the Topsy Turvy lexer's equivalent — UtopIR
    /// is line-oriented and newlines are structurally significant, consumed explicitly by
    /// <see cref="InstructionParser.Newline"/>, not silently absorbed as incidental whitespace.
    /// </remarks>
    public static readonly TextParser<char[]> Whitespace =
        Character
            .In(' ', '\t')
            .Many();

    /// <summary>
    /// Parses and discards at least one non-newline whitespace character.
    /// </summary>
    public static readonly TextParser<char[]> WhitespaceRequired =
        Character
            .In(' ', '\t')
            .AtLeastOnce();

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
             .Or(Character.Except('"').Select(character => character.ToString()))
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
    /// Requires a decimal point to disambiguate from <see cref="IntegerLiteral"/>.
    /// </remarks>
    public static readonly TextParser<double> FloatLiteral =
        (from integerPart in Numerics.IntegerInt32
         from dot in Character.EqualTo('.')
         from fractionalPart in Character.Digit.AtLeastOnce()
         select double.Parse($"{integerPart}.{new string(fractionalPart)}", CultureInfo.InvariantCulture))
            .Try();

    /// <summary>
    /// Parses a signed integer literal.
    /// </summary>
    public static readonly TextParser<long> IntegerLiteral =
        Numerics.IntegerInt64;

    /// <summary>
    /// Boxes a value parsed by <see cref="IntegerLiteral"/> as an <see cref="int"/> when it fits in
    /// the <see cref="int"/> range, otherwise as a <see cref="long"/>.
    /// </summary>
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
        Span.EqualToIgnoreCase(UtopIRKeywords.Literals.Verity)
            .Try()
            .Select(_ => true)
            .Or(Span.EqualToIgnoreCase(UtopIRKeywords.Literals.Nay)
                .Try()
                .Select(_ => false));

    /// <summary>
    /// Parses a <c>@</c>-prefixed comment, returning the comment text excluding the terminating newline.
    /// </summary>
    public static readonly TextParser<string> Comment =
        (from marker in Character.EqualTo('@')
         from content in Character.ExceptIn('\r', '\n')
            .Many()
         select new string(content))
            .Named("comment");

    /// <summary>
    /// Parses a valid UtopIR identifier.
    /// </summary>
    public static readonly TextParser<string> Identifier =
        (from firstCharacter in Character.Letter
            .Or(Character.EqualTo('_'))
         from remaining in Character.Matching(
                                character => char.IsLetterOrDigit(character) || character == '_' || character == '-',
                                "letter, digit, hyphen or underscore")
                            .Many()
         select firstCharacter + new string(remaining))
            .Named("identifier");

    /// <summary>
    /// Parses a <c>£</c>-starting variable reference, returning the identifier text without the <c>£</c> character.
    /// </summary>
    public static readonly TextParser<string> Variable =
        (from sigil in Character.EqualTo('£')
         from name in Identifier
         select name)
            .Named("variable");

    /// <summary>
    /// Parses a <c>!</c>-starting label reference, returning the identifier text without the <c>!</c> character.
    /// </summary>
    public static readonly TextParser<string> Label =
        (from sigil in Character.EqualTo('!')
         from name in Identifier
         select name)
            .Named("label");

    /// <summary>
    /// Returns a parser that matches the exact <paramref name="keyword"/> text case-insensitively.
    /// </summary>
    /// <param name="keyword">The keyword string to match.</param>
    /// <returns>A parser that returns the canonical lowercase keyword on match.</returns>
    public static TextParser<string> Keyword(string keyword) =>
        Span.EqualToIgnoreCase(keyword)
            .Try()
            .Select(_ => keyword.ToLowerInvariant());
}
