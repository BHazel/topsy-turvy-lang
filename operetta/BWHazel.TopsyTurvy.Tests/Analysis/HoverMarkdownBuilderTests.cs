using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Tests.Analysis;

/// <summary>
/// Tests for the <see cref="HoverMarkdownBuilder"/> class.
/// </summary>
public class HoverMarkdownBuilderTests
{
    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build(SymbolInfo)"/> method produces the expected Markdown for a variable symbol.
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
    /// Tests that the <see cref="HoverMarkdownBuilder.Build(SymbolInfo)"/> method produces the expected Markdown for a function symbol with typed parameters.
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
    /// Tests that the <see cref="HoverMarkdownBuilder.Build(SymbolInfo)"/> method produces the expected Markdown for a function symbol with an empty parameters list.
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
    /// Tests that the <see cref="HoverMarkdownBuilder.Build(SymbolInfo)"/> method produces the expected Markdown for a function symbol with a null parameters list.
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
    /// Tests that the <see cref="HoverMarkdownBuilder.Build(System.Collections.Generic.IReadOnlyList{SymbolInfo})"/>
    /// method renders every overload when there is more than one.
    /// </summary>
    [Fact]
    public void Build_WithMultipleOverloads_RendersEveryOverload()
    {
        SymbolInfo first = new() { Name = "describe", Kind = SymbolKind.Function, TypedParameters = [new("value", LiteralType.Integer, new(new(0, 0), new(0, 0)))], DefinitionLine = 4 };
        SymbolInfo second = new() { Name = "describe", Kind = SymbolKind.Function, TypedParameters = [new("value", LiteralType.String, new(new(0, 0), new(0, 0)))], DefinitionLine = 8 };

        string result = HoverMarkdownBuilder.Build([first, second]);

        result.ShouldContain("value AS A PEER");
        result.ShouldContain("value AS A YARN");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build(System.Collections.Generic.IReadOnlyList{SymbolInfo}, int)"/>
    /// method renders only the overload declared on the hovered line, when the cursor is directly on one.
    /// </summary>
    [Fact]
    public void Build_WithHoveredLineOnSpecificOverload_RendersOnlyThatOverload()
    {
        SymbolInfo first = new() { Name = "describe", Kind = SymbolKind.Function, TypedParameters = [new("value", LiteralType.Integer, new(new(0, 0), new(0, 0)))], DefinitionLine = 4 };
        SymbolInfo second = new() { Name = "describe", Kind = SymbolKind.Function, TypedParameters = [new("value", LiteralType.String, new(new(0, 0), new(0, 0)))], DefinitionLine = 8 };

        string result = HoverMarkdownBuilder.Build([first, second], hoveredLine: 3);

        result.ShouldContain("value AS A PEER");
        result.ShouldNotContain("value AS A YARN");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build(System.Collections.Generic.IReadOnlyList{SymbolInfo}, int)"/>
    /// method renders every overload when the hovered line does not match any specific overload declaration.
    /// </summary>
    [Fact]
    public void Build_WithHoveredLineNotOnAnyDeclaration_RendersEveryOverload()
    {
        SymbolInfo first = new() { Name = "describe", Kind = SymbolKind.Function, TypedParameters = [new("value", LiteralType.Integer, new(new(0, 0), new(0, 0)))], DefinitionLine = 4 };
        SymbolInfo second = new() { Name = "describe", Kind = SymbolKind.Function, TypedParameters = [new("value", LiteralType.String, new(new(0, 0), new(0, 0)))], DefinitionLine = 8 };

        string result = HoverMarkdownBuilder.Build([first, second], hoveredLine: 10);

        result.ShouldContain("value AS A PEER");
        result.ShouldContain("value AS A YARN");
    }

    /// <summary>
    /// Tests that the <see cref="HoverMarkdownBuilder.Build(SymbolInfo)"/> method produces the expected Markdown for a parameter symbol.
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
