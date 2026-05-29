using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;


namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Handles <c>textDocument/completion</c> requests.
/// </summary>
/// <remarks>
/// This returns declared identifiers and language keywords as completion candidates.
/// </remarks>
public class CompletionHandler : CompletionHandlerBase
{
    private const string LanguageId = "topsy-turvy";
    private readonly DocumentStateManager documentStateManager;

    /// <summary>
    /// A map of language keywords and descriptions.
    /// </summary>
    private static readonly (string Keyword, string Detail)[] Keywords =
    [
        ("HARK!",                                    "program header"),
        ("or,",                                      "program subtitle"),
        ("FINALE.",                                  "program end"),
        ("PRINCIPALS",                               "global declarations block"),
        ("THE CURTAIN RISES.",                       "end of declarations"),
        ("PRAY WELCOME",                             "variable declaration"),
        ("AS A",                                     "type annotation"),
        ("BEING",                                    "initial value"),
        ("IS APPOINTED",                             "assignment"),
        ("IS HENCEFORTH A",                          "in-place cast"),
        ("AS IT WERE",                               "expression cast"),
        ("BEHOLD",                                   "print"),
        ("WITHOUT CEREMONY",                         "print (no newline)"),
        ("PRAY TELL",                                "input"),
        ("ASIDE:",                                   "line comment"),
        ("(ASIDE, AT SOME LENGTH:",                  "block comment (open)"),
        ("END OF ASIDE.)",                           "block comment (close)"),
        ("SHOULD IT TRANSPIRE THAT",                 "if condition"),
        ("QUITE SO.",                                "then branch"),
        ("OR, IF NOT,",                              "else-if"),
        ("OTHERWISE,",                               "else branch"),
        ("SO MUCH FOR THAT.",                        "end if"),
        ("IN WHICH CAPACITY?",                       "switch"),
        ("WHEN ACTING AS",                           "case label"),
        ("FAILING ALL OF THE ABOVE,",                "default case"),
        ("NOTHING COULD BE MORE SATISFACTORY.",      "end switch"),
        ("BY A LEGAL FICTION",                       "loop"),
        ("ASCENDING",                                "increment loop variable"),
        ("DESCENDING",                               "decrement loop variable"),
        ("UNTIL",                                    "loop exit condition"),
        ("WHILST",                                   "loop while condition"),
        ("THE TERM EXPIRES.",                        "end loop"),
        ("ONCE MORE.",                               "continue"),
        ("THAT WILL DO.",                            "break (loop or switch)"),
        ("IT IS MY DUTY TO PERFORM",                 "function definition"),
        ("UNDER THE TERMS OF",                       "function parameters"),
        ("UNDER NO OBLIGATION",                      "no-parameter function"),
        ("AND SO I FIND",                            "return with value"),
        ("MY DUTY IS DISCHARGED.",                   "end function"),
        ("MY DUTY IS PREMATURELY DISCHARGED.",       "return (no value)"),
        ("SUMMON",                                   "function call"),
        ("WITH",                                     "function call arguments"),
        ("IF YOU PLEASE.",                           "end expression list"),
        ("A HIDEOUS CURSE ON",                       "throw"),
        ("WITH THE GREATEST RESPECT,",               "try block"),
        ("WITH GRATITUDE",                           "success handler"),
        ("MODIFIED RAPTURE",                         "exception handler"),
        ("THAT CONCLUDES THE MATTER.",               "end try/catch"),
        ("PRAY ADMIT",                               "import"),
        ("SUM OF",                                   "addition"),
        ("DIFFERENCE OF",                            "subtraction"),
        ("PRODUCT OF",                               "multiplication"),
        ("QUOTIENT OF",                              "division"),
        ("REMAINDER OF",                             "modulo"),
        ("LARGER OF",                                "maximum"),
        ("SMALLER OF",                               "minimum"),
        ("WOVEN OF",                                 "string concatenation"),
        ("BOTH",                                     "logical AND"),
        ("EITHER",                                   "logical OR"),
        ("HARDLY EVER",                              "logical NOT"),
        ("ALIKE",                                    "equality (==)"),
        ("UNLIKE",                                   "inequality (!=)"),
        ("PRE-ADAMITE",                              "greater than (>)"),
        ("LOWER DEGREE",                             "less than (<)"),
        ("ALL OF",                                   "all-true (variadic AND)"),
        ("ANY OF",                                   "any-true (variadic OR)"),
        ("VERITY",                                   "boolean true"),
        ("NAY",                                      "boolean false"),
        ("NAUGHT",                                   "null"),
        ("JUST SO",                                  "implicit accumulator"),
        ("PEER",                                     "integer type"),
        ("FATHOM",                                   "float type"),
        ("YARN",                                     "string type"),
        ("DECREE",                                   "boolean type"),
    ];

