using System;
using System.Collections.Generic;
using BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Resolves the configuration <c>varFormat</c> key to control how temporary variables are named during transformation.
/// </summary>
public static class VariableFormatConfig
{
    private const string Key = "varFormat";

    /// <summary>
    /// Resolves the <paramref name="config"/> <c>varFormat</c> value.
    /// </summary>
    /// <param name="config">The parsed <c>--config</c> values.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <param name="modeDescription">A human-readable description of the requested mode.</param>
    /// <param name="defaultFormat">The format to use when <c>varFormat</c> is not given.</param>
    /// <returns>The resolved <see cref="VariableNameFormat"/>.</returns>
    /// <exception cref="ToolchainConfigException">Thrown when <c>varFormat</c> is given an unsupported value.</exception>
    public static VariableNameFormat Resolve(IReadOnlyDictionary<string, string> config, bool tiptoe, string modeDescription, VariableNameFormat defaultFormat = VariableNameFormat.Numeric)
    {
        VariableNameFormat format = defaultFormat;
        List<string> unrecognizedKeys = [];

        foreach (KeyValuePair<string, string> entry in config)
        {
            if (!string.Equals(entry.Key, Key, StringComparison.OrdinalIgnoreCase))
            {
                unrecognizedKeys.Add(entry.Key);
                continue;
            }

            format = entry.Value.ToLowerInvariant() switch
            {
                "numeric" => VariableNameFormat.Numeric,
                "verbose" => VariableNameFormat.Verbose,
                _ => throw new ToolchainConfigException($"Invalid value '{entry.Value}' for --config {Key}. Expected 'verbose' or 'numeric'.")
            };
        }

        if (unrecognizedKeys.Count > 0)
        {
            PanelHelper.ReportUserWarning(
                tiptoe,
                $"{modeDescription} only supports the --config key '{Key}'; ignoring: {string.Join(", ", unrecognizedKeys)}.");
        }

        return format;
    }

    /// <summary>
    /// Creates the <see cref="ITemporaryVariableNameFormatter"/> corresponding to <paramref name="format"/>.
    /// </summary>
    /// <param name="format">The requested format.</param>
    /// <returns>A fresh formatter instance.</returns>
    public static ITemporaryVariableNameFormatter CreateFormatter(VariableNameFormat format) =>
        format switch
        {
            VariableNameFormat.Verbose => new InstructionDetailVariableFormatter(),
            _ => new IncrementingIntVariableFormatter()
        };

    /// <summary>
    /// Warns that <c>varFormat</c> has no effect for <c>.utopir</c> input.
    /// </summary>
    /// <param name="filename">The filename being processed.</param>
    /// <param name="format">The variable name format.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <remarks>
    /// UtopIR source text is already in a tranformed state, so the <c>varFormat</c> configuration has no effect.
    /// Additionally this only warns for <see cref="VariableNameFormat" /><c>.Verbose</c> as
    /// <see cref="VariableNameFormat" /><c>.Numeric</c> is indistinguishable from the unset default.
    /// </remarks>
    public static void WarnIfIgnoredForUtopIrInput(string filename, VariableNameFormat format, bool tiptoe)
    {
        if (format == VariableNameFormat.Verbose && filename.EndsWith(FileManager.UtopirFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            PanelHelper.ReportUserWarning(tiptoe, $"--config {Key} is ignored for .utopir input, which has no transform step.");
        }
    }
}
