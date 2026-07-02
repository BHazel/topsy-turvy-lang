using System;
using System.Collections.Generic;
using System.IO;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Resolves and executes:
/// * <c>--emit dotnet-cil</c> emit format to write .NET CIL to STDOUT.
/// * <c>--target dotnet</c> target to compile to a .NET assembly at the requested output path.
/// </summary>
/// <remarks>
/// ### Command-Line Configuration
/// _None_
/// 
/// ### Additional Configuration
/// A <see cref="CilEmitOptions"/> instance is populated directly from the dedicated <c>--output</c> flag.
/// </remarks>
public sealed class DotNetCilOptionsResolver : IEmitterOptionsResolver<CilEmitOptions>
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetAllowedExtensions() => [FileManager.TopsyTurvyFileExtension, FileManager.UtopirFileExtension];

    /// <inheritdoc/>
    public CilEmitOptions Apply(CilEmitOptions baseOptions, IReadOnlyDictionary<string, string> config, bool tiptoe)
    {
        if (config.Count > 0)
        {
            string keys = string.Join(", ", config.Keys);
            PanelHelper.ReportUserWarning(tiptoe, $"The .NET CIL emitter/target does not use any --config keys yet; ignoring: {keys}.");
        }

        return baseOptions;
    }

    /// <inheritdoc/>
    public int Emit(string filename, CilEmitOptions options, bool tiptoe)
    {
        UtopIRProgram? program = ToolchainOperations.GetUtopIrProgram(filename, tiptoe);
        if (program is null)
        {
            return 1;
        }

        CilEmitResult emitResult = new CilEmitter().Emit(program, options);
        switch (options.OutputKind)
        {
            case CilOutputKind.IlSourceOnly:
                Console.WriteLine(emitResult.IlSource);
                break;
            case CilOutputKind.Executable:
                if (!tiptoe)
                {
                    PanelHelper.WriteSuccess(
                        "Poured!",
                        $"[cyan]{options.OutputPath}[/]\nRun it with [lightgreen_1]dotnet {options.OutputPath}[/]");
                }

                break;
            case CilOutputKind.Library:
                break;
        }

        return 0;
    }

    /// <summary>
    /// Builds the base <see cref="CilEmitOptions"/> for <c>--emit dotnet-cil</c>, before any <c>--config</c> values are applied.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <returns>The base options.</returns>
    public static CilEmitOptions BuildEmitOptions(string filename)
    {
        string assemblyName = Path.GetFileNameWithoutExtension(filename);
        return new CilEmitOptions(assemblyName, string.Empty, CilOutputKind.IlSourceOnly);
    }

    /// <summary>
    /// Builds the base <see cref="CilEmitOptions"/> for <c>--target dotnet</c>, before any <c>--config</c> values are applied.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="output">The requested output path, or <c>null</c> for the default.</param>
    /// <returns>The base options.</returns>
    public static CilEmitOptions BuildTargetOptions(string filename, string? output)
    {
        string outputPath = output ?? Path.ChangeExtension(filename, ".dll");
        string assemblyName = Path.GetFileNameWithoutExtension(outputPath);
        return new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Executable);
    }
}
