using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.DebuggerHost;
using OmniSharp.Extensions.DebugAdapter.Protocol.Events;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="InitializeHandler"/> class.
/// </summary>
public class InitializeHandlerTests
{
    /// <summary>
    /// Tests that the <see cref="InitializeHandler.Handle"/> method advertises support for the DAP
    /// <c>configurationDone</c> request.
    /// </summary>
    [Fact]
    public async Task Handle_Always_AdvertisesConfigurationDoneSupport()
    {
        TestResponseRouter router = new();
        InitializeHandler handler = new(router);

        InitializeResponse response = await handler.Handle(new InitializeRequestArguments(), CancellationToken.None);

        response.SupportsConfigurationDoneRequest.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the <see cref="InitializeHandler.Handle"/> method eventually sends an <see cref="InitializedEvent"/>
    /// through the response router.
    /// </summary>
    [Fact]
    public async Task Handle_Always_EventuallySendsInitializedEvent()
    {
        TestResponseRouter router = new();
        InitializeHandler handler = new(router);

        await handler.Handle(new InitializeRequestArguments(), CancellationToken.None);
        await Task.Delay(200);

        router.Notifications.ShouldContain(notification => notification is InitializedEvent);
    }
}
