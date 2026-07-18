using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.UtopIR.Transformer.LabelNameFormatters;

namespace BWHazel.TopsyTurvy.UtopIR.Tests.Transformer;

/// <summary>
/// Tests for <see cref="InstructionIncrementingDetailLabelNameFormatter"/>.
/// </summary>
public class InstructionIncrementingDetailLabelNameFormatterTests
{
    /// <summary>
    /// A zero-origin span used for all synthetic test AST nodes.
    /// </summary>
    private static readonly SourceSpan PlaceholderSpan = new(new(0, 0), new(0, 0));

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.FormatText(string[])"/> joins parts with
    /// an underscore and upper-cases the result.
    /// </summary>
    [Fact]
    public void FormatText_WithMultipleParts_JoinsWithUnderscoresAndUppercases()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();

        string text = formatter.FormatText("preadam", "Peer1", "Peer2");

        text.ShouldBe("PREADAM_PEER1_PEER2");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.FormatText(string[])"/> replaces a
    /// decimal point between two digits with <c>P</c>.
    /// </summary>
    [Fact]
    public void FormatText_WithDecimalPointBetweenDigits_ReplacesWithP()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();

        string text = formatter.FormatText("alike", "2.5");

        text.ShouldBe("ALIKE_2P5");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.FormatText(string[])"/> replaces
    /// punctuation other than a decimal point between digits with an underscore.
    /// </summary>
    [Fact]
    public void FormatText_WithOtherPunctuation_ReplacesWithUnderscore()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();

        string text = formatter.FormatText("alike", "\"hi!\"");

        text.ShouldBe("ALIKE_HI_");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.FormatText(string[])"/> drops the
    /// <c>£</c> sigil entirely rather than replacing it with an underscore.
    /// </summary>
    [Fact]
    public void FormatText_WithSigil_DropsSigilEntirely()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();

        string text = formatter.FormatText("preadam", "£Peer1", "£Peer2");

        text.ShouldBe("PREADAM_PEER1_PEER2");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.FormatText(string[])"/> collapses runs
    /// of consecutive underscores produced by punctuation conversion into a single underscore.
    /// </summary>
    [Fact]
    public void FormatText_WithConsecutivePunctuation_CollapsesToSingleUnderscore()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();

        string text = formatter.FormatText("alike", "!!!", "Peer1");

        text.ShouldBe("ALIKE_PEER1");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.FormatText(string[])"/> upper-cases
    /// lower-case letters throughout the whole result.
    /// </summary>
    [Fact]
    public void FormatText_WithLowercaseLetters_UppercasesWholeResult()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();

        string text = formatter.FormatText("lowerdeg", "counter", "10");

        text.ShouldBe("LOWERDEG_COUNTER_10");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.ForConditional(ConditionalNode, string, IReadOnlyList{string})"/>
    /// returns the true/else/closing labels for a conditional with no else-if branches.
    /// </summary>
    [Fact]
    public void ForConditional_ConditionalNodeWithNoElseIfs_ReturnsTrueElseClosingLabels()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();
        ConditionalNode node = new()
        {
            Condition = Identifier("A"),
            TrueBlock = [],
            Span = PlaceholderSpan
        };

        ConditionalLabels labels = formatter.ForConditional(node, "A", []);

        labels.TrueLabel.Name.ShouldBe("T1QS_A");
        labels.ElseIfLabels.ShouldBeEmpty();
        labels.ElseLabel.Name.ShouldBe("T1O");
        labels.ClosingLabel.Name.ShouldBe("T1SMFT");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.ForConditional(ConditionalNode, string, IReadOnlyList{string})"/>
    /// returns one else-if label per supplied condition text, in order.
    /// </summary>
    [Fact]
    public void ForConditional_ConditionalNodeWithElseIfs_ReturnsElseIfLabelsInOrder()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();
        ConditionalNode node = new()
        {
            Condition = Identifier("A"),
            TrueBlock = [],
            Span = PlaceholderSpan
        };

