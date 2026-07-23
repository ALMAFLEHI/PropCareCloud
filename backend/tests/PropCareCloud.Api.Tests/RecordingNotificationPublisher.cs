using PropCareCloud.Api.DTOs.Notifications;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

internal sealed class RecordingNotificationPublisher :
    ITask2NotificationPublisher,
    IUserNotificationService
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

    public Task<NotificationDispatchResult> StoreAndPublishAsync(
        NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default) =>
        PublishAsync(notificationEvent, cancellationToken);

    public Task<List<UserNotificationResponse>> GetAsync(
        int limit,
        bool unreadOnly,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new List<UserNotificationResponse>());

    public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<UserNotificationResponse?> MarkReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<UserNotificationResponse?>(null);

    public Task<int> MarkAllReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}
