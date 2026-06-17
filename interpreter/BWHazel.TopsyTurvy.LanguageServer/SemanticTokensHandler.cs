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
/// Applies syntax highlighting to declared symbols in a document.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/semanticTokens/full</c>: The client requests semantic token data for all symbols in the document.
/// </para>
/// <para>
/// Semantic tokens allow editor themes to colour variables, parameters and functions differently from one another and
/// from language keywords.  The handler declares a <see cref="Legend"/> listing the three supported token types:
/// <c>variable</c>, <c>parameter</c> and <c>function</c>.  The TextMate grammar handles keyword colouring separately.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class SemanticTokensHandler(DocumentStateManager documentStateManager)
    : SemanticTokensHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// The token types and modifiers supported by this handler, declared up front so the client knows what to expect.
    /// </summary>
    private static readonly SemanticTokensLegend Legend = new()
    {
        TokenTypes = new("variable", "parameter", "function"),
        TokenModifiers = new("readonly")
    };

    /// <summary>
    /// Creates the registration options for semantic token handling.
    /// </summary>
    /// <param name="capability">The semantic tokens capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler is configured to provide full-document tokenisation only; range-based tokenisation
    /// is not supported.  The <see cref="Legend"/> declares the token types and modifiers supported by this handler.
    /// </remarks>
    /// <returns>The registration options for semantic token handling.</returns>
    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId),
            Legend = Legend,
            Full = true,
            Range = false
        };

    /// <summary>
    /// Creates a <see cref="SemanticTokensDocument"/> container for the current tokenisation pass.
    /// </summary>
    /// <param name="textDocumentIdentifierParams">The text document identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// Called by the base class before <see cref="Tokenize"/> to create the container into which tokens are pushed.
    /// The base class then encodes the populated document into the LSP wire format and sends it to the client.
    /// </remarks>
    /// <returns>
    /// A task resolving to a new <see cref="SemanticTokensDocument"/> initialised with the handler <see cref="Legend"/>.
    /// </returns>
    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(
        ITextDocumentIdentifierParams textDocumentIdentifierParams,
        CancellationToken cancellationToken) =>
        Task.FromResult(new SemanticTokensDocument(Legend));

    /// <summary>
    /// Populates the semantic token builder with tokens for all declared symbols in the document.
    /// </summary>
    /// <param name="builder">The builder to push tokens into.</param>
    /// <param name="identifier">The text document identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// * The document state is retrieved from the document state manager.  If <c>null</c>, the symbol table is <c>null</c>, or the source is empty, no tokens are pushed and the task completes immediately.
    /// * Symbols with spaces in their name are skipped: these are multi-word language keywords whose colouring is handled by the TextMate grammar, not semantic tokens.
    /// * All occurrences of each single-word symbol are located using <see cref="SourceAnalyser.FindWordOccurrences"/>, including functions imported from other open documents via <see cref="DocumentStateManager.GetImportedFunctionSymbols"/>.
    /// * Tokens are sorted by ascending line then character before being pushed, as LSP semantic tokens encode positions relative to the previous token and must therefore be delivered in document order.
    /// * Any exception is silently caught to prevent the language server from crashing.
    /// </remarks>
    /// <returns>A completed task.</returns>
    protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(identifier.TextDocument.Uri);
            if (state?.SymbolTable is null || string.IsNullOrEmpty(state.Source))
            {
                return Task.CompletedTask;
            }

            string source = state.Source;
            string[] lines = source.Split('\n');
            List<(int Line, int Char, int Length, string TokenType, string[] Modifiers)> tokens = [];

            IEnumerable<SymbolInfo> allSymbols = state.SymbolTable.AllSymbols()
                .Concat(this.documentStateManager.GetImportedFunctionSymbols(identifier.TextDocument.Uri));

            foreach (SymbolInfo symbol in allSymbols)
            {
                if (symbol.Name.Contains(' '))
                {
                    continue;
                }

                string tokenType = symbol.Kind switch
                {
                    TopsyTurvySymbolKind.Function => "function",
                    TopsyTurvySymbolKind.Parameter => "parameter",
                    _ => "variable"
                };

                string[] modifiers = symbol.IsConstant ? ["readonly"] : [];

                foreach ((int lineIndex, int foundAtIndex) in SourceAnalyser.FindWordOccurrences(lines, symbol.Name))
                {
                    tokens.Add((lineIndex, foundAtIndex, symbol.Name.Length, tokenType, modifiers));
                }
            }

            foreach ((int line, int character, int length, string tokenType, string[] modifiers) in
                tokens.OrderBy(token => token.Line).ThenBy(token => token.Char))
            {
                builder.Push(line, character, length, tokenType, modifiers);
            }
        }
        catch (Exception)
        {
        }

        return Task.CompletedTask;
    }
}
