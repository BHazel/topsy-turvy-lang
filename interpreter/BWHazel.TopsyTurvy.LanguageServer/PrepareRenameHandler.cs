using System;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles <c>textDocument/prepareRename</c> requests.
/// </summary>
/// <remarks>
/// Validates whether the symbol under the cursor can be renamed before the editor shows
/// the rename input box.  Returns the token span and current name as placeholder text
/// when the cursor is on a renameable user symbol.  It returns <c>null</c> to suppress the
/// box when the cursor is on a keyword, an unknown token or a multi-word built-in.
/// </remarks>
public class PrepareRenameHandler : PrepareRenameHandlerBase
{
    private const string LanguageId = "topsy-turvy";
    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// Initialises a new instance of the <see cref="PrepareRenameHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public PrepareRenameHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override RenameRegistrationOptions CreateRegistrationOptions(
        RenameCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageId),
            PrepareProvider = true
        };

    /// <inheritdoc/>
    public override Task<RangeOrPlaceholderRange?> Handle(
        PrepareRenameParams request, CancellationToken cancellationToken)
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
            int col = Math.Min(character, lineText.Length - 1);
            int startCol = col;
            while (startCol > 0 && IsIdentifierChar(lineText[startCol - 1]))
            {
                startCol--;
            }

            LspRange range = new(
                new Position(line, startCol),
                new Position(line, startCol + word.Length));

            return Task.FromResult<RangeOrPlaceholderRange?>(
                new RangeOrPlaceholderRange(new PlaceholderRange
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

    /// <summary>
    /// Determines if a character is valid inside a Topsy Turvy identifier.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns><c>true</c> if the character is a valid identifier character, otherwise <c>false</c>.</returns>
    private static bool IsIdentifierChar(char character) =>
        char.IsLetterOrDigit(character) || character == '-' || character == '_';
}
