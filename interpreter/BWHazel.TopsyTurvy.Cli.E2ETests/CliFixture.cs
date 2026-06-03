using System;
using System.IO;
using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// xUnit collection fixture that locates the operetta binary and manages the shared temporary directory root for all CLI E2E tests.
/// </summary>
public sealed class CliFixture : IAsyncLifetime
{
    private const string TemporaryDirectoryPrefix = "topsyturvy-cli-e2e-";
    private const string DefaultBinaryName = "operetta";

    /// <summary>
    /// Gets the absolute path to the CLI binary under test.
    /// </summary>
    public string BinaryPath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the root temporary directory for this test run.
    /// </summary>
    public string RootDirectory { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        string binaryName = OperatingSystem.IsWindows()
            ? $"{DefaultBinaryName}.exe"
            : DefaultBinaryName;
        
        this.BinaryPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "BWHazel.TopsyTurvy.Cli", "bin", "Debug", "net10.0", binaryName));

        if (!File.Exists(this.BinaryPath))
        {
            throw new InvalidOperationException(
                $"CLI binary not found at '{this.BinaryPath}'. " +
                "Ensure the CLI project is built before running the tests.");
        }

        string temporaryPath = Path.GetTempPath();
        foreach (string leftoverDirectory in Directory.GetDirectories(temporaryPath, $"{TemporaryDirectoryPrefix}*"))
        {
            try
            {
                Directory.Delete(leftoverDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        this.RootDirectory = Directory.CreateTempSubdirectory(TemporaryDirectoryPrefix).FullName;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        if (Directory.Exists(this.RootDirectory))
        {
            Directory.Delete(this.RootDirectory, recursive: true);
        }

        return Task.CompletedTask;
    }
}
