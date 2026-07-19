using System.Collections.Generic;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace BWHazel.TopsyTurvy.UtopIR.Parser;

/// <summary>
/// Implements the <see cref="TextParser{T}"/> combinators for UtopIR instructions and the
/// line-oriented programme structure that contains them.
/// </summary>
/// <remarks>
/// <para>
/// Builds on <see cref="Lexer"/> and <see cref="OperandParser"/> to parse each UtopIR instruction
/// form and assemble them into a programme.
/// </para>
/// <para>
/// ### Line Terminator
/// * <see cref="Newline"/> matches the line-terminating newline, tolerating a preceding <c>\r</c>.
///
/// ### Assignment Instruction Operands
/// Each of the following is a private helper parser for the operands for instructions performing assignment
/// (the "right-hand side").  They all take the already-parsed target variable name and are tried in turn by
/// <see cref="AssignmentInstruction"/> below but differ only in which keyword and operand shape they match:
/// * <c>WelcomeRhs</c> matches the <c>welcome</c> keyword, required whitespace, then a <see cref="OperandParser.Type"/>, producing a <see cref="WelcomeInstruction"/>.
/// * <c>AppointRhs</c> matches the <c>appoint</c> keyword, required whitespace, then an <see cref="OperandParser.Operand"/>, producing an <see cref="AppointInstruction"/>.
/// * <c>WereRhs</c> matches the <c>were</c> keyword, an <see cref="OperandParser.Operand"/>, a comma (with optional surrounding whitespace), then a <see cref="OperandParser.Type"/>, producing a <see cref="WereInstruction"/>.
/// * <c>LeaveRhs</c> matches only the <c>leave</c> keyword, with no operand, producing a <see cref="LeaveInstruction"/>.
/// * <c>ArithmeticRhs</c> matches an <see cref="OperandParser.ArithmeticOperation"/> mnemonic, then two comma-separated <see cref="OperandParser.Operand"/>s, producing an <see cref="ArithmeticInstruction"/>.
/// * <c>BitwiseRhs</c> matches an <see cref="OperandParser.BitwiseOperation"/> mnemonic, then two comma-separated <see cref="OperandParser.Operand"/>s, producing a <see cref="BitwiseInstruction"/>.
/// * <c>InvRhs</c> matches the <c>inv</c> keyword, required whitespace, then a single <see cref="OperandParser.Operand"/>, producing an <see cref="InvInstruction"/>.
/// * <c>ComparisonRhs</c> matches an <see cref="OperandParser.ComparisonOperation"/> mnemonic, then two comma-separated <see cref="OperandParser.Operand"/>s, producing a <see cref="ComparisonInstruction"/>.
/// * <c>LogicalRhs</c> matches an <see cref="OperandParser.LogicalOperation"/> mnemonic, then two comma-separated <see cref="OperandParser.Operand"/>s, producing a <see cref="LogicalInstruction"/>.
/// * <c>HardlyRhs</c> matches the <c>hardly</c> keyword, required whitespace, then a single <see cref="OperandParser.Operand"/>, producing a <see cref="HardlyInstruction"/>.
/// * <c>VictimYarnRhs</c> matches the <c>victim.yarn</c> keyword, required whitespace, then two comma-separated <see cref="OperandParser.Operand"/>s, producing a <see cref="VictimYarnInstruction"/>.
///
/// ### Assignment Instruction
/// * <see cref="AssignmentInstruction"/> matches <c>£&lt;var&gt; = &lt;rhs&gt;</c>:
///     * A <see cref="Lexer.Variable"/> target.
///     * An <c>=</c> (with optional surrounding whitespace).
///     * Whichever of the right-hand-side helpers above matches the keyword that follows.
///
/// ### Standalone Instructions
/// Standalone instructions consist of a single keyword and have no assignment target.
/// * <see cref="Prentice"/> matches the <c>prentice</c> keyword, required whitespace, then an <see cref="OperandParser.Operand"/>, producing a <see cref="PrenticeInstruction"/>.
/// * <see cref="Find"/> matches the <c>find</c> keyword followed by an optional <see cref="OperandParser.Operand"/>, producing a <see cref="FindInstruction"/>.
/// * <see cref="Sail"/> matches the <c>sail</c> keyword, required whitespace, then a <see cref="Lexer.Label"/>, producing a <see cref="SailInstruction"/>.
/// * <see cref="SailAlike"/> matches the <c>sailalike</c> keyword, required whitespace, an <see cref="OperandParser.Operand"/>, a comma (with optional surrounding whitespace), then a <see cref="Lexer.Label"/>, producing a <see cref="SailAlikeInstruction"/>.
/// * <see cref="SailUnlike"/> matches the <c>sailunlike</c> keyword the same way as <see cref="SailAlike"/>, producing a <see cref="SailUnlikeInstruction"/>.
/// * <see cref="Label"/> matches a bare <see cref="Lexer.Label"/> on its own line, producing a <see cref="LabelInstruction"/>.
/// * <see cref="StandaloneInstruction"/> tries all standalone instructions in turn:
///     * <see cref="Prentice"/>.
///     * <see cref="Find"/>.
///     * <see cref="SailAlike"/> and <see cref="SailUnlike"/>, tried before <see cref="Sail"/> for consistency
///     with the longest-mnemonic-first discipline used elsewhere, even though <c>sail</c> is not
///     itself a textual prefix of either (<see cref="Lexer.Keyword(string)"/> matches raw text with
///     no trailing word-boundary check, so this ordering costs nothing and avoids relying on that
///     absence being permanent).
///     * <see cref="Label"/>.
///
/// ### Instruction
/// The top-level parser for all instructions, trying each type in turn:
/// * <see cref="AssignmentInstruction"/>.
/// * <see cref="StandaloneInstruction"/>.
///
/// ### Programme Structure
/// * <c>Line</c> is a private helper matching the content of a single line: optional leading
/// whitespace, then an optional <see cref="Instruction"/> or <see cref="Lexer.Comment"/> (or
/// neither, for a blank line), then optional trailing whitespace.  It returns the matched
/// instruction, or <c>null</c> for a comment or blank line.
/// * <see cref="InstructionSequence"/> parses a whole programme as zero or more <c>Line</c>s
/// separated by <see cref="Newline"/>.  The parser has been designed to be versatile to
/// UtopIR evolution, therefore instead of a flat loop iterating over lines it is a reusable
/// recursive combinator implementing the committed-parse pattern and can therefore return
/// specific locations of errors, rather than silently ending the sequence:
///     * The top-level programme parser (see <see cref="UtopIRParser"/>) is simply
///     <see cref="InstructionSequence"/> run with no terminator, so it stops only at end-of-input.
///     * It returns the instructions found, in source order, with comment and blank lines omitted.
/// </para>
/// </remarks>
public static class InstructionParser
{
    /// <summary>
    /// Parses the line-terminating newline, tolerating a preceding carriage return.
    /// </summary>
    public static readonly TextParser<char> Newline =
        Character.
            EqualTo('\r')
            .Optional()
            .IgnoreThen(Character.EqualTo('\n'));

