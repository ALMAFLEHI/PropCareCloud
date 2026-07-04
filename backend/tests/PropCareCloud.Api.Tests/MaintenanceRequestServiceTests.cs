using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using PropCareCloud.Api.Controllers;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.MaintenanceRequests;
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
        UserRole role = UserRole.AdminOwner)
    {
        return new MaintenanceRequestService(
            dbContext,
            new FakeCurrentUserService(userProfileId ?? Guid.NewGuid(), role));
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

    private sealed record MaintenanceSeed(
        Property Property,
        RentalUnit Unit,
        UserProfile Tenant,
        UserProfile Manager,
        UserProfile Staff,
        MaintenanceRequest? Request);

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
