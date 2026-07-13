using System;
using System.Collections.Generic;
using System.Linq;
using Superpower;
using BWHazel.TopsyTurvy.Ast;
using static BWHazel.TopsyTurvy.Parser.ParserHelpers;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Implements the recursive <see cref="TextParser{T}"/> combinators for Topsy Turvy expressions.
/// </summary>
/// <remarks>
/// <para>
/// The expression parser is the second layer of the parser which builds on top of the lexer to parse expressions and operators and
/// create associated <see cref="BWHazel.TopsyTurvy.Ast.Expression"/> and <see cref="Operator"/> AST components.  Statements are not handled here.  As in the
/// Lexer, the expression parser is itself made up of parsers for each type of expression all of which are built up by smaller building
/// blocks called combinators.  Expression parsers are recursive and can therefore run themselves during the parsing of an expression.
/// The supported parsers for expressions are outlined below.
/// </para>
/// <para>
/// Please note that for brevity the capture of spans is not documented for each parser.  Please see the **Source Spans** section below for a description of
/// how spans are captured and associated with AST nodes.
/// </para>
/// <para>
/// ### Type Keywords
/// The <c>TypeKeyword</c> parser matches on type keywords, returning the corresponding <see cref="LiteralType"/> value.
/// For example, <c>PEER</c> returns <see cref="LiteralType"/><c>.Integer</c> and <c>STANDING PEER</c> returns
/// <see cref="LiteralType"/><c>.UnsignedInteger</c>.  This parser lives in <see cref="ExpressionParser"/>
/// (rather than <see cref="StatementParser"/>) because it is also needed by <see cref="ExpressionCast"/> at the
/// expression layer; placing it here avoids a circular static-field initialisation dependency.
/// </para>
/// <para>
/// The <c>STANDING</c> modifier is tried first as a two-token sequence (<c>STANDING &lt;integer-type&gt;</c>).  If the token following <c>STANDING</c> is not an integer type keyword, the combinator backtracks
/// and the match fails as <c>STANDING</c> on its own is not a valid type keyword.
/// </para>
/// <para>
/// ### Operators
/// The <c>OperatorToken</c> parser matches on any of the operator keywords and returns the corresponding <see cref="Operator"/> value.
/// * It uses the <c>Keyword</c> parser from the lexer to match on each operator keyword.
///
/// This parser supports back-tracking on failure.
/// </para>
/// <para>
/// ### Literal Expressions
/// The <c>LiteralExpression</c> parser matches on a literal, using parsers from the Lexer, and returns a <see cref="LiteralNode"/>
/// with the appropriate type and value as an expression.
/// </para>
/// <para>
/// For an integer literal, <see cref="Lexer.IntegerLiteral"/> returns the parsed value as a <c>long</c>
/// so magnitude is never lost and this parser then resolves the literal final <see cref="LiteralType"/>,
/// <c>Integer</c> or <c>Long</c>, from that magnitude via <see cref="Lexer.BoxIntegerLiteralValue(long)"/>,
/// which boxes the value as an <c>int</c> when it fits or as a <c>long</c> otherwise.  This is why a literal
/// like <c>200</c> is typed <c>Integer</c> but <c>5000000000</c> (too large for <c>Int32</c>) is typed
/// <c>Long</c> with no explicit type suffix required in Topsy Turvy source.
/// </para>
/// <para>
/// ### Identifier Expressions
/// 3 parsers are included for identifiers, all returning an <see cref="BWHazel.TopsyTurvy.Ast.Expression"/> which is an <see cref="IdentifierNode"/>.
/// * <c>JustSoExpression</c> matches on the implicit variable keyword "JUST SO", returning an <see cref="IdentifierNode"/> with the name "JUST SO".
/// * <c>ThePropsExpression</c> matches on the built-in arguments array "THE PROPS", returning an <see cref="IdentifierNode"/> with the name "THE PROPS".
/// * <c>IdentifierExpression</c> matches on any other identifier, returning an <see cref="IdentifierNode"/> with the corresponding name.
/// </para>
/// <para>
/// ### Operator Expressions
/// **These should not be confused with the <c>OperatorToken</c> parser which only matches on operator keywords and returns an <see cref="Operator"/> value.**
/// </para>
/// <para>
/// #### Single <c>AND</c> Expressions
/// The <c>SingleAndExpression</c> parser, which is a helper parser not intended to be used directly and documented here for
/// completeness, matches on a single "AND" keyword
/// followed by an expression, returning the <see cref="BWHazel.TopsyTurvy.Ast.Expression"/>:
/// * It first matches on required whitespace, which is then discarded.
/// * It then matches on the "AND" keyword, which is also discarded.
/// * It then matches on more required whitespace, which is discarded.
/// * Finally, it matches on an expression and returns it.
/// 
/// This parser supports back-tracking on failure.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// SUM OF 1 AND 2
/// SUM OF 3 AND PRODUCT OF 4 AND 5
/// </code>
/// the <c> AND 2</c> and <c> AND PRODUCT OF 4 AND 5</c> would be matched respectively by the <c>SingleAndExpression</c> parser,
/// returning the <see cref="LiteralNode"/> <c>2</c> and, after recursive parsing, the <see cref="PrefixExpressionNode"/>
/// <c>PRODUCT OF 4 AND 5</c> respectively.
/// </para>
/// <para>
/// #### Single <c>OR</c> Expressions
/// The <c>SingleOrExpression</c> parser, which is a helper parser not intended to be used directly and documented here for
/// completeness, matches on a single "OR" keyword
/// followed by an expression, returning the <see cref="BWHazel.TopsyTurvy.Ast.Expression"/>:
/// * It first matches on required whitespace, which is then discarded.
/// * It then matches on the "OR" keyword, which is also discarded.
/// * It then matches on more required whitespace, which is discarded.
/// * Finally, it matches on an expression and returns it.
///
/// This parser supports back-tracking on failure.
/// </para>
/// <para>
/// In the following Topsy Turvy example:
/// <code>
/// EITHER VERITY OR NAY
/// </code>
/// the <c> OR NAY</c> would be matched by the <c>SingleOrExpression</c> parser, returning the <see cref="LiteralNode"/> <c>NAY</c>.
/// </para>
/// <para>
/// #### Single <c>BY</c> Expressions
/// The <c>SingleByExpression</c> parser, which is a helper parser not intended to be used directly and documented here for
/// completeness, matches on a single "BY" keyword
/// followed by an expression, returning the <see cref="BWHazel.TopsyTurvy.Ast.Expression"/>:
/// * It first matches on required whitespace, which is then discarded.
/// * It then matches on the "BY" keyword, which is also discarded.
/// * It then matches on more required whitespace, which is discarded.
/// * Finally, it matches on an expression and returns it.
/// </para>
/// <para>
/// In the following Topsy Turvy example:
/// <code>
/// TRANSPOSITION UP x BY 3
/// </code>
/// the <c> BY 3</c> would be matched by the <c>SingleByExpression</c> parser, returning the <see cref="LiteralNode"/> <c>3</c>.
/// When <c>BY</c> is omitted, the resulting <see cref="PrefixExpressionNode.Arguments"/> holds a single entry and
/// downstream layers default the shift amount to 1.
/// </para>
/// <para>
/// #### Variadic <c>AND</c> Expressions
/// The <c>AndExpression</c> parser, also a helper parser not intended to be used directly and documented here for completeness,
///  matches on a sequence of zero or more <c>SingleAndExpression</c>, returning an <see cref="BWHazel.TopsyTurvy.Ast.Expression"/><c>[]</c> of the matched
/// expressions and an empty array if none are matched.  It should be noted that the first expression is already consumed by the
/// calling parser, so this parser only matches on the subsequent <c>AND</c> expressions.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// WOVEN OF "Hello" AND " " AND "World" IF YOU PLEASE.
/// ALL OF VERITY AND HARDLY EVER NAY AND PRE-ADAMITE 10 AND 5 IF YOU PLEASE.
/// </code>
/// * The first example would return an array of 2 <see cref="LiteralNode"/>s for the expressions <c>" "</c> and <c>"World"</c>.
/// * The second example would return an array of 2 expressions, a <see cref="PrefixExpressionNode"/> for <c>HARDLY EVER NAY</c> and another <see cref="PrefixExpressionNode"/> for <c>PRE-ADAMITE 10 AND 5</c>.
/// </para>
/// <para>
/// #### Prefix Expressions (Complete Operator Parsing)
/// The <c>PrefixExpression</c> parser matches on a complete operator expression in prefix notation (<c>OPERATOR OPERAND1 AND OPERAND2 [AND OPERAND3 ... IF YOU PLEASE.]</c>), returning a
/// <see cref="PrefixExpressionNode"/> with the appropriate operator and list of argument expressions:
/// * It first matches on the operator using the <c>OperatorToken</c> parser.
///     * If none matches, the parser fails and back-tracks.
/// * It then matches on required whitespace, which is discarded.
/// * It then matches on the first operand <see cref="BWHazel.TopsyTurvy.Ast.Expression"/>, which is required and returned as the first argument in the resulting <see cref="PrefixExpressionNode"/>.
///     * Recursive parsing may occur here if the operand is itself an operator expression.
/// * It then matches the remaining operands, first checking if the operator is variadic (<c>ALL OF</c>, <c>ANY OF</c>, <c>WOVEN OF</c>), thus accepting more than 2 operands.
///     * If variadic, parses zero or more <c>AND</c> expressions using the <c>AndExpression</c> parser, returning an array of the matched <see cref="BWHazel.TopsyTurvy.Ast.Expression"/>s or an empty array if none are matched.
///     * Otherwise, if the operator is <see cref="Operator.Either"/>, it parses a single <c>OR</c> expression using the <c>SingleOrExpression</c> parser; this is one exception where the separator is <c>OR</c> instead of <c>AND</c>.
///     * Alternatively, if the operator is <see cref="Operator.TranspositionUp"/> or <see cref="Operator.TranspositionDown"/>, it parses a single <c>BY</c> expression using the <c>SingleByExpression</c> parser; this is the other exception, where the separator is <c>BY</c> instead of <c>AND</c> and represents an optional shift-amount override rather than a second same-kind operand.
///     * For all other non-variadic operators, it parses a single <c>AND</c> expression using the <c>SingleAndExpression</c> parser, returning an array of one <see cref="BWHazel.TopsyTurvy.Ast.Expression"/> if matched or an empty array if not matched.
///         * The empty array handles single operand operators such as <c>HARDLY EVER</c>.
/// * Finally, it matches on the closer of the expression, once again checking if the operator is variadic:
///     * If variadic, it matches on the required sequence of whitespace followed by the keyword "IF YOU PLEASE.", which is discarded.
///     * Otherwise, no closer is required and returns an empty string.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// SUM OF 3 AND PRODUCT OF 4 AND 5
/// ALL OF VERITY AND HARDLY EVER NAY AND PRE-ADAMITE 10 AND 5 IF YOU PLEASE.
/// </code>
/// * The first example would return a <see cref="PrefixExpressionNode"/> with:
///     * The <c>SUM OF</c> operator.
///     * The operator is not variadic, so only the second operand is matched.
///     * The first operand is the <see cref="LiteralNode"/> <c>3</c> and the second operand is a <see cref="PrefixExpressionNode"/> for <c>PRODUCT OF 4 AND 5</c>.
///     * There is no closer therefore an empty string is returned and discarded.
/// * The second example would also return a <see cref="PrefixExpressionNode"/> with:
///     * The <c>ALL OF</c> operator.
///     * The operator is variadic, so any number of operands can be matched, 2 additional in this case: a <see cref="PrefixExpressionNode"/> for <c>HARDLY EVER NAY</c> and another <see cref="PrefixExpressionNode"/> for <c>PRE-ADAMITE 10 AND 5</c>.
///     * As the operator is variadic, the closer <c>IF YOU PLEASE.</c> is required and matched.
/// </para>
/// <para>
/// ### Summon Expressions (Function Calls)
/// The <c>SummonExpression</c> parser matches on a function call in the form of
/// <c>SUMMON FUNCTION_NAME WITH ARG1 [AND ARG2 ...] IF YOU PLEASE.</c>, returning a <see cref="PrefixExpressionNode"/> with the
/// operator set to <c>SUMMON</c> and the first argument as an <see cref="IdentifierNode"/> for the function name followed by the
/// argument <see cref="Expression"/>s:
/// * It first matches on the <c>SUMMON</c> keyword and required whitespace, both of which are discarded.
/// * It then matches on the function name as an identifier, followed by more required whitespace which is discarded.
///     * The function name is the first argument in the resulting <see cref="PrefixExpressionNode"/>.
/// * It then matches on the <c>WITH</c> keyword and more required whitespace, both of which are also discarded.
/// * It then matches on the arguments, first checking if there are no arguments by trying to match on the <c>NOTHING</c> keyword.
///     * If no arguments, it returns an empty list of <see cref="BWHazel.TopsyTurvy.Ast.Expression"/>s, but if this match fails it back-tracks and attempts to match on arguments.
///     * If there are arguments:
///         * It first matches on required whitespace which is discarded.
///         * It then recursively matches on the first argument as an <see cref="BWHazel.TopsyTurvy.Ast.Expression"/>.
///         * It then uses <c>AndExpression</c> to match on any subsequent arguments, returning a list of the matched <see cref="BWHazel.TopsyTurvy.Ast.Expression"/>s.
/// * Finally, it matches on the required <c>IF YOU PLEASE.</c> closer, discarding it.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// SUMMON HowManyMaidens WITH NOTHING IF YOU PLEASE.
/// SUMMON TotalLords WITH Conservatives AND Liberals IF YOU PLEASE.
/// </code>
/// * The first example would return a <see cref="PrefixExpressionNode"/> with:
///     * The <c>SUMMON</c> operator.
///     * The function name as an <see cref="IdentifierNode"/> for <c>HowManyMaidens</c>.
///     * No arguments, so an empty list of <see cref="BWHazel.TopsyTurvy.Ast.Expression"/>s.
/// * The second example would also return a <see cref="PrefixExpressionNode"/> with:
///     * The <c>SUMMON</c> operator.
///     * The function name as an <see cref="IdentifierNode"/> for <c>TotalLords</c>.
///     * Two arguments, an <see cref="IdentifierNode"/> each for <c>Conservatives</c> and <c>Liberals</c>.
/// </para>
/// <para>
/// ### Array Index Expressions
/// The <c>ArrayIndexExpression</c> parser matches on array element access, returning an <see cref="ArrayIndexNode"/>
/// that can be used wherever an <see cref="BWHazel.TopsyTurvy.Ast.Expression"/> is expected.  The index is 1-based:
/// <c>VICTIM 1</c> is the first element.
/// * It first matches the <c>VICTIM</c> keyword.
/// * It then matches required whitespace followed by a recursive <see cref="Expression"/> call for the index: any expression that evaluates to a <c>PEER</c> is accepted.
/// * It then matches required whitespace followed by the <c>ON</c> keyword.
/// * Finally it matches required whitespace followed by the array name via <c>ArrayNameParser</c>, which tries the
///   built-in <c>THE PROPS</c> keyword first and falls back to a plain identifier.
///
/// This parser supports back-tracking on failure.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// BEHOLD VICTIM 1 ON miscreants
/// PRAY WELCOME first AS A YARN BEING VICTIM 1 ON miscreants
/// BEHOLD VICTIM 1 ON THE PROPS
/// </code>
/// the first two would return an <see cref="ArrayIndexNode"/> with the index expression set to a <see cref="LiteralNode"/> of
/// integer 1 and the array name set to <c>miscreants</c>; the third sets the array name to <c>THE PROPS</c>.
/// </para>
/// <para>
/// This parser must appear in the <c>Expression</c> alternatives before <c>IdentifierExpression</c> so that the
/// <c>VICTIM</c> keyword is recognised as a keyword rather than consumed as an identifier.
/// </para>
/// <para>
/// ### Array Length Expressions
/// The <c>ArrayLengthExpression</c> parser matches on <c>RECKONING OF &lt;array&gt;</c>, returning an
/// <see cref="ArrayLengthNode"/> that evaluates to the number of elements in the array as a <c>PEER</c> (integer).
/// * It first matches the <c>RECKONING OF</c> keyword.
/// * It then matches required whitespace followed by the array name via <c>ArrayNameParser</c>, which tries the built-in <c>THE PROPS</c> keyword first and falls back to a plain identifier.
///
/// This parser supports back-tracking on failure and must appear before <c>IdentifierExpression</c> so that
/// <c>RECKONING</c> is recognised as a keyword rather than consumed as an identifier.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// BEHOLD RECKONING OF miscreants
/// length IS APPOINTED RECKONING OF miscreants
/// BEHOLD RECKONING OF THE PROPS
/// </code>
/// all three return an <see cref="ArrayLengthNode"/> with the array name set to <c>miscreants</c> on the first 2
/// and <c>THE PROPS</c> respectively.
/// </para>
/// <para>
/// ### Cast Expressions
/// The <c>ExpressionCast</c> parser matches on a non-mutating type cast in the form
/// <c>AS IT WERE &lt;expression&gt; AS A &lt;type&gt;</c>, returning an
/// <see cref="ExpressionCastNode"/> that can be used wherever an <see cref="BWHazel.TopsyTurvy.Ast.Expression"/> is expected.
/// When used as a standalone statement the result is stored in the implicit <c>JUST SO</c> variable via the
/// <c>ExpressionStatement</c> path.
/// </para>
/// <para>
/// ### Non-Ternary Expressions
/// The <c>NonTernaryExpression</c> parser matches on any valid expression that is not itself a ternary, returning an
/// <see cref="BWHazel.TopsyTurvy.Ast.Expression"/>.  It is identical to <c>Expression</c> minus the <c>TernaryExpression</c>
/// alternative.  It exists solely to provide a non-left-recursive entry point for the two positions within a ternary
/// expression that must not themselves be ternaries:
/// * <c>TrueValue</c>: If a ternary were allowed here, parsing <c>TrueValue</c> would immediately try another
///     ternary, which would try another, producing infinite recursion.
/// * <c>Condition</c>: If a ternary were allowed here, the ternary own <c>OTHERWISE,</c> keyword could be consumed
///     by the inner ternary <c>FalseValue</c> before the outer parser sees it, making the grammar ambiguous.
///
/// Recursive sub-expressions within the alternatives of <c>NonTernaryExpression</c> — for example, operands of a prefix
/// operator or arguments of a <c>SUMMON</c> call — still parse via the full <see cref="Expression"/>, so nesting a
/// ternary inside a function argument or operator operand remains valid.
/// </para>
/// <para>
/// ### Ternary Expressions
/// The <c>TernaryExpression</c> parser matches on an inline conditional expression, returning a
/// <see cref="TernaryExpressionNode"/>:
/// * It first matches on a <c>NonTernaryExpression</c> as the true value.
/// * It then matches on the <c>SHOULD IT TRANSPIRE THAT</c> keyword with surrounding whitespace.
///     * If this keyword is not found, the parser backtracks fully and yields control to the next
///       <c>Expression</c> alternative.  This is essential because <c>TrueValue</c> may have consumed part of the input.
/// * It then matches on a <c>NonTernaryExpression</c> as the condition with surrounding whitespace.
/// * It then matches on the <c>OTHERWISE,</c> keyword with surrounding whitespace.
/// * Finally, it recursively matches on the full <see cref="Expression"/> as the false value allowing right-chaining.
///
/// This parser must appear first in the <c>Expression</c> alternatives and must be wrapped in <c>.Try()</c>.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// label IS APPOINTED "High" SHOULD IT TRANSPIRE THAT PRE-ADAMITE score AND 90 OTHERWISE, "Low"
/// rank IS APPOINTED "Senior" SHOULD IT TRANSPIRE THAT PRE-ADAMITE age AND 60 OTHERWISE, ~
///   "Junior" SHOULD IT TRANSPIRE THAT LOWER DEGREE age AND 30 OTHERWISE, "Mid-level"
/// </code>
/// * The first example would return a <see cref="TernaryExpressionNode"/> with:
///     * <c>TrueValue</c> as the <see cref="LiteralNode"/> <c>"High"</c>.
///     * <c>Condition</c> as the <see cref="PrefixExpressionNode"/> <c>PRE-ADAMITE score AND 90</c>.
///     * <c>FalseValue</c> as the <see cref="LiteralNode"/> <c>"Low"</c>.
/// * The second example (right-chained) would return an outer <see cref="TernaryExpressionNode"/> whose
///   <c>FalseValue</c> is itself a <see cref="TernaryExpressionNode"/>, demonstrating right-associative chaining.
/// </para>
/// <para>
/// ### Source Spans
/// Every expression node produced by this class carries a <see cref="BWHazel.TopsyTurvy.Ast.Node.Span"/> mapping the node back to its
/// position in the original (pre-processed) source.  The mechanism is:
/// * <see cref="ParserHelpers.CurrentOffset"/>: A zero-consuming parser inserted at the start and end of each LINQ
///   chain to capture the absolute cursor offset before and after the expression is consumed.
/// * <see cref="ParserHelpers.BuildSpan"/>: Called at node construction time with the two captured offsets converting
///   them to <see cref="BWHazel.TopsyTurvy.Ast.SourceLocation"/> pairs via the <see cref="ParserHelpers.ActiveSourceMap"/> set by
///   <see cref="TopsyTurvyParser"/> before each parse.
/// * <see cref="ParserHelpers.WithOffsets{T}"/>: Used for <c>LiteralExpression</c> whose six branches are written as
///   chained <c>.Select()</c> calls; this extension wraps any parser to return a <c>(Value, StartOffset, EndOffset)</c> tuple.
///
/// The <c>SummonExpression</c> is the only parser that also captures an inner span: the function name
/// <see cref="BWHazel.TopsyTurvy.Ast.IdentifierNode"/> receives its own <c>BuildSpan</c> call using offsets captured
/// immediately before and after the function name <c>Lexer.Identifier</c> consume.
/// </para>
/// <para>
/// ### Expression Parser
/// The <c>Expression</c> parser is the main entry point for parsing any Topsy Turvy expression, matching on any of the above
/// expression types, returning an <see cref="BWHazel.TopsyTurvy.Ast.Expression"/> in the order they are tried as follows:
/// * Ternary Expression (<c>TernaryExpression</c>)
///     * Tried first and parses a <c>NonTernaryExpression</c> as the true value and, if
///       <c>SHOULD IT TRANSPIRE THAT</c> follows, completes the ternary, otherwise backtracks so the non-ternary path is tried.
/// * SUMMON Expression (<c>SummonExpression</c>)
///     * Checked early as <c>SUMMON</c> is a keyword and could be confused with an identifier if checked later.
/// * Cast Expression (<c>ExpressionCast</c>)
/// * Array Index Expression (<c>ArrayIndexExpression</c>)
///     * Checked before <c>IdentifierExpression</c> so <c>VICTIM</c> is matched as a keyword.
/// * Array Length Expression (<c>ArrayLengthExpression</c>)
///     * Checked before <c>IdentifierExpression</c> so <c>RECKONING</c> is matched as a keyword.
/// * Prefix Expression (<c>PrefixExpression</c>)
/// * Literal Expression (<c>LiteralExpression</c>)
/// * Just So Expression (<c>JustSoExpression</c>)
/// * The Props Expression (<c>ThePropsExpression</c>)
///     * Checked before <c>IdentifierExpression</c> so "THE" is not consumed as a plain identifier.
/// * Identifier Expression (<c>IdentifierExpression</c>)
/// </para>
/// </remarks>
public static class ExpressionParser
{
    /// <summary>
    /// Parses an operator keyword and returns the corresponding <see cref="Operator"/> value.
    /// </summary>
    public static readonly TextParser<Operator> OperatorToken =
        Parse.OneOf(
            Lexer.Keyword("SUM OF").Try().Value(Operator.Sum),
            Lexer.Keyword("DIFFERENCE OF").Try().Value(Operator.Difference),
            Lexer.Keyword("PRODUCT OF").Try().Value(Operator.Product),
            Lexer.Keyword("QUOTIENT OF").Try().Value(Operator.Quotient),
            Lexer.Keyword("REMAINDER OF").Try().Value(Operator.Remainder),
            Lexer.Keyword("LARGER OF").Try().Value(Operator.Larger),
            Lexer.Keyword("SMALLER OF").Try().Value(Operator.Smaller),
            Lexer.Keyword("BOTH").Try().Value(Operator.Both),
            Lexer.Keyword("EITHER").Try().Value(Operator.Either),
            Lexer.Keyword("HARDLY EVER").Try().Value(Operator.HardlyEver),
            Lexer.Keyword("ALIKE").Try().Value(Operator.Alike),
            Lexer.Keyword("UNLIKE").Try().Value(Operator.Unlike),
            Lexer.Keyword("PRE-ADAMITE").Try().Value(Operator.PreAdamite),
            Lexer.Keyword("LOWER DEGREE").Try().Value(Operator.LowerDegree),
            Lexer.Keyword("WOVEN OF").Try().Value(Operator.WovenOf),
            Lexer.Keyword("ALL OF").Try().Value(Operator.AllOf),
            Lexer.Keyword("ANY OF").Try().Value(Operator.AnyOf),
            Lexer.Keyword("CHORD OF").Try().Value(Operator.ChordOf),
            Lexer.Keyword("HARMONY OF").Try().Value(Operator.HarmonyOf),
            Lexer.Keyword("DISCORD OF").Try().Value(Operator.DiscordOf),
            Lexer.Keyword("INVERSION OF").Try().Value(Operator.InversionOf),
            Lexer.Keyword("TRANSPOSITION DOWN").Try().Value(Operator.TranspositionDown),
            Lexer.Keyword("TRANSPOSITION UP").Try().Value(Operator.TranspositionUp)
        );

