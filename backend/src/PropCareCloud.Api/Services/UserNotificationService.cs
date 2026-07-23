using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.DTOs.Notifications;

namespace PropCareCloud.Api.Services;

public interface IUserNotificationService
{
    Task<NotificationDispatchResult> StoreAndPublishAsync(
        NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default);
    Task<List<UserNotificationResponse>> GetAsync(
        int limit,
        bool unreadOnly,
        CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task<UserNotificationResponse?> MarkReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(CancellationToken cancellationToken = default);
}

public sealed class UserNotificationService(
    AppDbContext dbContext,
    ICurrentUserService currentUser,
    ITask2NotificationPublisher notificationPublisher,
    ILogger<UserNotificationService> logger) : IUserNotificationService
{
    public async Task<NotificationDispatchResult> StoreAndPublishAsync(
        NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        var inboxStored = await TryStoreAsync(notificationEvent, cancellationToken);
        var dispatch = await notificationPublisher.PublishAsync(
            notificationEvent,
            cancellationToken);

        logger.LogInformation(
            "Notification orchestration completed for correlation {CorrelationId}, type {EventType}, persistence {PersistenceStatus}, publish {PublishStatus}.",
            notificationEvent.CorrelationId,
            notificationEvent.EventType,
            inboxStored ? "Stored" : "Unavailable",
            dispatch.NotificationQueued ? "Queued" : "Unavailable");

        if (!inboxStored && dispatch.NotificationQueued)
        {
            return new NotificationDispatchResult(
                true,
                "Notification queued, but inbox storage is temporarily unavailable.");
        }

        return dispatch;
    }

    public async Task<List<UserNotificationResponse>> GetAsync(
        int limit,
        bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserProfileId is not { } userProfileId)
        {
            return [];
        }

        var query = dbContext.UserNotifications
            .AsNoTracking()
            .Where(notification => notification.UserProfileId == userProfileId);

        if (unreadOnly)
        {
            query = query.Where(notification => !notification.IsRead);
        }

        return await Project(query
                .OrderByDescending(notification => notification.CreatedAtUtc)
                .Take(limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserProfileId is not { } userProfileId)
        {
            return 0;
        }

        return await dbContext.UserNotifications.CountAsync(
            notification =>
                notification.UserProfileId == userProfileId &&
                !notification.IsRead,
            cancellationToken);
    }

    public async Task<UserNotificationResponse?> MarkReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserProfileId is not { } userProfileId)
        {
            return null;
        }

        var notification = await dbContext.UserNotifications.SingleOrDefaultAsync(
            item => item.Id == notificationId && item.UserProfileId == userProfileId,
            cancellationToken);
        if (notification is null)
        {
            return null;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToResponse(notification);
    }

    public async Task<int> MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserProfileId is not { } userProfileId)
        {
            return 0;
        }

        var unread = await dbContext.UserNotifications
            .Where(notification =>
                notification.UserProfileId == userProfileId &&
                !notification.IsRead)
            .ToListAsync(cancellationToken);
        if (unread.Count == 0)
        {
            return 0;
        }

        var readAtUtc = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = readAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    private async Task<bool> TryStoreAsync(
        NotificationEvent notificationEvent,
        CancellationToken cancellationToken)
    {
        var recipientIds = notificationEvent.TargetProfileIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (notificationEvent.EventType != NotificationEventTypes.MaintenanceRequestCreated &&
            notificationEvent.ActorUserId is { } actorUserId)
        {
            recipientIds = recipientIds.Where(id => id != actorUserId).ToArray();
        }

        if (recipientIds.Length == 0)
        {
            return true;
        }

        try
        {
            var activeRecipientIds = await dbContext.UserProfiles
                .AsNoTracking()
                .Where(user => recipientIds.Contains(user.Id) && user.IsActive)
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);
            var existingRecipientIds = await dbContext.UserNotifications
                .AsNoTracking()
                .Where(notification =>
                    notification.EventId == notificationEvent.EventId &&
                    activeRecipientIds.Contains(notification.UserProfileId))
                .Select(notification => notification.UserProfileId)
                .ToListAsync(cancellationToken);

            var rows = activeRecipientIds
                .Except(existingRecipientIds)
                .Select(recipientId => new UserNotification
                {
                    UserProfileId = recipientId,
                    EventId = notificationEvent.EventId,
                    CorrelationId = notificationEvent.CorrelationId,
                    EventType = notificationEvent.EventType,
                    MaintenanceRequestId = notificationEvent.MaintenanceRequestId,
                    Title = notificationEvent.Title,
                    Message = notificationEvent.Message,
                    CreatedAtUtc = notificationEvent.OccurredAtUtc
                })
                .ToArray();

            if (rows.Length > 0)
            {
                dbContext.UserNotifications.AddRange(rows);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation(
                "Stored {RecipientCount} inbox notifications for event {EventId}, correlation {CorrelationId}, type {EventType}.",
                rows.Length,
                notificationEvent.EventId,
                notificationEvent.CorrelationId,
                notificationEvent.EventType);
            return true;
        }
        catch (Exception exception)
        {
            try
            {
                foreach (var entry in dbContext.ChangeTracker
                             .Entries<UserNotification>()
                             .Where(entry => entry.State == EntityState.Added))
                {
                    entry.State = EntityState.Detached;
                }
            }
            catch (ObjectDisposedException)
            {
                // The dispatch still proceeds when the scoped context is unavailable.
            }

            logger.LogWarning(
                exception,
                "Inbox storage failed safely for event {EventId}, correlation {CorrelationId}, type {EventType}.",
                notificationEvent.EventId,
                notificationEvent.CorrelationId,
                notificationEvent.EventType);
            return false;
        }
    }

    private static IQueryable<UserNotificationResponse> Project(
        IQueryable<UserNotification> query) =>
        query.Select(notification => new UserNotificationResponse(
            notification.Id,
            notification.EventId,
            notification.EventType,
            notification.MaintenanceRequestId,
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.CreatedAtUtc,
            notification.ReadAtUtc));

    private static UserNotificationResponse ToResponse(UserNotification notification) =>
        new(
            notification.Id,
            notification.EventId,
            notification.EventType,
            notification.MaintenanceRequestId,
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.CreatedAtUtc,
            notification.ReadAtUtc);
}
