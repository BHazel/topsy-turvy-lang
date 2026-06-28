using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Converts a <see cref="BlazorDiagram"/> built by <see cref="VisualGraphBuilder"/> back into a <see cref="ProgramNode"/> AST node.
/// </summary>
/// <remarks>
/// <para>
/// This converter uses the original AST nodes stored in <see cref="TopsyTurvyVisualNodeModel.AstNode"/> as the structural
/// backbone of the programme.  For each statement in the original programme, the converter finds the corresponding visual node
/// and reconstructs an updated statement using the node editable properties.
/// </para>
/// <para>
/// Complex block structures (conditional, loop, try-catch, switch, guard) are passed through unchanged because their bodies are
/// rendered as separate sub-graphs that are not wired into a single traversable flow chain: editing these is not currently
/// supported.  Only simple statements (declaration, assignment, print, input) and their leaf expressions (literal, identifier)
/// pick up changes from the visual model properties.
/// </para>
/// </remarks>
internal sealed class VisualGraphToAstConverter
{
    /// <summary>
    /// Converts the given diagram back into a <see cref="ProgramNode"/>.
    /// </summary>
    /// <param name="diagram">The diagram produced by <see cref="VisualGraphBuilder"/>.</param>
    /// <returns>A reconstructed <see cref="ProgramNode"/> with any edited properties applied.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the diagram contains no HARK! node or no original <see cref="ProgramNode"/>.</exception>
    public ProgramNode Convert(BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel harkNode = diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .First(node => node.Title == "HARK!" && node.Kind == VisualNodeKind.Program);

        ProgramNode originalProgramNode = (ProgramNode)harkNode.AstNode!;
        List<Statement> statements = [];
        foreach (Statement statement in originalProgramNode.Statements)
        {
            statements.Add(this.ReconstructStatement(statement, diagram));
        }

        string title = harkNode.SymbolIdentifierNodeName ?? originalProgramNode.Title;
        string? subtitle = string.IsNullOrEmpty(harkNode.LiteralValue)
            ? null
            : harkNode.LiteralValue;

        return new()
        {
            Title = title,
            Subtitle = subtitle,
            Statements = statements,
            Span = originalProgramNode.Span,
        };
    }

    /// <summary>
    /// Reconstructs an updated statement by finding the visual node that owns the original AST node.
    /// </summary>
    /// <param name="statement">The original statement from the parsed AST.</param>
    /// <param name="diagram">The diagram to search for the corresponding visual node.</param>
    /// <returns>The reconstructed statement, updated from visual model properties where supported.</returns>
    private Statement ReconstructStatement(Statement statement, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel? visualNode = diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .FirstOrDefault(node => ReferenceEquals(node.AstNode, statement));

        if (visualNode is null)
        {
            return statement;
        }

        return statement switch
        {
            DeclarationNode declaration => this.ReconstructDeclaration(declaration, visualNode, diagram),
            AssignmentNode assignment => this.ReconstructAssignment(assignment, visualNode, diagram),
            PrintNode print => this.ReconstructPrint(print, visualNode, diagram),
            InputNode input => ReconstructInput(input, visualNode),
            PrincipalBlockNode principalBlock => this.ReconstructPrincipalBlock(principalBlock, diagram),
            _ => statement,
        };
    }

    /// <summary>
    /// Reconstructs a <see cref="DeclarationNode"/> from updated visual model properties.
    /// </summary>
    /// <param name="originalDeclaration">The original declaration node.</param>
    /// <param name="visualNode">The visual node that owns this declaration.</param>
    /// <param name="diagram">The diagram used to reconstruct any initial-value expression.</param>
    /// <returns>The reconstructed declaration node.</returns>
    private DeclarationNode ReconstructDeclaration(DeclarationNode originalDeclaration, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        LiteralType type = visualNode.NodeLiteralType ?? originalDeclaration.Type;
        Expression? initialValue = originalDeclaration.InitialValue is not null
            ? this.ReconstructExpression(originalDeclaration.InitialValue, diagram)
            : null;

        return new()
        {
            Name = visualNode.SymbolIdentifierNodeName ?? originalDeclaration.Name,
            Type = type,
            IsConstant = visualNode.IsIdentifierConstant,
            InitialValue = initialValue,
            Span = originalDeclaration.Span,
        };
    }

    /// <summary>
    /// Reconstructs an <see cref="AssignmentNode"/> from updated visual model properties.
    /// </summary>
    /// <param name="originalAssignment">The original assignment node.</param>
    /// <param name="visualNode">The visual node that owns this assignment.</param>
    /// <param name="diagram">The diagram used to reconstruct the value expression.</param>
    /// <returns>The reconstructed assignment node.</returns>
    private AssignmentNode ReconstructAssignment(AssignmentNode originalAssignment, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new()
        {
            Target = visualNode.SymbolIdentifierNodeName ?? originalAssignment.Target,
            Value = this.ReconstructExpression(originalAssignment.Value, diagram),
            Span = originalAssignment.Span,
        };
    }

    /// <summary>
    /// Reconstructs a <see cref="PrintNode"/> from updated visual model properties.
    /// </summary>
    /// <param name="originalPrint">The original print node.</param>
    /// <param name="visualNode">The visual node that owns this print statement.</param>
    /// <param name="diagram">The diagram used to reconstruct the expression.</param>
    /// <returns>The reconstructed print node.</returns>
    private PrintNode ReconstructPrint(PrintNode originalPrint, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new()
        {
            Expression = this.ReconstructExpression(originalPrint.Expression, diagram),
            SuppressNewline = visualNode.PrintSuppressNewline,
            Span = originalPrint.Span,
        };
    }

