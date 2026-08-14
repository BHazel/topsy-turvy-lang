using System;
using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// An observer that records every callback invocation, in order, for test assertions.
/// </summary>
internal sealed class TestExecutionObserver : IExecutionObserver
{
    /// <summary>
    /// Gets the callbacks recorded so far, in firing order.
    /// </summary>
    internal List<TestObserverEvent> Events { get; } = [];

    /// <inheritdoc/>
    public void OnBeforeStatement(Statement statement, TopsyTurvyEnvironment environment) =>
        this.Events.Add(new("Statement", statement.GetType().Name));

    /// <inheritdoc/>
    public void OnFunctionEnter(string functionName, SourceSpan callSite, TopsyTurvyEnvironment environment, bool isImportedFunction) =>
        this.Events.Add(new("Enter", $"{functionName}:{isImportedFunction}"));

    /// <inheritdoc/>
    public void OnFunctionExit(string functionName) =>
        this.Events.Add(new("Exit", functionName));

    /// <inheritdoc/>
    public void OnUnhandledError(Exception exception) =>
        this.Events.Add(new("Error", exception.GetType().Name));
}
