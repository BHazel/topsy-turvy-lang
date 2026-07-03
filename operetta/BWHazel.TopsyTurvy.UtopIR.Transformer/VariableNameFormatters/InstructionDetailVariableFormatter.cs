using System.Collections.Generic;
using System.Linq;

namespace BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

/// <summary>
/// Names temporary virtual registers with descriptive names derived from the instruction and operand details.
/// 
/// 
/// Builds descriptive temporary variable names from the instruction mnemonic/keyword and operand
/// labels, e.g. <c>_sum_a_b</c>, <c>_were_a_chancellor</c> — good for debugging, since the name
/// itself says what the register holds.
/// </summary>
/// <remarks>
/// <para>
/// Variable names are descriptive and verbose allowing for easier navigation when debugging transformed UtopIR code.  They
/// take the general form of <c>_{instruction}_{operand1}_{operand2}_...</c>, where each part is derived from the instruction
/// and operand details.  It does mean the names can become very long which can reduce readability.
/// </para>
/// <para>
/// Each part is sanitised to ensure it is a valid UtopIR identifier and all parts are joined using <c>_</c>.  Characters that
/// are not letters, digits, <c>-</c>, or <c>_</c> are replaced with <c>-</c> and decimal points in floating-point numbersare
/// replaced with <c>p</c>.
/// </para>
/// <para>
/// Using instruction and operand details directly could cause name collisions, therefore, deduplication is performed by
/// appending a numeric suffix to a name until it is unique within a transformation.
/// </para>
/// <para>
/// Example names produced by this formatter are:
/// * <c>_sum_a_b</c>: Corresponds to the <c>sum a, b</c> instruction with operands <c>a</c> and <c>b</c>.
/// * <c>_were_a_chancellor</c>: Corresponds to the <c>were a chancellor</c> instruction with operands <c>a</c> and <c>chancellor</c>.
/// * <c>_sum_a_b_2</c>: Corresponds to a second <c>sum a, b</c> instruction with operands <c>a</c> and <c>b</c>, which would otherwise have produced the same name as the first example.
/// * <c>_sum_a_3p14</c>: Corresponds to the <c>sum a, 3.14</c> instruction with operands <c>a</c> and <c>3.14</c>, where the decimal point is replaced with <c>p</c>.
/// </para>
/// </remarks>
public sealed class InstructionDetailVariableFormatter : ITemporaryVariableNameFormatter
{
    private readonly HashSet<string> usedNames = [];

    /// <inheritdoc/>
    public string CreateName(params string[] parts)
    {
        string baseName = "_" + string.Join("_", parts.Select(Sanitise));

        string name = baseName;
        int deduplicationSuffix = 2;
        while (!this.usedNames.Add(name))
        {
            name = $"{baseName}_{deduplicationSuffix}";
            deduplicationSuffix++;
        }

        return name;
    }

    /// <summary>
    /// Replaces any character in <paramref name="part"/> that would make the composed name an
    /// invalid identifier.
    /// </summary>
    /// <param name="part">The raw part to sanitise.</param>
    /// <remarks>
    /// Decimal points are replaced with <c>p</c> and non-supported characters are replaced with <c>-</c>.
    /// </remarks>
    /// <returns>The sanitised part.</returns>
    private static string Sanitise(string part)
    {
        char[] characters = part.ToCharArray();
        for (int i = 0; i < characters.Length; i++)
        {
            char character = characters[i];
            if (character == '.')
            {
                characters[i] = 'p';
            }
            else if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
            {
                characters[i] = '-';
            }
        }

        return new string(characters);
    }
}
