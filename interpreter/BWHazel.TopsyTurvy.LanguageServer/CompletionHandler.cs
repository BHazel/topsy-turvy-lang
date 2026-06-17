using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using TopsyTurvySymbolKind = BWHazel.TopsyTurvy.Analysis.SymbolKind;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Provides auto-completion candidates for symbols and language keywords.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/completion</c>: The client requests a list of completion candidates at a given position in a text document.
/// </para>
/// <para>
/// There are 2 optional phases to completion:
/// * Phase 1 involves the client requesting a list of completion candidates at a given position, handled by <see cref="Handle(CompletionParams, CancellationToken)"/>.
/// * Phase 2 involves the client requesting additional details for a specific completion item, handled by <see cref="Handle(CompletionItem, CancellationToken)"/>.
/// Phase 2 is not implemented in this handler as all item details are populated up front, therefore <c>ResolveProvider</c> is set to <c>false</c> during registration.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class CompletionHandler(DocumentStateManager documentStateManager)
    : CompletionHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for completion handling.
    /// </summary>
    /// <param name="capability">The completion capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler is configured to trigger completion on a space character, enabling multi-word keyword
    /// suggestions, and sets <c>ResolveProvider</c> to <c>false</c> as all item details are populated up front.
    /// </remarks>
    /// <returns>The registration options for completion handling.</returns>
    protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId),
            ResolveProvider = false,
            TriggerCharacters = new Container<string>(" ")
        };

    /// <summary>
    /// Handles the <c>textDocument/completion</c> request from the client when a completion list is requested.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// <para>
    /// Unlike most handlers, this does not return early when the document state is <c>null</c>.  Keywords are always offered
    /// as candidates even if the document has never been successfully parsed.
    /// </para>
    /// * The phrase before the cursor and the last word within it are extracted using <see cref="GetPhraseContext"/>.
    /// * A keyword context is determined: if the phrase is empty or a prefix of any known keyword, keyword candidates are included and filtered to those matching the phrase.
    /// * If a symbol table is available, symbol items are built from all symbols in the current document and any functions imported from other open documents via <see cref="DocumentStateManager.GetImportedFunctionSymbols"/>.  Symbols are filtered to those whose name starts with the last word typed.
    /// * Keyword items are built from <see cref="KeywordData.Keywords"/> and filtered as described above.  The <see cref="CompletionItem"/>.<c>InsertText</c> property is set to the remainder of the keyword after the already-typed portion.
    /// * The combined list is returned as incomplete, instructing the client to re-request as the user continues typing.
    /// </remarks>
    /// <returns>
    /// A task resolving to a <see cref="CompletionList"/> of symbol and keyword candidates, marked incomplete to
    /// prompt re-querying as the user continues typing.
    /// </returns>
    public override Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            string source = state?.Source ?? string.Empty;

            (string phrase, string lastWord) = GetPhraseContext(
                source,
                request.Position.Line,
                request.Position.Character);

            bool isKeywordContext = phrase.Length == 0
                || KeywordData.Keywords.Any(keywordInfo => keywordInfo.Keyword.StartsWith(phrase, StringComparison.OrdinalIgnoreCase));
            int insertOffset = isKeywordContext
                ? phrase.Length - lastWord.Length
                : 0;

            string keywordFilterText = isKeywordContext
                ? lastWord
                : string.Empty;

            List<CompletionItem> items = [];
            if (state?.SymbolTable is not null)
            {
                IEnumerable<SymbolInfo> allSymbols = state.SymbolTable.AllSymbols()
                    .Concat(this.documentStateManager.GetImportedFunctionSymbols(request.TextDocument.Uri));

                IEnumerable<CompletionItem> symbolItems = allSymbols
                    .Where(symbol => lastWord.Length == 0
                        || symbol.Name.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase))
                    .Select(BuildSymbolItem);

                items.AddRange(symbolItems);
            }

            IEnumerable<CompletionItem> keywordItems = KeywordData.Keywords
                .Where(keywordInfo => !isKeywordContext
                    || phrase.Length == 0
                    || keywordInfo.Keyword.StartsWith(phrase, StringComparison.OrdinalIgnoreCase))
                .Select(keywordInfo => BuildKeywordItem(keywordInfo, keywordFilterText, insertOffset));

            items.AddRange(keywordItems);
            return Task.FromResult(new CompletionList(items, isIncomplete: true));
        }
        catch (Exception)
        {
            return Task.FromResult(new CompletionList([], isIncomplete: false));
        }
    }

    /// <summary>
    /// Handles a completion item resolve request.
    /// </summary>
    /// <param name="request">The completion item to resolve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// <c>ResolveProvider</c> is set to <c>false</c> in <see cref="CreateRegistrationOptions"/>, so the client will not
    /// call this method.  It is implemented as a pass-through to satisfy the base class contract.
    /// </remarks>
    /// <returns>A task resolving to the same <see cref="CompletionItem"/> unchanged.</returns>
    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken) =>
        Task.FromResult(request);

    /// <summary>
    /// Builds a completion item from a symbol.
    /// </summary>
    /// <param name="symbol">The symbol information.</param>
    /// <returns>A completion item representing the symbol.</returns>
    private static CompletionItem BuildSymbolItem(SymbolInfo symbol) =>
        new()
        {
            Label = symbol.Name,
            Kind = symbol.Kind == TopsyTurvySymbolKind.Function
                ? CompletionItemKind.Function
                : CompletionItemKind.Variable,
            Detail = symbol.Kind switch
            {
                TopsyTurvySymbolKind.Variable => symbol.TypeDisplayName,
                TopsyTurvySymbolKind.Function => $"({string.Join(", ", symbol.Parameters ?? Array.Empty<string>())})",
                TopsyTurvySymbolKind.Parameter => "parameter",
                _ => null
            }
        };

    /// <summary>
    /// Builds a completion item from a keyword entry.
    /// </summary>
    /// <param name="keywordInfo">The keyword info.</param>
    /// <param name="filterText">The text used for filtering.</param>
    /// <param name="insertOffset">The offset for the insert text.</param>
    /// <returns>A completion item representing the keyword.</returns>
    private static CompletionItem BuildKeywordItem((string Keyword, string Detail) keywordInfo, string filterText, int insertOffset) =>
        new()
        {
            Label = keywordInfo.Keyword,
            Kind = CompletionItemKind.Keyword,
            Detail = keywordInfo.Detail,
            FilterText = filterText.Length > 0
                ? filterText
                : keywordInfo.Keyword,
            InsertText = keywordInfo.Keyword[insertOffset..]
        };

    /// <summary>
    /// Extracts the trimmed phrase on the current line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The extraction is up to the cursor and the last word within that phrase, used
    /// for server-side filtering and insert-text calculation.
    /// </para>
    /// <para>
    /// A phrase is considered to be all the text entered on a line so far trimmed of
    /// leading whitespace.  The last word is the last space-delimited token within that phrase
    /// or the entire phrase if no spaces are present.  The last word is used for filtering and
    /// insert-text calculation.
    /// </para>
    /// </remarks>
    /// <param name="source">The full document source.</param>
    /// <param name="line">0-indexed line number.</param>
    /// <param name="character">0-indexed character offset.</param>
    /// <returns>
    /// The trimmed phrase before the cursor and the last space-delimited word within it.
    /// </returns>
    private static (string Phrase, string LastWord) GetPhraseContext(string source, int line, int character)
    {
        string[] lines = source.Split('\n');
        if (line >= lines.Length)
        {
            return (string.Empty, string.Empty);
        }

        string lineText = lines[line];
        int safeCharacter = Math.Min(character, lineText.Length);
        string textBeforeCursor = lineText[..safeCharacter];

        string phrase = textBeforeCursor.TrimStart();
        int lastSpaceIndex = phrase.LastIndexOf(' ');
        string lastWord = lastSpaceIndex >= 0
            ? phrase[(lastSpaceIndex + 1)..]
            : phrase;

        return (phrase, lastWord);
    }
}
