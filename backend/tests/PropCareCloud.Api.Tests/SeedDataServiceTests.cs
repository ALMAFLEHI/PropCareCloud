using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class SeedDataServiceTests
{
    [Fact]
    public async Task SeedDemoDataAsync_CreatesCompleteDatasetFromEmptyDatabase()
    {
        var options = CreateOptions("propcare-seed-empty");

        await using var dbContext = new AppDbContext(options);
        var service = CreateSeedDataService(dbContext);

        var result = await service.SeedDemoDataAsync();

        Assert.True(result.Success);
        Assert.True(result.CreatedOrRepaired);
        Assert.False(result.SkippedBecauseAlreadySeeded);
        Assert.Equal(5, result.UsersCreated);
        Assert.Equal(2, result.PropertiesCreated);
        Assert.Equal(6, result.UnitsCreated);
        Assert.Equal(4, result.TenantAssignmentsCreated);
        Assert.Equal(4, result.RequestsCreated);
        Assert.Equal(4, result.CommentsCreated);
        Assert.Equal(1, result.AttachmentsCreated);
        Assert.Equal(26, result.RecordsCreated);
        Assert.Equal(5, result.RecordsRepaired);
        Assert.Equal(5, result.UsersTotal);
        Assert.Equal(2, result.PropertiesTotal);
        Assert.Equal(6, result.UnitsTotal);
        Assert.Equal(4, result.TenantAssignmentsTotal);
        Assert.Equal(4, result.RequestsTotal);
        Assert.Equal(4, result.CommentsTotal);
        Assert.Equal(1, result.AttachmentsTotal);

        await AssertCompleteSeedDatasetAsync(dbContext);
    }

    [Fact]
    public async Task SeedDemoDataAsync_RepairsPortfolioDataAfterAuthOnlySeed()
    {
        var options = CreateOptions("propcare-seed-auth-only");

        await using var dbContext = new AppDbContext(options);
        var authService = CreateAuthService(dbContext);
        await authService.EnsureDemoAccountsAsync();

        Assert.Equal(5, await dbContext.UserProfiles.CountAsync());
        Assert.Equal(5, await dbContext.AuthUserAccounts.CountAsync());
        Assert.Equal(0, await dbContext.Properties.CountAsync());
        Assert.Equal(0, await dbContext.RentalUnits.CountAsync());
        Assert.Equal(0, await dbContext.MaintenanceRequests.CountAsync());

        var service = new SeedDataService(dbContext, authService);

        var result = await service.SeedDemoDataAsync();

        Assert.True(result.Success);
        Assert.True(result.CreatedOrRepaired);
        Assert.False(result.SkippedBecauseAlreadySeeded);
        Assert.Equal(0, result.UsersCreated);
        Assert.Equal(2, result.PropertiesCreated);
        Assert.Equal(6, result.UnitsCreated);
        Assert.Equal(4, result.TenantAssignmentsCreated);
        Assert.Equal(4, result.RequestsCreated);
        Assert.Equal(4, result.CommentsCreated);
        Assert.Equal(1, result.AttachmentsCreated);

        await AssertCompleteSeedDatasetAsync(dbContext);
    }

    [Fact]
    public async Task SeedDemoDataAsync_CreatesAvailableUnitsForTenantRegistrationApproval()
    {
        var options = CreateOptions("propcare-seed-available-units");

        await using var dbContext = new AppDbContext(options);
        var service = CreateSeedDataService(dbContext);

        await service.SeedDemoDataAsync();
        await service.SeedDemoDataAsync();

        var availableUnits = await dbContext.RentalUnits
            .Where(unit => unit.Status == UnitStatus.Available)
            .Where(unit => !unit.TenantAssignments.Any(assignment =>
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null))
            .Select(unit => unit.UnitNumber)
            .OrderBy(unitNumber => unitNumber)
            .ToArrayAsync();

        Assert.Equal(["A-0303", "B-1401"], availableUnits);
        Assert.Equal(1, await dbContext.RentalUnits.CountAsync(unit => unit.UnitNumber == "A-0303"));
        Assert.Equal(1, await dbContext.RentalUnits.CountAsync(unit => unit.UnitNumber == "B-1401"));
    }

    [Fact]
    public async Task SeedDemoDataAsync_DoesNotDuplicateExistingSeedData()
    {
        var options = CreateOptions("propcare-seed-repeat");

        await using var dbContext = new AppDbContext(options);
        var service = CreateSeedDataService(dbContext);
        await service.SeedDemoDataAsync();

        var result = await service.SeedDemoDataAsync();

        Assert.True(result.Success);
        Assert.False(result.CreatedOrRepaired);
        Assert.True(result.SkippedBecauseAlreadySeeded);
        Assert.Equal(0, result.UsersCreated);
        Assert.Equal(0, result.PropertiesCreated);
        Assert.Equal(0, result.UnitsCreated);
        Assert.Equal(0, result.TenantAssignmentsCreated);
        Assert.Equal(0, result.RequestsCreated);
        Assert.Equal(0, result.CommentsCreated);
        Assert.Equal(0, result.AttachmentsCreated);
        Assert.Equal(0, result.RecordsCreated);
        Assert.Equal(0, result.RecordsRepaired);

        await AssertCompleteSeedDatasetAsync(dbContext);
    }

    [Fact]
    public async Task SeedDemoDataAsync_RepairsMissingActivityAndAttachmentRecords()
    {
        var options = CreateOptions("propcare-seed-repair-missing-children");

        await using var dbContext = new AppDbContext(options);
        var service = CreateSeedDataService(dbContext);
        await service.SeedDemoDataAsync();

        dbContext.MaintenanceRequestComments.RemoveRange(dbContext.MaintenanceRequestComments);
        dbContext.MaintenanceRequestAttachments.RemoveRange(dbContext.MaintenanceRequestAttachments);
        await dbContext.SaveChangesAsync();

        var result = await service.SeedDemoDataAsync();

        Assert.True(result.Success);
        Assert.True(result.CreatedOrRepaired);
        Assert.False(result.SkippedBecauseAlreadySeeded);
        Assert.Equal(4, result.CommentsCreated);
        Assert.Equal(1, result.AttachmentsCreated);
        Assert.Equal(4, await dbContext.MaintenanceRequestComments.CountAsync());
        Assert.Equal(1, await dbContext.MaintenanceRequestAttachments.CountAsync());

        await AssertCompleteSeedDatasetAsync(dbContext);
    }

    private static async Task AssertCompleteSeedDatasetAsync(AppDbContext dbContext)
    {
        Assert.Equal(5, await dbContext.AuthUserAccounts.CountAsync());
        Assert.Equal(5, await dbContext.UserProfiles.CountAsync());
        Assert.Equal(2, await dbContext.Properties.CountAsync());
        Assert.Equal(6, await dbContext.RentalUnits.CountAsync());
        Assert.Equal(4, await dbContext.TenantUnitAssignments
            .CountAsync(assignment => assignment.IsActive && assignment.LeaseEndDateUtc == null));
        Assert.Equal(4, await dbContext.MaintenanceRequests.CountAsync());
        Assert.Equal(4, await dbContext.MaintenanceRequestComments.CountAsync());
        Assert.Equal(1, await dbContext.MaintenanceRequestAttachments.CountAsync());

        var propertyNames = await dbContext.Properties
            .Select(property => property.Name)
            .OrderBy(propertyName => propertyName)
            .ToArrayAsync();
        Assert.Equal(["Cloud Residence", "Harbor Heights"], propertyNames);

        var unitNumbers = await dbContext.RentalUnits
            .Select(unit => unit.UnitNumber)
            .OrderBy(unitNumber => unitNumber)
            .ToArrayAsync();
        Assert.Equal(["A-0101", "A-0205", "A-0303", "B-1102", "B-1208", "B-1401"], unitNumbers);

        var saraTenantId = await dbContext.AuthUserAccounts
            .Where(account => account.Email == "tenant@propcare.demo")
            .Select(account => account.UserProfileId)
            .SingleAsync();
        var imranTenantId = await dbContext.AuthUserAccounts
            .Where(account => account.Email == "imran@propcare.demo")
            .Select(account => account.UserProfileId)
            .SingleAsync();

        var saraAssignments = await GetActiveUnitNumbersAsync(dbContext, saraTenantId);
        var imranAssignments = await GetActiveUnitNumbersAsync(dbContext, imranTenantId);

        Assert.Equal(["A-0101", "B-1102"], saraAssignments);
        Assert.Equal(["A-0205", "B-1208"], imranAssignments);

        var saraRequestUnits = await GetRequestUnitNumbersAsync(dbContext, saraTenantId);
        var imranRequestUnits = await GetRequestUnitNumbersAsync(dbContext, imranTenantId);

        Assert.Equal(saraAssignments, saraRequestUnits);
        Assert.Equal(imranAssignments, imranRequestUnits);

        var attachment = await dbContext.MaintenanceRequestAttachments.SingleAsync();
        Assert.False(string.IsNullOrWhiteSpace(attachment.StorageKey));
        Assert.StartsWith("future-s3/demo-maintenance/", attachment.StorageKey);
        Assert.EndsWith("/kitchen-sink-leak-demo.jpg", attachment.StorageKey);
    }

    private static DbContextOptions<AppDbContext> CreateOptions(string prefix)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"{prefix}-{Guid.NewGuid()}")
            .Options;
    }

    private static SeedDataService CreateSeedDataService(AppDbContext dbContext)
    {
        return new SeedDataService(dbContext, CreateAuthService(dbContext));
    }

    private static AuthService CreateAuthService(AppDbContext dbContext)
    {
        return new AuthService(dbContext, CreateConfiguration());
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "PropCareCloud",
                ["Jwt:Audience"] = "PropCareCloud.Frontend",
                ["Jwt:SigningKey"] = "UnitTestJwtSigningKeyForPropCareCloudSeedRepair"
            })
            .Build();
    }

    private static async Task<string[]> GetActiveUnitNumbersAsync(
        AppDbContext dbContext,
        Guid tenantProfileId)
    {
        return await dbContext.TenantUnitAssignments
            .Include(assignment => assignment.RentalUnit)
            .Where(assignment =>
                assignment.TenantProfileId == tenantProfileId &&
                assignment.IsActive &&
                assignment.LeaseEndDateUtc == null)
            .Select(assignment => assignment.RentalUnit!.UnitNumber)
            .OrderBy(unitNumber => unitNumber)
            .ToArrayAsync();
    }

    private static async Task<string[]> GetRequestUnitNumbersAsync(
        AppDbContext dbContext,
        Guid tenantProfileId)
    {
        return await dbContext.MaintenanceRequests
            .Include(request => request.RentalUnit)
            .Where(request => request.TenantProfileId == tenantProfileId)
            .Select(request => request.RentalUnit!.UnitNumber)
            .Distinct()
            .OrderBy(unitNumber => unitNumber)
            .ToArrayAsync();
    }
}
