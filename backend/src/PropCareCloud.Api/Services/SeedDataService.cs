using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.Models;

namespace PropCareCloud.Api.Services;

public interface ISeedDataService
{
    Task<SeedDataResult> SeedDemoDataAsync(CancellationToken cancellationToken = default);
}

public sealed class SeedDataService(AppDbContext dbContext) : ISeedDataService
{
    public async Task<SeedDataResult> SeedDemoDataAsync(CancellationToken cancellationToken = default)
    {
        var timestampUtc = DateTime.UtcNow;

        if (await dbContext.UserProfiles.AnyAsync(cancellationToken))
        {
            return new SeedDataResult(
                Success: true,
                Message: "Demo seed data already exists. No duplicate records were created.",
                UsersCreated: 0,
                PropertiesCreated: 0,
                UnitsCreated: 0,
                RequestsCreated: 0,
                CommentsCreated: 0,
                AttachmentsCreated: 0,
                SkippedBecauseAlreadySeeded: true,
                TimestampUtc: timestampUtc);
        }

        var users = CreateUsers(timestampUtc);
        var properties = CreateProperties(timestampUtc);
        var units = CreateUnits(properties, timestampUtc);
        var tenantUnitAssignments = CreateTenantUnitAssignments(users, units, timestampUtc);
        var requests = CreateMaintenanceRequests(users, units, timestampUtc);
        var comments = CreateComments(users, requests, timestampUtc);
        var attachments = CreateAttachments(users, requests, timestampUtc);

        await dbContext.UserProfiles.AddRangeAsync(users, cancellationToken);
        await dbContext.Properties.AddRangeAsync(properties, cancellationToken);
        await dbContext.RentalUnits.AddRangeAsync(units, cancellationToken);
        await dbContext.TenantUnitAssignments.AddRangeAsync(tenantUnitAssignments, cancellationToken);
        await dbContext.MaintenanceRequests.AddRangeAsync(requests, cancellationToken);
        await dbContext.MaintenanceRequestComments.AddRangeAsync(comments, cancellationToken);
        await dbContext.MaintenanceRequestAttachments.AddRangeAsync(attachments, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SeedDataResult(
            Success: true,
            Message: "Demo seed data created for local PostgreSQL testing.",
            UsersCreated: users.Count,
            PropertiesCreated: properties.Count,
            UnitsCreated: units.Count,
            RequestsCreated: requests.Count,
            CommentsCreated: comments.Count,
            AttachmentsCreated: attachments.Count,
            SkippedBecauseAlreadySeeded: false,
            TimestampUtc: timestampUtc);
    }

    private static List<UserProfile> CreateUsers(DateTime timestampUtc)
    {
        return
        [
            new()
            {
                FullName = "Amina Owner",
                Email = "admin.owner@example.com",
                PhoneNumber = "+60010000001",
                Role = UserRole.AdminOwner,
                CreatedAtUtc = timestampUtc
            },
            new()
            {
                FullName = "Daniel Property Manager",
                Email = "manager1@example.com",
                PhoneNumber = "+60010000002",
                Role = UserRole.PropertyManager,
                CreatedAtUtc = timestampUtc
            },
            new()
            {
                FullName = "Sara Tenant",
                Email = "tenant1@example.com",
                PhoneNumber = "+60010000003",
                Role = UserRole.Tenant,
                CreatedAtUtc = timestampUtc
            },
            new()
            {
                FullName = "Imran Tenant",
                Email = "tenant2@example.com",
                PhoneNumber = "+60010000004",
                Role = UserRole.Tenant,
                CreatedAtUtc = timestampUtc
            },
            new()
            {
                FullName = "Nadia Maintenance Staff",
                Email = "staff1@example.com",
                PhoneNumber = "+60010000005",
                Role = UserRole.MaintenanceStaff,
                CreatedAtUtc = timestampUtc
            },
            new()
            {
                FullName = "Leo Maintenance Staff",
                Email = "staff2@example.com",
                PhoneNumber = "+60010000006",
                Role = UserRole.MaintenanceStaff,
                CreatedAtUtc = timestampUtc
            }
        ];
    }

    private static List<Property> CreateProperties(DateTime timestampUtc)
    {
        return
        [
            new()
            {
                Name = "Cloud Residence",
                AddressLine1 = "12 Innovation Avenue",
                AddressLine2 = "Block A",
                City = "Kuala Lumpur",
                Country = "Malaysia",
                Status = PropertyStatus.Active,
                CreatedAtUtc = timestampUtc
            },
            new()
            {
                Name = "Harbor Heights",
                AddressLine1 = "88 Marina Link",
                City = "Penang",
                Country = "Malaysia",
                Status = PropertyStatus.Active,
                CreatedAtUtc = timestampUtc
            }
        ];
    }

    private static List<RentalUnit> CreateUnits(IReadOnlyList<Property> properties, DateTime timestampUtc)
    {
        return
        [
            new()
            {
                PropertyId = properties[0].Id,
                Property = properties[0],
                UnitNumber = "A-0101",
                Floor = "1",
                Bedrooms = 2,
                Status = UnitStatus.Occupied,
                CreatedAtUtc = timestampUtc
            },
            new()
            {
                PropertyId = properties[0].Id,
                Property = properties[0],
                UnitNumber = "A-0205",
                Floor = "2",
                Bedrooms = 3,
                Status = UnitStatus.Occupied,
                CreatedAtUtc = timestampUtc
            },
            new()
            {
                PropertyId = properties[1].Id,
                Property = properties[1],
                UnitNumber = "B-1102",
                Floor = "11",
                Bedrooms = 1,
                Status = UnitStatus.Occupied,
                CreatedAtUtc = timestampUtc
            },
            new()
            {
                PropertyId = properties[1].Id,
                Property = properties[1],
                UnitNumber = "B-1208",
                Floor = "12",
                Bedrooms = 2,
                Status = UnitStatus.UnderMaintenance,
                CreatedAtUtc = timestampUtc
            }
        ];
    }

    private static List<MaintenanceRequest> CreateMaintenanceRequests(
        IReadOnlyList<UserProfile> users,
        IReadOnlyList<RentalUnit> units,
        DateTime timestampUtc)
    {
        var tenantOne = users.Single(user => user.Email == "tenant1@example.com");
        var tenantTwo = users.Single(user => user.Email == "tenant2@example.com");
        var staffOne = users.Single(user => user.Email == "staff1@example.com");
        var staffTwo = users.Single(user => user.Email == "staff2@example.com");

        return
        [
            new()
            {
                RentalUnitId = units[0].Id,
                RentalUnit = units[0],
                TenantProfileId = tenantOne.Id,
                TenantProfile = tenantOne,
                AssignedStaffProfileId = staffOne.Id,
                AssignedStaffProfile = staffOne,
                Title = "Kitchen sink leaking",
                Description = "Water is leaking from the cabinet below the kitchen sink.",
                Category = MaintenanceCategory.Plumbing,
                Priority = MaintenancePriority.High,
                Status = MaintenanceStatus.Assigned,
                CreatedAtUtc = timestampUtc.AddDays(-4),
                UpdatedAtUtc = timestampUtc.AddDays(-3)
            },
            new()
            {
                RentalUnitId = units[1].Id,
                RentalUnit = units[1],
                TenantProfileId = tenantTwo.Id,
                TenantProfile = tenantTwo,
                AssignedStaffProfileId = staffTwo.Id,
                AssignedStaffProfile = staffTwo,
                Title = "Air conditioner not cooling",
                Description = "The bedroom air conditioner powers on but does not cool the room.",
                Category = MaintenanceCategory.HVAC,
                Priority = MaintenancePriority.Medium,
                Status = MaintenanceStatus.InProgress,
                CreatedAtUtc = timestampUtc.AddDays(-3),
                UpdatedAtUtc = timestampUtc.AddDays(-1)
            },
            new()
            {
                RentalUnitId = units[2].Id,
                RentalUnit = units[2],
                TenantProfileId = tenantOne.Id,
                TenantProfile = tenantOne,
                Title = "Lobby access card issue",
                Description = "The tenant access card sometimes fails at the lobby reader.",
                Category = MaintenanceCategory.Security,
                Priority = MaintenancePriority.Low,
                Status = MaintenanceStatus.UnderReview,
                CreatedAtUtc = timestampUtc.AddDays(-2),
                UpdatedAtUtc = timestampUtc.AddDays(-2)
            },
            new()
            {
                RentalUnitId = units[3].Id,
                RentalUnit = units[3],
                TenantProfileId = tenantTwo.Id,
                TenantProfile = tenantTwo,
                AssignedStaffProfileId = staffOne.Id,
                AssignedStaffProfile = staffOne,
                Title = "Bathroom light flickering",
                Description = "The bathroom ceiling light flickers after being on for a few minutes.",
                Category = MaintenanceCategory.Electrical,
                Priority = MaintenancePriority.Medium,
                Status = MaintenanceStatus.Submitted,
                CreatedAtUtc = timestampUtc.AddDays(-1)
            }
        ];
    }

    private static List<TenantUnitAssignment> CreateTenantUnitAssignments(
        IReadOnlyList<UserProfile> users,
        IReadOnlyList<RentalUnit> units,
        DateTime timestampUtc)
    {
        var tenantOne = users.Single(user => user.Email == "tenant1@example.com");
        var tenantTwo = users.Single(user => user.Email == "tenant2@example.com");

        return
        [
            new()
            {
                TenantProfileId = tenantOne.Id,
                TenantProfile = tenantOne,
                RentalUnitId = units[0].Id,
                RentalUnit = units[0],
                LeaseStartDateUtc = timestampUtc.AddMonths(-8),
                IsActive = true,
                CreatedAtUtc = timestampUtc
            },
            new()
            {
                TenantProfileId = tenantOne.Id,
                TenantProfile = tenantOne,
                RentalUnitId = units[2].Id,
                RentalUnit = units[2],
                LeaseStartDateUtc = timestampUtc.AddMonths(-4),
                IsActive = true,
                CreatedAtUtc = timestampUtc
            },
            new()
            {
                TenantProfileId = tenantTwo.Id,
                TenantProfile = tenantTwo,
                RentalUnitId = units[1].Id,
                RentalUnit = units[1],
                LeaseStartDateUtc = timestampUtc.AddMonths(-5),
                IsActive = true,
                CreatedAtUtc = timestampUtc
            }
        ];
    }

    private static List<MaintenanceRequestComment> CreateComments(
        IReadOnlyList<UserProfile> users,
        IReadOnlyList<MaintenanceRequest> requests,
        DateTime timestampUtc)
    {
        var manager = users.Single(user => user.Email == "manager1@example.com");
        var tenantOne = users.Single(user => user.Email == "tenant1@example.com");
        var staffOne = users.Single(user => user.Email == "staff1@example.com");
        var staffTwo = users.Single(user => user.Email == "staff2@example.com");

        return
        [
            new()
            {
                MaintenanceRequestId = requests[0].Id,
                MaintenanceRequest = requests[0],
                UserProfileId = tenantOne.Id,
                UserProfile = tenantOne,
                CommentText = "Leak is active when the tap is used.",
                IsInternal = false,
                CreatedAtUtc = timestampUtc.AddDays(-4).AddHours(1)
            },
            new()
            {
                MaintenanceRequestId = requests[0].Id,
                MaintenanceRequest = requests[0],
                UserProfileId = staffOne.Id,
                UserProfile = staffOne,
                CommentText = "Inspection scheduled for tomorrow morning.",
                IsInternal = false,
                CreatedAtUtc = timestampUtc.AddDays(-3)
            },
            new()
            {
                MaintenanceRequestId = requests[1].Id,
                MaintenanceRequest = requests[1],
                UserProfileId = staffTwo.Id,
                UserProfile = staffTwo,
                CommentText = "Filter cleaned; cooling performance will be checked again.",
                IsInternal = false,
                CreatedAtUtc = timestampUtc.AddDays(-1)
            },
            new()
            {
                MaintenanceRequestId = requests[2].Id,
                MaintenanceRequest = requests[2],
                UserProfileId = manager.Id,
                UserProfile = manager,
                CommentText = "Check access control logs before assigning staff.",
                IsInternal = true,
                CreatedAtUtc = timestampUtc.AddDays(-2).AddHours(2)
            }
        ];
    }

    private static List<MaintenanceRequestAttachment> CreateAttachments(
        IReadOnlyList<UserProfile> users,
        IReadOnlyList<MaintenanceRequest> requests,
        DateTime timestampUtc)
    {
        var tenantOne = users.Single(user => user.Email == "tenant1@example.com");

        return
        [
            new()
            {
                MaintenanceRequestId = requests[0].Id,
                MaintenanceRequest = requests[0],
                UploadedByUserProfileId = tenantOne.Id,
                UploadedByUserProfile = tenantOne,
                FileName = "kitchen-sink-leak-demo.jpg",
                ContentType = "image/jpeg",
                StorageKey = $"future-s3/demo-maintenance/{requests[0].Id}/kitchen-sink-leak-demo.jpg",
                UploadedAtUtc = timestampUtc.AddDays(-4).AddMinutes(30)
            }
        ];
    }
}
