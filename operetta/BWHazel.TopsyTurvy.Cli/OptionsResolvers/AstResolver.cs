using System.Collections.Generic;
using System.Text.Json;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Resolves and executes the <c>--emit ast</c> emit to write the Topsy Turvy AST as JSON to STDOUT.
/// </summary>
/// <remarks>
/// ### Command-Line Configuration
/// _None_
/// 
/// ### Additional Configuration
/// A <see cref="JsonEmitOptions"/> instance is populated directly from the dedicated <c>--abridged</c> and <c>--chromatic</c> flags.
/// </remarks>
public sealed class AstResolver : IEmitterOptionsResolver<JsonEmitOptions>
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetAllowedExtensions() => [FileManager.TopsyTurvyFileExtension];

    /// <inheritdoc/>
    public JsonEmitOptions Apply(JsonEmitOptions baseOptions, IReadOnlyDictionary<string, string> config, bool tiptoe)
    {
        if (config.Count > 0)
        {
            PanelHelper.ReportUserWarning(tiptoe, $"--emit ast does not use any --config keys; ignoring: {string.Join(", ", config.Keys)}.");
        }

        return baseOptions;
    }

    /// <inheritdoc/>
    public int Emit(string filename, JsonEmitOptions options, bool tiptoe)
    {
        ProgramNode? program = ToolchainOperations.ParseAndCheck(filename, tiptoe);
        if (program is null)
        {
            return 1;
        }

        JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = !options.Abridged,
            Converters =
            {
                new NodeJsonConverter()
            }
        };

        ToolchainOperations.WriteJson(JsonSerializer.Serialize(program, jsonOptions), options.Chromatic);
        return 0;
    }
}
