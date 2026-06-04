using System.Threading.Tasks;
using Microsoft.Playwright;

namespace BWHazel.TopsyTurvy.WebEditor.E2ETests;

/// <summary>
/// Playwright tests verifying the Web Editor toolbar behaviour.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="EditorToolbarTests"/> class.
/// </remarks>
/// <param name="fixture">The shared Web Editor app fixture.</param>
public class EditorToolbarTests(WebEditorAppFixture fixture)
    : WebEditorTestBase(fixture)
{
    /// <summary>
    /// Tests that toggling the G&S Labels switch changes every affected toolbar button label.
    /// </summary>
    /// <param name="gsLabel">The G&S-themed button label visible before toggling.</param>
    /// <param name="standardLabel">The standard button label expected after toggling.</param>
    [Theory]
    [InlineData("Mount", "New Project")]
    [InlineData("Commission", "New File")]
    [InlineData("Recall", "Open")]
    [InlineData("Pen", "Save")]
    [InlineData("Perform", "Run")]
    public async Task Toolbar_WhenGsLabelSwitchToggled_ChangesButtonLabel(string gsLabel, string standardLabel)
    {
        await Assertions.Expect(this.page.Locator($"button:has-text(\"{gsLabel}\")")).ToBeVisibleAsync();

        await this.page.GetByLabel("G&S Labels").ClickAsync();

        await this.page.WaitForSelectorAsync(
            $"button:has-text(\"{standardLabel}\")",
            new()
            {
                State = WaitForSelectorState.Visible
            });

        await Assertions.Expect(this.page.Locator($"button:has-text(\"{standardLabel}\")")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Tests that clicking the dark mode switch toggles between dark and light mode correctly.
    /// </summary>
    /// <remarks>
    /// The app starts in dark mode, so the first button visible is "Light Mode".
    /// Each toggle alternates the button label between "Dark Mode" and "Light Mode".
    /// </remarks>
    /// <param name="toggleCount">The number of times to click the toggle button.</param>
    /// <param name="expectedLabel">The button label expected after all toggles are complete.</param>
    [Theory]
    [InlineData(1, "Dark Mode")]
    [InlineData(2, "Light Mode")]
    public async Task Toolbar_DarkModeToggle_UpdatesButtonLabel(int toggleCount, string expectedLabel)
    {
        string[] clickSequence = ["Light Mode", "Dark Mode"];
        string[] resultSequence = ["Dark Mode", "Light Mode"];

        for (int i = 0; i < toggleCount; i++)
        {
            await this.page.ClickAsync($"button:has-text(\"{clickSequence[i]}\")");

            await this.page.WaitForSelectorAsync(
                $"button:has-text(\"{resultSequence[i]}\")",
                new()
                {
                    State = WaitForSelectorState.Visible
                });
        }

        await Assertions.Expect(this.page.Locator($"button:has-text(\"{expectedLabel}\")")).ToBeVisibleAsync();
    }
}
