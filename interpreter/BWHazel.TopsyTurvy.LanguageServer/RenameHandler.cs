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
/// Handles <c>textDocument/rename</c> requests.
/// </summary>
/// <remarks>
/// Finds every whole-word, case-insensitive occurrence of the symbol under the cursor
/// across all open documents (skipping comments and string literals) and replaces each
/// with the new name supplied by the editor, returning a <see cref="WorkspaceEdit"/>
/// with one <see cref="TextEdit"/> per occurrence.
/// </remarks>
public class RenameHandler : RenameHandlerBase
{
    private const string LanguageId = "topsy-turvy";
    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// Initialises a new instance of the <see cref="RenameHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public RenameHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override RenameRegistrationOptions CreateRegistrationOptions(
        RenameCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId)
        };

    /// <inheritdoc/>
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

            if (!state.SymbolTable.TryGetSymbol(word, out SymbolInfo? info) || info is null)
            {
                info = this.FindSymbolInOtherDocuments(request.TextDocument.Uri, word);
                if (info is null)
                {
                    return Task.FromResult<WorkspaceEdit?>(null);
                }
            }

            if (info.Name.Contains(' '))
            {
                return Task.FromResult<WorkspaceEdit?>(null);
            }

            Dictionary<DocumentUri, IEnumerable<TextEdit>> changes = [];
            foreach ((DocumentUri documentUri, DocumentState documentState) in this.documentStateManager.AllDocuments())
            {
                string documentSource = documentState.Source;
                if (string.IsNullOrEmpty(documentSource))
                {
                    continue;
                }

                List<TextEdit> edits = CollectEdits(documentSource, word, request.NewName);
                if (edits.Count > 0)
                {
                    changes[documentUri] = edits;
                }
            }

            if (changes.Count == 0)
            {
                return Task.FromResult<WorkspaceEdit?>(null);
            }

            return Task.FromResult<WorkspaceEdit?>(new WorkspaceEdit { Changes = changes });
        }
        catch (Exception)
        {
            return Task.FromResult<WorkspaceEdit?>(null);
        }
    }

    /// <summary>
    /// Searches all open documents other than the current one for a symbol with the given name.
    /// </summary>
    /// <param name="currentUri">The URI of the document being searched, which is excluded.</param>
    /// <param name="name">The symbol name to find.</param>
    /// <returns>The first matching <see cref="SymbolInfo"/>, or <c>null</c> if not found.</returns>
    private SymbolInfo? FindSymbolInOtherDocuments(DocumentUri currentUri, string name)
    {
        string currentKey = currentUri.ToString();
        foreach ((DocumentUri otherUri, DocumentState otherState) in this.documentStateManager.AllDocuments())
        {
            if (otherUri.ToString() == currentKey || otherState.SymbolTable is null)
            {
                continue;
            }

            if (otherState.SymbolTable.TryGetSymbol(name, out SymbolInfo? info) && info is not null)
            {
                return info;
            }
        }

        return null;
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
        int[] lineOffsets = SourceAnalyser.BuildLineOffsets(lines);
        List<(int Start, int End)> skipRanges = SourceAnalyser.FindSkipRanges(source);
        List<TextEdit> edits = [];

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string lineText = lines[lineIndex];
            int searchFrom = 0;
            int foundAt;

            while ((foundAt = lineText.IndexOf(word, searchFrom, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                searchFrom = foundAt + 1;
                if (foundAt > 0 && SourceAnalyser.IsIdentifierChar(lineText[foundAt - 1]))
                {
                    continue;
                }

                int endChar = foundAt + word.Length;
                if (endChar < lineText.Length && SourceAnalyser.IsIdentifierChar(lineText[endChar]))
                {
                    continue;
                }

                int absoluteOffset = lineOffsets[lineIndex] + foundAt;
                if (SourceAnalyser.IsInSkipRange(absoluteOffset, skipRanges))
                {
                    continue;
                }

                edits.Add(new TextEdit
                {
                    Range = new LspRange(
                        new Position(lineIndex, foundAt),
                        new Position(lineIndex, endChar)),
                    NewText = newName
                });
            }
        }

        return edits;
    }
}
