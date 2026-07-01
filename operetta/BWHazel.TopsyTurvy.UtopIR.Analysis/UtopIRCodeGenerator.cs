using System;
using System.Globalization;
using System.Text;
using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.UtopIR.Analysis;

/// <summary>
/// Generates human-readable UtopIR source text from a <see cref="UtopIRProgram"/> AST.
/// </summary>
/// <remarks>
/// The generated output matches the format defined in the specification and grammar.
/// </remarks>
public sealed class UtopIRCodeGenerator
{
    /// <summary>
    /// Generates UtopIR source code for the given programme.
    /// </summary>
    /// <param name="program">The root UtopIR AST node to generate source from.</param>
    /// <returns>A string containing the complete UtopIR source, one instruction per line.</returns>
    public string Generate(UtopIRProgram program)
    {
        StringBuilder generatedCodeBuilder = new();
        foreach (UtopIRInstruction instruction in program.Instructions)
        {
            this.WriteInstruction(instruction, generatedCodeBuilder);
        }

        return generatedCodeBuilder.ToString();
    }

    /// <summary>
    /// Writes a single instruction to the builder, followed by a newline.
    /// </summary>
    /// <param name="instruction">The instruction to emit.</param>
    /// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
    private void WriteInstruction(UtopIRInstruction instruction, StringBuilder builder)
    {
        switch (instruction)
        {
            case WelcomeInstruction welcome:
                builder.AppendLine($"£{welcome.Target.Name} = {UtopIRKeywords.Instructions.Welcome} {this.TypeKeyword(welcome.Type)}");
                break;
            case AppointInstruction appoint:
                builder.AppendLine($"£{appoint.Target.Name} = {UtopIRKeywords.Instructions.Appoint} {this.FormatOperand(appoint.Value)}");
                break;
            case ArithmeticInstruction arithmetic:
                builder.AppendLine(
                    $"£{arithmetic.Target.Name} = {this.OperationMnemonic(arithmetic.Operation)} " +
                    $"{this.FormatOperand(arithmetic.Operand1)}, {this.FormatOperand(arithmetic.Operand2)}");
                break;
            case PrenticeInstruction prentice:
                builder.AppendLine($"{UtopIRKeywords.Instructions.Prentice} {this.FormatOperand(prentice.Value)}");
                break;
            case LeaveInstruction leave:
                builder.AppendLine($"£{leave.Target.Name} = {UtopIRKeywords.Instructions.Leave}");
                break;
            case FindInstruction find:
                if (find.Value is null)
                {
                    builder.AppendLine(UtopIRKeywords.Instructions.Find);
                }
                else
                {
                    builder.AppendLine($"{UtopIRKeywords.Instructions.Find} {this.FormatOperand(find.Value)}");
                }

                break;
        }
    }

    /// <summary>
    /// Formats an operand as its UtopIR source representation.
    /// </summary>
    /// <param name="operand">The operand to format.</param>
    /// <returns>
    /// For a <see cref="VariableOperand"/>, the variable name prefixed with <c>£</c>.
    /// For a <see cref="LiteralOperand"/>, the formatted literal value.
    /// </returns>
    private string FormatOperand(UtopIROperand operand) => operand switch
    {
        VariableOperand variable => $"£{variable.Variable.Name}",
        LiteralOperand literal => this.FormatLiteral(literal.Value),
        _ => throw new ArgumentOutOfRangeException(nameof(operand), operand, "Unknown operand type.")
    };

    /// <summary>
    /// Formats a literal value as its UtopIR source representation.
    /// </summary>
    /// <param name="value">The raw literal value.</param>
    /// <returns>The UtopIR source token for the value.</returns>
    private string FormatLiteral(object value)
    {
        return value switch
        {
            bool boolValue => boolValue
                ? UtopIRKeywords.Literals.Verity
                : UtopIRKeywords.Literals.Nay,
            string stringValue => $"\"{this.EscapeString(stringValue)}\"",
            char charValue => $"'{this.EscapeChar(charValue)}'",
            float floatValue => this.FormatFloat(
                floatValue.ToString("G", CultureInfo.InvariantCulture)),
            double doubleValue => this.FormatFloat(
                doubleValue.ToString("G", CultureInfo.InvariantCulture)),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0"
        };
    }

