using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspSymbolKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Tests for the <see cref="DocumentSymbolHandler"/> class.
/// </summary>
public class DocumentSymbolHandlerTests : LanguageServerTestBase
{
    /// <summary>
    /// Tests that the <see cref="DocumentSymbolHandler.Handle"/> method returns an empty container when no document state is registered.
    /// </summary>
    [Fact]
    public async Task Handle_WithNoDocumentState_ReturnsEmptyContainer()
    {
        DocumentStateManager manager = new();
        DocumentSymbolHandler handler = new(manager);

        SymbolInformationOrDocumentSymbolContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the <see cref="DocumentSymbolHandler.Handle"/> method includes a declared variable in the Outline.
    /// </summary>
    [Fact]
    public async Task Handle_WithDeclaredVariable_IncludesVariableSymbol()
    {
        string source = """
            HARK! "Test"
            PRINCIPALS
              PRAY WELCOME greeting AS A YARN BEING "Hello"
            THE CURTAIN RISES.
            FINALE.
            """;
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        DocumentSymbolHandler handler = new(manager);

        SymbolInformationOrDocumentSymbolContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldContain(item => item.SymbolInformation!.Name == "greeting" && item.SymbolInformation.Kind == LspSymbolKind.Variable);
    }

    /// <summary>
    /// Tests that the <see cref="DocumentSymbolHandler.Handle"/> method includes the namespace declaration of a
    /// file in the Outline, mapped to the LSP <see cref="LspSymbolKind.Namespace"/> icon.
    /// </summary>
    [Fact]
    public async Task Handle_WithNamespaceDeclaration_IncludesNamespaceSymbol()
    {
        string source = "HARK! \"Test\"\nTOWN Accounts WITH DISTRICT Payroll\nFINALE.\n";
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        DocumentSymbolHandler handler = new(manager);

        SymbolInformationOrDocumentSymbolContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldContain(item => item.SymbolInformation!.Name == "Accounts*Payroll" && item.SymbolInformation.Kind == LspSymbolKind.Namespace);
    }

    /// <summary>
    /// Tests that the <see cref="DocumentSymbolHandler.Handle"/> method sets the <see cref="SymbolInformation.ContainerName"/>
    /// of a function to the namespace of its file, so editors that group by container name, such as the
    /// Outline panel or the breadcrumb bar, nest the function under its namespace.
    /// </summary>
    [Fact]
    public async Task Handle_WithFunctionInNamespacedFile_SetsContainerNameToNamespace()
    {
        string source = """
            HARK! "Test"
            TOWN Accounts
            PRINCIPALS
            THE CURTAIN RISES.
            IT IS MY DUTY TO PERFORM CalculateTax UNDER NO OBLIGATION
              BEHOLD "tax"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        DocumentSymbolHandler handler = new(manager);

        SymbolInformationOrDocumentSymbolContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        result.ShouldNotBeNull();
        SymbolInformation function = result.Single(item => item.SymbolInformation!.Name == "CalculateTax").SymbolInformation!;
        function.ContainerName.ShouldBe("Accounts");
    }

    /// <summary>
    /// Tests that the <see cref="DocumentSymbolHandler.Handle"/> method leaves <see cref="SymbolInformation.ContainerName"/>
    /// unset for a function in a file that declares no namespace.
    /// </summary>
    [Fact]
    public async Task Handle_WithFunctionInNonNamespacedFile_LeavesContainerNameNull()
    {
        string source = """
            HARK! "Test"
            IT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION
              BEHOLD "hello"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;
        DocumentStateManager manager = this.CreateManagerWithSource(source);
        DocumentSymbolHandler handler = new(manager);

        SymbolInformationOrDocumentSymbolContainer? result = await handler.Handle(this.MakeRequest(), CancellationToken.None);

        result.ShouldNotBeNull();
        SymbolInformation function = result.Single(item => item.SymbolInformation!.Name == "greet").SymbolInformation!;
        function.ContainerName.ShouldBeNull();
    }

    /// <summary>
    /// Creates a <see cref="DocumentSymbolParams"/> object for the test document.
    /// </summary>
    /// <returns>A <see cref="DocumentSymbolParams"/> object for the test document.</returns>
    private DocumentSymbolParams MakeRequest() =>
        new()
        {
            TextDocument = new()
            {
                Uri = this.testUri
            }
        };
}
