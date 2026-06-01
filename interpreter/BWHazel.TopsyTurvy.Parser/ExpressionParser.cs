using System.Collections.Generic;
using System.Linq;
using Superpower;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Implements the recursive <see cref="TextParser{T}"/> combinators for Topsy Turvy expressions.
/// </summary>
public static class ExpressionParser
{
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
                .Select(v => new LiteralNode { Value = v, Type = LiteralType.Null, Span = PlaceholderSpan })
            .Or(Lexer.BooleanLiteral
                .Select(v => new LiteralNode { Value = v, Type = LiteralType.Boolean, Span = PlaceholderSpan }))
            .Or(Lexer.FloatLiteral
                .Select(v => new LiteralNode { Value = v, Type = LiteralType.Float, Span = PlaceholderSpan }))
            .Or(Lexer.IntegerLiteral
                .Select(v => new LiteralNode { Value = v, Type = LiteralType.Integer, Span = PlaceholderSpan }))
            .Or(Lexer.StringLiteral
                .Select(v => new LiteralNode { Value = v, Type = LiteralType.String, Span = PlaceholderSpan }))
        ).Select(node => (Expression)node);

    /// <summary>
    /// Parses the implicit variable.
    /// </summary>
    public static readonly TextParser<Expression> JustSoExpression =
        Lexer.Keyword("JUST SO")
             .Select(_ => (Expression)new IdentifierNode { Name = "JUST SO", Span = PlaceholderSpan });

    /// <summary>
    /// Parses a variable or function name into an <see cref="IdentifierNode"/>.
    /// </summary>
    public static readonly TextParser<Expression> IdentifierExpression =
        Lexer.Identifier
            .Select(name => (Expression)new IdentifierNode { Name = name, Span = PlaceholderSpan });

    private static readonly TextParser<Expression[]> AndExpression =
        Lexer.WhitespaceRequired
            .IgnoreThen(Lexer.Keyword("AND"))
            .IgnoreThen(Lexer.WhitespaceRequired)
            .IgnoreThen(Parse.Ref(() => Expression)).Try()
        .Many();

    /// <summary>
    /// Parses a prefix-notation operator.
    /// </summary>
    public static readonly TextParser<Expression> PrefixExpression =
        from op in OperatorToken.Try()
        from first in Lexer.WhitespaceRequired.IgnoreThen(Parse.Ref(() => Expression))
        from rest in IsVariadic(op)
            ? AndExpression
            : Lexer.WhitespaceRequired
                   .IgnoreThen(Lexer.Keyword("AND"))
                   .IgnoreThen(Lexer.WhitespaceRequired)
                   .IgnoreThen(Parse.Ref(() => Expression)).Try()
               .Select(e => new Expression[] { e })
               .OptionalOrDefault(System.Array.Empty<Expression>())
        from _closer in IsVariadic(op)
            ? Lexer.WhitespaceRequired.IgnoreThen(Lexer.Keyword("IF YOU PLEASE."))
            : Parse.Return<string>(string.Empty)
        select (Expression)new PrefixExpressionNode
        {
            Operator = op,
            Arguments = new List<Expression>(rest.Length + 1) { first }.Concat(rest).ToList(),
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses a function call.
    /// </summary>
    public static readonly TextParser<Expression> SummonExpression =
        (from _ in Lexer.Keyword("SUMMON")
         from name in Lexer.WhitespaceRequired.IgnoreThen(Lexer.Identifier)
         from _with in Lexer.WhitespaceRequired.IgnoreThen(Lexer.Keyword("WITH"))
         from args in
             Lexer.WhitespaceRequired.IgnoreThen(Lexer.Keyword("NOTHING")).Try()
                 .Select(_ => new List<Expression>())
             .Or(
                 from first in Lexer.WhitespaceRequired.IgnoreThen(Parse.Ref(() => Expression))
                 from rest in (
                     Lexer.WhitespaceRequired
                         .IgnoreThen(Lexer.Keyword("AND"))
                         .IgnoreThen(Lexer.WhitespaceRequired)
                         .IgnoreThen(Parse.Ref(() => Expression)).Try()
                 ).Many()
                 select new List<Expression>(rest.Length + 1) { first }.Concat(rest).ToList()
             )
         from _cl in Lexer.WhitespaceRequired.IgnoreThen(Lexer.Keyword("IF YOU PLEASE."))
         select (Expression)new PrefixExpressionNode
         {
             Operator = Operator.Summon,
             Arguments = args
                 .Prepend(new IdentifierNode { Name = name, Span = PlaceholderSpan })
                 .ToList(),
             Span = PlaceholderSpan
         }).Try();

    /// <summary>
    /// Parses any valid Topsy Turvy expression.
    /// </summary>
    public static readonly TextParser<Expression> Expression =
        SummonExpression
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
