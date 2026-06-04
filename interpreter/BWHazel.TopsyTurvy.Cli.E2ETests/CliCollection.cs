namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// xUnit collection definition for CLI E2E tests sharing a single <see cref="CliFixture"/>.
/// </summary>
[CollectionDefinition("Cli")]
public sealed class CliCollection : ICollectionFixture<CliFixture>
{
}
