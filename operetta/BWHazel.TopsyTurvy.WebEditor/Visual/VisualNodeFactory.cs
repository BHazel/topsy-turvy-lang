using System;
using System.Collections.Generic;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Creates <see cref="TopsyTurvyVisualNodeModel"/> clusters for any statement or expression type
/// without requiring an existing AST node.
/// </summary>
/// <remarks>
/// Used by the context menu and symbol panel to add new nodes at runtime.
/// Block-producing methods return the opener plus any branch headers and the closer.  All nodes
/// are added to the diagram and the opener–closer pair IDs are wired.
/// </remarks>
internal static class VisualNodeFactory
{
    /// <summary>
    /// Creates a statement node cluster for the given <paramref name="statementType"/> and adds
    /// all produced nodes to <paramref name="diagram"/>.
    /// </summary>
    /// <param name="statementType">The statement type, equivalent to the name of the AST node class.</param>
    /// <param name="position">The canvas position for the primary, first, node.</param>
    /// <param name="diagram">The diagram to add nodes and links to.</param>
    /// <param name="nodeCounter">The counter incremented for each node created, used to generate unique IDs.</param>
    /// <returns>
    /// A list of all created nodes. For block types the opener is first and the closer is last, or an empty list for unknown node types.
    /// </returns>
    public static IReadOnlyList<TopsyTurvyVisualNodeModel> CreateStatement(
        string statementType,
        Point position,
        BlazorDiagram diagram,
        ref int nodeCounter)
    {
        return statementType switch
        {
            "DeclarationNode"  => [SimpleStatement(statementType, "PRAY WELCOME", "name : TYPE", VisualNodeKind.Declaration, position, diagram, ref nodeCounter, dataInLabels: ["Value"])],
            "ArrayDeclarationNode"  => [SimpleStatement(statementType, "PRAY WELCOME", "name : TYPE[]", VisualNodeKind.Declaration, position, diagram, ref nodeCounter)],
            "AssignmentNode" => [SimpleStatement(statementType, "IS APPOINTED", null, VisualNodeKind.Assignment, position, diagram, ref nodeCounter, dataInLabels: ["Variable", "Value"])],
            "ArrayElementAssignmentNode" => [SimpleStatement(statementType, "VICTIM IS APPOINTED", null, VisualNodeKind.Assignment, position, diagram, ref nodeCounter, dataInLabels: ["Variable", "Victim", "Value"])],
            "PrintNode" => [SimpleStatement(statementType, "BEHOLD", null, VisualNodeKind.Print, position, diagram, ref nodeCounter, dataInLabels: ["Expr"])],
            "InputNode" => [SimpleStatement(statementType, "PRAY TELL", null, VisualNodeKind.Input, position, diagram, ref nodeCounter, dataInLabels: ["Variable"])],
            "BreakNode" => [SimpleStatement(statementType, "THAT WILL DO.", null, VisualNodeKind.ControlFlow, position, diagram, ref nodeCounter, shouldFlowOut: false)],
            "ContinueNode" => [SimpleStatement(statementType, "ONCE MORE.", null, VisualNodeKind.ControlFlow, position, diagram, ref nodeCounter, shouldFlowOut: false)],
            "ProgrammeReturnNode" => [SimpleStatement(statementType, "AND SO I FIND", null, VisualNodeKind.Function, position, diagram, ref nodeCounter, dataInLabels: ["Value"])],
            "ReturnNode" => [SimpleStatement(statementType, "AND SO I FIND", null, VisualNodeKind.Function, position, diagram, ref nodeCounter, dataInLabels: ["Value"])],
            "ThrowNode" => [SimpleStatement(statementType, "A HIDEOUS CURSE ON", null, VisualNodeKind.ErrorHandling, position, diagram, ref nodeCounter, dataInLabels: ["Value"])],
            "ImportNode" => [SimpleStatement(statementType, "PRAY ADMIT", null, VisualNodeKind.Other, position, diagram, ref nodeCounter)],
            "AssertNode" => [SimpleStatement(statementType, "THE LAW IS", null, VisualNodeKind.ControlFlow, position, diagram, ref nodeCounter, dataInLabels: ["Cond", "Msg"])],
            "ExpressionStatement" => [SimpleStatement(statementType, "EXPRESSION", null, VisualNodeKind.Other, position, diagram, ref nodeCounter, dataInLabels: ["Expr"])],
            "ConditionalOpener" => CreateConditionalBlock(position, diagram, ref nodeCounter),
            "LoopOpener" => CreateLoopBlock(position, diagram, ref nodeCounter),
            "TryCatchOpener" => CreateTryCatchBlock(position, diagram, ref nodeCounter),
            "SwitchOpener" => CreateSwitchBlock(position, diagram, ref nodeCounter),
            "GuardOpener" => CreateGuardBlock(position, diagram, ref nodeCounter),
            "FunctionBodyOpener" => CreateFunctionBodyBlock(position, diagram, ref nodeCounter),
            _ => [],
        };
    }

