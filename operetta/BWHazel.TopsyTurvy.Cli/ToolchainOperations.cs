using System;
using Spectre.Console;
using Spectre.Console.Json;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using BWHazel.TopsyTurvy.UtopIR.Parser;
using BWHazel.TopsyTurvy.UtopIR.Transformer;

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
    /// <returns>The parsed programme, or <c>null</c> if parsing or type-checking failed as errors are already reported.</returns>
    public static ProgramNode? ParseAndCheck(string filename, bool tiptoe)
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
    /// Builds a <see cref="UtopIRProgram"/> from a source file reporting any errors encountered.
    /// </summary>
    /// <param name="filename">The filename to compile.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    /// <returns>The UtopIR programme, or <c>null</c> if parsing or type-checking failed as errors are already reported.</returns>
    public static UtopIRProgram? GetUtopIrProgram(string filename, bool tiptoe)
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

        ProgramNode? program = ParseAndCheck(filename, tiptoe);
        return program is null
            ? null
            : new TopsyTurvyToUtopIRTransformer().Transform(program);
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
