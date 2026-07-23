using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PropCareCloud.Api.Configuration;
using PropCareCloud.Api.DTOs.Notifications;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class Task2NotificationPublisherTests
{
    [Fact]
    public async Task AcceptedResponseReturnsNotificationQueuedTrue()
    {
        var handler = new StubHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
        var publisher = CreatePublisher(handler);

        var result = await publisher.PublishAsync(CreateEvent());

        Assert.True(result.NotificationQueued);
        Assert.Equal("Notification queued successfully.", result.NotificationMessage);
        Assert.True(handler.ApiKeyHeaderPresent);
        Assert.Equal(
            "https://notification-api.example.test/prod/notifications/publish",
            handler.RequestUri);
    }

    [Fact]
    public async Task ApiFailureReturnsNonBreakingDispatchResult()
    {
        var handler = new StubHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var publisher = CreatePublisher(handler);

        var result = await publisher.PublishAsync(CreateEvent());

        Assert.False(result.NotificationQueued);
        Assert.Contains("Update saved", result.NotificationMessage);
    }

    [Fact]
    public async Task TimeoutReturnsNonBreakingDispatchResult()
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, request.GetCancellationToken());
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var publisher = CreatePublisher(handler, timeoutSeconds: 1);

        var result = await publisher.PublishAsync(CreateEvent());

        Assert.False(result.NotificationQueued);
        Assert.Contains("temporarily unavailable", result.NotificationMessage);
    }

    [Fact]
    public async Task InvalidEventIsRejectedBeforeHttpCall()
    {
        var handler = new StubHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
        var publisher = CreatePublisher(handler);
        var invalidEvent = CreateEvent() with { EventType = "NotAllowed" };

        var result = await publisher.PublishAsync(invalidEvent);

        Assert.False(result.NotificationQueued);
        Assert.Equal(0, handler.CallCount);
    }

    private static Task2NotificationPublisher CreatePublisher(
        StubHttpMessageHandler handler,
        int timeoutSeconds = 3) =>
        new(
            new HttpClient(handler),
            Options.Create(new Task2NotificationOptions
            {
                ApiBaseUrl = "https://notification-api.example.test/prod",
                ApiKey = "server-side-test-placeholder",
                TimeoutSeconds = timeoutSeconds
            }),
            NullLogger<Task2NotificationPublisher>.Instance);

    private static NotificationEvent CreateEvent() =>
        NotificationEvent.Create(
            NotificationEventTypes.MaintenanceRequestStatusChanged,
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationTargetRoles.Multiple,
            [Guid.NewGuid()],
            "Maintenance request status changed",
            "The status of a maintenance request was updated.");

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessageWithCancellation, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public bool ApiKeyHeaderPresent { get; private set; }
        public string? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            ApiKeyHeaderPresent = request.Headers.Contains("x-api-key");
            RequestUri = request.RequestUri?.ToString();
            return responseFactory(new HttpRequestMessageWithCancellation(cancellationToken));
        }
    }

    private sealed record HttpRequestMessageWithCancellation(
        CancellationToken CancellationToken)
    {
        public CancellationToken GetCancellationToken() => CancellationToken;
    }
}
