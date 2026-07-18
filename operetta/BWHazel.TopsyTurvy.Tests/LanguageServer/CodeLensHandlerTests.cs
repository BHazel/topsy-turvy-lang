using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.LanguageServer;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="CodeLensHandler"/> class.
/// </summary>
public class CodeLensHandlerTests : LanguageServerTestBase
{
    /// <summary>
    /// Tests that the <see cref="CodeLensHandler.Handle(CodeLensParams, CancellationToken)"/> method returns an empty
    /// container when no document state is registered.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocumentState_ReturnsEmptyContainer()
    {
        DocumentStateManager manager = new();
        CodeLensHandler handler = new(manager);

        CodeLensContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="CodeLensHandler.Handle(CodeLensParams, CancellationToken)"/> method reports the
    /// correct reference count for a function called once elsewhere in the same document.
    /// </summary>
    [Fact]
    public async Task Handle_WithFunctionCalledOnce_ReportsOneReference()
    {
        string source = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
              BEHOLD "hello"
            MY DUTY IS DISCHARGED.
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON greet WITH NOTHING IF YOU PLEASE.
            FINALE.
            """;

        DocumentStateManager manager = this.CreateManagerWithSource(source);
        CodeLensHandler handler = new(manager);

        CodeLensContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        CodeLens lens = result!.Single(item => item.Range.Start.Line == 1);
        lens.Command!.Title.ShouldBe("1 reference");
    }

    /// <summary>
    /// Tests that clicking a <see cref="CodeLensHandler"/> annotation for a function produces a position that, when
    /// fed into <see cref="ReferencesHandler"/> exactly as the VS Code client would via the
    /// <c>topsy-turvy.showReferences</c> command, actually resolves references to the function itself rather than
    /// landing on the declaring keyword and finding nothing.
    /// </summary>
    [Fact]
    public async Task Handle_ClickThroughPositionFedIntoReferencesHandler_ResolvesFunctionReferences()
    {
        string source = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
              BEHOLD "hello"
            MY DUTY IS DISCHARGED.
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON greet WITH NOTHING IF YOU PLEASE.
            FINALE.
            """;

        DocumentStateManager manager = this.CreateManagerWithSource(source);
        CodeLensHandler codeLensHandler = new(manager);
        ReferencesHandler referencesHandler = new(manager);

        CodeLensContainer? codeLenses = await codeLensHandler.Handle(this.MakeRequest(), CancellationToken.None);
        CodeLens greetLens = codeLenses!.Single(item => item.Range.Start.Line == 1);

        JToken[] arguments = [.. greetLens.Command!.Arguments!];
        int clickLine = arguments[1].Value<int>();
        int clickCharacter = arguments[2].Value<int>();

        LocationContainer? references = await referencesHandler.Handle(
            new ReferenceParams()
            {
                TextDocument = new() { Uri = this.testUri },
                Position = new(clickLine, clickCharacter),
                Context = new() { IncludeDeclaration = true }
            },
            CancellationToken.None);

        references.ShouldNotBeNull();
        references.ShouldNotBeEmpty();
        references!.Count().ShouldBe(2);
    }

    /// <summary>
    /// Tests that the <see cref="CodeLensHandler.Handle(CodeLensParams, CancellationToken)"/> method excludes a
    /// same-named occurrence in another open document that has no <c>PRAY ADMIT</c> connection to the current
    /// document, so the reference count is not inflated by an unrelated file.
    /// </summary>
    [Fact]
    public async Task Handle_WithSameNameInUnrelatedDocument_DoesNotCountUnrelatedDocument()
    {
        string source = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
              BEHOLD "hello"
            MY DUTY IS DISCHARGED.
            PRINCIPALS
            THE CURTAIN RISES.
            SUMMON greet WITH NOTHING IF YOU PLEASE.
            FINALE.
            """;
        string unrelatedSource = "HARK! \"Unrelated\"\nIT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION\n  BEHOLD \"hi\"\nMY DUTY IS DISCHARGED.\nSUMMON greet WITH NOTHING IF YOU PLEASE.\nFINALE.\n";

        DocumentStateManager manager = this.CreateManagerWithSource(source);
        manager.Update(DocumentUri.From("file:///unrelated.topsy"), unrelatedSource, this.parser.TryParse(unrelatedSource));
        CodeLensHandler handler = new(manager);

        CodeLensContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        CodeLens lens = result!.Single(item => item.Range.Start.Line == 1);
        lens.Command!.Title.ShouldBe("1 reference");
    }

    /// <summary>
    /// Tests that the <see cref="CodeLensHandler.Handle(CodeLensParams, CancellationToken)"/> method does not
    /// produce an annotation for the namespace declaration of a document.
    /// </summary>
    /// <remarks>
    /// The synthesised <c>*</c>-joined display name of a namespace does not appear verbatim in source when the
    /// long-form <c>WITH DISTRICT</c> syntax is used elsewhere, so annotating it would under-count; excluded
    /// entirely rather than showing a misleading count.
    /// </remarks>
    [Fact]
    public async Task Handle_WithNamespaceDeclaration_DoesNotProduceAnnotation()
    {
        string source = "HARK! \"Test\"\nTOWN Accounts\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        CodeLensHandler handler = new(manager);

        CodeLensContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldNotContain(item => item.Range.Start.Line == 1);
    }

    /// <summary>
    /// Creates a <see cref="CodeLensParams"/> object for the test document.
    /// </summary>
    /// <returns>A <see cref="CodeLensParams"/> object for the test document.</returns>
    private CodeLensParams MakeRequest() =>
        new()
        {
            TextDocument = new()
            {
                Uri = this.testUri
            }
        };
}