    /// <summary>
    /// Creates a standalone expression node and adds it to the diagram.
    /// </summary>
    /// <param name="statementType">The statement type, equivalent to the name of the AST node class.</param>
    /// <param name="position">The canvas position for the node.</param>
    /// <param name="diagram">The diagram to add the node to.</param>
    /// <param name="nodeCounter">The counter incremented for each node created.</param>
    /// <returns>The created expression node, or <c>null</c> for unknown types.</returns>
    public static TopsyTurvyVisualNodeModel? CreateExpression(
        string statementType,
        Point position,
        BlazorDiagram diagram,
        ref int nodeCounter)
    {
        TopsyTurvyVisualNodeModel? node = statementType switch
        {
            "LiteralNode" => ExpressionNode(statementType, "Literal", null, VisualNodeKind.Literal, position, ref nodeCounter, defaultLiteralType: LiteralType.String),
            "IdentifierNode" => ExpressionNode(statementType, "name", null, VisualNodeKind.Identifier, position, ref nodeCounter),
            "OperatorNode" => ExpressionNode(statementType, "SUM OF", null, VisualNodeKind.Operator, position, ref nodeCounter, dataInLabels: ["Arg 1", "Arg 2"]),
            "ArithmeticNode" => ExpressionNode(statementType, "SUM OF", null, VisualNodeKind.Operator, position, ref nodeCounter, dataInLabels: ["Arg 1", "Arg 2"]),
            "BitwiseNode" => ExpressionNode(statementType, "CHORD OF", null, VisualNodeKind.Operator, position, ref nodeCounter, dataInLabels: ["Arg 1", "Arg 2"]),
            "LogicalNode" => ExpressionNode(statementType, "BOTH", null, VisualNodeKind.Operator, position, ref nodeCounter, dataInLabels: ["Arg 1", "Arg 2"]),
            "VariadicNode" => ExpressionNode(statementType, "ALL OF", null, VisualNodeKind.Operator, position, ref nodeCounter, dataInLabels: ["Arg 1", "Arg 2"]),
            "WovenNode" => ExpressionNode(statementType, "WOVEN OF", null, VisualNodeKind.Operator, position, ref nodeCounter, dataInLabels: ["Arg 1", "Arg 2"]),
            "TernaryNode" => ExpressionNode(statementType, "SHOULD IT TRANSPIRE THAT", null, VisualNodeKind.Conditional, position, ref nodeCounter, dataInLabels: ["Cond", "True", "False"]),
            "ArrayIndexNode" => ExpressionNode(statementType, "VICTIM", "at index", VisualNodeKind.Identifier, position, ref nodeCounter, dataInLabels: ["Variable", "Index"]),
            "ArrayLengthNode" => ExpressionNode(statementType, "RECKONING OF", null, VisualNodeKind.Operator, position, ref nodeCounter, dataInLabels: ["Variable"]),
            "ExpressionCastNode" => ExpressionNode(statementType, "AS IT WERE", "→ PEER", VisualNodeKind.Operator, position, ref nodeCounter, dataInLabels: ["Expr"], defaultLiteralType: LiteralType.Integer),
            "SummonNode" => CreateSummonExpressionNode(position, ref nodeCounter),
            "FunctionReferenceNode" => ExpressionNode("FunctionReferenceNode", "function", null, VisualNodeKind.Function, position, ref nodeCounter),
            _ => null,
        };

        if (node is not null)
        {
            diagram.Nodes.Add(node);
        }

        return node;
    }

