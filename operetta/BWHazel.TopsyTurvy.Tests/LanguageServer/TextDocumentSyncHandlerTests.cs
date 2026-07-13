using System;
using System.IO;
using BWHazel.TopsyTurvy.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="TextDocumentSyncHandler"/> class.
/// </summary>
public class TextDocumentSyncHandlerTests : LanguageServerTestBase
{
    /// <summary>
    /// Tests that the <see cref="TextDocumentSyncHandler.CreateFileResolver"/> method resolves an import against
    /// the live buffer of an already-open document rather than reading from disk.
    /// </summary>
    [Fact]
    public void CreateFileResolver_WithImportOpenInEditor_ReturnsLiveBufferContent()
    {
        string tempDirectory = Directory.CreateTempSubdirectory("topsyturvy-lsp-tests-").FullName;
        try
        {
            string mainPath = Path.Combine(tempDirectory, "main.topsy");
            string importedPath = Path.Combine(tempDirectory, "mathematical.topsy");
            File.WriteAllText(importedPath, "HARK! \"On disk, stale\"\nFINALE.\n");

            DocumentUri mainUri = DocumentUri.FromFileSystemPath(mainPath);
            DocumentUri importedUri = DocumentUri.FromFileSystemPath(importedPath);
            const string liveUnsavedContent = "HARK! \"Live unsaved buffer\"\nFINALE.\n";

            DocumentStateManager manager = new();
            manager.Update(importedUri, liveUnsavedContent, this.parser.TryParse(liveUnsavedContent));

            TextDocumentSyncHandler handler = new(languageServer: null!, manager);
            Func<string, string?> resolver = handler.CreateFileResolver(mainUri);

            string? resolved = resolver("mathematical.topsy");

            resolved.ShouldBe(liveUnsavedContent);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Tests that the <see cref="TextDocumentSyncHandler.CreateFileResolver"/> method falls back to reading the
    /// real file system when the imported file is not currently open in the editor.
    /// </summary>
    [Fact]
    public void CreateFileResolver_WithImportNotOpenInEditor_FallsBackToFileSystem()
    {
        string tempDirectory = Directory.CreateTempSubdirectory("topsyturvy-lsp-tests-").FullName;
        try
        {
            string mainPath = Path.Combine(tempDirectory, "main.topsy");
            string importedPath = Path.Combine(tempDirectory, "mathematical.topsy");
            const string onDiskContent = "HARK! \"On disk\"\nFINALE.\n";
            File.WriteAllText(importedPath, onDiskContent);

            DocumentUri mainUri = DocumentUri.FromFileSystemPath(mainPath);
            DocumentStateManager manager = new();
            TextDocumentSyncHandler handler = new(languageServer: null!, manager);
            Func<string, string?> resolver = handler.CreateFileResolver(mainUri);

            string? resolved = resolver("mathematical.topsy");

            resolved.ShouldBe(onDiskContent);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Tests that the <see cref="TextDocumentSyncHandler.CreateFileResolver"/> method returns <c>null</c> for an
    /// import that neither has an open document nor exists on disk.
    /// </summary>
    [Fact]
    public void CreateFileResolver_WithUnresolvableImport_ReturnsNull()
    {
        DocumentUri mainUri = DocumentUri.FromFileSystemPath("/nonexistent/directory/main.topsy");
        DocumentStateManager manager = new();
        TextDocumentSyncHandler handler = new(languageServer: null!, manager);
        Func<string, string?> resolver = handler.CreateFileResolver(mainUri);

        string? resolved = resolver("missing.topsy");

        resolved.ShouldBeNull();
    }
}
