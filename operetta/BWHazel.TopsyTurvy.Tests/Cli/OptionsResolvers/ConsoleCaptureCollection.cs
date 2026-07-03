namespace BWHazel.TopsyTurvy.Tests.Cli.OptionsResolvers;

/// <summary>
/// xUnit collection definition for tests that redirect the process-global <see cref="System.Console.Out"/> to capture emitted STDOUT text.
/// </summary>
/// <remarks>
/// This collection disables parallelisation between tests so one test redirect cannot leak into another running concurrently.
/// </remarks>
[CollectionDefinition("ConsoleCapture", DisableParallelization = true)]
public sealed class ConsoleCaptureCollection
{
}