    /// <summary>
    /// Creates a conditional block and adds its nodes to the diagram.
    /// </summary>
    /// <param name="position">The position where the block should be created.</param>
    /// <param name="diagram">The diagram to which the block's nodes will be added.</param>
    /// <param name="nodeCounter">A counter used to generate unique node IDs.</param>
    /// <returns>
    /// A list containing the opener, branch headers the closer nodes.
    /// </returns>
    private static IReadOnlyList<TopsyTurvyVisualNodeModel> CreateConditionalBlock(
        Point position, BlazorDiagram diagram, ref int nodeCounter)
    {
        TopsyTurvyVisualNodeModel openerNode = MakeNode("ConditionalOpener", "SHOULD IT TRANSPIRE THAT", null, VisualNodeKind.Conditional, position, ref nodeCounter);
        openerNode.AddPort(MakePort(openerNode, "In", VisualPortRole.FlowIn));
        openerNode.AddPort(MakePort(openerNode, "Cond", VisualPortRole.DataIn));
        
        TopsyTurvyVisualPortModel truePort = MakePort(openerNode, "True", VisualPortRole.BranchOut);
        openerNode.AddPort(truePort);
        
        TopsyTurvyVisualPortModel elsePort = MakePort(openerNode, "Else", VisualPortRole.BranchOut);
        openerNode.AddPort(elsePort);

        diagram.Nodes.Add(openerNode);

        const double branchSpacing = 660.0;
        double headerY = position.Y + NodeLayoutContext.RowSpacing;

        TopsyTurvyVisualNodeModel trueHeaderNode = MakeNode("ConditionalTrueBranch", "QUITE SO.", null, VisualNodeKind.Conditional, new Point(position.X - branchSpacing / 2, headerY), ref nodeCounter);
        TopsyTurvyVisualPortModel trueHeaderInPort = MakePort(trueHeaderNode, "In", VisualPortRole.FlowIn);
        trueHeaderNode.AddPort(trueHeaderInPort);
        trueHeaderNode.AddPort(MakePort(trueHeaderNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(trueHeaderNode);
        diagram.Links.Add(new LinkModel(truePort, trueHeaderInPort));

        TopsyTurvyVisualNodeModel elseHeaderNode = MakeNode("ConditionalElseBranch", "OTHERWISE,", null, VisualNodeKind.Conditional, new Point(position.X + branchSpacing / 2, headerY), ref nodeCounter);
        TopsyTurvyVisualPortModel elseHeaderInPort = MakePort(elseHeaderNode, "In", VisualPortRole.FlowIn);
        elseHeaderNode.AddPort(elseHeaderInPort);
        elseHeaderNode.AddPort(MakePort(elseHeaderNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(elseHeaderNode);
        diagram.Links.Add(new LinkModel(elsePort, elseHeaderInPort));

        double closerY = headerY + NodeLayoutContext.RowSpacing;
        TopsyTurvyVisualNodeModel closerNode = MakeNode("ConditionalCloser", "SO MUCH FOR THAT.", null, VisualNodeKind.Conditional, new Point(position.X, closerY), ref nodeCounter);
        closerNode.AddPort(MakePort(closerNode, "In", VisualPortRole.FlowIn));
        closerNode.AddPort(MakePort(closerNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(closerNode);

        openerNode.PairedCloserId = closerNode.Id;
        closerNode.PairedOpenerId = openerNode.Id;

        return [openerNode, trueHeaderNode, elseHeaderNode, closerNode];
    }

    /// <summary>
    /// Creates a loop block and adds its nodes to the diagram.
    /// </summary>
    /// <param name="position">The position where the block should be created.</param>
    /// <param name="diagram">The diagram to which the block's nodes will be added.</param>
    /// <param name="nodeCounter">A counter used to generate unique node IDs.</param>
    /// <returns>A list containing the opener and closer nodes of the loop block.</returns>
    private static IReadOnlyList<TopsyTurvyVisualNodeModel> CreateLoopBlock(
        Point position, BlazorDiagram diagram, ref int nodeCounter)
    {
        TopsyTurvyVisualNodeModel openerNode = MakeNode("LoopOpener", "BY A LEGAL FICTION", "Infinite", VisualNodeKind.Loop, position, ref nodeCounter);
        openerNode.AddPort(MakePort(openerNode, "In", VisualPortRole.FlowIn));
        openerNode.AddPort(MakePort(openerNode, "Cond", VisualPortRole.DataIn));
        openerNode.AddPort(MakePort(openerNode, "Step", VisualPortRole.DataIn));
        openerNode.AddPort(MakePort(openerNode, "Body", VisualPortRole.BranchOut));
        diagram.Nodes.Add(openerNode);

        Point closerPosition = new(position.X, position.Y + NodeLayoutContext.RowSpacing);
        TopsyTurvyVisualNodeModel closerNode = MakeNode("LoopCloser", "THE TERM EXPIRES.", null, VisualNodeKind.Loop, closerPosition, ref nodeCounter);
        closerNode.AddPort(MakePort(closerNode, "In", VisualPortRole.FlowIn));
        closerNode.AddPort(MakePort(closerNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(closerNode);

        openerNode.PairedCloserId = closerNode.Id;
        closerNode.PairedOpenerId = openerNode.Id;

        return [openerNode, closerNode];
    }

    /// <summary>
    /// Creates a try-catch block and adds its nodes to the diagram.
    /// </summary>
    /// <param name="position">The position where the block should be created.</param>
    /// <param name="diagram">The diagram to which the block's nodes will be added.</param>
    /// <param name="nodeCounter">A counter used to generate unique node IDs.</param>
    /// <returns>A list containing the opener, branch and closer nodes of the try-catch block.</returns>
    private static IReadOnlyList<TopsyTurvyVisualNodeModel> CreateTryCatchBlock(
        Point position, BlazorDiagram diagram, ref int nodeCounter)
    {
        TopsyTurvyVisualNodeModel openerNode = MakeNode("TryCatchOpener", "WITH THE GREATEST RESPECT,", "catch: error", VisualNodeKind.ErrorHandling, position, ref nodeCounter);
        openerNode.AddPort(MakePort(openerNode, "In", VisualPortRole.FlowIn));
        openerNode.AddPort(MakePort(openerNode, "Op", VisualPortRole.DataIn));
        
        TopsyTurvyVisualPortModel successPort = MakePort(openerNode, "Success", VisualPortRole.BranchOut);
        openerNode.AddPort(successPort);
        
        TopsyTurvyVisualPortModel errorPort = MakePort(openerNode, "Error", VisualPortRole.BranchOut);
        openerNode.AddPort(errorPort);

        diagram.Nodes.Add(openerNode);

        const double tryCatchSpacing = 660.0;
        double headerY = position.Y + NodeLayoutContext.RowSpacing;

        TopsyTurvyVisualNodeModel successHeaderNode = MakeNode("TryCatchSuccessBranch", "WITH GRATITUDE", null, VisualNodeKind.ErrorHandling, new Point(position.X - tryCatchSpacing / 2, headerY), ref nodeCounter);
        TopsyTurvyVisualPortModel successHeaderInPort = MakePort(successHeaderNode, "In", VisualPortRole.FlowIn);
        successHeaderNode.AddPort(successHeaderInPort);
        successHeaderNode.AddPort(MakePort(successHeaderNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(successHeaderNode);
        diagram.Links.Add(new LinkModel(successPort, successHeaderInPort));

        TopsyTurvyVisualNodeModel errorHeaderNode = MakeNode("TryCatchErrorBranch", "MODIFIED RAPTURE,", null, VisualNodeKind.ErrorHandling, new Point(position.X + tryCatchSpacing / 2, headerY), ref nodeCounter);
        TopsyTurvyVisualPortModel errorHeaderInPort = MakePort(errorHeaderNode, "In", VisualPortRole.FlowIn);
        errorHeaderNode.AddPort(errorHeaderInPort);
        errorHeaderNode.AddPort(MakePort(errorHeaderNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(errorHeaderNode);
        diagram.Links.Add(new LinkModel(errorPort, errorHeaderInPort));

        double closerY = headerY + NodeLayoutContext.RowSpacing;
        TopsyTurvyVisualNodeModel closerNode = MakeNode("TryCatchCloser", "THAT CONCLUDES THE MATTER.", null, VisualNodeKind.ErrorHandling, new Point(position.X, closerY), ref nodeCounter);
        closerNode.AddPort(MakePort(closerNode, "In", VisualPortRole.FlowIn));
        closerNode.AddPort(MakePort(closerNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(closerNode);

        openerNode.PairedCloserId = closerNode.Id;
        closerNode.PairedOpenerId = openerNode.Id;

        return [openerNode, successHeaderNode, errorHeaderNode, closerNode];
    }

    /// <summary>
    /// Creates a switch block and adds its nodes to the diagram.
    /// </summary>
    /// <param name="position">The position where the block should be created.</param>
    /// <param name="diagram">The diagram to which the block's nodes will be added.</param>
    /// <param name="nodeCounter">A counter used to generate unique node IDs.</param>
    /// <returns>A list containing the opener and closer nodes of the switch block.</returns>
    private static IReadOnlyList<TopsyTurvyVisualNodeModel> CreateSwitchBlock(
        Point position, BlazorDiagram diagram, ref int nodeCounter)
    {
        TopsyTurvyVisualNodeModel openerNode = MakeNode("SwitchOpener", "IN WHICH CAPACITY?", null, VisualNodeKind.Conditional, position, ref nodeCounter);
        openerNode.AddPort(MakePort(openerNode, "In", VisualPortRole.FlowIn));
        openerNode.AddPort(MakePort(openerNode, "Expr", VisualPortRole.DataIn));
        openerNode.AddPort(MakePort(openerNode, "Case 1", VisualPortRole.BranchOut));
        openerNode.AddPort(MakePort(openerNode, "Default", VisualPortRole.BranchOut));
        diagram.Nodes.Add(openerNode);

        Point closerPosition = new(position.X, position.Y + NodeLayoutContext.RowSpacing);
        TopsyTurvyVisualNodeModel closerNode = MakeNode("SwitchCloser", "NOTHING COULD BE MORE SATISFACTORY.", null, VisualNodeKind.Conditional, closerPosition, ref nodeCounter);
        closerNode.AddPort(MakePort(closerNode, "In", VisualPortRole.FlowIn));
        closerNode.AddPort(MakePort(closerNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(closerNode);

        openerNode.PairedCloserId = closerNode.Id;
        closerNode.PairedOpenerId = openerNode.Id;

        return [openerNode, closerNode];
    }

    /// <summary>
    /// Creates a guard block and adds its nodes to the diagram.
    /// </summary>
    /// <param name="position">The position where the block should be created.</param>
    /// <param name="diagram">The diagram to which the block's nodes will be added.</param>
    /// <param name="nodeCounter">A counter used to generate unique node IDs.</param>
    /// <returns>A list containing the opener and closer nodes of the guard block.</returns>
    private static IReadOnlyList<TopsyTurvyVisualNodeModel> CreateGuardBlock(
        Point position, BlazorDiagram diagram, ref int nodeCounter)
    {
        TopsyTurvyVisualNodeModel openerNode = MakeNode("GuardOpener", "YEOMAN", null, VisualNodeKind.ControlFlow, position, ref nodeCounter);
        openerNode.AddPort(MakePort(openerNode, "In", VisualPortRole.FlowIn));
        TopsyTurvyVisualPortModel guardOutPort = MakePort(openerNode, "Out", VisualPortRole.FlowOut);
        openerNode.AddPort(guardOutPort);
        openerNode.AddPort(MakePort(openerNode, "Cond", VisualPortRole.DataIn));
        openerNode.AddPort(MakePort(openerNode, "Else", VisualPortRole.BranchOut));
        diagram.Nodes.Add(openerNode);

        Point closerPosition = new(position.X, position.Y + NodeLayoutContext.RowSpacing);
        TopsyTurvyVisualNodeModel closerNode = MakeNode("GuardCloser", "UNDER ORDERS.", null, VisualNodeKind.ControlFlow, closerPosition, ref nodeCounter);
        TopsyTurvyVisualPortModel closerInPort = MakePort(closerNode, "In", VisualPortRole.FlowIn);
        closerNode.AddPort(closerInPort);
        closerNode.AddPort(MakePort(closerNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(closerNode);

        diagram.Links.Add(new LinkModel(guardOutPort, closerInPort));

        openerNode.PairedCloserId = closerNode.Id;
        closerNode.PairedOpenerId = openerNode.Id;

        return [openerNode, closerNode];
    }

    /// <summary>
    /// Creates a function body block and adds its nodes to the diagram.
    /// </summary>
    /// <param name="position">The position where the block should be created.</param>
    /// <param name="diagram">The diagram to which the block's nodes will be added.</param>
    /// <param name="nodeCounter">A counter used to generate unique node IDs.</param>
    /// <returns>A list containing the opener and closer nodes of the function body block.</returns>
    private static IReadOnlyList<TopsyTurvyVisualNodeModel> CreateFunctionBodyBlock(
        Point position, BlazorDiagram diagram, ref int nodeCounter)
    {
        TopsyTurvyVisualNodeModel openerNode = MakeNode("FunctionBodyOpener", "IT IS MY DUTY TO PERFORM", "functionName", VisualNodeKind.Function, position, ref nodeCounter);
        openerNode.AddPort(MakePort(openerNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(openerNode);

        Point closerPosition = new(position.X, position.Y + NodeLayoutContext.RowSpacing);
        TopsyTurvyVisualNodeModel closerNode = MakeNode("FunctionBodyCloser", "MY DUTY IS DISCHARGED.", null, VisualNodeKind.Function, closerPosition, ref nodeCounter);
        closerNode.AddPort(MakePort(closerNode, "In", VisualPortRole.FlowIn));
        diagram.Nodes.Add(closerNode);

        openerNode.PairedCloserId = closerNode.Id;
        closerNode.PairedOpenerId = openerNode.Id;

        return [openerNode, closerNode];
    }

    /// <summary>
    /// Creates a simple statement node and adds it to the diagram.
    /// </summary>
    /// <param name="statementType">The type of the statement.</param>
    /// <param name="title">The title of the node.</param>
    /// <param name="subtitle">The subtitle of the node.</param>
    /// <param name="kind">The kind of the visual node.</param>
    /// <param name="position">The position where the node should be created.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <param name="nodeCounter">A counter used to generate unique node IDs.</param>
    /// <param name="dataInLabels">An array of labels for data input ports.</param>
    /// <param name="shouldFlowOut">Indicates whether the node should have a flow output port.</param>
    /// <returns>The created visual node.</returns>
    private static TopsyTurvyVisualNodeModel SimpleStatement(
        string statementType,
        string title,
        string? subtitle,
        VisualNodeKind kind,
        Point position,
        BlazorDiagram diagram,
        ref int nodeCounter,
        string[]? dataInLabels = null,
        bool shouldFlowOut = true)
    {
        TopsyTurvyVisualNodeModel visualNode = MakeNode(statementType, title, subtitle, kind, position, ref nodeCounter);
        visualNode.AddPort(MakePort(visualNode, "In", VisualPortRole.FlowIn));
        if (shouldFlowOut)
        {
            visualNode.AddPort(MakePort(visualNode, "Out", VisualPortRole.FlowOut));
        }

        if (dataInLabels is not null)
        {
            foreach (string label in dataInLabels)
            {
                visualNode.AddPort(MakePort(visualNode, label, VisualPortRole.DataIn));
            }
        }

        diagram.Nodes.Add(visualNode);
        return visualNode;
    }

    /// <summary>
    /// Creates a SUMMON expression node.
    /// </summary>
    /// <param name="position">The position where the node should be created.</param>
    /// <param name="nodeCounter">A counter used to generate unique node IDs.</param>
    /// <remarks>
    /// Both Flow and Data ports are added to the node to allow for statement and expression use respectively.
    /// </remarks>
    /// <returns>The created SUMMON expression node.</returns>
    private static TopsyTurvyVisualNodeModel CreateSummonExpressionNode(Point position, ref int nodeCounter)
    {
        TopsyTurvyVisualNodeModel node = MakeNode("SummonNode", "SUMMON", null, VisualNodeKind.Function, position, ref nodeCounter);
        node.AddPort(MakePort(node, "In", VisualPortRole.FlowIn));
        node.AddPort(MakePort(node, "Function", VisualPortRole.DataIn));
        node.AddPort(MakePort(node, "Out", VisualPortRole.DataOut));
        node.AddPort(MakePort(node, "Out", VisualPortRole.FlowOut));
        return node;
    }

    /// <summary>
    /// Creates an expression node and adds it to the diagram.
    /// </summary>
    /// <param name="statementType">The type of the statement.</param>
    /// <param name="title">The title of the node.</param>
    /// <param name="subtitle">The subtitle of the node.</param>
    /// <param name="kind">The kind of the visual node.</param>
    /// <param name="position">The position where the node should be created.</param>
    /// <param name="nodeCounter">A counter used to generate unique node IDs.</param>
    /// <param name="dataInLabels">An array of labels for data input ports.</param>
    /// <param name="defaultLiteralType">The default literal type for the node.</param>
    /// <returns>The created visual node.</returns>
    private static TopsyTurvyVisualNodeModel ExpressionNode(
        string statementType,
        string title,
        string? subtitle,
        VisualNodeKind kind,
        Point position,
        ref int nodeCounter,
        string[]? dataInLabels = null,
        LiteralType? defaultLiteralType = null)
    {
        TopsyTurvyVisualNodeModel visualNode = MakeNode(statementType, title, subtitle, kind, position, ref nodeCounter);
        if (defaultLiteralType is not null)
        {
            visualNode.NodeLiteralType = defaultLiteralType;
        }

        if (dataInLabels is not null)
        {
            foreach (string label in dataInLabels)
            {
                visualNode.AddPort(MakePort(visualNode, label, VisualPortRole.DataIn));
            }
        }

        visualNode.AddPort(MakePort(visualNode, "Out", VisualPortRole.DataOut));
        return visualNode;
    }

    /// <summary>
    /// Creates a new visual node with a unique ID.
    /// </summary>
    /// <param name="statementType">The type of the statement.</param>
    /// <param name="title">The title of the node.</param>
    /// <param name="subtitle">The subtitle of the node.</param>
    /// <param name="kind">The kind of the visual node.</param>
    /// <param name="position">The position of the node.</param>
    /// <param name="nodeCounter">A counter used to generate unique node IDs.</param>
    /// <returns>The created visual node.</returns>
    private static TopsyTurvyVisualNodeModel MakeNode(
        string statementType,
        string title,
        string? subtitle,
        VisualNodeKind kind,
        Point position,
        ref int nodeCounter)
    {
        string id = $"factory-{++nodeCounter}-{Guid.NewGuid():N}";
        return new TopsyTurvyVisualNodeModel(id, position, title, subtitle, kind)
        {
            StatementType = statementType,
        };
    }

    /// <summary>
    /// Creates a new visual port for a given node.
    /// </summary>
    /// <param name="node">The node to which the port belongs.</param>
    /// <param name="label">The label of the port.</param>
    /// <param name="role">The role of the port.</param>
    /// <returns>The created visual port.</returns>
    private static TopsyTurvyVisualPortModel MakePort(
        TopsyTurvyVisualNodeModel node,
        string label,
        VisualPortRole role)
    {
        PortAlignment alignment = role switch
        {
            VisualPortRole.FlowIn => PortAlignment.Top,
            VisualPortRole.FlowOut => PortAlignment.Bottom,
            VisualPortRole.BranchOut => PortAlignment.Bottom,
            VisualPortRole.DataIn => PortAlignment.Left,
            VisualPortRole.DataOut => PortAlignment.Right,
            _ => PortAlignment.Bottom,
        };

        return new TopsyTurvyVisualPortModel(node, alignment, label, role);
    }
}
