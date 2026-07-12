using System.Text.Json;
using BWHazel.TopsyTurvy.WebEditor.IO;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.IO;

/// <summary>
/// Tests for the <see cref="EditorPreferences"/> class.
/// </summary>
public class EditorPreferencesTests
{
    /// <summary>
    /// Tests that serialising and deserialising an <see cref="EditorPreferences"/> instance preserves every property.
    /// </summary>
    [Fact]
    public void Serialize_Deserialize_RoundTrip_PreservesAllProperties()
    {
        EditorPreferences preferences = new()
        {
            Files = [new VirtualFile("main.topsy", "HARK! \"Test\"\nFINALE.")],
            ActiveFile = "main.topsy",
            CommandLineArguments = "--flag",
            StandardInput = "input line",
            AreGsLabelsEnabled = true,
            IsDarkMode = false,
            TerminalOutput = "some output",
        };

        string json = JsonSerializer.Serialize(preferences);
        EditorPreferences? deserialized = JsonSerializer.Deserialize<EditorPreferences>(json);

        deserialized.ShouldNotBeNull();
        deserialized.Files.ShouldNotBeNull();
        deserialized.Files.Count.ShouldBe(1);
        deserialized.Files[0].Name.ShouldBe("main.topsy");
        deserialized.Files[0].Content.ShouldBe("HARK! \"Test\"\nFINALE.");
        deserialized.ActiveFile.ShouldBe("main.topsy");
        deserialized.CommandLineArguments.ShouldBe("--flag");
        deserialized.StandardInput.ShouldBe("input line");
        deserialized.AreGsLabelsEnabled.ShouldBe(true);
        deserialized.IsDarkMode.ShouldBe(false);
        deserialized.TerminalOutput.ShouldBe("some output");
    }

    /// <summary>
    /// Tests that serialising an <see cref="EditorPreferences"/> instance uses the configured camelCase JSON property names.
    /// </summary>
    [Fact]
    public void Serialize_UsesConfiguredCamelCaseJsonPropertyNames()
    {
        EditorPreferences preferences = new()
        {
            Files = [],
            ActiveFile = "main.topsy",
            CommandLineArguments = "args",
            StandardInput = "stdin",
            AreGsLabelsEnabled = true,
            IsDarkMode = true,
            TerminalOutput = "output",
        };

        string json = JsonSerializer.Serialize(preferences);

        json.ShouldContain("\"files\"");
        json.ShouldContain("\"activeFile\"");
        json.ShouldContain("\"commandLineArguments\"");
        json.ShouldContain("\"standardInput\"");
        json.ShouldContain("\"areGsLabelsEnabled\"");
        json.ShouldContain("\"isDarkMode\"");
        json.ShouldContain("\"terminalOutput\"");
    }

    /// <summary>
    /// Tests that deserialising a minimal JSON object leaves all optional properties <c>null</c>.
    /// </summary>
    [Fact]
    public void Deserialize_WithMissingOptionalProperties_LeavesThemNull()
    {
        EditorPreferences? deserialized = JsonSerializer.Deserialize<EditorPreferences>("{}");

        deserialized.ShouldNotBeNull();
        deserialized.Files.ShouldBeNull();
        deserialized.ActiveFile.ShouldBeNull();
        deserialized.CommandLineArguments.ShouldBeNull();
        deserialized.StandardInput.ShouldBeNull();
        deserialized.AreGsLabelsEnabled.ShouldBeNull();
        deserialized.IsDarkMode.ShouldBeNull();
        deserialized.TerminalOutput.ShouldBeNull();
    }
}
