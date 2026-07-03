using System.Collections.Generic;
using System.Text.Json;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Resolves and performs the <c>--emit utopir-ast</c> emit to write the UtopIR AST as JSON to STDOUT.
/// </summary>
/// <remarks>
/// ### Command-Line Configuration
/// <c>varFormat</c>: Specifies how temporary variables are named during transformation and displayed in emitted CIL.
/// * <c>verbose</c>: Descriptive variable names comprising instruction and operand information; implemented by <see cref="InstructionDetailVariableFormatter"/>.
/// * <c>numeric</c> (default). Short incrementing integer variable names; implemented by <see cref="IncrementingIntVariableFormatter"/>.
/// 
/// Ignored for <c>.utopir</c> input, which has no transformation step.
/// 
/// ### Additional Configuration
/// _None_
/// </remarks>
public sealed class UtopIrAstResolver : IEmitterOptionsResolver<JsonEmitOptions>
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetAllowedExtensions() => [FileManager.TopsyTurvyFileExtension, FileManager.UtopirFileExtension];

    /// <inheritdoc/>
    public JsonEmitOptions Apply(JsonEmitOptions baseOptions, IReadOnlyDictionary<string, string> config, bool tiptoe)
    {
        VariableNameFormat format = VariableFormatConfig.Resolve(config, tiptoe, "--emit utopir-ast", baseOptions.Format);
        return baseOptions with
        {
            Format = format
        };
    }

    /// <inheritdoc/>
    public int Emit(string filename, JsonEmitOptions options, bool tiptoe)
    {
        VariableFormatConfig.WarnIfIgnoredForUtopIrInput(filename, options.Format, tiptoe);

        ITemporaryVariableNameFormatter formatter = VariableFormatConfig.CreateFormatter(options.Format);
        UtopIRProgram? utopIrProgram = ToolchainOperations.GetUtopIrProgram(filename, tiptoe, formatter);
        if (utopIrProgram is null)
        {
            return 1;
        }

        JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = !options.Abridged,
            Converters =
            {
                new UtopIRInstructionJsonConverter(),
                new UtopIROperandJsonConverter()
            }
        };

        ToolchainOperations.WriteJson(JsonSerializer.Serialize(utopIrProgram, jsonOptions), options.Chromatic);
        return 0;
    }
}
