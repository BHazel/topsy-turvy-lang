using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Json;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Analysis;
using BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;
using BWHazel.TopsyTurvy.UtopIR.Transformer;

using TopsyParseResult = BWHazel.TopsyTurvy.Parser.ParseResult;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>sorcerer</c> command.
/// </summary>
public static class SorcererCommandBuilder
{
    /// <summary>
    /// The UtopIR source file extension.
    /// </summary>
    private const string UtopIrFileExtension = ".utopir";

    /// <summary>
    /// Builds and configures the <c>sorcerer</c> command.
    /// </summary>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command sorcererCommand = new("sorcerer", "Compiles a Topsy Turvy programme and emits numerous intermediate formats.");
        sorcererCommand.Aliases.Add("compile");

        Argument<string> fileArgument = new("filename")
        {
            Description = "The Topsy Turvy or UtopIR file to compile."
        };

        Option<string?> emitOption = new("--emit")
        {
            Description = "Emits a plain-text intermediate format to STDOUT instead of building a target:\n" +
                "- Preprocessed Topsy Turvy Cue (.topsy): preprocess, p, cue, c.\n" +
                "- AST Promptbook (.topsy): ast, a, promptbook, b.\n" +
                "- UtopIR (.topsy): utopir, u.\n" +
                "- UtopIR AST Promptbook (.topsy, .utopir): utopir-ast, s.\n" +
                "- .NET CIL (.topsy, .utopir): dotnet-cil, d.\n" +
                "Takes priority over --target when both are provided."
        };

        emitOption.Aliases.Add("-e");

        Option<string> targetOption = new("--target")
        {
            Description = "The compilation target to build:\n" +
                "- .NET (.topsy, .utopir): dotnet. (default)"
        };

        targetOption.Aliases.Add("-t");
        targetOption.DefaultValueFactory = _ => "dotnet";

        Option<string?> outputOption = new("--output")
        {
            Description = "The output path for a built target.  Defaults to the input filename with its extension changed to .dll."
        };

        outputOption.Aliases.Add("-o");

        Option<bool> abridgedOption = new("--abridged")
        {
            Description = "Removes all whitespace from JSON output (ast, utopir-ast)."
        };

        abridgedOption.Aliases.Add("-a");

        Option<bool> chromaticOption = new("--chromatic")
        {
            Description = "Syntax highlights JSON output (ast, utopir-ast)."
        };

