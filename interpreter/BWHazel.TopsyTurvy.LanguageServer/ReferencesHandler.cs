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
/// Handles <c>textDocument/references</c> requests.
/// </summary>
/// <remarks>
/// Returns every whole-word, case-insensitive occurrence of the symbol under the cursor,
/// skipping occurrences inside comments and string literals.  When the request context
/// specifies that the declaration should be excluded, the occurrence on the definition
/// line is omitted from the result.
/// </remarks>
public class ReferencesHandler : ReferencesHandlerBase
{
    private const string LanguageId = "topsy-turvy";
    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// Initialises a new instance of the <see cref="ReferencesHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public ReferencesHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override ReferenceRegistrationOptions CreateRegistrationOptions(
        ReferenceCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId)
        };

    /// <inheritdoc/>
    public override Task<LocationContainer?> Handle(
        ReferenceParams request, CancellationToken cancellationToken)
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
                info = this.FindSymbolInOtherDocuments(request.TextDocument.Uri, word);
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
            int[] lineOffsets = SourceAnalyser.BuildLineOffsets(lines);
            List<(int Start, int End)> skipRanges = SourceAnalyser.FindSkipRanges(source);
            List<Location> locations = [];

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

                    if (!includeDeclaration && lineIndex == definitionLineIndex)
                    {
                        continue;
                    }

                    locations.Add(new Location
                    {
                        Uri = request.TextDocument.Uri,
                        Range = new LspRange(
                            new Position(lineIndex, foundAt),
                            new Position(lineIndex, endChar))
                    });
                }
            }

            foreach ((DocumentUri otherUri, DocumentState otherState) in
                this.documentStateManager.AllDocuments())
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
                int[] otherOffsets = SourceAnalyser.BuildLineOffsets(otherLines);
                List<(int Start, int End)> otherSkip = SourceAnalyser.FindSkipRanges(otherSource);
                for (int lineIndex = 0; lineIndex < otherLines.Length; lineIndex++)
                {
                    string lineText = otherLines[lineIndex];
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

                        int absoluteOffset = otherOffsets[lineIndex] + foundAt;
                        if (SourceAnalyser.IsInSkipRange(absoluteOffset, otherSkip))
                        {
                            continue;
                        }

                        locations.Add(new Location
                        {
                            Uri = otherUri,
                            Range = new LspRange(
                                new Position(lineIndex, foundAt),
                                new Position(lineIndex, endChar))
                        });
                    }
                }
            }

            return Task.FromResult<LocationContainer?>(new LocationContainer(locations));
        }
        catch (Exception)
        {
            return Task.FromResult<LocationContainer?>(null);
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
}
