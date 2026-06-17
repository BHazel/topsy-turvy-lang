using System;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Validates whether the symbol under the cursor can be renamed before the editor shows the rename input box.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/prepareRename</c>: The client requests validation of a rename target before showing the rename input box.
/// </para>
/// <para>
/// When a rename is triggered, the editor calls this handler first.  If <c>null</c> is returned the rename input box
/// is suppressed; if a <see cref="RangeOrPlaceholderRange"/> is returned the editor shows the input box pre-filled with
/// the symbol name.  The <see cref="RenameRegistrationOptions.PrepareProvider"/> property is set to <c>true</c> to enable
/// this pre-flight check.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class PrepareRenameHandler(DocumentStateManager documentStateManager)
    : PrepareRenameHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Creates the registration options for rename preparation handling.
    /// </summary>
    /// <param name="capability">The rename capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  The <see cref="RenameRegistrationOptions.PrepareProvider"/> property is set to <c>true</c> to instruct
    /// the editor to call <c>textDocument/prepareRename</c> before displaying the rename input box.
    /// </remarks>
    /// <returns>The registration options for rename handling.</returns>
    protected override RenameRegistrationOptions CreateRegistrationOptions(RenameCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId),
            PrepareProvider = true
        };

    /// <summary>
    /// Handles the <c>textDocument/prepareRename</c> request from the client when a rename is initiated.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// * The document state is retrieved from the document state manager.  If <c>null</c> or the symbol table is <c>null</c> then <c>null</c> is returned to suppress the rename input box.
    /// * The word at the cursor position is extracted using <see cref="SymbolTable.ExtractWordAt"/>.  If no word is found, <c>null</c> is returned.
    /// * The word is looked up in the current document symbol table only.  If not found, or the symbol name contains a space, indicating a multi-word built-in that cannot be renamed, <c>null</c> is returned.
    /// * The start column of the token is located by walking backwards from the cursor position using <see cref="SourceAnalyser.IsIdentifierChar"/> to build the token range.
    /// * A <see cref="PlaceholderRange"/> is returned containing the token range and the symbol name as placeholder text, which the editor uses to pre-fill the rename input box.
    /// </remarks>
    /// <returns>
    /// A task resolving to a <see cref="RangeOrPlaceholderRange"/> containing the token span and placeholder name,
    /// or <c>null</c> if the symbol at the cursor position cannot be renamed.
    /// </returns>
    public override Task<RangeOrPlaceholderRange?> Handle(PrepareRenameParams request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state?.SymbolTable is null)
            {
                return Task.FromResult<RangeOrPlaceholderRange?>(null);
            }

            int line = request.Position.Line;
            int character = request.Position.Character;

            string? word = SymbolTable.ExtractWordAt(state.Source, line, character);
            if (word is null)
            {
                return Task.FromResult<RangeOrPlaceholderRange?>(null);
            }

            if (!state.SymbolTable.TryGetSymbol(word, out SymbolInfo? info) || info is null)
            {
                return Task.FromResult<RangeOrPlaceholderRange?>(null);
            }

            if (info.Name.Contains(' '))
            {
                return Task.FromResult<RangeOrPlaceholderRange?>(null);
            }

            string[] lines = state.Source.Split('\n');
            if (line >= lines.Length)
            {
                return Task.FromResult<RangeOrPlaceholderRange?>(null);
            }

            string lineText = lines[line].TrimEnd('\r');
            int column = Math.Min(character, lineText.Length - 1);
            int startColumn = column;
            while (startColumn > 0 && SourceAnalyser.IsIdentifierChar(lineText[startColumn - 1]))
            {
                startColumn--;
            }

            LspRange range = new(new(line, startColumn), new(line, startColumn + word.Length));

            return Task.FromResult<RangeOrPlaceholderRange?>(
                new(new PlaceholderRange()
                {
                    Range = range,
                    Placeholder = word
                }));
        }
        catch (Exception)
        {
            return Task.FromResult<RangeOrPlaceholderRange?>(null);
        }
    }
}
