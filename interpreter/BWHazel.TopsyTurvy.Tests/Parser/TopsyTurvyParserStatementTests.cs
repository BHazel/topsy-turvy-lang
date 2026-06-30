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

        node.Name.ShouldBe("x");
        node.InitialValue.ShouldBeNull();
    }

    /// <summary>
    /// Tests that a PRAY WELCOME declaration with a BEING clause populates the initial value.
    /// </summary>
    [Fact]
    public void Parse_Declaration_WithInitialValue_SetsInitialValue()
    {
        DeclarationNode node = this.ParseFirstStatement<DeclarationNode>("PRAY WELCOME x AS A PEER BEING 42");

        node.Name.ShouldBe("x");
        node.InitialValue.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that IS APPOINTED produces an <see cref="AssignmentNode"/> with the correct target name.
    /// </summary>
    [Fact]
    public void Parse_WithAssignment_SetsTargetName()
    {
        AssignmentNode node = this.ParseFirstStatement<AssignmentNode>("Ko-Ko IS APPOINTED 99");

        node.Target.ShouldBe("Ko-Ko");
    }

    /// <summary>
    /// Tests that IS APPOINTED produces an <see cref="AssignmentNode"/> with a non-null value expression.
    /// </summary>
    [Fact]
    public void Parse_WithAssignment_SetsValueExpression()
    {
        AssignmentNode node = this.ParseFirstStatement<AssignmentNode>("x IS APPOINTED 99");

        node.Value.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that AS IT WERE produces an <see cref="ExpressionCastNode"/> with the correct new type when used as a standalone statement.
    /// </summary>
    [Fact]
    public void Parse_WithExpressionCast_SetsExpressionAndNewType()
    {
        ExpressionCastNode node = this.ParseFirstExpressionStatement<ExpressionCastNode>("AS IT WERE x AS A YARN");

        node.Expression.ShouldNotBeNull();
        node.NewType.ShouldBe(LiteralType.String);
    }

    /// <summary>
    /// Tests that BEHOLD produces a <see cref="PrintNode"/> with <see cref="PrintNode.SuppressNewline"/> set to <c>false</c> by default.
    /// </summary>
    [Fact]
    public void Parse_Print_WithoutWithoutCeremony_SuppressNewlineIsFalse()
    {
        PrintNode node = this.ParseFirstStatement<PrintNode>("BEHOLD \"hello\"");

        node.Expression.ShouldNotBeNull();
        node.SuppressNewline.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that BEHOLD … WITHOUT CEREMONY produces a <see cref="PrintNode"/> with <see cref="PrintNode.SuppressNewline"/> set to <c>true</c>.
    /// </summary>
    [Fact]
    public void Parse_Print_WithWithoutCeremony_SuppressNewlineIsTrue()
    {
        PrintNode node = this.ParseFirstStatement<PrintNode>("BEHOLD \"hello\" WITHOUT CEREMONY");
        node.SuppressNewline.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that PRAY TELL produces an <see cref="InputNode"/> with the correct target variable name.
    /// </summary>
    [Fact]
    public void Parse_Input_SetsTargetName()
    {
        InputNode node = this.ParseFirstStatement<InputNode>("PRAY TELL answer");
        node.Target.ShouldBe("answer");
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

        node.Condition.ShouldNotBeNull();
        node.TrueBlock.ShouldHaveSingleItem();
        node.ElseIfs.ShouldBeEmpty();
        node.ElseBlock.ShouldBeEmpty();
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

        node.TrueBlock.ShouldHaveSingleItem();
        node.ElseBlock.ShouldHaveSingleItem();
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
              OR, IF NOT, NAY
                BEHOLD "maybe"
            SO MUCH FOR THAT.
            """;

        ConditionalNode node = this.ParseFirstStatement<ConditionalNode>(statements);

        node.ElseIfs.ShouldHaveSingleItem();
        node.ElseIfs[0].Block.ShouldHaveSingleItem();
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

        node.Cases.Count.ShouldBe(2);
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

        node.DefaultBlock.ShouldHaveSingleItem();
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

        node.Type.ShouldBe(LoopType.Infinite);
        node.Label.ShouldBeNull();
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

        node.Type.ShouldBe(LoopType.Ascending);
        node.LoopVariable.ShouldBe("i");
        node.Condition.ShouldNotBeNull();
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

        node.Type.ShouldBe(LoopType.Descending);
        node.LoopVariable.ShouldBe("i");
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

        node.Type.ShouldBe(LoopType.Whilst);
        node.Condition.ShouldNotBeNull();
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

        node.Label.ShouldBe("mainLoop");
    }

    /// <summary>
    /// Tests that BY A LEGAL FICTION ASCENDING with a BY clause sets the step expression on a <see cref="LoopNode"/>.
    /// </summary>
    [Fact]
    public void Parse_AscendingLoop_WithByStep_SetsStep()
    {
        string statements = """
            BY A LEGAL FICTION ASCENDING i BY 3 UNTIL ALIKE i AND 21
              BEHOLD i
            THE TERM EXPIRES.
            """;

        LoopNode node = this.ParseFirstStatement<LoopNode>(statements);

        node.Type.ShouldBe(LoopType.Ascending);
        node.Step.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that BY A LEGAL FICTION DESCENDING with a BY clause sets the step expression on a <see cref="LoopNode"/>.
    /// </summary>
    [Fact]
    public void Parse_DescendingLoop_WithByStep_SetsStep()
    {
        string statements = """
            BY A LEGAL FICTION DESCENDING i BY 2 UNTIL ALIKE i AND 0
              BEHOLD i
            THE TERM EXPIRES.
            """;

        LoopNode node = this.ParseFirstStatement<LoopNode>(statements);

        node.Type.ShouldBe(LoopType.Descending);
        node.Step.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that BY A LEGAL FICTION ASCENDING without a BY clause leaves the step expression null on a <see cref="LoopNode"/>.
    /// </summary>
    [Fact]
    public void Parse_AscendingLoop_WithoutByStep_StepIsNull()
    {
        string statements = """
            BY A LEGAL FICTION ASCENDING i UNTIL ALIKE i AND 5
              BEHOLD i
            THE TERM EXPIRES.
            """;

        LoopNode node = this.ParseFirstStatement<LoopNode>(statements);

        node.Step.ShouldBeNull();
    }

    /// <summary>
    /// Tests that THAT WILL DO. at the top level produces a <see cref="BreakNode"/>.
    /// </summary>
    [Fact]
    public void Parse_WithBreak_ProducesBreakNode()
    {
        ProgramNode program = this.parser.Parse("HARK! \"T\" THAT WILL DO. FINALE.");

        program.Statements[0].ShouldBeOfType<BreakNode>();
    }

    /// <summary>
    /// Tests that ONCE MORE. at the top level produces a <see cref="ContinueNode"/>.
    /// </summary>
    [Fact]
    public void Parse_WithContinue_ProducesContinueNode()
    {
        ProgramNode program = this.parser.Parse("HARK! \"T\" ONCE MORE. FINALE.");

        program.Statements[0].ShouldBeOfType<ContinueNode>();
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

        node.Name.ShouldBe("greet");
        node.Parameters.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a function declared UNDER THE TERMS OF captures all typed parameter names and types.
    /// </summary>
    [Fact]
    public void Parse_FunctionDefinition_WithParameters_SetsParameterNames()
    {
        string statements = """
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF salutation AS A YARN AND recipient AS A YARN
            MY DUTY IS DISCHARGED.
            """;

        FunctionDefinitionNode node = this.ParseFirstStatement<FunctionDefinitionNode>(statements);

        node.Parameters.Count.ShouldBe(2);
        node.Parameters.ShouldContain(parameter => parameter.Name == "salutation");
        node.Parameters.ShouldContain(parameter => parameter.Name == "recipient");
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

        ReturnNode returnNode = functionNode.Body.ShouldHaveSingleItem().ShouldBeOfType<ReturnNode>();
        returnNode.Value.ShouldNotBeNull();
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

        ReturnNode returnNode = functionNode.Body.ShouldHaveSingleItem().ShouldBeOfType<ReturnNode>();
        returnNode.Value.ShouldBeNull();
    }

    /// <summary>
    /// Tests that A HIDEOUS CURSE ON produces a <see cref="ThrowNode"/> with a non-null value expression.
    /// </summary>
    [Fact]
    public void Parse_WithThrow_ProducesThrowNodeWithValue()
    {
        ThrowNode node = this.ParseFirstStatement<ThrowNode>("A HIDEOUS CURSE ON \"disaster\"");

        node.Value.ShouldNotBeNull();
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
              MODIFIED RAPTURE, Err
                BEHOLD "err"
            THAT CONCLUDES THE MATTER.
            """;

        TryCatchNode node = this.ParseFirstStatement<TryCatchNode>(statements);

        node.Operation.ShouldNotBeNull();
        node.SuccessBlock.ShouldHaveSingleItem();
        node.ExceptionBlock.ShouldHaveSingleItem();
    }

    /// <summary>
    /// Tests that the <see cref="StatementParser.TryCatch"/> parser sets <see cref="TryCatchNode.CaughtValueName"/> when an identifier follows MODIFIED RAPTURE.
    /// </summary>
    [Fact]
    public void Parse_TryCatch_WithCaughtBinding_SetsCaughtValueName()
    {
        string statements = """
            WITH THE GREATEST RESPECT, SUMMON risky WITH NOTHING IF YOU PLEASE.
              WITH GRATITUDE
                BEHOLD "ok"
              MODIFIED RAPTURE, Grievance
                BEHOLD Grievance
            THAT CONCLUDES THE MATTER.
            """;

        TryCatchNode node = this.ParseFirstStatement<TryCatchNode>(statements);

        node.CaughtValueName.ShouldBe("Grievance");
    }

    /// <summary>
    /// Tests that the <see cref="StatementParser.TryCatch"/> parser correctly parses both the caught binding and the exception block body when both are present.
    /// </summary>
    [Fact]
    public void Parse_TryCatch_CaughtBinding_WithBodyStatements_ParsesCorrectly()
    {
        string statements = """
            WITH THE GREATEST RESPECT, SUMMON risky WITH NOTHING IF YOU PLEASE.
              WITH GRATITUDE
                BEHOLD "ok"
              MODIFIED RAPTURE, Disaster
                BEHOLD Disaster
                BEHOLD "done"
            THAT CONCLUDES THE MATTER.
            """;

        TryCatchNode node = this.ParseFirstStatement<TryCatchNode>(statements);

        node.CaughtValueName.ShouldBe("Disaster");
        node.ExceptionBlock.Count.ShouldBe(2);
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

        node.Declarations.Count.ShouldBe(2);
        ((DeclarationNode)node.Declarations[0]).Name.ShouldBe("alpha");
        ((DeclarationNode)node.Declarations[1]).Name.ShouldBe("beta");
    }

    /// <summary>
    /// Tests that PRAY ADMIT produces an <see cref="ImportNode"/> with the correct file path.
    /// </summary>
    [Fact]
    public void Parse_WithImport_SetsFilePath()
    {
        ImportNode node = this.ParseFirstStatement<ImportNode>("PRAY ADMIT \"utils.topsy\"");

        node.FilePath.ShouldBe("utils.topsy");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyParser.Parse"/> method sets <see cref="DeclarationNode.IsConstant"/> to <c>true</c> when the CONSERVATIVE modifier is present.
    /// </summary>
    [Fact]
    public void Parse_Declaration_WithConservativeModifier_SetsIsConstantTrue()
    {
        DeclarationNode node = this.ParseFirstStatement<DeclarationNode>("PRAY WELCOME x AS A CONSERVATIVE PEER");

        node.IsConstant.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyParser.Parse"/> method sets <see cref="DeclarationNode.IsConstant"/> to <c>false</c> when the LIBERAL modifier is present.
    /// </summary>
    [Fact]
    public void Parse_Declaration_WithLiberalModifier_SetsIsConstantFalse()
    {
        DeclarationNode node = this.ParseFirstStatement<DeclarationNode>("PRAY WELCOME x AS A LIBERAL PEER");

        node.IsConstant.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyParser.Parse"/> method sets <see cref="DeclarationNode.IsConstant"/> to <c>false</c> when no mutability modifier is present.
    /// </summary>
    [Fact]
    public void Parse_Declaration_WithoutModifier_DefaultsIsConstantFalse()
    {
        DeclarationNode node = this.ParseFirstStatement<DeclarationNode>("PRAY WELCOME x AS A PEER");

        node.IsConstant.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyParser.Parse"/> method correctly parses a CONSERVATIVE declaration that also includes a BEING initial value.
    /// </summary>
    [Fact]
    public void Parse_Declaration_WithConservativeModifierAndInitialValue_ParsesCorrectly()
    {
        DeclarationNode node = this.ParseFirstStatement<DeclarationNode>("PRAY WELCOME x AS A CONSERVATIVE PEER BEING 42");

        node.IsConstant.ShouldBeTrue();
        node.InitialValue.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that <c>IS APPOINTED AS IT WERE</c> parses the assignment value as an <see cref="ExpressionCastNode"/>.
    /// </summary>
    [Fact]
    public void Parse_ExpressionCast_InAssignment_SetsValueNode()
    {
        AssignmentNode assignment = this.ParseFirstStatement<AssignmentNode>("x IS APPOINTED AS IT WERE y AS A YARN");

        ExpressionCastNode cast = assignment.Value.ShouldBeOfType<ExpressionCastNode>();
        cast.NewType.ShouldBe(LiteralType.String);
    }

    /// <summary>
    /// Tests that <c>PRAY WELCOME ... BEING AS IT WERE</c> parses the initial value as an <see cref="ExpressionCastNode"/>.
    /// </summary>
    [Fact]
    public void Parse_ExpressionCast_InDeclarationBeing_SetsInitialValue()
    {
        DeclarationNode declaration = this.ParseFirstStatement<DeclarationNode>("PRAY WELCOME x AS A YARN BEING AS IT WERE y AS A YARN");

        ExpressionCastNode cast = declaration.InitialValue.ShouldBeOfType<ExpressionCastNode>();
        cast.NewType.ShouldBe(LiteralType.String);
    }

    /// <summary>
    /// Tests that a <see cref="DeclarationNode"/> produced by parsing a <c>PRAY WELCOME</c> statement carries a real source span
    /// pointing to the start of that statement in the original source.
    /// </summary>
    [Fact]
    public void Parse_DeclarationNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nPRAY WELCOME x AS A PEER BEING 42\nFINALE.");

        DeclarationNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<DeclarationNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <see cref="FunctionDefinitionNode"/> produced by parsing an <c>IT IS MY DUTY TO PERFORM</c> block carries a
    /// real source span starting at the first character of that block.
    /// </summary>
    [Fact]
    public void Parse_FunctionDefinitionNode_SpanStartsAtCorrectLineAndColumn()
    {
        string source = "HARK! \"Test\"\nIT IS MY DUTY TO PERFORM Add UNDER NO OBLIGATION\nMY DUTY IS DISCHARGED.\nFINALE.";

        ProgramNode program = this.parser.Parse(source);
        
        FunctionDefinitionNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<FunctionDefinitionNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that each <see cref="TypedParameter.Span"/> carries a real source span at the correct position on the declaration line.
    /// </summary>
    [Fact]
    public void Parse_FunctionDefinitionNode_ParameterSpansAreOnCorrectLineWithDistinctColumns()
    {
        string source = "HARK! \"Test\"\nIT IS MY DUTY TO PERFORM Add UNDER THE TERMS OF alpha AS A PEER AND beta AS A PEER\nMY DUTY IS DISCHARGED.\nFINALE.";

        ProgramNode program = this.parser.Parse(source);

        FunctionDefinitionNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<FunctionDefinitionNode>();
        node.Parameters.Count.ShouldBe(2);
        node.Parameters[0].Span.Start.Line.ShouldBe(2);
        (node.Parameters[0].Span.Start.Column > 0).ShouldBeTrue();
        node.Parameters[1].Span.Start.Line.ShouldBe(2);
        (node.Parameters[1].Span.Start.Column > node.Parameters[0].Span.Start.Column).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that an <see cref="AssignmentNode"/> carries a real source span starting at the target variable.
    /// </summary>
    [Fact]
    public void Parse_AssignmentNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nx IS APPOINTED 42\nFINALE.");

        AssignmentNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<AssignmentNode>();

        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <see cref="PrintNode"/> carries a real source span starting at the BEHOLD keyword.
    /// </summary>
    [Fact]
    public void Parse_PrintNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD 42\nFINALE.");

        PrintNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();

        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <see cref="ReturnNode"/> inside a function body carries a span pointing to the return statement line.
    /// </summary>
    [Fact]
    public void Parse_ReturnNode_SpanStartsAtCorrectLineInsideFunctionBody()
    {
        ProgramNode program = this.parser.Parse("""
            HARK! "Test"
            IT IS MY DUTY TO PERFORM getValue UNDER NO OBLIGATION
            AND SO I FIND 42
            MY DUTY IS DISCHARGED.
            FINALE.
            """);

        FunctionDefinitionNode functionDefinition = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<FunctionDefinitionNode>();
        ReturnNode node = functionDefinition.Body.ShouldHaveSingleItem().ShouldBeOfType<ReturnNode>();
        node.Span.Start.Line.ShouldBe(3);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <see cref="ConditionalNode"/> carries a real source span.
    /// </summary>
    [Fact]
    public void Parse_ConditionalNode_SpanStartsAtCorrectLineAndColumnAndEndsOnLaterLine()
    {
        ProgramNode program = this.parser.Parse("""
            HARK! "Test"
            SHOULD IT TRANSPIRE THAT VERITY
            QUITE SO.
              BEHOLD "yes"
            SO MUCH FOR THAT.
            FINALE.
            """);

        ConditionalNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<ConditionalNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
        (node.Span.End.Line > node.Span.Start.Line).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a <see cref="LoopNode"/> carries a real source span.
    /// </summary>
    [Fact]
    public void Parse_LoopNode_SpanStartsAtCorrectLineAndColumnAndEndsOnLaterLine()
    {
        ProgramNode program = this.parser.Parse("""
            HARK! "Test"
            BY A LEGAL FICTION
              THAT WILL DO.
            THE TERM EXPIRES.
            FINALE.
            """);

        LoopNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<LoopNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
        (node.Span.End.Line > node.Span.Start.Line).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that an <see cref="ArrayDeclarationNode"/> carries a real source span.
    /// </summary>
    [Fact]
    public void Parse_ArrayDeclarationNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nPRAY WELCOME arr AS A LITTLE LIST OF PEER\nFINALE.");

        ArrayDeclarationNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<ArrayDeclarationNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that an <see cref="ImportNode"/> carries a real source span starting at the PRAY ADMIT keyword.
    /// </summary>
    [Fact]
    public void Parse_ImportNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nPRAY ADMIT \"utils.topsy\"\nFINALE.");

        ImportNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<ImportNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <see cref="GuardNode"/> carries a real source span starting at the YEOMAN keyword.
    /// </summary>
    [Fact]
    public void Parse_GuardNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nYEOMAN VERITY OTHERWISE, UNDER ORDERS.\nFINALE.");

        GuardNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<GuardNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that an <see cref="AssertNode"/> carries a real source span starting at the THE LAW IS keyword.
    /// </summary>
    [Fact]
    public void Parse_AssertNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nTHE LAW IS VERITY THAT \"ok\"\nFINALE.");

        AssertNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<AssertNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <see cref="TryCatchNode"/> carries a real source span.
    /// </summary>
    [Fact]
    public void Parse_TryCatchNode_SpanStartsAtCorrectLineAndColumnAndEndsOnLaterLine()
    {
        ProgramNode program = this.parser.Parse("""
            HARK! "Test"
            WITH THE GREATEST RESPECT, SUMMON doStuff WITH NOTHING IF YOU PLEASE.
              WITH GRATITUDE
                BEHOLD "ok"
              MODIFIED RAPTURE, Err
                BEHOLD "err"
            THAT CONCLUDES THE MATTER.
            FINALE.
            """);

        TryCatchNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<TryCatchNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
        (node.Span.End.Line > node.Span.Start.Line).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a <see cref="BreakNode"/> carries a real source span starting at the THAT WILL DO. keyword.
    /// </summary>
    [Fact]
    public void Parse_BreakNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nTHAT WILL DO.\nFINALE.");

        BreakNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<BreakNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <see cref="ContinueNode"/> carries a real source span starting at the ONCE MORE. keyword.
    /// </summary>
    [Fact]
    public void Parse_ContinueNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nONCE MORE.\nFINALE.");

        ContinueNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<ContinueNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a <see cref="ThrowNode"/> carries a real source span starting at the A HIDEOUS CURSE ON keyword.
    /// </summary>
    [Fact]
    public void Parse_ThrowNode_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nA HIDEOUS CURSE ON \"disaster\"\nFINALE.");

        ThrowNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<ThrowNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that an <see cref="AssignmentNode"/> span has a non-zero width where <c>End</c> is strictly after <c>Start</c>.
    /// </summary>
    [Fact]
    public void Parse_AssignmentNode_SpanEndIsAfterStart()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nx IS APPOINTED 42\nFINALE.");

        AssignmentNode node = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<AssignmentNode>();
        node.Span.End.Line.ShouldBe(2);
        node.Span.End.Column.ShouldBe(18);
    }

    /// <summary>
    /// Tests that the <see cref="ProgramNode"/> itself carries a span starting at line 1, column 1, and ending no earlier than the final line.
    /// </summary>
    [Fact]
    public void Parse_ProgramNode_SpanCoversEntireSource()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD 42\nFINALE.");

        program.Span.Start.Line.ShouldBe(1);
        program.Span.Start.Column.ShouldBe(1);
        (program.Span.End.Line >= 3).ShouldBeTrue();
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
        Statement statement = program.Statements.ShouldHaveSingleItem();
        return statement.ShouldBeOfType<T>();
    }

    /// <summary>
    /// Parses a single standalone expression statement and returns the inner expression cast as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected <see cref="Expression"/> type wrapped by the <see cref="ExpressionStatement"/>.</typeparam>
    /// <param name="statementSource">The source text for the expression statement.</param>
    private T ParseFirstExpressionStatement<T>(string statementSource) where T : Expression
    {
        ProgramNode program = this.parser.Parse($"HARK! \"T\" {statementSource} FINALE.");
        Statement statement = program.Statements.ShouldHaveSingleItem();
        ExpressionStatement expressionStatement = statement.ShouldBeOfType<ExpressionStatement>();
        return expressionStatement.Expression.ShouldBeOfType<T>();
    }
}
