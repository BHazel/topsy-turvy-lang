namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Options shared by resolvers that write JSON to STDOUT.
/// </summary>
/// <param name="Abridged">A value indicating whether to compact the JSON output.</param>
/// <param name="Chromatic">A value indicating whether to syntax-highlight the JSON output.</param>
/// <param name="Format">The temporary variable naming scheme to use when transforming Topsy Turvy source.</param>
/// <remarks>
/// The <paramref name="Abridged"/> and <paramref name="Chromatic"/> parameters come from the dedicated <c>--abridged</c>
/// and <c>--chromatic</c> command-line flags, not from <c>--config</c>.  The <paramref name="Format"/> parameter is
/// resolved from <c>--config varFormat</c> and handled by relevant resolvers handling UtopIR code.
/// </remarks>
public sealed record JsonEmitOptions(bool Abridged, bool Chromatic, VariableNameFormat Format = VariableNameFormat.Numeric);
