using BWHazel.TopsyTurvy.Analysis;

namespace BWHazel.TopsyTurvy.Tests;

/// <summary>
/// Tests for the <see cref="HoverMarkdownBuilder"/> class.
/// </summary>
public class HoverMarkdownBuilderTests
{
    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method produces the expected Markdown for a variable symbol.
    /// </summary>
    [Fact]
    public void Build_WithVariable_ReturnsVariableMarkdownWithType()
    {
        SymbolInfo info = new()
        {
            Name = "myVar",
            Kind = SymbolKind.Variable,
            TypeDisplayName = "PEER"
        };

        string result = HoverMarkdownBuilder.Build(info);

        Assert.Equal("**(variable)** `myVar` : PEER", result);
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method produces the expected Markdown for the implicit JUST SO variable.
    /// </summary>
    [Fact]
    public void Build_WithJustSo_ReturnsImplicitVariableMarkdown()
    {
        SymbolInfo info = new()
        {
            Name = "JUST SO",
            Kind = SymbolKind.Variable,
            TypeDisplayName = "implicit accumulator"
        };

        string result = HoverMarkdownBuilder.Build(info);

        Assert.Equal("**implicit variable** `JUST SO` — receives the result of the last expression", result);
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method produces the expected Markdown for the implicit JUST SO variable when in lower-case.
    /// </summary>
    [Fact]
    public void Build_WithJustSoLowercase_ReturnsImplicitVariableMarkdown()
    {
        SymbolInfo info = new()
        {
            Name = "just so",
            Kind = SymbolKind.Variable
        };

        string result = HoverMarkdownBuilder.Build(info);

        Assert.Contains("implicit variable", result);
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method produces the expected Markdown for a function symbol with parameters.
    /// </summary>
    [Fact]
    public void Build_WithFunctionWithParameters_ReturnsSignatureWithParameters()
    {
        SymbolInfo info = new()
        {
            Name = "greet",
            Kind = SymbolKind.Function,
            Parameters = ["salutation", "recipient"]
        };

        string result = HoverMarkdownBuilder.Build(info);

        Assert.Equal("**(function)** `greet`(salutation, recipient)", result);
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method produces the expected Markdown for a function symbol with an empty parameters list.
    /// </summary>
    [Fact]
    public void Build_WithFunctionWithEmptyParameters_ReturnsEmptyParentheses()
    {
        SymbolInfo info = new()
        {
            Name = "greet",
            Kind = SymbolKind.Function,
            Parameters = []
        };

        string result = HoverMarkdownBuilder.Build(info);
        
        Assert.Equal("**(function)** `greet`()", result);
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method produces the expected Markdown for a function symbol with a null parameters list.
    /// </summary>
    [Fact]
    public void Build_WithFunctionWithNullParameters_ReturnsEmptyParentheses()
    {
        SymbolInfo info = new()
        {
            Name = "greet",
            Kind = SymbolKind.Function,
            Parameters = null
        };

        string result = HoverMarkdownBuilder.Build(info);
        Assert.Equal("**(function)** `greet`()", result);
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method produces the expected Markdown for a parameter symbol.
    /// </summary>
    [Fact]
    public void Build_Parameter_ReturnsParameterMarkdown()
    {
        SymbolInfo info = new()
        {
            Name = "name",
            Kind = SymbolKind.Parameter
        };

        string result = HoverMarkdownBuilder.Build(info);
        
        Assert.Equal("**(parameter)** `name`", result);
    }
}
