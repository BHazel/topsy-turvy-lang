using System;
using System.IO;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.Runtime;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: topsyturvy <file.topsy>");
    return 1;
}

string path = args[0];
if (!File.Exists(path))
{
    Console.Error.WriteLine($"File not found: {path}");
    return 1;
}

string source = File.ReadAllText(path);

TopsyTurvyParser parser = new();
ProgramNode program = parser.Parse(source);

Interpreter interpreter = new(new ConsoleIO());
DiagnosticCollection diagnostics = interpreter.Execute(program);

if (diagnostics.HasErrors)
{
    foreach (Diagnostic diagnostic in diagnostics.Diagnostics)
    {
        Console.Error.WriteLine($"[{diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}] {diagnostic.Message}");
    }

    return 1;
}

return 0;
