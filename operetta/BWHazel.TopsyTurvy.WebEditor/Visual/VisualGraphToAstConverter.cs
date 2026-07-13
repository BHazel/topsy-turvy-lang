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
public sealed class VisualGraphToAstConverter
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

        ProgramNode? originalProgram = harkNode.AstNode as ProgramNode;

        // Reconstruct the statement sequence by walking the Flow Out chain from HARK!.
        // This discovers both existing (AstNode-backed) and factory nodes wired into the flow,
        // enabling true bidirectional editing without relying on the original AST as a backbone.
        List<Statement> allStatements = [];

        // Principals Block: Collect declaration nodes that are not wired into the main flow (no
        // incoming Flow In link). This covers both original AST-backed sidebar nodes and any new
        // factory declaration nodes the user has placed but left floating.
        List<Statement> sidebarDeclarations = [.. diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .Where(visualNode => (visualNode.StatementType == "DeclarationNode" || visualNode.StatementType == "ArrayDeclarationNode")
                        && !HasIncomingFlowLink(visualNode))
            .Select(visualNode => this.ReconstructSingleStatement(visualNode, diagram))
            .OfType<Statement>()];

        if (sidebarDeclarations.Count > 0)
        {
            allStatements.Add(new PrincipalBlockNode()
            {
                Declarations = sidebarDeclarations.AsReadOnly(),
                Span = PlaceholderSpan,
            });
        }

        // Sidebar import nodes are floating and not in the main flow.
        List<Statement> sidebarImports = [.. diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .Where(visualNode => visualNode.StatementType == "ImportNode" && !HasIncomingFlowLink(visualNode))
            .Select(visualNode => this.ReconstructSingleStatement(visualNode, diagram))
            .OfType<Statement>()];
        allStatements.AddRange(sidebarImports);

        // Sidebar namespace directives are floating and not in the main flow.
        List<Statement> sidebarNamespaceDirectives = [.. diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .Where(visualNode => (visualNode.StatementType == "NamespaceDeclarationNode" || visualNode.StatementType == "RecogniseNode")
                        && !HasIncomingFlowLink(visualNode))
            .Select(visualNode => this.ReconstructSingleStatement(visualNode, diagram))
            .OfType<Statement>()];
        allStatements.AddRange(sidebarNamespaceDirectives);

        // Function Definitions: Separate subgraphs identified by FunctionBodyOpener nodes.
        // Each opener has its own body flow chain distinct from the main programme flow.
        foreach (TopsyTurvyVisualNodeModel opener in diagram.Nodes
            .OfType<TopsyTurvyVisualNodeModel>()
            .Where(visualNode => visualNode.StatementType == "FunctionBodyOpener"))
        {
            if (opener.AstNode is FunctionDefinitionNode originalFunctionDefinition)
            {
                TopsyTurvyVisualNodeModel? sigNode = FindNodeByAst(originalFunctionDefinition, diagram);
                allStatements.Add(this.ReconstructFunctionDefinition(originalFunctionDefinition, sigNode ?? opener, diagram));
            }
            else
            {
                allStatements.Add(this.ReconstructFunctionBodyFactory(opener, diagram));
            }
        }

        // Walk the main Flow Out chain for the ordered statement sequence.
        allStatements.AddRange(this.WalkFlowStatements(harkNode, null, diagram));

        string title = harkNode.SymbolIdentifierNodeName ?? originalProgram?.Title ?? "Programme";
        string? subtitle = string.IsNullOrEmpty(harkNode.LiteralValue)
            ? originalProgram?.Subtitle
            : harkNode.LiteralValue;

        return new()
        {
            Title = title,
            Subtitle = subtitle,
            Statements = allStatements,
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Walks the Flow Out chain from the start node, collecting statements.
    /// </summary>
    /// <param name="startNode">The node whose Flow Out chain to walk.</param>
    /// <param name="stopAtId">If non-null, walking stops when a node with this ID is reached.</param>
    /// <param name="diagram">The diagram to walk.</param>
    /// <param name="skipFirst">A value indicating whether to advance past the start node before collecting, <c>false</c> for when the start node is the first real statement.</param>
    /// <remarks>Handles both existing (AstNode-backed) and factory nodes.</remarks>
    /// <returns>A list of reconstructed statements in flow order.</returns>
    private List<Statement> WalkFlowStatements(TopsyTurvyVisualNodeModel startNode, string? stopAtId, BlazorDiagram diagram, bool skipFirst = true)
    {
        List<Statement> statements = [];
        TopsyTurvyVisualNodeModel? currentVisualNode = skipFirst
            ? NextFlowNode(startNode)
            : startNode;
        
        HashSet<string> visitedVisualNodeIds = [];

        while (currentVisualNode is not null && currentVisualNode.Id != stopAtId)
        {
            if (!visitedVisualNodeIds.Add(currentVisualNode.Id))
            {
                break;
            }

            if (currentVisualNode.StatementType == "FinaleNode")
            {
                break;
            }

            // Branch header nodes (e.g. "QUITE SO.", "MODIFIED RAPTURE,", "WHEN ACTING AS …") are
            // structural artefacts inserted by VisualGraphBuilder to label branch columns.  They carry
            // no AST payload and must be skipped; the real body statements follow via their FlowOut.
            if (currentVisualNode.StatementType is
                "ConditionalTrueBranch" or "ConditionalElseIfBranch" or "ConditionalElseBranch" or
                "TryCatchSuccessBranch" or "TryCatchErrorBranch" or
                "SwitchCaseBranch" or "SwitchDefaultBranch" or
                "GuardElseBranch")
            {
                currentVisualNode = NextFlowNode(currentVisualNode);
                continue;
            }

            if (currentVisualNode.PairedCloserId is not null)
            {
                // Block Opener: Reconstruct the whole block then jump to after the closer.
                Statement block = this.ReconstructBlock(currentVisualNode, diagram);
                statements.Add(block);

                TopsyTurvyVisualNodeModel? closer = diagram.Nodes
                    .OfType<TopsyTurvyVisualNodeModel>()
                    .FirstOrDefault(visualNode => visualNode.Id == currentVisualNode.PairedCloserId);
                
                currentVisualNode = closer is not null
                    ? NextFlowNode(closer)
                    : null;
            }
            else
            {
                Statement? statement = this.ReconstructSingleStatement(currentVisualNode, diagram);
                if (statement is not null)
                {
                    statements.Add(statement);
                }

                currentVisualNode = NextFlowNode(currentVisualNode);
            }
        }

        return statements;
    }

    /// <summary>
    /// Gets the next node in the flow chain from the given node, following the Flow Out port and its link.
    /// </summary>
    /// <param name="node">The node whose Flow Out link to follow.</param>
    /// <returns>The next node in the flow chain, or <c>null</c> if there is no Flow Out link.</returns>
    private static TopsyTurvyVisualNodeModel? NextFlowNode(TopsyTurvyVisualNodeModel node)
    {
        TopsyTurvyVisualPortModel? flowOutPort = node.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .FirstOrDefault(port => port.Role == VisualPortRole.FlowOut);

        if (flowOutPort is null)
        {
            return null;
        }

        BaseLinkModel? link = flowOutPort.Links.FirstOrDefault();
        if (link is null)
        {
            return null;
        }

        return (link.Target as SinglePortAnchor)?.Port?.Parent as TopsyTurvyVisualNodeModel;
    }

    /// <summary>
    /// Reconstructs a single statement node, either from the original AST node or by using a factory method for new nodes.
    /// </summary>
    /// <param name="node">The visual node to reconstruct.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed statement, or <c>null</c> if the node cannot be reconstructed.</returns>
    private Statement? ReconstructSingleStatement(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        if (node.AstNode is Statement original)
        {
            return this.ReconstructStatement(original, diagram);
        }

        return node.StatementType switch
        {
            "DeclarationNode" => this.ReconstructDeclarationFactory(node, diagram),
            "ArrayDeclarationNode" => ReconstructArrayDeclarationFactory(node),
            "AssignmentNode" => this.ReconstructAssignmentFactory(node, diagram),
            "ArrayElementAssignmentNode" => this.ReconstructArrayElementAssignmentFactory(node, diagram),
            "PrintNode" => this.ReconstructPrintFactory(node, diagram),
            "InputNode" => ReconstructInputFactory(node),
            "BreakNode" => new BreakNode() { Span = PlaceholderSpan },
            "ContinueNode" => new ContinueNode() { Span = PlaceholderSpan },
            "ProgrammeReturnNode" => this.ReconstructProgrammeReturnFactory(node, diagram),
            "ReturnNode" => this.ReconstructReturnFactory(node, diagram),
            "ThrowNode" => this.ReconstructThrowFactory(node, diagram),
            "ImportNode" => ReconstructImportFactory(node),
            "NamespaceDeclarationNode" => ReconstructNamespaceDeclarationFactory(node),
            "RecogniseNode" => ReconstructRecogniseFactory(node),
            "AssertNode" => this.ReconstructAssertFactory(node, diagram),
            "ExpressionStatement" => this.ReconstructExpressionStatementFactory(node, diagram),
            "SummonNode" => new ExpressionStatement() { Expression = this.ReconstructSummonFromNode(node, diagram), Span = PlaceholderSpan },
            _ => null,
        };
    }

    /// <summary>
    /// Reconstructs a block statement starting from the given opener node.
    /// </summary>
    /// <param name="openerNode">The opener node of the block.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed block statement.</returns>
    private Statement ReconstructBlock(TopsyTurvyVisualNodeModel openerNode, BlazorDiagram diagram)
    {
        if (openerNode.AstNode is Statement original)
        {
            return this.ReconstructStatement(original, diagram);
        }

        return openerNode.StatementType switch
        {
            "ConditionalOpener" => this.ReconstructConditionalFactory(openerNode, diagram),
            "LoopOpener" => this.ReconstructLoopFactory(openerNode, diagram),
            "TryCatchOpener" => this.ReconstructTryCatchFactory(openerNode, diagram),
            "GuardOpener" => this.ReconstructGuardFactory(openerNode, diagram),
            "SwitchOpener" => this.ReconstructSwitchFactory(openerNode, diagram),
            "FunctionBodyOpener" => this.ReconstructFunctionBodyFactory(openerNode, diagram),
            _ => new BreakNode() { Span = PlaceholderSpan },
        };
    }

    /// <summary>
    /// Walks the body of a branch starting from the given opener node and branch port label.
    /// </summary>
    /// <param name="openerNode">The opener node of the branch.</param>
    /// <param name="branchPortLabel">The label of the branch port to follow.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>A list of statements in the branch body.</returns>
    private List<Statement> WalkBranchBody(TopsyTurvyVisualNodeModel openerNode, string branchPortLabel, BlazorDiagram diagram)
    {
        TopsyTurvyVisualNodeModel? branchEntry = GetBranchFlowTarget(openerNode, branchPortLabel);
        if (branchEntry is null)
        {
            return [];
        }

        // Branch-header keyword, such as `QUITE SO.`, are structural nodes that carry no AST payload.
        // WalkFlowStatements skips them via NextFlowNode(startNode) at its entry.
        // When there is no header (loop body, guard else from factory), branchEntry is the first real
        // statement and must not be skipped.
        bool skipFirst = IsBranchHeaderNode(branchEntry);
        return this.WalkFlowStatements(branchEntry, openerNode.PairedCloserId, diagram, skipFirst);
    }

    /// <summary>
    /// Determines if the branch header is a header keyword that carries no AST payload.
    /// </summary>
    private static bool IsBranchHeaderNode(TopsyTurvyVisualNodeModel node) =>
        node.StatementType is
            "ConditionalTrueBranch" or
            "ConditionalElseIfBranch" or
            "ConditionalElseBranch" or
            "TryCatchSuccessBranch" or
            "TryCatchErrorBranch" or
            "SwitchCaseBranch" or
            "SwitchDefaultBranch" or
            "GuardElseBranch";

    /// <summary>
    /// Reconstructs a declaration statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the declaration.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed declaration statement.</returns>
    private Statement ReconstructDeclarationFactory(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        if (node.NodeLiteralType == LiteralType.Array)
        {
            return this.ReconstructArrayDeclarationFromFactory(node, diagram);
        }

        return new DeclarationNode()
        {
            Name = node.SymbolIdentifierNodeName ?? string.Empty,
            NameSpan = PlaceholderSpan,
            Type = node.NodeLiteralType ?? LiteralType.String,
            IsConstant = node.IsIdentifierConstant,
            InitialValue = this.GetExpressionFromDataIn(node, "Value", diagram),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs an array declaration statement from a factory node whose type was set to LITTLE LIST via the editor.
    /// </summary>
    /// <param name="node">The factory node representing the array declaration.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed array declaration statement.</returns>
    private ArrayDeclarationNode ReconstructArrayDeclarationFromFactory(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        IEnumerable<TopsyTurvyVisualPortModel> elementPorts = node.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .Where(port => port.Role == VisualPortRole.DataIn && port.Label?.StartsWith("Victim ") == true)
            .OrderBy(port => port.Label);

        List<Expression> initialValues = [];
        foreach (TopsyTurvyVisualPortModel port in elementPorts)
        {
            Expression? expression = this.GetExpressionFromDataIn(node, port.Label!, diagram);
            if (expression is not null)
            {
                initialValues.Add(expression);
            }
        }

        int? size = initialValues.Count == 0 && int.TryParse(node.LiteralValue, out int initialArraySize)
            ? initialArraySize
            : (int?)null;

        return new()
        {
            Name = node.SymbolIdentifierNodeName ?? string.Empty,
            NameSpan = PlaceholderSpan,
            ElementType = node.ArrayElementLiteralType ?? LiteralType.String,
            Size = size,
            IsConstant = node.IsIdentifierConstant,
            InitialValues = initialValues.AsReadOnly(),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs an array declaration statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the array declaration.</param>
    /// <returns>The reconstructed array declaration statement.</returns>
    private static ArrayDeclarationNode ReconstructArrayDeclarationFactory(TopsyTurvyVisualNodeModel node)
    {
        int? size = int.TryParse(node.LiteralValue, out int initialArraySize) ? initialArraySize : (int?)null;
        return new()
        {
            Name = node.SymbolIdentifierNodeName ?? string.Empty,
            NameSpan = PlaceholderSpan,
            ElementType = node.ArrayElementLiteralType ?? LiteralType.String,
            Size = size,
            IsConstant = node.IsIdentifierConstant,
            InitialValues = (IReadOnlyList<Expression>)[],
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs an assignment statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the assignment.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed assignment statement.</returns>
    private AssignmentNode ReconstructAssignmentFactory(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        return new()
        {
            Target = GetTargetNameFromPort(node) ?? node.SymbolIdentifierNodeName ?? string.Empty,
            Value = this.GetExpressionFromDataIn(node, "Value", diagram) ?? Fallback(),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs an array element assignment statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the array element assignment.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed array element assignment statement.</returns>
    private ArrayElementAssignmentNode ReconstructArrayElementAssignmentFactory(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        return new()
        {
            ArrayName = GetTargetNameFromPort(node) ?? node.SymbolIdentifierNodeName ?? string.Empty,
            Index = this.GetExpressionFromDataIn(node, "Victim", diagram) ?? Fallback(),
            Value = this.GetExpressionFromDataIn(node, "Value", diagram) ?? Fallback(),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs a print statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the print statement.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed print statement.</returns>
    private PrintNode ReconstructPrintFactory(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        return new()
        {
            Expression = this.GetExpressionFromDataIn(node, "Expr", diagram) ?? Fallback(),
            SuppressNewline = node.PrintSuppressNewline,
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs an input statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the input statement.</param>
    /// <returns>The reconstructed input statement.</returns>
    private static InputNode ReconstructInputFactory(TopsyTurvyVisualNodeModel node)
    {
        return new()
        {
            Target = GetTargetNameFromPort(node) ?? node.SymbolIdentifierNodeName ?? string.Empty,
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs a top-level programme return statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the programme return statement.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed programme return statement.</returns>
    private ProgrammeReturnNode ReconstructProgrammeReturnFactory(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        return new()
        {
            Value = this.GetExpressionFromDataIn(node, "Value", diagram) ?? Fallback(),
            Span = PlaceholderSpan
        };
    }

    /// <summary>
    /// Reconstructs a return statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the return statement.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed return statement.</returns>
    private ReturnNode ReconstructReturnFactory(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        return new()
        {
            Value = this.GetExpressionFromDataIn(node, "Value", diagram),
            Span = PlaceholderSpan
        };
    }

    /// <summary>
    /// Reconstructs a throw statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the throw statement.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed throw statement.</returns>
    private ThrowNode ReconstructThrowFactory(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        return new()
        {
            Value = this.GetExpressionFromDataIn(node, "Value", diagram) ?? Fallback(),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs an import statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the import statement.</param>
    /// <returns>The reconstructed import statement.</returns>
    private static ImportNode ReconstructImportFactory(TopsyTurvyVisualNodeModel node)
    {
        return new()
        {
            FilePath = node.SymbolIdentifierNodeName ?? string.Empty,
            Span = PlaceholderSpan
        };
    }

    /// <summary>
    /// Reconstructs a namespace declaration statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the namespace declaration.</param>
    /// <returns>The reconstructed namespace declaration statement.</returns>
    private static NamespaceDeclarationNode ReconstructNamespaceDeclarationFactory(TopsyTurvyVisualNodeModel node)
    {
        return new()
        {
            Path = SplitNamespacePath(node.SymbolIdentifierNodeName),
            Span = PlaceholderSpan
        };
    }

    /// <summary>
    /// Reconstructs a namespace recognition directive from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the recognition directive.</param>
    /// <returns>The reconstructed recognition directive.</returns>
    private static RecogniseNode ReconstructRecogniseFactory(TopsyTurvyVisualNodeModel node)
    {
        return new()
        {
            Path = SplitNamespacePath(node.SymbolIdentifierNodeName),
            Span = PlaceholderSpan
        };
    }

    /// <summary>
    /// Splits a <c>*</c>-joined namespace path string into its ordered segments.
    /// </summary>
    /// <param name="path">The <c>*</c>-joined path, as edited by <see cref="Components.VisualEditor.VisualNodeWidget"/>.</param>
    /// <returns>The ordered, non-empty path segments.</returns>
    private static IReadOnlyList<string> SplitNamespacePath(string? path) =>
        string.IsNullOrWhiteSpace(path)
            ? []
            : path.Split('*', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Reconstructs an assert statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the assert statement.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed assert statement.</returns>
    private AssertNode ReconstructAssertFactory(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        return new()
        {
            Condition = this.GetExpressionFromDataIn(node, "Cond", diagram) ?? Fallback(),
            ErrorMessage = this.GetExpressionFromDataIn(node, "Msg", diagram) ?? Fallback(),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs an expression statement from a factory node in the diagram.
    /// </summary>
    /// <param name="node">The factory node representing the expression statement.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed expression statement.</returns>
    private ExpressionStatement ReconstructExpressionStatementFactory(TopsyTurvyVisualNodeModel node, BlazorDiagram diagram)
    {
        return new()
        {
            Expression = this.GetExpressionFromDataIn(node, "Expr", diagram) ?? Fallback(),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs a conditional statement from a factory node in the diagram.
    /// </summary>
    /// <param name="openerNode">The factory node representing the conditional statement.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed conditional statement.</returns>
    private ConditionalNode ReconstructConditionalFactory(TopsyTurvyVisualNodeModel openerNode, BlazorDiagram diagram)
    {
        List<ElseIfBranch> elseIfs = [];
        List<TopsyTurvyVisualPortModel> elseIfPorts = [.. openerNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .Where(port => port.Role == VisualPortRole.BranchOut && port.Label is not null && port.Label.StartsWith("Else If"))
            .OrderBy(port => port.Label)];

        foreach (TopsyTurvyVisualPortModel elseIfPort in elseIfPorts)
        {
            TopsyTurvyVisualNodeModel? headerNode = GetBranchFlowTarget(openerNode, elseIfPort.Label!);
            Expression condition = headerNode is not null
                ? this.GetExpressionFromDataIn(headerNode, "Cond", diagram) ?? Fallback()
                : Fallback();
            
            List<Statement> body = headerNode is not null
                ? this.WalkFlowStatements(headerNode, openerNode.PairedCloserId, diagram)
                : [];
            
            elseIfs.Add(new ElseIfBranch(condition, body));
        }

        return new()
        {
            Condition = this.GetExpressionFromDataIn(openerNode, "Cond", diagram) ?? Fallback(),
            TrueBlock = this.WalkBranchBody(openerNode, "True", diagram),
            ElseIfs = elseIfs.AsReadOnly(),
            ElseBlock = this.WalkBranchBody(openerNode, "Else", diagram),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs a loop statement from a factory node in the diagram.
    /// </summary>
    /// <param name="openerNode">The factory node representing the loop statement.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed loop statement.</returns>
    private LoopNode ReconstructLoopFactory(TopsyTurvyVisualNodeModel openerNode, BlazorDiagram diagram)
    {
        LoopType loopType = openerNode.Subtitle switch
        {
            "Ascending" => LoopType.Ascending,
            "Descending" => LoopType.Descending,
            "Whilst" => LoopType.Whilst,
            _ => LoopType.Infinite,
        };

        Expression? condition = loopType != LoopType.Infinite
            ? this.GetExpressionFromDataIn(openerNode, "Cond", diagram)
            : null;

        string? loopVariable = loopType is LoopType.Ascending or LoopType.Descending
            ? (GetTargetNameFromPort(openerNode) ?? "i")
            : null;

        Expression? step = loopType is LoopType.Ascending or LoopType.Descending
            ? this.GetExpressionFromDataIn(openerNode, "Step", diagram)
            : null;

        return new()
        {
            Label = null,
            Type = loopType,
            LoopVariable = loopVariable,
            Condition = condition,
            Step = step,
            Body = this.WalkBranchBody(openerNode, "Body", diagram),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs a try-catch statement from a factory node in the diagram.
    /// </summary>
    /// <param name="openerNode">The factory node representing the try-catch statement.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed try-catch statement.</returns>
    private TryCatchNode ReconstructTryCatchFactory(TopsyTurvyVisualNodeModel openerNode, BlazorDiagram diagram)
    {
        return new()
        {
            Operation = this.GetExpressionFromDataIn(openerNode, "Op", diagram) ?? Fallback(),
            CaughtValueName = openerNode.SymbolIdentifierNodeName ?? string.Empty,
            SuccessBlock = this.WalkBranchBody(openerNode, "Success", diagram),
            ExceptionBlock = this.WalkBranchBody(openerNode, "Error", diagram),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs a guard statement from a factory node in the diagram.
    /// </summary>
    /// <param name="openerNode">The factory node representing the guard statement.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed guard statement.</returns>
    private GuardNode ReconstructGuardFactory(TopsyTurvyVisualNodeModel openerNode, BlazorDiagram diagram)
    {
        return new()
        {
            Condition = this.GetExpressionFromDataIn(openerNode, "Cond", diagram) ?? Fallback(),
            ElseBlock = this.WalkBranchBody(openerNode, "Else", diagram),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs a switch statement from a factory node in the diagram.
    /// </summary>
    /// <param name="openerNode">The factory node representing the switch statement.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed switch statement.</returns>
    private SwitchNode ReconstructSwitchFactory(TopsyTurvyVisualNodeModel openerNode, BlazorDiagram diagram)
    {
        return new()
        {
            Expression = this.GetExpressionFromDataIn(openerNode, "Expr", diagram) ?? Fallback(),
            Cases = this.ReconstructSwitchCases(openerNode, diagram),
            DefaultBlock = this.WalkBranchBody(openerNode, "Default", diagram),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Discovers every case on a switch opener directly from its Branch Out ports, resolving each case
    /// literal from its header node and its body by walking the branch flow chain.
    /// </summary>
    /// <remarks>
    /// Reading cases from the live ports, rather than from the <c>Cases</c> list on the original AST, means a
    /// case added interactively is picked up even when the switch itself is
    /// otherwise AST-backed; the AST-backed <c>Cases</c> list is frozen at load time and has no entry for
    /// a case added afterwards.
    /// </remarks>
    /// <param name="openerNode">The opener node of the switch.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed switch cases, in declaration order.</returns>
    private IReadOnlyList<SwitchCase> ReconstructSwitchCases(TopsyTurvyVisualNodeModel openerNode, BlazorDiagram diagram)
    {
        List<TopsyTurvyVisualPortModel> casePorts = [.. openerNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .Where(port => port.Role == VisualPortRole.BranchOut && port.Label != "Default")];

        return [.. casePorts.Select(casePort =>
        {
            TopsyTurvyVisualNodeModel? header = GetBranchFlowTarget(openerNode, casePort.Label!);
            return new SwitchCase(GetCaseLiteralFromHeader(header), this.WalkBranchBody(openerNode, casePort.Label!, diagram));
        })];
    }

    /// <summary>
    /// Reads a switch case literal value from its "WHEN ACTING AS" header node, parsing the header
    /// <see cref="TopsyTurvyVisualNodeModel.LiteralValue"/> according to its <see cref="TopsyTurvyVisualNodeModel.NodeLiteralType"/>.
    /// </summary>
    /// <param name="header">The case header node, or <c>null</c> if the case port has no header wired.</param>
    /// <returns>The parsed literal value, or <c>null</c> if the header has no value set.</returns>
    private static object? GetCaseLiteralFromHeader(TopsyTurvyVisualNodeModel? header) =>
        header?.LiteralValue is not null
            ? ParseLiteralValue(header.NodeLiteralType ?? LiteralType.String, header.LiteralValue)
            : null;

    /// <summary>
    /// Reconstructs a function body from a factory node in the diagram.
    /// </summary>
    /// <param name="openerNode">The factory node representing the function body.</param>
    /// <param name="diagram">The diagram containing the visual node.</param>
    /// <returns>The reconstructed function body.</returns>
    private FunctionDefinitionNode ReconstructFunctionBodyFactory(TopsyTurvyVisualNodeModel openerNode, BlazorDiagram diagram)
    {
        List<Statement> body = this.WalkFlowStatements(openerNode, openerNode.PairedCloserId, diagram);

        List<TypedParameter> parameters = [.. openerNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .Where(port => port.Role == VisualPortRole.DataIn && port.Label?.StartsWith("Param ") == true)
            .OrderBy(port => port.Label)
            .Select(port =>
            {
                TopsyTurvyVisualNodeModel? termNode = GetExpressionSourceNode(port);
                return termNode is not null
                    ? new TypedParameter(
                        termNode.SymbolIdentifierNodeName ?? string.Empty,
                        termNode.NodeLiteralType ?? LiteralType.String,
                        PlaceholderSpan)
                    : null;
            })
            .OfType<TypedParameter>()];

        return new()
        {
            Name = openerNode.SymbolIdentifierNodeName ?? string.Empty,
            NameSpan = PlaceholderSpan,
            Parameters = parameters.AsReadOnly(),
            ReturnType = openerNode.NodeLiteralType,
            Body = body,
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Returns <c>true</c> when the node has at least one incoming link on its FlowIn port,
    /// indicating it is wired into a flow chain (main or branch body).
    /// </summary>
    private static bool HasIncomingFlowLink(TopsyTurvyVisualNodeModel node)
    {
        TopsyTurvyVisualPortModel? flowIn = node.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .FirstOrDefault(p => p.Role == VisualPortRole.FlowIn);
        return flowIn?.Links.Any() == true;
    }

    /// <summary>
    /// A zero-position span used for all reconstructed nodes.
    /// </summary>
    /// <remarks>
    /// Diagnostics are not needed on converter output.
    /// </remarks>
    private static SourceSpan PlaceholderSpan => new(new(0, 0), new(0, 0));

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
            BreakNode => new BreakNode { Span = PlaceholderSpan },
            ContinueNode => new ContinueNode { Span = PlaceholderSpan },
            ProgrammeReturnNode programmeReturn when visualNode is not null => this.ReconstructProgrammeReturn(programmeReturn, visualNode, diagram),
            ReturnNode returnNode when visualNode is not null => this.ReconstructReturn(returnNode, visualNode, diagram),
            ThrowNode throwNode when visualNode is not null => this.ReconstructThrow(throwNode, visualNode, diagram),
            TryCatchNode tryCatch when visualNode is not null => this.ReconstructTryCatch(tryCatch, visualNode, diagram),
            SwitchNode switchNode when visualNode is not null => this.ReconstructSwitch(switchNode, visualNode, diagram),
            ImportNode import when visualNode is not null => ReconstructImport(import, visualNode),
            NamespaceDeclarationNode namespaceDeclaration when visualNode is not null => ReconstructNamespaceDeclaration(namespaceDeclaration, visualNode),
            RecogniseNode recognise when visualNode is not null => ReconstructRecognise(recognise, visualNode),
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
            Span = PlaceholderSpan,
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
        List<TypedParameter> parameters = [.. visualNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .Where(port => port.Role == VisualPortRole.DataIn && port.Label?.StartsWith("Param ") == true)
            .OrderBy(port => port.Label)
            .Select(port =>
            {
                TopsyTurvyVisualNodeModel? termNode = GetExpressionSourceNode(port);
                return termNode is not null
                    ? new TypedParameter(
                        termNode.SymbolIdentifierNodeName ?? string.Empty,
                        termNode.NodeLiteralType ?? LiteralType.String,
                        PlaceholderSpan)
                    : null;
            })
            .OfType<TypedParameter>()];

        return new()
        {
            Name = visualNode.SymbolIdentifierNodeName ?? original.Name,
            NameSpan = original.NameSpan,
            Parameters = parameters.Count > 0 ? parameters.AsReadOnly() : original.Parameters,
            ReturnType = visualNode.NodeLiteralType ?? original.ReturnType,
            Body = this.WalkFlowStatements(visualNode, visualNode.PairedCloserId, diagram),
            Span = PlaceholderSpan,
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
            NameSpan = original.NameSpan,
            Type = visualNode.NodeLiteralType ?? original.Type,
            IsConstant = visualNode.IsIdentifierConstant,
            InitialValue = initialValue,
            Span = PlaceholderSpan,
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
        List<TopsyTurvyVisualPortModel> elementPorts = [.. visualNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .Where(port => port.Role == VisualPortRole.DataIn && port.Label?.StartsWith("Victim ") == true)
            .OrderBy(port => port.Label)];

        List<Expression> values = [];
        if (elementPorts.Count > 0)
        {
            foreach (TopsyTurvyVisualPortModel port in elementPorts)
            {
                Expression? expression = this.GetExpressionFromDataIn(visualNode, port.Label!, diagram);
                if (expression is not null)
                {
                    values.Add(expression);
                }
            }
        }
        else
        {
            for (int i = 0; i < original.InitialValues.Count; i++)
            {
                Expression? expression = this.GetExpressionFromDataIn(visualNode, $"Victim {i + 1}", diagram)
                    ?? this.ReconstructExpressionFromAst(original.InitialValues[i], diagram);
                values.Add(expression);
            }
        }

        int? size = int.TryParse(visualNode.LiteralValue, out int initialArraySize) ? initialArraySize : original.Size;

        return new()
        {
            Name = visualNode.SymbolIdentifierNodeName ?? original.Name,
            NameSpan = original.NameSpan,
            ElementType = visualNode.ArrayElementLiteralType ?? original.ElementType,
            Size = values.Count > 0 ? null : size,
            IsConstant = visualNode.IsIdentifierConstant,
            InitialValues = values.AsReadOnly(),
            Span = PlaceholderSpan,
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
            Target = GetTargetNameFromPort(visualNode) ?? visualNode.SymbolIdentifierNodeName ?? original.Target,
            Value = this.GetExpressionFromDataIn(visualNode, "Value", diagram)
                ?? this.ReconstructExpressionFromAst(original.Value, diagram),
            Span = PlaceholderSpan,
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
            ArrayName = GetTargetNameFromPort(visualNode) ?? original.ArrayName,
            Index = this.GetExpressionFromDataIn(visualNode, "Victim", diagram)
                ?? this.ReconstructExpressionFromAst(original.Index, diagram),
            Value = this.GetExpressionFromDataIn(visualNode, "Value", diagram)
                ?? this.ReconstructExpressionFromAst(original.Value, diagram),
            Span = PlaceholderSpan,
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
            Span = PlaceholderSpan,
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
            Target = GetTargetNameFromPort(visualNode) ?? visualNode.SymbolIdentifierNodeName ?? original.Target,
            Span = PlaceholderSpan,
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

        List<Statement> trueBlock = this.WalkBranchBody(visualNode, "True", diagram);

        List<ElseIfBranch> elseIfBlocks = [];
        for (int i = 0; i < original.ElseIfs.Count; i++)
        {
            TopsyTurvyVisualNodeModel? headerNode = GetBranchFlowTarget(visualNode, $"Else If {i + 1}");
            Expression elseIfCond = headerNode is not null
                ? this.GetExpressionFromDataIn(headerNode, "Cond", diagram) ?? this.ReconstructExpressionFromAst(original.ElseIfs[i].Condition, diagram)
                : this.ReconstructExpressionFromAst(original.ElseIfs[i].Condition, diagram);

            List<Statement> elseIfBody = headerNode is not null
                ? this.WalkFlowStatements(headerNode, visualNode.PairedCloserId, diagram)
                : [];
            
            elseIfBlocks.Add(new ElseIfBranch(elseIfCond, elseIfBody));
        }

        return new()
        {
            Condition = condition,
            TrueBlock = trueBlock,
            ElseIfs = elseIfBlocks.AsReadOnly(),
            ElseBlock = this.WalkBranchBody(visualNode, "Else", diagram),
            Span = PlaceholderSpan,
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
        LoopType loopType = (visualNode.Subtitle ?? string.Empty) switch
        {
            string subtitle when subtitle.StartsWith("Ascending") => LoopType.Ascending,
            string subtitle when subtitle.StartsWith("Descending") => LoopType.Descending,
            string subtitle when subtitle.StartsWith("Whilst") => LoopType.Whilst,
            _ => LoopType.Infinite,
        };

        Expression? condition = loopType != LoopType.Infinite
            ? this.GetExpressionFromDataIn(visualNode, "Cond", diagram) ?? (original.Condition is not null ? this.ReconstructExpressionFromAst(original.Condition, diagram) : null)
            : null;

        string? loopVariable = loopType is LoopType.Ascending or LoopType.Descending
            ? GetTargetNameFromPort(visualNode) ?? original.LoopVariable
            : null;

        Expression? step = loopType is LoopType.Ascending or LoopType.Descending
            ? this.GetExpressionFromDataIn(visualNode, "Step", diagram) ?? (original.Step is not null? this.ReconstructExpressionFromAst(original.Step, diagram) : null)
            : null;

        return new()
        {
            Label = original.Label,
            Type = loopType,
            LoopVariable = loopVariable,
            Condition = condition,
            Step = step,
            Body = this.WalkBranchBody(visualNode, "Body", diagram),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs a top-level programme return statement from the original AST node, its corresponding visual node, and the diagram.
    /// </summary>
    /// <param name="original">The original programme return node.</param>
    /// <param name="visualNode">The visual node corresponding to the programme return statement.</param>
    /// <param name="diagram">The diagram containing visual node information.</param>
    /// <returns>A reconstructed programme return node.</returns>
    private ProgrammeReturnNode ReconstructProgrammeReturn(ProgrammeReturnNode original, TopsyTurvyVisualNodeModel visualNode, BlazorDiagram diagram)
    {
        return new()
        {
            Value = this.GetExpressionFromDataIn(visualNode, "Value", diagram) ?? this.ReconstructExpressionFromAst(original.Value, diagram),
            Span = PlaceholderSpan
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
            Span = PlaceholderSpan
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
            Span = PlaceholderSpan,
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
            SuccessBlock = this.WalkBranchBody(visualNode, "Success", diagram),
            ExceptionBlock = this.WalkBranchBody(visualNode, "Error", diagram),
            Span = PlaceholderSpan,
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

        // Cases are discovered from the diagram live Branch Out ports, not from `original.Cases`, so a
        // case added after load is not silently dropped.
        return new()
        {
            Expression = expression,
            Cases = this.ReconstructSwitchCases(visualNode, diagram),
            DefaultBlock = this.WalkBranchBody(visualNode, "Default", diagram),
            Span = PlaceholderSpan,
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
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs a namespace declaration statement from the original AST node and its corresponding visual node.
    /// </summary>
    /// <param name="original">The original namespace declaration node.</param>
    /// <param name="visualNode">The visual node corresponding to the namespace declaration.</param>
    /// <returns>A reconstructed namespace declaration node.</returns>
    private static NamespaceDeclarationNode ReconstructNamespaceDeclaration(NamespaceDeclarationNode original, TopsyTurvyVisualNodeModel visualNode)
    {
        return new()
        {
            Path = visualNode.SymbolIdentifierNodeName is null ? original.Path : SplitNamespacePath(visualNode.SymbolIdentifierNodeName),
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Reconstructs a namespace recognition directive from the original AST node and its corresponding visual node.
    /// </summary>
    /// <param name="original">The original recognition directive node.</param>
    /// <param name="visualNode">The visual node corresponding to the recognition directive.</param>
    /// <returns>A reconstructed recognition directive node.</returns>
    private static RecogniseNode ReconstructRecognise(RecogniseNode original, TopsyTurvyVisualNodeModel visualNode)
    {
        return new()
        {
            Path = visualNode.SymbolIdentifierNodeName is null ? original.Path : SplitNamespacePath(visualNode.SymbolIdentifierNodeName),
            Span = PlaceholderSpan,
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
            ElseBlock = this.WalkBranchBody(visualNode, "Failure", diagram),
            Span = PlaceholderSpan,
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
            Span = PlaceholderSpan,
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
            Span = PlaceholderSpan,
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
            "OperatorNode" or "ArithmeticNode" or "BitwiseNode" or "LogicalNode" or "VariadicNode" or "WovenNode"
                => this.ReconstructOperatorFromNode(node, diagram),
            "TernaryNode" => this.ReconstructTernaryFromNode(node, diagram),
            "ArrayIndexNode" => this.ReconstructArrayIndexFromNode(node, diagram),
            "ArrayLengthNode" => ReconstructArrayLengthFromNode(node),
            "ExpressionCastNode" => this.ReconstructExpressionCastFromNode(node, diagram),
            "SummonNode" => this.ReconstructSummonFromNode(node, diagram),
            _ => node.AstNode as Expression ?? new LiteralNode { Type = LiteralType.Null, Value = null, Span = PlaceholderSpan },
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
            Span = PlaceholderSpan
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
            Span = PlaceholderSpan,
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
            Span = PlaceholderSpan
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
            Span = PlaceholderSpan
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
            ArrayName = GetTargetNameFromPort(visualNode) ?? visualNode.SymbolIdentifierNodeName ?? visualNode.Title ?? string.Empty,
            Index = arrayIndex,
            Span = PlaceholderSpan,
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
            ArrayName = GetTargetNameFromPort(visualNode) ?? visualNode.SymbolIdentifierNodeName ?? visualNode.Title ?? string.Empty,
            Span = PlaceholderSpan,
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
            Span = PlaceholderSpan
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
        TopsyTurvyVisualPortModel? functionPort = visualNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .FirstOrDefault(port => port.Role == VisualPortRole.DataIn && port.Label == "Function");

        TopsyTurvyVisualNodeModel? functionNode = functionPort is not null
            ? GetExpressionSourceNode(functionPort)
            : null;

        string functionName = functionNode?.SymbolIdentifierNodeName
            ?? functionNode?.Title
            ?? visualNode.SymbolIdentifierNodeName
            ?? visualNode.Title
            ?? "unknown";

        List<Expression> arguments = [];
        arguments.Add(new IdentifierNode() { Name = functionName, Span = PlaceholderSpan });

        List<TopsyTurvyVisualPortModel> summonArgumentPorts = [.. visualNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .Where(port => port.Role == VisualPortRole.DataIn && port.Label != "Function")
            .OrderBy(port => port.Label)];

        IEnumerable<TopsyTurvyVisualPortModel> argumentPorts = summonArgumentPorts.Count > 0
            ? summonArgumentPorts
            : functionNode?.Ports.OfType<TopsyTurvyVisualPortModel>().Where(port => port.Role == VisualPortRole.DataIn)
                ?? [];

        foreach (TopsyTurvyVisualPortModel argumentPort in argumentPorts)
        {
            TopsyTurvyVisualNodeModel? expressionSourceNode = GetExpressionSourceNode(argumentPort);
            if (expressionSourceNode is not null)
            {
                arguments.Add(this.ReconstructExpression(expressionSourceNode, diagram));
            }
        }

        return new PrefixExpressionNode() { Operator = Operator.Summon, Arguments = arguments.AsReadOnly(), Span = PlaceholderSpan };
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
    /// Gets the target name of a variable from a visual node Data In port labeled "Variable".
    /// </summary>
    /// <param name="visualNode">The visual node containing the Data In port.</param>
    /// <returns>The target name of the variable, or <c>null</c> if the port is disconnected or not found.</returns>
    private static string? GetTargetNameFromPort(TopsyTurvyVisualNodeModel visualNode)
    {
        TopsyTurvyVisualPortModel? port = visualNode.Ports
            .OfType<TopsyTurvyVisualPortModel>()
            .FirstOrDefault(port => port.Role == VisualPortRole.DataIn && port.Label == "Variable");
        
        return port is null
            ? null
            : GetExpressionSourceNode(port)?.SymbolIdentifierNodeName;
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
            LiteralType.Char => value.Length > 0 ? value[0] : '\0',
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
        Span = PlaceholderSpan
    };
}
