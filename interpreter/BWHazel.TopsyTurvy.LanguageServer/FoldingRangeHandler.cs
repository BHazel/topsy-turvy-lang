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
/// Handles <c>textDocument/foldingRange</c> requests.
/// </summary>
/// <remarks>
/// Uses a stack-based single-pass line scan to identify foldable regions.
/// </remarks>
public class FoldingRangeHandler : FoldingRangeHandlerBase
{
    private readonly DocumentStateManager documentStateManager;

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
        "END OF ASIDE.)",
    ];

    /// <summary>
    /// Initialises a new instance of the <see cref="FoldingRangeHandler"/> class.
    /// </summary>
    /// <param name="documentStateManager">The manager providing per-document symbol state.</param>
    public FoldingRangeHandler(DocumentStateManager documentStateManager)
    {
        this.documentStateManager = documentStateManager;
    }

    /// <inheritdoc/>
    protected override FoldingRangeRegistrationOptions CreateRegistrationOptions(
        FoldingRangeCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageServerConstants.LanguageId)
        };

    /// <inheritdoc/>
    public override Task<Container<FoldingRange>?> Handle(
        FoldingRangeRequestParam request, CancellationToken cancellationToken)
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
                string trimmed = lines[i].Trim();

                if (IsCloser(trimmed))
                {
                    if (stack.Count > 0)
                    {
                        (int startLine, string kind) = stack.Pop();
                        int endLine = i - 1;
                        if (endLine > startLine)
                        {
                            ranges.Add(new FoldingRange
                            {
                                StartLine = startLine,
                                EndLine = endLine,
                                Kind = kind
                            });
                        }
                    }

                    continue;
                }

                string? openerKind = FindOpenerKind(trimmed);
                if (openerKind is not null)
                {
                    stack.Push((i, openerKind));
                }
            }

            return Task.FromResult<Container<FoldingRange>?>(new Container<FoldingRange>(ranges));
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

        char next = trimmedLine[keyword.Length];
        return !char.IsLetterOrDigit(next) && next != '_' && next != '-';
    }
}
