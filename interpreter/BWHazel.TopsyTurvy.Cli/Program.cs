using System.CommandLine;
using BWHazel.TopsyTurvy.Cli;
using BWHazel.TopsyTurvy.Cli.CommandBuilders;

string rootDescription = $"{TopsyTurvyBranding.Title}\n\nTopsy Turvy - A Gilbert & Sullivan Programming Language";
RootCommand rootCommand = new(rootDescription)
{
    PedigreeCommandBuilder.Build(),
    MountCommandBuilder.Build(),
    CommissionCommandBuilder.Build(),
    RehearseCommandBuilder.Build(),
    PerformCommandBuilder.Build(),
    PlaybillCommandBuilder.Build(),
    SorcererCommandBuilder.Build()
};

return await rootCommand.Parse(args).InvokeAsync();
