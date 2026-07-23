using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using PropCareCloud.Api.Controllers;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.MaintenanceRequests;
using PropCareCloud.Api.DTOs.Notifications;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class MaintenanceRequestServiceTests
{
    [Fact]
    public async Task GetRequestsAsync_ReturnsSeededMaintenanceRequests()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        await SeedMaintenanceGraphAsync(dbContext);
        var service = CreateService(dbContext);

        var requests = await service.GetRequestsAsync();

        Assert.Single(requests);
        Assert.Equal("Air conditioner issue", requests[0].Title);
        Assert.Equal("A-0201", requests[0].UnitNumber);
        Assert.Equal("Cloud Residence", requests[0].PropertyName);
    }

    [Fact]
    public async Task CreateRequestAsync_CreatesRequestWhenTenantAndRentalUnitExist()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext, includeRequest: false);
        var service = CreateService(dbContext);

        var created = await service.CreateRequestAsync(new MaintenanceRequestCreateRequest
        {
            RentalUnitId = seed.Unit.Id,
            TenantProfileId = seed.Tenant.Id,
            Title = "Kitchen sink leak",
            Description = "Water is leaking under the kitchen sink.",
            Category = MaintenanceCategory.Plumbing,
            Priority = MaintenancePriority.High
        });

        Assert.NotNull(created);
        Assert.Equal(MaintenanceStatus.Submitted, created.Status);
        Assert.Equal("Kitchen sink leak", created.Title);
        Assert.Equal(1, await dbContext.MaintenanceRequests.CountAsync());
    }

    [Fact]
    public async Task CreateRequestAsync_ReturnsNullWhenUserIsNotTenant()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext, includeRequest: false);
        var service = CreateService(dbContext);

        var created = await service.CreateRequestAsync(new MaintenanceRequestCreateRequest
        {
            RentalUnitId = seed.Unit.Id,
            TenantProfileId = seed.Manager.Id,
            Title = "Invalid tenant role",
            Description = "This should not create a maintenance request.",
            Category = MaintenanceCategory.Other,
            Priority = MaintenancePriority.Low
        });

        Assert.Null(created);
        Assert.Empty(dbContext.MaintenanceRequests);
    }

    [Fact]
    public async Task AssignRequestAsync_AssignsOnlyMaintenanceStaffRole()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        var service = CreateService(dbContext);

        var invalidAssignment = await service.AssignRequestAsync(seed.Request!.Id, new MaintenanceRequestAssignRequest
        {
            AssignedStaffProfileId = seed.Tenant.Id
        });
        var validAssignment = await service.AssignRequestAsync(seed.Request.Id, new MaintenanceRequestAssignRequest
        {
            AssignedStaffProfileId = seed.Staff.Id
        });

        Assert.Null(invalidAssignment);
        Assert.NotNull(validAssignment);
        Assert.Equal(seed.Staff.Id, validAssignment.AssignedStaffProfileId);
        Assert.Equal(MaintenanceStatus.Assigned, validAssignment.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_SetsCompletedAtUtcWhenCompleted()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        var service = CreateService(dbContext);

        var updated = await service.UpdateStatusAsync(seed.Request!.Id, new MaintenanceRequestStatusUpdateRequest
        {
            Status = MaintenanceStatus.Completed
        });

        Assert.NotNull(updated);
        Assert.Equal(MaintenanceStatus.Completed, updated.Status);
        Assert.NotNull(updated.CompletedAtUtc);
    }

    [Fact]
    public async Task CreateRequestAsync_PublishesCreatedEventAfterPersistence()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext, includeRequest: false);
        var publisher = new RecordingNotificationPublisher();
        var service = CreateService(dbContext, notificationPublisher: publisher);

        var created = await service.CreateRequestAsync(new MaintenanceRequestCreateRequest
        {
            RentalUnitId = seed.Unit.Id,
            TenantProfileId = seed.Tenant.Id,
            Title = "Notification test",
            Description = "The request must be saved before notification dispatch.",
            Category = MaintenanceCategory.Other,
            Priority = MaintenancePriority.Low
        });

        Assert.NotNull(created);
        Assert.True(created.NotificationQueued);
        var published = Assert.Single(publisher.PublishedEvents);
        Assert.Equal(NotificationEventTypes.MaintenanceRequestCreated, published.EventType);
        Assert.Equal(created.Id, published.MaintenanceRequestId);
        Assert.Equal(1, await dbContext.MaintenanceRequests.CountAsync());
    }

    [Fact]
    public async Task AssignRequestAsync_PublishesOnlyWhenAssignmentChanges()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        var publisher = new RecordingNotificationPublisher();
        var service = CreateService(dbContext, notificationPublisher: publisher);

        var first = await service.AssignRequestAsync(
            seed.Request!.Id,
            new MaintenanceRequestAssignRequest
            {
                AssignedStaffProfileId = seed.Staff.Id
            });
        var unchanged = await service.AssignRequestAsync(
            seed.Request.Id,
            new MaintenanceRequestAssignRequest
            {
                AssignedStaffProfileId = seed.Staff.Id
            });

        Assert.NotNull(first);
        Assert.NotNull(unchanged);
        var published = Assert.Single(publisher.PublishedEvents);
        Assert.Equal(NotificationEventTypes.MaintenanceRequestAssigned, published.EventType);
    }

    [Fact]
    public async Task UpdateStatusAsync_PublishesOnlyWhenStatusChanges()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        var publisher = new RecordingNotificationPublisher();
        var service = CreateService(dbContext, notificationPublisher: publisher);

        var first = await service.UpdateStatusAsync(
            seed.Request!.Id,
            new MaintenanceRequestStatusUpdateRequest
            {
                Status = MaintenanceStatus.UnderReview
            });
        var unchanged = await service.UpdateStatusAsync(
            seed.Request.Id,
            new MaintenanceRequestStatusUpdateRequest
            {
                Status = MaintenanceStatus.UnderReview
            });

        Assert.NotNull(first);
        Assert.NotNull(unchanged);
        var published = Assert.Single(publisher.PublishedEvents);
        Assert.Equal(
            NotificationEventTypes.MaintenanceRequestStatusChanged,
            published.EventType);
    }

    [Fact]
    public async Task PublisherFailureDoesNotRollbackCreatedRequest()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext, includeRequest: false);
        var publisher = new RecordingNotificationPublisher
        {
            Result = NotificationDispatchResult.Failed(
                "Update saved, but notification delivery is temporarily unavailable.")
        };
        var service = CreateService(dbContext, notificationPublisher: publisher);

        var created = await service.CreateRequestAsync(new MaintenanceRequestCreateRequest
        {
            RentalUnitId = seed.Unit.Id,
            TenantProfileId = seed.Tenant.Id,
            Title = "Non-breaking failure",
            Description = "A publisher failure must not roll back the request.",
            Category = MaintenanceCategory.Other,
            Priority = MaintenancePriority.Medium
        });

        Assert.NotNull(created);
        Assert.False(created.NotificationQueued);
        Assert.Equal(1, await dbContext.MaintenanceRequests.CountAsync());
    }

    [Fact]
    public async Task AddCommentAsync_AddsCommentToRequest()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant);

        var comment = await service.AddCommentAsync(seed.Request!.Id, new MaintenanceRequestCommentCreateRequest
        {
            UserProfileId = seed.Tenant.Id,
            CommentText = "Please check this today.",
            IsInternal = false
        });

        Assert.NotNull(comment);
        Assert.Equal("Please check this today.", comment.CommentText);
        Assert.Equal(1, await dbContext.MaintenanceRequestComments.CountAsync());
    }

    [Fact]
    public async Task GetCommentsAsync_ReturnsComments()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        dbContext.MaintenanceRequestComments.Add(new MaintenanceRequestComment
        {
            MaintenanceRequestId = seed.Request!.Id,
            UserProfileId = seed.Tenant.Id,
            UserProfile = seed.Tenant,
            CommentText = "Existing comment",
            IsInternal = false
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var comments = await service.GetCommentsAsync(seed.Request.Id);

        Assert.Single(comments);
        Assert.Equal("Existing comment", comments[0].CommentText);
        Assert.Equal(seed.Tenant.FullName, comments[0].UserFullName);
    }

    [Fact]
    public async Task GetCommentsAsync_HidesInternalCommentsFromTenants()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        dbContext.MaintenanceRequestComments.AddRange(
            new MaintenanceRequestComment
            {
                MaintenanceRequestId = seed.Request!.Id,
                UserProfileId = seed.Manager.Id,
                UserProfile = seed.Manager,
                CommentText = "Internal scheduling note",
                IsInternal = true
            },
            new MaintenanceRequestComment
            {
                MaintenanceRequestId = seed.Request.Id,
                UserProfileId = seed.Tenant.Id,
                UserProfile = seed.Tenant,
                CommentText = "Visible tenant note",
                IsInternal = false
            });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant);

        var comments = await service.GetCommentsAsync(seed.Request.Id);

        Assert.Single(comments);
        Assert.Equal("Visible tenant note", comments[0].CommentText);
    }

    [Fact]
    public async Task AddCommentAsync_TenantCannotCreateInternalComment()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant);

        var comment = await service.AddCommentAsync(seed.Request!.Id, new MaintenanceRequestCommentCreateRequest
        {
            UserProfileId = seed.Tenant.Id,
            CommentText = "This should not become internal.",
            IsInternal = true
        });

        Assert.Null(comment);
        Assert.Empty(dbContext.MaintenanceRequestComments);
    }

    [Fact]
    public async Task GetRequestsAsync_TenantSeesOnlyOwnRequests()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        var otherTenant = new UserProfile
        {
            FullName = "Other Tenant",
            Email = "tenant.other@example.com",
            Role = UserRole.Tenant
        };
        dbContext.UserProfiles.Add(otherTenant);
        dbContext.MaintenanceRequests.Add(new MaintenanceRequest
        {
            RentalUnitId = seed.Unit.Id,
            TenantProfileId = otherTenant.Id,
            Title = "Other tenant request",
            Description = "This record should not appear for the signed-in tenant.",
            Category = MaintenanceCategory.Other,
            Priority = MaintenancePriority.Low,
            Status = MaintenanceStatus.Submitted
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant);

        var requests = await service.GetRequestsAsync();

        Assert.Single(requests);
        Assert.Equal(seed.Tenant.Id, requests[0].TenantProfileId);
    }

    [Fact]
    public async Task GetRequestsAsync_IsolatesSaraAndImranTenantRequests()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedTwoTenantGraphAsync(dbContext);
        var saraService = CreateService(dbContext, seed.SaraTenant.Id, UserRole.Tenant);
        var imranService = CreateService(dbContext, seed.ImranTenant.Id, UserRole.Tenant);

        var saraRequests = await saraService.GetRequestsAsync();
        var imranRequests = await imranService.GetRequestsAsync();

        Assert.Single(saraRequests);
        Assert.Single(imranRequests);
        Assert.Equal(seed.SaraRequest.Id, saraRequests[0].Id);
        Assert.Equal(seed.ImranRequest.Id, imranRequests[0].Id);
        Assert.DoesNotContain(saraRequests, request => request.TenantProfileId == seed.ImranTenant.Id);
        Assert.DoesNotContain(imranRequests, request => request.TenantProfileId == seed.SaraTenant.Id);
    }

    [Fact]
    public async Task GetRequestByIdAsync_BlocksCrossTenantRequestAccess()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedTwoTenantGraphAsync(dbContext);
        var saraService = CreateService(dbContext, seed.SaraTenant.Id, UserRole.Tenant);
        var imranService = CreateService(dbContext, seed.ImranTenant.Id, UserRole.Tenant);

        var saraViewingImran = await saraService.GetRequestByIdAsync(seed.ImranRequest.Id);
        var imranViewingSara = await imranService.GetRequestByIdAsync(seed.SaraRequest.Id);

        Assert.Null(saraViewingImran);
        Assert.Null(imranViewingSara);
    }

    [Fact]
    public async Task GetRequestsAsync_MaintenanceStaffSeesOnlyAssignedRequests()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        seed.Request!.AssignedStaffProfileId = seed.Staff.Id;
        var unassignedRequest = new MaintenanceRequest
        {
            RentalUnitId = seed.Unit.Id,
            TenantProfileId = seed.Tenant.Id,
            Title = "Unassigned tenant request",
            Description = "This record should not appear for maintenance staff.",
            Category = MaintenanceCategory.Plumbing,
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceStatus.Submitted
        };
        dbContext.MaintenanceRequests.Add(unassignedRequest);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, seed.Staff.Id, UserRole.MaintenanceStaff);

        var requests = await service.GetRequestsAsync();

        Assert.Single(requests);
        Assert.Equal(seed.Staff.Id, requests[0].AssignedStaffProfileId);
    }

    [Fact]
    public async Task GetRequestByIdAsync_MaintenanceStaffCannotFetchUnassignedRequest()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Staff.Id, UserRole.MaintenanceStaff);

        var request = await service.GetRequestByIdAsync(seed.Request!.Id);

        Assert.Null(request);
    }

    [Fact]
    public async Task GetRequestsAsync_AdminAndManagerSeeAllRequests()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        dbContext.MaintenanceRequests.Add(new MaintenanceRequest
        {
            RentalUnitId = seed.Unit.Id,
            TenantProfileId = seed.Tenant.Id,
            Title = "Second request",
            Description = "Admin and manager should both see this request.",
            Category = MaintenanceCategory.Security,
            Priority = MaintenancePriority.Low,
            Status = MaintenanceStatus.UnderReview
        });
        await dbContext.SaveChangesAsync();
        var adminService = CreateService(dbContext, role: UserRole.AdminOwner);
        var managerService = CreateService(dbContext, seed.Manager.Id, UserRole.PropertyManager);

        var adminRequests = await adminService.GetRequestsAsync();
        var managerRequests = await managerService.GetRequestsAsync();

        Assert.Equal(2, adminRequests.Count);
        Assert.Equal(2, managerRequests.Count);
    }

    [Fact]
    public async Task CreateRequestAsync_TenantRequiresActiveAssignedUnit()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext, includeRequest: false);
        dbContext.TenantUnitAssignments.Add(new TenantUnitAssignment
        {
            TenantProfileId = seed.Tenant.Id,
            RentalUnitId = seed.Unit.Id,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant);

        var created = await service.CreateRequestAsync(new MaintenanceRequestCreateRequest
        {
            RentalUnitId = seed.Unit.Id,
            TenantProfileId = seed.Manager.Id,
            Title = "Tenant submitted issue",
            Description = "The API should use the signed-in tenant profile.",
            Category = MaintenanceCategory.Other,
            Priority = MaintenancePriority.Medium
        });

        Assert.NotNull(created);
        Assert.Equal(seed.Tenant.Id, created.TenantProfileId);
    }

    [Fact]
    public async Task CreateRequestAsync_TenantCannotCreateForUnassignedUnit()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext, includeRequest: false);
        var service = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant);

        var created = await service.CreateRequestAsync(new MaintenanceRequestCreateRequest
        {
            RentalUnitId = seed.Unit.Id,
            TenantProfileId = seed.Tenant.Id,
            Title = "Unassigned unit",
            Description = "This should be blocked by tenant assignment rules.",
            Category = MaintenanceCategory.Other,
            Priority = MaintenancePriority.Medium
        });

        Assert.Null(created);
    }

    [Fact]
    public async Task CreateRequestAsync_TenantsCannotCreateForEachOthersUnits()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedTwoTenantGraphAsync(dbContext);
        var saraService = CreateService(dbContext, seed.SaraTenant.Id, UserRole.Tenant);
        var imranService = CreateService(dbContext, seed.ImranTenant.Id, UserRole.Tenant);

        var saraUsingImranUnit = await saraService.CreateRequestAsync(new MaintenanceRequestCreateRequest
        {
            RentalUnitId = seed.ImranUnit.Id,
            TenantProfileId = seed.ImranTenant.Id,
            Title = "Wrong unit attempt",
            Description = "Sara should not create a request for Imran's unit.",
            Category = MaintenanceCategory.Other,
            Priority = MaintenancePriority.Low
        });
        var imranUsingSaraUnit = await imranService.CreateRequestAsync(new MaintenanceRequestCreateRequest
        {
            RentalUnitId = seed.SaraUnit.Id,
            TenantProfileId = seed.SaraTenant.Id,
            Title = "Wrong unit attempt",
            Description = "Imran should not create a request for Sara's unit.",
            Category = MaintenanceCategory.Other,
            Priority = MaintenancePriority.Low
        });

        Assert.Null(saraUsingImranUnit);
        Assert.Null(imranUsingSaraUnit);
    }

    [Fact]
    public async Task CreateRequestAsync_TenantCanCreateForOwnAssignedUnit()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedTwoTenantGraphAsync(dbContext);
        var imranService = CreateService(dbContext, seed.ImranTenant.Id, UserRole.Tenant);

        var created = await imranService.CreateRequestAsync(new MaintenanceRequestCreateRequest
        {
            RentalUnitId = seed.ImranSecondUnit.Id,
            TenantProfileId = seed.SaraTenant.Id,
            Title = "Bedroom light repair",
            Description = "The backend should use Imran's authenticated tenant profile.",
            Category = MaintenanceCategory.Electrical,
            Priority = MaintenancePriority.Medium
        });

        Assert.NotNull(created);
        Assert.Equal(seed.ImranTenant.Id, created.TenantProfileId);
        Assert.Equal(seed.ImranSecondUnit.Id, created.RentalUnitId);
    }

    [Fact]
    public async Task UpdateStatusAsync_MaintenanceStaffCanOnlyUpdateAssignedJobs()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        seed.Request!.AssignedStaffProfileId = seed.Staff.Id;
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, seed.Staff.Id, UserRole.MaintenanceStaff);

        var validUpdate = await service.UpdateStatusAsync(seed.Request.Id, new MaintenanceRequestStatusUpdateRequest
        {
            Status = MaintenanceStatus.InProgress
        });
        var invalidUpdate = await service.UpdateStatusAsync(seed.Request.Id, new MaintenanceRequestStatusUpdateRequest
        {
            Status = MaintenanceStatus.Cancelled
        });

        Assert.NotNull(validUpdate);
        Assert.Equal(MaintenanceStatus.InProgress, validUpdate.Status);
        Assert.Null(invalidUpdate);
    }

    [Fact]
    public async Task UpdateStatusAsync_MaintenanceStaffCannotUpdateUnassignedJob()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Staff.Id, UserRole.MaintenanceStaff);

        var updated = await service.UpdateStatusAsync(seed.Request!.Id, new MaintenanceRequestStatusUpdateRequest
        {
            Status = MaintenanceStatus.InProgress
        });

        Assert.Null(updated);
    }

    [Fact]
    public async Task UpdateStatusAsync_TenantCannotUpdateStatus()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedMaintenanceGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant);

        var updated = await service.UpdateStatusAsync(seed.Request!.Id, new MaintenanceRequestStatusUpdateRequest
        {
            Status = MaintenanceStatus.Completed
        });

        Assert.Null(updated);
    }

    [Fact]
    public void PropertiesController_RequiresAdminOrManagerPolicy()
    {
        var authorizeAttribute = typeof(PropertiesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal("AdminOrManager", authorizeAttribute.Policy);
    }

    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"propcare-maintenance-{Guid.NewGuid()}")
            .Options;
    }

    private static MaintenanceRequestService CreateService(
        AppDbContext dbContext,
        Guid? userProfileId = null,
        UserRole role = UserRole.AdminOwner,
        ITask2NotificationPublisher? notificationPublisher = null)
    {
        return new MaintenanceRequestService(
            dbContext,
            new FakeCurrentUserService(userProfileId ?? Guid.NewGuid(), role),
            notificationPublisher ?? new RecordingNotificationPublisher());
    }

    private static async Task<MaintenanceSeed> SeedMaintenanceGraphAsync(
        AppDbContext dbContext,
        bool includeRequest = true)
    {
        var property = new Property
        {
            Name = "Cloud Residence",
            AddressLine1 = "12 Innovation Avenue",
            City = "Kuala Lumpur",
            Country = "Malaysia",
            Status = PropertyStatus.Active
        };
        var unit = new RentalUnit
        {
            PropertyId = property.Id,
            Property = property,
            UnitNumber = "A-0201",
            Floor = "2",
            Bedrooms = 2,
            Status = UnitStatus.Occupied
        };
        var tenant = new UserProfile
        {
            FullName = "Tenant User",
            Email = "tenant.workflow@example.com",
            Role = UserRole.Tenant
        };
        var manager = new UserProfile
        {
            FullName = "Manager User",
            Email = "manager.workflow@example.com",
            Role = UserRole.PropertyManager
        };
        var staff = new UserProfile
        {
            FullName = "Staff User",
            Email = "staff.workflow@example.com",
            Role = UserRole.MaintenanceStaff
        };
        MaintenanceRequest? request = null;

        if (includeRequest)
        {
            request = new MaintenanceRequest
            {
                RentalUnitId = unit.Id,
                RentalUnit = unit,
                TenantProfileId = tenant.Id,
                TenantProfile = tenant,
                Title = "Air conditioner issue",
                Description = "The unit is not cooling.",
                Category = MaintenanceCategory.HVAC,
                Priority = MaintenancePriority.Medium,
                Status = MaintenanceStatus.Submitted
            };
        }

        dbContext.Properties.Add(property);
        dbContext.RentalUnits.Add(unit);
        dbContext.UserProfiles.AddRange(tenant, manager, staff);
        if (request is not null)
        {
            dbContext.MaintenanceRequests.Add(request);
        }

        await dbContext.SaveChangesAsync();

        return new MaintenanceSeed(property, unit, tenant, manager, staff, request);
    }

    private static async Task<TwoTenantSeed> SeedTwoTenantGraphAsync(AppDbContext dbContext)
    {
        var property = new Property
        {
            Name = "Tenant Isolation Residence",
            AddressLine1 = "45 Privacy Lane",
            City = "Kuala Lumpur",
            Country = "Malaysia",
            Status = PropertyStatus.Active
        };
        var saraUnit = new RentalUnit
        {
            PropertyId = property.Id,
            Property = property,
            UnitNumber = "B-1102",
            Floor = "11",
            Bedrooms = 1,
            Status = UnitStatus.Occupied
        };
        var saraSecondUnit = new RentalUnit
        {
            PropertyId = property.Id,
            Property = property,
            UnitNumber = "A-0101",
            Floor = "1",
            Bedrooms = 2,
            Status = UnitStatus.Occupied
        };
        var imranUnit = new RentalUnit
        {
            PropertyId = property.Id,
            Property = property,
            UnitNumber = "A-0205",
            Floor = "2",
            Bedrooms = 3,
            Status = UnitStatus.Occupied
        };
        var imranSecondUnit = new RentalUnit
        {
            PropertyId = property.Id,
            Property = property,
            UnitNumber = "B-1208",
            Floor = "12",
            Bedrooms = 2,
            Status = UnitStatus.UnderMaintenance
        };
        var saraTenant = new UserProfile
        {
            FullName = "Sara Tenant",
            Email = "tenant1@example.com",
            Role = UserRole.Tenant
        };
        var imranTenant = new UserProfile
        {
            FullName = "Imran Tenant",
            Email = "tenant2@example.com",
            Role = UserRole.Tenant
        };
        var manager = new UserProfile
        {
            FullName = "Manager User",
            Email = "manager.isolation@example.com",
            Role = UserRole.PropertyManager
        };
        var staff = new UserProfile
        {
            FullName = "Nadia Maintenance Staff",
            Email = "staff1@example.com",
            Role = UserRole.MaintenanceStaff
        };
        var saraRequest = new MaintenanceRequest
        {
            RentalUnitId = saraUnit.Id,
            RentalUnit = saraUnit,
            TenantProfileId = saraTenant.Id,
            TenantProfile = saraTenant,
            AssignedStaffProfileId = staff.Id,
            AssignedStaffProfile = staff,
            Title = "Sara kitchen sink",
            Description = "Sara's private maintenance request.",
            Category = MaintenanceCategory.Plumbing,
            Priority = MaintenancePriority.High,
            Status = MaintenanceStatus.Assigned
        };
        var imranRequest = new MaintenanceRequest
        {
            RentalUnitId = imranUnit.Id,
            RentalUnit = imranUnit,
            TenantProfileId = imranTenant.Id,
            TenantProfile = imranTenant,
            Title = "Imran AC service",
            Description = "Imran's private maintenance request.",
            Category = MaintenanceCategory.HVAC,
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceStatus.UnderReview
        };

        dbContext.Properties.Add(property);
        dbContext.RentalUnits.AddRange(saraUnit, saraSecondUnit, imranUnit, imranSecondUnit);
        dbContext.UserProfiles.AddRange(saraTenant, imranTenant, manager, staff);
        dbContext.TenantUnitAssignments.AddRange(
            new TenantUnitAssignment
            {
                TenantProfileId = saraTenant.Id,
                TenantProfile = saraTenant,
                RentalUnitId = saraUnit.Id,
                RentalUnit = saraUnit,
                IsActive = true
            },
            new TenantUnitAssignment
            {
                TenantProfileId = saraTenant.Id,
                TenantProfile = saraTenant,
                RentalUnitId = saraSecondUnit.Id,
                RentalUnit = saraSecondUnit,
                IsActive = true
            },
            new TenantUnitAssignment
            {
                TenantProfileId = imranTenant.Id,
                TenantProfile = imranTenant,
                RentalUnitId = imranUnit.Id,
                RentalUnit = imranUnit,
                IsActive = true
            },
            new TenantUnitAssignment
            {
                TenantProfileId = imranTenant.Id,
                TenantProfile = imranTenant,
                RentalUnitId = imranSecondUnit.Id,
                RentalUnit = imranSecondUnit,
                IsActive = true
            });
        dbContext.MaintenanceRequests.AddRange(saraRequest, imranRequest);
        await dbContext.SaveChangesAsync();

        return new TwoTenantSeed(
            saraTenant,
            imranTenant,
            manager,
            staff,
            saraUnit,
            saraSecondUnit,
            imranUnit,
            imranSecondUnit,
            saraRequest,
            imranRequest);
    }

    private sealed record MaintenanceSeed(
        Property Property,
        RentalUnit Unit,
        UserProfile Tenant,
        UserProfile Manager,
        UserProfile Staff,
        MaintenanceRequest? Request);

    private sealed record TwoTenantSeed(
        UserProfile SaraTenant,
        UserProfile ImranTenant,
        UserProfile Manager,
        UserProfile Staff,
        RentalUnit SaraUnit,
        RentalUnit SaraSecondUnit,
        RentalUnit ImranUnit,
        RentalUnit ImranSecondUnit,
        MaintenanceRequest SaraRequest,
        MaintenanceRequest ImranRequest);

    private sealed class FakeCurrentUserService(Guid userProfileId, UserRole role) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserProfileId => userProfileId;
        public string? Email => "test-user@propcare.local";
        public UserRole? Role => role;
        public bool IsAdminOwner => role == UserRole.AdminOwner;
        public bool IsPropertyManager => role == UserRole.PropertyManager;
        public bool IsTenant => role == UserRole.Tenant;
        public bool IsMaintenanceStaff => role == UserRole.MaintenanceStaff;
        public bool IsAdminOrManager => role is UserRole.AdminOwner or UserRole.PropertyManager;

        public bool HasRole(params UserRole[] roles)
        {
            return roles.Contains(role);
        }
    }
}
