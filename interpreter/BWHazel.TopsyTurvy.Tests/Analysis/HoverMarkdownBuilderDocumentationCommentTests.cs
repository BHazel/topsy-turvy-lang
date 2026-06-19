using System.Collections.Generic;
using BWHazel.TopsyTurvy.Analysis;

namespace BWHazel.TopsyTurvy.Tests.Analysis;

/// <summary>
/// Tests for documentation comment rendering in the <see cref="HoverMarkdownBuilder"/> class.
/// </summary>
public class HoverMarkdownBuilderDocumentationCommentTests
{
    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method returns the signature only when documentation is null.
    /// </summary>
    [Fact]
    public void Build_WithNoDocumentation_ReturnsSignatureOnly()
    {
        SymbolInfo symbolInfo = new()
        {
            Name = "Numbers",
            Kind = SymbolKind.Variable,
            TypeDisplayName = "PEER"
        };

        string result = HoverMarkdownBuilder.Build(symbolInfo);

        result.ShouldBe("**(variable)** `Numbers` : PEER");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method includes the summary when a LEGEND tag is present.
    /// </summary>
    [Fact]
    public void Build_WithSummary_IncludesSummaryInOutput()
    {
        SymbolInfo symbolInfo = new()
        {
            Name = "Numbers",
            Kind = SymbolKind.Variable,
            TypeDisplayName = "PEER",
            Documentation = new DocumentationComment()
            {
                Summary = "Holds the numbers."
            }
        };

        string result = HoverMarkdownBuilder.Build(symbolInfo);

        result.ShouldContain("Holds the numbers.");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method includes the deprecation notice when IsDeprecated is true.
    /// </summary>
    [Fact]
    public void Build_WithIsDeprecated_IncludesDeprecationNotice()
    {
        SymbolInfo symbolInfo = new()
        {
            Name = "Numbers",
            Kind = SymbolKind.Variable,
            TypeDisplayName = "PEER",
            Documentation = new DocumentationComment()
            {
                IsDeprecated = true,
                DeprecationMessage = "Use NewNumbers instead."
            }
        };

        string result = HoverMarkdownBuilder.Build(symbolInfo);

        result.ShouldContain("~~Deprecated~~");
        result.ShouldContain("Use NewNumbers instead.");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method includes the deprecation notice without a message when DeprecationMessage is null.
    /// </summary>
    [Fact]
    public void Build_WithIsDeprecatedAndNoMessage_IncludesDeprecationNoticeWithoutMessage()
    {
        SymbolInfo symbolInfo = new()
        {
            Name = "Numbers",
            Kind = SymbolKind.Variable,
            TypeDisplayName = "PEER",
            Documentation = new DocumentationComment()
            {
                IsDeprecated = true
            }
        };

        string result = HoverMarkdownBuilder.Build(symbolInfo);

        result.ShouldContain("~~Deprecated~~");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method includes parameter descriptions when ARTICLE tags are present.
    /// </summary>
    [Fact]
    public void Build_WithParameters_IncludesParameterList()
    {
        SymbolInfo symbolInfo = new()
        {
            Name = "SumRange",
            Kind = SymbolKind.Function,
            Parameters = ["Start", "End"],
            Documentation = new DocumentationComment()
            {
                Parameters = new Dictionary<string, (string Type, string Description)>()
                {
                    ["Start"] = ("PEER", "The starting number."),
                    ["End"] = ("PEER", "The ending number.")
                }
            }
        };

        string result = HoverMarkdownBuilder.Build(symbolInfo);

        result.ShouldContain("**Parameters:**");
        result.ShouldContain("`Start`");
        result.ShouldContain("The starting number.");
        result.ShouldContain("`End`");
        result.ShouldContain("The ending number.");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method includes the return value when a CONSEQUENCE tag is present.
    /// </summary>
    [Fact]
    public void Build_WithReturnValue_IncludesReturnSection()
    {
        SymbolInfo symbolInfo = new()
        {
            Name = "SumRange",
            Kind = SymbolKind.Function,
            Parameters = ["Start", "End"],
            Documentation = new DocumentationComment()
            {
                ReturnValue = ("PEER", "The total sum.")
            }
        };

        string result = HoverMarkdownBuilder.Build(symbolInfo);

        result.ShouldContain("**Returns:**");
        result.ShouldContain("`PEER`");
        result.ShouldContain("The total sum.");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method includes thrown value descriptions when CURSES tags are present.
    /// </summary>
    [Fact]
    public void Build_WithExceptions_IncludesThrowsSection()
    {
        SymbolInfo symbolInfo = new()
        {
            Name = "SumRange",
            Kind = SymbolKind.Function,
            Parameters = ["Start", "End"],
            Documentation = new DocumentationComment()
            {
                Exceptions = [("SameValues", "DECREE", "Thrown if Start and End are the same.")]
            }
        };

        string result = HoverMarkdownBuilder.Build(symbolInfo);

        result.ShouldContain("**Throws:**");
        result.ShouldContain("`SameValues`");
        result.ShouldContain("Thrown if Start and End are the same.");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method includes code examples when a CHORUS tag is present.
    /// </summary>
    [Fact]
    public void Build_WithExample_IncludesCodeBlock()
    {
        SymbolInfo symbolInfo = new()
        {
            Name = "SumRange",
            Kind = SymbolKind.Function,
            Parameters = ["Start", "End"],
            Documentation = new DocumentationComment()
            {
                Examples = ["SUMMON SumRange WITH 1 AND 10 IF YOU PLEASE."]
            }
        };

        string result = HoverMarkdownBuilder.Build(symbolInfo);

        result.ShouldContain("**Example:**");
        result.ShouldContain("```topsy");
        result.ShouldContain("SUMMON SumRange WITH 1 AND 10 IF YOU PLEASE.");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method includes see-also references when ENSEMBLE tags are present.
    /// </summary>
    [Fact]
    public void Build_WithSeeAlso_IncludesSeeAlsoSection()
    {
        SymbolInfo symbolInfo = new()
        {
            Name = "SumRange",
            Kind = SymbolKind.Function,
            Parameters = ["Start", "End"],
            Documentation = new DocumentationComment()
            {
                SeeAlso = ["OtherFunc", "AnotherFunc"]
            }
        };

        string result = HoverMarkdownBuilder.Build(symbolInfo);

        result.ShouldContain("**See also:**");
        result.ShouldContain("OtherFunc");
        result.ShouldContain("AnotherFunc");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method still starts with the symbol signature when documentation is present.
    /// </summary>
    [Fact]
    public void Build_WithDocumentation_StartsWithSignature()
    {
        SymbolInfo symbolInfo = new()
        {
            Name = "SumRange",
            Kind = SymbolKind.Function,
            Parameters = ["Start", "End"],
            Documentation = new DocumentationComment()
            {
                Summary = "Sums a range."
            }
        };

        string result = HoverMarkdownBuilder.Build(symbolInfo);

        result.ShouldStartWith("**(function)** `SumRange`(Start, End)");
    }
}
