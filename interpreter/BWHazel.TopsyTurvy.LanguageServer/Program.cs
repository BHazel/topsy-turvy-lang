using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using BWHazel.TopsyTurvy.LanguageServer;

var server = await LanguageServer.From(options =>
    options
        .WithInput(Console.OpenStandardInput())
        .WithOutput(Console.OpenStandardOutput())
        .WithServices(services => services.AddSingleton<DocumentStateManager>())
        .WithHandler<TextDocumentSyncHandler>()
        .WithHandler<HoverHandler>()
        .WithHandler<DefinitionHandler>()
        .WithHandler<CompletionHandler>()
        .WithHandler<SemanticTokensHandler>()
        .WithHandler<RenameHandler>()
        .ConfigureLogging(logging =>
            logging
                .ClearProviders()
                .AddLanguageProtocolLogging()
                .SetMinimumLevel(LogLevel.Warning))
);

await server.WaitForExit;
