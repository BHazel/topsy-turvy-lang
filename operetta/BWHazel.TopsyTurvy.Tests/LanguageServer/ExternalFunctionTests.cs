using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.LanguageServer;
using BWHazel.TopsyTurvy.Tests.Runtime;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for external function (Standard Library and external library) support across the Language Server handlers.
/// </summary>
/// <remarks>
/// These tests resolve against <see cref="TestExternalFunctionBindingClass"/>, a test-only catalogue shared with
/// <c>Tests/Runtime</c> and <c>Tests/TypeChecker</c>, rather than the real Standard Library, so they stay valid
/// regardless of which functions the Standard Library ends up shipping.
/// </remarks>
public class ExternalFunctionTests : LanguageServerTestBase
{
    private readonly BindingCatalogue catalogue = BindingCatalogue.Create(typeof(TestExternalFunctionBindingClass));

    private readonly string sourceSummoningExternalFunction = """
        HARK! "External Function"
        PRINCIPALS
        THE CURTAIN RISES.
        SUMMON TestPreviewWrite WITH "Hello" IF YOU PLEASE.
        FINALE.
        """;

    /// <summary>
    /// Tests that hovering over a <c>SUMMON</c> call to an external function returns Markdown mentioning the
    /// preview note and its keyword analogue.
    /// </summary>
    [Fact]
    public async Task Hover_OverExternalFunctionCall_ReturnsPreviewAndKeywordAnalogueMarkdown()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceSummoningExternalFunction, this.catalogue);
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(
            new()
            {
                TextDocument = new() { Uri = this.testUri },
                Position = new(3, 8)
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.Contents.HasMarkupContent.ShouldBeTrue();
        string markdown = result.Contents.MarkupContent!.Value;
        markdown.ShouldContain("Preview");
        markdown.ShouldContain("BEHOLD");
    }

    /// <summary>
    /// Tests that completion includes an external function, with no source-level declaration.
    /// </summary>
    [Fact]
    public async Task Completion_WithNoDeclaration_IncludesExternalFunction()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceSummoningExternalFunction, this.catalogue);
        CompletionHandler handler = new(manager);

        CompletionList result = await handler.Handle(
            new CompletionParams()
            {
                TextDocument = new() { Uri = this.testUri },
                Position = new(0, 0)
            },
            CancellationToken.None);

        result.Items.ShouldContain(item => item.Label == "TestPreviewWrite");
    }

    /// <summary>
    /// Tests that signature help inside a <c>SUMMON</c> call to an external function shows its signature.
    /// </summary>
    [Fact]
    public async Task SignatureHelp_InsideExternalFunctionCall_ReturnsSignatureInfo()
    {
        // Seed the symbol table with external functions from a complete, valid programme first, then switch to an
        // in-progress (incomplete) SUMMON call for the cursor-position check: SignatureHelpHandler bails out once
        // "IF YOU PLEASE." has already been typed, matching how a user would actually see signature help while composing a call.
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceSummoningExternalFunction, this.catalogue);
        string incompleteSource = """
            HARK! "External Function"
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON TestPreviewWrite WITH "Hello"
            FINALE.
            """;

        manager.Update(this.testUri, incompleteSource, this.parser.TryParse(incompleteSource), this.catalogue);
        SignatureHelpHandler handler = new(manager);
        string[] lines = incompleteSource.Split('\n');
        int summonLine = System.Array.FindIndex(lines, line => line.TrimStart().StartsWith("SUMMON"));

        SignatureHelp? result = await handler.Handle(
            new()
            {
                TextDocument = new() { Uri = this.testUri },
                Position = new(summonLine, lines[summonLine].Length)
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.Signatures.ShouldNotBeEmpty();
        result.Signatures.First().Label.Split('(')[0].ShouldBe("TestPreviewWrite");
    }

    /// <summary>
    /// Tests that go-to-definition on an external function returns no location, since it has no source position.
    /// </summary>
    [Fact]
    public async Task Definition_OnExternalFunctionCall_ReturnsEmpty()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceSummoningExternalFunction, this.catalogue);
        DefinitionHandler handler = new(manager);

        LocationOrLocationLinks? result = await handler.Handle(
            new()
            {
                TextDocument = new() { Uri = this.testUri },
                Position = new(3, 8)
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that preparing a rename on an external function is refused, since it has no source position to rename.
    /// </summary>
    [Fact]
    public async Task PrepareRename_OnExternalFunctionCall_ReturnsNull()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceSummoningExternalFunction, this.catalogue);
        PrepareRenameHandler handler = new(manager);

        RangeOrPlaceholderRange? result = await handler.Handle(
            new()
            {
                TextDocument = new() { Uri = this.testUri },
                Position = new(3, 8)
            },
            CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that renaming an external function is refused, since it has no source position to rename.
    /// </summary>
    [Fact]
    public async Task Rename_OnExternalFunctionCall_ReturnsNull()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceSummoningExternalFunction, this.catalogue);
        RenameHandler handler = new(manager);

        WorkspaceEdit? result = await handler.Handle(
            new()
            {
                TextDocument = new() { Uri = this.testUri },
                Position = new(3, 8),
                NewName = "RenamedFunction"
            },
            CancellationToken.None);

        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that finding references to an external function still finds its call sites, since references are not
    /// tied to a definition position the way rename and go-to-definition are.
    /// </summary>
    [Fact]
    public async Task References_ToExternalFunctionCall_FindsCallSite()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceSummoningExternalFunction, this.catalogue);
        ReferencesHandler handler = new(manager);

        LocationContainer? result = await handler.Handle(
            new()
            {
                TextDocument = new() { Uri = this.testUri },
                Position = new(3, 8),
                Context = new() { IncludeDeclaration = true }
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Tests that CodeLens produces no annotation for an external function, since it has no source position to attach one to.
    /// </summary>
    [Fact]
    public async Task CodeLens_WithOnlyExternalFunctionCall_ReturnsNoAnnotations()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceSummoningExternalFunction, this.catalogue);
        CodeLensHandler handler = new(manager);

        CodeLensContainer? result = await handler.Handle(
            new CodeLensParams() { TextDocument = new() { Uri = this.testUri } },
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a user-defined global function sharing a name with an external function is recorded as shadowed,
    /// so a host can surface a warning diagnostic for it.
    /// </summary>
    [Fact]
    public void Update_WithUserFunctionShadowingExternalFunction_RecordsShadowedName()
    {
        string source = """
            HARK! "Shadowing"
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM TestPreviewWrite UNDER THE TERMS OF text AS A YARN
              BEHOLD "shadowed"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        DocumentStateManager manager = this.CreateManagerWithSource(source, this.catalogue);

        DocumentState? state = manager.Get(this.testUri);

        state.ShouldNotBeNull();
        state.ShadowedExternalFunctionNames.ShouldContain("TestPreviewWrite");
    }

    /// <summary>
    /// Tests that a document with no name collisions records no shadowed external functions.
    /// </summary>
    [Fact]
    public void Update_WithNoShadowing_RecordsNoShadowedNames()
    {
        DocumentStateManager manager = this.CreateManagerWithSource(this.sourceSummoningExternalFunction, this.catalogue);

        DocumentState? state = manager.Get(this.testUri);

        state.ShouldNotBeNull();
        state.ShadowedExternalFunctionNames.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a document with no explicit catalogue falls back to <see cref="BindingCatalogue.Default"/>, so the
    /// real Standard Library is reachable with the overload every existing call site already uses.
    /// </summary>
    [Fact]
    public async Task Hover_WithNoExplicitCatalogue_ResolvesAgainstBindingCatalogueDefault()
    {
        string source = """
            HARK! "Default Catalogue"
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON PreviewBehold WITH "Hello" AND VERITY IF YOU PLEASE.
            FINALE.
            """;
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        HoverHandler handler = new(manager);

        Hover? result = await handler.Handle(
            new()
            {
                TextDocument = new() { Uri = this.testUri },
                Position = new(3, 8)
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
    }
}
