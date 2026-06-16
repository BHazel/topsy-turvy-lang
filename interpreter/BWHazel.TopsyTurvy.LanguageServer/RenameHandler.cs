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
/// Renames all occurrences of a symbol across all open documents.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/rename</c>: The client requests a rename of the symbol at a given position in a text document.
/// </para>
/// <para>
/// Every whole-word, case-insensitive occurrence of the symbol across all open documents is replaced with the new
/// name, skipping occurrences inside comments and string literals.  Multi-word built-in symbols cannot be renamed
/// and result in a <c>null</c> response.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class RenameHandler(DocumentStateManager documentStateManager)
    : RenameHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for rename handling.
    /// </summary>
    /// <param name="capability">The rename capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler has no additional configuration beyond registering for Topsy Turvy documents.
    /// </remarks>
    /// <returns>The registration options for rename handling.</returns>
    protected override RenameRegistrationOptions CreateRegistrationOptions(
        RenameCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId)
        };

    /// <summary>
    /// Handles the <c>textDocument/rename</c> request from the client when a rename is applied.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// * The document state is retrieved from the document state manager.  If <c>null</c> or the symbol table is <c>null</c> then <c>null</c> is returned so the editor takes no action.
    /// * The word at the cursor position is extracted using <see cref="SymbolTable.ExtractWordAt"/>.  If no word is found, <c>null</c> is returned.
    /// * The word is looked up in the current document symbol table.  If not found, other open documents are searched via <see cref="DocumentStateManager.FindSymbolInOtherDocuments"/>.  If still not found, or the symbol name contains a space (indicating a multi-word built-in that cannot be renamed), <c>null</c> is returned.
    /// * All open documents are scanned and <see cref="TextEdit"/> items are collected for each occurrence using <see cref="CollectEdits"/>.
    /// * A <see cref="WorkspaceEdit"/> grouping the edits by document URI is returned to the client.
    /// </remarks>
    /// <returns>
    /// A task resolving to a <see cref="WorkspaceEdit"/> containing one <see cref="TextEdit"/> per occurrence across
    /// all open documents, or <c>null</c> if the symbol cannot be found or renamed.
    /// </returns>
    public override Task<WorkspaceEdit?> Handle(RenameParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<WorkspaceEdit?>(null);
            }

            string? word = SymbolTable.ExtractWordAt(
                state.Source,
                request.Position.Line,
                request.Position.Character);

            if (word is null)
            {
                return Task.FromResult<WorkspaceEdit?>(null);
            }

            if (!state.SymbolTable.TryGetSymbol(word, out SymbolInfo? symbolInfo) || symbolInfo is null)
            {
                symbolInfo = this.documentStateManager.FindSymbolInOtherDocuments(word, request.TextDocument.Uri);
                if (symbolInfo is null)
                {
                    return Task.FromResult<WorkspaceEdit?>(null);
                }
            }

            if (symbolInfo.Name.Contains(' '))
            {
                return Task.FromResult<WorkspaceEdit?>(null);
            }

            Dictionary<DocumentUri, IEnumerable<TextEdit>> renames = [];
            foreach ((DocumentUri documentUri, DocumentState documentState) in this.documentStateManager.AllDocuments())
            {
                string documentSource = documentState.Source;
                if (string.IsNullOrEmpty(documentSource))
                {
                    continue;
                }

                List<TextEdit> textEdits = CollectEdits(documentSource, word, request.NewName);
                if (textEdits.Count > 0)
                {
                    renames[documentUri] = textEdits;
                }
            }

            if (renames.Count == 0)
            {
                return Task.FromResult<WorkspaceEdit?>(null);
            }

            return Task.FromResult<WorkspaceEdit?>(new()
                {
                    Changes = renames
                });
        }
        catch (Exception)
        {
            return Task.FromResult<WorkspaceEdit?>(null);
        }
    }

    /// <summary>
    /// Collects whole-word, case-insensitive rename edits for a symbol across a single document.
    /// </summary>
    /// <param name="source">The document source text to scan.</param>
    /// <param name="word">The symbol name to find.</param>
    /// <param name="newName">The replacement name.</param>
    /// <returns>A list of <see cref="TextEdit"/> covering every occurrence.</returns>
    private static List<TextEdit> CollectEdits(string source, string word, string newName)
    {
        string[] lines = source.Split('\n');
        List<TextEdit> edits = [];
        foreach ((int lineIndex, int foundAtIndex) in SourceAnalyser.FindWordOccurrences(lines, word))
        {
            int endCharacter = foundAtIndex + word.Length;
            edits.Add(new()
            {
                Range = new(new(lineIndex, foundAtIndex), new(lineIndex, endCharacter)),
                NewText = newName
            });
        }

        return edits;
    }
}