        ConditionalLabels labels = formatter.ForConditional(node, "A", ["B", "C"]);

        labels.ElseIfLabels.Count.ShouldBe(2);
        labels.ElseIfLabels[0].Name.ShouldBe("T1OIN_B");
        labels.ElseIfLabels[1].Name.ShouldBe("T1OIN_C");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.ForConditional(TernaryExpressionNode, string)"/>
    /// returns the same true/else/closing shape as a conditional, with no else-if labels.
    /// </summary>
    [Fact]
    public void ForConditional_TernaryExpressionNode_ReturnsTrueElseClosingLabelsWithNoElseIfs()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();
        TernaryExpressionNode node = new()
        {
            TrueValue = Identifier("X"),
            Condition = Identifier("A"),
            FalseValue = Identifier("Y"),
            Span = PlaceholderSpan
        };

        ConditionalLabels labels = formatter.ForConditional(node, "A");

        labels.TrueLabel.Name.ShouldBe("T1QS_A");
        labels.ElseIfLabels.ShouldBeEmpty();
        labels.ElseLabel.Name.ShouldBe("T1O");
        labels.ClosingLabel.Name.ShouldBe("T1SMFT");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.ForGuard(GuardNode)"/> returns the
    /// else and closing labels for a guard clause.
    /// </summary>
    [Fact]
    public void ForGuard_ReturnsElseAndClosingLabels()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();
        GuardNode node = new()
        {
            Condition = Identifier("A"),
            ElseBlock = [],
            Span = PlaceholderSpan
        };

        GuardLabels labels = formatter.ForGuard(node);

        labels.ElseLabel.Name.ShouldBe("G1O");
        labels.ClosingLabel.Name.ShouldBe("G1UO");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.ForSwitch(SwitchNode, IReadOnlyList{string})"/>
    /// returns one case label per supplied case text, in order, plus the default and closing labels.
    /// </summary>
    [Fact]
    public void ForSwitch_ReturnsCaseLabelsInOrderPlusDefaultAndClosing()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();
        SwitchNode node = new()
        {
            Expression = Identifier("X"),
            Cases = [new SwitchCase(1, []), new SwitchCase(2, [])],
            Span = PlaceholderSpan
        };

        SwitchLabels labels = formatter.ForSwitch(node, ["1", "2"]);

        labels.CaseLabels.Count.ShouldBe(2);
        labels.CaseLabels[0].Name.ShouldBe("C1AS_1");
        labels.CaseLabels[1].Name.ShouldBe("C1AS_2");
        labels.DefaultLabel.Name.ShouldBe("C1FAIL");
        labels.ClosingLabel.Name.ShouldBe("C1NCBMS");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.ForLoop(LoopNode, string?)"/> returns a
    /// <c>null</c> continue label for an infinite loop, since it reuses <c>Opening</c> directly.
    /// </summary>
    [Fact]
    public void ForLoop_Infinite_ReturnsNullContinueLabel()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();
        LoopNode node = new()
        {
            Type = LoopType.Infinite,
            Body = [],
            Span = PlaceholderSpan
        };

        LoopLabels labels = formatter.ForLoop(node, conditionText: null);

        labels.OpeningLabel.Name.ShouldBe("L1");
        labels.ContinueLabel.ShouldBeNull();
        labels.ClosingLabel.Name.ShouldBe("L1TTE");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.ForLoop(LoopNode, string?)"/> returns a
    /// <c>null</c> continue label for a Whilst loop, since it reuses <c>Opening</c> directly.
    /// </summary>
    [Fact]
    public void ForLoop_Whilst_ReturnsNullContinueLabel()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();
        LoopNode node = new()
        {
            Type = LoopType.Whilst,
            Condition = Identifier("A"),
            Body = [],
            Span = PlaceholderSpan
        };

