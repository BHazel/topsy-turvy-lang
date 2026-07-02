using System;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Thrown by <see cref="IEmitterOptionsResolver{TOptions}.Apply"/> when <c>--config</c> contains an
/// unrecognised key or an invalid value for a recognised key.
/// </summary>
/// <param name="message">A user-facing message describing the invalid key or value.</param>
public sealed class ToolchainConfigException(string message)
    : Exception(message);
