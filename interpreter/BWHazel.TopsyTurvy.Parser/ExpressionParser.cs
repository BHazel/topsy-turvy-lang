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
/// ### Known Issue: Source Spans
/// The current expression parser implementation does not support parsing of source code with accurate source spans (<see cref="SourceSpan"/>),
/// with all spans set to a placeholder value of line 0 and column 0, which are deliberately out-of-range.  This is an identified known issue
/// and will be addressed in the future.  A workaround, used by the LSP server and web editor, performs a scan line-by-line of the
/// source code to assign approximate spans as needed.  Once the expression parser supports accurate spans, this workaround can be removed.
/// </para>
/// <para>
/// ### Type Keywords
/// The <c>TypeKeyword</c> parser matches on type keywords, returning the corresponding <see cref="LiteralType"/> value.  As an
/// example, the keyword <c>PEER</c> will be parsed as <see cref="LiteralType"/><c>.Integer</c>.  This parser lives in
/// <see cref="ExpressionParser"/> (rather than <see cref="StatementParser"/>) because it is also needed by
/// <see cref="ExpressionCast"/> at the expression layer; placing it here avoids a circular static-field initialisation dependency.
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
/// ### Identifier Expressions
/// 2 parsers are included for identifiers, both returning an <see cref="BWHazel.TopsyTurvy.Ast.Expression"/> which is an <see cref="IdentifierNode"/>.
/// * <c>JustSoExpression</c> matches on the implicit variable keyword "JUST SO", returning an <see cref="IdentifierNode"/> with the name "JUST SO".
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
///     * Otherwise, it parses a single <c>AND</c> expression using the <c>SingleAndExpression</c> parser, returning an array of one <see cref="BWHazel.TopsyTurvy.Ast.Expression"/> if matched or an empty array if not matched.
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
/// * Finally it matches required whitespace followed by an identifier for the array variable name.
///
/// This parser supports back-tracking on failure.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// BEHOLD VICTIM 1 ON miscreants
/// PRAY WELCOME first AS A YARN BEING VICTIM 1 ON miscreants
/// </code>
/// both would return an <see cref="ArrayIndexNode"/> with the index expression set to a <see cref="LiteralNode"/> of
/// integer 1 and the array name set to <c>miscreants</c>.
/// </para>
/// <para>
/// This parser must appear in the <c>Expression</c> alternatives before <c>IdentifierExpression</c> so that the
/// <c>VICTIM</c> keyword is recognised as a keyword rather than consumed as an identifier.
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
/// ### Expression Parser
/// The <c>Expression</c> parser is the main entry point for parsing any Topsy Turvy expression, matching on any of the above
/// expression types, returning an <see cref="BWHazel.TopsyTurvy.Ast.Expression"/> in the order they are tried as follows:
/// * SUMMON Expression (<c>SummonExpression</c>)
///     * This is checked first as <c>SUMMON</c> is a keyword and could be confused with an identifier if checked later.
/// * Cast Expression (<c>ExpressionCast</c>)
/// * Array Index Expression (<c>ArrayIndexExpression</c>)
///     * Checked before <c>IdentifierExpression</c> so <c>VICTIM</c> is matched as a keyword.
/// * Prefix Expression (<c>PrefixExpression</c>)
/// * Literal Expression (<c>LiteralExpression</c>)
/// * Just So Expression (<c>JustSoExpression</c>)
/// * Identifier Expression (<c>IdentifierExpression</c>)
/// </para>
/// </remarks>
public static class ExpressionParser
{
    /// <summary>
    /// A placeholder source span used for all expressions.
    /// </summary>
    /// <remarks>
    /// This is a known issue and will be replaced with accurate spans in the future.
    /// </remarks>
    private static readonly SourceSpan PlaceholderSpan =
        new(new SourceLocation(0, 0), new SourceLocation(0, 0));

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
            Lexer.Keyword("ANY OF").Try().Value(Operator.AnyOf)
        );

    /// <summary>
    /// Parses a literal value into a <see cref="LiteralNode"/>.
    /// </summary>
    public static readonly TextParser<Expression> LiteralExpression =
        (
            Lexer.NullLiteral
                .Select(literalValue => new LiteralNode() { Value = literalValue, Type = LiteralType.Null, Span = PlaceholderSpan })
                .Or(Lexer.BooleanLiteral
                    .Select(literalValue => new LiteralNode() { Value = literalValue, Type = LiteralType.Boolean, Span = PlaceholderSpan }))
                .Or(Lexer.FloatLiteral
                    .Select(literalValue => new LiteralNode() { Value = literalValue, Type = LiteralType.Float, Span = PlaceholderSpan }))
                .Or(Lexer.IntegerLiteral
                    .Select(literalValue => new LiteralNode() { Value = literalValue, Type = LiteralType.Integer, Span = PlaceholderSpan }))
                .Or(Lexer.StringLiteral
                    .Select(literalValue => new LiteralNode() { Value = literalValue, Type = LiteralType.String, Span = PlaceholderSpan }))
        ).Select(literalNode => (Expression)literalNode);

    /// <summary>
    /// Parses the implicit variable.
    /// </summary>
    public static readonly TextParser<Expression> JustSoExpression =
        Lexer.Keyword("JUST SO")
            .Select(_ => (Expression)new IdentifierNode()
                {
                    Name = "JUST SO",
                    Span = PlaceholderSpan
                });

    /// <summary>
    /// Parses a variable or function name into an <see cref="IdentifierNode"/>.
    /// </summary>
    public static readonly TextParser<Expression> IdentifierExpression =
        Lexer.Identifier
            .Select(name => (Expression)new IdentifierNode()
                {
                    Name = name,
                    Span = PlaceholderSpan
                });

    /// <summary>
    /// Parses a single AND keyword followed by one expression.
    /// </summary>
    /// <remarks>
    /// Serves as the shared building block for <see cref="AndExpression"/> and the non-variadic branch of
    /// <see cref="PrefixExpression"/>.
    /// </remarks>
    private static readonly TextParser<Expression> SingleAndExpression =
        Lexer.WhitespaceRequired
            .IgnoreThen(Lexer.Keyword("AND"))
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
        from theOperator in OperatorToken.Try()
        from firstExpression in Lexer.WhitespaceRequired
            .IgnoreThen(Parse.Ref(() => Expression!))
        from remainingExpressions in IsVariadic(theOperator)
            ? AndExpression
            : SingleAndExpression
                  .Select(expression => new Expression[] { expression })
                  .OptionalOrDefault(Array.Empty<Expression>())
        from expressionCloser in IsVariadic(theOperator)
            ? Lexer.WhitespaceRequired.IgnoreThen(Lexer.Keyword("IF YOU PLEASE."))
            : Parse.Return<string>(string.Empty)
        select (Expression)new PrefixExpressionNode()
        {
            Operator = theOperator,
            Arguments = new List<Expression>(remainingExpressions.Length + 1) { firstExpression }
                .Concat(remainingExpressions).ToList(),
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses a function call.
    /// </summary>
    public static readonly TextParser<Expression> SummonExpression =
        (from _ in Lexer.Keyword("SUMMON")
         from functionName in Lexer.WhitespaceRequired
            .IgnoreThen(Lexer.Identifier)
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
         select (Expression)new PrefixExpressionNode
         {
             Operator = Operator.Summon,
             Arguments = arguments
                 .Prepend(new IdentifierNode { Name = functionName, Span = PlaceholderSpan })
                 .ToList(),
             Span = PlaceholderSpan
         }).Try();

    /// <summary>
    /// Parses a Topsy Turvy type keyword and returns the corresponding <see cref="LiteralType"/> value.
    /// </summary>
    public static readonly TextParser<LiteralType> TypeKeyword =
        Lexer.Keyword(Keywords.TypeNames.Peer)
            .Value(LiteralType.Integer)
            .Or(Lexer.Keyword(Keywords.TypeNames.Fathom)
                .Value(LiteralType.Float))
            .Or(Lexer.Keyword(Keywords.TypeNames.Yarn)
                .Value(LiteralType.String))
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
        (from victimKeyword in Lexer.Keyword("VICTIM")
         from index in Lexer.WhitespaceRequired
            .IgnoreThen(Parse.Ref(() => Expression!))
         from onKeyword in Lexer.WhitespaceRequired
            .IgnoreThen(Lexer.Keyword("ON"))
         from arrayName in Lexer.WhitespaceRequired
            .IgnoreThen(Lexer.Identifier)
         select (Expression)new ArrayIndexNode()
         {
             Index = index,
             ArrayName = arrayName,
             Span = PlaceholderSpan
         }).Try();

    /// <summary>
    /// Parses a non-mutating expression cast.
    /// </summary>
    public static readonly TextParser<Expression> ExpressionCast =
        from asItWwereKeyword in Lexer.Keyword("AS IT WERE")
        from expression in Ws(Parse.Ref(() => Expression!))
        from asAKeyword in Ws(Lexer.Keyword("AS A"))
        from newType in Ws(TypeKeyword)
        select (Expression)new ExpressionCastNode()
        {
            Expression = expression,
            NewType = newType,
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses any valid Topsy Turvy expression.
    /// </summary>
    public static readonly TextParser<Expression> Expression =
        SummonExpression
            .Or(ExpressionCast)
            .Or(ArrayIndexExpression)
            .Or(Parse.Ref(() => PrefixExpression))
            .Or(Parse.Ref(() => LiteralExpression))
            .Or(JustSoExpression)
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
