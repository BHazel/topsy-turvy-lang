using BWHazel.TopsyTurvy.UtopIR.Ast;
using Superpower;

namespace BWHazel.TopsyTurvy.UtopIR.Parser;

/// <summary>
/// Implements the <see cref="TextParser{T}"/> combinators for UtopIR types, operators and operands.
/// </summary>
/// <remarks>
/// <para>
/// The following parsers are defined for working with operands in UtopIR instructions:
/// * <see cref="Type"/> matches one of the supported UtopIR type keywords, returning the corresponding
/// <see cref="UtopIRType"/>.
/// * <see cref="ArithmeticOperation"/> matches one of the supported arithmetic instructions,
/// returning the corresponding <see cref="UtopIRArithmeticOperation"/>.
/// * <see cref="BitwiseOperation"/> matches one of the supported binary bitwise instructions,
/// returning the corresponding <see cref="UtopIRBitwiseOperation"/>.
/// * <see cref="ComparisonOperation"/> matches one of the supported comparison instructions,
/// returning the corresponding <see cref="UtopIRComparisonOperation"/>.
/// * <see cref="LogicalOperation"/> matches one of the supported binary logical instructions,
/// returning the corresponding <see cref="UtopIRLogicalOperation"/>.
/// * <see cref="PointerArithmeticOperation"/> matches one of the supported pointer arithmetic
/// instructions, returning the corresponding <see cref="UtopIRPointerArithmeticOperation"/>.
/// * <see cref="Literal"/> matches a compile-time constant, trying boolean, <c>naught</c> (the null
/// literal, returned as the <see cref="NaughtLiteral"/> singleton), character, float, integer then
/// string in that order.  The matched value is boxed as its natural CLR type; integer literals are passed through
/// <see cref="Lexer.BoxIntegerLiteralValue(long)"/>, so a literal is only ever boxed as
/// <see cref="int"/> or <see cref="long"/> so narrower or unsigned CLR types on a
/// <see cref="LiteralOperand"/> only ever arise from an explicit <c>were</c> cast, never directly
/// from parsed literal text.
/// * <see cref="Operand"/> matches either a <see cref="Lexer.Variable"/> reference, wrapped as a
/// <see cref="VariableOperand"/>, a <see cref="Lexer.Parameter"/> reference, wrapped as a
/// <see cref="ParameterOperand"/>, or a <see cref="Literal"/>, wrapped as a
/// <see cref="LiteralOperand"/>.
/// * <see cref="TermType"/> matches a type valid in a <c>term</c>/<c>finds</c> signature position,
/// either a plain <see cref="Type"/> or the array form <c>list.&lt;type&gt;</c>, returning the
/// corresponding <see cref="UtopIRTermType"/>.
/// </para>
/// </remarks>
public static class OperandParser
{
    /// <summary>
    /// Parses a UtopIR type keyword returning the corresponding <see cref="UtopIRType"/>.
    /// </summary>
    public static readonly TextParser<UtopIRType> Type =
        Lexer.Keyword(UtopIRKeywords.TypeNames.Chancellor).Value(UtopIRType.Chancellor)
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.StandingChancellor).Value(UtopIRType.StandingChancellor))
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.Peer).Value(UtopIRType.Peer))
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.StandingPeer).Value(UtopIRType.StandingPeer))
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.Pirate).Value(UtopIRType.Pirate))
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.StandingPirate).Value(UtopIRType.StandingPirate))
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.SausageRoll).Value(UtopIRType.SausageRoll))
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.StandingSausageRoll).Value(UtopIRType.StandingSausageRoll))
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.Fathom).Value(UtopIRType.Fathom))
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.Foot).Value(UtopIRType.Foot))
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.Decree).Value(UtopIRType.Decree))
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.Stitch).Value(UtopIRType.Stitch))
            .Or(Lexer.Keyword(UtopIRKeywords.TypeNames.Yarn).Value(UtopIRType.Yarn))
            .Named("type");

    /// <summary>
    /// Parses an arithmetic operation mnemonic returning the corresponding <see cref="UtopIRArithmeticOperation"/>.
    /// </summary>
    /// <remarks>
    /// Each <c>.f</c>-suffixed floating-point mnemonic is tried before its plain integer
    /// counterpart.  <see cref="Lexer.Keyword"/> matches raw text with no trailing word-boundary
    /// check, so trying <c>sum</c> first against the input <c>sum.f</c> would succeed on the
    /// <c>sum</c> prefix and leave <c>.f</c> behind to fail the rest of the instruction parse.
    /// </remarks>
    public static readonly TextParser<UtopIRArithmeticOperation> ArithmeticOperation =
        Lexer.Keyword(UtopIRKeywords.Instructions.SumFloat).Value(UtopIRArithmeticOperation.SumFloat)
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Sum).Value(UtopIRArithmeticOperation.Sum))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.DiffFloat).Value(UtopIRArithmeticOperation.DiffFloat))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Diff).Value(UtopIRArithmeticOperation.Diff))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.ProdFloat).Value(UtopIRArithmeticOperation.ProdFloat))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Prod).Value(UtopIRArithmeticOperation.Prod))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.QuotFloat).Value(UtopIRArithmeticOperation.QuotFloat))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Quot).Value(UtopIRArithmeticOperation.Quot))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.RemFloat).Value(UtopIRArithmeticOperation.RemFloat))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Rem).Value(UtopIRArithmeticOperation.Rem))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.MaxFloat).Value(UtopIRArithmeticOperation.MaxFloat))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Max).Value(UtopIRArithmeticOperation.Max))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.MinFloat).Value(UtopIRArithmeticOperation.MinFloat))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Min).Value(UtopIRArithmeticOperation.Min))
            .Named("arithmetic operator");

    /// <summary>
    /// Parses a pointer arithmetic operation mnemonic returning the corresponding
    /// <see cref="UtopIRPointerArithmeticOperation"/>.
    /// </summary>
    public static readonly TextParser<UtopIRPointerArithmeticOperation> PointerArithmeticOperation =
        Lexer.Keyword(UtopIRKeywords.Instructions.SumPointer).Value(UtopIRPointerArithmeticOperation.Sum)
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.DiffPointer).Value(UtopIRPointerArithmeticOperation.Diff))
            .Named("pointer arithmetic operator");

    /// <summary>
    /// Parses a binary bitwise operation mnemonic returning the corresponding <see cref="UtopIRBitwiseOperation"/>.
    /// </summary>
    /// <remarks>
    /// The unary <c>inv</c> mnemonic is not included here as its instruction shape is different. It is
    /// matched directly by the instruction parser.
    /// </remarks>
    public static readonly TextParser<UtopIRBitwiseOperation> BitwiseOperation =
        Lexer.Keyword(UtopIRKeywords.Instructions.Chord).Value(UtopIRBitwiseOperation.Chord)
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Harmony).Value(UtopIRBitwiseOperation.Harmony))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Discord).Value(UtopIRBitwiseOperation.Discord))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.TransUp).Value(UtopIRBitwiseOperation.TransUp))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.TransDown).Value(UtopIRBitwiseOperation.TransDown))
            .Named("bitwise operator");

    /// <summary>
    /// Parses a comparison operation mnemonic returning the corresponding <see cref="UtopIRComparisonOperation"/>.
    /// </summary>
    /// <remarks>
    /// Each <c>.f</c>-suffixed floating-point mnemonic is tried before its plain integer
    /// counterpart, for the same reason documented on <see cref="ArithmeticOperation"/>.
    /// </remarks>
    public static readonly TextParser<UtopIRComparisonOperation> ComparisonOperation =
        Lexer.Keyword(UtopIRKeywords.Instructions.AlikeFloat).Value(UtopIRComparisonOperation.AlikeFloat)
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Alike).Value(UtopIRComparisonOperation.Alike))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.UnlikeFloat).Value(UtopIRComparisonOperation.UnlikeFloat))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Unlike).Value(UtopIRComparisonOperation.Unlike))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.PreAdamFloat).Value(UtopIRComparisonOperation.PreAdamFloat))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.PreAdam).Value(UtopIRComparisonOperation.PreAdam))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.LowerDegFloat).Value(UtopIRComparisonOperation.LowerDegFloat))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.LowerDeg).Value(UtopIRComparisonOperation.LowerDeg))
            .Named("comparison operator");

    /// <summary>
    /// Parses a binary logical operation mnemonic returning the corresponding <see cref="UtopIRLogicalOperation"/>.
    /// </summary>
    /// <remarks>
    /// The unary <c>hardly</c> mnemonic is not included here as its instruction shape is different. It is
    /// matched directly by the instruction parser.
    /// </remarks>
    public static readonly TextParser<UtopIRLogicalOperation> LogicalOperation =
        Lexer.Keyword(UtopIRKeywords.Instructions.Both).Value(UtopIRLogicalOperation.Both)
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Either).Value(UtopIRLogicalOperation.Either))
            .Named("logical operator");

    /// <summary>
    /// Parses a compile-time constant literal value returning it boxed as its natural CLR type.
    /// </summary>
    /// <remarks>
    /// The <c>naught</c> null literal is returned as the <see cref="NaughtLiteral"/> singleton, not a
    /// bare CLR <c>null</c>, so downstream switches over the value can pattern-match it by type.
    /// </remarks>
    public static readonly TextParser<object> Literal =
        Lexer.BooleanLiteral.Select(value => (object)value)
            .Or(Lexer.Keyword(UtopIRKeywords.Literals.Naught).Select(_ => (object)NaughtLiteral.Instance))
            .Or(Lexer.CharacterLiteral.Select(value => (object)value))
            .Or(Lexer.FloatLiteral.Select(value => (object)value))
            .Or(Lexer.IntegerLiteral.Select(Lexer.BoxIntegerLiteralValue))
            .Or(Lexer.StringLiteral.Select(value => (object)value))
            .Named("literal");

    /// <summary>
    /// Parses an operand, either a variable reference, a parameter reference or a literal value.
    /// </summary>
    public static readonly TextParser<UtopIROperand> Operand =
        Lexer.Variable
            .Select(name => (UtopIROperand)new VariableOperand(new UtopIRVariable(name)))
            .Or(Lexer.Parameter.Select(name => (UtopIROperand)new ParameterOperand(new UtopIRParameter(name))))
            .Or(Literal.Select(value => (UtopIROperand)new LiteralOperand(value)))
            .Named("operand");

    /// <summary>
    /// Parses a type valid in a <c>term</c>/<c>finds</c> signature position, returning the
    /// corresponding <see cref="UtopIRTermType"/>.
    /// </summary>
    public static readonly TextParser<UtopIRTermType> TermType =
        (from listPrefix in Lexer.Keyword(UtopIRKeywords.SpecialTypes.ListPrefix)
         from elementType in Type
         select new UtopIRTermType(UtopIRType.Array, elementType))
            .Try()
            .Or(Type.Select(type => new UtopIRTermType(type)))
            .Named("term type");
}
