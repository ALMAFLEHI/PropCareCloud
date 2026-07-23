using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PropCareCloud.Api.Configuration;
using PropCareCloud.Api.DTOs.Notifications;

namespace PropCareCloud.Api.Services;

public interface ITask2NotificationPublisher
{
    bool IsConfigured { get; }
    Task<NotificationDispatchResult> PublishAsync(
        NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default);
}

public sealed class Task2NotificationPublisher(
    HttpClient httpClient,
    IOptions<Task2NotificationOptions> options,
    ILogger<Task2NotificationPublisher> logger) : ITask2NotificationPublisher
{
    private readonly Task2NotificationOptions notificationOptions = options.Value;

    public bool IsConfigured => notificationOptions.IsConfigured;

    public async Task<NotificationDispatchResult> PublishAsync(
        NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!notificationEvent.IsValid())
        {
            return NotificationDispatchResult.Failed(
                "Update saved, but notification delivery is temporarily unavailable.");
        }

        if (!notificationOptions.IsConfigured)
        {
            logger.LogWarning(
                "Task 2 notification service is not configured for event {EventId}.",
                notificationEvent.EventId);
            return NotificationDispatchResult.Failed(
                "Update saved, but notification delivery is temporarily unavailable.");
        }

        try
        {
            var baseUrl = notificationOptions.ApiBaseUrl.TrimEnd('/');
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl}/notifications/publish")
            {
                Content = JsonContent.Create(notificationEvent)
            };
            request.Headers.Add("x-api-key", notificationOptions.ApiKey);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(
                Math.Clamp(notificationOptions.TimeoutSeconds, 1, 10)));

            using var response = await httpClient.SendAsync(
                request,
                timeoutSource.Token);
            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                return NotificationDispatchResult.Queued();
            }

            logger.LogWarning(
                "Task 2 notification service returned HTTP {StatusCode} for event {EventId}.",
                (int)response.StatusCode,
                notificationEvent.EventId);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "Task 2 notification service timed out for event {EventId}.",
                notificationEvent.EventId);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Task 2 notification service was unavailable for event {EventId}.",
                notificationEvent.EventId);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Task 2 notification dispatch failed safely for event {EventId}.",
                notificationEvent.EventId);
        }

        return NotificationDispatchResult.Failed(
            "Update saved, but notification delivery is temporarily unavailable.");
    }
}

public sealed record NotificationDispatchResult(
    bool NotificationQueued,
    string NotificationMessage)
{
    public static NotificationDispatchResult Queued() =>
        new(true, "Notification queued successfully.");

    public static NotificationDispatchResult Failed(string message) =>
        new(false, message);
}
