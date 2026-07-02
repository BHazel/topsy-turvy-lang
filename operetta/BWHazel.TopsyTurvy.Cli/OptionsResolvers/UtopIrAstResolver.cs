using System.Collections.Generic;
using System.Text.Json;
using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Resolves and performs the <c>--emit utopir-ast</c> emit to write the UtopIR AST as JSON to STDOUT.
/// </summary>
/// <remarks>
/// ### Command-Line Configuration
/// _None_
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
        if (config.Count > 0)
        {
            PanelHelper.ReportUserWarning(tiptoe, $"--emit utopir-ast does not use any --config keys; ignoring: {string.Join(", ", config.Keys)}.");
        }

        return baseOptions;
    }

    /// <inheritdoc/>
    public int Emit(string filename, JsonEmitOptions options, bool tiptoe)
    {
        UtopIRProgram? utopIrProgram = ToolchainOperations.GetUtopIrProgram(filename, tiptoe);
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
