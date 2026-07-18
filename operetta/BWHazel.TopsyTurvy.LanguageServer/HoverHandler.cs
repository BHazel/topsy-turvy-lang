using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Provides hover information for symbols.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/hover</c>: The client requests hover information for a symbol at a given position in a text document.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class HoverHandler(DocumentStateManager documentStateManager)
    : HoverHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for hover handling.
    /// </summary>
    /// <param name="capability">The hover capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler has no additional configuration beyond registering for Topsy Turvy documents.
    /// </remarks>
    /// <returns>The registration options for hover handling.</returns>
    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId)
        };

    /// <summary>
    /// Handles the <c>textDocument/hover</c> request from the client when hover information is requested.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// * The document state is retrieved from the document state manager.  If <c>null</c> or the symbol table is <c>null</c>, <c>null</c> is returned so no hover pop-up is displayed.
    /// * The word at the cursor position is extracted from the source using <see cref="SymbolTable.ExtractWordAt"/>.  If no word is found, <c>null</c> is returned.
    /// * The word is looked up in the current document symbol table, then in other open documents via <see cref="DocumentStateManager.FindSymbolInOtherDocuments"/>.  If still not found, <see cref="TryBuildNamespaceHoverAsync"/> checks whether it matches a namespace segment instead.
    /// * A Markdown hover card is built using <see cref="HoverMarkdownBuilder.Build"/> and returned to the client.
    /// </remarks>
    /// <returns>
    /// A task resolving to a <see cref="Hover"/> containing a Markdown card for the symbol under the cursor,
    /// or <c>null</c> if no symbol is found at that position.
    /// </returns>
    public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<Hover?>(null);
            }

            string? word = SymbolTable.ExtractWordAt(
                state.Source,
                request.Position.Line,
                request.Position.Character);

            if (word is null)
            {
                return Task.FromResult<Hover?>(null);
            }

            if (!state.SymbolTable.TryGetSymbol(word, out SymbolInfo? symbolInfo) || symbolInfo is null)
            {
                symbolInfo = this.documentStateManager.FindSymbolInOtherDocuments(word, request.TextDocument.Uri);
                if (symbolInfo is null)
                {
                    return this.TryBuildNamespaceHoverAsync(word);
                }
            }

            if (symbolInfo.Kind == BWHazel.TopsyTurvy.Analysis.SymbolKind.Namespace)
            {
                return this.BuildNamespaceHoverForSymbolAsync(symbolInfo);
            }

            return Task.FromResult<Hover?>(
                new()
                {
                    Contents = new(new MarkupContent()
                    {
                        Kind = MarkupKind.Markdown,
                        Value = HoverMarkdownBuilder.Build(symbolInfo)
                    })
                });
        }
        catch (Exception)
        {
            return Task.FromResult<Hover?>(null);
        }
    }

    /// <summary>
    /// Builds a hover card for a <see cref="SymbolInfo"/> that is itself a namespace declaration.
    /// </summary>
    /// <param name="symbolInfo">The namespace symbol; <see cref="SymbolInfo.Name"/> is the <c>*</c>-joined namespace path.</param>
    /// <remarks>
    /// Reached when the word under the cursor exactly matches the whole name of a single-segment namespace, e.g.
    /// <c>Accounts</c> in <c>TOWN Accounts</c>. Individual segments of a multi-segment namespace go via
    /// <see cref="TryBuildNamespaceHoverAsync"/> instead.
    /// </remarks>
    /// <returns>A task resolving to a <see cref="Hover"/> for the namespace, listing its declared functions.</returns>
    private Task<Hover?> BuildNamespaceHoverForSymbolAsync(SymbolInfo symbolInfo)
    {
        string[] namespacePath = symbolInfo.Name.Split('*');
        IEnumerable<SymbolInfo> functions = this.documentStateManager.GetFunctionsInNamespace(namespacePath);

        return Task.FromResult<Hover?>(
            new()
            {
                Contents = new(new MarkupContent()
                {
                    Kind = MarkupKind.Markdown,
                    Value = HoverMarkdownBuilder.BuildNamespaceHover(namespacePath, functions)
                })
            });
    }

    /// <summary>
    /// Builds a hover card for a word matching a namespace path segment.
    /// </summary>
    /// <param name="word">The word under the cursor.</param>
    /// <remarks>
    /// A namespace segment has no <see cref="SymbolInfo"/> of its own, so this is the fallback once ordinary symbol
    /// lookup fails. A word matching more than one namespace path gets one hover block per match.
    /// </remarks>
    /// <returns>A task resolving to a <see cref="Hover"/> for every matching namespace, or <c>null</c> if none match.</returns>
    private Task<Hover?> TryBuildNamespaceHoverAsync(string word)
    {
        try
        {
            List<string> matchingBlocks = [];
            foreach (IReadOnlyList<string> namespacePath in this.documentStateManager.GetKnownNamespacePaths())
            {
                if (!namespacePath.Any(segment => string.Equals(segment, word, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                IEnumerable<SymbolInfo> functions = this.documentStateManager.GetFunctionsInNamespace(namespacePath);
                matchingBlocks.Add(HoverMarkdownBuilder.BuildNamespaceHover(namespacePath, functions));
            }

            if (matchingBlocks.Count == 0)
            {
                return Task.FromResult<Hover?>(null);
            }

            return Task.FromResult<Hover?>(
                new()
                {
                    Contents = new(new MarkupContent()
                    {
                        Kind = MarkupKind.Markdown,
                        Value = string.Join("\n\n---\n\n", matchingBlocks)
                    })
                });
        }
        catch (Exception)
        {
            return Task.FromResult<Hover?>(null);
        }
    }
}
