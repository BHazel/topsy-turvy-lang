using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspSymbolKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind;
using TopsyTurvySymbolKind = BWHazel.TopsyTurvy.Analysis.SymbolKind;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Provides shared LSP utility methods used across multiple handlers.
/// </summary>
internal static class LspUtilities
{
    /// <summary>
    /// Builds an LSP <see cref="Location"/> for the given symbol.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <param name="symbol">The symbol whose definition location is required.</param>
    /// <returns>
    /// A <see cref="Location"/> covering the symbol name token on its definition line,
    /// or a zero-point location when the definition position is not known.
    /// </returns>
    public static Location BuildLocation(DocumentUri uri, SymbolInfo symbol)
    {
        LspRange range;
        if (symbol.DefinitionLine != 0)
        {
            int startLine = symbol.DefinitionLine - 1;
            int startChar = symbol.DefinitionColumn - 1;
            int endChar = startChar + symbol.Name.Length;
            range = new(new(startLine, startChar), new(startLine, endChar));
        }
        else
        {
            range = new(new(0, 0), new(0, 0));
        }

        return new()
        {
            Uri = uri,
            Range = range
        };
    }

    /// <summary>
    /// Maps a Topsy Turvy <see cref="BWHazel.TopsyTurvy.Analysis.SymbolKind"/> to the corresponding LSP <see cref="LspSymbolKind"/>.
    /// </summary>
    /// <param name="kind">The Topsy Turvy symbol kind.</param>
    /// <returns>The LSP symbol kind.</returns>
    public static LspSymbolKind MapSymbolKind(TopsyTurvySymbolKind kind) => kind switch
    {
        TopsyTurvySymbolKind.Function => LspSymbolKind.Function,
        TopsyTurvySymbolKind.Parameter => LspSymbolKind.TypeParameter,
        _ => LspSymbolKind.Variable
    };
}
