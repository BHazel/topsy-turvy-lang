using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace BWHazel.TopsyTurvy.WebEditor.E2ETests;

/// <summary>
/// xUnit collection fixture that starts the Blazor WASM Web Editor and a headless browser for the test run.
/// </summary>
public class WebEditorAppFixture : IAsyncLifetime
{
    /// <summary>
    /// The base URL at which the Web Editor is served during tests.
    /// </summary>
    public const string BaseUrl = "http://localhost:5188";

    private Process? appProcess;
    private IPlaywright? playwright;

    /// <summary>
    /// Gets the headless browser shared across the test collection.
    /// </summary>
    public IBrowser Browser { get; private set; } = null!;

    /// <summary>
    /// Starts the Web Editor process and the Playwright browser.
    /// </summary>
    public async Task InitializeAsync()
    {
        string projectRootDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string webEditorProject = Path.Combine(projectRootDirectory, "BWHazel.TopsyTurvy.WebEditor");

        this.appProcess = new()
        {
            StartInfo = new()
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{webEditorProject}\" --launch-profile http",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        this.appProcess.Start();

        using HttpClient httpClient = new();
        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(30));
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(BaseUrl, cancellationTokenSource.Token);
                if (response.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch
            {
                await Task.Delay(500, cancellationTokenSource.Token);
            }
        }

        this.playwright = await Playwright.CreateAsync();
        this.Browser = await this.playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    /// <summary>
    /// Disposes the browser, Playwright instance and the Web Editor process.
    /// </summary>
    public async Task DisposeAsync()
    {
        await this.Browser.DisposeAsync();
        this.playwright?.Dispose();

        if (this.appProcess is not null && !this.appProcess.HasExited)
        {
            this.appProcess.Kill(entireProcessTree: true);
            this.appProcess.Dispose();
        }
    }
}
