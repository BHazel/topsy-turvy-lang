using System;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.TypeChecker;

namespace BWHazel.TopsyTurvy.Tests.TypeChecker;

/// <summary>
/// Tests for the <see cref="TopsyTurvyTypeChecker"/> class.
/// </summary>
public class TypeCheckerTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds for a well-typed function call.
    /// </summary>
    [Fact]
    public void Check_WithMatchingArgumentTypes_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM add UNDER THE TERMS OF alpha AS A PEER AND beta AS A PEER TO FIND PEER
              AND SO I FIND SUM OF alpha AND beta
            MY DUTY IS DISCHARGED.
            PRAY WELCOME result AS A PEER BEING SUMMON add WITH 1 AND 2 IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds for a void function.
    /// </summary>
    [Fact]
    public void Check_WithVoidFunction_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
              BEHOLD name
            MY DUTY IS DISCHARGED.
            SUMMON greet WITH "Alice" IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds when NAUGHT is assigned to a YARN variable.
    /// </summary>
    [Fact]
    public void Check_AssignNaughtToYarn_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME str AS A YARN BEING NAUGHT
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds when integer widening is used in arithmetic.
    /// </summary>
    [Fact]
    public void Check_WithIntegerWideningArithmetic_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 1
            PRAY WELCOME dbl AS A FATHOM BEING 2.0
            PRAY WELCOME total AS A FATHOM BEING SUM OF num AND dbl
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds for a forward function reference.
    /// </summary>
    [Fact]
    public void Check_WithForwardFunctionReference_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME result AS A PEER BEING SUMMON getVal WITH NOTHING IF YOU PLEASE.
            IT IS MY DUTY TO PERFORM getVal UNDER NO OBLIGATION TO FIND PEER
              AND SO I FIND 42
            MY DUTY IS DISCHARGED.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds for widening-compatible ALIKE comparison.
    /// </summary>
    [Fact]
    public void Check_AlikePeerAndFathom_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 1
            PRAY WELCOME dbl AS A FATHOM BEING 1.0
            PRAY WELCOME equal AS A DECREE BEING ALIKE num AND dbl
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds for WOVEN OF with mixed types.
    /// </summary>
    [Fact]
    public void Check_WovenOfWithPeerAndYarn_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 42
            PRAY WELCOME msg AS A YARN BEING WOVEN OF "Value: " AND num IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds when AS IT WERE is used for an explicit cast.
    /// </summary>
    [Fact]
    public void Check_WithExpressionCast_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME str AS A YARN BEING "3"
            PRAY WELCOME num AS A PEER BEING AS IT WERE str AS A PEER
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds for a recursive function.
    /// </summary>
    [Fact]
    public void Check_WithRecursiveFunction_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM fib UNDER THE TERMS OF n AS A PEER TO FIND PEER
              SHOULD IT TRANSPIRE THAT LOWER DEGREE n AND 2
                QUITE SO.
                  AND SO I FIND 1
              SO MUCH FOR THAT.
              AND SO I FIND SUM OF SUMMON fib WITH DIFFERENCE OF n AND 1 IF YOU PLEASE. AND SUMMON fib WITH DIFFERENCE OF n AND 2 IF YOU PLEASE.
            MY DUTY IS DISCHARGED.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a function argument type mismatches.
    /// </summary>
    [Fact]
    public void Check_WithArgumentTypeMismatch_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
              BEHOLD name
            MY DUTY IS DISCHARGED.
            SUMMON greet WITH 42 IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a return type mismatches the declared return type.
    /// </summary>
    [Fact]
    public void Check_WithReturnTypeMismatch_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM getValue UNDER NO OBLIGATION TO FIND PEER
              AND SO I FIND "wrong"
            MY DUTY IS DISCHARGED.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when AND SO I FIND is used in a void function.
    /// </summary>
    [Fact]
    public void Check_WithReturnInVoidFunction_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM doSomething UNDER NO OBLIGATION
              AND SO I FIND 42
            MY DUTY IS DISCHARGED.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when MY DUTY IS PREMATURELY DISCHARGED is used in a typed function.
    /// </summary>
    [Fact]
    public void Check_WithEarlyReturnInTypedFunction_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM getValue UNDER NO OBLIGATION TO FIND PEER
              MY DUTY IS PREMATURELY DISCHARGED.
            MY DUTY IS DISCHARGED.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a variable is assigned the wrong type.
    /// </summary>
    [Fact]
    public void Check_WithVariableAssignmentTypeMismatch_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 42
            num IS APPOINTED "wrong"
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when NAUGHT is assigned to a PEER variable.
    /// </summary>
    [Fact]
    public void Check_AssignNaughtToPeer_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING NAUGHT
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when PRAY TELL targets a non-YARN variable.
    /// </summary>
    [Fact]
    public void Check_WithPrayTellOnNonYarnVariable_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER
            PRAY TELL num
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when A HIDEOUS CURSE ON receives a non-YARN value.
    /// </summary>
    [Fact]
    public void Check_WithThrowNonYarnValue_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            A HIDEOUS CURSE ON 42
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds and gives the MODIFIED RAPTURE binding YARN type in the catch scope.
    /// </summary>
    [Fact]
    public void Check_WithTryCatch_CaughtVariableIsYarn()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM risky UNDER NO OBLIGATION
              A HIDEOUS CURSE ON "oops"
            MY DUTY IS DISCHARGED.
            WITH THE GREATEST RESPECT, SUMMON risky WITH NOTHING IF YOU PLEASE.
              WITH GRATITUDE
                BEHOLD "ok"
              MODIFIED RAPTURE, err
                BEHOLD err
            THAT CONCLUDES THE MATTER.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a non-DECREE value is used in SHOULD IT TRANSPIRE THAT.
    /// </summary>
    [Fact]
    public void Check_WithNonDecreeConditionInConditional_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 1
            SHOULD IT TRANSPIRE THAT num
              QUITE SO.
                BEHOLD "yes"
            SO MUCH FOR THAT.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a non-DECREE value is used in WHILST.
    /// </summary>
    [Fact]
    public void Check_WithNonDecreeConditionInWhilst_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 1
            BY A LEGAL FICTION WHILST num
              num IS APPOINTED DIFFERENCE OF num AND 1
            THE TERM EXPIRES.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a non-DECREE operand is used in BOTH.
    /// </summary>
    [Fact]
    public void Check_WithNonDecreeOperandInBoth_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME flag AS A DECREE BEING VERITY
            PRAY WELCOME num AS A PEER BEING 1
            PRAY WELCOME combined AS A DECREE BEING BOTH flag AND num
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when ALIKE compares incompatible types.
    /// </summary>
    [Fact]
    public void Check_AlikePeerAndYarn_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 1
            PRAY WELCOME str AS A YARN BEING "x"
            PRAY WELCOME equal AS A DECREE BEING ALIKE num AND str
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when SUM OF uses a non-numeric operand.
    /// </summary>
    [Fact]
    public void Check_SumOfPeerAndYarn_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 1
            PRAY WELCOME str AS A YARN BEING "x"
            PRAY WELCOME total AS A PEER BEING SUM OF num AND str
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds and produces FATHOM type for SUM OF PEER and FATHOM.
    /// </summary>
    [Fact]
    public void Check_SumOfPeerAndFathom_SucceedsWithFathomResult()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 1
            PRAY WELCOME dbl AS A FATHOM BEING 1.0
            PRAY WELCOME total AS A FATHOM BEING SUM OF num AND dbl
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when PRE-ADAMITE uses a non-numeric operand.
    /// </summary>
    [Fact]
    public void Check_PreAdamiteYarnAndPeer_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME str AS A YARN BEING "x"
            PRAY WELCOME num AS A PEER BEING 1
            PRAY WELCOME less AS A DECREE BEING PRE-ADAMITE str AND num
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a bitwise operator uses a floating-point operand.
    /// </summary>
    [Fact]
    public void Check_BitwiseOperatorOnFathom_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME dbl AS A FATHOM BEING 1.0
            PRAY WELCOME bits AS A PEER BEING CHORD OF dbl AND 3
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds when a TRANSPOSITION UP/DOWN
    /// BY clause operand is a wider integer type than the value being shifted, since the result type is
    /// always the type of the shifted value and never widens across the shift amount.
    /// </summary>
    [Fact]
    public void Check_TranspositionWithWiderByOperand_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME small AS A PEER BEING 1
            PRAY WELCOME wide AS A CHANCELLOR BEING 3
            PRAY WELCOME result AS A PEER BEING TRANSPOSITION UP small BY wide
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a TRANSPOSITION UP/DOWN
    /// BY clause operand is not an integer type.
    /// </summary>
    [Fact]
    public void Check_TranspositionWithNonIntegerByOperand_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME value AS A PEER BEING 1
            PRAY WELCOME shiftAmount AS A FATHOM BEING 1.0
            PRAY WELCOME result AS A PEER BEING TRANSPOSITION UP value BY shiftAmount
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when an array element is assigned the wrong type.
    /// </summary>
    [Fact]
    public void Check_WithArrayElementAssignmentTypeMismatch_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME nums AS A LITTLE LIST OF PEER BEING 1 AND 2 AND 3 IF YOU PLEASE.
            VICTIM 1 ON nums IS APPOINTED "wrong"
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when ternary arms have incompatible types.
    /// </summary>
    [Fact]
    public void Check_WithTernaryIncompatibleArmTypes_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME flag AS A DECREE BEING VERITY
            PRAY WELCOME result AS A PEER BEING 42 SHOULD IT TRANSPIRE THAT flag OTHERWISE, "text"
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a switch case literal type mismatches the switch expression.
    /// </summary>
    [Fact]
    public void Check_WithSwitchCaseLiteralTypeMismatch_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 1
            IN WHICH CAPACITY? num
              WHEN ACTING AS "one"
                BEHOLD "text"
            NOTHING COULD BE MORE SATISFACTORY.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when THE LAW IS message is not YARN.
    /// </summary>
    [Fact]
    public void Check_WithAssertNonYarnMessage_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 1
            THE LAW IS ALIKE num AND 1 THAT 99
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method emits a warning for a standalone expression statement that discards a value.
    /// </summary>
    [Fact]
    public void Check_WithStandaloneExpressionStatement_EmitsWarning()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM getValue UNDER NO OBLIGATION TO FIND PEER
              AND SO I FIND 42
            MY DUTY IS DISCHARGED.
            SUMMON getValue WITH NOTHING IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method records the correct type for a literal in the semantic model.
    /// </summary>
    [Fact]
    public void Check_SemanticModel_RecordsIntegerLiteralType()
    {
        ProgramNode program = this.parser.Parse("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME num AS A PEER BEING 42
            FINALE.
            """);

        TopsyTurvyTypeChecker typeChecker = new();

        TypeCheckResult result = typeChecker.Check(program);

        result.Success.ShouldBeTrue();
        result.Model.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method records function signatures in the semantic model.
    /// </summary>
    [Fact]
    public void Check_SemanticModel_RecordsFunctionSignature()
    {
        ProgramNode program = this.parser.Parse("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM add UNDER THE TERMS OF alpha AS A PEER AND beta AS A PEER TO FIND PEER
              AND SO I FIND SUM OF alpha AND beta
            MY DUTY IS DISCHARGED.
            FINALE.
            """);

        TopsyTurvyTypeChecker typeChecker = new();
        
        TypeCheckResult result = typeChecker.Check(program);

        result.Success.ShouldBeTrue();
        FunctionSignature? signature = result.Model.GetFunctionSignature("add");
        signature.ShouldNotBeNull();
        signature!.ParameterTypes.Count.ShouldBe(2);
        signature.ReturnType.ShouldBe(LiteralType.Integer);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method recognises a function declared in a <c>PRAY ADMIT</c> import when a source file resolver is supplied.
    /// </summary>
    [Fact]
    public void Check_WithFunctionCallFromResolvedImport_Succeeds()
    {
        string importedSource = """
            HARK! "Utils"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
              BEHOLD WOVEN OF "Hello, " AND name AND "!" IF YOU PLEASE.
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        TypeCheckResult result = this.Check(
            """
            HARK! "Import Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY ADMIT "utils.topsy"
            SUMMON greet WITH "World" IF YOU PLEASE.
            FINALE.
            """,
            filename => filename == "utils.topsy" ? importedSource : null);

        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method still reports an undefined-function error when no source filer resolver is supplied, even in the presence of a <c>PRAY ADMIT</c> import.
    /// </summary>
    [Fact]
    public void Check_WithFunctionCallFromImportAndNoResolver_ReportsUndefinedFunction()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Import Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY ADMIT "utils.topsy"
            SUMMON greet WITH "World" IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains("greet"));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method does not recurse indefinitely when two imported files admit each other.
    /// </summary>
    [Fact]
    public void Check_WithCircularImports_DoesNotRecurseIndefinitely()
    {
        string fileA = """
            HARK! "A"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY ADMIT "b.topsy"
            IT IS MY DUTY TO PERFORM fromA UNDER NO OBLIGATION
              BEHOLD "A"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string fileB = """
            HARK! "B"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY ADMIT "a.topsy"
            IT IS MY DUTY TO PERFORM fromB UNDER NO OBLIGATION
              BEHOLD "B"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        TypeCheckResult result = this.Check(
            """
            HARK! "Main"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY ADMIT "a.topsy"
            SUMMON fromA WITH NOTHING IF YOU PLEASE.
            SUMMON fromB WITH NOTHING IF YOU PLEASE.
            FINALE.
            """,
            filename => filename switch
            {
                "a.topsy" => fileA,
                "b.topsy" => fileB,
                _ => null,
            });

        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds for a cross-file fully-qualified SUMMON call in both the long-form and short-form syntax.
    /// </summary>
    [Fact]
    public void Check_WithCrossFileFullyQualifiedSummon_Succeeds()
    {
        string importedSource = """
            HARK! "Mathematical"
            TOWN Mathematical
            IT IS MY DUTY TO PERFORM Add UNDER THE TERMS OF Num1 AS A PEER AND Num2 AS A PEER TO FIND PEER
              AND SO I FIND SUM OF Num1 AND Num2
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        TypeCheckResult result = this.Check(
            """
            HARK! "Main"
            PRAY ADMIT "mathematical.topsy"
            BEHOLD SUMMON Mathematical WITH DUTY Add WITH 2 AND 3 IF YOU PLEASE.
            BEHOLD SUMMON Mathematical*Add WITH 10 AND 20 IF YOU PLEASE.
            FINALE.
            """,
            filename => filename == "mathematical.topsy" ? importedSource : null);

        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds for a bare SUMMON call after PRAY RECOGNISE opens the declaring namespace.
    /// </summary>
    [Fact]
    public void Check_WithRecogniseThenBareSummon_Succeeds()
    {
        string importedSource = """
            HARK! "Mathematical"
            TOWN Mathematical
            IT IS MY DUTY TO PERFORM Add UNDER THE TERMS OF Num1 AS A PEER AND Num2 AS A PEER TO FIND PEER
              AND SO I FIND SUM OF Num1 AND Num2
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        TypeCheckResult result = this.Check(
            """
            HARK! "Main"
            PRAY ADMIT "mathematical.topsy"
            PRAY RECOGNISE Mathematical
            BEHOLD SUMMON Add WITH 7 AND 8 IF YOU PLEASE.
            FINALE.
            """,
            filename => filename == "mathematical.topsy" ? importedSource : null);

        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds for a bare SUMMON call between two functions declared in the same namespace, without requiring PRAY RECOGNISE.
    /// </summary>
    [Fact]
    public void Check_WithSameNamespaceBareSummon_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "SameNamespace"
            TOWN Mathematical
            IT IS MY DUTY TO PERFORM Double UNDER THE TERMS OF N AS A PEER TO FIND PEER
              AND SO I FIND SUMMON Add WITH N AND N IF YOU PLEASE.
            MY DUTY IS DISCHARGED.
            IT IS MY DUTY TO PERFORM Add UNDER THE TERMS OF Num1 AS A PEER AND Num2 AS A PEER TO FIND PEER
              AND SO I FIND SUM OF Num1 AND Num2
            MY DUTY IS DISCHARGED.
            BEHOLD SUMMON Double WITH 21 IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a bare SUMMON name matches functions in two different open namespaces.
    /// </summary>
    [Fact]
    public void Check_WithBareSummonMatchingTwoOpenNamespaces_ReportsAmbiguousReference()
    {
        string fileA = """
            HARK! "A"
            TOWN Alpha
            IT IS MY DUTY TO PERFORM Greet UNDER NO OBLIGATION
              BEHOLD "Alpha"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string fileB = """
            HARK! "B"
            TOWN Beta
            IT IS MY DUTY TO PERFORM Greet UNDER NO OBLIGATION
              BEHOLD "Beta"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        TypeCheckResult result = this.Check(
            """
            HARK! "Main"
            PRAY ADMIT "a.topsy"
            PRAY ADMIT "b.topsy"
            PRAY RECOGNISE Alpha
            PRAY RECOGNISE Beta
            SUMMON Greet WITH NOTHING IF YOU PLEASE.
            FINALE.
            """,
            filename => filename switch
            {
                "a.topsy" => fileA,
                "b.topsy" => fileB,
                _ => null,
            });

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains("ambiguous"));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an undefined function when a bare SUMMON call targets a namespaced function whose namespace has not been recognised.
    /// </summary>
    [Fact]
    public void Check_WithBareSummonToNamespacedFunctionWithoutRecognise_ReportsUndefinedFunction()
    {
        string importedSource = """
            HARK! "Mathematical"
            TOWN Mathematical
            IT IS MY DUTY TO PERFORM Add UNDER THE TERMS OF Num1 AS A PEER AND Num2 AS A PEER TO FIND PEER
              AND SO I FIND SUM OF Num1 AND Num2
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        TypeCheckResult result = this.Check(
            """
            HARK! "Main"
            PRAY ADMIT "mathematical.topsy"
            BEHOLD SUMMON Add WITH 1 AND 2 IF YOU PLEASE.
            FINALE.
            """,
            filename => filename == "mathematical.topsy" ? importedSource : null);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains("Add"));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a programme declares more than one TOWN.
    /// </summary>
    [Fact]
    public void Check_WithDuplicateNamespaceDeclaration_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Duplicate"
            TOWN Alpha
            TOWN Beta
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains("TOWN"));
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds when a pointer is declared pointing at a variable of the same declared type.
    /// </summary>
    [Fact]
    public void Check_WithPointerDeclarationExactTypeMatch_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME Number AS A PEER BEING 42
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Number
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a pointer type is declared as A GALLERY PICTURE OF NAUGHT.
    /// </summary>
    [Fact]
    public void Check_WithPointerDeclarationOfNaughtPointeeType_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME BadPointer AS A GALLERY PICTURE OF NAUGHT
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a pointer is pointed at a variable of a different type, since no numeric widening is permitted for pointee types.
    /// </summary>
    [Fact]
    public void Check_WithPointerDeclarationPointeeTypeMismatch_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME Amount AS A FATHOM BEING 1.0
            PRAY WELCOME AmountPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Amount
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a pointer to an array pointee type does not match the array declared element type.
    /// </summary>
    [Fact]
    public void Check_WithPointerToArrayElementTypeMismatch_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME Words AS A LITTLE LIST OF YARN BEING "Ruddigore" IF YOU PLEASE.
            PRAY WELCOME WordsPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Words
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds when a pointer to a YARN is declared with a STITCH pointee type, since a string decays to its first character.
    /// </summary>
    [Fact]
    public void Check_WithPointerToStringDeclaredAsStitch_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME Word AS A YARN BEING "Ruddigore"
            PRAY WELCOME WordPointer AS A GALLERY PICTURE OF STITCH BEING GALLERY PICTURE TO Word
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a pointer is assigned a literal value directly rather than an address-of expression.
    /// </summary>
    [Fact]
    public void Check_WithPointerAssignedLiteral_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER BEING 42
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a pointer is assigned another pointer directly by bare identifier rather than by address-of.
    /// </summary>
    [Fact]
    public void Check_WithPointerAssignedBareIdentifier_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME Number AS A PEER BEING 42
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Number
            PRAY WELCOME OtherPointer AS A GALLERY PICTURE OF PEER BEING NumberPointer
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds when a pointer is reassigned the result of pointer arithmetic on itself.
    /// </summary>
    [Fact]
    public void Check_WithPointerReassignedPointerArithmetic_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME Values AS A LITTLE LIST OF PEER BEING 1 AND 2 AND 3 IF YOU PLEASE.
            PRAY WELCOME ValuesPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Values
            ValuesPointer IS APPOINTED SUM OF ValuesPointer AND 1
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a dereference assignment writes a value of the wrong type through a pointer.
    /// </summary>
    [Fact]
    public void Check_WithDereferenceAssignmentTypeMismatch_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME Number AS A PEER BEING 42
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Number
            VIEW FROM NumberPointer IS APPOINTED "wrong"
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a dereference assignment targets a variable that is not a pointer.
    /// </summary>
    [Fact]
    public void Check_WithDereferenceAssignmentOnNonPointer_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME Number AS A PEER BEING 42
            VIEW FROM Number IS APPOINTED 5
            FINALE.
            """);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method succeeds when a pointer variable is compared against NAUGHT.
    /// </summary>
    [Fact]
    public void Check_WithPointerComparedToNaught_Succeeds()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER
            PRAY WELCOME IsUnassigned AS A DECREE BEING ALIKE NumberPointer AND NAUGHT
            FINALE.
            """);

        result.Success.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a void function call
    /// is used as a declaration initial value.
    /// </summary>
    [Fact]
    public void Check_WithVoidFunctionAsDeclarationValue_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
              BEHOLD name
            MY DUTY IS DISCHARGED.
            PRAY WELCOME result AS A YARN BEING SUMMON greet WITH "Alice" IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a void function call
    /// is used as an assignment value.
    /// </summary>
    [Fact]
    public void Check_WithVoidFunctionAsAssignmentValue_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
              PRAY WELCOME result AS A YARN
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
              BEHOLD name
            MY DUTY IS DISCHARGED.
            result IS APPOINTED SUMMON greet WITH "Alice" IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a void function call
    /// is used as an argument to another function call.
    /// </summary>
    [Fact]
    public void Check_WithVoidFunctionAsArgument_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
              BEHOLD name
            MY DUTY IS DISCHARGED.
            IT IS MY DUTY TO PERFORM identity UNDER THE TERMS OF text AS A YARN TO FIND YARN
              AND SO I FIND text
            MY DUTY IS DISCHARGED.
            SUMMON identity WITH SUMMON greet WITH "Alice" IF YOU PLEASE. IF YOU PLEASE.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyTypeChecker.Check"/> method reports an error when a void function call
    /// is used as a guard condition.
    /// </summary>
    [Fact]
    public void Check_WithVoidFunctionAsGuardCondition_ReportsError()
    {
        TypeCheckResult result = this.Check("""
            HARK! "Test"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
              BEHOLD name
            MY DUTY IS DISCHARGED.
            YEOMAN SUMMON greet WITH "Alice" IF YOU PLEASE.
              OTHERWISE,
                A HIDEOUS CURSE ON "Guard failed"
            UNDER ORDERS.
            FINALE.
            """);

        result.Success.ShouldBeFalse();
    }

    /// <summary>
    /// Runs the type checker on the given source code and returns the result.
    /// </summary>
    /// <param name="source">The source code to check.</param>
    /// <returns>The result of the type check.</returns>
    private TypeCheckResult Check(string source)
    {
        ProgramNode program = this.parser.Parse(source);
        TopsyTurvyTypeChecker typeChecker = new();
        return typeChecker.Check(program);
    }

    /// <summary>
    /// Runs the type checker on the given source code with a <c>PRAY ADMIT</c> resolver and returns the result.
    /// </summary>
    /// <param name="source">The source code to check.</param>
    /// <param name="sourceFileResolver">Resolves an import's filename to its source text.</param>
    /// <returns>The result of the type check.</returns>
    private TypeCheckResult Check(string source, Func<string, string?> sourceFileResolver)
    {
        ProgramNode program = this.parser.Parse(source);
        TopsyTurvyTypeChecker typeChecker = new();
        return typeChecker.Check(program, sourceFileResolver);
    }
}
