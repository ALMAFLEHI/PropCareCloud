using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PropCareCloud.Api.Controllers;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.Auth;
using PropCareCloud.Api.DTOs.UserManagement;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class UserManagementServiceTests
{
    [Fact]
    public async Task GetUsersAsync_AdminCanListUsers()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);

        var users = await service.GetUsersAsync();

        Assert.Equal(4, users.Count);
        Assert.Contains(users, user => user.Email == "admin@example.com" && user.Role == UserRole.AdminOwner);
        Assert.Contains(users, user => user.Email == "tenant@example.com" && user.ActiveUnitCount == 1);
        Assert.Contains(users, user => user.Email == "staff@example.com" && user.RequestCount == 1);
    }

    [Theory]
    [InlineData(UserRole.PropertyManager)]
    [InlineData(UserRole.Tenant)]
    [InlineData(UserRole.MaintenanceStaff)]
    public void UserManagementController_BlocksNonAdminRoles(UserRole blockedRole)
    {
        var authorizeAttribute = typeof(UserManagementController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal("AdminOnly", authorizeAttribute.Policy);
        Assert.NotEqual(UserRole.AdminOwner, blockedRole);
    }

    [Fact]
    public async Task CreateInternalUserAsync_CreatesPropertyManagerAccount()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);

        var created = await service.CreateInternalUserAsync(new CreateInternalUserRequest
        {
            FullName = "New Manager",
            Email = "new.manager@example.com",
            Password = "ManagerPass123!",
            Role = UserRole.PropertyManager
        });

        Assert.Equal(UserRole.PropertyManager, created.Role);
        Assert.True(created.IsActive);
        Assert.Contains(await dbContext.AuthUserAccounts.ToListAsync(), account =>
            account.Email == "new.manager@example.com");
    }

    [Fact]
    public async Task CreateInternalUserAsync_CreatesMaintenanceStaffAccount()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);

        var created = await service.CreateInternalUserAsync(new CreateInternalUserRequest
        {
            FullName = "New Staff",
            Email = "new.staff@example.com",
            Password = "StaffPass123!",
            Role = UserRole.MaintenanceStaff
        });

        Assert.Equal(UserRole.MaintenanceStaff, created.Role);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task CreateInternalUserAsync_RejectsTenantRole()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateInternalUserAsync(new CreateInternalUserRequest
            {
                FullName = "Direct Tenant",
                Email = "direct.tenant@example.com",
                Password = "TenantPass123!",
                Role = UserRole.Tenant
            }));
    }

    [Fact]
    public async Task CreateInternalUserAsync_RejectsDuplicateEmail()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateInternalUserAsync(new CreateInternalUserRequest
            {
                FullName = "Duplicate Manager",
                Email = "manager@example.com",
                Password = "ManagerPass123!",
                Role = UserRole.PropertyManager
            }));
    }

    [Fact]
    public async Task CreateInternalUserAsync_StoresPasswordAsHash()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);
        const string plainPassword = "SafeStaffPass123!";

        var created = await service.CreateInternalUserAsync(new CreateInternalUserRequest
        {
            FullName = "Hash Checked Staff",
            Email = "hash.staff@example.com",
            Password = plainPassword,
            Role = UserRole.MaintenanceStaff
        });
        var account = await dbContext.AuthUserAccounts.SingleAsync(account =>
            account.UserProfileId == created.UserProfileId);

        Assert.StartsWith("$2", account.PasswordHash);
        Assert.DoesNotContain(plainPassword, account.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(plainPassword, account.PasswordHash));
    }

    [Fact]
    public async Task DisabledAccount_CannotLogin()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var userService = CreateService(dbContext, seed.Admin.Id);
        var authService = new AuthService(dbContext, CreateConfiguration());

        await userService.UpdateAccountStatusAsync(seed.Staff.Id, isActive: false);
        var login = await authService.LoginAsync(new LoginRequest
        {
            Email = "staff@example.com",
            Password = SeedPassword
        });

        Assert.False(login.Success);
        Assert.Equal("Account is disabled. Contact an administrator.", login.Message);
        Assert.Null(login.Token);
    }

    [Fact]
    public async Task UpdateAccountStatusAsync_RejectsDisablingOwnAccount()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAccountStatusAsync(seed.Admin.Id, isActive: false));
    }

    [Fact]
    public async Task UpdateAccountStatusAsync_RejectsDisablingLastActiveAdminOwner()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Manager.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAccountStatusAsync(seed.Admin.Id, isActive: false));
    }

    [Fact]
    public async Task UpdateAccountStatusAsync_ReactivatesDisabledAccount()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);

        await service.UpdateAccountStatusAsync(seed.Staff.Id, isActive: false);
        await service.UpdateAccountStatusAsync(seed.Staff.Id, isActive: true);

        var account = await dbContext.AuthUserAccounts
            .Include(authAccount => authAccount.UserProfile)
            .SingleAsync(authAccount => authAccount.UserProfileId == seed.Staff.Id);
        Assert.True(account.IsActive);
        Assert.True(account.UserProfile?.IsActive);
    }

    [Fact]
    public async Task ResetPasswordAsync_UpdatesHashAndNewPasswordWorks()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var userService = CreateService(dbContext, seed.Admin.Id);
        var authService = new AuthService(dbContext, CreateConfiguration());
        const string newPassword = "ManagerNewPass123!";

        var updated = await userService.ResetPasswordAsync(seed.Manager.Id, new ResetUserPasswordRequest
        {
            NewPassword = newPassword
        });
        var oldLogin = await authService.LoginAsync(new LoginRequest
        {
            Email = "manager@example.com",
            Password = SeedPassword
        });
        var newLogin = await authService.LoginAsync(new LoginRequest
        {
            Email = "manager@example.com",
            Password = newPassword
        });

        Assert.True(updated);
        Assert.False(oldLogin.Success);
        Assert.True(newLogin.Success);
    }

    [Fact]
    public async Task AssignTenantToUnitAsync_RejectsNonTenantProfile()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignTenantToUnitAsync(new CreateTenantUnitAssignmentRequest
            {
                TenantProfileId = seed.Manager.Id,
                RentalUnitId = seed.AvailableUnit.Id
            }));
    }

    [Fact]
    public async Task AssignTenantToUnitAsync_RejectsAlreadyAssignedUnit()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignTenantToUnitAsync(new CreateTenantUnitAssignmentRequest
            {
                TenantProfileId = seed.SecondTenant.Id,
                RentalUnitId = seed.AssignedUnit.Id
            }));
    }

    [Fact]
    public async Task AssignTenantToUnitAsync_AllowsTenantToHaveMultipleActiveUnits()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);

        await service.AssignTenantToUnitAsync(new CreateTenantUnitAssignmentRequest
        {
            TenantProfileId = seed.Tenant.Id,
            RentalUnitId = seed.AvailableUnit.Id
        });
        var activeAssignments = await dbContext.TenantUnitAssignments
            .CountAsync(assignment =>
                assignment.TenantProfileId == seed.Tenant.Id &&
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null);

        Assert.Equal(2, activeAssignments);
    }

    [Fact]
    public async Task EndTenantUnitAssignmentAsync_MakesUnitAvailableAgain()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var seed = await SeedUserManagementGraphAsync(dbContext);
        var service = CreateService(dbContext, seed.Admin.Id);

        var ended = await service.EndTenantUnitAssignmentAsync(
            seed.Assignment.Id,
            new EndTenantUnitAssignmentRequest());
        var availableUnits = await service.GetAvailableUnitsAsync();

        Assert.True(ended);
        Assert.Contains(availableUnits, unit => unit.RentalUnitId == seed.AssignedUnit.Id);
    }

    private const string SeedPassword = "Password123!";

    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"propcare-user-management-{Guid.NewGuid()}")
            .Options;
    }

    private static UserManagementService CreateService(AppDbContext dbContext, Guid adminProfileId)
    {
        return new UserManagementService(
            dbContext,
            new FakeCurrentUserService(adminProfileId, UserRole.AdminOwner));
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "PropCareCloud",
                ["Jwt:Audience"] = "PropCareCloud.Frontend",
                ["Jwt:SigningKey"] = "UnitTestJwtSigningKeyForPropCareCloudSprintTen"
            })
            .Build();
    }

    private static async Task<UserManagementSeed> SeedUserManagementGraphAsync(AppDbContext dbContext)
    {
        var property = new Property
        {
            Name = "Access Residence",
            AddressLine1 = "10 Admin Avenue",
            City = "Kuala Lumpur",
            Country = "Malaysia",
            Status = PropertyStatus.Active
        };
        var assignedUnit = new RentalUnit
        {
            PropertyId = property.Id,
            Property = property,
            UnitNumber = "A-0101",
            Floor = "1",
            Bedrooms = 2,
            Status = UnitStatus.Occupied
        };
        var availableUnit = new RentalUnit
        {
            PropertyId = property.Id,
            Property = property,
            UnitNumber = "A-0202",
            Floor = "2",
            Bedrooms = 1,
            Status = UnitStatus.Available
        };
        var admin = CreateUser("Admin User", "admin@example.com", UserRole.AdminOwner);
        var manager = CreateUser("Manager User", "manager@example.com", UserRole.PropertyManager);
        var tenant = CreateUser("Tenant User", "tenant@example.com", UserRole.Tenant);
        var secondTenant = CreateUser("Second Tenant", "tenant.two@example.com", UserRole.Tenant);
        var staff = CreateUser("Staff User", "staff@example.com", UserRole.MaintenanceStaff);
        var assignment = new TenantUnitAssignment
        {
            TenantProfileId = tenant.Id,
            TenantProfile = tenant,
            RentalUnitId = assignedUnit.Id,
            RentalUnit = assignedUnit,
            IsActive = true,
            LeaseStartDateUtc = DateTime.UtcNow.AddMonths(-2)
        };
        var request = new MaintenanceRequest
        {
            RentalUnitId = assignedUnit.Id,
            RentalUnit = assignedUnit,
            TenantProfileId = tenant.Id,
            TenantProfile = tenant,
            AssignedStaffProfileId = staff.Id,
            AssignedStaffProfile = staff,
            Title = "Assigned repair",
            Description = "Repair assigned to staff.",
            Category = MaintenanceCategory.Other,
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceStatus.Assigned
        };

        dbContext.Properties.Add(property);
        dbContext.RentalUnits.AddRange(assignedUnit, availableUnit);
        dbContext.UserProfiles.AddRange(admin, manager, tenant, secondTenant, staff);
        dbContext.AuthUserAccounts.AddRange(
            CreateAccount(admin),
            CreateAccount(manager),
            CreateAccount(tenant),
            CreateAccount(staff));
        dbContext.TenantUnitAssignments.Add(assignment);
        dbContext.MaintenanceRequests.Add(request);
        await dbContext.SaveChangesAsync();

        return new UserManagementSeed(
            admin,
            manager,
            tenant,
            secondTenant,
            staff,
            assignedUnit,
            availableUnit,
            assignment);
    }

    private static UserProfile CreateUser(string fullName, string email, UserRole role)
    {
        return new UserProfile
        {
            FullName = fullName,
            Email = email,
            Role = role,
            IsActive = true
        };
    }

    private static AuthUserAccount CreateAccount(UserProfile userProfile)
    {
        return new AuthUserAccount
        {
            UserProfileId = userProfile.Id,
            UserProfile = userProfile,
            Email = userProfile.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(SeedPassword),
            IsActive = true
        };
    }

    private sealed record UserManagementSeed(
        UserProfile Admin,
        UserProfile Manager,
        UserProfile Tenant,
        UserProfile SecondTenant,
        UserProfile Staff,
        RentalUnit AssignedUnit,
        RentalUnit AvailableUnit,
        TenantUnitAssignment Assignment);

    private sealed class FakeCurrentUserService(Guid userProfileId, UserRole role) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserProfileId => userProfileId;
        public string? Email => "admin@example.com";
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
