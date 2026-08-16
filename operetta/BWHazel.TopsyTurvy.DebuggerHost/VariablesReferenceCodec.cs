namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Encodes and decodes a DAP <c>variablesReference</c> as a frame ID and scope index pair, avoiding a separate
/// reference-tracking table.
/// </summary>
/// <remarks>
/// A <see cref="Debugger.DebugSession"/> never returns more than a handful of <see cref="Debugger.VariableScope"/>s
/// per frame, so packing the scope index into the low decimal digit of the frame ID is safe and collision-free for
/// as long as frame IDs stay below <see cref="System.Int32.MaxValue"/> / 10.
/// </remarks>
public static class VariablesReferenceCodec
{
    private const int ScopeIndexBase = 10;

    /// <summary>
    /// Encodes a frame ID and scope index into a single DAP <c>variablesReference</c>.
    /// </summary>
    /// <param name="frameId">The ID of the stack frame.</param>
    /// <param name="scopeIndex">The position of the scope in the list <see cref="Debugger.DebugSession.GetVariables"/> returned for that frame.</param>
    /// <returns>The encoded reference.</returns>
    public static long Encode(int frameId, int scopeIndex) => (frameId * ScopeIndexBase) + scopeIndex;

    /// <summary>
    /// Decodes a DAP <c>variablesReference</c> back into its frame ID and scope index.
    /// </summary>
    /// <param name="variablesReference">The reference to decode.</param>
    /// <returns>The frame id and scope index.</returns>
    public static (int FrameId, int ScopeIndex) Decode(long variablesReference) =>
        ((int)(variablesReference / ScopeIndexBase), (int)(variablesReference % ScopeIndexBase));
}
