using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Provides forward and reverse maps between <see cref="Operator"/> enum values and their visual editor title strings.
/// </summary>
internal static class VisualOperatorMaps
{
    /// <summary>
    /// Maps each <see cref="Operator"/> value to the keyword string used as the node title in the visual editor.
    /// </summary>
    internal static readonly Dictionary<Operator, string> OperatorToTitle = new()
    {
        [Operator.Sum] = "SUM OF",
        [Operator.Difference] = "DIFFERENCE OF",
        [Operator.Product] = "PRODUCT OF",
        [Operator.Quotient] = "QUOTIENT OF",
        [Operator.Remainder] = "REMAINDER OF",
        [Operator.Larger] = "LARGER OF",
        [Operator.Smaller] = "SMALLER OF",
        [Operator.Both] = "BOTH",
        [Operator.Either] = "EITHER",
        [Operator.HardlyEver] = "HARDLY EVER",
        [Operator.Alike] = "ALIKE",
        [Operator.Unlike] = "UNLIKE",
        [Operator.PreAdamite] = "PRE-ADAMITE",
        [Operator.LowerDegree] = "LOWER DEGREE",
        [Operator.WovenOf] = "WOVEN OF",
        [Operator.Summon] = "SUMMON",
        [Operator.AllOf] = "ALL OF",
        [Operator.AnyOf] = "ANY OF",
        [Operator.ChordOf] = "CHORD OF",
        [Operator.HarmonyOf] = "HARMONY OF",
        [Operator.DiscordOf] = "DISCORD OF",
        [Operator.InversionOf] = "INVERSION OF",
        [Operator.TranspositionUp] = "TRANSPOSITION UP",
        [Operator.TranspositionDown] = "TRANSPOSITION DOWN",
    };

    /// <summary>
    /// Maps each visual editor title string back to the corresponding <see cref="Operator"/> value.
    /// </summary>
    internal static readonly Dictionary<string, Operator> TitleToOperator;

    /// <summary>
    /// Initialises the <see cref="TitleToOperator"/> map by reversing the <see cref="OperatorToTitle"/> map.
    /// </summary>
    static VisualOperatorMaps()
    {
        TitleToOperator = new(OperatorToTitle.Count);
        foreach (KeyValuePair<Operator, string> kvp in OperatorToTitle)
        {
            TitleToOperator[kvp.Value] = kvp.Key;
        }
    }
}
