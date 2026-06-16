using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Locates all occurrences of a symbol across all open documents.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/references</c>: The client requests all references to the symbol at a given position in a text document.
/// </para>
/// <para>
/// Every whole-word, case-insensitive occurrence is returned, skipping occurrences inside comments and string literals.
/// When the request context specifies that the declaration should be excluded, the occurrence on the definition line
/// of the current document is omitted from the result.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class ReferencesHandler(DocumentStateManager documentStateManager)
    : ReferencesHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for references handling.
    /// </summary>
    /// <param name="capability">The reference capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler has no additional configuration beyond registering for Topsy Turvy documents.
    /// </remarks>
    /// <returns>The registration options for references handling.</returns>
    protected override ReferenceRegistrationOptions CreateRegistrationOptions(
        ReferenceCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId)
        };

    /// <summary>
    /// Handles the <c>textDocument/references</c> request from the client when all references to a symbol are requested.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// * The document state is retrieved from the document state manager.  If <c>null</c> or the symbol table is <c>null</c> then <c>null</c> is returned so the editor displays no results.
    /// * The word at the cursor position is extracted using <see cref="SymbolTable.ExtractWordAt"/>.  If no word is found, <c>null</c> is returned.
    /// * The word is looked up in the current document symbol table.  If not found, other open documents are searched via <see cref="DocumentStateManager.FindSymbolInOtherDocuments"/>.  If still not found, or the symbol name contains a space, indicating a multi-word built-in, <c>null</c> is returned.
    /// * The current document is scanned for occurrences using <see cref="SourceAnalyser.FindWordOccurrences"/>.  If <c>IncludeDeclaration</c> in the request context is <c>false</c>, the occurrence on the definition line is skipped.
    /// * All other open documents are then scanned and their occurrences appended to the result.
    /// </remarks>
    /// <returns>
    /// A task resolving to a <see cref="LocationContainer"/> with one <see cref="Location"/> per occurrence across all
    /// open documents, or <c>null</c> if the symbol cannot be found.
    /// </returns>
    public override Task<LocationContainer?> Handle(ReferenceParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<LocationContainer?>(null);
            }

            string? word = SymbolTable.ExtractWordAt(
                state.Source,
                request.Position.Line,
                request.Position.Character);

            if (word is null)
            {
                return Task.FromResult<LocationContainer?>(null);
            }

            if (!state.SymbolTable.TryGetSymbol(word, out SymbolInfo? info) || info is null)
            {
                info = this.documentStateManager.FindSymbolInOtherDocuments(word, request.TextDocument.Uri);
                if (info is null)
                {
                    return Task.FromResult<LocationContainer?>(null);
                }
            }

            if (info.Name.Contains(' '))
            {
                return Task.FromResult<LocationContainer?>(null);
            }

            bool includeDeclaration = request.Context?.IncludeDeclaration ?? true;
            int definitionLineIndex = info.DefinitionLine > 0
                ? info.DefinitionLine - 1
                : -1;

            string source = state.Source;
            string[] lines = source.Split('\n');
            List<Location> locations = [];
            foreach ((int lineIndex, int foundAtIndex) in SourceAnalyser.FindWordOccurrences(lines, word))
            {
                if (!includeDeclaration && lineIndex == definitionLineIndex)
                {
                    continue;
                }

                int endCharacter = foundAtIndex + word.Length;
                locations.Add(new()
                {
                    Uri = request.TextDocument.Uri,
                    Range = new(new(lineIndex, foundAtIndex), new(lineIndex, endCharacter))
                });
            }

            foreach ((DocumentUri otherUri, DocumentState otherState) in this.documentStateManager.AllDocuments())
            {
                if (otherUri.ToString() == request.TextDocument.Uri.ToString())
                {
                    continue;
                }

                string otherSource = otherState.Source;
                if (string.IsNullOrEmpty(otherSource))
                {
                    continue;
                }

                string[] otherLines = otherSource.Split('\n');
                foreach ((int lineIndex, int foundAtIndex) in SourceAnalyser.FindWordOccurrences(otherLines, word))
                {
                    int endCharacter = foundAtIndex + word.Length;
                    locations.Add(new()
                    {
                        Uri = otherUri,
                        Range = new(new(lineIndex, foundAtIndex), new(lineIndex, endCharacter))
                    });
                }
            }

            return Task.FromResult<LocationContainer?>(new(locations));
        }
        catch (Exception)
        {
            return Task.FromResult<LocationContainer?>(null);
        }
    }
}
