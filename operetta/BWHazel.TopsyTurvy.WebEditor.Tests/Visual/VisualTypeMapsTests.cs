using System;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Tests for the <see cref="VisualTypeMaps"/> class.
/// </summary>
public class VisualTypeMapsTests
{
    /// <summary>
    /// Tests that <see cref="VisualTypeMaps.TypeToKeyword"/> has exactly one entry per <see cref="LiteralType"/> value.
    /// </summary>
    [Fact]
    public void TypeToKeyword_ContainsExactlyOneEntryPerLiteralTypeEnumValue()
    {
        LiteralType[] allTypes = Enum.GetValues<LiteralType>();

        VisualTypeMaps.TypeToKeyword.Count.ShouldBe(allTypes.Length);
        foreach (LiteralType type in allTypes)
        {
            VisualTypeMaps.TypeToKeyword.ShouldContainKey(type);
        }
    }

    /// <summary>
    /// Tests that unsigned integer type variants compose the <c>STANDING</c> prefix with their base keyword.
    /// </summary>
    [Theory]
    [InlineData(LiteralType.UnsignedInteger, "STANDING PEER")]
    [InlineData(LiteralType.UnsignedLong, "STANDING CHANCELLOR")]
    [InlineData(LiteralType.UnsignedShort, "STANDING PIRATE")]
    [InlineData(LiteralType.Byte, "STANDING SAUSAGE-ROLL")]
    public void TypeToKeyword_UnsignedVariants_ComposeStandingPrefixWithBaseKeyword(LiteralType type, string expectedKeyword)
    {
        VisualTypeMaps.TypeToKeyword[type].ShouldBe(expectedKeyword);
    }

    /// <summary>
    /// Tests that <see cref="LiteralType.Array"/> and <see cref="LiteralType.Null"/> map to
    /// <c>LITTLE LIST OF</c> and <c>NAUGHT</c> respectively.
    /// </summary>
    [Fact]
    public void TypeToKeyword_ArrayAndNull_MapToLittleListOfAndNaughtRespectively()
    {
        VisualTypeMaps.TypeToKeyword[LiteralType.Array].ShouldBe("LITTLE LIST OF");
        VisualTypeMaps.TypeToKeyword[LiteralType.Null].ShouldBe("NAUGHT");
    }
}
