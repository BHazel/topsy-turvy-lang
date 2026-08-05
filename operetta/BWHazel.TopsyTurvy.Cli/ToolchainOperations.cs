using System;
using Spectre.Console;
using Spectre.Console.Json;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Parser;
using BWHazel.TopsyTurvy.UtopIR.Transformer;
using BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

using TopsyParseResult = BWHazel.TopsyTurvy.Parser.ParseResult;

namespace BWHazel.TopsyTurvy.Cli;

/// <summary>
/// Compiler toolchain operations for Topsy Turvy and UtopIR.
/// </summary>
public static class ToolchainOperations
{
    /// <summary>
    /// Parses and type-checks a Topsy Turvy source file reporting any errors encountered.
    /// </summary>
    /// <param name="filename">The filename to parse.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <param name="externalFunctions">The catalogue of external functions available to <c>SUMMON</c>, or <c>null</c> to use only the Standard Library.</param>
    /// <returns>The parsed programme, or <c>null</c> if parsing or type-checking failed as errors are already reported.</returns>
    public static ProgramNode? ParseAndCheck(string filename, bool tiptoe, BindingCatalogue? externalFunctions = null)
    {
        (ProgramExecutionResult result, TopsyParseResult? parseData) = ProgramRunner.ParseFile(filename, externalFunctions: externalFunctions);
        if (!result.IsSuccess)
        {
            PanelHelper.ReportErrors(result, tiptoe);
            return null;
        }

        return parseData!.Program;
    }

    /// <summary>
    /// Builds a <see cref="UtopIRProgram"/> from a source file reporting any errors encountered.
    /// </summary>
    /// <param name="filename">The filename to compile.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <param name="formatter">The formatter used to name temporary virtual registers when transforming Topsy Turvy source; unused for <c>.utopir</c> input, which has no transform step.</param>
    /// <param name="externalFunctions">The catalogue of external functions available to <c>SUMMON</c>, or <c>null</c> to use only the Standard Library. Ignored for <c>.utopir</c> input, which has no type-check or transform step.</param>
    /// <returns>The UtopIR programme, or <c>null</c> if parsing or type-checking failed as errors are already reported.</returns>
    public static UtopIRProgram? GetUtopIrProgram(string filename, bool tiptoe, ITemporaryVariableNameFormatter formatter, BindingCatalogue? externalFunctions = null)
    {
        if (filename.EndsWith(FileManager.UtopirFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            (bool success, string? errorMessage) = FileManager.TryReadSource(filename, out string source);
            if (!success)
            {
                PanelHelper.ReportErrors(ProgramExecutionResult.Failure(errorMessage!), tiptoe);
                return null;
            }

            UtopIRParseResult parseResult = new UtopIRParser().TryParse(source);
            if (!parseResult.Success)
            {
                foreach (UtopIRDiagnostic diagnostic in parseResult.Diagnostics)
                {
                    PanelHelper.ReportUserError(tiptoe, $"[{diagnostic.Line}:{diagnostic.Column}] {diagnostic.Message}");
                }

                return null;
            }

            return parseResult.Program;
        }

        ProgramNode? program = ParseAndCheck(filename, tiptoe, externalFunctions);
        return program is null
            ? null
            : new TopsyTurvyToUtopIRTransformer(formatter, externalFunctions: externalFunctions).Transform(program);
    }

    /// <summary>
    /// Writes JSON text to STDOUT.
    /// </summary>
    /// <param name="json">The JSON text to write.</param>
    /// <param name="chromatic">A value indicating whether to syntax-highlight the output.</param>
    public static void WriteJson(string json, bool chromatic)
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
