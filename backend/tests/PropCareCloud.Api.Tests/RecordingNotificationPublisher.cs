using PropCareCloud.Api.DTOs.Notifications;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

internal sealed class RecordingNotificationPublisher : ITask2NotificationPublisher
{
    public bool IsConfigured => true;
    public List<NotificationEvent> PublishedEvents { get; } = [];
    public NotificationDispatchResult Result { get; set; } =
        NotificationDispatchResult.Queued();

    public Task<NotificationDispatchResult> PublishAsync(
        NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        PublishedEvents.Add(notificationEvent);
        return Task.FromResult(Result);
    }
}
