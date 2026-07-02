using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using BWHazel.TopsyTurvy.Cli.OptionsResolvers;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>sorcerer</c> command.
/// </summary>
public static class SorcererCommandBuilder
{
    private const string CompilingTitle = "Pouring...";

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
        emitOption.Aliases.Add("--pour");
        emitOption.Aliases.Add("-p");

        Option<string> targetOption = new("--target")
        {
            Description = "The compilation target to build:\n" +
                "- .NET (.topsy, .utopir): dotnet. (default)"
        };

        targetOption.Aliases.Add("-t");
        targetOption.Aliases.Add("--philtre");
        targetOption.Aliases.Add("-f");
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

        Option<string[]> configOption = new("--config")
        {
            Description = "A key:value pair passed to the emitter/target to configure its behaviour, " +
                "e.g. --config optimise:true.  May be given multiple times."
        };

        configOption.Aliases.Add("-g");
        configOption.Aliases.Add("--tablet");
        configOption.Aliases.Add("-l");

        sorcererCommand.Arguments.Add(fileArgument);
        sorcererCommand.Options.Add(emitOption);
        sorcererCommand.Options.Add(targetOption);
        sorcererCommand.Options.Add(outputOption);
        sorcererCommand.Options.Add(abridgedOption);
        sorcererCommand.Options.Add(chromaticOption);
        sorcererCommand.Options.Add(tiptoeOption);
        sorcererCommand.Options.Add(configOption);

        sorcererCommand.SetAction(parseResult =>
            HandleSorcerer(
                parseResult.GetValue(fileArgument) ?? string.Empty,
                parseResult.GetValue(emitOption),
                parseResult.GetValue(targetOption) ?? "dotnet",
                parseResult.GetValue(outputOption),
                parseResult.GetValue(abridgedOption),
                parseResult.GetValue(chromaticOption),
                parseResult.GetValue(configOption) ?? [],
                parseResult.GetValue(tiptoeOption)));

        return sorcererCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>sorcerer</c> command.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="emit">The requested emit format, or <c>null</c> if not given.</param>
    /// <param name="target">The requested target.</param>
    /// <param name="output">The requested output path for a built target, or <c>null</c> for the default.</param>
    /// <param name="abridged">A value indicating whether to compact JSON output.</param>
    /// <param name="chromatic">A value indicating whether to syntax-highlight JSON output.</param>
    /// <param name="configEntries">The raw <c>key:value</c> configuration entries, if any.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <remarks>
    /// Checks the file exists, parses <c>--config</c>, then dispatches to the requested <c>--emit</c> or <c>--target</c> format.
    /// </remarks>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandleSorcerer(string filename, string? emit, string target, string? output, bool abridged, bool chromatic, string[] configEntries, bool tiptoe)
    {
        if (!File.Exists(filename))
        {
            return PanelHelper.ReportUserError(tiptoe, $"File not found: {filename}");
        }

        Dictionary<string, string>? config = ParseConfig(configEntries, tiptoe);
        if (config is null)
        {
            return 1;
        }

        if (!string.IsNullOrEmpty(emit))
        {
            return HandleEmit(filename, emit, abridged, chromatic, config, tiptoe);
        }

        return HandleTarget(filename, target, output, config, tiptoe);
    }

    /// <summary>
    /// Handles the <c>--emit</c> pathway.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="emit">The requested emit format.</param>
    /// <param name="abridged">A value indicating whether to compact JSON output.</param>
    /// <param name="chromatic">A value indicating whether to syntax-highlight JSON output.</param>
    /// <param name="config">The parsed configuration values.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandleEmit(string filename, string emit, bool abridged, bool chromatic, IReadOnlyDictionary<string, string> config, bool tiptoe) =>
        emit.ToLowerInvariant() switch
        {
            "preprocess" or "p" or "cue" or "c" =>
                RunResolver(new PreprocessResolver(), filename, NoOptions.Default, config, tiptoe),
            "ast" or "a" or "promptbook" or "b" =>
                RunResolver(new AstResolver(), filename, new JsonEmitOptions(abridged, chromatic), config, tiptoe),
            "utopir" or "u" =>
                RunResolver(new UtopIrResolver(), filename, NoOptions.Default, config, tiptoe),
            "utopir-ast" or "s" =>
                RunResolver(new UtopIrAstResolver(), filename, new JsonEmitOptions(abridged, chromatic), config, tiptoe),
            "dotnet-cil" or "d" =>
                RunResolver(new DotNetCilOptionsResolver(), filename, DotNetCilOptionsResolver.BuildEmitOptions(filename), config, tiptoe),
            _ => PanelHelper.ReportUserError(tiptoe, $"Unknown --emit format: '{emit}'.")
        };

    /// <summary>
    /// Handles the <c>--target</c> pathway.
    /// </summary>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="target">The requested target.</param>
    /// <param name="output">The requested output path, or <c>null</c> for the default.</param>
    /// <param name="config">The parsed configuration values.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandleTarget(string filename, string target, string? output, IReadOnlyDictionary<string, string> config, bool tiptoe) =>
        target.ToLowerInvariant() switch
        {
            "dotnet" =>
                RunResolver(new DotNetCilOptionsResolver(), filename, DotNetCilOptionsResolver.BuildTargetOptions(filename, output), config, tiptoe),
            _ => PanelHelper.ReportUserError(tiptoe, $"Unknown --target: '{target}'.")
        };

    /// <summary>
    /// Runs the given emit or target resolver.
    /// </summary>
    /// <typeparam name="TOptions">The resolver options type.</typeparam>
    /// <param name="resolver">The resolver to run.</param>
    /// <param name="filename">The filename of the file to compile.</param>
    /// <param name="baseOptions">The options already determined from CLI arguments, before any configuration values are applied.</param>
    /// <param name="config">The parsed configuration values.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int RunResolver<TOptions>(IEmitterOptionsResolver<TOptions> resolver, string filename, TOptions baseOptions, IReadOnlyDictionary<string, string> config, bool tiptoe)
    {
        if (!resolver.CanEmit(filename))
        {
            string extensionList = string.Join(" or ", resolver.GetAllowedExtensions());
            return PanelHelper.ReportUserError(tiptoe, $"Invalid file extension: '{filename}'. Only {extensionList} files are supported.");
        }

        if (!tiptoe)
        {
            PanelHelper.WriteDefault(CompilingTitle, $"[cyan]{filename}[/]");
        }

        try
        {
            TOptions options = resolver.Apply(baseOptions, config, tiptoe);
            return resolver.Emit(filename, options, tiptoe);
        }
        catch (ToolchainConfigException exception)
        {
            return PanelHelper.ReportUserError(tiptoe, exception.Message);
        }
    }

    /// <summary>
    /// Parses configuration entries into a dictionary, reporting a user error for any entry that is not in the <c>key:value</c> form.
    /// </summary>
    /// <param name="rawEntries">The raw <c>key:value</c> entries.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>The parsed configuration, or <c>null</c> if a malformed entry was reported.</returns>
    private static Dictionary<string, string>? ParseConfig(string[] rawEntries, bool tiptoe)
    {
        Dictionary<string, string> config = [];
        foreach (string entry in rawEntries)
        {
            int separatorIndex = entry.IndexOf(':');
            if (separatorIndex <= 0)
            {
                PanelHelper.ReportUserError(tiptoe, $"Invalid --config entry: '{entry}'. Expected the form key:value.");
                return null;
            }

            config[entry[..separatorIndex]] = entry[(separatorIndex + 1)..];
        }

        return config;
    }
}