    /// <summary>
    /// Reconstructs an <see cref="InputNode"/> from updated visual model properties.
    /// </summary>
    /// <param name="originalInput">The original input node.</param>
    /// <param name="visualNode">The visual node that owns this input statement.</param>
    /// <returns>The reconstructed input node.</returns>
    private static InputNode ReconstructInput(InputNode originalInput, TopsyTurvyVisualNodeModel visualNode)
    {
        return new()
        {
            Target = visualNode.SymbolIdentifierNodeName ?? originalInput.Target,
            Span = originalInput.Span,
        };
    }

    /// <summary>
    /// Reconstructs a <see cref="PrincipalBlockNode"/> by finding updated sidebar declaration visual nodes.
    /// </summary>
    /// <param name="originalPrincipalBlock">The original principal block node.</param>
    /// <param name="diagram">The diagram containing the sidebar declaration nodes.</param>
    /// <returns>The reconstructed principal block node.</returns>
    private PrincipalBlockNode ReconstructPrincipalBlock(PrincipalBlockNode originalPrincipalBlock, BlazorDiagram diagram)
    {
        List<Statement> declarations = [];
        foreach (Statement declaration in originalPrincipalBlock.Declarations)
        {
            declarations.Add(this.ReconstructStatement(declaration, diagram));
        }

        return new()
        {
            Declarations = declarations,
            Span = originalPrincipalBlock.Span,
        };
    }

    /// <summary>
    /// Reconstructs an expression by finding the visual node that owns the original AST expression node.
    /// </summary>
    /// <param name="originalExpression">The original expression from the AST.</param>
    /// <param name="diagram">The diagram to search for the corresponding visual node.</param>
    /// <returns>The reconstructed expression, updated from visual model properties where supported.</returns>
    private Expression ReconstructExpression(Expression originalExpression, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel? visualNode = diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .FirstOrDefault(node => ReferenceEquals(node.AstNode, originalExpression));

        if (visualNode is null)
        {
            return originalExpression;
        }

        return originalExpression switch
        {
            LiteralNode literal => ReconstructLiteral(literal, visualNode),
            IdentifierNode identifier => ReconstructIdentifier(identifier, visualNode),
            _ => originalExpression,
        };
    }

    /// <summary>
    /// Reconstructs a <see cref="LiteralNode"/> from updated visual model properties.
    /// </summary>
    /// <param name="originalLiteral">The original literal node.</param>
    /// <param name="visualNode">The visual node that owns this literal expression.</param>
    /// <returns>The reconstructed literal node.</returns>
    private static LiteralNode ReconstructLiteral(LiteralNode originalLiteral, TopsyTurvyVisualNodeModel visualNode)
    {
        LiteralType type = visualNode.NodeLiteralType ?? originalLiteral.Type;
        object? value = ParseLiteralValue(type, visualNode.LiteralValue);

        return new()
        {
            Type = type,
            Value = value,
            Span = originalLiteral.Span,
        };
    }

    /// <summary>
    /// Reconstructs an <see cref="IdentifierNode"/> from updated visual model properties.
    /// </summary>
    /// <param name="originalIdentifier">The original identifier node.</param>
    /// <param name="visualNode">The visual node that owns this identifier expression.</param>
    /// <returns>The reconstructed identifier node.</returns>
    private static IdentifierNode ReconstructIdentifier(IdentifierNode originalIdentifier, TopsyTurvyVisualNodeModel visualNode)
    {
        return new()
        {
            Name = visualNode.SymbolIdentifierNodeName ?? originalIdentifier.Name,
            Span = originalIdentifier.Span,
        };
    }

    /// <summary>
    /// Parses a literal value string into the appropriate CLR type for the given <see cref="LiteralType"/>.
    /// </summary>
    /// <param name="type">The Topsy Turvy literal type.</param>
    /// <param name="value">The string representation of the value, as stored on the visual node.</param>
    /// <returns>The parsed value, or the original string if parsing fails.</returns>
    private static object? ParseLiteralValue(LiteralType type, string? value)
    {
        if (value is null)
        {
            return null;
        }

        return type switch
        {
            LiteralType.Null => null,
            LiteralType.Integer => int.TryParse(value, out int i) ? i : (object?)value,
            LiteralType.Long => long.TryParse(value, out long l) ? l : (object?)value,
            LiteralType.Short => short.TryParse(value, out short s) ? s : (object?)value,
            LiteralType.SignedByte => sbyte.TryParse(value, out sbyte sb) ? sb : (object?)value,
            LiteralType.UnsignedInteger => uint.TryParse(value, out uint ui) ? ui : (object?)value,
            LiteralType.UnsignedLong => ulong.TryParse(value, out ulong ul) ? ul : (object?)value,
            LiteralType.Double => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ? d : (object?)value,
            LiteralType.Single => float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float f) ? f : (object?)value,
            LiteralType.Boolean => string.Equals(value, "VERITY", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "True", StringComparison.OrdinalIgnoreCase)
                ? true
                : (object?)false,
            _ => value,
        };
    }
}
