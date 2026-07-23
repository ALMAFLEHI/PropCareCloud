using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PropCareCloud.Api.Controllers;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.Notifications;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class UserNotificationServiceTests
{
    [Fact]
    public async Task StoreAndPublishAsync_StoresDistinctActiveRecipientsAndSameEvent()
    {
        await using var dbContext = CreateContext();
        var actor = User("Nadia Staff", UserRole.MaintenanceStaff);
        var tenant = User("Sara Tenant", UserRole.Tenant);
        var inactive = User("Inactive Tenant", UserRole.Tenant, false);
        var request = Request(tenant, actor);
        dbContext.AddRange(actor, tenant, inactive, request);
        await dbContext.SaveChangesAsync();
        var publisher = new RecordingNotificationPublisher();
        var service = CreateService(dbContext, actor.Id, publisher);
        var notificationEvent = NotificationEvent.Create(
            NotificationEventTypes.MaintenanceRequestStatusChanged,
            request.Id,
            actor.Id,
            NotificationTargetRoles.Multiple,
            [tenant.Id, actor.Id, tenant.Id, inactive.Id, Guid.NewGuid()],
            "Maintenance request status changed",
            "The status of a maintenance request was updated.");

        var result = await service.StoreAndPublishAsync(notificationEvent);

        Assert.True(result.NotificationQueued);
        var stored = Assert.Single(await dbContext.UserNotifications.ToListAsync());
        Assert.Equal(tenant.Id, stored.UserProfileId);
        Assert.Equal(notificationEvent.EventId, stored.EventId);
        Assert.Equal(notificationEvent.CorrelationId, stored.CorrelationId);
        Assert.Same(notificationEvent, Assert.Single(publisher.PublishedEvents));
    }

    [Fact]
    public async Task StoreAndPublishAsync_KeepsTenantActorForCreatedEvent()
    {
        await using var dbContext = CreateContext();
        var tenant = User("Sara Tenant", UserRole.Tenant);
        var request = Request(tenant);
        dbContext.AddRange(tenant, request);
        await dbContext.SaveChangesAsync();
        var service = CreateService(
            dbContext,
            tenant.Id,
            new RecordingNotificationPublisher());
        var notificationEvent = NotificationEvent.Create(
            NotificationEventTypes.MaintenanceRequestCreated,
            request.Id,
            tenant.Id,
            NotificationTargetRoles.Tenant,
            [tenant.Id],
            "Maintenance request created",
            "A maintenance request was submitted and is ready for review.");

        await service.StoreAndPublishAsync(notificationEvent);

        Assert.Equal(tenant.Id, (await dbContext.UserNotifications.SingleAsync()).UserProfileId);
    }

    [Fact]
    public async Task StoreAndPublishAsync_DoesNotDuplicateRecipientForSameEvent()
    {
        await using var dbContext = CreateContext();
        var tenant = User("Sara Tenant", UserRole.Tenant);
        var request = Request(tenant);
        dbContext.AddRange(tenant, request);
        await dbContext.SaveChangesAsync();
        var service = CreateService(
            dbContext,
            tenant.Id,
            new RecordingNotificationPublisher());
        var notificationEvent = NotificationEvent.Create(
            NotificationEventTypes.MaintenanceRequestCreated,
            request.Id,
            tenant.Id,
            NotificationTargetRoles.Tenant,
            [tenant.Id],
            "Maintenance request created",
            "A maintenance request was submitted and is ready for review.");

        await service.StoreAndPublishAsync(notificationEvent);
        await service.StoreAndPublishAsync(notificationEvent);

        Assert.Equal(1, await dbContext.UserNotifications.CountAsync());
    }

    [Fact]
    public async Task PublisherFailure_DoesNotRemoveStoredInboxNotification()
    {
        await using var dbContext = CreateContext();
        var tenant = User("Sara Tenant", UserRole.Tenant);
        var request = Request(tenant);
        dbContext.AddRange(tenant, request);
        await dbContext.SaveChangesAsync();
        var publisher = new RecordingNotificationPublisher
        {
            Result = NotificationDispatchResult.Failed("Publisher unavailable.")
        };
        var service = CreateService(dbContext, tenant.Id, publisher);

        var result = await service.StoreAndPublishAsync(NotificationEvent.Create(
            NotificationEventTypes.MaintenanceRequestCreated,
            request.Id,
            tenant.Id,
            NotificationTargetRoles.Tenant,
            [tenant.Id],
            "Maintenance request created",
            "A maintenance request was submitted and is ready for review."));

        Assert.False(result.NotificationQueued);
        Assert.Equal(1, await dbContext.UserNotifications.CountAsync());
    }

    [Fact]
    public async Task PersistenceFailure_DoesNotPreventEventPublishing()
    {
        var dbContext = CreateContext();
        var publisher = new RecordingNotificationPublisher();
        var service = CreateService(dbContext, Guid.NewGuid(), publisher);
        await dbContext.DisposeAsync();
        var notificationEvent = NotificationEvent.Create(
            NotificationEventTypes.MaintenanceRequestAssigned,
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationTargetRoles.Multiple,
            [Guid.NewGuid()],
            "Maintenance request assigned",
            "A maintenance request was assigned to maintenance staff.");

        var result = await service.StoreAndPublishAsync(notificationEvent);

        Assert.True(result.NotificationQueued);
        Assert.Single(publisher.PublishedEvents);
        Assert.Contains("inbox storage", result.NotificationMessage);
    }

    [Fact]
    public async Task GetAndUnreadCount_ReturnOnlyCurrentUsersNotifications()
    {
        await using var dbContext = CreateContext();
        var current = User("Current Tenant", UserRole.Tenant);
        var other = User("Other Tenant", UserRole.Tenant);
        dbContext.AddRange(current, other);
        dbContext.UserNotifications.AddRange(
            Inbox(current.Id, false, DateTime.UtcNow),
            Inbox(current.Id, true, DateTime.UtcNow.AddMinutes(-1)),
            Inbox(other.Id, false, DateTime.UtcNow.AddMinutes(1)));
        await dbContext.SaveChangesAsync();
        var service = CreateService(
            dbContext,
            current.Id,
            new RecordingNotificationPublisher());

        var all = await service.GetAsync(20, false);
        var unread = await service.GetAsync(20, true);
        var count = await service.GetUnreadCountAsync();

        Assert.Equal(2, all.Count);
        Assert.True(all[0].CreatedAtUtc > all[1].CreatedAtUtc);
        Assert.Single(unread);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task MarkRead_UsesCurrentUserIsolationAndMarkAllCountsChanges()
    {
        await using var dbContext = CreateContext();
        var current = User("Current Tenant", UserRole.Tenant);
        var other = User("Other Tenant", UserRole.Tenant);
        var first = Inbox(current.Id, false, DateTime.UtcNow);
        var second = Inbox(current.Id, false, DateTime.UtcNow.AddMinutes(-1));
        var foreign = Inbox(other.Id, false, DateTime.UtcNow);
        dbContext.AddRange(current, other, first, second, foreign);
        await dbContext.SaveChangesAsync();
        var service = CreateService(
            dbContext,
            current.Id,
            new RecordingNotificationPublisher());

        Assert.Null(await service.MarkReadAsync(foreign.Id));
        var marked = await service.MarkReadAsync(first.Id);
        var changed = await service.MarkAllReadAsync();

        Assert.NotNull(marked);
        Assert.True(marked.IsRead);
        Assert.Equal(1, changed);
        Assert.False((await dbContext.UserNotifications.FindAsync(foreign.Id))!.IsRead);
    }

    [Fact]
    public async Task NotificationsController_RejectsInvalidLimitAndRequiresAuthentication()
    {
        var authorize = typeof(NotificationsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();
        var controller = new NotificationsController(new RecordingNotificationPublisher());

        var response = await controller.GetNotifications(51, false);

        Assert.Equal("AllPortalRoles", authorize.Policy);
        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"propcare-notifications-{Guid.NewGuid()}")
            .Options);

    private static UserNotificationService CreateService(
        AppDbContext dbContext,
        Guid userProfileId,
        ITask2NotificationPublisher publisher) =>
        new(
            dbContext,
            new FakeCurrentUserService(userProfileId),
            publisher,
            NullLogger<UserNotificationService>.Instance);

    private static UserProfile User(string name, UserRole role, bool isActive = true) =>
        new()
        {
            FullName = name,
            Email = $"{Guid.NewGuid():N}@example.test",
            Role = role,
            IsActive = isActive
        };

    private static MaintenanceRequest Request(
        UserProfile tenant,
        UserProfile? staff = null) =>
        new()
        {
            RentalUnitId = Guid.NewGuid(),
            TenantProfileId = tenant.Id,
            TenantProfile = tenant,
            AssignedStaffProfileId = staff?.Id,
            AssignedStaffProfile = staff,
            Title = "Notification test request",
            Description = "Used to validate safe notification storage."
        };

    private static UserNotification Inbox(
        Guid userProfileId,
        bool isRead,
        DateTime createdAtUtc) =>
        new()
        {
            UserProfileId = userProfileId,
            EventId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            EventType = NotificationEventTypes.MaintenanceRequestStatusChanged,
            Title = "Request updated",
            Message = "A maintenance request was updated.",
            IsRead = isRead,
            CreatedAtUtc = createdAtUtc,
            ReadAtUtc = isRead ? createdAtUtc : null
        };

    private sealed class FakeCurrentUserService(Guid userProfileId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserProfileId => userProfileId;
        public string? Email => "notification.user@example.test";
        public UserRole? Role => UserRole.Tenant;
        public bool IsAdminOwner => false;
        public bool IsPropertyManager => false;
        public bool IsTenant => true;
        public bool IsMaintenanceStaff => false;
        public bool IsAdminOrManager => false;
        public bool HasRole(params UserRole[] roles) => roles.Contains(UserRole.Tenant);
    }
}
