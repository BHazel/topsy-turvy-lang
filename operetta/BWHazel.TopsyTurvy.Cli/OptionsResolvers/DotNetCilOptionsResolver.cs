using System;
using System.Collections.Generic;
using System.IO;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;
using BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Resolves and executes:
/// * <c>--emit dotnet-cil</c> emit format to write .NET CIL to STDOUT.
/// * <c>--target dotnet</c> target to compile to a .NET assembly at the requested output path.
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
/// A <see cref="CilEmitOptions"/> instance is populated directly from the dedicated <c>--output</c> flag.
/// </remarks>
public sealed class DotNetCilOptionsResolver : IEmitterOptionsResolver<CilTargetOptions>
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetAllowedExtensions() => [FileManager.TopsyTurvyFileExtension, FileManager.UtopirFileExtension];

    /// <inheritdoc/>
    public CilTargetOptions Apply(CilTargetOptions baseOptions, IReadOnlyDictionary<string, string> config, bool tiptoe)
    {
        VariableNameFormat format = VariableFormatConfig.Resolve(config, tiptoe, "The .NET CIL emitter/target", baseOptions.Format);
        return baseOptions with
        {
            Format = format
        };
    }

    /// <inheritdoc/>
    public int Emit(string filename, CilTargetOptions options, bool tiptoe)
    {
        VariableFormatConfig.WarnIfIgnoredForUtopIrInput(filename, options.Format, tiptoe);

        ITemporaryVariableNameFormatter formatter = VariableFormatConfig.CreateFormatter(options.Format);
        UtopIRProgram? program = ToolchainOperations.GetUtopIrProgram(filename, tiptoe, formatter);
        if (program is null)
        {
            return 1;
        }

        CilEmitOptions emitOptions = options.EmitOptions;
        CilEmitResult emitResult = new CilEmitter().Emit(program, emitOptions);
        switch (emitOptions.OutputKind)
        {
            case CilOutputKind.IlSourceOnly:
                Console.WriteLine(emitResult.IlSource);
                break;
            case CilOutputKind.Executable:
                if (!tiptoe)
                {
                    PanelHelper.WriteSuccess(
                        "Poured!",
                        $"Run it with [lightgreen_1]dotnet {emitOptions.OutputPath}[/]");
                }

                break;
            case CilOutputKind.Library:
                break;
        }

        return 0;
    }

    /// <summary>
    /// Builds the base <see cref="CilTargetOptions"/> for <c>--emit dotnet-cil</c>, before any <c>--config</c> values are applied.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <returns>The base options.</returns>
    public static CilTargetOptions BuildEmitOptions(string filename)
    {
        string assemblyName = Path.GetFileNameWithoutExtension(filename);
        return new CilTargetOptions(new CilEmitOptions(assemblyName, string.Empty, CilOutputKind.IlSourceOnly));
    }

    /// <summary>
    /// Builds the base <see cref="CilTargetOptions"/> for <c>--target dotnet</c>, before any <c>--config</c> values are applied.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="output">The requested output path, or <c>null</c> for the default.</param>
    /// <returns>The base options.</returns>
    public static CilTargetOptions BuildTargetOptions(string filename, string? output)
    {
        string outputPath = output ?? Path.ChangeExtension(filename, ".dll");
        string assemblyName = Path.GetFileNameWithoutExtension(outputPath);
        return new CilTargetOptions(new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Executable));
    }
}
