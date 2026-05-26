using System;
using System.CommandLine;
using Spectre.Console;
using BWHazel.TopsyTurvy.Cli;
using BWHazel.TopsyTurvy.Cli.CommandBuilders;

string rootDescription = $"{TopsyTurvyBranding.Title}\n\nTopsy Turvy - A Gilbert & Sullivan Programming Language";
RootCommand rootCommand = new RootCommand(rootDescription)
{
    PerformCommandBuilder.Build()
};

return await rootCommand.Parse(args).InvokeAsync();
