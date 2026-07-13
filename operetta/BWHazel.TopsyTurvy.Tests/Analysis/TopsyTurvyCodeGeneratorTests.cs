using System.Linq;
using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Analysis;

/// <summary>
/// Tests for the <see cref="TopsyTurvyCodeGenerator"/> class.
/// </summary>
public class TopsyTurvyCodeGeneratorTests
{
    private readonly TopsyTurvyCodeGenerator generator = new();
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method produces source that re-parses without errors.
    /// </summary>
    [Theory]
    [InlineData("hello", null)]
    [InlineData("hello", "a subtitle")]
    public void Generate_ProgramHeader_ProducesValidSource(string title, string? subtitle)
    {
        ProgramNode program = new()
        {
            Title = title,
            Subtitle = subtitle,
            Statements = [],
            Span = PlaceholderSpan
        };

        string source = this.generator.Generate(program);

        ParseResult result = this.parser.TryParse(source);
        result.Diagnostics.ShouldBeEmpty();
        result.Program.ShouldNotBeNull();
        result.Program!.Title.ShouldBe(title);
        result.Program.Subtitle.ShouldBe(subtitle);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>PRAY WELCOME</c> declaration.
    /// </summary>
    [Fact]
    public void Generate_Declaration_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME x AS A PEER BEING 5\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        result.Program.ShouldNotBeNull();
        DeclarationNode declaration = result.Program!.Statements.OfType<DeclarationNode>().First();
        declaration.Name.ShouldBe("x");
        declaration.Type.ShouldBe(LiteralType.Integer);
        ((LiteralNode)declaration.InitialValue!).Value.ShouldBe(5);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>CONSERVATIVE</c> declaration.
    /// </summary>
    [Fact]
    public void Generate_ConstantDeclaration_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME pi AS A CONSERVATIVE FATHOM BEING 3.14\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        DeclarationNode declaration = result.Program!.Statements.OfType<DeclarationNode>().First();
        declaration.IsConstant.ShouldBeTrue();
        declaration.Type.ShouldBe(LiteralType.Double);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips an <c>IS APPOINTED</c> assignment.
    /// </summary>
    [Fact]
    public void Generate_Assignment_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME x AS A PEER\nx IS APPOINTED 42\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        AssignmentNode assignment = result.Program!.Statements.OfType<AssignmentNode>().First();
        assignment.Target.ShouldBe("x");
        ((LiteralNode)assignment.Value).Value.ShouldBe(42);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>BEHOLD</c> print statement.
    /// </summary>
    [Fact]
    public void Generate_PrintStatement_RoundTrips()
    {
        string source = "HARK! \"T\"\nBEHOLD \"hello\"\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrintNode print = result.Program!.Statements.OfType<PrintNode>().First();
        ((LiteralNode)print.Expression).Value.ShouldBe("hello");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>SHOULD IT TRANSPIRE THAT</c> conditional.
    /// </summary>
    [Fact]
    public void Generate_Conditional_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME x AS A PEER BEING 1
            SHOULD IT TRANSPIRE THAT ALIKE x AND 1
            QUITE SO.
              BEHOLD "yes"
            OTHERWISE,
              BEHOLD "no"
            SO MUCH FOR THAT.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        ConditionalNode cond = result.Program!.Statements.OfType<ConditionalNode>().First();
        cond.TrueBlock.Count.ShouldBe(1);
        cond.ElseBlock.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>BY A LEGAL FICTION WHILST</c> loop.
    /// </summary>
    [Fact]
    public void Generate_WhilstLoop_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME count AS A PEER BEING 0
            BY A LEGAL FICTION WHILST LOWER DEGREE count AND 3
              count IS APPOINTED SUM OF count AND 1
            THE TERM EXPIRES.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        LoopNode loop = result.Program!.Statements.OfType<LoopNode>().First();
        loop.Type.ShouldBe(LoopType.Whilst);
        loop.Body.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a function definition with parameters and return type.
    /// </summary>
    [Fact]
    public void Generate_FunctionDefinition_RoundTrips()
    {
        string source = """
            HARK! "T"
            IT IS MY DUTY TO PERFORM add UNDER THE TERMS OF lhs AS A PEER AND rhs AS A PEER TO FIND PEER
              AND SO I FIND SUM OF lhs AND rhs
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        FunctionDefinitionNode functionDefinition = result.Program!.Statements.OfType<FunctionDefinitionNode>().First();
        functionDefinition.Name.ShouldBe("add");
        functionDefinition.Parameters.Count.ShouldBe(2);
        functionDefinition.ReturnType.ShouldBe(LiteralType.Integer);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a try-catch block.
    /// </summary>
    [Fact]
    public void Generate_TryCatch_RoundTrips()
    {
        string source = """
            HARK! "T"
            IT IS MY DUTY TO PERFORM riskyOp UNDER NO OBLIGATION
              A HIDEOUS CURSE ON "fail"
            MY DUTY IS DISCHARGED.
            WITH THE GREATEST RESPECT, SUMMON riskyOp WITH NOTHING IF YOU PLEASE.
              WITH GRATITUDE
                BEHOLD "ok"
              MODIFIED RAPTURE, err
                BEHOLD err
            THAT CONCLUDES THE MATTER.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        TryCatchNode tryCatch = result.Program!.Statements.OfType<TryCatchNode>().First();
        tryCatch.SuccessBlock.Count.ShouldBe(1);
        tryCatch.ExceptionBlock.Count.ShouldBe(1);
        tryCatch.CaughtValueName.ShouldBe("err");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>WOVEN OF</c> string concatenation.
    /// </summary>
    [Fact]
    public void Generate_WovenOf_RoundTrips()
    {
        string source = "HARK! \"T\"\nBEHOLD WOVEN OF \"a\" AND \"b\" AND \"c\" IF YOU PLEASE.\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrintNode print = result.Program!.Statements.OfType<PrintNode>().First();
        PrefixExpressionNode woven = (PrefixExpressionNode)print.Expression;
        woven.Operator.ShouldBe(Operator.WovenOf);
        woven.Arguments.Count.ShouldBe(3);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips an array declaration with initial values.
    /// </summary>
    [Fact]
    public void Generate_ArrayDeclaration_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME arr AS A LITTLE LIST OF PEER BEING 1 AND 2 AND 3 IF YOU PLEASE.\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        ArrayDeclarationNode arrayDeclaration = result.Program!.Statements.OfType<ArrayDeclarationNode>().First();
        arrayDeclaration.Name.ShouldBe("arr");
        arrayDeclaration.ElementType.ShouldBe(LiteralType.Integer);
        arrayDeclaration.InitialValues.Count.ShouldBe(3);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips declarations for all numeric and primitive type keywords.
    /// </summary>
    /// <param name="typeName">The name of the type to test.</param>
    /// <param name="expectedType">The expected <see cref="LiteralType"/> for the given type name.</param>
    /// <param name="valueExpression">The value expression to use in the declaration.</param>
    [Theory]
    [InlineData("PEER", LiteralType.Integer, "5")]
    [InlineData("CHANCELLOR", LiteralType.Long, "5")]
    [InlineData("PIRATE", LiteralType.Short, "5")]
    [InlineData("SAUSAGE-ROLL", LiteralType.SignedByte, "5")]
    [InlineData("STANDING PEER", LiteralType.UnsignedInteger, "5")]
    [InlineData("STANDING CHANCELLOR", LiteralType.UnsignedLong, "5")]
    [InlineData("STANDING PIRATE", LiteralType.UnsignedShort, "5")]
    [InlineData("STANDING SAUSAGE-ROLL", LiteralType.Byte, "5")]
    [InlineData("FATHOM", LiteralType.Double, "3.14")]
    [InlineData("FOOT", LiteralType.Single, "3.14")]
    [InlineData("YARN", LiteralType.String, "\"hello\"")]
    [InlineData("DECREE", LiteralType.Boolean, "VERITY")]
    public void Generate_Declaration_AllTypes_RoundTrips(string typeName, LiteralType expectedType, string valueExpression)
    {
        string source = $"HARK! \"T\"\nPRAY WELCOME num AS A {typeName} BEING {valueExpression}\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        DeclarationNode declaration = result.Program!.Statements.OfType<DeclarationNode>().First();
        declaration.Type.ShouldBe(expectedType);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>STITCH</c> character literal declaration.
    /// </summary>
    [Fact]
    public void Generate_CharDeclaration_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME ch AS A STITCH BEING 'c'\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        DeclarationNode declaration = result.Program!.Statements.OfType<DeclarationNode>().First();
        declaration.Type.ShouldBe(LiteralType.Char);
        ((LiteralNode)declaration.InitialValue!).Value.ShouldBe('c');
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>NAUGHT</c> null literal assignment.
    /// </summary>
    [Fact]
    public void Generate_NullAssignment_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME msg AS A YARN\nmsg IS APPOINTED NAUGHT\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        AssignmentNode assignment = result.Program!.Statements.OfType<AssignmentNode>().First();
        ((LiteralNode)assignment.Value).Type.ShouldBe(LiteralType.Null);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>VERITY</c> boolean literal.
    /// </summary>
    [Fact]
    public void Generate_BooleanLiteralVerity_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME flag AS A DECREE BEING VERITY\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        DeclarationNode declaration = result.Program!.Statements.OfType<DeclarationNode>().First();
        ((LiteralNode)declaration.InitialValue!).Value.ShouldBe(true);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>NAY</c> boolean literal.
    /// </summary>
    [Fact]
    public void Generate_BooleanLiteralNay_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME flag AS A DECREE BEING NAY\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        DeclarationNode declaration = result.Program!.Statements.OfType<DeclarationNode>().First();
        ((LiteralNode)declaration.InitialValue!).Value.ShouldBe(false);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips arithmetic binary operators.
    /// </summary>
    /// <param name="expression">The expression to test.</param>
    /// <param name="expectedOperator">The expected <see cref="Operator"/> for the given expression.</param>
    [Theory]
    [InlineData("SUM OF lhs AND rhs", Operator.Sum)]
    [InlineData("DIFFERENCE OF lhs AND rhs", Operator.Difference)]
    [InlineData("PRODUCT OF lhs AND rhs", Operator.Product)]
    [InlineData("QUOTIENT OF lhs AND rhs", Operator.Quotient)]
    [InlineData("REMAINDER OF lhs AND rhs", Operator.Remainder)]
    [InlineData("LARGER OF lhs AND rhs", Operator.Larger)]
    [InlineData("SMALLER OF lhs AND rhs", Operator.Smaller)]
    public void Generate_ArithmeticOperator_RoundTrips(string expression, Operator expectedOperator)
    {
        string source = $"""
            HARK! "T"
            PRAY WELCOME lhs AS A PEER BEING 10
            PRAY WELCOME rhs AS A PEER BEING 3
            BEHOLD {expression}
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrintNode print = result.Program!.Statements.OfType<PrintNode>().First();
        PrefixExpressionNode theOperator = (PrefixExpressionNode)print.Expression;
        theOperator.Operator.ShouldBe(expectedOperator);
        theOperator.Arguments.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips comparison binary operators.
    /// </summary>
    /// <param name="expression">The expression to test.</param>
    /// <param name="expectedOperator">The expected <see cref="Operator"/> for the given expression.</param>
    [Theory]
    [InlineData("ALIKE lhs AND rhs", Operator.Alike)]
    [InlineData("UNLIKE lhs AND rhs", Operator.Unlike)]
    [InlineData("PRE-ADAMITE lhs AND rhs", Operator.PreAdamite)]
    [InlineData("LOWER DEGREE lhs AND rhs", Operator.LowerDegree)]
    public void Generate_ComparisonOperator_RoundTrips(string expression, Operator expectedOperator)
    {
        string source = $"""
            HARK! "T"
            PRAY WELCOME lhs AS A PEER BEING 10
            PRAY WELCOME rhs AS A PEER BEING 3
            BEHOLD {expression}
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrintNode print = result.Program!.Statements.OfType<PrintNode>().First();
        PrefixExpressionNode theOperator = (PrefixExpressionNode)print.Expression;
        theOperator.Operator.ShouldBe(expectedOperator);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips the <c>BOTH</c> logical AND operator.
    /// </summary>
    [Fact]
    public void Generate_BothOperator_RoundTrips()
    {
        string source = "HARK! \"T\"\nBEHOLD BOTH VERITY AND VERITY\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrefixExpressionNode theOperator = (PrefixExpressionNode)result.Program!.Statements.OfType<PrintNode>().First().Expression;
        theOperator.Operator.ShouldBe(Operator.Both);
        theOperator.Arguments.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips the <c>HARDLY EVER</c> logical NOT operator.
    /// </summary>
    [Fact]
    public void Generate_HardlyEverOperator_RoundTrips()
    {
        string source = "HARK! \"T\"\nBEHOLD HARDLY EVER NAY\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrefixExpressionNode theOperator = (PrefixExpressionNode)result.Program!.Statements.OfType<PrintNode>().First().Expression;
        theOperator.Operator.ShouldBe(Operator.HardlyEver);
        theOperator.Arguments.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips bitwise binary operators.
    /// </summary>
    /// <param name="expression">The expression to test.</param>
    /// <param name="expectedOperator">The expected <see cref="Operator"/> for the given expression.</param>
    [Theory]
    [InlineData("CHORD OF lhs AND rhs", Operator.ChordOf)]
    [InlineData("HARMONY OF lhs AND rhs", Operator.HarmonyOf)]
    [InlineData("DISCORD OF lhs AND rhs", Operator.DiscordOf)]
    public void Generate_BitwiseBinaryOperator_RoundTrips(string expression, Operator expectedOperator)
    {
        string source = $"""
            HARK! "T"
            PRAY WELCOME lhs AS A PEER BEING 12
            PRAY WELCOME rhs AS A PEER BEING 10
            BEHOLD {expression}
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrintNode print = result.Program!.Statements.OfType<PrintNode>().First();
        PrefixExpressionNode theOperator = (PrefixExpressionNode)print.Expression;
        theOperator.Operator.ShouldBe(expectedOperator);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips the <c>INVERSION OF</c> bitwise NOT operator.
    /// </summary>
    [Fact]
    public void Generate_InversionOf_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME num AS A PEER BEING 5\nBEHOLD INVERSION OF num\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrefixExpressionNode theOperator = (PrefixExpressionNode)result.Program!.Statements.OfType<PrintNode>().First().Expression;
        theOperator.Operator.ShouldBe(Operator.InversionOf);
        theOperator.Arguments.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips <c>TRANSPOSITION UP</c> with an explicit <c>BY</c> clause, preserving the second argument.
    /// </summary>
    [Fact]
    public void Generate_TranspositionUpWithByClause_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME num AS A PEER BEING 5\nBEHOLD TRANSPOSITION UP num BY 3\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrefixExpressionNode theOperator = (PrefixExpressionNode)result.Program!.Statements.OfType<PrintNode>().First().Expression;
        theOperator.Operator.ShouldBe(Operator.TranspositionUp);
        theOperator.Arguments.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method does not emit a spurious <c>BY 1</c>
    /// clause for a <c>TRANSPOSITION UP</c>/<c>TRANSPOSITION DOWN</c> expression whose source never had one,
    /// mirroring how a loop with no <c>Step</c> round-trips without a <c>BY</c> clause.
    /// </summary>
    [Fact]
    public void Generate_TranspositionUpWithoutByClause_DoesNotEmitByOne()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME num AS A PEER BEING 5\nBEHOLD TRANSPOSITION UP num\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrefixExpressionNode theOperator = (PrefixExpressionNode)result.Program!.Statements.OfType<PrintNode>().First().Expression;
        theOperator.Operator.ShouldBe(Operator.TranspositionUp);
        theOperator.Arguments.Count.ShouldBe(1);
        generatedCode.ShouldNotContain(" BY ");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips the variadic <c>ALL OF</c> operator.
    /// </summary>
    [Fact]
    public void Generate_AllOf_RoundTrips()
    {
        string source = "HARK! \"T\"\nBEHOLD ALL OF VERITY AND VERITY AND VERITY IF YOU PLEASE.\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrefixExpressionNode theOperator = (PrefixExpressionNode)result.Program!.Statements.OfType<PrintNode>().First().Expression;
        theOperator.Operator.ShouldBe(Operator.AllOf);
        theOperator.Arguments.Count.ShouldBe(3);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips the variadic <c>ANY OF</c> operator.
    /// </summary>
    [Fact]
    public void Generate_AnyOf_RoundTrips()
    {
        string source = "HARK! \"T\"\nBEHOLD ANY OF NAY AND VERITY AND NAY IF YOU PLEASE.\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrefixExpressionNode theOperator = (PrefixExpressionNode)result.Program!.Statements.OfType<PrintNode>().First().Expression;
        theOperator.Operator.ShouldBe(Operator.AnyOf);
        theOperator.Arguments.Count.ShouldBe(3);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>BEHOLD … WITHOUT CEREMONY</c> print with newline suppressed.
    /// </summary>
    [Fact]
    public void Generate_PrintSuppressNewline_RoundTrips()
    {
        string source = "HARK! \"T\"\nBEHOLD \"hello\" WITHOUT CEREMONY\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrintNode print = result.Program!.Statements.OfType<PrintNode>().First();
        print.SuppressNewline.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>PRAY TELL</c> input statement.
    /// </summary>
    [Fact]
    public void Generate_InputStatement_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME name AS A YARN\nPRAY TELL name\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        InputNode input = result.Program!.Statements.OfType<InputNode>().First();
        input.Target.ShouldBe("name");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>THAT WILL DO.</c> break inside a loop.
    /// </summary>
    [Fact]
    public void Generate_BreakInLoop_RoundTrips()
    {
        string source = """
            HARK! "T"
            BY A LEGAL FICTION WHILST VERITY
              THAT WILL DO.
            THE TERM EXPIRES.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        LoopNode loop = result.Program!.Statements.OfType<LoopNode>().First();
        loop.Body.OfType<BreakNode>().ShouldHaveSingleItem();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>ONCE MORE.</c> continue inside a loop.
    /// </summary>
    [Fact]
    public void Generate_ContinueInLoop_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME count AS A PEER BEING 0
            BY A LEGAL FICTION ASCENDING count UNTIL ALIKE count AND 5
              ONCE MORE.
            THE TERM EXPIRES.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        LoopNode loop = result.Program!.Statements.OfType<LoopNode>().First();
        loop.Body.OfType<ContinueNode>().ShouldHaveSingleItem();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips an <c>A HIDEOUS CURSE ON</c> throw statement.
    /// </summary>
    [Fact]
    public void Generate_ThrowStatement_RoundTrips()
    {
        string source = """
            HARK! "T"
            IT IS MY DUTY TO PERFORM fail UNDER NO OBLIGATION
              A HIDEOUS CURSE ON "something went wrong"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        FunctionDefinitionNode functionDefinition = result.Program!.Statements.OfType<FunctionDefinitionNode>().First();
        ThrowNode thrown = functionDefinition.Body.OfType<ThrowNode>().First();
        ((LiteralNode)thrown.Value).Value.ShouldBe("something went wrong");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>THE LAW IS</c> assert statement.
    /// </summary>
    [Fact]
    public void Generate_AssertStatement_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME score AS A PEER BEING 50
            THE LAW IS PRE-ADAMITE score AND 0 THAT "score must be non-negative"
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        AssertNode assert = result.Program!.Statements.OfType<AssertNode>().First();
        ((LiteralNode)assert.ErrorMessage).Value.ShouldBe("score must be non-negative");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>PRAY ADMIT</c> import statement.
    /// </summary>
    [Fact]
    public void Generate_ImportStatement_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY ADMIT \"utils\"\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        ImportNode import = result.Program!.Statements.OfType<ImportNode>().First();
        import.FilePath.ShouldBe("utils");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips an early return via <c>MY DUTY IS PREMATURELY DISCHARGED.</c>
    /// </summary>
    [Fact]
    public void Generate_EarlyReturn_RoundTrips()
    {
        string source = """
            HARK! "T"
            IT IS MY DUTY TO PERFORM quit UNDER NO OBLIGATION
              MY DUTY IS PREMATURELY DISCHARGED.
              BEHOLD "unreachable"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        FunctionDefinitionNode functionDefinition = result.Program!.Statements.OfType<FunctionDefinitionNode>().First();
        ReturnNode returnNode = functionDefinition.Body.OfType<ReturnNode>().First();
        returnNode.Value.ShouldBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a return with a value via <c>AND SO I FIND</c>.
    /// </summary>
    [Fact]
    public void Generate_ReturnWithValue_RoundTrips()
    {
        string source = """
            HARK! "T"
            IT IS MY DUTY TO PERFORM getValue UNDER NO OBLIGATION TO FIND PEER
              AND SO I FIND 42
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        FunctionDefinitionNode functionDefinition = result.Program!.Statements.OfType<FunctionDefinitionNode>().First();
        ReturnNode returnNode = functionDefinition.Body.OfType<ReturnNode>().First();
        ((LiteralNode)returnNode.Value!).Value.ShouldBe(42);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a conditional with an <c>OR, IF NOT,</c> else-if branch.
    /// </summary>
    [Fact]
    public void Generate_ConditionalWithElseIf_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME num AS A PEER BEING 2
            SHOULD IT TRANSPIRE THAT ALIKE num AND 1
            QUITE SO.
              BEHOLD "one"
              OR, IF NOT, ALIKE num AND 2
                BEHOLD "two"
            OTHERWISE,
              BEHOLD "other"
            SO MUCH FOR THAT.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        ConditionalNode conditional = result.Program!.Statements.OfType<ConditionalNode>().First();
        conditional.ElseIfs.Count.ShouldBe(1);
        conditional.ElseBlock.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips an ascending counted loop.
    /// </summary>
    [Fact]
    public void Generate_AscendingLoop_RoundTrips()
    {
        string source = """
            HARK! "T"
            BY A LEGAL FICTION ASCENDING count UNTIL ALIKE count AND 5
              BEHOLD count
            THE TERM EXPIRES.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        LoopNode loop = result.Program!.Statements.OfType<LoopNode>().First();
        loop.Type.ShouldBe(LoopType.Ascending);
        loop.LoopVariable.ShouldBe("count");
        loop.Body.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a descending counted loop.
    /// </summary>
    [Fact]
    public void Generate_DescendingLoop_RoundTrips()
    {
        string source = """
            HARK! "T"
            BY A LEGAL FICTION DESCENDING count UNTIL ALIKE count AND 0
              BEHOLD count
            THE TERM EXPIRES.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        LoopNode loop = result.Program!.Statements.OfType<LoopNode>().First();
        loop.Type.ShouldBe(LoopType.Descending);
        loop.LoopVariable.ShouldBe("count");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a labelled loop using <c>KNOWN AS</c>.
    /// </summary>
    [Fact]
    public void Generate_LoopWithLabel_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME count AS A PEER BEING 0
            BY A LEGAL FICTION KNOWN AS outer WHILST LOWER DEGREE count AND 3
              count IS APPOINTED SUM OF count AND 1
            THE TERM EXPIRES.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        LoopNode loop = result.Program!.Statements.OfType<LoopNode>().First();
        loop.Label.ShouldBe("outer");
        loop.Type.ShouldBe(LoopType.Whilst);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips an ascending counted loop with a BY step.
    /// </summary>
    [Fact]
    public void Generate_AscendingLoopWithByStep_RoundTrips()
    {
        string source = """
            HARK! "T"
            BY A LEGAL FICTION ASCENDING count BY 3 UNTIL PRE-ADAMITE count AND 21
              BEHOLD count
            THE TERM EXPIRES.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        LoopNode loop = result.Program!.Statements.OfType<LoopNode>().First();
        loop.Type.ShouldBe(LoopType.Ascending);
        loop.Step.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a descending counted loop with a BY step.
    /// </summary>
    [Fact]
    public void Generate_DescendingLoopWithByStep_RoundTrips()
    {
        string source = """
            HARK! "T"
            BY A LEGAL FICTION DESCENDING count BY 2 UNTIL ALIKE count AND 0
              BEHOLD count
            THE TERM EXPIRES.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        LoopNode loop = result.Program!.Statements.OfType<LoopNode>().First();
        loop.Type.ShouldBe(LoopType.Descending);
        loop.Step.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips an <c>IN WHICH CAPACITY?</c> switch statement.
    /// </summary>
    [Fact]
    public void Generate_SwitchStatement_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME num AS A PEER BEING 1
            IN WHICH CAPACITY? num
              WHEN ACTING AS 1
                BEHOLD "one"
                THAT WILL DO.
              WHEN ACTING AS 2
                BEHOLD "two"
                THAT WILL DO.
            NOTHING COULD BE MORE SATISFACTORY.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        SwitchNode switchBlock = result.Program!.Statements.OfType<SwitchNode>().First();
        switchBlock.Cases.Count.ShouldBe(2);
        switchBlock.DefaultBlock.Count.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a switch with a <c>FAILING ALL OF THE ABOVE,</c> default.
    /// </summary>
    [Fact]
    public void Generate_SwitchWithDefault_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME num AS A PEER BEING 3
            IN WHICH CAPACITY? num
              WHEN ACTING AS 1
                BEHOLD "one"
                THAT WILL DO.
              FAILING ALL OF THE ABOVE,
                BEHOLD "other"
            NOTHING COULD BE MORE SATISFACTORY.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        SwitchNode switchBlock = result.Program!.Statements.OfType<SwitchNode>().First();
        switchBlock.Cases.Count.ShouldBe(1);
        switchBlock.DefaultBlock.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>YEOMAN</c> guard clause.
    /// </summary>
    [Fact]
    public void Generate_GuardStatement_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME score AS A PEER BEING 50
            YEOMAN PRE-ADAMITE score AND 0
            OTHERWISE,
              A HIDEOUS CURSE ON "negative score"
            UNDER ORDERS.
            BEHOLD score
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        GuardNode guard = result.Program!.Statements.OfType<GuardNode>().First();
        guard.ElseBlock.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips an array declaration with a fixed size.
    /// </summary>
    [Fact]
    public void Generate_ArrayDeclarationWithSize_RoundTrips()
    {
        string source = "HARK! \"T\"\nPRAY WELCOME arr AS A LITTLE LIST OF 5 PEER\nFINALE.";

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        ArrayDeclarationNode arrayDeclaration = result.Program!.Statements.OfType<ArrayDeclarationNode>().First();
        arrayDeclaration.Name.ShouldBe("arr");
        arrayDeclaration.ElementType.ShouldBe(LiteralType.Integer);
        arrayDeclaration.InitialValues.Count.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>VICTIM … ON … IS APPOINTED</c> array element assignment.
    /// </summary>
    [Fact]
    public void Generate_ArrayElementAssignment_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME arr AS A LITTLE LIST OF PEER BEING 1 AND 2 AND 3 IF YOU PLEASE.
            VICTIM 2 ON arr IS APPOINTED 99
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        ArrayElementAssignmentNode arrayElementAssignment = result.Program!.Statements.OfType<ArrayElementAssignmentNode>().First();
        arrayElementAssignment.ArrayName.ShouldBe("arr");
        ((LiteralNode)arrayElementAssignment.Value).Value.ShouldBe(99);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>VICTIM … ON</c> array index expression.
    /// </summary>
    [Fact]
    public void Generate_ArrayIndexExpression_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME arr AS A LITTLE LIST OF PEER BEING 10 AND 20 AND 30 IF YOU PLEASE.
            BEHOLD VICTIM 2 ON arr
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrintNode print = result.Program!.Statements.OfType<PrintNode>().First();
        ArrayIndexNode arrayIndex = (ArrayIndexNode)print.Expression;
        arrayIndex.ArrayName.ShouldBe("arr");
        ((LiteralNode)arrayIndex.Index).Value.ShouldBe(2);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>RECKONING OF</c> array length expression.
    /// </summary>
    [Fact]
    public void Generate_ArrayLength_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME arr AS A LITTLE LIST OF PEER BEING 1 AND 2 AND 3 IF YOU PLEASE.
            BEHOLD RECKONING OF arr
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrintNode print = result.Program!.Statements.OfType<PrintNode>().First();
        ArrayLengthNode arrayLength = (ArrayLengthNode)print.Expression;
        arrayLength.ArrayName.ShouldBe("arr");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips an <c>AS IT WERE … AS A</c> type cast expression.
    /// </summary>
    [Fact]
    public void Generate_TypeCast_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME num AS A PEER BEING 20
            AS IT WERE num AS A FATHOM
            BEHOLD JUST SO
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        ExpressionStatement castStatement = result.Program!.Statements.OfType<ExpressionStatement>().First();
        ExpressionCastNode cast = (ExpressionCastNode)castStatement.Expression;
        cast.NewType.ShouldBe(LiteralType.Double);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>SHOULD IT TRANSPIRE THAT … OTHERWISE,</c> ternary expression.
    /// </summary>
    [Fact]
    public void Generate_TernaryExpression_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRAY WELCOME flag AS A DECREE BEING VERITY
            BEHOLD "yes" SHOULD IT TRANSPIRE THAT flag OTHERWISE, "no"
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrintNode print = result.Program!.Statements.OfType<PrintNode>().First();
        TernaryExpressionNode ternary = (TernaryExpressionNode)print.Expression;
        ((LiteralNode)ternary.TrueValue).Value.ShouldBe("yes");
        ((LiteralNode)ternary.FalseValue).Value.ShouldBe("no");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>SUMMON</c> function call as a standalone expression statement.
    /// </summary>
    [Fact]
    public void Generate_ExpressionStatement_RoundTrips()
    {
        string source = """
            HARK! "T"
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF msg AS A YARN
              BEHOLD msg
            MY DUTY IS DISCHARGED.
            SUMMON greet WITH "hello" IF YOU PLEASE.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        ExpressionStatement statement = result.Program!.Statements.OfType<ExpressionStatement>().First();
        PrefixExpressionNode functionCall = (PrefixExpressionNode)statement.Expression;
        functionCall.Operator.ShouldBe(Operator.Summon);
        functionCall.Arguments.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a <c>PRINCIPALS</c> block containing global declarations.
    /// </summary>
    [Fact]
    public void Generate_PrincipalBlock_RoundTrips()
    {
        string source = """
            HARK! "T"
            PRINCIPALS
              PRAY WELCOME pi AS A CONSERVATIVE FATHOM BEING 3.14
            THE CURTAIN RISES.
            BEHOLD pi
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        PrincipalBlockNode principalBlock = result.Program!.Statements.OfType<PrincipalBlockNode>().First();
        principalBlock.Declarations.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a function with no return type.
    /// </summary>
    [Fact]
    public void Generate_FunctionWithNoReturnType_RoundTrips()
    {
        string source = """
            HARK! "T"
            IT IS MY DUTY TO PERFORM announce UNDER THE TERMS OF msg AS A YARN
              BEHOLD msg
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        FunctionDefinitionNode functionDefinition = result.Program!.Statements.OfType<FunctionDefinitionNode>().First();
        functionDefinition.Name.ShouldBe("announce");
        functionDefinition.ReturnType.ShouldBeNull();
        functionDefinition.Parameters.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a top-level AND SO I FIND programme return.
    /// </summary>
    [Fact]
    public void Generate_TopLevelProgrammeReturn_RoundTrips()
    {
        string source = """
            HARK! "T"
            AND SO I FIND 42
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        ProgrammeReturnNode programmeReturn = result.Program!.Statements.OfType<ProgrammeReturnNode>().First();
        ((LiteralNode)programmeReturn.Value).Value.ShouldBe(42);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a TOWN declaration and normalises a long-form WITH DISTRICT chain to the short-form <c>*</c>-joined syntax.
    /// </summary>
    [Fact]
    public void Generate_NamespaceDeclarationLongForm_RoundTripsAsShortForm()
    {
        string source = """
            HARK! "T"
            TOWN Accounts WITH DISTRICT Payroll
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        NamespaceDeclarationNode node = result.Program!.Statements.OfType<NamespaceDeclarationNode>().First();
        node.Path.ShouldBe(["Accounts", "Payroll"]);
        generatedCode.ShouldContain("TOWN Accounts*Payroll");
        generatedCode.ShouldNotContain("WITH DISTRICT");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a PRAY RECOGNISE directive and normalises a long-form WITH DISTRICT chain to the short-form <c>*</c>-joined syntax.
    /// </summary>
    [Fact]
    public void Generate_RecogniseStatementLongForm_RoundTripsAsShortForm()
    {
        string source = """
            HARK! "T"
            PRAY RECOGNISE Accounts WITH DISTRICT Payroll
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        RecogniseNode node = result.Program!.Statements.OfType<RecogniseNode>().First();
        node.Path.ShouldBe(["Accounts", "Payroll"]);
        generatedCode.ShouldContain("PRAY RECOGNISE Accounts*Payroll");
        generatedCode.ShouldNotContain("WITH DISTRICT");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a long-form fully-qualified SUMMON call, re-emitting the target with the short-form <c>*</c>-joined syntax rather than the literal dot-joined internal name.
    /// </summary>
    [Fact]
    public void Generate_FullyQualifiedSummonLongForm_RoundTripsAsShortForm()
    {
        string source = """
            HARK! "T"
            TOWN Accounts WITH DISTRICT Payroll
            IT IS MY DUTY TO PERFORM CalculateTax UNDER THE TERMS OF Amount AS A PEER TO FIND PEER
              AND SO I FIND Amount
            MY DUTY IS DISCHARGED.
            BEHOLD SUMMON Accounts WITH DISTRICT Payroll WITH DUTY CalculateTax WITH 100 IF YOU PLEASE.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        generatedCode.ShouldContain("SUMMON Accounts*Payroll*CalculateTax WITH 100 IF YOU PLEASE.");
        generatedCode.ShouldNotContain("WITH DUTY");
        generatedCode.ShouldNotContain("Accounts.Payroll");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method round-trips a short-form fully-qualified SUMMON call unchanged.
    /// </summary>
    [Fact]
    public void Generate_FullyQualifiedSummonShortForm_RoundTrips()
    {
        string source = """
            HARK! "T"
            TOWN Accounts*Payroll
            IT IS MY DUTY TO PERFORM CalculateTax UNDER THE TERMS OF Amount AS A PEER TO FIND PEER
              AND SO I FIND Amount
            MY DUTY IS DISCHARGED.
            BEHOLD SUMMON Accounts*Payroll*CalculateTax WITH 100 IF YOU PLEASE.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        generatedCode.ShouldContain("SUMMON Accounts*Payroll*CalculateTax WITH 100 IF YOU PLEASE.");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyCodeGenerator.Generate"/> method leaves a plain, unqualified SUMMON call unaffected by the fully-qualified target handling.
    /// </summary>
    [Fact]
    public void Generate_PlainSummon_RoundTripsWithNoStarCharacter()
    {
        string source = """
            HARK! "T"
            IT IS MY DUTY TO PERFORM Greet UNDER THE TERMS OF Name AS A YARN
              BEHOLD Name
            MY DUTY IS DISCHARGED.
            SUMMON Greet WITH "Ko-Ko" IF YOU PLEASE.
            FINALE.
            """;

        string generatedCode = this.GenerateFromSource(source);

        ParseResult result = this.parser.TryParse(generatedCode);
        result.Diagnostics.ShouldBeEmpty();
        generatedCode.ShouldNotContain("*");
    }

    /// <summary>
    /// Generates source from a raw Topsy Turvy programme string by parsing it and re-generating.
    /// </summary>
    /// <param name="source">The input Topsy Turvy source.</param>
    /// <returns>The re-generated source string.</returns>
    private string GenerateFromSource(string source)
    {
        ParseResult result = this.parser.TryParse(source);
        result.Program.ShouldNotBeNull($"Source failed to parse: {source}");
        return this.generator.Generate(result.Program!);
    }

    /// <summary>
    /// Gets a placeholder source span used when constructing AST nodes directly in tests.
    /// </summary>
    private static SourceSpan PlaceholderSpan => new(
        new(0, 0),
        new(0, 0));
}
