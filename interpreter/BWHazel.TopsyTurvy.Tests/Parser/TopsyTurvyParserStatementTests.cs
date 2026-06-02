using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for every statement form parsed by <see cref="TopsyTurvyParser"/> class.
/// </summary>
public class TopsyTurvyParserStatementTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that a PRAY WELCOME declaration without an initial value produces a <see cref="DeclarationNode"/> with a null initial value.
    /// </summary>
    [Fact]
    public void Parse_Declaration_WithoutInitialValue_ProducesNullInitialValue()
    {
        DeclarationNode node = this.ParseFirstStatement<DeclarationNode>("PRAY WELCOME x AS A PEER");

        Assert.Equal("x", node.Name);
        Assert.Null(node.InitialValue);
    }

    /// <summary>
    /// Tests that a PRAY WELCOME declaration with a BEING clause populates the initial value.
    /// </summary>
    [Fact]
    public void Parse_Declaration_WithInitialValue_SetsInitialValue()
    {
        DeclarationNode node = this.ParseFirstStatement<DeclarationNode>("PRAY WELCOME x AS A PEER BEING 42");

        Assert.Equal("x", node.Name);
        Assert.NotNull(node.InitialValue);
    }

    /// <summary>
    /// Tests that each type keyword maps to the correct <see cref="LiteralType"/> on the declaration node.
    /// </summary>
    /// <param name="keyword">The type keyword to test.</param>
    /// <param name="expectedType">The expected <see cref="LiteralType"/> corresponding to
    [Theory]
    [InlineData("PEER",   LiteralType.Integer)]
    [InlineData("FATHOM", LiteralType.Float)]
    [InlineData("YARN",   LiteralType.String)]
    [InlineData("DECREE", LiteralType.Boolean)]
    [InlineData("NAUGHT", LiteralType.Null)]
    public void Parse_Declaration_WithTypeKeyword_MapsToCorrectLiteralType(string keyword, LiteralType expectedType)
    {
        DeclarationNode node = this.ParseFirstStatement<DeclarationNode>($"PRAY WELCOME x AS A {keyword}");

        Assert.Equal(expectedType, node.Type);
    }

    /// <summary>
    /// Tests that IS APPOINTED produces an <see cref="AssignmentNode"/> with the correct target name.
    /// </summary>
    [Fact]
    public void Parse_WithAssignment_SetsTargetName()
    {
        AssignmentNode node = this.ParseFirstStatement<AssignmentNode>("Ko-Ko IS APPOINTED 99");

        Assert.Equal("Ko-Ko", node.Target);
    }

    /// <summary>
    /// Tests that IS APPOINTED produces an <see cref="AssignmentNode"/> with a non-null value expression.
    /// </summary>
    [Fact]
    public void Parse_WithAssignment_SetsValueExpression()
    {
        AssignmentNode node = this.ParseFirstStatement<AssignmentNode>("x IS APPOINTED 99");

        Assert.NotNull(node.Value);
    }

    /// <summary>
    /// Tests that IS HENCEFORTH A produces an <see cref="InPlaceCastNode"/> with the correct target and new type.
    /// </summary>
    [Fact]
    public void Parse_WithInPlaceCast_SetsTargetAndNewType()
    {
        InPlaceCastNode node = this.ParseFirstStatement<InPlaceCastNode>("x IS HENCEFORTH A FATHOM");

        Assert.Equal("x", node.Target);
        Assert.Equal(LiteralType.Float, node.NewType);
    }

    /// <summary>
    /// Tests that AS IT WERE produces an <see cref="ExpressionCastNode"/> with the correct new type.
    /// </summary>
    [Fact]
    public void Parse_WithExpressionCast_SetsExpressionAndNewType()
    {
        ExpressionCastNode node = this.ParseFirstStatement<ExpressionCastNode>("AS IT WERE x AS A YARN");

        Assert.NotNull(node.Expression);
        Assert.Equal(LiteralType.String, node.NewType);
    }

    /// <summary>
    /// Tests that BEHOLD produces a <see cref="PrintNode"/> with <see cref="PrintNode.SuppressNewline"/> set to <c>false</c> by default.
    /// </summary>
    [Fact]
    public void Parse_Print_WithoutWithoutCeremony_SuppressNewlineIsFalse()
    {
        PrintNode node = this.ParseFirstStatement<PrintNode>("BEHOLD \"hello\"");

        Assert.NotNull(node.Expression);
        Assert.False(node.SuppressNewline);
    }

    /// <summary>
    /// Tests that BEHOLD … WITHOUT CEREMONY produces a <see cref="PrintNode"/> with <see cref="PrintNode.SuppressNewline"/> set to <c>true</c>.
    /// </summary>
    [Fact]
    public void Parse_Print_WithWithoutCeremony_SuppressNewlineIsTrue()
    {
        PrintNode node = this.ParseFirstStatement<PrintNode>("BEHOLD \"hello\" WITHOUT CEREMONY");
        Assert.True(node.SuppressNewline);
    }

    /// <summary>
    /// Tests that PRAY TELL produces an <see cref="InputNode"/> with the correct target variable name.
    /// </summary>
    [Fact]
    public void Parse_Input_SetsTargetName()
    {
        InputNode node = this.ParseFirstStatement<InputNode>("PRAY TELL answer");
        Assert.Equal("answer", node.Target);
    }

    /// <summary>
    /// Tests that SHOULD IT TRANSPIRE THAT with only a QUITE SO. branch produces a <see cref="ConditionalNode"/> with a populated true block.
    /// </summary>
    [Fact]
    public void Parse_Conditional_WithTrueBranchOnly_SetsTrueBlock()
    {
        string statements = """
            SHOULD IT TRANSPIRE THAT VERITY
              QUITE SO.
                BEHOLD "yes"
            SO MUCH FOR THAT.
            """;

        ConditionalNode node = this.ParseFirstStatement<ConditionalNode>(statements);

        Assert.NotNull(node.Condition);
        Assert.Single(node.TrueBlock);
        Assert.Empty(node.ElseIfs);
        Assert.Empty(node.ElseBlock);
    }

    /// <summary>
    /// Tests that OTHERWISE, populates the else block of a <see cref="ConditionalNode"/>.
    /// </summary>
    [Fact]
    public void Parse_Conditional_WithElseBranch_SetsElseBlock()
    {
        string statements = """
            SHOULD IT TRANSPIRE THAT VERITY
              QUITE SO.
                BEHOLD "yes"
              OTHERWISE,
                BEHOLD "no"
            SO MUCH FOR THAT.
            """;

        ConditionalNode node = this.ParseFirstStatement<ConditionalNode>(statements);

        Assert.Single(node.TrueBlock);
        Assert.Single(node.ElseBlock);
    }

    /// <summary>
    /// Tests that OR, IF NOT, populates the else-if list of a <see cref="ConditionalNode"/>.
    /// </summary>
    [Fact]
    public void Parse_Conditional_WithElseIfBranch_SetsElseIfs()
    {
        string statements = """
            SHOULD IT TRANSPIRE THAT VERITY
              QUITE SO.
                BEHOLD "yes"
              OR, IF NOT,
                BEHOLD "maybe"
            SO MUCH FOR THAT.
            """;

        ConditionalNode node = this.ParseFirstStatement<ConditionalNode>(statements);

        Assert.Single(node.ElseIfs);
        Assert.Single(node.ElseIfs[0].Block);
    }

    /// <summary>
    /// Tests that IN WHICH CAPACITY? produces a <see cref="SwitchNode"/> with the declared cases.
    /// </summary>
    [Fact]
    public void Parse_Switch_WithCases_SetsCases()
    {
        string statements = """
            IN WHICH CAPACITY? x
              WHEN ACTING AS 1
                BEHOLD "one"
              WHEN ACTING AS 2
                BEHOLD "two"
            NOTHING COULD BE MORE SATISFACTORY.
            """;

        SwitchNode node = this.ParseFirstStatement<SwitchNode>(statements);

        Assert.Equal(2, node.Cases.Count);
    }

    /// <summary>
    /// Tests that FAILING ALL OF THE ABOVE, populates the default block of a <see cref="SwitchNode"/>.
    /// </summary>
    [Fact]
    public void Parse_Switch_WithDefaultCase_SetsDefaultBlock()
    {
        string statements = """
            IN WHICH CAPACITY? x
              WHEN ACTING AS 1
                BEHOLD "one"
              FAILING ALL OF THE ABOVE,
                BEHOLD "other"
            NOTHING COULD BE MORE SATISFACTORY.
            """;

        SwitchNode node = this.ParseFirstStatement<SwitchNode>(statements);

        Assert.Single(node.DefaultBlock);
    }

    /// <summary>
    /// Tests that BY A LEGAL FICTION alone produces a <see cref="LoopNode"/> of type <see cref="LoopType.Infinite"/>.
    /// </summary>
    [Fact]
    public void Parse_InfiniteLoop_SetsLoopTypeInfinite()
    {
        string statements = """
            BY A LEGAL FICTION
              THAT WILL DO.
            THE TERM EXPIRES.
            """;

        LoopNode node = this.ParseFirstStatement<LoopNode>(statements);

        Assert.Equal(LoopType.Infinite, node.Type);
        Assert.Null(node.Label);
    }

    /// <summary>
    /// Tests that BY A LEGAL FICTION ASCENDING produces a <see cref="LoopNode"/> with <see cref="LoopType.Ascending"/> and sets the loop variable.
    /// </summary>
    [Fact]
    public void Parse_AscendingLoop_SetsLoopTypeAndVariable()
    {
        string statements = """
            BY A LEGAL FICTION ASCENDING i UNTIL ALIKE i AND 5
              BEHOLD i
            THE TERM EXPIRES.
            """;

        LoopNode node = this.ParseFirstStatement<LoopNode>(statements);

        Assert.Equal(LoopType.Ascending, node.Type);
        Assert.Equal("i", node.LoopVariable);
        Assert.NotNull(node.Condition);
    }

    /// <summary>
    /// Tests that BY A LEGAL FICTION DESCENDING produces a <see cref="LoopNode"/> with <see cref="LoopType.Descending"/> and sets the loop variable.
    /// </summary>
    [Fact]
    public void Parse_DescendingLoop_SetsLoopTypeAndVariable()
    {
        string statements = """
            BY A LEGAL FICTION DESCENDING i UNTIL ALIKE i AND 0
              BEHOLD i
            THE TERM EXPIRES.
            """;

        LoopNode node = this.ParseFirstStatement<LoopNode>(statements);

        Assert.Equal(LoopType.Descending, node.Type);
        Assert.Equal("i", node.LoopVariable);
    }

    /// <summary>
    /// Tests that BY A LEGAL FICTION WHILST produces a <see cref="LoopNode"/> with <see cref="LoopType.Whilst"/> and a condition.
    /// </summary>
    [Fact]
    public void Parse_WhilstLoop_SetsLoopTypeAndCondition()
    {
        string statements = """
            BY A LEGAL FICTION WHILST VERITY
              THAT WILL DO.
            THE TERM EXPIRES.
            """;

        LoopNode node = this.ParseFirstStatement<LoopNode>(statements);

        Assert.Equal(LoopType.Whilst, node.Type);
        Assert.NotNull(node.Condition);
    }

    /// <summary>
    /// Tests that KNOWN AS sets the loop label on a <see cref="LoopNode"/>.
    /// </summary>
    [Fact]
    public void Parse_Loop_WithKnownAsLabel_SetsLabel()
    {
        string statements = """
            BY A LEGAL FICTION KNOWN AS mainLoop
              THAT WILL DO.
            THE TERM EXPIRES.
            """;

        LoopNode node = this.ParseFirstStatement<LoopNode>(statements);

        Assert.Equal("mainLoop", node.Label);
    }

    /// <summary>
    /// Tests that THAT WILL DO. at the top level produces a <see cref="BreakNode"/>.
    /// </summary>
    [Fact]
    public void Parse_WithBreak_ProducesBreakNode()
    {
        ProgramNode program = this.parser.Parse("HARK! \"T\" THAT WILL DO. FINALE.");

        Assert.IsType<BreakNode>(program.Statements[0]);
    }

    /// <summary>
    /// Tests that ONCE MORE. at the top level produces a <see cref="ContinueNode"/>.
    /// </summary>
    [Fact]
    public void Parse_WithContinue_ProducesContinueNode()
    {
        ProgramNode program = this.parser.Parse("HARK! \"T\" ONCE MORE. FINALE.");

        Assert.IsType<ContinueNode>(program.Statements[0]);
    }

    /// <summary>
    /// Tests that a function declared UNDER NO OBLIGATION produces a <see cref="FunctionDefinitionNode"/> with an empty parameter list.
    /// </summary>
    [Fact]
    public void Parse_FunctionDefinition_WithNoObligation_SetsEmptyParameters()
    {
        string statements = """
            IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
            MY DUTY IS DISCHARGED.
            """;

        FunctionDefinitionNode node = this.ParseFirstStatement<FunctionDefinitionNode>(statements);

        Assert.Equal("greet", node.Name);
        Assert.Empty(node.Parameters);
    }

    /// <summary>
    /// Tests that a function declared UNDER THE TERMS OF captures all parameter names.
    /// </summary>
    [Fact]
    public void Parse_FunctionDefinition_WithParameters_SetsParameterNames()
    {
        string statements = """
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF salutation AND recipient
            MY DUTY IS DISCHARGED.
            """;

        FunctionDefinitionNode node = this.ParseFirstStatement<FunctionDefinitionNode>(statements);

        Assert.Equal(2, node.Parameters.Count);
        Assert.Contains("salutation", node.Parameters);
        Assert.Contains("recipient", node.Parameters);
    }

    /// <summary>
    /// Tests that AND SO I FIND inside a function body produces a <see cref="ReturnNode"/> with a non-null value.
    /// </summary>
    [Fact]
    public void Parse_WithReturn_ProducesReturnNodeWithValue()
    {
        string statements = """
            IT IS MY DUTY TO PERFORM getValue UNDER NO OBLIGATION
              AND SO I FIND 42
            MY DUTY IS DISCHARGED.
            """;

        FunctionDefinitionNode functionNode = this.ParseFirstStatement<FunctionDefinitionNode>(statements);

        ReturnNode returnNode = Assert.IsType<ReturnNode>(Assert.Single(functionNode.Body));
        Assert.NotNull(returnNode.Value);
    }

    /// <summary>
    /// Tests that MY DUTY IS PREMATURELY DISCHARGED. produces a <see cref="ReturnNode"/> with a null value.
    /// </summary>
    [Fact]
    public void Parse_WithEarlyDischarge_ProducesReturnNodeWithNullValue()
    {
        string statements = """
            IT IS MY DUTY TO PERFORM doWork UNDER NO OBLIGATION
              MY DUTY IS PREMATURELY DISCHARGED.
            MY DUTY IS DISCHARGED.
            """;

        FunctionDefinitionNode functionNode = this.ParseFirstStatement<FunctionDefinitionNode>(statements);

        ReturnNode returnNode = Assert.IsType<ReturnNode>(Assert.Single(functionNode.Body));
        Assert.Null(returnNode.Value);
    }

    /// <summary>
    /// Tests that A HIDEOUS CURSE ON produces a <see cref="ThrowNode"/> with a non-null value expression.
    /// </summary>
    [Fact]
    public void Parse_WithThrow_ProducesThrowNodeWithValue()
    {
        ThrowNode node = this.ParseFirstStatement<ThrowNode>("A HIDEOUS CURSE ON \"disaster\"");

        Assert.NotNull(node.Value);
    }

    /// <summary>
    /// Tests that WITH THE GREATEST RESPECT, produces a <see cref="TryCatchNode"/> with populated success and exception blocks.
    /// </summary>
    [Fact]
    public void Parse_TryCatch_SetsSuccessAndExceptionBlocks()
    {
        string statements = """
            WITH THE GREATEST RESPECT, SUMMON risky WITH NOTHING IF YOU PLEASE.
              WITH GRATITUDE
                BEHOLD "ok"
              MODIFIED RAPTURE
                BEHOLD "err"
            THAT CONCLUDES THE MATTER.
            """;

        TryCatchNode node = this.ParseFirstStatement<TryCatchNode>(statements);

        Assert.NotNull(node.Operation);
        Assert.Single(node.SuccessBlock);
        Assert.Single(node.ExceptionBlock);
    }

    /// <summary>
    /// Tests that a PRINCIPALS block collects all its inner declarations.
    /// </summary>
    [Fact]
    public void Parse_WithPrincipalsBlock_CollectsDeclarations()
    {
        string statements = """
            PRINCIPALS
              PRAY WELCOME alpha AS A PEER BEING 1
              PRAY WELCOME beta AS A YARN BEING "hi"
            THE CURTAIN RISES.
            """;

        PrincipalBlockNode node = this.ParseFirstStatement<PrincipalBlockNode>(statements);

        Assert.Equal(2, node.Declarations.Count);
        Assert.Equal("alpha", node.Declarations[0].Name);
        Assert.Equal("beta", node.Declarations[1].Name);
    }

    /// <summary>
    /// Tests that PRAY ADMIT produces an <see cref="ImportNode"/> with the correct file path.
    /// </summary>
    [Fact]
    public void Parse_WithImport_SetsFilePath()
    {
        ImportNode node = this.ParseFirstStatement<ImportNode>("PRAY ADMIT \"utils.topsy\"");

        Assert.Equal("utils.topsy", node.FilePath);
    }

    /// <summary>
    /// Parses a single statement of a specific type from a source string.
    /// </summary>
    /// <typeparam name="T">The type of statement to parse.</typeparam>
    /// <param name="statementSource">The source string containing the statement.</param>
    /// <remarks>
    /// Wraps the statement in a minimal program structure to allow parsing, and asserts that exactly one statement of the expected type is produced.
    /// </remarks>
    /// <returns>The parsed statement of the specified type.</returns>
    private T ParseFirstStatement<T>(string statementSource) where T : Statement
    {
        ProgramNode program = this.parser.Parse($"HARK! \"T\" {statementSource} FINALE.");
        Statement statement = Assert.Single(program.Statements);
        return Assert.IsType<T>(statement);
    }
}
