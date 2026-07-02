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
/// * <see cref="Literal"/> matches a compile-time constant, trying boolean, character, float,
/// integer then string in that order.  This is the same disambiguation order as the Topsy Turvy expression
/// parser in its own literal parsing, minus the null literal, which UtopIR has no equivalent of.  The
/// matched value is boxed as its natural CLR type; integer literals are passed through
/// <see cref="Lexer.BoxIntegerLiteralValue(long)"/>, so a literal is only ever boxed as
/// <see cref="int"/> or <see cref="long"/> so narrower or unsigned CLR types on a
/// <see cref="LiteralOperand"/> only ever arise from an explicit <c>were</c> cast, never directly
/// from parsed literal text.
/// * <see cref="Operand"/> matches either a <see cref="Lexer.Variable"/> reference, wrapped as a
/// <see cref="VariableOperand"/>, or a <see cref="Literal"/>, wrapped as a
/// <see cref="LiteralOperand"/>.
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
    public static readonly TextParser<UtopIRArithmeticOperation> ArithmeticOperation =
        Lexer.Keyword(UtopIRKeywords.Instructions.Sum).Value(UtopIRArithmeticOperation.Sum)
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Diff).Value(UtopIRArithmeticOperation.Diff))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Prod).Value(UtopIRArithmeticOperation.Prod))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Quot).Value(UtopIRArithmeticOperation.Quot))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Rem).Value(UtopIRArithmeticOperation.Rem))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Max).Value(UtopIRArithmeticOperation.Max))
            .Or(Lexer.Keyword(UtopIRKeywords.Instructions.Min).Value(UtopIRArithmeticOperation.Min))
            .Named("arithmetic operator");

    /// <summary>
    /// Parses a compile-time constant literal value returning it boxed as its natural CLR type.
    /// </summary>
    public static readonly TextParser<object> Literal =
        Lexer.BooleanLiteral.Select(value => (object)value)
            .Or(Lexer.CharacterLiteral.Select(value => (object)value))
            .Or(Lexer.FloatLiteral.Select(value => (object)value))
            .Or(Lexer.IntegerLiteral.Select(Lexer.BoxIntegerLiteralValue))
            .Or(Lexer.StringLiteral.Select(value => (object)value))
            .Named("literal");

    /// <summary>
    /// Parses an operand, either a variable reference or a literal value.
    /// </summary>
    public static readonly TextParser<UtopIROperand> Operand =
        Lexer.Variable
            .Select(name => (UtopIROperand)new VariableOperand(new UtopIRVariable(name)))
            .Or(Literal.Select(value => (UtopIROperand)new LiteralOperand(value)))
            .Named("operand");
}
