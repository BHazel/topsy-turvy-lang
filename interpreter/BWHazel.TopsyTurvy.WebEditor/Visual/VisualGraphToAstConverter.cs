using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Converts a <see cref="BlazorDiagram"/> built by <see cref="VisualGraphBuilder"/> back into a <see cref="ProgramNode"/> AST.
/// </summary>
/// <remarks>
/// <para>
/// Uses edge-walking to reconstruct expressions by following Data Out to DataIn links from each statement Data In ports,
/// then recursively reconstructs the expression subgraph from the connected source nodes.
/// </para>
/// <para>
/// The structural backbone (which statements exist and in what order) comes from the original AST stored in
/// <see cref="TopsyTurvyVisualNodeModel.AstNode"/> while all editable properties and all expression trees are
/// reconstructed from the live visual model and its port connections.
/// </para>
/// </remarks>
internal sealed class VisualGraphToAstConverter
{
    /// <summary>
    /// Converts the given diagram back into a <see cref="ProgramNode"/>.
    /// </summary>
    /// <param name="diagram">The diagram produced by <see cref="VisualGraphBuilder"/>.</param>
    /// <returns>A reconstructed <see cref="ProgramNode"/> with all visual model changes applied.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the diagram contains no HARK! node.</exception>
    public ProgramNode Convert(BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel harkNode = diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .First(node => node.Title == "HARK!" && node.Kind == VisualNodeKind.Program);

        ProgramNode originalProgram = (ProgramNode)harkNode.AstNode!;
        List<Statement> statements = originalProgram.Statements
            .Select(statement => this.ReconstructStatement(statement, diagram))
            .ToList();

        string title = harkNode.SymbolIdentifierNodeName ?? originalProgram.Title;
        string? subtitle = string.IsNullOrEmpty(harkNode.LiteralValue)
            ? null
            : harkNode.LiteralValue;

        return new()
        {
            Title = title,
            Subtitle = subtitle,
            Statements = statements,
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// A zero-position span used for all reconstructed nodes.
    /// </summary>
    /// <remarks>
    /// Diagnostics are not needed on converter output.
    /// </remarks>
    private static SourceSpan EmptySpan => new(new(0, 0), new(0, 0));

    /// <summary>
    /// Reconstructs a statement from the original AST node and its corresponding visual node in the diagram.
    /// </summary>
    /// <param name="statement">The original AST statement node.</param>
    /// <param name="diagram">The diagram containing the visual representation of the statement.</param>
    /// <returns>The reconstructed statement node.</returns>
    private Statement ReconstructStatement(Statement statement, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel? visualNode = FindNodeByAst(statement, diagram);

        return statement switch
        {
            PrincipalBlockNode principalBlock => this.ReconstructPrincipalBlock(principalBlock, diagram),
            FunctionDefinitionNode functionDefinition when visualNode is not null => this.ReconstructFunctionDefinition(functionDefinition, visualNode, diagram),
            DeclarationNode declaration when visualNode is not null => this.ReconstructDeclaration(declaration, visualNode, diagram),
            ArrayDeclarationNode arrayDeclaration when visualNode is not null => this.ReconstructArrayDeclaration(arrayDeclaration, visualNode, diagram),
            AssignmentNode assignment when visualNode is not null => this.ReconstructAssignment(assignment, visualNode, diagram),
            ArrayElementAssignmentNode arrayElementAssignment when visualNode is not null => this.ReconstructArrayElementAssignment(arrayElementAssignment, visualNode, diagram),
            PrintNode print when visualNode is not null => this.ReconstructPrint(print, visualNode, diagram),
            InputNode input when visualNode is not null => ReconstructInput(input, visualNode),
            ConditionalNode conditional when visualNode is not null => this.ReconstructConditional(conditional, visualNode, diagram),
            LoopNode loop when visualNode is not null => this.ReconstructLoop(loop, visualNode, diagram),
            BreakNode => new BreakNode { Span = EmptySpan },
            ContinueNode => new ContinueNode { Span = EmptySpan },
            ReturnNode returnNode when visualNode is not null => this.ReconstructReturn(returnNode, visualNode, diagram),
            ThrowNode throwNode when visualNode is not null => this.ReconstructThrow(throwNode, visualNode, diagram),
            TryCatchNode tryCatch when visualNode is not null => this.ReconstructTryCatch(tryCatch, visualNode, diagram),
            SwitchNode switchNode when visualNode is not null => this.ReconstructSwitch(switchNode, visualNode, diagram),
            ImportNode import when visualNode is not null => ReconstructImport(import, visualNode),
            GuardNode guard when visualNode is not null => this.ReconstructGuard(guard, visualNode, diagram),
            AssertNode assert when visualNode is not null => this.ReconstructAssert(assert, visualNode, diagram),
            ExpressionStatement expressionStatement when visualNode is not null => this.ReconstructExpressionStatement(expressionStatement, visualNode, diagram),
            _ => statement,
        };
    }

    /// <summary>
    /// Reconstructs a principal block statement from the original AST node and the diagram.
    /// </summary>
    /// <param name="original">The original principal block node.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed principal block node.</returns>
    private PrincipalBlockNode ReconstructPrincipalBlock(PrincipalBlockNode original, BlazorDiagram diagram)
    {
        return new()
        {
            Declarations = original.Declarations
                .Select(declaration => this.ReconstructStatement(declaration, diagram))
                .ToList()
                .AsReadOnly(),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs a function definition statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original function definition node.</param>
    /// <param name="visualNode">The visual node corresponding to the function definition.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed function definition node.</returns>
    private FunctionDefinitionNode ReconstructFunctionDefinition(FunctionDefinitionNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new()
        {
            Name = visualNode.SymbolIdentifierNodeName ?? original.Name,
            Parameters = original.Parameters,
            ReturnType = original.ReturnType,
            Body = ReconstructBodyStatements(original.Body, diagram),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs a variable declaration statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original variable declaration node.</param>
    /// <param name="visualNode">The visual node corresponding to the variable declaration.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed variable declaration node.</returns>
    private DeclarationNode ReconstructDeclaration(DeclarationNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        Expression? initialValue = original.InitialValue is not null
            ? this.GetExpressionFromDataIn(visualNode, "Value", diagram) ?? this.ReconstructExpressionFromAst(original.InitialValue, diagram)
            : null;

        return new()
        {
            Name = visualNode.SymbolIdentifierNodeName ?? original.Name,
            Type = visualNode.NodeLiteralType ?? original.Type,
            IsConstant = visualNode.IsIdentifierConstant,
            InitialValue = initialValue,
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs an array declaration statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original array declaration node.</param>
    /// <param name="visualNode">The visual node corresponding to the array declaration.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed array declaration node.</returns>
    private ArrayDeclarationNode ReconstructArrayDeclaration(ArrayDeclarationNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        List<Expression> values = [];
        for (int i = 0; i < original.InitialValues.Count; i++)
        {
            Expression? expression = this.GetExpressionFromDataIn(visualNode, $"val {i + 1}", diagram)
                ?? this.ReconstructExpressionFromAst(original.InitialValues[i], diagram);
            
            values.Add(expression);
        }

        return new()
        {
            Name = original.Name,
            ElementType = original.ElementType,
            Size = original.Size,
            IsConstant = original.IsConstant,
            InitialValues = values.AsReadOnly(),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs an assignment statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original assignment node.</param>
    /// <param name="visualNode">The visual node corresponding to the assignment.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed assignment node.</returns>
    private AssignmentNode ReconstructAssignment(AssignmentNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new()
        {
            Target = visualNode.SymbolIdentifierNodeName ?? original.Target,
            Value = this.GetExpressionFromDataIn(visualNode, "Value", diagram)
                ?? this.ReconstructExpressionFromAst(original.Value, diagram),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs an array element assignment statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original array element assignment node.</param>
    /// <param name="visualNode">The visual node corresponding to the array element assignment.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed array element assignment node.</returns>
    private ArrayElementAssignmentNode ReconstructArrayElementAssignment(ArrayElementAssignmentNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new()
        {
            ArrayName = original.ArrayName,
            Index = this.GetExpressionFromDataIn(visualNode, "Index", diagram)
                ?? this.ReconstructExpressionFromAst(original.Index, diagram),
            Value = this.GetExpressionFromDataIn(visualNode, "Value", diagram)
                ?? this.ReconstructExpressionFromAst(original.Value, diagram),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs a print statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original print node.</param>
    /// <param name="visualNode">The visual node corresponding to the print statement.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed print node.</returns>
    private PrintNode ReconstructPrint(PrintNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new()
        {
            Expression = this.GetExpressionFromDataIn(visualNode, "Expr", diagram)
                ?? this.ReconstructExpressionFromAst(original.Expression, diagram),
            SuppressNewline = visualNode.PrintSuppressNewline,
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs an input statement from the original AST node and its corresponding visual node.
    /// </summary>
    /// <param name="original">The original input node.</param>
    /// <param name="visualNode">The visual node corresponding to the input statement.</param>
    /// <returns>A reconstructed input node.</returns>
    private static InputNode ReconstructInput(InputNode original, TopsyTurvyVisualNodeModel visualNode)
    {
        return new()
        {
            Target = visualNode.SymbolIdentifierNodeName ?? original.Target,
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs a conditional statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original conditional node.</param>
    /// <param name="visualNode">The visual node corresponding to the conditional statement.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed conditional node.</returns>
    private ConditionalNode ReconstructConditional(ConditionalNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        Expression condition = this.GetExpressionFromDataIn(visualNode, "Cond", diagram)
            ?? this.ReconstructExpressionFromAst(original.Condition, diagram);

        List<Statement> trueBlock = ReconstructBodyStatements(original.TrueBlock, diagram);

        List<ElseIfBranch> elseIfBlocks = [];
        for (int i = 0; i < original.ElseIfs.Count; i++)
        {
            TopsyTurvyVisualNodeModel? headerNode = GetBranchFlowTarget(visualNode, $"Else If {i + 1}");
            Expression elseIfCond = headerNode is not null
                ? this.GetExpressionFromDataIn(headerNode, "Cond", diagram) ?? this.ReconstructExpressionFromAst(original.ElseIfs[i].Condition, diagram)
                : this.ReconstructExpressionFromAst(original.ElseIfs[i].Condition, diagram);

            elseIfBlocks.Add(new ElseIfBranch(elseIfCond, ReconstructBodyStatements(original.ElseIfs[i].Block, diagram)));
        }

        return new()
        {
            Condition = condition,
            TrueBlock = trueBlock,
            ElseIfs = elseIfBlocks.AsReadOnly(),
            ElseBlock = ReconstructBodyStatements(original.ElseBlock, diagram),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs a loop statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original loop node.</param>
    /// <param name="visualNode">The visual node corresponding to the loop statement.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed loop node.</returns>
    private LoopNode ReconstructLoop(LoopNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        Expression? condition = original.Condition is not null
            ? this.GetExpressionFromDataIn(visualNode, "Cond", diagram) ?? this.ReconstructExpressionFromAst(original.Condition, diagram)
            : null;

        return new()
        {
            Label = original.Label,
            Type = original.Type,
            LoopVariable = original.LoopVariable,
            Condition = condition,
            Body = ReconstructBodyStatements(original.Body, diagram),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs a return statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original return node.</param>
    /// <param name="visualNode">The visual node corresponding to the return statement.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed return node.</returns>
    private ReturnNode ReconstructReturn(ReturnNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        Expression? value = original.Value is not null
            ? this.GetExpressionFromDataIn(visualNode, "Value", diagram) ?? this.ReconstructExpressionFromAst(original.Value, diagram)
            : null;

        return new()
        {
            Value = value,
            Span = EmptySpan
        };
    }

    /// <summary>
    /// Reconstructs a throw statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original throw node.</param>
    /// <param name="visualNode">The visual node corresponding to the throw statement.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed throw node.</returns>
    private ThrowNode ReconstructThrow(ThrowNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new()
        {
            Value = this.GetExpressionFromDataIn(visualNode, "Value", diagram)
                ?? this.ReconstructExpressionFromAst(original.Value, diagram),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs a try-catch statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original try-catch node.</param>
    /// <param name="visualNode">The visual node corresponding to the try-catch statement.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed try-catch node.</returns>
    private TryCatchNode ReconstructTryCatch(TryCatchNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new()
        {
            Operation = this.GetExpressionFromDataIn(visualNode, "Op", diagram)
                ?? this.ReconstructExpressionFromAst(original.Operation, diagram),
            CaughtValueName = visualNode.SymbolIdentifierNodeName ?? original.CaughtValueName,
            SuccessBlock = ReconstructBodyStatements(original.SuccessBlock, diagram),
            ExceptionBlock = ReconstructBodyStatements(original.ExceptionBlock, diagram),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs a switch statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original switch node.</param>
    /// <param name="visualNode">The visual node corresponding to the switch statement.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed switch node.</returns>
    private SwitchNode ReconstructSwitch(SwitchNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        Expression expression = this.GetExpressionFromDataIn(visualNode, "Expr", diagram)
            ?? this.ReconstructExpressionFromAst(original.Expression, diagram);

        List<SwitchCase> cases = [.. original.Cases.Select(switchCase => new SwitchCase(switchCase.Literal, ReconstructBodyStatements(switchCase.Block, diagram)))];

        return new()
        {
            Expression = expression,
            Cases = cases.AsReadOnly(),
            DefaultBlock = ReconstructBodyStatements(original.DefaultBlock, diagram),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs an import statement from the original AST node and its corresponding visual node.
    /// </summary>
    /// <param name="original">The original import node.</param>
    /// <param name="visualNode">The visual node corresponding to the import statement.</param>
    /// <returns>A reconstructed import node.</returns>
    private static ImportNode ReconstructImport(ImportNode original, TopsyTurvyVisualNodeModel visualNode)
    {
        return new()
        {
            FilePath = visualNode.SymbolIdentifierNodeName ?? original.FilePath,
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs a guard statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original guard node.</param>
    /// <param name="visualNode">The visual node corresponding to the guard statement.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed guard node.</returns>
    private GuardNode ReconstructGuard(GuardNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new()
        {
            Condition = this.GetExpressionFromDataIn(visualNode, "Cond", diagram)
                ?? this.ReconstructExpressionFromAst(original.Condition, diagram),
            ElseBlock = ReconstructBodyStatements(original.ElseBlock, diagram),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs an assert statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original assert node.</param>
    /// <param name="visualNode">The visual node corresponding to the assert statement.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed assert node.</returns>
    private AssertNode ReconstructAssert(AssertNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new()
        {
            Condition = this.GetExpressionFromDataIn(visualNode, "Cond", diagram)
                ?? this.ReconstructExpressionFromAst(original.Condition, diagram),
            ErrorMessage = this.GetExpressionFromDataIn(visualNode, "Msg", diagram)
                ?? this.ReconstructExpressionFromAst(original.ErrorMessage, diagram),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs an expression statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original expression statement node.</param>
    /// <param name="visualNode">The visual node corresponding to the expression statement.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed expression statement node.</returns>
    private ExpressionStatement ReconstructExpressionStatement(ExpressionStatement original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new ExpressionStatement
        {
            Expression = this.GetExpressionFromDataIn(visualNode, "Expr", diagram)
                ?? this.ReconstructExpressionFromAst(original.Expression, diagram),
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs the body statements of a block statement by recursively reconstructing each statement in the original AST.
    /// </summary>
    /// <param name="statements">The original list of statements in the block.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A list of reconstructed statements.</returns>
    private static List<Statement> ReconstructBodyStatements(IReadOnlyList<Statement> statements, BlazorDiagram diagram)
    {
        VisualGraphToAstConverter converter = new();
        return [.. statements.Select(statement => converter.ReconstructStatement(statement, diagram))];
    }

    /// <summary>
    /// Gets the expression connected to a Data In port.
    /// </summary>
    /// <param name="visualNode">The visual node containing the Data In port.</param>
    /// <param name="portLabel">The label of the Data In port.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <remarks>
    /// Finds the Data In port with <paramref name="portLabel"/> on <paramref name="visualNode"/>, follows its
    /// incoming link to the source node and recursively reconstructs the expression from that node.
    /// Returns <c>null</c> when the port has no incoming link.
    /// </remarks>
    /// <returns>The reconstructed expression, or <c>null</c> if the port is disconnected.</returns>
    private Expression? GetExpressionFromDataIn(TopsyTurvyVisualNodeModel visualNode, string portLabel, BlazorDiagram diagram)
    {
        TopsyTurvyVisualPortModel? dataInPort = visualNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .FirstOrDefault(port => port.Role == VisualPortRole.DataIn && port.Label == portLabel);

        if (dataInPort is null)
        {
            return null;
        }

        TopsyTurvyVisualNodeModel? sourceNode = GetExpressionSourceNode(dataInPort);
        return sourceNode is not null
            ? this.ReconstructExpression(sourceNode, diagram)
            : null;
    }

    /// <summary>
    /// Reconstructs an expression from the given visual expression node.
    /// </summary>
    /// <remarks>
    /// Dispatches on <c>StatementType</c> and falls back to the original AST expression if <c>StatementType</c> is unknown.
    /// </remarks>
    private Expression ReconstructExpression(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        return (node.StatementType ?? string.Empty) switch
        {
            "LiteralNode" => ReconstructLiteralFromNode(node),
            "IdentifierNode" => ReconstructIdentifierFromNode(node),
            "OperatorNode" => this.ReconstructOperatorFromNode(node, diagram),
            "TernaryNode" => this.ReconstructTernaryFromNode(node, diagram),
            "ArrayIndexNode" => this.ReconstructArrayIndexFromNode(node, diagram),
            "ArrayLengthNode" => ReconstructArrayLengthFromNode(node),
            "ExpressionCastNode" => this.ReconstructExpressionCastFromNode(node, diagram),
            "SummonNode" => this.ReconstructSummonFromNode(node, diagram),
            _ => node.AstNode as Expression ?? new LiteralNode { Type = LiteralType.Null, Value = null, Span = EmptySpan },
        };
    }

    /// <summary>
    /// Reconstructs an expression from the original AST node
    /// </summary>
    /// <param name="original">The original AST expression node.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>The reconstructed expression node.</returns>
    private Expression ReconstructExpressionFromAst(Expression original, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel? visualNode = FindNodeByAst(original, diagram);
        return visualNode is not null
            ? this.ReconstructExpression(visualNode, diagram)
            : original;
    }

    /// <summary>
    /// Reconstructs a literal expression from the given visual literal node.
    /// </summary>
    /// <param name="visualNode">The visual literal node.</param>
    /// <returns>The reconstructed literal expression node.</returns>
    private static LiteralNode ReconstructLiteralFromNode(TopsyTurvyVisualNodeModel visualNode)
    {
        LiteralType type = visualNode.NodeLiteralType ?? LiteralType.String;
        object? value = ParseLiteralValue(type, visualNode.LiteralValue);

        return new()
        {
            Type = type,
            Value = value,
            Span = EmptySpan
        };
    }

    /// <summary>
    /// Reconstructs an identifier expression from the given visual identifier node.
    /// </summary>
    /// <param name="visualNode">The visual identifier node.</param>
    /// <returns>The reconstructed identifier expression node.</returns>
    private static IdentifierNode ReconstructIdentifierFromNode(TopsyTurvyVisualNodeModel visualNode)
    {
        return new()
        {
            Name = visualNode.SymbolIdentifierNodeName ?? visualNode.Title ?? string.Empty,
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs an operator expression from the given visual operator node.
    /// </summary>
    /// <param name="visualNode">The visual operator node.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>The reconstructed operator expression node.</returns>
    private PrefixExpressionNode ReconstructOperatorFromNode(TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        if (!VisualOperatorMaps.TitleToOperator.TryGetValue(visualNode.Title ?? string.Empty, out Operator theOperator))
        {
            theOperator = Operator.Sum;
        }

        List<TopsyTurvyVisualPortModel> argumentPorts = [.. visualNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .Where(port => port.Role == VisualPortRole.DataIn)
            .OrderBy(port => port.Label)];

        List<Expression> arguments = [.. argumentPorts
            .Select(port =>
            {
                TopsyTurvyVisualNodeModel? src = GetExpressionSourceNode(port);
                return src is not null ? this.ReconstructExpression(src, diagram) : (Expression?)null;
            })
            .Where(expression => expression is not null)
            .Select(expression => expression!)];

        return new()
        {
            Operator = theOperator,
            Arguments = arguments.AsReadOnly(),
            Span = EmptySpan
        };
    }

    /// <summary>
    /// Reconstructs a ternary expression from the given visual ternary node.
    /// </summary>
    /// <param name="visualNode">The visual ternary node.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>The reconstructed ternary expression node.</returns>
    private TernaryExpressionNode ReconstructTernaryFromNode(TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        Expression condition = this.GetExpressionFromDataIn(visualNode, "Cond", diagram) ?? Fallback();
        Expression trueValue = this.GetExpressionFromDataIn(visualNode, "True", diagram) ?? Fallback();
        Expression falseValue = this.GetExpressionFromDataIn(visualNode, "False", diagram) ?? Fallback();

        return new()
        {
            Condition = condition,
            TrueValue = trueValue,
            FalseValue = falseValue,
            Span = EmptySpan
        };
    }

    /// <summary>
    /// Reconstructs an array index expression from the given visual array index node.
    /// </summary>
    /// <param name="visualNode">The visual array index node.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>The reconstructed array index expression node.</returns>
    private ArrayIndexNode ReconstructArrayIndexFromNode(TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        Expression arrayIndex = this.GetExpressionFromDataIn(visualNode, "Index", diagram) ?? Fallback();

        return new()
        {
            ArrayName = visualNode.SymbolIdentifierNodeName ?? visualNode.Title ?? string.Empty,
            Index = arrayIndex,
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs an array length expression from the given visual array length node.
    /// </summary>
    /// <param name="visualNode">The visual array length node.</param>
    /// <returns>The reconstructed array length expression node.</returns>
    private static ArrayLengthNode ReconstructArrayLengthFromNode(TopsyTurvyVisualNodeModel visualNode)
    {
        return new()
        {
            ArrayName = visualNode.SymbolIdentifierNodeName ?? visualNode.Title ?? string.Empty,
            Span = EmptySpan,
        };
    }

    /// <summary>
    /// Reconstructs an expression cast from the given visual expression cast node.
    /// </summary>
    /// <param name="visualNode">The visual expression cast node.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>The reconstructed expression cast node.</returns>
    private ExpressionCastNode ReconstructExpressionCastFromNode(TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        LiteralType targetType = visualNode.NodeLiteralType ?? LiteralType.Integer;
        Expression expression = this.GetExpressionFromDataIn(visualNode, "Expr", diagram) ?? Fallback();

        return new()
        {
            Expression = expression,
            NewType = targetType,
            Span = EmptySpan
        };
    }

    /// <summary>
    /// Reconstructs a summon expression from the given visual summon node.
    /// </summary>
    /// <param name="visualNode">The visual summon node.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>The reconstructed summon expression node.</returns>
    private PrefixExpressionNode ReconstructSummonFromNode(TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        // The SUMMON node has a "Function" DataIn port linked to the function identifier node.
        TopsyTurvyVisualPortModel? functionPort = visualNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .FirstOrDefault(port => port.Role == VisualPortRole.DataIn && port.Label == "Function");

        TopsyTurvyVisualNodeModel? functionNode = functionPort is not null
            ? GetExpressionSourceNode(functionPort)
            : null;

        List<Expression> arguments = [];
        string functionName = functionNode?.SymbolIdentifierNodeName ?? functionNode?.Title ?? "unknown";
        arguments.Add(new IdentifierNode()
        {
            Name = functionName,
            Span = EmptySpan
        });

        if (functionNode is not null)
        {
            List<TopsyTurvyVisualPortModel> callArgumentPorts = [.. functionNode.Ports
                .OfType<TopsyTurvyVisualPortModel>()
                .Where(port => port.Role == VisualPortRole.DataIn)
                .OrderBy(port => port.Label)];

            foreach (TopsyTurvyVisualPortModel argumentPort in callArgumentPorts)
            {
                TopsyTurvyVisualNodeModel? expressionSourceNode = GetExpressionSourceNode(argumentPort);
                if (expressionSourceNode is not null)
                {
                    arguments.Add(this.ReconstructExpression(expressionSourceNode, diagram));
                }
            }
        }

        return new PrefixExpressionNode { Operator = Operator.Summon, Arguments = arguments.AsReadOnly(), Span = EmptySpan };
    }

    /// <summary>
    /// Finds the visual node in the diagram that corresponds to the given AST object.
    /// </summary>
    /// <param name="astObject">The AST object to find the corresponding visual node for.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>The visual node corresponding to the AST object, or <c>null</c> if no matching node is found.</returns>
    private static TopsyTurvyVisualNodeModel? FindNodeByAst(object astObject, BlazorDiagram diagram)
    {
        return diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .FirstOrDefault(visualNode => ReferenceEquals(visualNode.AstNode, astObject));
    }

    /// <summary>
    /// Gets the source node of an expression connected to a Data In port.
    /// </summary>
    /// <param name="dataInPort">The Data In port to follow.</param>
    /// <remarks>
    /// Follows the incoming link on a DataIn port to its source (Data Out) node.
    /// Returns <c>null</c> when no link is connected.
    /// </remarks>
    /// <returns>The source node of the expression, or <c>null</c> if the port is disconnected.</returns>
    private static TopsyTurvyVisualNodeModel? GetExpressionSourceNode(TopsyTurvyVisualPortModel dataInPort)
    {
        BaseLinkModel? link = dataInPort.Links.FirstOrDefault();
        if (link is null)
        {
            return null;
        }

        // When created with new LinkModel(outputPort, inputPort), Source wraps the output (Data Out) port.
        PortModel? sourcePort = (link.Source as SinglePortAnchor)?.Port;
        return sourcePort?.Parent as TopsyTurvyVisualNodeModel;
    }

    /// <summary>
    /// Gets the target node of a branch flow from an opener node and a branch label.
    /// </summary>
    /// <param name="openerNode">The visual node that opens the branch.</param>
    /// <param name="branchLabel">The label of the branch to follow.</param>
    /// <remarks>
    /// Follows a Branch Out link from <paramref name="openerNode"/> with the given <paramref name="branchLabel"/>
    /// to the branch header node (the Flow In target of the branch link).
    /// </remarks>
    /// <returns>The target node of the branch flow, or <c>null</c> if the branch is disconnected.</returns>
    private static TopsyTurvyVisualNodeModel? GetBranchFlowTarget(TopsyTurvyVisualNodeModel openerNode, string branchLabel)
    {
        TopsyTurvyVisualPortModel? branchPort = openerNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .FirstOrDefault(port => port.Role == VisualPortRole.BranchOut && port.Label == branchLabel);

        if (branchPort is null)
        {
            return null;
        }

        BaseLinkModel? link = branchPort.Links.FirstOrDefault();
        if (link is null)
        {
            return null;
        }

        // Branch Out → Flow In: Target wraps the FlowIn port.
        PortModel? targetPort = (link.Target as SinglePortAnchor)?.Port;
        return targetPort?.Parent as TopsyTurvyVisualNodeModel;
    }

    /// <summary>
    /// Parses a literal value string into the appropriate CLR type for the given <see cref="LiteralType"/>.
    /// </summary>
    /// <param name="type">The literal type to parse the value as.</param>
    /// <param name="value">The string representation of the literal value.</param>
    /// <returns>The parsed literal value as an object, or the original string if parsing fails, or <c>null</c> if the value is <c>null</c>.</returns>
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
            LiteralType.UnsignedShort => ushort.TryParse(value, out ushort us) ? us : (object?)value,
            LiteralType.Byte => byte.TryParse(value, out byte b) ? b : (object?)value,
            LiteralType.Double => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ? d : (object?)value,
            LiteralType.Single => float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float f) ? f : (object?)value,
            LiteralType.Boolean => string.Equals(value, "VERITY", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "True", StringComparison.OrdinalIgnoreCase)
                ? true
                : (object?)false,
            _ => value,
        };
    }

    /// <summary>
    /// Returns a null literal as a safe fallback expression when a required port is disconnected.
    /// </summary>
    /// <returns>A <see cref="LiteralNode"/> representing a null literal with an empty span.</returns>
    private static LiteralNode Fallback() => new()
    {
        Type = LiteralType.Null,
        Value = null,
        Span = EmptySpan
    };
}