        chromaticOption.Aliases.Add("-c");

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "Only prints program output and thus chooses to discard aestheticism."
        };

        sorcererCommand.Arguments.Add(fileArgument);
        sorcererCommand.Options.Add(emitOption);
        sorcererCommand.Options.Add(targetOption);
        sorcererCommand.Options.Add(outputOption);
        sorcererCommand.Options.Add(abridgedOption);
        sorcererCommand.Options.Add(chromaticOption);
        sorcererCommand.Options.Add(tiptoeOption);

        sorcererCommand.SetAction(parseResult =>
            HandleSorcerer(
                parseResult.GetValue(fileArgument) ?? string.Empty,
                parseResult.GetValue(emitOption),
                parseResult.GetValue(targetOption) ?? "dotnet",
                parseResult.GetValue(outputOption),
                parseResult.GetValue(abridgedOption),
                parseResult.GetValue(chromaticOption),
                parseResult.GetValue(tiptoeOption)));

        return sorcererCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>sorcerer</c> command, dispatching to the requested
    /// <c>--emit</c> format or <c>--target</c>.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="emit">The requested emit format, or <c>null</c> if not given.</param>
    /// <param name="target">The requested target.</param>
    /// <param name="output">The requested output path for a built target, or <c>null</c> for the default.</param>
    /// <param name="abridged">A value indicating whether to compact JSON output.</param>
    /// <param name="chromatic">A value indicating whether to syntax-highlight JSON output.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandleSorcerer(string filename, string? emit, string target, string? output, bool abridged, bool chromatic, bool tiptoe)
    {
        if (!string.IsNullOrEmpty(emit))
        {
            return HandleEmit(filename, emit, abridged, chromatic, tiptoe);
        }

        return HandleTarget(filename, target, output, tiptoe);
    }

    /// <summary>
    /// Handles the <c>--emit</c> pathway, dispatching to the requested format.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="emit">The requested emit format.</param>
    /// <param name="abridged">A value indicating whether to compact JSON output.</param>
    /// <param name="chromatic">A value indicating whether to syntax-highlight JSON output.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandleEmit(string filename, string emit, bool abridged, bool chromatic, bool tiptoe)
    {
        return emit.ToLowerInvariant() switch
        {
            "preprocess" or "p" or "cue" or "c" =>
                WithValidExtension(filename, tiptoe, "--emit preprocess", [FileManager.FileExtension],
                    () => EmitPreprocess(filename, tiptoe)),
            "ast" or "a" or "promptbook" or "b" =>
                WithValidExtension(filename, tiptoe, "--emit ast", [FileManager.FileExtension],
                    () => EmitAst(filename, abridged, chromatic, tiptoe)),
            "utopir" or "u" =>
                WithValidExtension(filename, tiptoe, "--emit utopir", [FileManager.FileExtension],
                    () => EmitUtopIr(filename, tiptoe)),
            "utopir-ast" or "s" =>
                WithValidExtensionAllowingUtopIr(filename, tiptoe, "--emit utopir-ast",
                    () => EmitUtopIrAst(filename, abridged, chromatic, tiptoe)),
            "dotnet-cil" or "d" =>
                WithValidExtensionAllowingUtopIr(filename, tiptoe, "--emit dotnet-cil",
                    () => EmitDotNetCil(filename, tiptoe)),
            _ => ReportUserError(tiptoe, $"Unknown --emit format: '{emit}'.")
        };
    }

    /// <summary>
    /// Handles the <c>--target</c> pathway, dispatching to the requested target.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="target">The requested target.</param>
    /// <param name="output">The requested output path, or <c>null</c> for the default.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandleTarget(string filename, string target, string? output, bool tiptoe)
    {
        return target.ToLowerInvariant() switch
        {
            "dotnet" =>
                WithValidExtensionAllowingUtopIr(filename, tiptoe, "--target dotnet",
                    () => TargetDotNet(filename, output, tiptoe)),
            _ => ReportUserError(tiptoe, $"Unknown --target: '{target}'.")
        };
    }

    /// <summary>
    /// Validates that <paramref name="filename"/> has one of the allowed extensions
    /// before invoking the specified action.
    /// </summary>
    /// <param name="filename">The filename to validate.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <param name="modeDescription">A human-readable description of the requested mode, for the error message.</param>
    /// <param name="allowedExtensions">The extensions accepted by the requested mode.</param>
    /// <param name="action">The action to invoke when the extension is valid.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int WithValidExtension(string filename, bool tiptoe, string modeDescription, string[] allowedExtensions, Func<int> action)
    {
        foreach (string extension in allowedExtensions)
        {
            if (filename.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                return action();
            }
        }

        string extensionList = string.Join(" or ", allowedExtensions);
        return ReportUserError(tiptoe, $"Invalid file extension: '{filename}'. Only {extensionList} files are supported for {modeDescription}.");
    }

    /// <summary>
    /// Validates that the filename has either the Topsy Turvy or UtopIR source
    /// extension, then shows a "not yet supported" panel for UtopIR source files (no UtopIR
    /// source parser exists yet) before invoking the specified action for Topsy Turvy files.
    /// </summary>
    /// <param name="filename">The filename to validate.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <param name="modeDescription">A human-readable description of the requested mode, for the error message.</param>
    /// <param name="action">The action to invoke for a Topsy Turvy source file.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int WithValidExtensionAllowingUtopIr(string filename, bool tiptoe, string modeDescription, Func<int> action)
    {
        if (filename.EndsWith(UtopIrFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            return ReportUserError(tiptoe, "UtopIR source files are not yet supported: no UtopIR source parser has been implemented yet.");
        }

        return WithValidExtension(filename, tiptoe, modeDescription, [FileManager.FileExtension], action);
    }

    /// <summary>
    /// Reports a user error via a panel or, in <c>--tiptoe</c> mode, plain text to STDERR.
    /// </summary>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <param name="message">The error message.</param>
    /// <returns><c>1</c>, the failure exit code.</returns>
    private static int ReportUserError(bool tiptoe, string message)
    {
        if (tiptoe)
        {
            Console.Error.WriteLine(message);
        }
        else
        {
            PanelHelper.WriteUserError(message);
        }

        return 1;
    }

    /// <summary>
    /// Parses and type-checks a Topsy Turvy source file, reporting any errors encountered.
    /// </summary>
    /// <param name="filename">The filename to parse.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>The parsed programme, or <c>null</c> if parsing or type-checking failed as errors are already reported.</returns>
    private static ProgramNode? ParseAndCheck(string filename, bool tiptoe)
    {
        (ProgramExecutionResult result, TopsyParseResult? parseData) = ProgramRunner.ParseFile(filename);
        if (!result.IsSuccess)
        {
            PanelHelper.ReportErrors(result, tiptoe);
            return null;
        }

        return parseData!.Program;
    }

    /// <summary>
    /// Emits the preprocessed Topsy Turvy source text to STDOUT.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int EmitPreprocess(string filename, bool tiptoe)
    {
        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Conjuring...", $"[cyan]{filename}[/]");
        }

        (bool success, string? errorMessage) = FileManager.TryReadSource(filename, out string source);
        if (!success)
        {
            PanelHelper.ReportErrors(ProgramExecutionResult.Failure(errorMessage!), tiptoe);
            return 1;
        }

        PreProcessorPipeline pipeline = new();
        pipeline.AddProcessor(new CommentsPreProcessor());
        pipeline.AddProcessor(new VictorianFlourishPreProcessor());
        PreProcessResult result = pipeline.Execute(source);
        Console.Write(result.TransformedText);

        return 0;
    }

    /// <summary>
    /// Emits the Topsy Turvy AST as JSON to STDOUT.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="abridged">A value indicating whether to compact JSON output.</param>
    /// <param name="chromatic">A value indicating whether to syntax-highlight JSON output.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int EmitAst(string filename, bool abridged, bool chromatic, bool tiptoe)
    {
        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Conjuring...", $"[cyan]{filename}[/]");
        }

        ProgramNode? program = ParseAndCheck(filename, tiptoe);
        if (program is null)
        {
            return 1;
        }

        JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = !abridged,
            Converters =
            {
                new NodeJsonConverter()
            }
        };

        WriteJson(JsonSerializer.Serialize(program, jsonOptions), chromatic);
        return 0;
    }

    /// <summary>
    /// Emits the UtopIR source text (transformed from the Topsy Turvy source) to STDOUT.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int EmitUtopIr(string filename, bool tiptoe)
    {
        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Conjuring...", $"[cyan]{filename}[/]");
        }

        ProgramNode? program = ParseAndCheck(filename, tiptoe);
        if (program is null)
        {
            return 1;
        }

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer().Transform(program);
        Console.Write(new UtopIRCodeGenerator().Generate(utopIrProgram));

        return 0;
    }

    /// <summary>
    /// Emits the UtopIR AST as JSON to STDOUT.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="abridged">A value indicating whether to compact JSON output.</param>
    /// <param name="chromatic">A value indicating whether to syntax-highlight JSON output.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int EmitUtopIrAst(string filename, bool abridged, bool chromatic, bool tiptoe)
    {
        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Conjuring...", $"[cyan]{filename}[/]");
        }

        ProgramNode? program = ParseAndCheck(filename, tiptoe);
        if (program is null)
        {
            return 1;
        }

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer().Transform(program);

        JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = !abridged,
            Converters =
            {
                new UtopIRInstructionJsonConverter(),
                new UtopIROperandJsonConverter()
            }
        };

        WriteJson(JsonSerializer.Serialize(utopIrProgram, jsonOptions), chromatic);
        return 0;
    }

    /// <summary>
    /// Emits the .NET CIL disassembly text to STDOUT.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int EmitDotNetCil(string filename, bool tiptoe)
    {
        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Conjuring...", $"[cyan]{filename}[/]");
        }

        ProgramNode? program = ParseAndCheck(filename, tiptoe);
        if (program is null)
        {
            return 1;
        }

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer().Transform(program);
        string assemblyName = Path.GetFileNameWithoutExtension(filename);
        CilEmitResult emitResult = new CilEmitter().Emit(
            utopIrProgram,
            new CilEmitOptions(assemblyName, string.Empty, CilOutputKind.IlSourceOnly));

        Console.WriteLine(emitResult.IlSource);
        return 0;
    }

    /// <summary>
    /// Compiles the Topsy Turvy programme to a runnable .NET executable assembly.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="output">The output filename for the compiled assembly.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int TargetDotNet(string filename, string? output, bool tiptoe)
    {
        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Conjuring...", $"[cyan]{filename}[/]");
        }

        ProgramNode? program = ParseAndCheck(filename, tiptoe);
        if (program is null)
        {
            return 1;
        }

        string outputPath = output ?? Path.ChangeExtension(filename, ".dll");
        string assemblyName = Path.GetFileNameWithoutExtension(outputPath);

        UtopIRProgram utopIrProgram = new TopsyTurvyToUtopIRTransformer().Transform(program);
        new CilEmitter().Emit(
            utopIrProgram,
            new CilEmitOptions(assemblyName, outputPath, CilOutputKind.Executable));

        if (!tiptoe)
        {
            PanelHelper.WriteSuccess("Conjured!", $"[cyan]{outputPath}[/]\nRun it with [lightgreen_1]dotnet {outputPath}[/]");
        }

        return 0;
    }

    /// <summary>
    /// Writes JSON text to STDOUT, syntax-highlighted via Spectre.Console when requested.
    /// </summary>
    /// <param name="json">The JSON text to write.</param>
    /// <param name="chromatic">A value indicating whether to syntax-highlight the output.</param>
    private static void WriteJson(string json, bool chromatic)
    {
        if (chromatic)
        {
            AnsiConsole.Write(new JsonText(json));
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine(json);
        }
    }
}