    /// <summary>
    /// Parses a literal value into a <see cref="LiteralNode"/>.
    /// </summary>
    /// <remarks>
    /// Character literals (<c>'A'</c>) are tried before string literals so that the leading <c>'</c> is not
    /// accidentally consumed by another combinator.
    /// </remarks>
    public static readonly TextParser<Expression> LiteralExpression =
        (
            Lexer.NullLiteral
                .WithOffsets()
                .Select(valueWithOffsets => new LiteralNode() { Value = valueWithOffsets.Value, Type = LiteralType.Null, Span = BuildSpan(valueWithOffsets.StartOffset, valueWithOffsets.EndOffset) })
            .Or(Lexer.BooleanLiteral
                .WithOffsets()
                .Select(valueWithOffsets => new LiteralNode() { Value = valueWithOffsets.Value, Type = LiteralType.Boolean, Span = BuildSpan(valueWithOffsets.StartOffset, valueWithOffsets.EndOffset) }))
            .Or(Lexer.CharacterLiteral
                .WithOffsets()
                .Select(valueWithOffsets => new LiteralNode() { Value = valueWithOffsets.Value, Type = LiteralType.Char, Span = BuildSpan(valueWithOffsets.StartOffset, valueWithOffsets.EndOffset) }))
            .Or(Lexer.FloatLiteral
                .WithOffsets()
                .Select(valueWithOffsets => new LiteralNode() { Value = valueWithOffsets.Value, Type = LiteralType.Double, Span = BuildSpan(valueWithOffsets.StartOffset, valueWithOffsets.EndOffset) }))
            .Or(Lexer.IntegerLiteral
                .WithOffsets()
                .Select(valueWithOffsets =>
                {
                    object boxedValue = Lexer.BoxIntegerLiteralValue(valueWithOffsets.Value);
                    return new LiteralNode()
                    {
                        Value = boxedValue,
                        Type = boxedValue is int
                            ? LiteralType.Integer
                            : LiteralType.Long,
                        Span = BuildSpan(valueWithOffsets.StartOffset, valueWithOffsets.EndOffset)
                    };
                }))
            .Or(Lexer.StringLiteral
                .WithOffsets()
                .Select(valueWithOffsets => new LiteralNode() { Value = valueWithOffsets.Value, Type = LiteralType.String, Span = BuildSpan(valueWithOffsets.StartOffset, valueWithOffsets.EndOffset) }))
        ).Select(literalNode => (Expression)literalNode);

