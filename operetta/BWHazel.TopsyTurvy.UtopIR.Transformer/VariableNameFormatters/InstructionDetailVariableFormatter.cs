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
/// take the general form of <c>_{mnemonic}_{operand1}_{operand2}_...</c>, where each part is derived from the instruction
/// mnemonic and operand details.  It does mean the names can become very long which can reduce readability.
/// </para>
/// <para>
/// Each part is sanitised to ensure it is a valid UtopIR identifier and all parts are joined using <c>_</c>.  Characters that
/// are not letters, digits, <c>-</c>, or <c>_</c> are replaced with <c>-</c>.  The mnemonic and the operand parts are
/// sanitised differently where <c>.</c> is concerned: a <c>.</c> in the mnemonic is dropped entirely, since it is punctuation
/// belonging to the instruction name rather than a value.  A <c>.</c> in an operand part is replaced with <c>p</c> instead,
/// since dropping it would silently change the value the name represents.
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
/// * <c>_sum_a_3p14</c>: Corresponds to the <c>sum a, 3.14</c> instruction with operands <c>a</c> and <c>3.14</c>, where the decimal point in the operand is replaced with <c>p</c>.
/// * <c>_sumf_a_b</c>: Corresponds to the <c>sum.f a, b</c> instruction, where the <c>.</c> in the mnemonic is dropped rather than replaced with <c>p</c>.
/// </para>
/// </remarks>
public sealed class InstructionDetailVariableFormatter : ITemporaryVariableNameFormatter
{
    private readonly HashSet<string> usedNames = [];

    /// <inheritdoc/>
    public string CreateName(string mnemonic, params string[] operandParts)
    {
        IEnumerable<string> sanitisedParts = [SanitiseMnemonic(mnemonic), .. operandParts.Select(SanitiseOperand)];
        string baseName = "_" + string.Join("_", sanitisedParts);

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
    /// Sanitises an instruction mnemonic for use in a composed name.
    /// </summary>
    /// <param name="mnemonic">The raw mnemonic to sanitise.</param>
    /// <returns>The sanitised mnemonic.</returns>
    private static string SanitiseMnemonic(string mnemonic) =>
        ReplaceInvalidCharacters(mnemonic.Replace(".", string.Empty));

    /// <summary>
    /// Sanitises an operand part for use in a composed name.
    /// </summary>
    /// <param name="part">The raw operand part to sanitise.</param>
    /// <returns>The sanitised operand part.</returns>
    private static string SanitiseOperand(string part) =>
        ReplaceInvalidCharacters(part.Replace(".", "p"));

    /// <summary>
    /// Replaces any character in <paramref name="part"/> that would make the composed name an
    /// invalid identifier with <c>-</c>.
    /// </summary>
    /// <param name="part">The part to sanitise, with any <c>.</c> handling already applied by the caller.</param>
    /// <returns>The sanitised part.</returns>
    private static string ReplaceInvalidCharacters(string part)
    {
        char[] characters = part.ToCharArray();
        for (int i = 0; i < characters.Length; i++)
        {
            char character = characters[i];
            if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
            {
                characters[i] = '-';
            }
        }

        return new string(characters);
    }
}
