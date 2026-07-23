namespace PropCareCloud.Api.DTOs.Notifications;

public static class NotificationEventTypes
{
    public const string MaintenanceRequestCreated = "MaintenanceRequestCreated";
    public const string MaintenanceRequestAssigned = "MaintenanceRequestAssigned";
    public const string MaintenanceRequestStatusChanged = "MaintenanceRequestStatusChanged";
    public const string AttachmentConfirmed = "AttachmentConfirmed";

    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(
        [
            MaintenanceRequestCreated,
            MaintenanceRequestAssigned,
            MaintenanceRequestStatusChanged,
            AttachmentConfirmed
        ],
        StringComparer.Ordinal);
}

public static class NotificationTargetRoles
{
    public const string Admin = "Admin";
    public const string PropertyManager = "PropertyManager";
    public const string Tenant = "Tenant";
    public const string MaintenanceStaff = "MaintenanceStaff";
    public const string Multiple = "Multiple";

    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(
        [Admin, PropertyManager, Tenant, MaintenanceStaff, Multiple],
        StringComparer.Ordinal);
}

public sealed record NotificationEvent(
    string SchemaVersion,
    Guid EventId,
    string EventType,
    DateTime OccurredAtUtc,
    Guid MaintenanceRequestId,
    Guid? ActorUserId,
    string TargetRole,
    IReadOnlyList<Guid> TargetProfileIds,
    string Title,
    string Message,
    Guid CorrelationId,
    string Source)
{
    public const string CurrentSchemaVersion = "1.0";
    public const string ApiSource = "PropCareCloud.Api";
    public const int MaximumTitleLength = 120;
    public const int MaximumMessageLength = 500;
    public const int MaximumTargetProfileIds = 20;

    public static NotificationEvent Create(
        string eventType,
        Guid maintenanceRequestId,
        Guid? actorUserId,
        string targetRole,
        IEnumerable<Guid> targetProfileIds,
        string title,
        string message)
    {
        var notificationEvent = new NotificationEvent(
            CurrentSchemaVersion,
            Guid.NewGuid(),
            eventType,
            DateTime.UtcNow,
            maintenanceRequestId,
            actorUserId,
            targetRole,
            targetProfileIds.Distinct().Take(MaximumTargetProfileIds + 1).ToArray(),
            title.Trim(),
            message.Trim(),
            Guid.NewGuid(),
            ApiSource);

        if (!notificationEvent.IsValid())
        {
            throw new ArgumentException("The notification event is invalid.");
        }

        return notificationEvent;
    }

    public bool IsValid() =>
        SchemaVersion == CurrentSchemaVersion &&
        EventId != Guid.Empty &&
        NotificationEventTypes.Allowed.Contains(EventType) &&
        OccurredAtUtc.Kind == DateTimeKind.Utc &&
        MaintenanceRequestId != Guid.Empty &&
        NotificationTargetRoles.Allowed.Contains(TargetRole) &&
        TargetProfileIds.Count <= MaximumTargetProfileIds &&
        TargetProfileIds.All(id => id != Guid.Empty) &&
        !string.IsNullOrWhiteSpace(Title) &&
        Title.Length <= MaximumTitleLength &&
        !string.IsNullOrWhiteSpace(Message) &&
        Message.Length <= MaximumMessageLength &&
        CorrelationId != Guid.Empty &&
        Source == ApiSource;
}
