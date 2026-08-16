using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// A test response router that records every notification sent through it.
/// </summary>
public sealed class TestResponseRouter : IResponseRouter
{
    /// <summary>
    /// Gets the notifications sent through <see cref="SendNotification(IRequest)"/>, in order.
    /// </summary>
    public List<IRequest> Notifications { get; } = [];

    /// <inheritdoc/>
    public void SendNotification(IRequest notification) => this.Notifications.Add(notification);

    /// <inheritdoc/>
    public void SendNotification(string method) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void SendNotification<T>(string method, T @params) => throw new NotSupportedException();

    /// <inheritdoc/>
    public IResponseRouterReturns SendRequest(string method) => throw new NotSupportedException();

    /// <inheritdoc/>
    public IResponseRouterReturns SendRequest<T>(string method, T @params) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public bool TryGetRequest(long id, out string method, out TaskCompletionSource<JToken> pendingTask) =>
        throw new NotSupportedException();
}
