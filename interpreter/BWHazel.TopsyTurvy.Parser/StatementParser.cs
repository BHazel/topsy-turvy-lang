using System.Collections.Generic;
using System.Linq;
using Superpower;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Implements the <see cref="TextParser{T}"/> combinators for Topsy Turvy statement forms.
/// </summary>
public static class StatementParser
{
    private static readonly SourceSpan PlaceholderSpan =
        new SourceSpan(new SourceLocation(0, 0), new SourceLocation(0, 0));

    private static TextParser<T> Ws<T>(TextParser<T> parser) =>
        Lexer.WhitespaceRequired.IgnoreThen(parser);

    /// <summary>
    /// Parses a Topsy Turvy type keyword and returns the corresponding <see cref="LiteralType"/> value.
    /// </summary>
    public static readonly TextParser<LiteralType> TypeKeyword =
        Lexer.Keyword(Keywords.TypeNames.Peer).Value(LiteralType.Integer)
        .Or(Lexer.Keyword(Keywords.TypeNames.Fathom).Value(LiteralType.Float))
        .Or(Lexer.Keyword(Keywords.TypeNames.Yarn).Value(LiteralType.String))
        .Or(Lexer.Keyword(Keywords.TypeNames.Decree).Value(LiteralType.Boolean))
        .Or(Lexer.Keyword(Keywords.TypeNames.Naught).Value(LiteralType.Null));

