using System;
using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Analysis;
using BWHazel.TopsyTurvy.UtopIR.Transformer;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Resolves and performs the <c>--emit utopir</c> emit to transform Topsy Turvy source to UtopIR and writes the resulting UtopIR source text to STDOUT.
/// </summary>
/// <remarks>
/// ### Command-Line Configuration
/// _None_
/// 
/// ### Additional Configuration
/// _None_
/// </remarks>
public sealed class UtopIrResolver : IEmitterOptionsResolver<NoOptions>
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetAllowedExtensions() => [FileManager.TopsyTurvyFileExtension];

    /// <inheritdoc/>
    public NoOptions Apply(NoOptions baseOptions, IReadOnlyDictionary<string, string> config, bool tiptoe)
    {
        if (config.Count > 0)
        {
            PanelHelper.ReportUserWarning(tiptoe, $"--emit utopir does not use any --config keys; ignoring: {string.Join(", ", config.Keys)}.");
        }

        return baseOptions;
    }

    /// <inheritdoc/>
    public int Emit(string filename, NoOptions options, bool tiptoe)
    {
        ProgramNode? program = ToolchainOperations.ParseAndCheck(filename, tiptoe);
        if (program is null)
        {
            return 1;
        }

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer().Transform(program);
        Console.Write(new UtopIRCodeGenerator().Generate(utopIrProgram));

        return 0;
    }
}
