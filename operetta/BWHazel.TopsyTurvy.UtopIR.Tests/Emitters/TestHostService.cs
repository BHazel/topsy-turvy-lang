using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.UtopIR.Tests.Emitters;

/// <summary>
/// A minimal host-injected service used to test the <see cref="BWHazel.TopsyTurvy.UtopIR.Emitters.Cil.CilEmitter"/>
/// <c>summon</c>/<c>summon.find</c> emission, without depending on a real service.
/// </summary>
/// <remarks>
/// An instance of this type is constructed by the emitted programme itself, not by the test, so
/// observing what it recorded, and how many times it was constructed, is done through static state
/// shared across every instance, reset at the start of each test via <see cref="ClearLog"/>.
/// </remarks>
public sealed class TestHostService
{
    private static readonly List<string> log = [];

    /// <summary>
    /// Initialises a new instance of the <see cref="TestHostService"/> class, incrementing <see cref="ConstructionCount"/>.
    /// </summary>
    public TestHostService() => ConstructionCount++;

    /// <summary>
    /// Gets every value recorded via <see cref="Record"/> across every constructed instance, in call order.
    /// </summary>
    public static IReadOnlyList<string> Log => log;

    /// <summary>
    /// Gets the number of <see cref="TestHostService"/> instances constructed since the last <see cref="ClearLog"/>.
    /// </summary>
    public static int ConstructionCount { get; private set; }

    /// <summary>
    /// Clears <see cref="Log"/> and resets <see cref="ConstructionCount"/> to zero.
    /// </summary>
    public static void ClearLog()
    {
        log.Clear();
        ConstructionCount = 0;
    }

    /// <summary>
    /// Records a value into <see cref="Log"/>.
    /// </summary>
    /// <param name="text">The text to record.</param>
    public void Record(string text) => log.Add(text);
}
