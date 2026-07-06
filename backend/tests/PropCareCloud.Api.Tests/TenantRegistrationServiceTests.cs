using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Controllers;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.TenantRegistrations;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class TenantRegistrationServiceTests
{
    [Fact]
    public async Task SubmitAsync_CreatesPendingRegistration()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = CreateService(dbContext);

        var created = await service.SubmitAsync(CreateSubmitRequest());

        Assert.Equal(TenantRegistrationStatus.Pending, created.Status);
        Assert.Equal("lina.tenant@example.com", created.Email);
        Assert.Equal(1, await dbContext.TenantRegistrationRequests.CountAsync());
        Assert.Equal(0, await dbContext.AuthUserAccounts.CountAsync());
    }

    [Fact]
    public async Task SubmitAsync_RejectsDuplicatePendingRegistrationForSameEmail()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = CreateService(dbContext);
        await service.SubmitAsync(CreateSubmitRequest());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitAsync(CreateSubmitRequest()));
    }

    [Theory]
    [InlineData("", "Tenant", "lina.tenant@example.com")]
    [InlineData("Lina", "", "lina.tenant@example.com")]
    [InlineData("Lina", "Tenant", "not-an-email")]
    public async Task SubmitAsync_RejectsMissingRequiredFields(
        string firstName,
        string lastName,
        string email)
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitAsync(new TenantRegistrationSubmitRequest
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email
            }));
    }

    [Fact]
    public async Task SubmitAsync_RejectsActiveAccountEmail()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedRegistrationGraphAsync(dbContext);
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitAsync(new TenantRegistrationSubmitRequest
            {
                FirstName = "Existing",
                LastName = "Tenant",
                Email = seed.ExistingTenant.Email
            }));
    }

    [Theory]
    [InlineData(UserRole.Tenant)]
    [InlineData(UserRole.MaintenanceStaff)]
    public async Task ReviewOperations_BlockTenantAndMaintenanceStaff(UserRole blockedRole)
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedRegistrationGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id, blockedRole);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetRegistrationsAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApproveAsync(seed.PendingRegistration.Id, new TenantRegistrationApproveRequest
            {
                RentalUnitId = seed.AvailableUnit.Id,
                TemporaryPassword = "TenantPass123!"
            }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RejectAsync(seed.PendingRegistration.Id, new TenantRegistrationRejectRequest()));
    }

    [Fact]
    public async Task GetRegistrationsAsync_AdminCanListPendingRegistrations()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedRegistrationGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id, UserRole.AdminOwner);

        var pending = await service.GetRegistrationsAsync(TenantRegistrationStatus.Pending);

        Assert.Single(pending);
        Assert.Equal(seed.PendingRegistration.Email, pending[0].Email);
    }

    [Fact]
    public async Task GetRegistrationsAsync_ManagerCanListPendingRegistrations()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedRegistrationGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Manager.Id, UserRole.PropertyManager);

        var pending = await service.GetRegistrationsAsync(TenantRegistrationStatus.Pending);

        Assert.Single(pending);
    }

    [Fact]
    public async Task ApproveAsync_AdminCreatesTenantAccountAndAssignment()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedRegistrationGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id, UserRole.AdminOwner);
        const string temporaryPassword = "ApprovedTenant123!";

        var approved = await service.ApproveAsync(seed.PendingRegistration.Id, new TenantRegistrationApproveRequest
        {
            RentalUnitId = seed.AvailableUnit.Id,
            TemporaryPassword = temporaryPassword,
            ReviewNote = "Approved for available unit."
        });

        Assert.NotNull(approved);
        Assert.Equal(TenantRegistrationStatus.Approved, approved.Status);
        Assert.Equal(seed.AvailableUnit.Id, approved.ApprovedRentalUnitId);
        Assert.Equal(seed.Admin.Id, approved.ReviewedByUserProfileId);

        var account = await dbContext.AuthUserAccounts
            .Include(existing => existing.UserProfile)
            .SingleAsync(existing => existing.Email == seed.PendingRegistration.Email);
        Assert.Equal(UserRole.Tenant, account.UserProfile?.Role);
        Assert.StartsWith("$2", account.PasswordHash);
        Assert.DoesNotContain(temporaryPassword, account.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(temporaryPassword, account.PasswordHash));

        var assignment = await dbContext.TenantUnitAssignments
            .SingleAsync(existing => existing.RentalUnitId == seed.AvailableUnit.Id);
        Assert.Equal(account.UserProfileId, assignment.TenantProfileId);
        Assert.True(assignment.IsActive);
    }

    [Fact]
    public async Task ApproveAsync_ManagerCanApprovePendingRegistration()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedRegistrationGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Manager.Id, UserRole.PropertyManager);

        var approved = await service.ApproveAsync(seed.PendingRegistration.Id, new TenantRegistrationApproveRequest
        {
            RentalUnitId = seed.AvailableUnit.Id,
            TemporaryPassword = "ManagerApproved123!"
        });

        Assert.NotNull(approved);
        Assert.Equal(TenantRegistrationStatus.Approved, approved.Status);
        Assert.Equal(seed.Manager.Id, approved.ReviewedByUserProfileId);
    }

    [Fact]
    public async Task ApproveAsync_PreventsAssigningAlreadyOccupiedUnit()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedRegistrationGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id, UserRole.AdminOwner);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApproveAsync(seed.PendingRegistration.Id, new TenantRegistrationApproveRequest
            {
                RentalUnitId = seed.OccupiedUnit.Id,
                TemporaryPassword = "TenantPass123!"
            }));
    }

    [Fact]
    public async Task RejectAsync_MarksRequestRejectedWithoutCreatingAccountOrAssignment()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedRegistrationGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id, UserRole.AdminOwner);

        var rejected = await service.RejectAsync(seed.PendingRegistration.Id, new TenantRegistrationRejectRequest
        {
            ReviewNote = "Applicant could not be verified."
        });

        Assert.NotNull(rejected);
        Assert.Equal(TenantRegistrationStatus.Rejected, rejected.Status);
        Assert.Equal("Applicant could not be verified.", rejected.ReviewNote);
        Assert.False(await dbContext.AuthUserAccounts
            .AnyAsync(account => account.Email == seed.PendingRegistration.Email));
        Assert.False(await dbContext.TenantUnitAssignments
            .AnyAsync(assignment => assignment.RentalUnitId == seed.AvailableUnit.Id));
    }

    [Fact]
    public void TenantRegistrationsController_UsesExpectedAuthorization()
    {
        var submitMethod = typeof(TenantRegistrationsController).GetMethod(nameof(TenantRegistrationsController.Submit));
        var getAllMethod = typeof(TenantRegistrationsController).GetMethod(nameof(TenantRegistrationsController.GetAll));
        var approveMethod = typeof(TenantRegistrationsController).GetMethod(nameof(TenantRegistrationsController.Approve));
        var rejectMethod = typeof(TenantRegistrationsController).GetMethod(nameof(TenantRegistrationsController.Reject));

        Assert.NotNull(submitMethod);
        Assert.NotNull(getAllMethod);
        Assert.NotNull(approveMethod);
        Assert.NotNull(rejectMethod);
        Assert.Contains(submitMethod!.GetCustomAttributes(inherit: true), attribute => attribute is AllowAnonymousAttribute);
        Assert.Equal("AdminOrManager", GetAuthorizePolicy(getAllMethod!));
        Assert.Equal("AdminOrManager", GetAuthorizePolicy(approveMethod!));
        Assert.Equal("AdminOrManager", GetAuthorizePolicy(rejectMethod!));
    }

    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"propcare-tenant-registration-{Guid.NewGuid()}")
            .Options;
    }

    private static TenantRegistrationService CreateService(
        AppDbContext dbContext,
        Guid? userProfileId = null,
        UserRole? role = null)
    {
        return new TenantRegistrationService(
            dbContext,
            new FakeCurrentUserService(userProfileId, role));
    }

    private static TenantRegistrationSubmitRequest CreateSubmitRequest()
    {
        return new TenantRegistrationSubmitRequest
        {
            FirstName = "Lina",
            LastName = "Tenant",
            Email = "lina.tenant@example.com",
            PhoneNumber = "+60010000100",
            RequestedPropertyOrUnit = "Cloud Residence - Unit A-0303",
            Note = "I need portal access for my rental unit."
        };
    }

    private static async Task<TenantRegistrationSeed> SeedRegistrationGraphAsync(AppDbContext dbContext)
    {
        var timestampUtc = DateTime.UtcNow;
        var property = new Property
        {
            Name = "Cloud Residence",
            AddressLine1 = "12 Innovation Avenue",
            City = "Kuala Lumpur",
            Country = "Malaysia",
            Status = PropertyStatus.Active,
            CreatedAtUtc = timestampUtc
        };
        var availableUnit = new RentalUnit
        {
            PropertyId = property.Id,
            Property = property,
            UnitNumber = "A-0303",
            Floor = "3",
            Bedrooms = 2,
            Status = UnitStatus.Available,
            CreatedAtUtc = timestampUtc
        };
        var occupiedUnit = new RentalUnit
        {
            PropertyId = property.Id,
            Property = property,
            UnitNumber = "A-0205",
            Floor = "2",
            Bedrooms = 3,
            Status = UnitStatus.Occupied,
            CreatedAtUtc = timestampUtc
        };
        var admin = CreateUser("Amina Owner", "admin@example.com", UserRole.AdminOwner);
        var manager = CreateUser("Daniel Manager", "manager@example.com", UserRole.PropertyManager);
        var existingTenant = CreateUser("Existing Tenant", "existing.tenant@example.com", UserRole.Tenant);
        var occupiedAssignment = new TenantUnitAssignment
        {
            TenantProfileId = existingTenant.Id,
            TenantProfile = existingTenant,
            RentalUnitId = occupiedUnit.Id,
            RentalUnit = occupiedUnit,
            LeaseStartDateUtc = timestampUtc.AddMonths(-6),
            IsActive = true,
            CreatedAtUtc = timestampUtc
        };
        var pendingRegistration = new TenantRegistrationRequest
        {
            FirstName = "Lina",
            LastName = "Tenant",
            Email = "lina.tenant@example.com",
            PhoneNumber = "+60010000100",
            RequestedPropertyOrUnit = "Cloud Residence - Unit A-0303",
            Note = "I need portal access for my rental unit.",
            Status = TenantRegistrationStatus.Pending,
            SubmittedAtUtc = timestampUtc
        };

        dbContext.Properties.Add(property);
        dbContext.RentalUnits.AddRange(availableUnit, occupiedUnit);
        dbContext.UserProfiles.AddRange(admin, manager, existingTenant);
        dbContext.AuthUserAccounts.AddRange(
            CreateAccount(admin),
            CreateAccount(manager),
            CreateAccount(existingTenant));
        dbContext.TenantUnitAssignments.Add(occupiedAssignment);
        dbContext.TenantRegistrationRequests.Add(pendingRegistration);
        await dbContext.SaveChangesAsync();

        return new TenantRegistrationSeed(
            Admin: admin,
            Manager: manager,
            ExistingTenant: existingTenant,
            AvailableUnit: availableUnit,
            OccupiedUnit: occupiedUnit,
            PendingRegistration: pendingRegistration);
    }

    private static UserProfile CreateUser(string fullName, string email, UserRole role)
    {
        return new UserProfile
        {
            FullName = fullName,
            Email = email,
            Role = role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static AuthUserAccount CreateAccount(UserProfile userProfile)
    {
        return new AuthUserAccount
        {
            UserProfileId = userProfile.Id,
            UserProfile = userProfile,
            Email = userProfile.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static string? GetAuthorizePolicy(System.Reflection.MethodInfo method)
    {
        return method
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .Single()
            .Policy;
    }

    private sealed record TenantRegistrationSeed(
        UserProfile Admin,
        UserProfile Manager,
        UserProfile ExistingTenant,
        RentalUnit AvailableUnit,
        RentalUnit OccupiedUnit,
        TenantRegistrationRequest PendingRegistration);

    private sealed class FakeCurrentUserService(Guid? userProfileId, UserRole? role) : ICurrentUserService
    {
        public bool IsAuthenticated => userProfileId is not null && role is not null;
        public Guid? UserProfileId => userProfileId;
        public string? Email => null;
        public UserRole? Role => role;
        public bool IsAdminOwner => role == UserRole.AdminOwner;
        public bool IsPropertyManager => role == UserRole.PropertyManager;
        public bool IsTenant => role == UserRole.Tenant;
        public bool IsMaintenanceStaff => role == UserRole.MaintenanceStaff;
        public bool IsAdminOrManager => role is UserRole.AdminOwner or UserRole.PropertyManager;

        public bool HasRole(params UserRole[] roles)
        {
            return role is not null && roles.Contains(role.Value);
        }
    }
}
