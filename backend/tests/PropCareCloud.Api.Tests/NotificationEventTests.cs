using PropCareCloud.Api.DTOs.Notifications;

namespace PropCareCloud.Api.Tests;

public sealed class NotificationEventTests
{
    [Fact]
    public void CreateBuildsValidVersionedEventWithUniqueIdentifiers()
    {
        var requestId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var first = CreateEvent(requestId, actorId);
        var second = CreateEvent(requestId, actorId);

        Assert.True(first.IsValid());
        Assert.Equal(NotificationEvent.CurrentSchemaVersion, first.SchemaVersion);
        Assert.Equal(NotificationEvent.ApiSource, first.Source);
        Assert.NotEqual(Guid.Empty, first.EventId);
        Assert.NotEqual(Guid.Empty, first.CorrelationId);
        Assert.NotEqual(first.EventId, second.EventId);
        Assert.NotEqual(first.CorrelationId, second.CorrelationId);
    }

    [Fact]
    public void IsValidRejectsUnknownEventType()
    {
        var notificationEvent = CreateEvent() with { EventType = "UnknownEvent" };

        Assert.False(notificationEvent.IsValid());
    }

    [Fact]
    public void IsValidRejectsOversizedSafeText()
    {
        var notificationEvent = CreateEvent() with
        {
            Title = new string('T', NotificationEvent.MaximumTitleLength + 1),
            Message = new string('M', NotificationEvent.MaximumMessageLength + 1)
        };

        Assert.False(notificationEvent.IsValid());
    }

    [Fact]
    public void CreateRejectsMoreThanTwentyTargetProfiles()
    {
        var targetIds = Enumerable.Range(0, NotificationEvent.MaximumTargetProfileIds + 1)
            .Select(_ => Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => NotificationEvent.Create(
            NotificationEventTypes.MaintenanceRequestCreated,
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationTargetRoles.Multiple,
            targetIds,
            "Maintenance request created",
            "A maintenance request was submitted."));
    }

    private static NotificationEvent CreateEvent(
        Guid? requestId = null,
        Guid? actorId = null) =>
        NotificationEvent.Create(
            NotificationEventTypes.MaintenanceRequestCreated,
            requestId ?? Guid.NewGuid(),
            actorId ?? Guid.NewGuid(),
            NotificationTargetRoles.Multiple,
            [Guid.NewGuid()],
            "Maintenance request created",
            "A maintenance request was submitted.");
}
