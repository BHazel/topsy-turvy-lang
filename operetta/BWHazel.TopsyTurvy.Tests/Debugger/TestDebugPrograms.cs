using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;

namespace BWHazel.TopsyTurvy.Tests.Debugger;

/// <summary>
/// Shared fixture source text, line-number constants and pause/end synchronisation helpers for
/// <see cref="DebugSession"/> tests.
/// </summary>
internal static class TestDebugPrograms
{
    /// <summary>
    /// The virtual file name used for <see cref="Simple"/> and <see cref="WithGlobalAndFunctionScope"/>.
    /// </summary>
    internal const string SimpleFileName = "simple.topsy";

    /// <summary>
    /// The line of the single assignment statement in <see cref="Simple"/>.
    /// </summary>
    internal const int SimpleAssignmentLine = 4;

    /// <summary>
    /// A minimal programme containing one declaration, one assignment and one print statement. Line 3 is blank,
    /// used to test unreachable-breakpoint handling.
    /// </summary>
    internal const string Simple = """
        HARK! "Simple"
        PRAY WELCOME result AS A PEER BEING 0

        result IS APPOINTED 42
        BEHOLD result
        FINALE.
        """;

    /// <summary>
    /// The line of the local declaration inside the body of <c>AddOne</c> in <see cref="WithGlobalAndFunctionScope"/>.
    /// </summary>
    internal const int FunctionLocalDeclarationLine = 6;

    /// <summary>
    /// A programme with a global <c>PRINCIPALS</c> declaration, a one-parameter function with its own local
    /// declaration and a call to that function from the top level.
    /// </summary>
    /// <remarks>
    /// Used to exercise call-stack, variable-scope and stepping behaviour together.
    /// </remarks>
    internal const string WithGlobalAndFunctionScope = """
        HARK! "Fixture"
        PRINCIPALS
          PRAY WELCOME globalValue AS A PEER BEING 100
        THE CURTAIN RISES.
        IT IS MY DUTY TO PERFORM AddOne UNDER THE TERMS OF n AS A PEER TO FIND PEER
          PRAY WELCOME localValue AS A PEER BEING SUM OF n AND 1
          AND SO I FIND localValue
        MY DUTY IS DISCHARGED.
        PRAY WELCOME result AS A PEER BEING 0

        result IS APPOINTED SUMMON AddOne WITH 5 IF YOU PLEASE.
        BEHOLD result
        FINALE.
        """;

    /// <summary>
    /// A resolver mapping <see cref="SimpleFileName"/> and <see cref="FixtureFileName"/> to their fixture source text.
    /// </summary>
    internal static Func<string, string?> Resolver { get; } = CreateResolver();

    /// <summary>
    /// The virtual file name used for <see cref="WithGlobalAndFunctionScope"/>.
    /// </summary>
    internal const string FixtureFileName = "fixture.topsy";

    /// <summary>
    /// Subscribes to the next <see cref="DebugSession.Paused"/> event before returning.
    /// </summary>
    /// <remarks>
    /// Call this before triggering the action expected to cause the pause, for example <see cref="DebugSession.Continue"/>,
    /// then await its result. Triggering the action first and only then subscribing risks missing the pause: a
    /// fast-pausing action, such as a loop with no breakpoint hit until <see cref="DebugSession.Pause"/> is called,
    /// can fire and complete before the subscription is even in place.
    /// </remarks>
    /// <param name="session">The session to observe.</param>
    /// <returns>A task that completes with the event data of the pause, or fails after a short timeout instead of hanging.</returns>
    internal static Task<PausedEventArgs> PreparePausedWaitAsync(DebugSession session)
    {
        TaskCompletionSource<PausedEventArgs> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object? sender, PausedEventArgs eventArgs)
        {
            completionSource.TrySetResult(eventArgs);
            session.Paused -= Handler;
        }

        session.Paused += Handler;
        return completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Subscribes to the <see cref="DebugSession.Ended"/> event before returning.
    /// </summary>
    /// <remarks>
    /// Call this before letting the session run to completion for the same reason as <see cref="PreparePausedWaitAsync"/>:
    /// a short fixture can finish, especially under parallel test execution, before the caller would otherwise
    /// have subscribed.
    /// </remarks>
    /// <param name="session">The session to observe.</param>
    /// <returns>A task that completes with the session-ended event data, or fails after a short timeout instead of hanging.</returns>
    internal static Task<EndedEventArgs> PrepareEndedWaitAsync(DebugSession session)
    {
        TaskCompletionSource<EndedEventArgs> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object? sender, EndedEventArgs eventArgs)
        {
            completionSource.TrySetResult(eventArgs);
            session.Ended -= Handler;
        }

        session.Ended += Handler;
        return completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Creates a resolver mapping <see cref="SimpleFileName"/> and <see cref="FixtureFileName"/> to their fixture source text.
    /// </summary>
    /// <returns>A function that resolves file names to their fixture source text.</returns>
    private static Func<string, string?> CreateResolver()
    {
        Dictionary<string, string> files = new()
        {
            [SimpleFileName] = Simple,
            [FixtureFileName] = WithGlobalAndFunctionScope
        };

        return name => files.GetValueOrDefault(name);
    }
}