    /// <summary>
    /// Parses a <c>welcome</c> assignment right-hand side for the given target variable.
    /// </summary>
    /// <param name="target">The already-parsed assignment target variable name.</param>
    private static TextParser<UtopIRInstruction> WelcomeRhs(string target) =>
        from welcomeKeyword in Lexer.Keyword(UtopIRKeywords.Instructions.Welcome)
        from whitespace in Lexer.WhitespaceRequired
        from type in OperandParser.Type
        select (UtopIRInstruction)new WelcomeInstruction(new UtopIRVariable(target), type);

    /// <summary>
    /// Parses an <c>appoint</c> assignment right-hand side for the given target variable.
    /// </summary>
    /// <param name="target">The already-parsed assignment target variable name.</param>
    private static TextParser<UtopIRInstruction> AppointRhs(string target) =>
        from appointKeyword in Lexer.Keyword(UtopIRKeywords.Instructions.Appoint)
        from whitespace in Lexer.WhitespaceRequired
        from value in OperandParser.Operand
        select (UtopIRInstruction)new AppointInstruction(new UtopIRVariable(target), value);

    /// <summary>
    /// Parses a <c>were</c> assignment right-hand side for the given target variable.
    /// </summary>
    /// <param name="target">The already-parsed assignment target variable name.</param>
    private static TextParser<UtopIRInstruction> WereRhs(string target) =>
        from wereKeyword in Lexer.Keyword(UtopIRKeywords.Instructions.Were)
        from whitespace1 in Lexer.WhitespaceRequired
        from value in OperandParser.Operand
        from whitespace2 in Lexer.Whitespace
        from comma in Character.EqualTo(',')
        from whitespace3 in Lexer.Whitespace
        from type in OperandParser.Type
        select (UtopIRInstruction)new WereInstruction(new UtopIRVariable(target), value, type);

