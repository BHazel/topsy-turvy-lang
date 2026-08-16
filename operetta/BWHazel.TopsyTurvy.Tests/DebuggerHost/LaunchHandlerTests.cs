using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="LaunchHandler"/> class.
/// </summary>
public class LaunchHandlerTests
{
    /// <summary>
    /// Tests that the <see cref="LaunchHandler.Handle"/> method starts the session on the file named by the
    /// <c>program</c> extension field, leaving it paused before the first statement.
    /// </summary>
    [Fact]
    public async Task Handle_WithProgramExtensionField_StartsSessionPausedBeforeFirstStatement()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        LaunchHandler handler = new(session, new HostCancellationToken(CancellationToken.None));

        LaunchRequestArguments request = new()
        {
            ExtensionData = new Dictionary<string, object>()
            {
                ["program"] = TestDebugPrograms.SimpleFileName
            }
        };

        await handler.Handle(request, CancellationToken.None);

        session.Status.ShouldBe(SessionStatus.Paused);
        session.SourceFilePath.ShouldBe(TestDebugPrograms.SimpleFileName);
    }
}