    /// <summary>
    /// Matches an array name: either the built-in <c>THE PROPS</c> or any single-word identifier.
    /// </summary>
    /// <remarks>
    /// Used in both <see cref="ArrayIndexExpression"/> and <see cref="StatementParser.ArrayElementAssignment"/>
    /// so that <c>THE PROPS</c> can appear as an array target without being split into two identifier tokens.
    /// </remarks>
    public static readonly TextParser<string> ArrayNameParser =
        Lexer.Keyword("THE PROPS")
            .Select(_ => Keywords.SpecialNames.TheProps)
            .Try()
            .Or(Lexer.Identifier);

    /// <summary>
    /// Parses the implicit variable.
    /// </summary>
    public static readonly TextParser<Expression> JustSoExpression =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("JUST SO")
        from endOffset in CurrentOffset
        select (Expression)new IdentifierNode()
            {
                Name = "JUST SO",
                Span = BuildSpan(startOffset, endOffset)
            };

    /// <summary>
    /// Parses the built-in programme arguments array.
    /// </summary>
    public static readonly TextParser<Expression> ThePropsExpression =
        from startOffset in CurrentOffset
        from _ in Lexer.Keyword("THE PROPS")
        from endOffset in CurrentOffset
        select (Expression)new IdentifierNode()
            {
                Name = Keywords.SpecialNames.TheProps,
                Span = BuildSpan(startOffset, endOffset)
            };

