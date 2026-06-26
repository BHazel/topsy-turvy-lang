using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Tests.Analysis;

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

        result.ShouldBe("**(variable)** `myVar` : PEER");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build"/> method produces the expected Markdown for a function symbol with typed parameters.
    /// </summary>
    [Fact]
    public void Build_WithFunctionWithParameters_ReturnsSignatureWithTypedParameters()
    {
        SymbolInfo info = new()
        {
            Name = "greet",
            Kind = SymbolKind.Function,
            TypedParameters =
            [
                new TypedParameter("salutation", LiteralType.String, new(new(0, 0), new(0, 0))),
                new TypedParameter("recipient", LiteralType.String, new(new(0, 0), new(0, 0)))
            ]
        };

        string result = HoverMarkdownBuilder.Build(info);

        result.ShouldBe("**(function)** `greet`(salutation AS A YARN, recipient AS A YARN)");
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
            TypedParameters = []
        };

        string result = HoverMarkdownBuilder.Build(info);

        result.ShouldBe("**(function)** `greet`()");
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
            TypedParameters = null
        };

        string result = HoverMarkdownBuilder.Build(info);
        result.ShouldBe("**(function)** `greet`()");
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

        result.ShouldBe("**(parameter)** `name`");
    }
}
