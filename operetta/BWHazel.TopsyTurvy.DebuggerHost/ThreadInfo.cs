namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// One entry in a <see cref="ThreadsResult"/>.
/// </summary>
/// <param name="Id">The thread ID. Always <see cref="DebugSessionConstants.SingleThreadId"/>.</param>
/// <param name="Name">A display name for the thread.</param>
internal sealed record ThreadInfo(long Id, string Name);