    /// <summary>
    /// Parses a <c>leave</c> assignment right-hand side for the given target variable.
    /// </summary>
    /// <param name="target">The already-parsed assignment target variable name.</param>
    private static TextParser<UtopIRInstruction> LeaveRhs(string target) =>
        Lexer.Keyword(UtopIRKeywords.Instructions.Leave)
            .Select(_ => (UtopIRInstruction)new LeaveInstruction(new UtopIRVariable(target)));

    /// <summary>
    /// Parses an arithmetic assignment right-hand side for the given target variable.
    /// </summary>
    /// <param name="target">The already-parsed assignment target variable name.</param>
    private static TextParser<UtopIRInstruction> ArithmeticRhs(string target) =>
        from operation in OperandParser.ArithmeticOperation
        from whitespace1 in Lexer.WhitespaceRequired
        from operand1 in OperandParser.Operand
        from whitespace2 in Lexer.Whitespace
        from comma in Character.EqualTo(',')
        from whitespace3 in Lexer.Whitespace
        from operand2 in OperandParser.Operand
        select (UtopIRInstruction)new ArithmeticInstruction(operation, new UtopIRVariable(target), operand1, operand2);

    /// <summary>
    /// Parses a binary bitwise assignment right-hand side for the given target variable.
    /// </summary>
    /// <param name="target">The already-parsed assignment target variable name.</param>
    private static TextParser<UtopIRInstruction> BitwiseRhs(string target) =>
        from operation in OperandParser.BitwiseOperation
        from whitespace1 in Lexer.WhitespaceRequired
        from operand1 in OperandParser.Operand
        from whitespace2 in Lexer.Whitespace
        from comma in Character.EqualTo(',')
        from whitespace3 in Lexer.Whitespace
        from operand2 in OperandParser.Operand
        select (UtopIRInstruction)new BitwiseInstruction(operation, new UtopIRVariable(target), operand1, operand2);

    /// <summary>
    /// Parses an <c>inv</c> assignment right-hand side for the given target variable.
    /// </summary>
    /// <param name="target">The already-parsed assignment target variable name.</param>
    private static TextParser<UtopIRInstruction> InvRhs(string target) =>
        from invKeyword in Lexer.Keyword(UtopIRKeywords.Instructions.Inv)
        from whitespace in Lexer.WhitespaceRequired
        from operand in OperandParser.Operand
        select (UtopIRInstruction)new InvInstruction(new UtopIRVariable(target), operand);

    /// <summary>
    /// Parses a comparison assignment right-hand side for the given target variable.
    /// </summary>
    /// <param name="target">The already-parsed assignment target variable name.</param>
    private static TextParser<UtopIRInstruction> ComparisonRhs(string target) =>
        from operation in OperandParser.ComparisonOperation
        from whitespace1 in Lexer.WhitespaceRequired
        from operand1 in OperandParser.Operand
        from whitespace2 in Lexer.Whitespace
        from comma in Character.EqualTo(',')
        from whitespace3 in Lexer.Whitespace
        from operand2 in OperandParser.Operand
        select (UtopIRInstruction)new ComparisonInstruction(operation, new UtopIRVariable(target), operand1, operand2);

    /// <summary>
    /// Parses a binary logical assignment right-hand side for the given target variable.
    /// </summary>
    /// <param name="target">The already-parsed assignment target variable name.</param>
    private static TextParser<UtopIRInstruction> LogicalRhs(string target) =>
        from operation in OperandParser.LogicalOperation
        from whitespace1 in Lexer.WhitespaceRequired
        from operand1 in OperandParser.Operand
        from whitespace2 in Lexer.Whitespace
        from comma in Character.EqualTo(',')
        from whitespace3 in Lexer.Whitespace
        from operand2 in OperandParser.Operand
        select (UtopIRInstruction)new LogicalInstruction(operation, new UtopIRVariable(target), operand1, operand2);

