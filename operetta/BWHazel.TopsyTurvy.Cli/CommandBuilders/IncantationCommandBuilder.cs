using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.LanguageServer;

using OmniSharpServer = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>incantation</c> command.
/// </summary>
public static class IncantationCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>incantation</c> command.
    /// </summary>
    /// <remarks>
    /// The --stdio flag is included for compatibility with language clients that automatically
    /// append it when using stdio transport, such as vscode-languageclient. It is accepted but
    /// silently ignored by the command handler.
    /// </remarks>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command incantationCommand = new("incantation", "Starts the Topsy Turvy LSP language server over stdio (JSON-RPC).");
        incantationCommand.Aliases.Add("lsp");

        Option<bool> stdioOption = new("--stdio")
        {
            Description = "Accepted for language client compatibility for stdio transport.",
            Hidden = true,
        };

        Option<string[]> admitOption = new("--admit")
        {
            Description = "Loads an external .NET assembly (.dll) so its functions become callable via SUMMON.  Can be specified multiple times."
        };

        admitOption.Aliases.Add("--include");

        incantationCommand.Options.Add(stdioOption);
        incantationCommand.Options.Add(admitOption);
        incantationCommand.SetAction(parseResult => HandleIncantationAsync(parseResult.GetValue(admitOption) ?? []));

        return incantationCommand;
    }

    /// <summary>
    /// Handles execution of the <c>incantation</c> subcommand by starting the LSP server.
    /// </summary>
    /// <param name="externalLibraryAssemblyPaths">The paths to external library assemblies to admit, or empty for none.</param>
    /// <returns>An integer exit code with 0 for a normal server exit or 1 if an admitted external library failed to load.</returns>
    /// <remarks>
    /// A load failure is written as a single line to <see cref="System.Console.Error"/> rather than through
    /// <see cref="PanelHelper"/>, since this command binds JSON-RPC to standard output and a styled panel would
    /// corrupt that stream. The failure is also reported before the language server starts, so no partial server
    /// state is left behind.
    /// </remarks>
    private static async Task<int> HandleIncantationAsync(string[] externalLibraryAssemblyPaths)
    {
        ExternalLibraryLoadResult loadResult = ExternalLibraryLoader.Load(externalLibraryAssemblyPaths);
        if (loadResult.ErrorMessage is not null)
        {
            await System.Console.Error.WriteLineAsync(loadResult.ErrorMessage);
            return 1;
        }

        BindingCatalogue externalFunctions = loadResult.Catalogue!;

        ILanguageServer server = await OmniSharpServer.From(options =>
            options
                .WithInput(System.Console.OpenStandardInput())
                .WithOutput(System.Console.OpenStandardOutput())
                .WithServices(services => services
                    .AddSingleton<DocumentStateManager>()
                    .AddSingleton(externalFunctions))
                .WithHandler<TextDocumentSyncHandler>()
                .WithHandler<HoverHandler>()
                .WithHandler<DefinitionHandler>()
                .WithHandler<CompletionHandler>()
                .WithHandler<SemanticTokensHandler>()
                .WithHandler<RenameHandler>()
                .WithHandler<PrepareRenameHandler>()
                .WithHandler<DocumentSymbolHandler>()
                .WithHandler<WorkspaceSymbolHandler>()
                .WithHandler<ReferencesHandler>()
                .WithHandler<SignatureHelpHandler>()
                .WithHandler<FoldingRangeHandler>()
                .WithHandler<DocumentFormattingHandler>()
                .WithHandler<CodeLensHandler>()
                .ConfigureLogging(logging =>
                    logging
                        .ClearProviders()
                        .AddLanguageProtocolLogging()
                        .SetMinimumLevel(LogLevel.Warning)));

        await server.WaitForExit;
        return 0;
    }
}
