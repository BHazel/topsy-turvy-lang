using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using TopsyTurvySymbolKind = BWHazel.TopsyTurvy.Analysis.SymbolKind;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Shows inline reference-count annotations above each function and variable declaration.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP requests:
/// * <c>textDocument/codeLens</c>: The client requests inline CodeLens annotations for a document.
/// * <c>codeLens/resolve</c>: The client requests additional detail for a specific CodeLens item.
/// </para>
/// <para>
/// Each annotation displays the number of references to the symbol in the current document plus any other open
/// document connected to it by a <c>PRAY ADMIT</c> import (see <see cref="DocumentStateManager.GetImportConnectedDocuments"/>),
/// excluding the declaration line itself, so <c>0 references</c> indicates an unused symbol.  Clicking the
/// annotation opens the Find All References panel at the declaration position.  Parameters, namespace symbols, and
/// symbols with unknown definition positions are excluded (the synthesised <c>*</c>-joined namespace name does not
/// appear verbatim in source for long-form <c>WITH DISTRICT</c> usages, so scanning for it would under-count).
/// </para>
/// <para>
/// The <see cref="CodeLensRegistrationOptions.ResolveProvider"/> property is set to <c>false</c> as all CodeLens data is
/// populated up front, removing the need for a second resolve request per CodeLens item.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class CodeLensHandler(DocumentStateManager documentStateManager)
    : CodeLensHandlerBase
{
    /// <summary>
    /// The VS Code command identifier used to open the Find All References panel at the symbol position.
    /// </summary>
    private const string ShowReferencesCommandId = "topsy-turvy.showReferences";

    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for code lens handling.
    /// </summary>
    /// <param name="capability">The CodeLens capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  The <see cref="CodeLensRegistrationOptions.ResolveProvider"/> property is set to <c>false</c> as all
    /// CodeLens data is populated up front in <see cref="Handle(CodeLensParams, CancellationToken)"/>.
    /// </remarks>
    /// <returns>The registration options for code lens handling.</returns>
    protected override CodeLensRegistrationOptions CreateRegistrationOptions(CodeLensCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId),
            ResolveProvider = false
        };

    /// <summary>
    /// Handles a CodeLens resolve request.
    /// </summary>
    /// <param name="request">The CodeLens item to resolve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// <c>ResolveProvider</c> is set to <c>false</c> in <see cref="CreateRegistrationOptions"/>, so the client will not
    /// call this method.  It is implemented as a pass-through to satisfy the base class contract.
    /// </remarks>
    /// <returns>A task resolving to the same <see cref="CodeLens"/> unchanged.</returns>
    public override Task<CodeLens> Handle(CodeLens request, CancellationToken cancellationToken) =>
        Task.FromResult(request);

    /// <summary>
    /// Handles the <c>textDocument/codeLens</c> request from the client when CodeLens annotations are requested.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// * The document state is retrieved from the document state manager.  If <c>null</c> or the symbol table is <c>null</c>, an empty container is returned so no annotations are displayed.
    /// * Parameters and symbols with unknown definition positions are skipped.
    /// * For each remaining symbol, <see cref="SourceAnalyser.CountOccurrences"/> is called to count references in the current document, excluding the definition line.  References in other open documents connected by a <c>PRAY ADMIT</c> import are also counted and added to the total.
    /// * A <see cref="CodeLens"/> is built for each symbol with the reference count as the title and a command targeting <see cref="ShowReferencesCommandId"/> to open the Find All References panel.
    /// </remarks>
    /// <returns>
    /// A task resolving to a <see cref="CodeLensContainer"/> with one annotation per function and variable symbol,
    /// or an empty container if the document has not been successfully parsed or an error occurs.
    /// </returns>
    public override Task<CodeLensContainer?> Handle(CodeLensParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<CodeLensContainer?>(new CodeLensContainer());
            }

            string source = state.Source;
            string[] lines = source.Split('\n');
            List<CodeLens> codeLenses = [];
            foreach (SymbolInfo symbol in state.SymbolTable.AllSymbols())
            {
                if (symbol.Kind == TopsyTurvySymbolKind.Parameter
                    || symbol.Kind == TopsyTurvySymbolKind.Namespace
                    || symbol.DefinitionLine == 0)
                {
                    continue;
                }

                int lspLine = symbol.DefinitionLine - 1;
                int lspCharacter = symbol.DefinitionColumn - 1;

                int referenceCount = SourceAnalyser.CountOccurrences(lines, symbol.Name, lspLine);

                foreach ((_, DocumentState otherState) in this.documentStateManager.GetImportConnectedDocuments(request.TextDocument.Uri))
                {
                    string otherSource = otherState.Source;
                    if (string.IsNullOrEmpty(otherSource))
                    {
                        continue;
                    }

                    string[] otherLines = otherSource.Split('\n');
                    referenceCount += SourceAnalyser.CountOccurrences(otherLines, symbol.Name, -1);
                }

                string title = referenceCount == 1
                    ? "1 reference"
                    : $"{referenceCount} references";
                
                codeLenses.Add(new()
                {
                    Range = new(new(lspLine, lspCharacter), new(lspLine, lspCharacter + symbol.Name.Length)),
                    Command = new()
                    {
                        Title = title,
                        Name = ShowReferencesCommandId,
                        Arguments = new JArray(
                            JValue.CreateString(request.TextDocument.Uri.ToString()),
                            new JValue(lspLine),
                            new JValue(lspCharacter))
                    }
                });
            }

            return Task.FromResult<CodeLensContainer?>(new(codeLenses));
        }
        catch (Exception)
        {
            return Task.FromResult<CodeLensContainer?>(new());
        }
    }
}
