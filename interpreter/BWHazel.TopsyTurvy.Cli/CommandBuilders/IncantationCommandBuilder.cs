using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using BWHazel.TopsyTurvy.LanguageServer;

using OmniSharpServer = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>incantation</c> subcommand of <c>sorcerer</c>.
/// </summary>
public static class IncantationCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>incantation</c> subcommand.
    /// </summary>
    /// <remarks>
    /// The ---stdio flag is included for compatibility with language clients that automatically
    /// append it when using stdio transport, such as vscode-languageclient. It is accepted but
    /// silently ignored by the command handler.
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

        incantationCommand.Options.Add(stdioOption);
        incantationCommand.SetAction(_ => HandleIncantationAsync());

        return incantationCommand;
    }

    /// <summary>
    /// Handles execution of the <c>incantation</c> subcommand by starting the LSP server.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the language server exits.</returns>
    private static async Task HandleIncantationAsync()
    {
        ILanguageServer server = await OmniSharpServer.From(options =>
            options
                .WithInput(System.Console.OpenStandardInput())
                .WithOutput(System.Console.OpenStandardOutput())
                .WithServices(services => services.AddSingleton<DocumentStateManager>())
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
    }
}
