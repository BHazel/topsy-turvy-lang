using System;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using BWHazel.TopsyTurvy.LanguageServer;

var server = await LanguageServer.From(options =>
    options
        .WithInput(Console.OpenStandardInput())
        .WithOutput(Console.OpenStandardOutput())
        .WithHandler<TextDocumentSyncHandler>()
        .ConfigureLogging(logging =>
            logging
                .ClearProviders()
                .AddLanguageProtocolLogging()
                .SetMinimumLevel(LogLevel.Warning))
);

await server.WaitForExit;
