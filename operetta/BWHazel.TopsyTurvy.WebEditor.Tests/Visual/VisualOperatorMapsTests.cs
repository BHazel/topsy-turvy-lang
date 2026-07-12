using System;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Tests for the <see cref="VisualOperatorMaps"/> class.
/// </summary>
public class VisualOperatorMapsTests
{
    /// <summary>
    /// Tests that <see cref="VisualOperatorMaps.OperatorToTitle"/> has exactly one entry per <see cref="Operator"/> value.
    /// </summary>
    [Fact]
    public void OperatorToTitle_ContainsExactlyOneEntryPerOperatorEnumValue()
    {
        Operator[] allOperators = Enum.GetValues<Operator>();

        VisualOperatorMaps.OperatorToTitle.Count.ShouldBe(allOperators.Length);
        foreach (Operator theOperator in allOperators)
        {
            VisualOperatorMaps.OperatorToTitle.ShouldContainKey(theOperator);
        }
    }

    /// <summary>
    /// Tests that <see cref="VisualOperatorMaps.TitleToOperator"/> has the same number of entries as
    /// <see cref="VisualOperatorMaps.OperatorToTitle"/>, and that looking up each operator title in
    /// <see cref="VisualOperatorMaps.TitleToOperator"/> maps back to that same operator, i.e. no two
    /// operators share a title.
    /// </summary>
    [Fact]
    public void TitleToOperator_MapsEachTitleBackToItsOriginalOperatorWithNoCollisions()
    {
        VisualOperatorMaps.TitleToOperator.Count.ShouldBe(VisualOperatorMaps.OperatorToTitle.Count);

        foreach ((Operator theOperator, string title) in VisualOperatorMaps.OperatorToTitle.Select(kvp => (kvp.Key, kvp.Value)))
        {
            VisualOperatorMaps.TitleToOperator.ShouldContainKey(title);
            VisualOperatorMaps.TitleToOperator[title].ShouldBe(theOperator);
        }
    }

    /// <summary>
    /// Tests that every title in <see cref="VisualOperatorMaps.OperatorToTitle"/> is non-empty.
    /// </summary>
    [Fact]
    public void OperatorToTitle_AllTitlesAreNonEmpty()
    {
        foreach (string title in VisualOperatorMaps.OperatorToTitle.Values)
        {
            title.ShouldNotBeNullOrWhiteSpace();
        }
    }
}
