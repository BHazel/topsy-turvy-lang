using System.Threading.Tasks;
using Microsoft.Playwright;

namespace BWHazel.TopsyTurvy.WebEditor.E2ETests;

/// <summary>
/// Base class for the Web Editor Playwright tests.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="WebEditorTestBase"/> class.
/// </remarks>
/// <param name="fixture">The shared Web Editor app fixture.</param>
[Collection("WebEditor")]
public abstract class WebEditorTestBase(WebEditorAppFixture fixture) : IAsyncLifetime
{
    private readonly WebEditorAppFixture fixture = fixture;

    /// <summary>
    /// The Playwright page used for each individual test.
    /// </summary>
    protected IPage page = null!;

    /// <summary>
    /// Opens a new browser page and navigates to the Web Editor.
    /// </summary>
    public async Task InitializeAsync()
    {
        this.page = await this.fixture.Browser.NewPageAsync();
        await this.page.GotoAsync(WebEditorAppFixture.BaseUrl);
        await this.WaitForAppReadyAsync();
    }

    /// <summary>
    /// Closes the page after each test.
    /// </summary>
    public async Task DisposeAsync() => await this.page.CloseAsync();

    /// <summary>
    /// Sets the Monaco editor content to the provided source code.
    /// </summary>
    /// <param name="source">The source code to load into the editor.</param>
    protected async Task SetEditorValueAsync(string source) =>
        await this.page.EvaluateAsync("(src) => monaco.editor.getModels()[0].setValue(src)", source);

    /// <summary>
    /// Waits for the Web Editor app to be ready.
    /// </summary>
    /// <remarks>
    /// Checks for the presence of the Perform button and the Monaco editor instance.
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task WaitForAppReadyAsync()
    {
        await this.page.WaitForSelectorAsync(
            "button:has-text(\"Perform\")",
            new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30_000
            });

        await this.page.WaitForFunctionAsync(
            "() => typeof monaco !== 'undefined' && monaco.editor !== undefined",
            null,
            new()
            {
                Timeout = 10_000
            });
    }
}
