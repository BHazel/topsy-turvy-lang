using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using TopsyTurvySymbolKind = BWHazel.TopsyTurvy.Analysis.SymbolKind;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles <c>textDocument/codeLens</c> requests.
/// </summary>
/// <remarks>
/// Emits an inline reference-count annotation above each function and variable declaration.
/// Clicking the lens opens the Find All References panel at the declaration position.
/// Reference counts exclude the declaration line itself, so <c>0 references</c> indicates an
/// unused symbol.  Parameters are excluded as their scope is local to the enclosing function.
/// Symbols with unknown definition positions are also excluded.
/// </remarks>
public class CodeLensHandler : CodeLensHandlerBase
{
    private const string ShowReferencesCommandId = "topsy-turvy.showReferences";
    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// Initialises a new instance of the <see cref="CodeLensHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public CodeLensHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override CodeLensRegistrationOptions CreateRegistrationOptions(
        CodeLensCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId),
            ResolveProvider = false
        };

    /// <inheritdoc/>
    /// <remarks>
    /// Resolve is not used (<c>ResolveProvider = false</c>); lenses are returned fully populated
    /// from <see cref="Handle(CodeLensParams, CancellationToken)"/>.
    /// </remarks>
    public override Task<CodeLens> Handle(CodeLens request, CancellationToken cancellationToken) =>
        Task.FromResult(request);

    /// <inheritdoc/>
    public override Task<CodeLensContainer?> Handle(
        CodeLensParams request, CancellationToken cancellationToken)
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
            List<CodeLens> lenses = [];

            foreach (SymbolInfo symbol in state.SymbolTable.AllSymbols())
            {
                if (symbol.Kind == TopsyTurvySymbolKind.Parameter || symbol.DefinitionLine == 0)
                {
                    continue;
                }

                int lspLine = symbol.DefinitionLine - 1;
                int lspChar = symbol.DefinitionColumn - 1;

                int refCount = SourceAnalyser.CountOccurrences(lines, symbol.Name, lspLine);

                foreach ((_, DocumentState otherState) in this.documentStateManager.AllDocuments())
                {
                    if (object.ReferenceEquals(otherState, state))
                    {
                        continue;
                    }

                    string otherSource = otherState.Source;
                    if (string.IsNullOrEmpty(otherSource))
                    {
                        continue;
                    }

                    string[] otherLines = otherSource.Split('\n');
                    refCount += SourceAnalyser.CountOccurrences(otherLines, symbol.Name, -1);
                }

                string title = refCount == 1 ? "1 reference" : $"{refCount} references";

                lenses.Add(new CodeLens
                {
                    Range = new LspRange(
                        new Position(lspLine, lspChar),
                        new Position(lspLine, lspChar + symbol.Name.Length)),
                    Command = new Command
                    {
                        Title = title,
                        Name = ShowReferencesCommandId,
                        Arguments = new JArray(
                            JValue.CreateString(request.TextDocument.Uri.ToString()),
                            new JValue(lspLine),
                            new JValue(lspChar))
                    }
                });
            }

            return Task.FromResult<CodeLensContainer?>(new CodeLensContainer(lenses));
        }
        catch (Exception)
        {
            return Task.FromResult<CodeLensContainer?>(new CodeLensContainer());
        }
    }

}
