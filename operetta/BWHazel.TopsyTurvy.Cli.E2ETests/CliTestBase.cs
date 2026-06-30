using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.Cli.E2ETests;

/// <summary>
/// Abstract base class for CLI E2E tests providing per-test working directories and a helper to spawn the CLI binary.
/// </summary>
public abstract class CliTestBase : IDisposable
{
    private readonly CliFixture fixture;

    /// <summary>
    /// Initialises a new instance of the <see cref="CliTestBase"/> class.
    /// </summary>
    /// <param name="fixture">The shared CLI fixture providing the binary path and root directory.</param>
    protected CliTestBase(CliFixture fixture)
    {
        this.fixture = fixture;
        string subDirectory = Path.Combine(fixture.RootDirectory, Path.GetRandomFileName());
        Directory.CreateDirectory(subDirectory);
        this.WorkingDirectory = subDirectory;
    }

    /// <summary>
    /// Gets the unique per-test working directory on the real filesystem.
    /// </summary>
    protected string WorkingDirectory { get; }

    /// <summary>
    /// Runs the CLI binary with the given argument string and returns the exit code, stdout and stderr.
    /// </summary>
    /// <param name="args">The command-line arguments to pass to the CLI binary.</param>
    /// <returns>A tuple of exit code, captured stdout and captured stderr.</returns>
    protected async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(string args)
    {
        ProcessStartInfo startInfo = new(this.fixture.BinaryPath, args)
        {
            WorkingDirectory = this.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = new()
        {
            StartInfo = startInfo
        };

        process.Start();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        bool exited = process.WaitForExit(TimeSpan.FromSeconds(10));
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;
        return (process.ExitCode, stdout, stderr);
    }

    /// <summary>
    /// Runs the CLI binary with the given argument string, writes <paramref name="stdin"/> to the
    /// process standard input, and returns the exit code, stdout and stderr.
    /// </summary>
    /// <param name="args">The command-line arguments to pass to the CLI binary.</param>
    /// <param name="stdin">The text to supply as standard input, with lines separated by <c>\n</c>.</param>
    /// <returns>A tuple of exit code, captured stdout and captured stderr.</returns>
    protected async Task<(int ExitCode, string Stdout, string Stderr)> RunWithStdinAsync(string args, string stdin)
    {
        ProcessStartInfo startInfo = new(this.fixture.BinaryPath, args)
        {
            WorkingDirectory = this.WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = new()
        {
            StartInfo = startInfo
        };

        process.Start();
        await process.StandardInput.WriteAsync(stdin);
        process.StandardInput.Close();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        bool isExited = process.WaitForExit(TimeSpan.FromSeconds(10));
        if (!isExited)
        {
            process.Kill(entireProcessTree: true);
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;
        return (process.ExitCode, stdout, stderr);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(this.WorkingDirectory))
        {
            Directory.Delete(this.WorkingDirectory, recursive: true);
        }
    }
}