    /// <summary>
    /// Initialises a new instance of the <see cref="CompletionHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public CompletionHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override CompletionRegistrationOptions CreateRegistrationOptions(
        CompletionCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector  = TextDocumentSelector.ForLanguage(LanguageId),
            ResolveProvider   = false,
            TriggerCharacters = new Container<string>(" ")
        };

    /// <inheritdoc/>
    /// <remarks>
    /// As part of the handling process, keywords are only narrowed by phrase when the
    /// phrase is actually a prefix of a known keyword (case-insensitive).  This correctly
    /// handles both uppercase variables and lowercase keyword usage, unlike a simple
    /// case check on the first character.
    /// </remarks>
    public override Task<CompletionList> Handle(
        CompletionParams request, CancellationToken cancellationToken)
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
                || Keywords.Any(k => k.Keyword.StartsWith(phrase, StringComparison.OrdinalIgnoreCase));
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
                    .Concat(this.GetImportedFunctionSymbols(request.TextDocument.Uri));

                IEnumerable<CompletionItem> symbolItems = allSymbols
                    .Where(symbol => lastWord.Length == 0
                        || symbol.Name.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase))
                    .Select(BuildSymbolItem);

                items.AddRange(symbolItems);
            }

            IEnumerable<CompletionItem> keywordItems = Keywords
                .Where(keyword => !isKeywordContext
                    || phrase.Length == 0
                    || keyword.Keyword.StartsWith(phrase, StringComparison.OrdinalIgnoreCase))
                .Select(keyword => BuildKeywordItem(keyword, keywordFilterText, insertOffset));

            items.AddRange(keywordItems);
            return Task.FromResult(new CompletionList(items, isIncomplete: true));
        }
        catch (Exception)
        {
            return Task.FromResult(new CompletionList([], isIncomplete: false));
        }
    }

    /// <inheritdoc/>
    public override Task<CompletionItem> Handle(
        CompletionItem request, CancellationToken cancellationToken) =>
        Task.FromResult(request);

    /// <summary>
    /// Returns function symbols declared in all open documents other than the given document.
    /// </summary>
    /// <param name="currentUri">The URI of the document requesting completion, which is excluded.</param>
    /// <returns>Function <see cref="SymbolInfo"/> records from every other open document.</returns>
    private IEnumerable<SymbolInfo> GetImportedFunctionSymbols(DocumentUri currentUri)
    {
        string currentKey = currentUri.ToString();
        foreach ((DocumentUri otherUri, DocumentState otherState) in this.documentStateManager.AllDocuments())
        {
            if (otherUri.ToString() == currentKey || otherState.SymbolTable is null)
            {
                continue;
            }

            foreach (SymbolInfo symbol in otherState.SymbolTable.AllSymbols())
            {
                if (symbol.Kind == SymbolKind.Function)
                {
                    yield return symbol;
                }
            }
        }
    }

    /// <summary>
    /// Builds a completion item from a symbol.
    /// </summary>
    /// <param name="symbol">The symbol information.</param>
    /// <returns>A completion item representing the symbol.</returns>
    private static CompletionItem BuildSymbolItem(SymbolInfo symbol) =>
        new()
        {
            Label  = symbol.Name,
            Kind   = symbol.Kind == SymbolKind.Function
                ? CompletionItemKind.Function
                : CompletionItemKind.Variable,
            Detail = symbol.Kind switch
            {
                SymbolKind.Variable  => symbol.TypeDisplayName,
                SymbolKind.Function  => $"({string.Join(", ", symbol.Parameters ?? Array.Empty<string>())})",
                SymbolKind.Parameter => "parameter",
                _                    => null
            }
        };

    /// <summary>
    /// Builds a completion item from a keyword entry.
    /// </summary>
    /// <param name="entry">The keyword entry.</param>
    /// <param name="filterText">The text used for filtering.</param>
    /// <param name="insertOffset">The offset for the insert text.</param>
    /// <returns>A completion item representing the keyword.</returns>
    private static CompletionItem BuildKeywordItem(
        (string Keyword, string Detail) entry, string filterText, int insertOffset) =>
        new()
        {
            Label      = entry.Keyword,
            Kind       = CompletionItemKind.Keyword,
            Detail     = entry.Detail,
            FilterText = filterText.Length > 0 ? filterText : entry.Keyword,
            InsertText = entry.Keyword[insertOffset..]
        };

    /// <summary>
    /// Extracts the trimmed phrase on the current line.
    /// </summary>
    /// <remarks>
    /// The extraction is up to the cursor and the last word within that phrase, used
    /// for server-side filtering and insert-text calculation.
    /// </remarks>
    /// <param name="source">The full document source.</param>
    /// <param name="line">0-indexed line number (LSP convention).</param>
    /// <param name="character">0-indexed character offset (LSP convention).</param>
    /// <returns>
    /// The trimmed phrase before the cursor and the last space-delimited word within it.
    /// </returns>
    private static (string Phrase, string LastWord) GetPhraseContext(
        string source, int line, int character)
    {
        string[] lines = source.Split('\n');
        if (line >= lines.Length)
        {
            return (string.Empty, string.Empty);
        }

        string lineText = lines[line];
        int safeChar = Math.Min(character, lineText.Length);
        string beforeCursor = lineText[..safeChar];

        string phrase = beforeCursor.TrimStart();
        int lastSpaceIndex = phrase.LastIndexOf(' ');
        string lastWord = lastSpaceIndex >= 0
            ? phrase[(lastSpaceIndex + 1)..]
            : phrase;

        return (phrase, lastWord);
    }
}
