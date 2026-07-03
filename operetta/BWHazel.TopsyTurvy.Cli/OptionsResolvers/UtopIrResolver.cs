using System;
using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Analysis;
using BWHazel.TopsyTurvy.UtopIR.Transformer;
using BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Resolves and performs the <c>--emit utopir</c> emit to transform Topsy Turvy source to UtopIR and writes the resulting UtopIR source text to STDOUT.
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
public sealed class UtopIrResolver : IEmitterOptionsResolver<VariableNameFormat>
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetAllowedExtensions() => [FileManager.TopsyTurvyFileExtension];

    /// <inheritdoc/>
    public VariableNameFormat Apply(VariableNameFormat baseOptions, IReadOnlyDictionary<string, string> config, bool tiptoe) =>
        VariableFormatConfig.Resolve(config, tiptoe, "--emit utopir", baseOptions);

    /// <inheritdoc/>
    public int Emit(string filename, VariableNameFormat options, bool tiptoe)
    {
        ProgramNode? program = ToolchainOperations.ParseAndCheck(filename, tiptoe);
        if (program is null)
        {
            return 1;
        }

        ITemporaryVariableNameFormatter formatter = VariableFormatConfig.CreateFormatter(options);
        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer(formatter).Transform(program);
        Console.Write(new UtopIRCodeGenerator().Generate(utopIrProgram));

        return 0;
    }
}
