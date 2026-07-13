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
    /// <remarks>
    /// <para>
    /// Used by LSP handlers requiring symbol listing, which require a <see cref="Location"/> for each symbol:
    /// * <see cref="DocumentSymbolHandler"/>: Builds the Outline panel for all symbols in a document.
    /// * <see cref="WorkspaceSymbolHandler"/>: Used to locate the symbol when selecting Go to Symbol in Workspace.
    /// </para>
    /// <para>
    /// Co-ordinates in LSP are 0-based, while co-ordinates in Topsy Turvy are 1-based. This method converts the symbol definition
    /// line and column to 0-based values for the LSP location.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A <see cref="Location"/> covering the symbol name token on its definition line,
    /// or a zero-point location when the definition position is not known.
    /// </returns>
    public static Location GetSymbolDefinitionLocation(DocumentUri uri, SymbolInfo symbol)
    {
        LspRange range;
        if (symbol.DefinitionLine != 0)
        {
            int startLine = symbol.DefinitionLine - 1;
            int startCharacter = symbol.DefinitionColumn - 1;
            int endCharacter = startCharacter + symbol.Name.Length;
            range = new(new(startLine, startCharacter), new(startLine, endCharacter));
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
    /// Maps a Topsy Turvy <see cref="Analysis.SymbolKind"/> to the corresponding LSP <see cref="OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind"/>.
    /// </summary>
    /// <param name="symbolKind">The Topsy Turvy symbol kind.</param>
    /// <remarks>
    /// <para>
    /// Used by LSP handlers requiring symbol listing, which require a <see cref="OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind"/>
    /// to determine the icon to display for each symbol:
    /// * <see cref="DocumentSymbolHandler"/>: Used to determine which icon to display in the Outline panel for each symbol.
    /// * <see cref="WorkspaceSymbolHandler"/>: Used to determine which icon to display in the search results for each symbol.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The <see cref="OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind"/> corresponding to the given
    /// Topsy Turvy symbol kind, used to determine the icon displayed for the symbol in the editor.
    /// </returns>
    public static LspSymbolKind MapSymbolKind(TopsyTurvySymbolKind symbolKind) => symbolKind switch
    {
        TopsyTurvySymbolKind.Function => LspSymbolKind.Function,
        TopsyTurvySymbolKind.Parameter => LspSymbolKind.TypeParameter,
        TopsyTurvySymbolKind.Namespace => LspSymbolKind.Namespace,
        _ => LspSymbolKind.Variable
    };
}