    /// <summary>
    /// Parses a <c>hardly</c> assignment right-hand side for the given target variable.
    /// </summary>
    /// <param name="target">The already-parsed assignment target variable name.</param>
    private static TextParser<UtopIRInstruction> HardlyRhs(string target) =>
        from hardlyKeyword in Lexer.Keyword(UtopIRKeywords.Instructions.Hardly)
        from whitespace in Lexer.WhitespaceRequired
        from operand in OperandParser.Operand
        select (UtopIRInstruction)new HardlyInstruction(new UtopIRVariable(target), operand);

    /// <summary>
    /// Parses a <c>victim.yarn</c> assignment right-hand side for the given target variable.
    /// </summary>
    /// <param name="target">The already-parsed assignment target variable name.</param>
    private static TextParser<UtopIRInstruction> VictimYarnRhs(string target) =>
        from victimYarnKeyword in Lexer.Keyword(UtopIRKeywords.Instructions.VictimYarn)
        from whitespace1 in Lexer.WhitespaceRequired
        from yarn in OperandParser.Operand
        from whitespace2 in Lexer.Whitespace
        from comma in Character.EqualTo(',')
        from whitespace3 in Lexer.Whitespace
        from index in OperandParser.Operand
        select (UtopIRInstruction)new VictimYarnInstruction(new UtopIRVariable(target), yarn, index);

    /// <summary>
    /// Parses <c>£&lt;var&gt; = &lt;rhs&gt;</c>, dispatching to the correct right-hand-side parser.
    /// </summary>
    public static readonly TextParser<UtopIRInstruction> AssignmentInstruction =
        from target in Lexer.Variable
        from whitespace1 in Lexer.Whitespace
        from equalsCharacter in Character.EqualTo('=')
        from whitespace2 in Lexer.Whitespace
        from instruction in
            WelcomeRhs(target)
                .Or(AppointRhs(target))
                .Or(WereRhs(target))
                .Or(LeaveRhs(target))
                .Or(ArithmeticRhs(target))
                .Or(BitwiseRhs(target))
                .Or(InvRhs(target))
                .Or(ComparisonRhs(target))
                .Or(LogicalRhs(target))
                .Or(HardlyRhs(target))
                .Or(VictimYarnRhs(target))
        select instruction;

    /// <summary>
    /// Parses a <c>prentice</c> instruction.
    /// </summary>
    public static readonly TextParser<UtopIRInstruction> Prentice =
        from prenticeKeyword in Lexer.Keyword(UtopIRKeywords.Instructions.Prentice)
        from whitespace in Lexer.WhitespaceRequired
        from value in OperandParser.Operand
        select (UtopIRInstruction)new PrenticeInstruction(value);

    /// <summary>
    /// Parses a <c>find</c> instruction, with or without a return value.
    /// </summary>
    public static readonly TextParser<UtopIRInstruction> Find =
        from findKeyword in Lexer.Keyword(UtopIRKeywords.Instructions.Find)
        from value in Lexer.WhitespaceRequired.IgnoreThen(OperandParser.Operand)
            .Try()
            .OptionalOrDefault(null!)
        select (UtopIRInstruction)new FindInstruction(value);

    /// <summary>
    /// Parses a <c>sailalike</c> instruction.
    /// </summary>
    public static readonly TextParser<UtopIRInstruction> SailAlike =
        from sailAlikeKeyword in Lexer.Keyword(UtopIRKeywords.Instructions.SailAlike)
        from whitespace1 in Lexer.WhitespaceRequired
        from value in OperandParser.Operand
        from whitespace2 in Lexer.Whitespace
        from comma in Character.EqualTo(',')
        from whitespace3 in Lexer.Whitespace
        from label in Lexer.Label
        select (UtopIRInstruction)new SailAlikeInstruction(value, new UtopIRLabel(label));

