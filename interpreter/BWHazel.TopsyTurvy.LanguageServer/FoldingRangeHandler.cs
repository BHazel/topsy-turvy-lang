using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.LanguageServer;

/// <summary>
/// Identifies foldable regions in a document so the editor can collapse code blocks.
/// </summary>
/// <remarks>
/// <para>
/// This handler handles the following LSP request:
/// * <c>textDocument/foldingRange</c>: The client requests a list of foldable regions in a text document.
/// </para>
/// <para>
/// Folding ranges are identified by a single-pass line scan using a stack to match opener and closer keywords.
/// When an opener is encountered its line number and folding kind are pushed onto the stack; when a closer is
/// encountered the most recent opener is popped and a range is recorded.  Each opener is paired with a folding
/// kind: <c>region</c> for code blocks and <c>comment</c> for comments.
/// </para>
/// </remarks>
/// <param name="documentStateManager">The manager providing per-document symbol state.</param>
public class FoldingRangeHandler(DocumentStateManager documentStateManager)
    : FoldingRangeHandlerBase
{
    private readonly DocumentStateManager documentStateManager = documentStateManager;

    /// <summary>
    /// Block openers paired with their folding kind.
    /// </summary>
    private static readonly (string Opener, string Kind)[] Openers =
    [
        ("HARK!",                       FoldingRangeKind.Region),
        ("PRINCIPALS",                  FoldingRangeKind.Region),
        ("IT IS MY DUTY TO PERFORM",    FoldingRangeKind.Region),
        ("BY A LEGAL FICTION",          FoldingRangeKind.Region),
        ("SHOULD IT TRANSPIRE THAT",    FoldingRangeKind.Region),
        ("IN WHICH CAPACITY?",          FoldingRangeKind.Region),
        ("WITH THE GREATEST RESPECT,",  FoldingRangeKind.Region),
        ("YEOMAN",                      FoldingRangeKind.Region),
        ("(ASIDE, AT SOME LENGTH:",     FoldingRangeKind.Comment),
    ];

    /// <summary>
    /// Block closers.
    /// </summary>
    private static readonly string[] Closers =
    [
        "FINALE.",
        "THE CURTAIN RISES.",
        "MY DUTY IS DISCHARGED.",
        "THE TERM EXPIRES.",
        "SO MUCH FOR THAT.",
        "NOTHING COULD BE MORE SATISFACTORY.",
        "THAT CONCLUDES THE MATTER.",
        "UNDER ORDERS.",
        "END OF ASIDE.)",
    ];

    /// <summary>
    /// Creates the registration options for folding range handling.
    /// </summary>
    /// <param name="capability">The folding range capability of the client.</param>
    /// <param name="clientCapabilities">The capabilities of the client.</param>
    /// <remarks>
    /// This is called by the language server on start-up to register the handler document handling capabilities and options
    /// with the server.  This handler has no additional configuration beyond registering for Topsy Turvy documents.
    /// </remarks>
    /// <returns>The registration options for folding range handling.</returns>
    protected override FoldingRangeRegistrationOptions CreateRegistrationOptions(FoldingRangeCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId)
        };

    /// <summary>
    /// Handles the <c>textDocument/foldingRange</c> request from the client when folding ranges are requested.
    /// </summary>
    /// <param name="request">The parameters of the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// <para>
    /// Unlike most handlers, this does not check whether the symbol table is available: folding is determined
    /// entirely from the source text, so it works even when the document has syntax errors.
    /// </para>
    /// * The document source is split into lines and scanned in a single pass.
    /// * When an opener keyword is encountered, its line number and folding kind are pushed onto a stack.
    /// * When a closer keyword is encountered, the most recent opener is popped from the stack and a <see cref="FoldingRange"/> is recorded covering the lines between them.  The end line is the line immediately before the closer.
    /// * Ranges of zero or negative length are discarded.
    /// </remarks>
    /// <returns>
    /// A task resolving to a container of <see cref="FoldingRange"/> items for each matched opener/closer pair,
    /// or <c>null</c> if the document state is unavailable.
    /// </returns>
    public override Task<Container<FoldingRange>?> Handle(FoldingRangeRequestParam request, CancellationToken cancellationToken)
    {
        try
        {
            DocumentState? state = this.documentStateManager.Get(request.TextDocument.Uri);
            if (state is null)
            {
                return Task.FromResult<Container<FoldingRange>?>(null);
            }

            string[] lines = state.Source.Split('\n');
            Stack<(int StartLine, string Kind)> stack = new();
            List<FoldingRange> ranges = [];

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmedLine = lines[i].Trim();
                if (IsCloser(trimmedLine))
                {
                    if (stack.Count > 0)
                    {
                        (int startLine, string kind) = stack.Pop();
                        int endLine = i - 1;
                        if (endLine > startLine)
                        {
                            ranges.Add(new()
                            {
                                StartLine = startLine,
                                EndLine = endLine,
                                Kind = kind
                            });
                        }
                    }

                    continue;
                }

                string? openerKind = FindOpenerKind(trimmedLine);
                if (openerKind is not null)
                {
                    stack.Push((i, openerKind));
                }
            }

            return Task.FromResult<Container<FoldingRange>?>(new(ranges));
        }
        catch (Exception)
        {
            return Task.FromResult<Container<FoldingRange>?>(null);
        }
    }

    /// <summary>
    /// Returns the folding kind for a line that starts with a known opener,
    /// or <c>null</c> if the line is not an opener.
    /// </summary>
    /// <param name="trimmedLine">The trimmed source line.</param>
    /// <returns>The folding kind string, or <c>null</c>.</returns>
    private static string? FindOpenerKind(string trimmedLine)
    {
        foreach ((string opener, string kind) in Openers)
        {
            if (LineStartsWith(trimmedLine, opener))
            {
                return kind;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether the trimmed line is a block closer.
    /// </summary>
    /// <param name="trimmedLine">The trimmed source line.</param>
    /// <returns><c>true</c> if the line starts with a known closer, otherwise <c>false</c>.</returns>
    private static bool IsCloser(string trimmedLine)
    {
        foreach (string closer in Closers)
        {
            if (LineStartsWith(trimmedLine, closer))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a trimmed line starts with a keyword
    /// with a non-identifier character (or end of string) following it.
    /// </summary>
    /// <param name="trimmedLine">The trimmed source line.</param>
    /// <param name="keyword">The keyword to match.</param>
    /// <returns><c>true</c> if the line starts with the keyword, otherwise <c>false</c>.</returns>
    private static bool LineStartsWith(string trimmedLine, string keyword)
    {
        if (!trimmedLine.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (trimmedLine.Length == keyword.Length)
        {
            return true;
        }

        char nextCharacter = trimmedLine[keyword.Length];
        return !char.IsLetterOrDigit(nextCharacter) &&
            nextCharacter != '_' &&
            nextCharacter != '-';
    }
}
