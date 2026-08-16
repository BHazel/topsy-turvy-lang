using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// The response shape for the DAP <c>threads</c> request.
/// </summary>
/// <remarks>
/// Topsy Turvy debugging is single-threaded, so exactly one thread is always reported.
/// </remarks>
/// <param name="Threads">The list of threads; always exactly one entry.</param>
internal sealed record ThreadsResult(IReadOnlyList<ThreadInfo> Threads);
