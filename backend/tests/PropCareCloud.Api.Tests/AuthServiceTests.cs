using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.Auth;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task EnsureDemoAccountsAsync_CreatesFourHashedDemoAccounts()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = new AuthService(dbContext, CreateConfiguration());

        await service.EnsureDemoAccountsAsync();

        var accounts = await dbContext.AuthUserAccounts
            .Include(account => account.UserProfile)
            .ToListAsync();

        Assert.Equal(4, accounts.Count);
        Assert.All(accounts, account =>
        {
            Assert.NotNull(account.UserProfile);
            Assert.True(account.IsActive);
            Assert.StartsWith("$2", account.PasswordHash);
            Assert.DoesNotContain("PropCare@", account.PasswordHash);
        });
    }

    [Fact]
    public async Task EnsureDemoAccountsAsync_DoesNotDuplicateAccounts()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = new AuthService(dbContext, CreateConfiguration());

        await service.EnsureDemoAccountsAsync();
        await service.EnsureDemoAccountsAsync();

        Assert.Equal(4, await dbContext.AuthUserAccounts.CountAsync());
        Assert.Equal(4, await dbContext.UserProfiles.CountAsync());
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
    public async Task GetDemoCredentialsAsync_ReturnsAllFourRoles()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = new AuthService(dbContext, CreateConfiguration());

        var credentials = await service.GetDemoCredentialsAsync();

        Assert.Equal(4, credentials.Count);
        Assert.Contains(credentials, credential => credential.Email == "admin@propcare.demo");
        Assert.Contains(credentials, credential => credential.Email == "manager@propcare.demo");
        Assert.Contains(credentials, credential => credential.Email == "tenant@propcare.demo");
        Assert.Contains(credentials, credential => credential.Email == "staff@propcare.demo");
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
}
