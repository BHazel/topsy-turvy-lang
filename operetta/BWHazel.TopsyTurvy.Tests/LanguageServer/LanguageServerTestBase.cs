using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.LanguageServer;
using BWHazel.TopsyTurvy.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace BWHazel.TopsyTurvy.Tests.LanguageServer;

/// <summary>
/// Base class for Language Server handler tests.
/// </summary>
public abstract class LanguageServerTestBase
{
    /// <summary>
    /// The URI used as the primary test document across handler tests.
    /// </summary>
    protected readonly DocumentUri testUri = DocumentUri.From("file:///test.topsy");

    /// <summary>
    /// The parser used to produce parse results for test setup.
    /// </summary>
    protected readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Creates a <see cref="DocumentStateManager"/> with a single document containing the provided source code.
    /// </summary>
    /// <param name="source">The source code to include in the document.</param>
    /// <param name="externalFunctions">The catalogue of external functions to seed the symbol table with, or <c>null</c> to fall back to <see cref="BindingCatalogue.Default"/> via <c>ExternalFunctionRegistrar.Register</c>.</param>
    /// <returns>A <see cref="DocumentStateManager"/> containing the document with the provided source code.</returns>
    protected DocumentStateManager CreateManagerWithSource(string source, BindingCatalogue? externalFunctions = null)
    {
        DocumentStateManager manager = new();
        manager.Update(this.testUri, source, this.parser.TryParse(source), externalFunctions);
        return manager;
    }
}
