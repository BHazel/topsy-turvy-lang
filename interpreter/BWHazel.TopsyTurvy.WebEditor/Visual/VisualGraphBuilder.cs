using System;
using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Behaviors;
using Blazor.Diagrams.Core.Models;
using BWHazel.TopsyTurvy.Ast;

using BlazorDiagramsPoint = Blazor.Diagrams.Core.Geometry.Point;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Builds a <see cref="BlazorDiagram"/> for display in the Visual Editor from a successfully-parsed <see cref="ProgramNode"/>.
/// </summary>
/// <remarks>
/// The automatic layout strategy for the visual editor is as follows:
/// * Main Flow Column: X - 700.
///     * Statement nodes are chained top-to-bottom with Flow Out to FlowIn edges.
///     * Expression nodes sit to the left of their consumer at X = <c>consumer.X</c> - <c>Expression Colum nWidth</c> per depth level.
///     * Declarations and imports float in a sidebar column at X = 80, outside the main execution flow.
///     * Function bodies are rendered in a separate column at X = 1400, outside the main execution flow and stacked vertically.
///     * Branching statements fan out centered on the opener X, with the closer placed below the deepest tail.
/// </remarks>
public sealed class VisualGraphBuilder
{
    /// <summary>
    /// Horizontal distance between an expression node and its consumer, per depth level.
    /// </summary>
    private const double ExpressionColumnWidth = 210.0;

    /// <summary>
    /// Vertical gap between Data In ports on the same consumer node, used to separate parallel expression inputs.
    /// </summary>
    private const double ExpressionInputPortSpacingY = 80.0;

    private int nodeCounter;
    private HashSet<string> currentFunctionParameters = new();

    /// <summary>
    /// A reference to the last block closer node.
    /// </summary>
    /// <remarks>
    /// Set by block-statement factory methods to the closer node they add to the diagram so that the
    /// calling loop can chain subsequent statements from the closer rather than the opener.
    /// Reset to <c>null</c> before each <see cref="CreateStatementNode"/> call.
    /// </remarks>
    private TopsyTurvyVisualNodeModel? lastBlockCloser;

    /// <summary>
    /// Constructs a <see cref="BlazorDiagram"/> from the given programme AST.
    /// </summary>
    /// <param name="program">Root node of the parsed Topsy Turvy programme.</param>
    /// <param name="isReadOnly">
    /// When <c>true</c> (the default), the diagram is in read-only mode: nodes can be dragged to reposition them but
    /// the editing controls in <c>VisualNodeWidget</c> are hidden and selection is disabled.
    /// When <c>false</c>, <see cref="SelectionBehavior"/> is also active and all editing controls are shown.
    /// </param>
    /// <returns>A fully configured diagram ready to be passed to the visual editor component.</returns>
    public BlazorDiagram Build(ProgramNode program, bool isReadOnly = true)
    {
        this.nodeCounter = 0;
        BlazorDiagram diagram = new();

        if (isReadOnly)
        {
            diagram.UnregisterBehavior<DragNewLinkBehavior>();
        }

        NodeLayoutContext layout = new(primaryX: 700, secondaryX: 500);
        NodeLayoutContext sideLayout = new(primaryX: 80, secondaryX: 80);

        TopsyTurvyVisualNodeModel programNode = this.CreateProgramNode(program, layout);
        programNode.StatementType = "HarkNode";
        diagram.Nodes.Add(programNode);

        TopsyTurvyVisualNodeModel? previousStatement = programNode;

        // Collect function definitions for deferred body rendering.
        List<FunctionDefinitionNode> functionDefinitions = [];

        // Process each statement in the programme.
        foreach (Statement statement in program.Statements)
        {
            if (statement is PrincipalBlockNode declarations)
            {
                // Float each variable declaration as a standalone sidebar node and not part of the main flow.
                foreach (Statement decl in declarations.Declarations)
                {
                    TopsyTurvyVisualNodeModel declNode = this.CreateStatementNode(decl, sideLayout, diagram);
                    diagram.Nodes.Add(declNode);
                }

                continue;
            }

            if (statement is FunctionDefinitionNode definition)
            {
                // Collect for body subgraph rendering only: no sidebar signature node.
                functionDefinitions.Add(definition);
                continue;
            }

            if (statement is ImportNode)
            {
                // Float import directives in the sidebar alongside declarations.
                TopsyTurvyVisualNodeModel importNode = this.CreateStatementNode(statement, sideLayout, diagram);
                diagram.Nodes.Add(importNode);
                continue;
            }

            this.lastBlockCloser = null;
            TopsyTurvyVisualNodeModel statementNode = this.CreateStatementNode(statement, layout, diagram);
            diagram.Nodes.Add(statementNode);
            this.LinkFlow(previousStatement, statementNode, diagram);
            previousStatement = this.lastBlockCloser ?? statementNode;
        }

        TopsyTurvyVisualNodeModel finaleNode = this.MakeNode(layout.NextPrimaryPosition(), "FINALE.", null, VisualNodeKind.Program);
        finaleNode.StatementType = "FinaleNode";
        finaleNode.AddPort(this.MakePort(finaleNode, "In", VisualPortRole.FlowIn));
        diagram.Nodes.Add(finaleNode);
        this.LinkFlow(previousStatement, finaleNode, diagram);

        // Render each function body as a separate subgraph to the right of the main flow.
        // Each function body occupies its own vertical strip and are stacked with a gap between them.
        double functionBodyStartY = 60;
        foreach (FunctionDefinitionNode definition in functionDefinitions)
        {
            NodeLayoutContext functionLayout = new(primaryX: 1400, secondaryX: 1180, startY: functionBodyStartY);
            int bodyNodeCount = this.CreateFunctionBodySubgraph(definition, functionLayout, diagram);
            functionBodyStartY += (bodyNodeCount + 2) * 160 + 100;
        }

        return diagram;
    }

    /// <summary>
    /// Creates a link between the Flow Out port of the <paramref name="from"/> node and the Flow In port of the <paramref name="to"/> node, if both ports exist.
    /// </summary>
    /// <param name="from">The node from which the flow originates.</param>
    /// <param name="to">The node to which the flow is directed.</param>
    /// <param name="diagram">The diagram to which the link will be added.</param>
    private void LinkFlow(TopsyTurvyVisualNodeModel? from, TopsyTurvyVisualNodeModel to, BlazorDiagram diagram)
    {
        if (from is null)
        {
            return;
        }

        PortModel? outputPort = FindPort(from, VisualPortRole.FlowOut);
        PortModel? inputPort = FindPort(to, VisualPortRole.FlowIn);
        if (outputPort is not null && inputPort is not null)
        {
            diagram.Links.Add(new LinkModel(outputPort, inputPort));
        }
    }

    /// <summary>
    /// Creates the top-level visual node for a programme.
    /// </summary>
    /// <param name="program">The parsed programme.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <returns>The created visual node model for the programme.</returns>
    private TopsyTurvyVisualNodeModel CreateProgramNode(ProgramNode program, NodeLayoutContext layout)
    {
        string subtitle = program.Subtitle is not null
            ? $"\"{program.Title}\" or, \"{program.Subtitle}\""
            : $"\"{program.Title}\"";

        TopsyTurvyVisualNodeModel node = this.MakeNode(layout.NextPrimaryPosition(), "HARK!", subtitle, VisualNodeKind.Program);
        node.AddPort(this.MakePort(node, "Out", VisualPortRole.FlowOut));
        node.AstNode = program;
        node.SymbolIdentifierNodeName = program.Title;
        node.LiteralValue = program.Subtitle;
        return node;
    }

    /// <summary>
    /// Creates a visual node for a given statement.
    /// </summary>
    /// <param name="statement">The statement node.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The created visual node model for the statement.</returns>
    private TopsyTurvyVisualNodeModel CreateStatementNode(Statement statement, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        return statement switch
        {
            DeclarationNode node  => this.CreateDeclarationNode(node, layout, diagram),
            ArrayDeclarationNode node => this.CreateArrayDeclarationNode(node, layout, diagram),
            AssignmentNode node => this.CreateAssignmentNode(node, layout, diagram),
            ArrayElementAssignmentNode node => this.CreateArrayElementAssignmentNode(node, layout, diagram),
            PrintNode node => this.CreatePrintNode(node, layout, diagram),
            InputNode node => this.CreateInputNode(node, layout, diagram),
            ConditionalNode node => this.CreateConditionalNode(node, layout, diagram),
            LoopNode node => this.CreateLoopNode(node, layout, diagram),
            BreakNode => this.CreateSimpleNode(layout, "THAT WILL DO.", null, VisualNodeKind.ControlFlow, flowIn: true, flowOut: false, statementType: "BreakNode"),
            ContinueNode => this.CreateSimpleNode(layout, "ONCE MORE.", null, VisualNodeKind.ControlFlow, flowIn: true, flowOut: false, statementType: "ContinueNode"),
            FunctionDefinitionNode node => this.CreateFunctionSignatureNode(node, layout),
            ReturnNode node => this.CreateReturnNode(node, layout, diagram),
            ThrowNode node => this.CreateThrowNode(node, layout, diagram),
            TryCatchNode node => this.CreateTryCatchNode(node, layout, diagram),
            SwitchNode node => this.CreateSwitchNode(node, layout, diagram),
            ImportNode node => this.CreateImportNode(node, layout),
            GuardNode node => this.CreateGuardNode(node, layout, diagram),
            AssertNode node => this.CreateAssertNode(node, layout, diagram),
            ExpressionStatement node => this.CreateExpressionStatementNode(node, layout, diagram),
            _ => this.CreateSimpleNode(layout, "(unknown)", statement.GetType().Name, VisualNodeKind.Other, flowIn: true, flowOut: true),
        };
    }

