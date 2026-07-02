namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Options shared by resolvers that write JSON to STDOUT.
/// </summary>
/// <param name="Abridged">A value indicating whether to compact the JSON output.</param>
/// <param name="Chromatic">A value indicating whether to syntax-highlight the JSON output.</param>
/// <remarks>
/// These values come from the dedicated <c>--abridged</c> and <c>--chromatic</c> command-line flags, not from
/// <c>--config</c>.  A resolver using this type still rejects any <c>--config</c> key via
/// <see cref="IEmitterOptionsResolver{TOptions}.Apply"/>, the same as a resolver using <see cref="NoOptions"/>.
/// </remarks>
public sealed record JsonEmitOptions(bool Abridged, bool Chromatic);
