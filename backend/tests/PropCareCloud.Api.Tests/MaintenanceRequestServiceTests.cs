using Microsoft.EntityFrameworkCore;
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
        var service = new MaintenanceRequestService(dbContext);

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
        var service = new MaintenanceRequestService(dbContext);

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
        var service = new MaintenanceRequestService(dbContext);

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
        var service = new MaintenanceRequestService(dbContext);

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
        var service = new MaintenanceRequestService(dbContext);

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
        var service = new MaintenanceRequestService(dbContext);

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
        var service = new MaintenanceRequestService(dbContext);

        var comments = await service.GetCommentsAsync(seed.Request.Id);

        Assert.Single(comments);
        Assert.Equal("Existing comment", comments[0].CommentText);
        Assert.Equal(seed.Tenant.FullName, comments[0].UserFullName);
    }

    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"propcare-maintenance-{Guid.NewGuid()}")
            .Options;
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
}
