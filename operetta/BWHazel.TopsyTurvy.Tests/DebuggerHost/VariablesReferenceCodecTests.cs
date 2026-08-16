using BWHazel.TopsyTurvy.DebuggerHost;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="VariablesReferenceCodec"/> class.
/// </summary>
public class VariablesReferenceCodecTests
{
    /// <summary>
    /// Tests that the <see cref="VariablesReferenceCodec.Decode"/> method reverses
    /// <see cref="VariablesReferenceCodec.Encode"/> for a range of frame IDs and scope indices.
    /// </summary>
    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(42, 0)]
    [InlineData(42, 9)]
    public void Decode_AfterEncode_ReturnsOriginalFrameIdAndScopeIndex(int frameId, int scopeIndex)
    {
        long reference = VariablesReferenceCodec.Encode(frameId, scopeIndex);

        (int decodedFrameId, int decodedScopeIndex) = VariablesReferenceCodec.Decode(reference);

        decodedFrameId.ShouldBe(frameId);
        decodedScopeIndex.ShouldBe(scopeIndex);
    }
}