        LoopLabels labels = formatter.ForLoop(node, "A");

        labels.OpeningLabel.Name.ShouldBe("L1W_A");
        labels.ContinueLabel.ShouldBeNull();
        labels.ClosingLabel.Name.ShouldBe("L1WTTE");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.ForLoop(LoopNode, string?)"/> returns a
    /// non-null <c>OM</c> continue label for an ascending loop.
    /// </summary>
    [Fact]
    public void ForLoop_Ascending_ReturnsNonNullContinueLabel()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();
        LoopNode node = new()
        {
            Type = LoopType.Ascending,
            Condition = Identifier("A"),
            LoopVariable = "Counter",
            Body = [],
            Span = PlaceholderSpan
        };

        LoopLabels labels = formatter.ForLoop(node, "A");

        labels.OpeningLabel.Name.ShouldBe("L1ASC_A");
        labels.ContinueLabel.ShouldNotBeNull();
        labels.ContinueLabel!.Name.ShouldBe("L1ASCOM");
        labels.ClosingLabel.Name.ShouldBe("L1ASCTTE");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.ForLoop(LoopNode, string?)"/> returns a
    /// non-null <c>OM</c> continue label for a descending loop, distinguished from ascending by the <c>DESC</c> tag.
    /// </summary>
    [Fact]
    public void ForLoop_Descending_ReturnsNonNullContinueLabel()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();
        LoopNode node = new()
        {
            Type = LoopType.Descending,
            Condition = Identifier("A"),
            LoopVariable = "Counter",
            Body = [],
            Span = PlaceholderSpan
        };

        LoopLabels labels = formatter.ForLoop(node, "A");

        labels.OpeningLabel.Name.ShouldBe("L1DESC_A");
        labels.ContinueLabel.ShouldNotBeNull();
        labels.ContinueLabel!.Name.ShouldBe("L1DESCOM");
        labels.ClosingLabel.Name.ShouldBe("L1DESCTTE");
    }

    /// <summary>
    /// Tests that <see cref="InstructionIncrementingDetailLabelNameFormatter.ForLoop(LoopNode, string?)"/> embeds a
    /// loop optional <c>KNOWN AS</c> name, formatted, into its opening label.
    /// </summary>
    [Fact]
    public void ForLoop_WithName_EmbedsFormattedNameInOpeningLabel()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();
        LoopNode node = new()
        {
            Label = "Incrementer",
            Type = LoopType.Infinite,
            Body = [],
            Span = PlaceholderSpan
        };

        LoopLabels labels = formatter.ForLoop(node, conditionText: null);

        labels.OpeningLabel.Name.ShouldBe("L1_INCREMENTER");
    }

    /// <summary>
    /// Tests that the construct counter is shared across every <c>For*</c> dispatch method, not
    /// tracked separately per construct kind.
    /// </summary>
    [Fact]
    public void ForGuard_ThenForConditional_ShareSingleIncrementingCounter()
    {
        InstructionIncrementingDetailLabelNameFormatter formatter = new();
        GuardNode guardNode = new()
        {
            Condition = Identifier("A"),
            ElseBlock = [],
            Span = PlaceholderSpan
        };

        ConditionalNode conditionalNode = new()
        {
            Condition = Identifier("A"),
            TrueBlock = [],
            Span = PlaceholderSpan
        };

        GuardLabels guardLabels = formatter.ForGuard(guardNode);
        ConditionalLabels conditionalLabels = formatter.ForConditional(conditionalNode, "A", []);

        guardLabels.ElseLabel.Name.ShouldBe("G1O");
        conditionalLabels.TrueLabel.Name.ShouldBe("T2QS_A");
    }

    /// <summary>
    /// Builds a minimal <see cref="IdentifierNode"/>, for use as a placeholder condition expression.
    /// </summary>
    private static IdentifierNode Identifier(string name) => new()
    {
        Name = name,
        Span = PlaceholderSpan
    };
}
