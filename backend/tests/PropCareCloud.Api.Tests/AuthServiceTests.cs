using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.Auth;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task EnsureDemoAccountsAsync_CreatesFiveHashedDemoAccounts()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = new AuthService(dbContext, CreateConfiguration());

        await service.EnsureDemoAccountsAsync();

        var accounts = await dbContext.AuthUserAccounts
            .Include(account => account.UserProfile)
            .ToListAsync();

        Assert.Equal(5, accounts.Count);
        Assert.All(accounts, account =>
        {
            Assert.NotNull(account.UserProfile);
            Assert.True(account.IsActive);
            Assert.StartsWith("$2", account.PasswordHash);
            Assert.DoesNotContain("PropCare@", account.PasswordHash);
        });
        Assert.Equal(5, accounts.Select(account => account.UserProfileId).Distinct().Count());
    }

    [Fact]
    public async Task EnsureDemoAccountsAsync_DoesNotDuplicateAccounts()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = new AuthService(dbContext, CreateConfiguration());

        await service.EnsureDemoAccountsAsync();
        await service.EnsureDemoAccountsAsync();

        Assert.Equal(5, await dbContext.AuthUserAccounts.CountAsync());
        Assert.Equal(5, await dbContext.UserProfiles.CountAsync());
    }

    [Fact]
    public async Task EnsureDemoAccountsAsync_CreatesSeparateTenantAccountsAndAssignments()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        await SeedOccupiedUnitsAsync(dbContext);
        var service = new AuthService(dbContext, CreateConfiguration());

        await service.EnsureDemoAccountsAsync();
        await service.EnsureDemoAccountsAsync();

        var saraAccount = await dbContext.AuthUserAccounts
            .Include(account => account.UserProfile)
            .SingleAsync(account => account.Email == "tenant@propcare.demo");
        var imranAccount = await dbContext.AuthUserAccounts
            .Include(account => account.UserProfile)
            .SingleAsync(account => account.Email == "imran@propcare.demo");
        var saraAssignments = await dbContext.TenantUnitAssignments
            .Include(assignment => assignment.RentalUnit)
            .Where(assignment => assignment.TenantProfileId == saraAccount.UserProfileId)
            .ToListAsync();
        var imranAssignments = await dbContext.TenantUnitAssignments
            .Include(assignment => assignment.RentalUnit)
            .Where(assignment => assignment.TenantProfileId == imranAccount.UserProfileId)
            .ToListAsync();
        var activeAssignments = await dbContext.TenantUnitAssignments
            .Where(assignment => assignment.IsActive && assignment.LeaseEndDateUtc == null)
            .ToListAsync();

        Assert.Equal(UserRole.Tenant, saraAccount.UserProfile?.Role);
        Assert.Equal(UserRole.Tenant, imranAccount.UserProfile?.Role);
        Assert.NotEqual(saraAccount.UserProfileId, imranAccount.UserProfileId);
        Assert.Single(saraAssignments);
        Assert.Single(imranAssignments);
        Assert.NotEqual(saraAssignments[0].RentalUnitId, imranAssignments[0].RentalUnitId);
        Assert.Equal("B-1102", saraAssignments[0].RentalUnit?.UnitNumber);
        Assert.Equal("A-0205", imranAssignments[0].RentalUnit?.UnitNumber);
        Assert.Equal(activeAssignments.Count, activeAssignments.Select(assignment => assignment.RentalUnitId).Distinct().Count());
    }

    [Fact]
    public async Task LoginAsync_SucceedsWithImranTenantCredential()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = new AuthService(dbContext, CreateConfiguration());
        await service.EnsureDemoAccountsAsync();

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "imran@propcare.demo",
            Password = "PropCare@Imran123"
        });

        Assert.True(response.Success);
        Assert.NotNull(response.User);
        Assert.Equal("Imran Tenant", response.User.FullName);
        Assert.Equal(UserRole.Tenant, response.User.Role);
    }

    [Fact]
    public async Task LoginAsync_SucceedsWithValidDemoCredential()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = new AuthService(dbContext, CreateConfiguration());
        await service.EnsureDemoAccountsAsync();

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "admin@propcare.demo",
            Password = "PropCare@Admin123"
        });

        Assert.True(response.Success);
        Assert.Equal("Login successful.", response.Message);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.NotNull(response.ExpiresAtUtc);
        Assert.NotNull(response.User);
        Assert.Equal(UserRole.AdminOwner, response.User.Role);
        Assert.Equal("Admin / Owner", response.User.RoleDisplayName);
        Assert.NotNull(await dbContext.AuthUserAccounts
            .Where(account => account.Email == "admin@propcare.demo")
            .Select(account => account.LastLoginAtUtc)
            .SingleAsync());
    }

    [Fact]
    public async Task LoginAsync_FailsWithWrongPassword()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = new AuthService(dbContext, CreateConfiguration());
        await service.EnsureDemoAccountsAsync();

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "admin@propcare.demo",
            Password = "WrongPassword123"
        });

        Assert.False(response.Success);
        Assert.Null(response.Token);
        Assert.Null(response.User);
    }

    [Fact]
    public async Task GetDemoCredentialsAsync_ReturnsAllFiveDemoAccounts()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = new AuthService(dbContext, CreateConfiguration());

        var credentials = await service.GetDemoCredentialsAsync();

        Assert.Equal(5, credentials.Count);
        Assert.Contains(credentials, credential => credential.Email == "admin@propcare.demo");
        Assert.Contains(credentials, credential => credential.Email == "manager@propcare.demo");
        Assert.Contains(credentials, credential => credential.Email == "tenant@propcare.demo");
        Assert.Contains(credentials, credential => credential.Email == "imran@propcare.demo");
        Assert.Contains(credentials, credential => credential.Email == "staff@propcare.demo");
        Assert.Contains(credentials, credential =>
            credential.Role == "Tenant - Sara" &&
            credential.Purpose.Contains("Primary tenant demo", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(credentials, credential =>
            credential.Role == "Tenant - Imran" &&
            credential.Purpose.Contains("isolation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsExpectedRole()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = new AuthService(dbContext, CreateConfiguration());
        await service.EnsureDemoAccountsAsync();
        var tenantProfileId = await dbContext.AuthUserAccounts
            .Where(account => account.Email == "tenant@propcare.demo")
            .Select(account => account.UserProfileId)
            .SingleAsync();

        var user = await service.GetCurrentUserAsync(tenantProfileId);

        Assert.NotNull(user);
        Assert.Equal(UserRole.Tenant, user.Role);
        Assert.Equal("Tenant", user.RoleDisplayName);
        Assert.Equal("tenant@propcare.demo", user.Email);
    }

    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"propcare-auth-{Guid.NewGuid()}")
            .Options;
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "PropCareCloud",
                ["Jwt:Audience"] = "PropCareCloud.Frontend",
                ["Jwt:SigningKey"] = "UnitTestJwtSigningKeyForPropCareCloudAuthSprintNine"
            })
            .Build();
    }

    private static async Task SeedOccupiedUnitsAsync(AppDbContext dbContext)
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
            UnitNumber = "B-1102",
            Floor = "11",
            Bedrooms = 1,
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

        dbContext.Properties.Add(property);
        dbContext.RentalUnits.AddRange(unit, imranUnit);
        await dbContext.SaveChangesAsync();
    }
}