    /// <summary>
    /// Parses a variable or function name into an <see cref="IdentifierNode"/>.
    /// </summary>
    public static readonly TextParser<Expression> IdentifierExpression =
        from startOffset in CurrentOffset
        from name in Lexer.Identifier
        from endOffset in CurrentOffset
        select (Expression)new IdentifierNode()
            {
                Name = name,
                Span = BuildSpan(startOffset, endOffset)
            };

    /// <summary>
    /// Parses a single AND keyword followed by one expression.
    /// </summary>
    /// <remarks>
    /// Serves as the shared building block for <see cref="AndExpression"/> and the non-variadic branch of
    /// <see cref="PrefixExpression"/> for all operators except <see cref="Operator.Either"/>.
    /// </remarks>
    private static readonly TextParser<Expression> SingleAndExpression =
        Lexer.WhitespaceRequired
            .IgnoreThen(Lexer.Keyword("AND"))
            .IgnoreThen(Lexer.WhitespaceRequired)
            .IgnoreThen(Parse.Ref(() => Expression!))
            .Try();

    /// <summary>
    /// Parses a single OR keyword followed by one expression.
    /// </summary>
    /// <remarks>
    /// Used exclusively as the argument separator for <see cref="Operator.Either"/> — the sole binary operator
    /// whose separator is <c>OR</c> rather than <c>AND</c>.
    /// </remarks>
    private static readonly TextParser<Expression> SingleOrExpression =
        Lexer.WhitespaceRequired
            .IgnoreThen(Lexer.Keyword("OR"))
            .IgnoreThen(Lexer.WhitespaceRequired)
            .IgnoreThen(Parse.Ref(() => Expression!))
            .Try();