    /// <summary>
    /// Ensures a floating-point string always contains a decimal point.
    /// </summary>
    /// <param name="value">The formatted floating-point string.</param>
    /// <returns>The value with a <c>.0</c> suffix appended when no decimal point is present.</returns>
    private string FormatFloat(string value)
    {
        if (!value.Contains('.') && !value.Contains('E'))
        {
            return value + ".0";
        }

        return value;
    }

    /// <summary>
    /// Applies UtopIR <c>~</c>-based escape sequences to a string value.
    /// </summary>
    /// <param name="value">The raw string value.</param>
    /// <returns>The escaped string, suitable for embedding between double-quote delimiters.</returns>
    private string EscapeString(string value)
    {
        return value
            .Replace("~", "~~")
            .Replace("\"", "~\"")
            .Replace("\n", "~n")
            .Replace("\t", "~t");
    }

    /// <summary>
    /// Applies UtopIR <c>~</c>-based escape sequences to a single character value.
    /// </summary>
    /// <param name="value">The raw character value.</param>
    /// <returns>The escaped character, suitable for embedding between single-quote delimiters.</returns>
    private string EscapeChar(char value) => value switch
    {
        '~' => "~~",
        '\'' => "~'",
        '\n' => "~n",
        '\t' => "~t",
        _ => value.ToString()
    };

    /// <summary>
    /// Returns the UtopIR keyword for the given <see cref="UtopIRType"/>.
    /// </summary>
    /// <param name="type">The type to convert.</param>
    /// <returns>The UtopIR type keyword.</returns>
    private string TypeKeyword(UtopIRType type) => type switch
    {
        UtopIRType.Chancellor => UtopIRKeywords.TypeNames.Chancellor,
        UtopIRType.Peer => UtopIRKeywords.TypeNames.Peer,
        UtopIRType.Pirate => UtopIRKeywords.TypeNames.Pirate,
        UtopIRType.SausageRoll => UtopIRKeywords.TypeNames.SausageRoll,
        UtopIRType.StandingChancellor => UtopIRKeywords.TypeNames.StandingChancellor,
        UtopIRType.StandingPeer => UtopIRKeywords.TypeNames.StandingPeer,
        UtopIRType.StandingPirate => UtopIRKeywords.TypeNames.StandingPirate,
        UtopIRType.StandingSausageRoll => UtopIRKeywords.TypeNames.StandingSausageRoll,
        UtopIRType.Fathom => UtopIRKeywords.TypeNames.Fathom,
        UtopIRType.Foot => UtopIRKeywords.TypeNames.Foot,
        UtopIRType.Decree => UtopIRKeywords.TypeNames.Decree,
        UtopIRType.Stitch => UtopIRKeywords.TypeNames.Stitch,
        UtopIRType.Yarn => UtopIRKeywords.TypeNames.Yarn,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown UtopIR type.")
    };

    /// <summary>
    /// Returns the UtopIR mnemonic for the given <see cref="UtopIRArithmeticOperation"/>.
    /// </summary>
    /// <param name="operation">The arithmetic operation to convert.</param>
    /// <returns>The UtopIR operation mnemonic.</returns>
    private string OperationMnemonic(UtopIRArithmeticOperation operation) => operation switch
    {
        UtopIRArithmeticOperation.Sum => UtopIRKeywords.Instructions.Sum,
        UtopIRArithmeticOperation.Diff => UtopIRKeywords.Instructions.Diff,
        UtopIRArithmeticOperation.Prod => UtopIRKeywords.Instructions.Prod,
        UtopIRArithmeticOperation.Quot => UtopIRKeywords.Instructions.Quot,
        UtopIRArithmeticOperation.Rem => UtopIRKeywords.Instructions.Rem,
        UtopIRArithmeticOperation.Max => UtopIRKeywords.Instructions.Max,
        UtopIRArithmeticOperation.Min => UtopIRKeywords.Instructions.Min,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown arithmetic operation.")
    };
}
