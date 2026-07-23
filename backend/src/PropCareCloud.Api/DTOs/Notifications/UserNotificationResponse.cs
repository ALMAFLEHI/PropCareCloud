namespace PropCareCloud.Api.DTOs.Notifications;

public sealed record UserNotificationResponse(
    Guid Id,
    Guid EventId,
    string EventType,
    Guid? MaintenanceRequestId,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public sealed record UnreadNotificationCountResponse(int UnreadCount);

public sealed record MarkAllNotificationsReadResponse(int ChangedCount);
