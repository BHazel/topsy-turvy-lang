namespace BWHazel.TopsyTurvy.UtopIR.Tests.Emitters;

/// <summary>
/// Functions used as <see cref="BWHazel.TopsyTurvy.UtopIR.Emitters.Cil.CilExternalFunction"/> targets to
/// test the <see cref="BWHazel.TopsyTurvy.UtopIR.Emitters.Cil.CilEmitter"/> <c>summon</c>/<c>summon.find</c>
/// emission, without depending on the real Standard Library.
/// </summary>
public static class TestSummonTargets
{
    /// <summary>
    /// A void function that records <paramref name="text"/> via the host-injected <see cref="TestHostService"/>.
    /// </summary>
    /// <param name="text">The text to record.</param>
    /// <param name="service">The host-injected service.</param>
    public static void WriteText(string text, TestHostService service) => service.Record(text);

    /// <summary>
    /// A value-returning function with no host-injected parameter, used to test <c>summon.find</c>.
    /// </summary>
    /// <param name="value">The value to double.</param>
    /// <returns><paramref name="value"/> multiplied by two.</returns>
    public static int Double(int value) => value * 2;
}
