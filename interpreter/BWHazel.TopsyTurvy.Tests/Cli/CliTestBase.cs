using Testably.Abstractions.Testing;

namespace BWHazel.TopsyTurvy.Tests.Cli;

/// <summary>
/// Base class for CLI unit tests.
/// </summary>
public abstract class CliTestBase
{
    /// <summary>
    /// The in-memory mock file system shared across each test case.
    /// </summary>
    protected readonly MockFileSystem fileSystem = new();
}
