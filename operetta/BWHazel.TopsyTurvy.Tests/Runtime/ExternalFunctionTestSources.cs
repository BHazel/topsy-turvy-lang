namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// Shared <c>.topsy</c> source fixtures for external function resolution against <see cref="TestExternalFunctionBindingClass"/>,
/// consumed by both the interpreter and type checker test suites to prove the two layers agree.
/// </summary>
internal static class ExternalFunctionTestSources
{
    /// <summary>
    /// A user-defined global function sharing a name with an external function; the user function must always win.
    /// </summary>
    internal const string Shadowing = """
        HARK! "Shadowing"
        PRINCIPALS
        THE CURTAIN RISES.
        IT IS MY DUTY TO PERFORM TestWrite UNDER THE TERMS OF text AS A YARN
          BEHOLD "shadowed"
        MY DUTY IS DISCHARGED.
        SUMMON TestWrite WITH "Hello" IF YOU PLEASE.
        FINALE.
        """;
}