    /// <summary>
    /// Parses a single BY keyword followed by one expression.
    /// </summary>
    /// <remarks>
    /// Used exclusively as the argument separator for <see cref="Operator.TranspositionUp"/> and
    /// <see cref="Operator.TranspositionDown"/> — the sole operators whose optional second operand
    /// (the shift amount) is introduced by <c>BY</c> rather than <c>AND</c>.  <c>BY</c> is already a
    /// lexed keyword, shared with the ascending/descending loop step clause.
    /// </remarks>
    private static readonly TextParser<Expression> SingleByExpression =
        Lexer.WhitespaceRequired
            .IgnoreThen(Lexer.Keyword("BY"))
            .IgnoreThen(Lexer.WhitespaceRequired)
            .IgnoreThen(Parse.Ref(() => Expression!))
            .Try();

    /// <summary>
    /// Parses a sequence of expressions for variadic operators.
    /// </summary>
    private static readonly TextParser<Expression[]> AndExpression =
        SingleAndExpression.Many();

    /// <summary>
    /// Parses a prefix-notation operator.
    /// </summary>
    public static readonly TextParser<Expression> PrefixExpression =
        from startOffset in CurrentOffset
        from theOperator in OperatorToken.Try()
        from firstExpression in Lexer.WhitespaceRequired
            .IgnoreThen(Parse.Ref(() => Expression!))
        from remainingExpressions in IsVariadic(theOperator)
            ? AndExpression
            : (theOperator == Operator.Either
                ? SingleOrExpression
                : theOperator is Operator.TranspositionUp or Operator.TranspositionDown
                    ? SingleByExpression
                    : SingleAndExpression)
                    .Select(expression => new Expression[] { expression })
                    .OptionalOrDefault(Array.Empty<Expression>())
        from expressionCloser in IsVariadic(theOperator)
            ? Lexer.WhitespaceRequired.IgnoreThen(Lexer.Keyword("IF YOU PLEASE."))
            : Parse.Return<string>(string.Empty)
        from endOffset in CurrentOffset
        select (Expression)new PrefixExpressionNode()
        {
            Operator = theOperator,
            Arguments = new List<Expression>(remainingExpressions.Length + 1) { firstExpression }
                .Concat(remainingExpressions).ToList(),
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses a function call.
    /// </summary>
    public static readonly TextParser<Expression> SummonExpression =
        (from startOffset in CurrentOffset
         from _ in Lexer.Keyword("SUMMON")
         from functionNameStart in Lexer.WhitespaceRequired.IgnoreThen(CurrentOffset)
         from functionName in Lexer.Identifier
         from functionNameEnd in CurrentOffset
         from withKeyword in Lexer.WhitespaceRequired
            .IgnoreThen(Lexer.Keyword("WITH"))
         from arguments in
             Lexer.WhitespaceRequired
                .IgnoreThen(Lexer.Keyword("NOTHING"))
                .Try()
                .Select(_ => new List<Expression>())
                .Or(
                    from firstArgument in Lexer.WhitespaceRequired
                        .IgnoreThen(Parse.Ref(() => Expression!))
                    from remainingArguments in AndExpression
                    select new List<Expression>(remainingArguments.Length + 1) { firstArgument }
                        .Concat(remainingArguments).ToList()
                )
         from expressionCloser in Lexer.WhitespaceRequired
            .IgnoreThen(Lexer.Keyword("IF YOU PLEASE."))
         from endOffset in CurrentOffset
         select (Expression)new PrefixExpressionNode
         {
             Operator = Operator.Summon,
             Arguments = [.. arguments.Prepend(new IdentifierNode() { Name = functionName, Span = BuildSpan(functionNameStart, functionNameEnd) })],
             Span = BuildSpan(startOffset, endOffset)
         }).Try();

    /// <summary>
    /// Parses a Topsy Turvy type keyword and returns the corresponding <see cref="LiteralType"/> value.
    /// </summary>
    /// <remarks>
    /// The <c>STANDING &lt;integer-type&gt;</c> two-token sequence is tried first and on failure the
    /// combinator backtracks to the start of the type position.
    /// </remarks>
    public static readonly TextParser<LiteralType> TypeKeyword =
        // Unsigned integer types.
        (from _ in Lexer.Keyword(Keywords.TypeNames.Standing)
         from baseType in Ws(
             Lexer.Keyword(Keywords.TypeNames.Peer)
                .Value(LiteralType.UnsignedInteger)
                .Or(Lexer.Keyword(Keywords.TypeNames.Chancellor)
                    .Value(LiteralType.UnsignedLong))
                .Or(Lexer.Keyword(Keywords.TypeNames.Pirate)
                    .Value(LiteralType.UnsignedShort))
                .Or(Lexer.Keyword(Keywords.TypeNames.SausageRoll)
                    .Value(LiteralType.Byte)))
         select baseType).Try()
        // Signed integer types.
        .Or(Lexer.Keyword(Keywords.TypeNames.Peer)
            .Value(LiteralType.Integer))
        .Or(Lexer.Keyword(Keywords.TypeNames.Chancellor)
            .Value(LiteralType.Long))
        .Or(Lexer.Keyword(Keywords.TypeNames.Pirate)
            .Value(LiteralType.Short))
        .Or(Lexer.Keyword(Keywords.TypeNames.SausageRoll)
            .Value(LiteralType.SignedByte))
        // Floating-point types.
        .Or(Lexer.Keyword(Keywords.TypeNames.Fathom)
            .Value(LiteralType.Double))
        .Or(Lexer.Keyword(Keywords.TypeNames.Foot)
            .Value(LiteralType.Single))
        // String and character types.
        .Or(Lexer.Keyword(Keywords.TypeNames.Yarn)
            .Value(LiteralType.String))
        .Or(Lexer.Keyword(Keywords.TypeNames.Stitch)
            .Value(LiteralType.Char))
        // Boolean and null types.
        .Or(Lexer.Keyword(Keywords.TypeNames.Decree)
            .Value(LiteralType.Boolean))
        .Or(Lexer.Keyword(Keywords.TypeNames.Naught)
            .Value(LiteralType.Null));

    /// <summary>
    /// Parses an array element access expression.
    /// </summary>
    /// <remarks>
    /// Matches <c>VICTIM &lt;index&gt; ON &lt;array&gt;</c> and returns an <see cref="ArrayIndexNode"/>.
    /// The index is 1-based.  This parser must appear in the <see cref="Expression"/> alternatives before
    /// <see cref="IdentifierExpression"/> so that the <c>VICTIM</c> keyword is matched as a keyword rather than
    /// consumed as an identifier.
    /// </remarks>
    public static readonly TextParser<Expression> ArrayIndexExpression =
        (from startOffset in CurrentOffset
         from victimKeyword in Lexer.Keyword("VICTIM")
         from index in Lexer.WhitespaceRequired
            .IgnoreThen(Parse.Ref(() => Expression!))
         from onKeyword in Lexer.WhitespaceRequired
            .IgnoreThen(Lexer.Keyword("ON"))
         from arrayName in Lexer.WhitespaceRequired
            .IgnoreThen(ArrayNameParser)
         from endOffset in CurrentOffset
         select (Expression)new ArrayIndexNode()
         {
             Index = index,
             ArrayName = arrayName,
             Span = BuildSpan(startOffset, endOffset)
         }).Try();

    /// <summary>
    /// Parses an array length expression.
    /// </summary>
    /// <remarks>
    /// Matches <c>RECKONING OF &lt;array&gt;</c> and returns an <see cref="ArrayLengthNode"/>.
    /// This parser must appear in the <see cref="Expression"/> alternatives before
    /// <see cref="IdentifierExpression"/> so that <c>RECKONING</c> is matched as a keyword rather than
    /// consumed as an identifier.
    /// </remarks>
    public static readonly TextParser<Expression> ArrayLengthExpression =
        (from startOffset in CurrentOffset
         from reckoningKeyword in Lexer.Keyword("RECKONING OF")
         from arrayName in Lexer.WhitespaceRequired
            .IgnoreThen(ArrayNameParser)
         from endOffset in CurrentOffset
         select (Expression)new ArrayLengthNode()
         {
             ArrayName = arrayName,
             Span = BuildSpan(startOffset, endOffset)
         }).Try();

    /// <summary>
    /// Parses a non-mutating expression cast.
    /// </summary>
    public static readonly TextParser<Expression> ExpressionCast =
        from startOffset in CurrentOffset
        from asItWwereKeyword in Lexer.Keyword("AS IT WERE")
        from expression in Ws(Parse.Ref(() => Expression!))
        from asAKeyword in Ws(Lexer.Keyword("AS A"))
        from newType in Ws(TypeKeyword)
        from endOffset in CurrentOffset
        select (Expression)new ExpressionCastNode()
        {
            Expression = expression,
            NewType = newType,
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses any valid Topsy Turvy expression that is not itself a ternary.
    /// </summary>
    /// <remarks>
    /// Used as the parser for <c>TrueValue</c> and <c>Condition</c> in a ternary expression to avoid left-recursion
    /// and <c>OTHERWISE,</c> ambiguity.  Recursive sub-expressions within these alternatives, e.g. operands of a prefix
    /// operator or arguments of a <c>SUMMON</c> call, are parsed via <see cref="Expression"/>, which includes the ternary
    /// form so nesting is still possible inside operands.
    /// </remarks>
    public static readonly TextParser<Expression> NonTernaryExpression =
        SummonExpression
            .Or(ExpressionCast)
            .Or(ArrayIndexExpression)
            .Or(ArrayLengthExpression)
            .Or(Parse.Ref(() => PrefixExpression))
            .Or(Parse.Ref(() => LiteralExpression))
            .Or(JustSoExpression)
            .Or(ThePropsExpression)
            .Or(Parse.Ref(() => IdentifierExpression));

    /// <summary>
    /// Parses a ternary (inline conditional) expression.
    /// </summary>
    /// <remarks>
    /// Matches <c>&lt;true-value&gt; SHOULD IT TRANSPIRE THAT &lt;condition&gt; OTHERWISE, &lt;false-value&gt;</c> and
    /// returns a <see cref="TernaryExpressionNode"/>.  <c>TrueValue</c> and <c>Condition</c> use
    /// <see cref="NonTernaryExpression"/> to prevent left-recursion and <c>OTHERWISE,</c> ambiguity; <c>FalseValue</c>
    /// uses the full <see cref="Expression"/> to allow right-chained ternaries.
    /// This parser must appear first in the <see cref="Expression"/> alternatives and must be wrapped in <c>.Try()</c>
    /// so that the parser backtracks fully when <c>SHOULD IT TRANSPIRE THAT</c> is not found after a <c>TrueValue</c>.
    /// </remarks>
    public static readonly TextParser<Expression> TernaryExpression =
        (from startOffset in CurrentOffset
         from trueValue in NonTernaryExpression
         from shouldItTranspireThatKeyword in Ws(Lexer.Keyword("SHOULD IT TRANSPIRE THAT"))
         from condition in Ws(NonTernaryExpression)
         from otherwiseKeyword in Ws(Lexer.Keyword("OTHERWISE,"))
         from falseValue in Ws(Parse.Ref(() => Expression!))
         from endOffset in CurrentOffset
         select (Expression)new TernaryExpressionNode()
         {
             TrueValue = trueValue,
             Condition = condition,
             FalseValue = falseValue,
             Span = BuildSpan(startOffset, endOffset)
         }).Try();

    /// <summary>
    /// Parses any valid Topsy Turvy expression.
    /// </summary>
    public static readonly TextParser<Expression> Expression =
        TernaryExpression
            .Or(SummonExpression)
            .Or(ExpressionCast)
            .Or(ArrayIndexExpression)
            .Or(ArrayLengthExpression)
            .Or(Parse.Ref(() => PrefixExpression))
            .Or(Parse.Ref(() => LiteralExpression))
            .Or(JustSoExpression)
            .Or(ThePropsExpression)
            .Or(Parse.Ref(() => IdentifierExpression));

    /// <summary>
    /// Determines whether the given operator is variadic.
    /// </summary>
    /// <param name="op">The operator.</param>
    /// <returns><c>true</c> if the operator is variadic, otherwise <c>false</c>.</returns>
    private static bool IsVariadic(Operator op) =>
        op == Operator.WovenOf ||
        op == Operator.Summon ||
        op == Operator.AllOf ||
        op == Operator.AnyOf;
}
