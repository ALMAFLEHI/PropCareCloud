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

public sealed class SeedDataService(
    AppDbContext dbContext,
    IAuthService authService) : ISeedDataService
{
    private static readonly DemoUserDefinition[] DemoUsers =
    [
        new(
            AccountEmail: "admin@propcare.demo",
            LegacySeedEmail: "admin.owner@example.com",
            FullName: "Amina Owner",
            PhoneNumber: "+60010000001",
            Role: UserRole.AdminOwner),
        new(
            AccountEmail: "manager@propcare.demo",
            LegacySeedEmail: "manager1@example.com",
            FullName: "Daniel Property Manager",
            PhoneNumber: "+60010000002",
            Role: UserRole.PropertyManager),
        new(
            AccountEmail: "tenant@propcare.demo",
            LegacySeedEmail: "tenant1@example.com",
            FullName: "Sara Tenant",
            PhoneNumber: "+60010000003",
            Role: UserRole.Tenant),
        new(
            AccountEmail: "imran@propcare.demo",
            LegacySeedEmail: "tenant2@example.com",
            FullName: "Imran Tenant",
            PhoneNumber: "+60010000004",
            Role: UserRole.Tenant),
        new(
            AccountEmail: "staff@propcare.demo",
            LegacySeedEmail: "staff1@example.com",
            FullName: "Nadia Maintenance Staff",
            PhoneNumber: "+60010000005",
            Role: UserRole.MaintenanceStaff)
    ];

    private static readonly DemoPropertyDefinition[] DemoProperties =
    [
        new(
            Name: "Cloud Residence",
            AddressLine1: "12 Innovation Avenue",
            AddressLine2: "Block A",
            City: "Kuala Lumpur",
            Country: "Malaysia",
            Status: PropertyStatus.Active),
        new(
            Name: "Harbor Heights",
            AddressLine1: "88 Marina Link",
            AddressLine2: null,
            City: "Penang",
            Country: "Malaysia",
            Status: PropertyStatus.Active)
    ];

    private static readonly DemoUnitDefinition[] DemoUnits =
    [
        new(
            PropertyName: "Cloud Residence",
            UnitNumber: "A-0101",
            Floor: "1",
            Bedrooms: 2,
            Status: UnitStatus.Occupied),
        new(
            PropertyName: "Cloud Residence",
            UnitNumber: "A-0205",
            Floor: "2",
            Bedrooms: 3,
            Status: UnitStatus.Occupied),
        new(
            PropertyName: "Harbor Heights",
            UnitNumber: "B-1102",
            Floor: "11",
            Bedrooms: 1,
            Status: UnitStatus.Occupied),
        new(
            PropertyName: "Harbor Heights",
            UnitNumber: "B-1208",
            Floor: "12",
            Bedrooms: 2,
            Status: UnitStatus.UnderMaintenance)
    ];

    private static readonly DemoTenantAssignmentDefinition[] DemoTenantAssignments =
    [
        new(TenantAccountEmail: "tenant@propcare.demo", UnitNumber: "A-0101", LeaseStartMonthsAgo: 8),
        new(TenantAccountEmail: "tenant@propcare.demo", UnitNumber: "B-1102", LeaseStartMonthsAgo: 4),
        new(TenantAccountEmail: "imran@propcare.demo", UnitNumber: "A-0205", LeaseStartMonthsAgo: 5),
        new(TenantAccountEmail: "imran@propcare.demo", UnitNumber: "B-1208", LeaseStartMonthsAgo: 3)
    ];

    private static readonly DemoRequestDefinition[] DemoRequests =
    [
        new(
            TenantAccountEmail: "tenant@propcare.demo",
            UnitNumber: "A-0101",
            AssignedStaffAccountEmail: "staff@propcare.demo",
            Title: "Kitchen sink leaking",
            Description: "Water is leaking from the cabinet below the kitchen sink.",
            Category: MaintenanceCategory.Plumbing,
            Priority: MaintenancePriority.High,
            Status: MaintenanceStatus.Assigned,
            CreatedDaysAgo: 4,
            UpdatedDaysAgo: 3),
        new(
            TenantAccountEmail: "imran@propcare.demo",
            UnitNumber: "A-0205",
            AssignedStaffAccountEmail: "staff@propcare.demo",
            Title: "Air conditioner not cooling",
            Description: "The bedroom air conditioner powers on but does not cool the room.",
            Category: MaintenanceCategory.HVAC,
            Priority: MaintenancePriority.Medium,
            Status: MaintenanceStatus.InProgress,
            CreatedDaysAgo: 3,
            UpdatedDaysAgo: 1),
        new(
            TenantAccountEmail: "tenant@propcare.demo",
            UnitNumber: "B-1102",
            AssignedStaffAccountEmail: null,
            Title: "Lobby access card issue",
            Description: "The tenant access card sometimes fails at the lobby reader.",
            Category: MaintenanceCategory.Security,
            Priority: MaintenancePriority.Low,
            Status: MaintenanceStatus.UnderReview,
            CreatedDaysAgo: 2,
            UpdatedDaysAgo: 2),
        new(
            TenantAccountEmail: "imran@propcare.demo",
            UnitNumber: "B-1208",
            AssignedStaffAccountEmail: "staff@propcare.demo",
            Title: "Bathroom light flickering",
            Description: "The bathroom ceiling light flickers after being on for a few minutes.",
            Category: MaintenanceCategory.Electrical,
            Priority: MaintenancePriority.Medium,
            Status: MaintenanceStatus.Submitted,
            CreatedDaysAgo: 1,
            UpdatedDaysAgo: null)
    ];

    private static readonly DemoCommentDefinition[] DemoComments =
    [
        new(
            RequestTitle: "Kitchen sink leaking",
            AuthorAccountEmail: "tenant@propcare.demo",
            CommentText: "Leak is active when the tap is used.",
            IsInternal: false,
            CreatedDaysAgo: 4,
            CreatedHoursOffset: 1),
        new(
            RequestTitle: "Kitchen sink leaking",
            AuthorAccountEmail: "staff@propcare.demo",
            CommentText: "Inspection scheduled for tomorrow morning.",
            IsInternal: false,
            CreatedDaysAgo: 3,
            CreatedHoursOffset: 0),
        new(
            RequestTitle: "Air conditioner not cooling",
            AuthorAccountEmail: "staff@propcare.demo",
            CommentText: "Filter cleaned; cooling performance will be checked again.",
            IsInternal: false,
            CreatedDaysAgo: 1,
            CreatedHoursOffset: 0),
        new(
            RequestTitle: "Lobby access card issue",
            AuthorAccountEmail: "manager@propcare.demo",
            CommentText: "Check access control logs before assigning staff.",
            IsInternal: true,
            CreatedDaysAgo: 2,
            CreatedHoursOffset: 2)
    ];

    private static readonly DemoAttachmentDefinition[] DemoAttachments =
    [
        new(
            RequestTitle: "Kitchen sink leaking",
            UploadedByAccountEmail: "tenant@propcare.demo",
            FileName: "kitchen-sink-leak-demo.jpg",
            ContentType: "image/jpeg",
            UploadedDaysAgo: 4,
            UploadedMinutesOffset: 30)
    ];

    public async Task<SeedDataResult> SeedDemoDataAsync(CancellationToken cancellationToken = default)
    {
        var timestampUtc = DateTime.UtcNow;
        var demoUserCountBefore = await CountDemoUsersAsync(cancellationToken);
        var stats = new SeedRepairStats();

        await authService.EnsureDemoAccountsAsync();

        var users = await EnsureDemoUsersAsync(timestampUtc, stats, cancellationToken);
        stats.UsersCreated += Math.Max(0, users.Count - demoUserCountBefore);

        var properties = await EnsureDemoPropertiesAsync(timestampUtc, stats, cancellationToken);
        var units = await EnsureDemoUnitsAsync(properties, timestampUtc, stats, cancellationToken);
        await EnsureDemoTenantAssignmentsAsync(users, units, timestampUtc, stats, cancellationToken);
        var requests = await EnsureDemoRequestsAsync(users, units, timestampUtc, stats, cancellationToken);
        await EnsureDemoCommentsAsync(users, requests, timestampUtc, stats, cancellationToken);
        await EnsureDemoAttachmentsAsync(users, requests, timestampUtc, stats, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var totals = await GetCurrentTotalsAsync(cancellationToken);
        var createdOrRepaired = stats.RecordsCreated > 0 || stats.RecordsRepaired > 0;

        return new SeedDataResult(
            Success: true,
            Message: createdOrRepaired
                ? "Demo seed data was created or repaired. The endpoint is safe to rerun and will not duplicate existing demo records."
                : "Demo seed data is already complete. No duplicate records were created.",
            UsersCreated: stats.UsersCreated,
            PropertiesCreated: stats.PropertiesCreated,
            UnitsCreated: stats.UnitsCreated,
            TenantAssignmentsCreated: stats.TenantAssignmentsCreated,
            RequestsCreated: stats.RequestsCreated,
            CommentsCreated: stats.CommentsCreated,
            AttachmentsCreated: stats.AttachmentsCreated,
            UsersTotal: totals.Users,
            PropertiesTotal: totals.Properties,
            UnitsTotal: totals.Units,
            TenantAssignmentsTotal: totals.TenantAssignments,
            RequestsTotal: totals.Requests,
            CommentsTotal: totals.Comments,
            AttachmentsTotal: totals.Attachments,
            RecordsCreated: stats.RecordsCreated,
            RecordsRepaired: stats.RecordsRepaired,
            CreatedOrRepaired: createdOrRepaired,
            SkippedBecauseAlreadySeeded: !createdOrRepaired,
            TimestampUtc: timestampUtc);
    }

    private async Task<Dictionary<string, UserProfile>> EnsureDemoUsersAsync(
        DateTime timestampUtc,
        SeedRepairStats stats,
        CancellationToken cancellationToken)
    {
        var users = new Dictionary<string, UserProfile>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in DemoUsers)
        {
            var user = await FindDemoUserAsync(definition, cancellationToken);
            if (user is null)
            {
                user = new UserProfile
                {
                    FullName = definition.FullName,
                    Email = definition.AccountEmail,
                    PhoneNumber = definition.PhoneNumber,
                    Role = definition.Role,
                    IsActive = true,
                    CreatedAtUtc = timestampUtc
                };
                dbContext.UserProfiles.Add(user);
                stats.UsersCreated++;
            }
            else
            {
                RepairUserProfile(user, definition, stats);
            }

            users[definition.AccountEmail] = user;
        }

        return users;
    }

    private async Task<UserProfile?> FindDemoUserAsync(
        DemoUserDefinition definition,
        CancellationToken cancellationToken)
    {
        var accountEmail = Normalize(definition.AccountEmail);
        var legacySeedEmail = Normalize(definition.LegacySeedEmail);

        var account = await dbContext.AuthUserAccounts
            .Include(authAccount => authAccount.UserProfile)
            .SingleOrDefaultAsync(
                authAccount => authAccount.Email.ToLower() == accountEmail,
                cancellationToken);
        if (account?.UserProfile is not null)
        {
            return account.UserProfile;
        }

        return await dbContext.UserProfiles
            .Where(user => user.Role == definition.Role)
            .OrderBy(user => user.Email.ToLower() == accountEmail ? 0 :
                user.Email.ToLower() == legacySeedEmail ? 1 : 2)
            .ThenBy(user => user.CreatedAtUtc)
            .FirstOrDefaultAsync(user =>
                user.Email.ToLower() == accountEmail ||
                user.Email.ToLower() == legacySeedEmail,
                cancellationToken);
    }

    private static void RepairUserProfile(
        UserProfile user,
        DemoUserDefinition definition,
        SeedRepairStats stats)
    {
        var repaired = false;
        repaired |= SetIfChanged(user.FullName, definition.FullName, value => user.FullName = value ?? string.Empty);
        repaired |= SetIfChanged(user.Email, definition.AccountEmail, value => user.Email = value ?? string.Empty);
        repaired |= SetIfChanged(user.PhoneNumber, definition.PhoneNumber, value => user.PhoneNumber = value);
        if (user.Role != definition.Role)
        {
            user.Role = definition.Role;
            repaired = true;
        }

        if (!user.IsActive)
        {
            user.IsActive = true;
            repaired = true;
        }

        if (repaired)
        {
            stats.RecordsRepaired++;
        }
    }

    private async Task<Dictionary<string, Property>> EnsureDemoPropertiesAsync(
        DateTime timestampUtc,
        SeedRepairStats stats,
        CancellationToken cancellationToken)
    {
        var properties = new Dictionary<string, Property>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in DemoProperties)
        {
            var property = await dbContext.Properties
                .OrderBy(property => property.CreatedAtUtc)
                .FirstOrDefaultAsync(
                    property => property.Name.ToLower() == Normalize(definition.Name),
                    cancellationToken);
            if (property is null)
            {
                property = new Property
                {
                    Name = definition.Name,
                    AddressLine1 = definition.AddressLine1,
                    AddressLine2 = definition.AddressLine2,
                    City = definition.City,
                    Country = definition.Country,
                    Status = definition.Status,
                    CreatedAtUtc = timestampUtc
                };
                dbContext.Properties.Add(property);
                stats.PropertiesCreated++;
            }
            else
            {
                RepairProperty(property, definition, stats);
            }

            properties[definition.Name] = property;
        }

        return properties;
    }

    private static void RepairProperty(
        Property property,
        DemoPropertyDefinition definition,
        SeedRepairStats stats)
    {
        var repaired = false;
        repaired |= SetIfChanged(property.AddressLine1, definition.AddressLine1, value => property.AddressLine1 = value ?? string.Empty);
        repaired |= SetIfChanged(property.AddressLine2, definition.AddressLine2, value => property.AddressLine2 = value);
        repaired |= SetIfChanged(property.City, definition.City, value => property.City = value ?? string.Empty);
        repaired |= SetIfChanged(property.Country, definition.Country, value => property.Country = value ?? string.Empty);
        if (property.Status != definition.Status)
        {
            property.Status = definition.Status;
            repaired = true;
        }

        if (repaired)
        {
            stats.RecordsRepaired++;
        }
    }

    private async Task<Dictionary<string, RentalUnit>> EnsureDemoUnitsAsync(
        IReadOnlyDictionary<string, Property> properties,
        DateTime timestampUtc,
        SeedRepairStats stats,
        CancellationToken cancellationToken)
    {
        var units = new Dictionary<string, RentalUnit>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in DemoUnits)
        {
            var property = properties[definition.PropertyName];
            var unit = await dbContext.RentalUnits
                .OrderBy(unit => unit.CreatedAtUtc)
                .FirstOrDefaultAsync(
                    unit => unit.UnitNumber.ToLower() == Normalize(definition.UnitNumber),
                    cancellationToken);
            if (unit is null)
            {
                unit = new RentalUnit
                {
                    PropertyId = property.Id,
                    Property = property,
                    UnitNumber = definition.UnitNumber,
                    Floor = definition.Floor,
                    Bedrooms = definition.Bedrooms,
                    Status = definition.Status,
                    CreatedAtUtc = timestampUtc
                };
                dbContext.RentalUnits.Add(unit);
                stats.UnitsCreated++;
            }
            else
            {
                RepairRentalUnit(unit, property, definition, stats);
            }

            units[definition.UnitNumber] = unit;
        }

        return units;
    }

    private static void RepairRentalUnit(
        RentalUnit unit,
        Property property,
        DemoUnitDefinition definition,
        SeedRepairStats stats)
    {
        var repaired = false;
        if (unit.PropertyId != property.Id)
        {
            unit.PropertyId = property.Id;
            unit.Property = property;
            repaired = true;
        }

        repaired |= SetIfChanged(unit.Floor, definition.Floor, value => unit.Floor = value);
        if (unit.Bedrooms != definition.Bedrooms)
        {
            unit.Bedrooms = definition.Bedrooms;
            repaired = true;
        }

        if (unit.Status != definition.Status)
        {
            unit.Status = definition.Status;
            repaired = true;
        }

        if (repaired)
        {
            stats.RecordsRepaired++;
        }
    }

    private async Task EnsureDemoTenantAssignmentsAsync(
        IReadOnlyDictionary<string, UserProfile> users,
        IReadOnlyDictionary<string, RentalUnit> units,
        DateTime timestampUtc,
        SeedRepairStats stats,
        CancellationToken cancellationToken)
    {
        foreach (var definition in DemoTenantAssignments)
        {
            var tenant = users[definition.TenantAccountEmail];
            var unit = units[definition.UnitNumber];

            var tenantAlreadyAssigned = await dbContext.TenantUnitAssignments
                .AnyAsync(assignment =>
                    assignment.TenantProfileId == tenant.Id &&
                    assignment.RentalUnitId == unit.Id &&
                    assignment.IsActive &&
                    assignment.LeaseEndDateUtc == null,
                    cancellationToken);
            if (tenantAlreadyAssigned)
            {
                continue;
            }

            var existingActiveAssignment = await dbContext.TenantUnitAssignments
                .Include(assignment => assignment.TenantProfile)
                .FirstOrDefaultAsync(assignment =>
                    assignment.RentalUnitId == unit.Id &&
                    assignment.IsActive &&
                    assignment.LeaseEndDateUtc == null,
                    cancellationToken);
            if (existingActiveAssignment is not null && !IsDemoTenant(existingActiveAssignment.TenantProfile))
            {
                continue;
            }

            if (existingActiveAssignment is not null)
            {
                existingActiveAssignment.IsActive = false;
                existingActiveAssignment.LeaseEndDateUtc = timestampUtc;
                stats.RecordsRepaired++;
            }

            dbContext.TenantUnitAssignments.Add(new TenantUnitAssignment
            {
                TenantProfileId = tenant.Id,
                TenantProfile = tenant,
                RentalUnitId = unit.Id,
                RentalUnit = unit,
                LeaseStartDateUtc = timestampUtc.AddMonths(-definition.LeaseStartMonthsAgo),
                IsActive = true,
                CreatedAtUtc = timestampUtc
            });
            stats.TenantAssignmentsCreated++;
        }
    }

    private async Task<Dictionary<string, MaintenanceRequest>> EnsureDemoRequestsAsync(
        IReadOnlyDictionary<string, UserProfile> users,
        IReadOnlyDictionary<string, RentalUnit> units,
        DateTime timestampUtc,
        SeedRepairStats stats,
        CancellationToken cancellationToken)
    {
        var requests = new Dictionary<string, MaintenanceRequest>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in DemoRequests)
        {
            var tenant = users[definition.TenantAccountEmail];
            var unit = units[definition.UnitNumber];
            var assignedStaff = definition.AssignedStaffAccountEmail is null
                ? null
                : users[definition.AssignedStaffAccountEmail];
            var request = await dbContext.MaintenanceRequests
                .OrderBy(request => request.CreatedAtUtc)
                .FirstOrDefaultAsync(request =>
                    request.TenantProfileId == tenant.Id &&
                    request.RentalUnitId == unit.Id &&
                    request.Title.ToLower() == Normalize(definition.Title),
                    cancellationToken);

            if (request is null)
            {
                request = new MaintenanceRequest
                {
                    RentalUnitId = unit.Id,
                    RentalUnit = unit,
                    TenantProfileId = tenant.Id,
                    TenantProfile = tenant,
                    AssignedStaffProfileId = assignedStaff?.Id,
                    AssignedStaffProfile = assignedStaff,
                    Title = definition.Title,
                    Description = definition.Description,
                    Category = definition.Category,
                    Priority = definition.Priority,
                    Status = definition.Status,
                    CreatedAtUtc = timestampUtc.AddDays(-definition.CreatedDaysAgo),
                    UpdatedAtUtc = definition.UpdatedDaysAgo is null
                        ? null
                        : timestampUtc.AddDays(-definition.UpdatedDaysAgo.Value)
                };
                dbContext.MaintenanceRequests.Add(request);
                stats.RequestsCreated++;
            }
            else
            {
                RepairMaintenanceRequest(request, unit, tenant, assignedStaff, definition, stats);
            }

            requests[definition.Title] = request;
        }

        return requests;
    }

    private static void RepairMaintenanceRequest(
        MaintenanceRequest request,
        RentalUnit unit,
        UserProfile tenant,
        UserProfile? assignedStaff,
        DemoRequestDefinition definition,
        SeedRepairStats stats)
    {
        var repaired = false;
        if (request.RentalUnitId != unit.Id)
        {
            request.RentalUnitId = unit.Id;
            request.RentalUnit = unit;
            repaired = true;
        }

        if (request.TenantProfileId != tenant.Id)
        {
            request.TenantProfileId = tenant.Id;
            request.TenantProfile = tenant;
            repaired = true;
        }

        if (request.AssignedStaffProfileId != assignedStaff?.Id)
        {
            request.AssignedStaffProfileId = assignedStaff?.Id;
            request.AssignedStaffProfile = assignedStaff;
            repaired = true;
        }

        repaired |= SetIfChanged(request.Description, definition.Description, value => request.Description = value ?? string.Empty);
        if (request.Category != definition.Category)
        {
            request.Category = definition.Category;
            repaired = true;
        }

        if (request.Priority != definition.Priority)
        {
            request.Priority = definition.Priority;
            repaired = true;
        }

        if (request.Status != definition.Status)
        {
            request.Status = definition.Status;
            repaired = true;
        }

        if (repaired)
        {
            stats.RecordsRepaired++;
        }
    }

    private async Task EnsureDemoCommentsAsync(
        IReadOnlyDictionary<string, UserProfile> users,
        IReadOnlyDictionary<string, MaintenanceRequest> requests,
        DateTime timestampUtc,
        SeedRepairStats stats,
        CancellationToken cancellationToken)
    {
        foreach (var definition in DemoComments)
        {
            var request = requests[definition.RequestTitle];
            var author = users[definition.AuthorAccountEmail];
            var exists = await dbContext.MaintenanceRequestComments.AnyAsync(comment =>
                comment.MaintenanceRequestId == request.Id &&
                comment.UserProfileId == author.Id &&
                comment.CommentText == definition.CommentText,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            dbContext.MaintenanceRequestComments.Add(new MaintenanceRequestComment
            {
                MaintenanceRequestId = request.Id,
                MaintenanceRequest = request,
                UserProfileId = author.Id,
                UserProfile = author,
                CommentText = definition.CommentText,
                IsInternal = definition.IsInternal,
                CreatedAtUtc = timestampUtc
                    .AddDays(-definition.CreatedDaysAgo)
                    .AddHours(definition.CreatedHoursOffset)
            });
            stats.CommentsCreated++;
        }
    }

    private async Task EnsureDemoAttachmentsAsync(
        IReadOnlyDictionary<string, UserProfile> users,
        IReadOnlyDictionary<string, MaintenanceRequest> requests,
        DateTime timestampUtc,
        SeedRepairStats stats,
        CancellationToken cancellationToken)
    {
        foreach (var definition in DemoAttachments)
        {
            var request = requests[definition.RequestTitle];
            var uploadedBy = users[definition.UploadedByAccountEmail];
            var exists = await dbContext.MaintenanceRequestAttachments.AnyAsync(attachment =>
                attachment.MaintenanceRequestId == request.Id &&
                attachment.FileName == definition.FileName,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            dbContext.MaintenanceRequestAttachments.Add(new MaintenanceRequestAttachment
            {
                MaintenanceRequestId = request.Id,
                MaintenanceRequest = request,
                UploadedByUserProfileId = uploadedBy.Id,
                UploadedByUserProfile = uploadedBy,
                FileName = definition.FileName,
                ContentType = definition.ContentType,
                StorageKey = $"future-s3/demo-maintenance/{request.Id}/{definition.FileName}",
                UploadedAtUtc = timestampUtc
                    .AddDays(-definition.UploadedDaysAgo)
                    .AddMinutes(definition.UploadedMinutesOffset)
            });
            stats.AttachmentsCreated++;
        }
    }

    private async Task<int> CountDemoUsersAsync(CancellationToken cancellationToken)
    {
        var accountEmails = DemoUsers.Select(user => Normalize(user.AccountEmail)).ToArray();
        var legacySeedEmails = DemoUsers.Select(user => Normalize(user.LegacySeedEmail)).ToArray();

        return await dbContext.UserProfiles.CountAsync(user =>
            accountEmails.Contains(user.Email.ToLower()) ||
            legacySeedEmails.Contains(user.Email.ToLower()) ||
            dbContext.AuthUserAccounts.Any(account =>
                account.UserProfileId == user.Id &&
                accountEmails.Contains(account.Email.ToLower())),
            cancellationToken);
    }

    private async Task<SeedDataTotals> GetCurrentTotalsAsync(CancellationToken cancellationToken)
    {
        return new SeedDataTotals(
            Users: await dbContext.UserProfiles.CountAsync(cancellationToken),
            Properties: await dbContext.Properties.CountAsync(cancellationToken),
            Units: await dbContext.RentalUnits.CountAsync(cancellationToken),
            TenantAssignments: await dbContext.TenantUnitAssignments
                .CountAsync(assignment => assignment.IsActive && assignment.LeaseEndDateUtc == null, cancellationToken),
            Requests: await dbContext.MaintenanceRequests.CountAsync(cancellationToken),
            Comments: await dbContext.MaintenanceRequestComments.CountAsync(cancellationToken),
            Attachments: await dbContext.MaintenanceRequestAttachments.CountAsync(cancellationToken));
    }

    private static bool IsDemoTenant(UserProfile? userProfile)
    {
        if (userProfile is null)
        {
            return false;
        }

        var normalizedEmail = Normalize(userProfile.Email);
        return DemoUsers.Any(user =>
            user.Role == UserRole.Tenant &&
            (Normalize(user.AccountEmail) == normalizedEmail ||
             Normalize(user.LegacySeedEmail) == normalizedEmail));
    }

    private static bool SetIfChanged(string? currentValue, string? newValue, Action<string?> assign)
    {
        if (string.Equals(currentValue, newValue, StringComparison.Ordinal))
        {
            return false;
        }

        assign(newValue);
        return true;
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private sealed class SeedRepairStats
    {
        public int UsersCreated { get; set; }
        public int PropertiesCreated { get; set; }
        public int UnitsCreated { get; set; }
        public int TenantAssignmentsCreated { get; set; }
        public int RequestsCreated { get; set; }
        public int CommentsCreated { get; set; }
        public int AttachmentsCreated { get; set; }
        public int RecordsRepaired { get; set; }

        public int RecordsCreated =>
            UsersCreated +
            PropertiesCreated +
            UnitsCreated +
            TenantAssignmentsCreated +
            RequestsCreated +
            CommentsCreated +
            AttachmentsCreated;
    }

    private sealed record SeedDataTotals(
        int Users,
        int Properties,
        int Units,
        int TenantAssignments,
        int Requests,
        int Comments,
        int Attachments);

    private sealed record DemoUserDefinition(
        string AccountEmail,
        string LegacySeedEmail,
        string FullName,
        string PhoneNumber,
        UserRole Role);

    private sealed record DemoPropertyDefinition(
        string Name,
        string AddressLine1,
        string? AddressLine2,
        string City,
        string Country,
        PropertyStatus Status);

    private sealed record DemoUnitDefinition(
        string PropertyName,
        string UnitNumber,
        string Floor,
        int Bedrooms,
        UnitStatus Status);

    private sealed record DemoTenantAssignmentDefinition(
        string TenantAccountEmail,
        string UnitNumber,
        int LeaseStartMonthsAgo);

    private sealed record DemoRequestDefinition(
        string TenantAccountEmail,
        string UnitNumber,
        string? AssignedStaffAccountEmail,
        string Title,
        string Description,
        MaintenanceCategory Category,
        MaintenancePriority Priority,
        MaintenanceStatus Status,
        int CreatedDaysAgo,
        int? UpdatedDaysAgo);

    private sealed record DemoCommentDefinition(
        string RequestTitle,
        string AuthorAccountEmail,
        string CommentText,
        bool IsInternal,
        int CreatedDaysAgo,
        int CreatedHoursOffset);

    private sealed record DemoAttachmentDefinition(
        string RequestTitle,
        string UploadedByAccountEmail,
        string FileName,
        string ContentType,
        int UploadedDaysAgo,
        int UploadedMinutesOffset);
}
