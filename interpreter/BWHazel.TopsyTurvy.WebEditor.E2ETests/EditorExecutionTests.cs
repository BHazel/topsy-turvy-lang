using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.WebEditor.E2ETests;

/// <summary>
/// Playwright tests verifying that the Web Editor can execute Topsy Turvy programmes.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="EditorExecutionTests"/> class.
/// </remarks>
/// <param name="fixture">The shared Web Editor app fixture.</param>
public class EditorExecutionTests(WebEditorAppFixture fixture)
    : WebEditorTestBase(fixture)
{
    private const string HelloWorldSource = """
        HARK! "Test"
        PRINCIPALS
        THE CURTAIN RISES.
        BEHOLD "Hello, World!"
        FINALE.
        """;

    private const string StdinSource = """
        HARK! "Stdin Test"
        PRINCIPALS
          PRAY WELCOME name AS A YARN
        THE CURTAIN RISES.
        PRAY TELL name
        BEHOLD name
        FINALE.
        """;

    /// <summary>
    /// Tests that the Web Editor runs a Hello World programme and shows output in the terminal.
    /// </summary>
    [Fact]
    public async Task RunProgramme_WithHelloWorld_ShowsOutputInTerminal()
    {
        await this.SetEditorValueAsync(HelloWorldSource);

        await this.page.ClickAsync("button:has-text(\"Perform\")");

        await this.page.WaitForFunctionAsync(
            "() => document.body.innerText.includes('Hello, World!')",
            null,
            new()
            {
                Timeout = 10_000
            });
        
        string bodyText = await this.page.EvaluateAsync<string>("() => document.body.innerText");
        Assert.Contains("Hello, World!", bodyText);
    }

    /// <summary>
    /// Tests that the Web Editor passes standard input to the programme and shows it in the terminal.
    /// </summary>
    [Fact]
    public async Task RunProgramme_WithStdinInput_ShowsInputValueInTerminal()
    {
        Microsoft.Playwright.ILocator stdinTextarea = this.page
            .Locator("label:has-text(\"Standard Input\")")
            .Locator("xpath=../..")
            .Locator("textarea");
        await stdinTextarea.FillAsync("Gilbert");
        await this.SetEditorValueAsync(StdinSource);

        await this.page.ClickAsync("button:has-text(\"Perform\")");

        await this.page.WaitForFunctionAsync(
            "() => document.body.innerText.includes('Gilbert')",
            null,
            new()
            {
                Timeout = 10_000
            });
        
        string bodyText = await this.page.EvaluateAsync<string>("() => document.body.innerText");
        Assert.Contains("Gilbert", bodyText);
    }
}
