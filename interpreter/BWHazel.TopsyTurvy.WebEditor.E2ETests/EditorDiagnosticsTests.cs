using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.WebEditor.E2ETests;

/// <summary>
/// Playwright tests verifying that the Monaco editor shows diagnostics for Topsy Turvy source code.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="EditorDiagnosticsTests"/> class.
/// </remarks>
/// <param name="fixture">The shared Web Editor app fixture.</param>
public class EditorDiagnosticsTests(WebEditorAppFixture fixture)
    : WebEditorTestBase(fixture)
{
    private const string HelloWorldSource = """
        HARK! "Test"
        PRINCIPALS
        THE CURTAIN RISES.
        BEHOLD "Hello, World!"
        FINALE.
        """;

    /// <summary>
    /// Tests that error markers are shown in the Monaco editor when the source contains syntax errors.
    /// </summary>
    [Fact]
    public async Task Editor_WhenSyntaxErrorTyped_ShowsErrorMarkerInEditor()
    {
        await this.SetEditorValueAsync("HARK! \"Test\" INVALID FINALE.");

        await this.page.WaitForFunctionAsync(
            "() => monaco.editor.getModelMarkers({}).length > 0",
            null,
            new()
            {
                Timeout = 5_000
            });
        
        int count = await this.page.EvaluateAsync<int>("() => monaco.editor.getModelMarkers({}).length");

        Assert.True(count > 0);
    }

    /// <summary>
    /// Tests that no error markers are shown in the Monaco editor when the source is valid.
    /// </summary>
    [Fact]
    public async Task Editor_WhenValidSourceTyped_ShowsNoErrorMarkers()
    {
        await this.SetEditorValueAsync(HelloWorldSource);

        await this.page.WaitForFunctionAsync(
            "() => monaco.editor.getModelMarkers({}).length === 0",
            null,
            new()
            {
                Timeout = 5_000
            });
        
        int count = await this.page.EvaluateAsync<int>("() => monaco.editor.getModelMarkers({}).length");

        Assert.Equal(0, count);
    }
}