    /// <summary>
    /// Parses a <c>sailunlike</c> instruction.
    /// </summary>
    public static readonly TextParser<UtopIRInstruction> SailUnlike =
        from sailUnlikeKeyword in Lexer.Keyword(UtopIRKeywords.Instructions.SailUnlike)
        from whitespace1 in Lexer.WhitespaceRequired
        from value in OperandParser.Operand
        from whitespace2 in Lexer.Whitespace
        from comma in Character.EqualTo(',')
        from whitespace3 in Lexer.Whitespace
        from label in Lexer.Label
        select (UtopIRInstruction)new SailUnlikeInstruction(value, new UtopIRLabel(label));

    /// <summary>
    /// Parses an unconditional <c>sail</c> instruction.
    /// </summary>
    public static readonly TextParser<UtopIRInstruction> Sail =
        from sailKeyword in Lexer.Keyword(UtopIRKeywords.Instructions.Sail)
        from whitespace in Lexer.WhitespaceRequired
        from label in Lexer.Label
        select (UtopIRInstruction)new SailInstruction(new UtopIRLabel(label));

    /// <summary>
    /// Parses a bare label declaration.
    /// </summary>
    public static readonly TextParser<UtopIRInstruction> Label =
        from label in Lexer.Label
        select (UtopIRInstruction)new LabelInstruction(new UtopIRLabel(label));

    /// <summary>
    /// Parses any of the standalone instruction forms with no assignment target.
    /// </summary>
    public static readonly TextParser<UtopIRInstruction> StandaloneInstruction =
        Prentice
            .Or(Find)
            .Or(SailAlike)
            .Or(SailUnlike)
            .Or(Sail)
            .Or(Label);

    /// <summary>
    /// Parses any single UtopIR instruction.
    /// </summary>
    public static readonly TextParser<UtopIRInstruction> Instruction =
        AssignmentInstruction
            .Or(StandaloneInstruction);

    /// <summary>
    /// Parses the content of a single line.
    /// </summary>
    /// <remarks>
    /// This can be an optional instruction,  an optional comment, or
    /// neither (a blank line), surrounded by optional whitespace.
    /// </remarks>
    private static readonly TextParser<UtopIRInstruction?> Line =
        from leadingWhitespace in Lexer.Whitespace
        from content in
            Instruction.Select(instruction => (UtopIRInstruction?)instruction)
                .Or(Lexer.Comment.Select(_ => (UtopIRInstruction?)null))
                .OptionalOrDefault(null)
        from trailingWhitespace in Lexer.Whitespace
        select content;

    /// <summary>
    /// Parses zero or more <see cref="Line"/>s separated by <see cref="Newline"/>, stopping at
    /// either end-of-input or, if given, a match against <paramref name="terminator"/>.
    /// </summary>
    /// <param name="terminator">An optional parser whose success, without being consumed, ends the sequence; <c>null</c> to run to end-of-input only.</param>
    /// <returns>A parser producing the instructions found, in source order, with comment/blank lines omitted.</returns>
    public static TextParser<UtopIRInstruction[]> InstructionSequence(TextParser<Unit>? terminator = null) =>
        input =>
        {
            List<UtopIRInstruction> instructions = [];
            TextSpan remainder = input;
            while (true)
            {
                if (terminator is not null && terminator(remainder).HasValue)
                {
                    break;
                }

                if (remainder.IsAtEnd)
                {
                    break;
                }

                Result<UtopIRInstruction?> lineResult = Line(remainder);
                if (!lineResult.HasValue)
                {
                    return Result.CastEmpty<UtopIRInstruction?, UtopIRInstruction[]>(lineResult);
                }

                if (lineResult.Value is not null)
                {
                    instructions.Add(lineResult.Value);
                }

                remainder = lineResult.Remainder;
                if (remainder.IsAtEnd)
                {
                    break;
                }

                Result<char> newlineResult = Newline(remainder);
                if (!newlineResult.HasValue)
                {
                    return Result.CastEmpty<char, UtopIRInstruction[]>(newlineResult);
                }

                remainder = newlineResult.Remainder;
            }

            return Result.Value(instructions.ToArray(), input, remainder);
        };
}
