using System.Collections.Generic;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Base class for <see cref="Interpreter"/> tests.
/// </summary>
public abstract class TopsyTurvyInterpreterTestBase
{
    /// <summary>
    /// The parser used to produce <see cref="Ast.ProgramNode"/> instances for test setup.
    /// </summary>
    protected readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Creates an <see cref="Interpreter"/> wired to a captured output list and an optional input queue.
    /// </summary>
    /// <param name="inputLines">Lines to enqueue as simulated standard input.</param>
    /// <returns>The configured interpreter and the list that will accumulate its output.</returns>
    protected (Interpreter Interpreter, List<string> Output) CreateInterpreter(params string[] inputLines)
    {
        List<string> output = [];
        Queue<string> input = new(inputLines);
        TestIO io = new(output, input);
        return (new(io), output);
    }

    /// <summary>
    /// Creates an <see cref="Interpreter"/> wired to a given external function catalogue, a captured output list, and an
    /// optional input queue.
    /// </summary>
    /// <param name="externalFunctions">The external function catalogue the interpreter resolves <c>SUMMON</c> targets against.</param>
    /// <param name="inputLines">Lines to enqueue as simulated standard input.</param>
    /// <returns>The configured interpreter and the list that will accumulate its output.</returns>
    protected (Interpreter Interpreter, List<string> Output) CreateInterpreter(BindingCatalogue externalFunctions, params string[] inputLines)
    {
        List<string> output = [];
        Queue<string> input = new(inputLines);
        TestIO io = new(output, input);
        return (new(io, externalFunctions), output);
    }
}