    /// <summary>
    /// Creates a visual node for a variable declaration statement.
    /// </summary>
    /// <param name="node">The declaration node.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The visual node model for the variable declaration.</returns>
    private TopsyTurvyVisualNodeModel CreateDeclarationNode(DeclarationNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        string typeLabel = FormatLiteralType(node.Type);
        string constantLabel = node.IsConstant
            ? "CONSERVATIVE "
            : string.Empty;
        
        string subtitle = $"{node.Name} : {constantLabel}{typeLabel}";

        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "PRAY WELCOME", subtitle, VisualNodeKind.Declaration);
        statementNode.StatementType = "DeclarationNode";
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;
        statementNode.SymbolIdentifierNodeName = node.Name;
        statementNode.NodeLiteralType = node.Type;
        statementNode.IsIdentifierConstant = node.IsConstant;

        if (node.InitialValue is not null)
        {
            TopsyTurvyVisualPortModel dataInPort = this.MakePort(statementNode, "Value", VisualPortRole.DataIn);
            statementNode.AddPort(dataInPort);
            this.CreateExpressionNode(node.InitialValue, dataInPort, layout, diagram, anchor: statementNode.Position);
        }

        return statementNode;
    }

    /// <summary>
    /// Creates a visual node for an array declaration statement.
    /// </summary>
    /// <param name="node">The array declaration node.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The visual node model for the array declaration.</returns>
    private TopsyTurvyVisualNodeModel CreateArrayDeclarationNode(ArrayDeclarationNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        string typeLabel = FormatLiteralType(node.ElementType);
        string constantLabel = node.IsConstant
            ? "CONSERVATIVE "
            : string.Empty;
        
        string sizeLabel = node.Size.HasValue
            ? $" [{node.Size}]"
            : string.Empty;
        
        string subtitle = $"{node.Name} : {constantLabel}LITTLE LIST OF {typeLabel}{sizeLabel}";

        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "PRAY WELCOME", subtitle, VisualNodeKind.Declaration);
        statementNode.StatementType = "ArrayDeclarationNode";
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;
        statementNode.SymbolIdentifierNodeName = node.Name;
        statementNode.NodeLiteralType = LiteralType.Array;
        statementNode.ArrayElementLiteralType = node.ElementType;
        statementNode.IsIdentifierConstant = node.IsConstant;

        if (node.InitialValues.Count == 0 && node.Size.HasValue)
        {
            statementNode.LiteralValue = node.Size.Value.ToString();
        }

        for (int i = 0; i < node.InitialValues.Count; i++)
        {
            TopsyTurvyVisualPortModel port = this.MakePort(statementNode, $"Victim {i + 1}", VisualPortRole.DataIn);
            statementNode.AddPort(port);
            this.CreateExpressionNode(node.InitialValues[i], port, layout, diagram, anchor: statementNode.Position, portIndex: i);
        }

        return statementNode;
    }

    /// <summary>
    /// Creates a visual node for an assignment statement.
    /// </summary>
    /// <param name="node">The assignment node.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The visual node model for the assignment.</returns>
    private TopsyTurvyVisualNodeModel CreateAssignmentNode(AssignmentNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "IS APPOINTED", null, VisualNodeKind.Assignment);
        statementNode.StatementType = "AssignmentNode";
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;

        TopsyTurvyVisualPortModel targetPort = this.MakePort(statementNode, "Variable", VisualPortRole.DataIn);
        statementNode.AddPort(targetPort);
        this.CreateTargetIdentifierNode(node.Target, targetPort, statementNode.Position, diagram);

        TopsyTurvyVisualPortModel valuePort = this.MakePort(statementNode, "Value", VisualPortRole.DataIn);
        statementNode.AddPort(valuePort);
        this.CreateExpressionNode(node.Value, valuePort, layout, diagram, anchor: statementNode.Position, portIndex: 1);
        return statementNode;
    }

    /// <summary>
    /// Creates a visual node for an array element assignment statement.
    /// </summary>
    /// <param name="node">The array element assignment node.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The visual node model for the array element assignment.</returns>
    private TopsyTurvyVisualNodeModel CreateArrayElementAssignmentNode(ArrayElementAssignmentNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "VICTIM IS APPOINTED", null, VisualNodeKind.Assignment);
        statementNode.StatementType = "ArrayElementAssignmentNode";
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;

        TopsyTurvyVisualPortModel targetPort = this.MakePort(statementNode, "Variable", VisualPortRole.DataIn);
        statementNode.AddPort(targetPort);
        this.CreateTargetIdentifierNode(node.ArrayName, targetPort, statementNode.Position, diagram);

        TopsyTurvyVisualPortModel indexPort = this.MakePort(statementNode, "Victim", VisualPortRole.DataIn);
        statementNode.AddPort(indexPort);

        TopsyTurvyVisualPortModel valuePort = this.MakePort(statementNode, "Value", VisualPortRole.DataIn);
        statementNode.AddPort(valuePort);

        this.CreateExpressionNode(node.Index, indexPort, layout, diagram, anchor: statementNode.Position, portIndex: 1);
        this.CreateExpressionNode(node.Value, valuePort, layout, diagram, anchor: statementNode.Position, portIndex: 2);
        return statementNode;
    }

    /// <summary>
    /// Creates a visual node for a print statement.
    /// </summary>
    /// <param name="node">The print statement node.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The visual node model for the print statement.</returns>
    private TopsyTurvyVisualNodeModel CreatePrintNode(PrintNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "BEHOLD", null, VisualNodeKind.Print);
        statementNode.StatementType = "PrintNode";
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;
        statementNode.PrintSuppressNewline = node.SuppressNewline;

        TopsyTurvyVisualPortModel dataInPort = this.MakePort(statementNode, "Expr", VisualPortRole.DataIn);
        statementNode.AddPort(dataInPort);

        this.CreateExpressionNode(node.Expression, dataInPort, layout, diagram, anchor: statementNode.Position);
        return statementNode;
    }

    /// <summary>
    /// Creates a visual node for an input statement.
    /// </summary>
    /// <param name="node">The input statement node.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The visual node model for the input statement.</returns>
    private TopsyTurvyVisualNodeModel CreateInputNode(InputNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "PRAY TELL", null, VisualNodeKind.Input);
        statementNode.StatementType = "InputNode";
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;

        TopsyTurvyVisualPortModel targetPort = this.MakePort(statementNode, "Variable", VisualPortRole.DataIn);
        statementNode.AddPort(targetPort);
        this.CreateTargetIdentifierNode(node.Target, targetPort, statementNode.Position, diagram);
        return statementNode;
    }

    /// <summary>
    /// Creates a visual node for a conditional statement, including its branches and closer.
    /// </summary>
    /// <param name="node">The conditional statement node.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="diagram">The diagram context for adding visual elements.</param>
    /// <returns>The visual node model for the conditional statement.</returns>
    private TopsyTurvyVisualNodeModel CreateConditionalNode(ConditionalNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        BlazorDiagramsPoint position = layout.NextPrimaryPosition();
        TopsyTurvyVisualNodeModel openerNode = this.MakeNode(position, "SHOULD IT TRANSPIRE THAT", null, VisualNodeKind.Conditional);
        openerNode.StatementType = "ConditionalOpener";
        openerNode.AddPort(this.MakePort(openerNode, "In", VisualPortRole.FlowIn));
        openerNode.AstNode = node;

        TopsyTurvyVisualPortModel conditionPort = this.MakePort(openerNode, "Cond", VisualPortRole.DataIn);
        openerNode.AddPort(conditionPort);
        this.CreateExpressionNode(node.Condition, conditionPort, layout, diagram, anchor: position);

        // Branch Out ports fan out from the opener.
        TopsyTurvyVisualPortModel truePort = this.MakePort(openerNode, "True", VisualPortRole.BranchOut);
        openerNode.AddPort(truePort);

        List<TopsyTurvyVisualPortModel> elseIfPorts = [];
        for (int i = 0; i < node.ElseIfs.Count; i++)
        {
            TopsyTurvyVisualPortModel elseIfPort = this.MakePort(openerNode, $"Else If {i + 1}", VisualPortRole.BranchOut);
            openerNode.AddPort(elseIfPort);
            elseIfPorts.Add(elseIfPort);
        }

        TopsyTurvyVisualPortModel? elsePort = null;
        if (node.ElseBlock.Count > 0)
        {
            elsePort = this.MakePort(openerNode, "Else", VisualPortRole.BranchOut);
            openerNode.AddPort(elsePort);
        }

        // Build branches first so we can measure the deepest tail before placing the closer.
        // Branches are centred around the opener X: branch i at `position.X + (i - (N-1)/2) * spacing`.
        // Spacing of 660 accommodates Else If condition expression trees (up to 2 levels = 420 px to the left).
        List<TopsyTurvyVisualNodeModel> tails = [];
        int branchCount = 1 + node.ElseIfs.Count + (node.ElseBlock.Count > 0 ? 1 : 0);
        const double conditionalSpacing = 660.0;

        // True branch (index 0).
        {
            double xPosition = position.X + (0 - (branchCount - 1) / 2.0) * conditionalSpacing;
            TopsyTurvyVisualNodeModel tail = this.CreateBranchWithHeader(truePort, node.TrueBlock, "QUITE SO.", null, VisualNodeKind.Conditional,
                new NodeLayoutContext(
                    primaryX: xPosition,
                    secondaryX: xPosition - 220,
                    startY: position.Y + NodeLayoutContext.RowSpacing),
                    diagram,
                    statementType: "ConditionalTrueBranch");
            
            tails.Add(tail);
        }

        for (int i = 0; i < node.ElseIfs.Count; i++)
        {
            double xPosition = position.X + (i + 1 - (branchCount - 1) / 2.0) * conditionalSpacing;
            TopsyTurvyVisualNodeModel tail = this.CreateBranchWithHeader(
                elseIfPorts[i],
                node.ElseIfs[i].Block,
                "OR, IF NOT,",
                null,
                VisualNodeKind.Conditional,
                new NodeLayoutContext(primaryX: xPosition, secondaryX: xPosition - 220, startY: position.Y + NodeLayoutContext.RowSpacing),
                diagram,
                condition: node.ElseIfs[i].Condition,
                statementType: "ConditionalElseIfBranch");
            
            tails.Add(tail);
        }

        if (elsePort is not null)
        {
            int elseIndex = 1 + node.ElseIfs.Count;
            double xPosition = position.X + (elseIndex - (branchCount - 1) / 2.0) * conditionalSpacing;
            TopsyTurvyVisualNodeModel tail = this.CreateBranchWithHeader(
                elsePort,
                node.ElseBlock,
                "OTHERWISE,",
                null,
                VisualNodeKind.Conditional,
                new NodeLayoutContext(primaryX: xPosition, secondaryX: xPosition - 220, startY: position.Y + NodeLayoutContext.RowSpacing), diagram,
                statementType: "ConditionalElseBranch");
            
            tails.Add(tail);
        }

        // Place the closer below the deepest branch tail then advance the main layout past it.
        double maxTailY = tails.Count > 0
            ? tails.Max(tail => tail.Position.Y)
            : position.Y;
        
        double closerY = maxTailY + NodeLayoutContext.RowSpacing;
        layout.AdvancePrimaryYTo(closerY + NodeLayoutContext.RowSpacing);

        TopsyTurvyVisualNodeModel closerNode = this.MakeNode(new BlazorDiagramsPoint(position.X, closerY), "SO MUCH FOR THAT.", null, VisualNodeKind.Conditional);
        closerNode.StatementType = "ConditionalCloser";
        closerNode.AddPort(this.MakePort(closerNode, "In", VisualPortRole.FlowIn));
        closerNode.AddPort(this.MakePort(closerNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(closerNode);

        openerNode.PairedCloserId = closerNode.Id;
        closerNode.PairedOpenerId = openerNode.Id;

        foreach (TopsyTurvyVisualNodeModel tail in tails)
        {
            this.LinkFlow(tail, closerNode, diagram);
        }

        this.lastBlockCloser = closerNode;
        return openerNode;
    }

    /// <summary>
    /// Creates a visual node for a loop statement, including its body and closer.
    /// </summary>
    /// <param name="node">The loop node to create a visual representation for.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The visual node representing the loop statement.</returns>
    private TopsyTurvyVisualNodeModel CreateLoopNode(LoopNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        BlazorDiagramsPoint position = layout.NextPrimaryPosition();
        string subtitle = node.Label is not null
            ? $"{node.Type} ({node.Label})"
            : node.Type.ToString();

        TopsyTurvyVisualNodeModel openerNode = this.MakeNode(position, "BY A LEGAL FICTION", subtitle, VisualNodeKind.Loop);
        openerNode.StatementType = "LoopOpener";
        openerNode.AddPort(this.MakePort(openerNode, "In", VisualPortRole.FlowIn));
        openerNode.AstNode = node;

        TopsyTurvyVisualPortModel bodyPort = this.MakePort(openerNode, "Body", VisualPortRole.BranchOut);
        openerNode.AddPort(bodyPort);

        if (node.Condition is not null)
        {
            TopsyTurvyVisualPortModel conditionPort = this.MakePort(openerNode, "Cond", VisualPortRole.DataIn);
            openerNode.AddPort(conditionPort);
            this.CreateExpressionNode(node.Condition, conditionPort, layout, diagram, anchor: position);
        }

        if (node.Step is not null)
        {
            TopsyTurvyVisualPortModel stepPort = this.MakePort(openerNode, "Step", VisualPortRole.DataIn);
            openerNode.AddPort(stepPort);
            this.CreateExpressionNode(node.Step, stepPort, layout, diagram, anchor: position);
        }

        TopsyTurvyVisualNodeModel? bodyTail = null;
        if (node.Body.Count > 0)
        {
            NodeLayoutContext bodyLayout = new(primaryX: position.X, secondaryX: position.X - 220, startY: position.Y + NodeLayoutContext.RowSpacing);
            bodyTail = this.BuildBodySubgraph(node.Body, bodyPort, bodyLayout, diagram);
        }

        double closerY = (bodyTail is not null ? bodyTail.Position.Y : position.Y) + NodeLayoutContext.RowSpacing;
        layout.AdvancePrimaryYTo(closerY + NodeLayoutContext.RowSpacing);

        TopsyTurvyVisualNodeModel closerNode = this.MakeNode(new BlazorDiagramsPoint(position.X, closerY), "THE TERM EXPIRES.", null, VisualNodeKind.Loop);
        closerNode.StatementType = "LoopCloser";
        closerNode.AddPort(this.MakePort(closerNode, "In", VisualPortRole.FlowIn));
        closerNode.AddPort(this.MakePort(closerNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(closerNode);

        openerNode.PairedCloserId = closerNode.Id;
        closerNode.PairedOpenerId = openerNode.Id;

        if (bodyTail is not null)
        {
            this.LinkFlow(bodyTail, closerNode, diagram);
        }

        this.lastBlockCloser = closerNode;
        return openerNode;
    }

    /// <summary>
    /// Creates the subtitle for a function signature node, including the function name and return type if present.
    /// </summary>
    /// <param name="node">The function definition node.</param>
    /// <returns>A string representing the function signature subtitle.</returns>
    private static string FunctionSubtitle(FunctionDefinitionNode node)
    {
        string returnLabel = node.ReturnType is not null
            ? $"→ {FormatLiteralType(node.ReturnType.Value)}"
            : string.Empty;
        
        return returnLabel.Length > 0
            ? $"{node.Name} {returnLabel}"
            : node.Name;
    }

    /// <summary>
    /// Creates a visual node for a function signature, including its parameters and return type if present.
    /// </summary>
    /// <param name="node">The function definition node.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <returns>A visual node representing the function signature.</returns>
    private TopsyTurvyVisualNodeModel CreateFunctionSignatureNode(FunctionDefinitionNode node, NodeLayoutContext layout)
    {
        string subtitle = FunctionSubtitle(node);

        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "IT IS MY DUTY TO PERFORM", subtitle, VisualNodeKind.Function);
        statementNode.StatementType = "FunctionSignatureNode";
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;
        statementNode.SymbolIdentifierNodeName = node.Name;

        foreach (TypedParameter parameter in node.Parameters)
        {
            string paramLabel = $"{parameter.Name} : {FormatLiteralType(parameter.Type)}";
            statementNode.AddPort(this.MakePort(statementNode, paramLabel, VisualPortRole.DataOut));
        }

        return statementNode;
    }

    /// <summary>
    /// Creates a self-contained subgraph for a function body in the function-definition column.
    /// </summary>
    /// <param name="node">The function definition node containing the body statements.</param>
    /// <param name="layout">The layout context for positioning the nodes in the function body.</param>
    /// <param name="diagram">The diagram to which the function body nodes will be added.</param>
    /// <returns>The number of body statement nodes created, used to advance the function-body Y cursor.</returns>
    private int CreateFunctionBodySubgraph(FunctionDefinitionNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        string subtitle = FunctionSubtitle(node);
        TopsyTurvyVisualNodeModel openerNode = this.MakeNode(layout.NextPrimaryPosition(), "IT IS MY DUTY TO PERFORM", subtitle, VisualNodeKind.Function);
        openerNode.StatementType = "FunctionBodyOpener";
        openerNode.SymbolIdentifierNodeName = node.Name;
        openerNode.AstNode = node;
        openerNode.NodeLiteralType = node.ReturnType;
        openerNode.AddPort(this.MakePort(openerNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(openerNode);

        BlazorDiagramsPoint openerPosition = openerNode.Position;
        for (int i = 0; i < node.Parameters.Count; i++)
        {
            TypedParameter parameter = node.Parameters[i];
            TopsyTurvyVisualPortModel paramPort = this.MakePort(openerNode, $"Param {i + 1}", VisualPortRole.DataIn);
            openerNode.AddPort(paramPort);

            TopsyTurvyVisualNodeModel termNode = this.MakeNode(
                new BlazorDiagramsPoint(openerPosition.X - 280, openerPosition.Y + i * NodeLayoutContext.RowSpacing),
                "TERM",
                parameter.Name,
                VisualNodeKind.Parameter);
            
            termNode.StatementType = "ParameterNode";
            termNode.SymbolIdentifierNodeName = parameter.Name;
            termNode.NodeLiteralType = parameter.Type;

            TopsyTurvyVisualPortModel termOutPort = this.MakePort(termNode, "Out", VisualPortRole.DataOut);
            termNode.AddPort(termOutPort);

            diagram.Nodes.Add(termNode);
            diagram.Links.Add(new LinkModel(termOutPort, paramPort));
        }

        TopsyTurvyVisualNodeModel? previousNode = openerNode;
        int bodyCount = 0;

        this.currentFunctionParameters = [.. node.Parameters.Select(parameter => parameter.Name)];
        foreach (Statement statement in node.Body)
        {
            this.lastBlockCloser = null;
            TopsyTurvyVisualNodeModel statementNode = this.CreateStatementNode(statement, layout, diagram);
            diagram.Nodes.Add(statementNode);
            this.LinkFlow(previousNode, statementNode, diagram);
            previousNode = this.lastBlockCloser ?? statementNode;
            bodyCount++;
        }

        this.currentFunctionParameters.Clear();

        TopsyTurvyVisualNodeModel closerNode = this.MakeNode(layout.NextPrimaryPosition(), "MY DUTY IS DISCHARGED.", null, VisualNodeKind.Function);
        closerNode.StatementType = "FunctionBodyCloser";
        closerNode.AddPort(this.MakePort(closerNode, "In", VisualPortRole.FlowIn));
        diagram.Nodes.Add(closerNode);

        openerNode.PairedCloserId = closerNode.Id;
        closerNode.PairedOpenerId = openerNode.Id;

        this.LinkFlow(previousNode, closerNode, diagram);

        return bodyCount;
    }

    /// <summary>
    /// Creates a visual node for a return statement, including its value expression if present.
    /// </summary>
    /// <param name="node">The return statement node.</param>
    /// <param name="layout">The layout context for positioning nodes.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The created visual node model.</returns>
    private TopsyTurvyVisualNodeModel CreateReturnNode(ReturnNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "AND SO I FIND", null, VisualNodeKind.Function);
        statementNode.StatementType = "ReturnNode";
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;

        if (node.Value is not null)
        {
            TopsyTurvyVisualPortModel dataInPort = this.MakePort(statementNode, "Value", VisualPortRole.DataIn);
            statementNode.AddPort(dataInPort);
            this.CreateExpressionNode(node.Value, dataInPort, layout, diagram, anchor: statementNode.Position);
        }

        return statementNode;
    }

    /// <summary>
    /// Creates a visual node for a throw statement, including its value expression.
    /// </summary>
    /// <param name="node">The throw statement node.</param>
    /// <param name="layout">The layout context for positioning nodes.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The created visual node model.</returns>
    private TopsyTurvyVisualNodeModel CreateThrowNode(ThrowNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "A HIDEOUS CURSE ON", null, VisualNodeKind.ErrorHandling);
        statementNode.StatementType = "ThrowNode";
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;

        TopsyTurvyVisualPortModel dataInPort = this.MakePort(statementNode, "Value", VisualPortRole.DataIn);
        statementNode.AddPort(dataInPort);

        this.CreateExpressionNode(node.Value, dataInPort, layout, diagram, anchor: statementNode.Position);
        return statementNode;
    }

    /// <summary>
    /// Creates a visual node for a try-catch statement, including its success and exception branches and closer.
    /// </summary>
    /// <param name="node">The try-catch statement node.</param>
    /// <param name="layout">The layout context for positioning nodes.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The created visual node model.</returns>
    private TopsyTurvyVisualNodeModel CreateTryCatchNode(TryCatchNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        BlazorDiagramsPoint position = layout.NextPrimaryPosition();
        TopsyTurvyVisualNodeModel openerNode = this.MakeNode(position, "WITH THE GREATEST RESPECT,", $"catch: {node.CaughtValueName}", VisualNodeKind.ErrorHandling);
        openerNode.StatementType = "TryCatchOpener";
        openerNode.SymbolIdentifierNodeName = node.CaughtValueName;
        openerNode.AddPort(this.MakePort(openerNode, "In", VisualPortRole.FlowIn));
        openerNode.AstNode = node;

        TopsyTurvyVisualPortModel operationPort = this.MakePort(openerNode, "Op", VisualPortRole.DataIn);
        openerNode.AddPort(operationPort);
        this.CreateExpressionNode(node.Operation, operationPort, layout, diagram, anchor: position);

        // Branch Out ports fan out from the opener to explicit branch-keyword header nodes.
        TopsyTurvyVisualPortModel successPort = this.MakePort(openerNode, "Success", VisualPortRole.BranchOut);
        openerNode.AddPort(successPort);

        TopsyTurvyVisualPortModel errorPort = this.MakePort(openerNode, "Error", VisualPortRole.BranchOut);
        openerNode.AddPort(errorPort);

        // Build branches first so the deepest tail can be measured before placing the closer.
        // Two branches centred around the opener with success on the left and error on the right.
        List<TopsyTurvyVisualNodeModel> tails = [];
        const double tryCatchSpacing = 660.0;

        // Success branch (index 0 of 2), exception branch (index 1 of 2) centred around the opener.
        double successX = position.X - tryCatchSpacing / 2.0;
        tails.Add(this.CreateBranchWithHeader(
            successPort,
            node.SuccessBlock,
            "WITH GRATITUDE",
            null,
            VisualNodeKind.ErrorHandling,
            new NodeLayoutContext(primaryX: successX, secondaryX: successX - 220, startY: position.Y + NodeLayoutContext.RowSpacing), diagram,
            statementType: "TryCatchSuccessBranch"));

        double errorX = position.X + tryCatchSpacing / 2.0;
        tails.Add(this.CreateBranchWithHeader(
            errorPort,
            node.ExceptionBlock,
            "MODIFIED RAPTURE,",
            node.CaughtValueName,
            VisualNodeKind.ErrorHandling,
            new NodeLayoutContext(primaryX: errorX, secondaryX: errorX - 220, startY: position.Y + NodeLayoutContext.RowSpacing), diagram,
            statementType: "TryCatchErrorBranch"));

        double maxTailY = tails.Count > 0
            ? tails.Max(tail => tail.Position.Y)
            : position.Y;
        
        double closerY = maxTailY + NodeLayoutContext.RowSpacing;
        layout.AdvancePrimaryYTo(closerY + NodeLayoutContext.RowSpacing);

        TopsyTurvyVisualNodeModel closerNode = this.MakeNode(new BlazorDiagramsPoint(position.X, closerY), "THAT CONCLUDES THE MATTER.", null, VisualNodeKind.ErrorHandling);
        closerNode.StatementType = "TryCatchCloser";
        closerNode.AddPort(this.MakePort(closerNode, "In", VisualPortRole.FlowIn));
        closerNode.AddPort(this.MakePort(closerNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(closerNode);

        openerNode.PairedCloserId = closerNode.Id;
        closerNode.PairedOpenerId = openerNode.Id;

        foreach (TopsyTurvyVisualNodeModel tail in tails)
        {
            this.LinkFlow(tail, closerNode, diagram);
        }

        this.lastBlockCloser = closerNode;
        return openerNode;
    }

    /// <summary>
    /// Creates a visual node for a switch statement, including its case branches and closer.
    /// </summary>
    /// <param name="node">The switch node to create a visual representation for.</param>
    /// <param name="layout">The layout context for positioning nodes.</param>
    /// <param name="diagram">The diagram to which the nodes will be added.</param>
    /// <returns>The opener node of the switch statement.</returns>
    private TopsyTurvyVisualNodeModel CreateSwitchNode(SwitchNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        BlazorDiagramsPoint position = layout.NextPrimaryPosition();
        TopsyTurvyVisualNodeModel opener = this.MakeNode(position, "IN WHICH CAPACITY?", null, VisualNodeKind.Conditional);
        opener.StatementType = "SwitchOpener";
        opener.AddPort(this.MakePort(opener, "In", VisualPortRole.FlowIn));
        opener.AstNode = node;

        TopsyTurvyVisualPortModel expressionPort = this.MakePort(opener, "Expr", VisualPortRole.DataIn);
        opener.AddPort(expressionPort);
        this.CreateExpressionNode(node.Expression, expressionPort, layout, diagram, anchor: position);

        // One Branch Out port per case and fan from the opener.
        List<TopsyTurvyVisualPortModel> casePorts = [];
        for (int i = 0; i < node.Cases.Count; i++)
        {
            string label = node.Cases[i].Literal?.ToString() ?? $"case {i + 1}";
            TopsyTurvyVisualPortModel casePort = this.MakePort(opener, label, VisualPortRole.BranchOut);
            opener.AddPort(casePort);
            casePorts.Add(casePort);
        }

        TopsyTurvyVisualPortModel? defaultPort = null;
        if (node.DefaultBlock.Count > 0)
        {
            defaultPort = this.MakePort(opener, "Default", VisualPortRole.BranchOut);
            opener.AddPort(defaultPort);
        }

        // Build all case branches first so the deepest tail can be measured before placing the closer.
        // Branches are centred around the opener X with uniform spacing.
        List<TopsyTurvyVisualNodeModel> tails = [];
        int switchBranchCount = node.Cases.Count + (node.DefaultBlock.Count > 0 ? 1 : 0);
        const double switchSpacing = 440.0;

        // WHEN ACTING AS header nodes.
        for (int i = 0; i < node.Cases.Count; i++)
        {
            string label = node.Cases[i].Literal?.ToString() ?? $"Case {i + 1}";
            double xPosition = position.X + (i - (switchBranchCount - 1) / 2.0) * switchSpacing;
            TopsyTurvyVisualNodeModel tail = this.CreateBranchWithHeader(
                casePorts[i],
                node.Cases[i].Block,
                "WHEN ACTING AS",
                label,
                VisualNodeKind.Conditional,
                new NodeLayoutContext(primaryX: xPosition, secondaryX: xPosition - 220, startY: position.Y + NodeLayoutContext.RowSpacing), diagram,
                statementType: "SwitchCaseBranch");
            
            tails.Add(tail);
        }

        // FAILING ALL OF THE ABOVE, header node for the default block.
        if (defaultPort is not null)
        {
            double xPosition = position.X + (node.Cases.Count - (switchBranchCount - 1) / 2.0) * switchSpacing;
            TopsyTurvyVisualNodeModel tail = this.CreateBranchWithHeader(
                defaultPort,
                node.DefaultBlock,
                "FAILING ALL OF THE ABOVE,",
                null,
                VisualNodeKind.Conditional,
                new NodeLayoutContext(primaryX: xPosition, secondaryX: xPosition - 220, startY: position.Y + NodeLayoutContext.RowSpacing), diagram,
                statementType: "SwitchDefaultBranch");
            
            tails.Add(tail);
        }

        double maxTailY = tails.Count > 0
            ? tails.Max(tail => tail.Position.Y)
            : position.Y;
        
        double closerY = maxTailY + NodeLayoutContext.RowSpacing;
        layout.AdvancePrimaryYTo(closerY + NodeLayoutContext.RowSpacing);

        TopsyTurvyVisualNodeModel closer = this.MakeNode(new BlazorDiagramsPoint(position.X, closerY), "NOTHING COULD BE MORE SATISFACTORY.", null, VisualNodeKind.Conditional);
        closer.StatementType = "SwitchCloser";
        closer.AddPort(this.MakePort(closer, "In", VisualPortRole.FlowIn));
        closer.AddPort(this.MakePort(closer, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(closer);

        opener.PairedCloserId = closer.Id;
        closer.PairedOpenerId = opener.Id;

        foreach (TopsyTurvyVisualNodeModel tail in tails)
        {
            this.LinkFlow(tail, closer, diagram);
        }

        this.lastBlockCloser = closer;
        return opener;
    }

    /// <summary>
    /// Creates a visual node for an import statement.
    /// </summary>
    /// <param name="node">The import node to create a visual representation for.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <returns>The created visual node model.</returns>
    private TopsyTurvyVisualNodeModel CreateImportNode(ImportNode node, NodeLayoutContext layout)
    {
        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "PRAY ADMIT", node.FilePath, VisualNodeKind.Other);
        statementNode.StatementType = "ImportNode";
        statementNode.SymbolIdentifierNodeName = node.FilePath;
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;
        return statementNode;
    }

    /// <summary>
    /// Creates a named header node at the top of a branch column and wires it to the body subgraph.
    /// </summary>
    /// <remarks>
    /// The header has a Flow In port, linked from <paramref name="branchPort"/>, and a Flow Out port chained
    /// to the first body statement.  An optional <paramref name="condition"/> adds a Data In port, used by
    /// Else If branches.
    /// </remarks>
    /// <returns>
    /// The last node in the branch: tail of the body or the header itself when body is empty.
    /// The caller links this node Flow Out to the block closer Flow In.
    /// </returns>
    private TopsyTurvyVisualNodeModel CreateBranchWithHeader(
        TopsyTurvyVisualPortModel branchPort,
        IReadOnlyList<Statement> body,
        string headerTitle,
        string? headerSubtitle,
        VisualNodeKind headerKind,
        NodeLayoutContext layout,
        BlazorDiagram diagram,
        Expression? condition = null,
        string? statementType = null)
    {
        BlazorDiagramsPoint headerPosition = layout.NextPrimaryPosition();
        TopsyTurvyVisualNodeModel headerNode = this.MakeNode(headerPosition, headerTitle, headerSubtitle, headerKind);
        headerNode.StatementType = statementType;

        TopsyTurvyVisualPortModel headerInPort = this.MakePort(headerNode, "In", VisualPortRole.FlowIn);
        headerNode.AddPort(headerInPort);

        TopsyTurvyVisualPortModel headerOutPort = this.MakePort(headerNode, "Out", VisualPortRole.FlowOut);
        headerNode.AddPort(headerOutPort);

        if (condition is not null)
        {
            TopsyTurvyVisualPortModel conditionPort = this.MakePort(headerNode, "Cond", VisualPortRole.DataIn);
            headerNode.AddPort(conditionPort);
            this.CreateExpressionNode(condition, conditionPort, layout, diagram, anchor: headerPosition);
        }

        diagram.Nodes.Add(headerNode);
        diagram.Links.Add(new LinkModel(branchPort, headerInPort));

        if (body.Count > 0)
        {
            return this.BuildBodySubgraph(body, headerOutPort, layout, diagram);
        }

        return headerNode;
    }

    /// <summary>
    /// Chains a sequence of statement nodes as a subgraph, linking the first to <paramref name="entryPort"/>.
    /// </summary>
    /// <returns>The last node in the subgraph, so the caller can link it onward, e.g. to a block closer.</returns>
    private TopsyTurvyVisualNodeModel BuildBodySubgraph(
        IReadOnlyList<Statement> body,
        TopsyTurvyVisualPortModel entryPort,
        NodeLayoutContext layout,
        BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel? previousNode = null;
        foreach (Statement statement in body)
        {
            this.lastBlockCloser = null;
            TopsyTurvyVisualNodeModel statementNode = this.CreateStatementNode(statement, layout, diagram);
            diagram.Nodes.Add(statementNode);

            if (previousNode is null)
            {
                PortModel? inPort = FindPort(statementNode, VisualPortRole.FlowIn);
                if (inPort is not null)
                {
                    diagram.Links.Add(new LinkModel(entryPort, inPort));
                }
            }
            else
            {
                this.LinkFlow(previousNode, statementNode, diagram);
            }

            previousNode = this.lastBlockCloser ?? statementNode;
        }

        // Previous node is never null here: body.Count > 0 is guaranteed by all callers.
        return previousNode!;
    }

    /// <summary>
    /// Creates a visual node for a guard statement, including its condition, success path, and else path.
    /// </summary>
    /// <param name="node">The guard node to create a visual representation for.</param>
    /// <param name="layout">The layout context for positioning nodes.</param>
    /// <param name="diagram">The diagram to which the visual nodes will be added.</param>
    /// <returns>The visual node representing the guard statement.</returns>
    private TopsyTurvyVisualNodeModel CreateGuardNode(GuardNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        BlazorDiagramsPoint position = layout.NextPrimaryPosition();
        TopsyTurvyVisualNodeModel openerNode = this.MakeNode(position, "YEOMAN", null, VisualNodeKind.ControlFlow);
        openerNode.StatementType = "GuardOpener";
        openerNode.AddPort(this.MakePort(openerNode, "In", VisualPortRole.FlowIn));
        openerNode.AstNode = node;
        openerNode.AddPort(this.MakePort(openerNode, "Success", VisualPortRole.FlowOut));

        TopsyTurvyVisualPortModel conditionPort = this.MakePort(openerNode, "Cond", VisualPortRole.DataIn);
        openerNode.AddPort(conditionPort);
        this.CreateExpressionNode(node.Condition, conditionPort, layout, diagram, anchor: position);

        TopsyTurvyVisualPortModel otherwisePort = this.MakePort(openerNode, "Failure", VisualPortRole.BranchOut);
        openerNode.AddPort(otherwisePort);

        // OTHERWISE, branch located to the right as success path flows directly to UNDER ORDERS.
        const double guardElseOffset = 500.0;
        double elseX = position.X + guardElseOffset;
        TopsyTurvyVisualNodeModel elseTail = this.CreateBranchWithHeader(
            otherwisePort,
            node.ElseBlock,
            "OTHERWISE,",
            null,
            VisualNodeKind.ControlFlow,
            new NodeLayoutContext(primaryX: elseX, secondaryX: elseX - 220, startY: position.Y + NodeLayoutContext.RowSpacing),
            diagram,
            statementType: "GuardElseBranch");

        double closerY = Math.Max(elseTail.Position.Y, position.Y) + NodeLayoutContext.RowSpacing;
        layout.AdvancePrimaryYTo(closerY + NodeLayoutContext.RowSpacing);

        TopsyTurvyVisualNodeModel closer = this.MakeNode(new BlazorDiagramsPoint(position.X, closerY), "UNDER ORDERS.", null, VisualNodeKind.ControlFlow);
        closer.StatementType = "GuardCloser";
        closer.AddPort(this.MakePort(closer, "In", VisualPortRole.FlowIn));
        closer.AddPort(this.MakePort(closer, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(closer);

        openerNode.PairedCloserId = closer.Id;
        closer.PairedOpenerId = openerNode.Id;

        this.LinkFlow(openerNode, closer, diagram);
        this.LinkFlow(elseTail, closer, diagram);

        this.lastBlockCloser = closer;
        return openerNode;
    }

    /// <summary>
    /// Creates a visual node for an assert statement, including its condition and error message.
    /// </summary>
    /// <param name="node">The assert node to create a visual representation for.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The created visual node model.</returns>
    private TopsyTurvyVisualNodeModel CreateAssertNode(AssertNode node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "THE LAW IS", null, VisualNodeKind.ErrorHandling);
        statementNode.StatementType = "AssertNode";
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;

        TopsyTurvyVisualPortModel conditionPort = this.MakePort(statementNode, "Cond", VisualPortRole.DataIn);
        statementNode.AddPort(conditionPort);

        TopsyTurvyVisualPortModel messagePort = this.MakePort(statementNode, "Msg", VisualPortRole.DataIn);
        statementNode.AddPort(messagePort);
        this.CreateExpressionNode(node.Condition, conditionPort, layout, diagram, anchor: statementNode.Position, portIndex: 0);
        this.CreateExpressionNode(node.ErrorMessage, messagePort, layout, diagram, anchor: statementNode.Position, portIndex: 1);

        return statementNode;
    }

    /// <summary>
    /// Creates a visual node for an expression statement, including its expression and wiring to the consumer node.
    /// </summary>
    /// <param name="node">The expression statement node to create a visual representation for.</param>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="diagram">The diagram to which the node will be added.</param>
    /// <returns>The created visual node model.</returns>
    private TopsyTurvyVisualNodeModel CreateExpressionStatementNode(ExpressionStatement node, NodeLayoutContext layout, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), "EXPRESSION", null, VisualNodeKind.Other);
        statementNode.StatementType = "ExpressionStatement";
        statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        statementNode.AstNode = node;

        TopsyTurvyVisualPortModel expressionPort = this.MakePort(statementNode, "Expr", VisualPortRole.DataIn);
        statementNode.AddPort(expressionPort);
        this.CreateExpressionNode(node.Expression, expressionPort, layout, diagram, anchor: statementNode.Position);

        return statementNode;
    }

    /// <summary>
    /// Creates a simple visual node with optional Flow In and Flow Out ports.
    /// </summary>
    /// <param name="layout">The layout context for positioning the node.</param>
    /// <param name="title">The title of the node.</param>
    /// <param name="subtitle">The subtitle of the node.</param>
    /// <param name="kind">The kind of the visual node.</param>
    /// <param name="flowIn">Indicates whether the node should have a Flow In port.</param>
    /// <param name="flowOut">Indicates whether the node should have a Flow Out port.</param>
    /// <param name="statementType">The type of statement for the node.</param>
    /// <remarks>
    /// This is used for statements that do not require complex wiring.
    /// </remarks>
    /// <returns>The created visual node model.</returns>
    private TopsyTurvyVisualNodeModel CreateSimpleNode(
        NodeLayoutContext layout,
        string title,
        string? subtitle,
        VisualNodeKind kind,
        bool flowIn,
        bool flowOut,
        string? statementType = null)
    {
        TopsyTurvyVisualNodeModel statementNode = this.MakeNode(layout.NextPrimaryPosition(), title, subtitle, kind);
        statementNode.StatementType = statementType;
        if (flowIn)
        {
            statementNode.AddPort(this.MakePort(statementNode, "In", VisualPortRole.FlowIn));
        }

        if (flowOut)
        {
            statementNode.AddPort(this.MakePort(statementNode, "Out", VisualPortRole.FlowOut));
        }

        return statementNode;
    }

    /// <summary>
    /// Creates expression node(s) for the given expression, adds them to the diagram and links their
    /// Data Out port to <paramref name="targetDataInPort"/>.
    /// </summary>
    /// <param name="expression">The expression to render as a node.</param>
    /// <param name="targetDataInPort">The Data In port on the consumer node to wire to.</param>
    /// <param name="layout">The fallback layout context used when <paramref name="anchor"/> is null.</param>
    /// <param name="diagram">The diagram being built.</param>
    /// <param name="anchor">
    /// Position of the consuming node.  When provided, the expression node is placed at
    /// <c>(anchor.X − Expression Column Width, anchor.Y + portIndex × Expression Port SpacingY)</c>
    /// and recursive child expressions propagate the expression node own position as its anchor.
    /// Falls back to <see cref="NodeLayoutContext.NextSecondaryPosition"/> when null.
    /// </param>
    /// <param name="portIndex">
    /// Zero-based index of <paramref name="targetDataInPort"/> among the consumer Data In ports,
    /// used to spread parallel expression inputs vertically.
    /// </param>
    private void CreateExpressionNode(
        Expression expression,
        TopsyTurvyVisualPortModel targetDataInPort,
        NodeLayoutContext layout,
        BlazorDiagram diagram,
        BlazorDiagramsPoint? anchor = null,
        int portIndex = 0)
    {
        BlazorDiagramsPoint position = anchor is not null
            ? new BlazorDiagramsPoint(anchor.X - ExpressionColumnWidth, anchor.Y + portIndex * ExpressionInputPortSpacingY)
            : layout.NextSecondaryPosition();

        if (expression is PrefixExpressionNode prefix)
        {
            if (prefix.Operator == Operator.Summon)
            {
                this.CreateSummonNode(prefix, targetDataInPort, layout, diagram, position);
                return;
            }

            // Generic operator: one Data In per argument and one Data Out wired to the consumer.
            // Added to diagram before recursing so child link targets are valid.
            string title = VisualOperatorMaps.OperatorToTitle.TryGetValue(prefix.Operator, out string? mapped)
                ? mapped
                : prefix.Operator.ToString();

            TopsyTurvyVisualNodeModel operatorNode = this.MakeNode(position, title, null, VisualNodeKind.Operator);
            operatorNode.StatementType = GetOperatorStatementType(prefix.Operator);

            List<TopsyTurvyVisualPortModel> argPorts = [];
            for (int i = 0; i < prefix.Arguments.Count; i++)
            {
                TopsyTurvyVisualPortModel argumentPort = this.MakePort(operatorNode, $"Arg {i + 1}", VisualPortRole.DataIn);
                operatorNode.AddPort(argumentPort);
                argPorts.Add(argumentPort);
            }

            operatorNode.AddPort(this.MakePort(operatorNode, "Out", VisualPortRole.DataOut));
            diagram.Nodes.Add(operatorNode);

            PortModel? operatorOutPort = FindPort(operatorNode, VisualPortRole.DataOut);
            if (operatorOutPort is not null)
            {
                diagram.Links.Add(new LinkModel(operatorOutPort, targetDataInPort));
            }

            for (int i = 0; i < prefix.Arguments.Count; i++)
            {
                this.CreateExpressionNode(prefix.Arguments[i], argPorts[i], layout, diagram, anchor: position, portIndex: i);
            }

            return;
        }

        if (expression is TernaryExpressionNode ternary)
        {
            TopsyTurvyVisualNodeModel ternaryNode = this.MakeNode(position, "SHOULD IT TRANSPIRE THAT", null, VisualNodeKind.Conditional);
            ternaryNode.StatementType = "TernaryNode";
            TopsyTurvyVisualPortModel ternaryConditionPort = this.MakePort(ternaryNode, "Cond", VisualPortRole.DataIn);
            ternaryNode.AddPort(ternaryConditionPort);

            TopsyTurvyVisualPortModel ternaryTruePort = this.MakePort(ternaryNode, "True", VisualPortRole.DataIn);
            ternaryNode.AddPort(ternaryTruePort);

            TopsyTurvyVisualPortModel ternaryFalsePort = this.MakePort(ternaryNode, "False", VisualPortRole.DataIn);
            ternaryNode.AddPort(ternaryFalsePort);

            ternaryNode.AddPort(this.MakePort(ternaryNode, "Out", VisualPortRole.DataOut));
            diagram.Nodes.Add(ternaryNode);

            PortModel? ternaryOut = FindPort(ternaryNode, VisualPortRole.DataOut);
            if (ternaryOut is not null)
            {
                diagram.Links.Add(new LinkModel(ternaryOut, targetDataInPort));
            }

            this.CreateExpressionNode(ternary.Condition, ternaryConditionPort, layout, diagram, anchor: position, portIndex: 0);
            this.CreateExpressionNode(ternary.TrueValue, ternaryTruePort, layout, diagram, anchor: position, portIndex: 1);
            this.CreateExpressionNode(ternary.FalseValue, ternaryFalsePort, layout, diagram, anchor: position, portIndex: 2);
            return;
        }

        if (expression is ArrayIndexNode arrayIndex)
        {
            TopsyTurvyVisualNodeModel indexNode = this.MakeNode(position, "VICTIM", "at index", VisualNodeKind.Identifier);
            indexNode.StatementType = "ArrayIndexNode";
            indexNode.SymbolIdentifierNodeName = arrayIndex.ArrayName;

            TopsyTurvyVisualPortModel varPort = this.MakePort(indexNode, "Variable", VisualPortRole.DataIn);
            indexNode.AddPort(varPort);
            TopsyTurvyVisualPortModel indexPort = this.MakePort(indexNode, "Index", VisualPortRole.DataIn);
            indexNode.AddPort(indexPort);
            indexNode.AddPort(this.MakePort(indexNode, "Out", VisualPortRole.DataOut));
            diagram.Nodes.Add(indexNode);

            PortModel? indexOut = FindPort(indexNode, VisualPortRole.DataOut);
            if (indexOut is not null)
            {
                diagram.Links.Add(new LinkModel(indexOut, targetDataInPort));
            }

            BlazorDiagramsPoint variablePosition = new(position.X - ExpressionColumnWidth, position.Y);
            TopsyTurvyVisualNodeModel variableNode = this.MakeNode(variablePosition, arrayIndex.ArrayName, null, VisualNodeKind.Identifier);
            variableNode.StatementType = "IdentifierNode";
            variableNode.SymbolIdentifierNodeName = arrayIndex.ArrayName;
            variableNode.AddPort(this.MakePort(variableNode, "Out", VisualPortRole.DataOut));
            diagram.Nodes.Add(variableNode);
            PortModel? variableOutPort = FindPort(variableNode, VisualPortRole.DataOut);
            if (variableOutPort is not null)
            {
                diagram.Links.Add(new LinkModel(variableOutPort, varPort));
            }

            this.CreateExpressionNode(arrayIndex.Index, indexPort, layout, diagram, anchor: position, portIndex: 1);
            return;
        }

        if (expression is ArrayLengthNode arrayLength)
        {
            TopsyTurvyVisualNodeModel lengthNode = this.MakeNode(position, "RECKONING OF", null, VisualNodeKind.Operator);
            lengthNode.StatementType = "ArrayLengthNode";
            lengthNode.SymbolIdentifierNodeName = arrayLength.ArrayName;
            TopsyTurvyVisualPortModel arrayPort = this.MakePort(lengthNode, "Variable", VisualPortRole.DataIn);
            lengthNode.AddPort(arrayPort);
            lengthNode.AddPort(this.MakePort(lengthNode, "Out", VisualPortRole.DataOut));
            diagram.Nodes.Add(lengthNode);

            PortModel? lengthOutPort = FindPort(lengthNode, VisualPortRole.DataOut);
            if (lengthOutPort is not null)
            {
                diagram.Links.Add(new LinkModel(lengthOutPort, targetDataInPort));
            }

            BlazorDiagramsPoint indexPosition = new(position.X - ExpressionColumnWidth, position.Y);
            TopsyTurvyVisualNodeModel indexNode = this.MakeNode(indexPosition, arrayLength.ArrayName, null, VisualNodeKind.Identifier);
            indexNode.StatementType = "IdentifierNode";
            indexNode.SymbolIdentifierNodeName = arrayLength.ArrayName;
            indexNode.AddPort(this.MakePort(indexNode, "Out", VisualPortRole.DataOut));
            diagram.Nodes.Add(indexNode);

            PortModel? indexOutPort = FindPort(indexNode, VisualPortRole.DataOut);
            if (indexOutPort is not null)
            {
                diagram.Links.Add(new LinkModel(indexOutPort, arrayPort));
            }

            return;
        }

        if (expression is ExpressionCastNode cast)
        {
            string castSubtitle = $"→ {FormatLiteralType(cast.NewType)}";
            TopsyTurvyVisualNodeModel castNode = this.MakeNode(position, "AS IT WERE", castSubtitle, VisualNodeKind.Operator);
            castNode.StatementType = "ExpressionCastNode";
            castNode.NodeLiteralType = cast.NewType;
            TopsyTurvyVisualPortModel expressionInPort = this.MakePort(castNode, "Expr", VisualPortRole.DataIn);
            castNode.AddPort(expressionInPort);
            castNode.AddPort(this.MakePort(castNode, "Out", VisualPortRole.DataOut));
            diagram.Nodes.Add(castNode);

            PortModel? castOut = FindPort(castNode, VisualPortRole.DataOut);
            if (castOut is not null)
            {
                diagram.Links.Add(new LinkModel(castOut, targetDataInPort));
            }

            this.CreateExpressionNode(cast.Expression, expressionInPort, layout, diagram, anchor: position, portIndex: 0);
            return;
        }

        TopsyTurvyVisualNodeModel? expressionNode = expression switch
        {
            LiteralNode node => this.CreateLiteralExpressionNode(node, position),
            IdentifierNode node => this.CreateIdentifierExpressionNode(node, position),
            _ => null,
        };

        if (expressionNode is null)
        {
            return;
        }

        diagram.Nodes.Add(expressionNode);

        PortModel? dataOutPort = FindPort(expressionNode, VisualPortRole.DataOut);
        if (dataOutPort is not null)
        {
            diagram.Links.Add(new LinkModel(dataOutPort, targetDataInPort));
        }
    }

    /// <summary>
    /// Creates a visual node for a literal expression.
    /// </summary>
    /// <param name="node">The literal node to create a visual representation for.</param>
    /// <param name="pos">The position to place the visual node.</param>
    /// <returns>The created visual node.</returns>
    private TopsyTurvyVisualNodeModel CreateLiteralExpressionNode(LiteralNode node, BlazorDiagramsPoint pos)
    {
        string subtitle = $"{FormatLiteralType(node.Type)} : {node.Value?.ToString() ?? Keywords.Literals.Naught}";
        TopsyTurvyVisualNodeModel expressionNode = this.MakeNode(pos, "Literal", subtitle, VisualNodeKind.Literal);
        expressionNode.StatementType = "LiteralNode";
        expressionNode.AddPort(this.MakePort(expressionNode, "Out", VisualPortRole.DataOut));
        expressionNode.AstNode = node;
        expressionNode.LiteralValue = node.Value?.ToString() ?? Keywords.Literals.Naught;
        expressionNode.NodeLiteralType = node.Type;
        return expressionNode;
    }

    /// <summary>
    /// Creates a visual node for an identifier expression, determining if it is a parameter or a regular identifier.
    /// </summary>
    /// <param name="node">The identifier node to create a visual representation for.</param>
    /// <param name="pos">The position to place the visual node.</param>
    /// <returns>The created visual node.</returns>
    private TopsyTurvyVisualNodeModel CreateIdentifierExpressionNode(IdentifierNode node, BlazorDiagramsPoint pos)
    {
        VisualNodeKind kind = this.currentFunctionParameters.Contains(node.Name)
            ? VisualNodeKind.Parameter
            : VisualNodeKind.Identifier;
        
        TopsyTurvyVisualNodeModel expressionNode = this.MakeNode(pos, node.Name, null, kind);
        expressionNode.StatementType = "IdentifierNode";
        expressionNode.AddPort(this.MakePort(expressionNode, "Out", VisualPortRole.DataOut));
        expressionNode.AstNode = node;
        expressionNode.SymbolIdentifierNodeName = node.Name;
        return expressionNode;
    }

    /// <summary>
    /// Gets the statement type corresponding to the AST node for an operator node based on its operator category.
    /// </summary>
    private static string GetOperatorStatementType(Operator theOperator) => theOperator switch
    {
        Operator.ChordOf or
            Operator.HarmonyOf or
            Operator.DiscordOf or
            Operator.InversionOf or
            Operator.TranspositionUp or
            Operator.TranspositionDown => "BitwiseNode",
        Operator.Both or
            Operator.Either or
            Operator.HardlyEver or
            Operator.Alike or
            Operator.Unlike => "LogicalNode",
        Operator.AllOf or
            Operator.AnyOf => "VariadicNode",
        Operator.WovenOf => "WovenNode",
        _ => "ArithmeticNode",
    };

    /// <summary>
    /// Builds the SUMMON sub-graph.
    /// </summary>
    /// <param name="prefix">The prefix expression node representing the SUMMON operation.</param>
    /// <param name="targetDataInPort">The Data In port on the consumer node to wire to.</param>
    /// <param name="layout">The layout context for positioning nodes.</param>
    /// <param name="diagram">The diagram being built.</param>
    /// <param name="summonPosition">The position to place the SUMMON node.</param>
    private void CreateSummonNode(PrefixExpressionNode prefix, TopsyTurvyVisualPortModel targetDataInPort, NodeLayoutContext layout, BlazorDiagram diagram, BlazorDiagramsPoint summonPosition)
    {
        TopsyTurvyVisualNodeModel summonNode = this.MakeNode(summonPosition, "SUMMON", null, VisualNodeKind.Function);
        summonNode.StatementType = "SummonNode";
        summonNode.AddPort(this.MakePort(summonNode, "In", VisualPortRole.FlowIn));
        TopsyTurvyVisualPortModel functionPort = this.MakePort(summonNode, "Function", VisualPortRole.DataIn);
        summonNode.AddPort(functionPort);

        // The first argument is the function identifier, followed by zero or more argument expressions.
        List<TopsyTurvyVisualPortModel> argumentPorts = [];
        for (int i = 1; i < prefix.Arguments.Count; i++)
        {
            TopsyTurvyVisualPortModel argumentPort = this.MakePort(summonNode, $"Arg {i}", VisualPortRole.DataIn);
            summonNode.AddPort(argumentPort);
            argumentPorts.Add(argumentPort);
        }

        summonNode.AddPort(this.MakePort(summonNode, "Out", VisualPortRole.DataOut));
        summonNode.AddPort(this.MakePort(summonNode, "Out", VisualPortRole.FlowOut));
        diagram.Nodes.Add(summonNode);

        PortModel? summonOut = FindPort(summonNode, VisualPortRole.DataOut);
        if (summonOut is not null)
        {
            diagram.Links.Add(new LinkModel(summonOut, targetDataInPort));
        }

        if (prefix.Arguments.Count == 0)
        {
            return;
        }

        // The function identifier node carries the function name and connects to the SUMMON
        // "Function" Data In port.  Arguments are handled by the SUMMON node.
        string functionTitle = prefix.Arguments[0] is IdentifierNode id
            ? id.Name
            : "Function";

        BlazorDiagramsPoint functionPos = new(summonPosition.X - ExpressionColumnWidth, summonPosition.Y);
        TopsyTurvyVisualNodeModel functionNode = this.MakeNode(functionPos, functionTitle, null, VisualNodeKind.Function);
        functionNode.StatementType = "SummonFunctionNode";
        functionNode.SymbolIdentifierNodeName = functionTitle;
        functionNode.AddPort(this.MakePort(functionNode, "Out", VisualPortRole.DataOut));
        diagram.Nodes.Add(functionNode);

        PortModel? functionOut = FindPort(functionNode, VisualPortRole.DataOut);
        if (functionOut is not null)
        {
            diagram.Links.Add(new LinkModel(functionOut, functionPort));
        }

        for (int i = 0; i < argumentPorts.Count; i++)
        {
            this.CreateExpressionNode(prefix.Arguments[i + 1], argumentPorts[i], layout, diagram, anchor: summonPosition, portIndex: i + 1);
        }
    }

    /// <summary>
    /// Creates a visual node for a target identifier and links it to the specified target port.
    /// </summary>
    /// <param name="name">The name of the identifier.</param>
    /// <param name="targetPort">The target port to link the identifier node to.</param>
    /// <param name="statementPosition">The position of the statement node that consumes the identifier.</param>
    /// <param name="diagram">The diagram to which the identifier node will be added.</param>
    private void CreateTargetIdentifierNode(string name, TopsyTurvyVisualPortModel targetPort, BlazorDiagramsPoint statementPosition, BlazorDiagram diagram)
    {
        BlazorDiagramsPoint position = new(statementPosition.X - ExpressionColumnWidth, statementPosition.Y);
        TopsyTurvyVisualNodeModel identifierVisualNode = this.MakeNode(position, name, null, VisualNodeKind.Identifier);
        identifierVisualNode.StatementType = "IdentifierNode";
        identifierVisualNode.SymbolIdentifierNodeName = name;
        identifierVisualNode.AddPort(this.MakePort(identifierVisualNode, "Out", VisualPortRole.DataOut));
        diagram.Nodes.Add(identifierVisualNode);
        PortModel? outPort = FindPort(identifierVisualNode, VisualPortRole.DataOut);
        if (outPort is not null)
        {
            diagram.Links.Add(new LinkModel(outPort, targetPort));
        }
    }

    /// <summary>
    /// Creates a new visual node model.
    /// </summary>
    /// <param name="position">The position of the node.</param>
    /// <param name="title">The title of the node.</param>
    /// <param name="subtitle">The subtitle of the node.</param>
    /// <param name="kind">The kind of the node.</param>
    /// <returns>A new instance of <see cref="TopsyTurvyVisualNodeModel"/>.</returns>
    private TopsyTurvyVisualNodeModel MakeNode(BlazorDiagramsPoint position, string title, string? subtitle, VisualNodeKind kind)
    {
        string id = $"node-{++this.nodeCounter}";
        return new(id, position, title, subtitle, kind);
    }

    /// <summary>
    /// Creates a new visual port model for a given parent node, label, and role.
    /// </summary>
    /// <param name="parent">The parent node of the port.</param>
    /// <param name="label">The label of the port.</param>
    /// <param name="role">The role of the port.</param>
    /// <returns>A new instance of <see cref="TopsyTurvyVisualPortModel"/>.</returns>
    private TopsyTurvyVisualPortModel MakePort(TopsyTurvyVisualNodeModel parent, string label, VisualPortRole role)
    {
        PortAlignment alignment = role switch
        {
            VisualPortRole.FlowIn  => PortAlignment.Top,
            VisualPortRole.FlowOut => PortAlignment.Bottom,
            VisualPortRole.BranchOut => PortAlignment.Bottom,
            VisualPortRole.DataIn  => PortAlignment.Left,
            VisualPortRole.DataOut => PortAlignment.Right,
            _ => PortAlignment.Bottom,
        };

        return new TopsyTurvyVisualPortModel(parent, alignment, label, role);
    }

    /// <summary>
    /// Finds a port in the given node that matches the specified role.
    /// </summary>
    /// <param name="node">The node to search for the port.</param>
    /// <param name="role">The role of the port to find.</param>
    /// <returns>The port that matches the specified role, or null if not found.</returns>
    private static PortModel? FindPort(TopsyTurvyVisualNodeModel node, VisualPortRole role)
    {
        foreach (PortModel port in node.Ports)
        {
            if (port is TopsyTurvyVisualPortModel visualPort && visualPort.Role == role)
            {
                return visualPort;
            }
        }

        return null;
    }

    /// <summary>
    /// Formats a literal type into a human-readable string representation.
    /// </summary>
    /// <param name="type">The literal type to format.</param>
    /// <returns>A human-readable string representation of the literal type.</returns>
    private static string FormatLiteralType(LiteralType type) => type switch
    {
        LiteralType.Integer => Keywords.TypeNames.Peer,
        LiteralType.Long => Keywords.TypeNames.Chancellor,
        LiteralType.Short => Keywords.TypeNames.Pirate,
        LiteralType.SignedByte => Keywords.TypeNames.SausageRoll,
        LiteralType.UnsignedInteger => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Peer}",
        LiteralType.UnsignedLong => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Chancellor}",
        LiteralType.UnsignedShort => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.Pirate}",
        LiteralType.Byte => $"{Keywords.TypeNames.Standing} {Keywords.TypeNames.SausageRoll}",
        LiteralType.Double => Keywords.TypeNames.Fathom,
        LiteralType.Single => Keywords.TypeNames.Foot,
        LiteralType.String => Keywords.TypeNames.Yarn,
        LiteralType.Char => Keywords.TypeNames.Stitch,
        LiteralType.Boolean => Keywords.TypeNames.Decree,
        LiteralType.Null => Keywords.TypeNames.Naught,
        LiteralType.Array => Keywords.TypeNames.LittleListOf,
        _ => type.ToString(),
    };

}