    /// <summary>
    /// Parses avariable declaration.
    /// </summary>
    public static readonly TextParser<Statement> Declaration =
        from _ in Lexer.Keyword("PRAY WELCOME")
        from name in Ws(Lexer.Identifier)
        from _asA in Ws(Lexer.Keyword("AS A"))
        from type in Ws(TypeKeyword)
        from init in Ws(Lexer.Keyword("BEING").IgnoreThen(Ws(ExpressionParser.Expression)))
                       .Try().OptionalOrDefault(null!)
        select (Statement)new DeclarationNode
        {
            Name = name,
            Type = type,
            InitialValue = init,
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses an assignment statement.
    /// </summary>
    public static readonly TextParser<Statement> Assignment =
        (from name in Lexer.Identifier
         from _ in Ws(Lexer.Keyword("IS APPOINTED"))
         from val in Ws(ExpressionParser.Expression)
         select (Statement)new AssignmentNode
         {
             Target = name,
             Value = val,
             Span = PlaceholderSpan
         }).Try();

    /// <summary>
    /// Parses an in-place type cast.
    /// </summary>
    public static readonly TextParser<Statement> InPlaceCast =
        (from name in Lexer.Identifier
         from _ in Ws(Lexer.Keyword("IS HENCEFORTH A"))
         from type in Ws(TypeKeyword)
         select (Statement)new InPlaceCastNode
         {
             Target = name,
             NewType = type,
             Span = PlaceholderSpan
         }).Try();

    /// <summary>
    /// Parses a non-mutating expression cast.
    /// </summary>
    public static readonly TextParser<Statement> ExpressionCast =
        from _ in Lexer.Keyword("AS IT WERE")
        from expr in Ws(ExpressionParser.Expression)
        from _asA in Ws(Lexer.Keyword("AS A"))
        from type in Ws(TypeKeyword)
        select (Statement)new ExpressionCastNode
        {
            Expression = expr,
            NewType = type,
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses either form of type cast.
    /// </summary>
    public static readonly TextParser<Statement> TypeCast =
        InPlaceCast.Or(ExpressionCast);

    /// <summary>
    /// Parses an output statement.
    /// </summary>
    public static readonly TextParser<Statement> Print =
        from _ in Lexer.Keyword("BEHOLD")
        from expr in Ws(ExpressionParser.Expression)
        from ceremony in Ws(Lexer.Keyword("WITHOUT CEREMONY")).Try().OptionalOrDefault(null!)
        select (Statement)new PrintNode
        {
            Expression = expr,
            SuppressNewline = ceremony is not null,
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses an input statement.
    /// </summary>
    public static readonly TextParser<Statement> Input =
        from _ in Lexer.Keyword("PRAY TELL")
        from name in Ws(Lexer.Identifier)
        select (Statement)new InputNode { Target = name, Span = PlaceholderSpan };

    /// <summary>
    /// Parses the body of a conditional.
    /// </summary>
    public static readonly TextParser<(IReadOnlyList<Statement> TrueBlock, IReadOnlyList<ElseIfBranch> ElseIfs, IReadOnlyList<Statement> ElseBlock)> ConditionalBody =
        from _trueMark in Ws(Lexer.Keyword("QUITE SO.").Named("QUITE SO. (then-block)"))
        from trueBr in Ws(Parse.Ref(() => Statement!)).Try().Many()
        from elseIfs in (
            from _ in Ws(Lexer.Keyword("OR, IF NOT,"))
            from cond in Ws(ExpressionParser.Expression).Try().OptionalOrDefault(null!)
            from block in Ws(Parse.Ref(() => Statement!)).Try().Many()
            select new ElseIfBranch(cond, block.ToList())
        ).Try().Many()
        from elseBr in (
            from _ in Ws(Lexer.Keyword("OTHERWISE,"))
            from block in Ws(Parse.Ref(() => Statement!)).Try().Many()
            select (IReadOnlyList<Statement>)block.ToList()
        ).Try().OptionalOrDefault(null!)
        select (
            (IReadOnlyList<Statement>)trueBr.ToList(),
            (IReadOnlyList<ElseIfBranch>)elseIfs.ToList(),
            elseBr ?? []
        );

    /// <summary>
    /// Parses a conditional statement.
    /// </summary>
    public static readonly TextParser<Statement> Conditional =
        from _ in Lexer.Keyword("SHOULD IT TRANSPIRE THAT")
        from cond in Ws(ExpressionParser.Expression).Try().OptionalOrDefault(null!)
        from body in ConditionalBody
        from _end in Ws(Lexer.Keyword("SO MUCH FOR THAT.").Named("SO MUCH FOR THAT. (end of conditional)"))
        select (Statement)new ConditionalNode
        {
            Condition = cond,
            TrueBlock = body.TrueBlock,
            ElseIfs = body.ElseIfs,
            ElseBlock = body.ElseBlock,
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses the body of a switch.
    /// </summary>
    public static readonly TextParser<(IReadOnlyList<SwitchCase> Cases, IReadOnlyList<Statement> DefaultBlock)> SwitchBody =
        from cases in (
            from _ in Ws(Lexer.Keyword("WHEN ACTING AS"))
            from lit in Ws(
                Lexer.NullLiteral.Select(v => (object?)v)
                .Or(Lexer.BooleanLiteral.Select(v => (object?)v))
                .Or(Lexer.FloatLiteral.Select(v => (object?)v))
                .Or(Lexer.IntegerLiteral.Select(v => (object?)v))
                .Or(Lexer.StringLiteral.Select(v => (object?)v)))
            from body in Ws(Parse.Ref(() => Statement!)).Try().Many()
            select new SwitchCase(lit, body.ToList())
        ).Try().Many()
        from def in (
            from _ in Ws(Lexer.Keyword("FAILING ALL OF THE ABOVE,"))
            from block in Ws(Parse.Ref(() => Statement!)).Try().Many()
            select (IReadOnlyList<Statement>)block.ToList()
        ).Try().OptionalOrDefault(null!)
        select (
            (IReadOnlyList<SwitchCase>)cases.ToList(),
            def ?? (IReadOnlyList<Statement>)new List<Statement>()
        );

    /// <summary>
    /// Parses a switch statement.
    /// </summary>
    public static readonly TextParser<Statement> Switch =
        from _ in Lexer.Keyword("IN WHICH CAPACITY?")
        from expr in Ws(ExpressionParser.Expression).Try().OptionalOrDefault(null!)
        from body in SwitchBody
        from _end in Ws(Lexer.Keyword("NOTHING COULD BE MORE SATISFACTORY.").Named("NOTHING COULD BE MORE SATISFACTORY. (end of switch)"))
        select (Statement)new SwitchNode
        {
            Expression = expr,
            Cases = body.Cases,
            DefaultBlock = body.DefaultBlock,
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses the loop-type clause following the label.
    /// </summary>
    private static readonly TextParser<LoopDefinition> LoopTypeParser =
        (from _ in Ws(Lexer.Keyword("ASCENDING"))
         from var in Ws(Lexer.Identifier)
         from _u in Ws(Lexer.Keyword("UNTIL"))
         from cond in Ws(ExpressionParser.Expression)
         select new LoopDefinition(LoopType.Ascending, cond, var)).Try()
        .Or((from _ in Ws(Lexer.Keyword("DESCENDING"))
             from var in Ws(Lexer.Identifier)
             from _u in Ws(Lexer.Keyword("UNTIL"))
             from cond in Ws(ExpressionParser.Expression)
             select new LoopDefinition(LoopType.Descending, cond, var)).Try())
        .Or((from _ in Ws(Lexer.Keyword("WHILST"))
             from cond in Ws(ExpressionParser.Expression)
             select new LoopDefinition(LoopType.Whilst, cond, null)).Try())
        .Or(Parse.Return(new LoopDefinition(LoopType.Infinite, null, null)));

    /// <summary>
    /// Parses a break statement.
    /// </summary>
    public static readonly TextParser<Statement> Break =
        Lexer.Keyword("THAT WILL DO.")
             .Value((Statement)new BreakNode { Span = PlaceholderSpan });

    /// <summary>
    /// Parses a continue statement.
    /// </summary>
    public static readonly TextParser<Statement> Continue =
        Lexer.Keyword("ONCE MORE.")
             .Value((Statement)new ContinueNode { Span = PlaceholderSpan });

    /// <summary>
    /// Parses a loop.
    /// </summary>
    public static readonly TextParser<Statement> Loop =
        from _ in Lexer.Keyword("BY A LEGAL FICTION")
        from label in Ws(Lexer.Keyword("KNOWN AS").IgnoreThen(Ws(Lexer.Identifier)))
                         .Try().OptionalOrDefault(null!)
        from loopDef in LoopTypeParser
        from body in Ws(Parse.Ref(() => Statement!)).Try().Many()
        from _end in Ws(Lexer.Keyword("THE TERM EXPIRES.").Named("THE TERM EXPIRES. (end of loop)"))
        select (Statement)new LoopNode
        {
            Label = label,
            Type = loopDef.Type,
            Condition = loopDef.Condition,
            LoopVariable = loopDef.Variable,
            Body = body.ToList(),
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses an exception-handling block.
    /// </summary>
    public static readonly TextParser<Statement> TryCatch =
        from _op in Lexer.Keyword("WITH THE GREATEST RESPECT,")
        from op in Ws(ExpressionParser.Expression)
        from _wg in Ws(Lexer.Keyword("WITH GRATITUDE"))
        from succ in Ws(Parse.Ref(() => Statement!)).Try().Many()
        from _mr in Ws(Lexer.Keyword("MODIFIED RAPTURE"))
        from ex in Ws(Parse.Ref(() => Statement!)).Try().Many()
        from _end in Ws(Lexer.Keyword("THAT CONCLUDES THE MATTER.").Named("THAT CONCLUDES THE MATTER. (end of try/catch)"))
        select (Statement)new TryCatchNode
        {
            Operation = op,
            SuccessBlock = succ.ToList(),
            ExceptionBlock = ex.ToList(),
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses the parameter list of a function definition.
    /// </summary>
    public static readonly TextParser<List<string>> ParameterList =
        (from _ in Ws(Lexer.Keyword("UNDER THE TERMS OF"))
         from first in Ws(Lexer.Identifier)
         from rest in Ws(Lexer.Keyword("AND").IgnoreThen(Ws(Lexer.Identifier))).Try().Many()
         select new List<string>(rest.Length + 1) { first }.Concat(rest).ToList())
        .Or(Ws(Lexer.Keyword("UNDER NO OBLIGATION")).Select(_ => new List<string>()));

    /// <summary>
    /// Parses a return statement.
    /// </summary>
    public static readonly TextParser<Statement> Return =
        from _ in Lexer.Keyword("AND SO I FIND")
        from v in Ws(ExpressionParser.Expression)
        select (Statement)new ReturnNode { Value = v, Span = PlaceholderSpan };

    /// <summary>
    /// Parses an early no-value return.
    /// </summary>
    public static readonly TextParser<Statement> EarlyDischarge =
        Lexer.Keyword("MY DUTY IS PREMATURELY DISCHARGED.")
             .Value((Statement)new ReturnNode { Value = null, Span = PlaceholderSpan });

    /// <summary>
    /// Parses a throw statement.
    /// </summary>
    public static readonly TextParser<Statement> Curse =
        from _ in Lexer.Keyword("A HIDEOUS CURSE ON")
        from v in Ws(ExpressionParser.Expression)
        select (Statement)new ThrowNode { Value = v, Span = PlaceholderSpan };

    /// <summary>
    /// Parses a function definition.
    /// </summary>
    public static readonly TextParser<Statement> FunctionDefinition =
        from _ in Lexer.Keyword("IT IS MY DUTY TO PERFORM")
        from name in Ws(Lexer.Identifier)
        from terms in ParameterList
        from body in Ws(Parse.Ref(() => Statement!)).Try().Many()
        from _end in Ws(Lexer.Keyword("MY DUTY IS DISCHARGED.").Named("MY DUTY IS DISCHARGED. (end of function)"))
        select (Statement)new FunctionDefinitionNode
        {
            Name = name,
            Parameters = terms,
            Body = body.ToList(),
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses a variable-declaration block.
    /// </summary>
    public static readonly TextParser<Statement> PrincipalBlock =
        from _ in Lexer.Keyword("PRINCIPALS")
        from decls in (from d in Ws(Declaration) select (DeclarationNode)d).Try().Many()
        from _end in Ws(Lexer.Keyword("THE CURTAIN RISES.").Named("THE CURTAIN RISES. (end of declarations)"))
        select (Statement)new PrincipalBlockNode
        {
            Declarations = decls.ToList(),
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Parses an import directive.
    /// </summary>
    public static readonly TextParser<Statement> Import =
        from _ in Lexer.Keyword("PRAY ADMIT")
        from path in Ws(Lexer.StringLiteral)
        select (Statement)new ImportNode { FilePath = path, Span = PlaceholderSpan };

    /// <summary>
    /// Parses a standalone expression as a statement.
    /// </summary>
    public static readonly TextParser<Statement> ExpressionStatementParser =
        ExpressionParser.SummonExpression
            .Or(ExpressionParser.PrefixExpression)
            .Or(ExpressionParser.LiteralExpression)
            .Or(ExpressionParser.JustSoExpression)
            .Select(e => (Statement)new ExpressionStatement { Expression = e, Span = PlaceholderSpan });

    /// <summary>
    /// Parses any single Topsy Turvy statement.
    /// </summary>
    public static readonly TextParser<Statement> Statement =
        PrincipalBlock
        .Or(Declaration)
        .Or(Assignment)
        .Or(TypeCast)
        .Or(Print)
        .Or(Input)
        .Or(Conditional)
        .Or(Switch)
        .Or(Loop)
        .Or(TryCatch)
        .Or(FunctionDefinition)
        .Or(Import)
        .Or(EarlyDischarge)
        .Or(Return)
        .Or(Curse)
        .Or(Break)
        .Or(Continue)
        .Or(ExpressionStatementParser);
}
