using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class SeedDataServiceTests
{
    [Fact]
    public async Task SeedDemoDataAsync_CreatesSampleDataOnce()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"propcare-seed-{Guid.NewGuid()}")
            .Options;

        await using (var dbContext = new AppDbContext(options))
        {
            var service = new SeedDataService(dbContext);

            var result = await service.SeedDemoDataAsync();

            Assert.True(result.Success);
            Assert.False(result.SkippedBecauseAlreadySeeded);
            Assert.Equal(6, result.UsersCreated);
            Assert.Equal(2, result.PropertiesCreated);
            Assert.Equal(4, result.UnitsCreated);
            Assert.Equal(4, result.RequestsCreated);
            Assert.Equal(4, result.CommentsCreated);
            Assert.Equal(1, result.AttachmentsCreated);
        }

        await using (var dbContext = new AppDbContext(options))
        {
            Assert.Equal(6, await dbContext.UserProfiles.CountAsync());
            Assert.Equal(2, await dbContext.Properties.CountAsync());
            Assert.Equal(4, await dbContext.RentalUnits.CountAsync());
            Assert.Equal(3, await dbContext.TenantUnitAssignments.CountAsync());
            Assert.Equal(4, await dbContext.MaintenanceRequests.CountAsync());
            Assert.Equal(4, await dbContext.MaintenanceRequestComments.CountAsync());
            Assert.Equal(1, await dbContext.MaintenanceRequestAttachments.CountAsync());

            var saraTenantId = await dbContext.UserProfiles
                .Where(user => user.Email == "tenant1@example.com")
                .Select(user => user.Id)
                .SingleAsync();
            var imranTenantId = await dbContext.UserProfiles
                .Where(user => user.Email == "tenant2@example.com")
                .Select(user => user.Id)
                .SingleAsync();
            var saraAssignments = await dbContext.TenantUnitAssignments
                .Where(assignment => assignment.TenantProfileId == saraTenantId &&
                                     assignment.IsActive &&
                                     assignment.LeaseEndDateUtc == null)
                .ToListAsync();
            var imranAssignments = await dbContext.TenantUnitAssignments
                .Where(assignment => assignment.TenantProfileId == imranTenantId &&
                                     assignment.IsActive &&
                                     assignment.LeaseEndDateUtc == null)
                .ToListAsync();

            Assert.Equal(2, saraAssignments.Count);
            Assert.Single(imranAssignments);
            Assert.Empty(saraAssignments.Select(assignment => assignment.RentalUnitId)
                .Intersect(imranAssignments.Select(assignment => assignment.RentalUnitId)));

            var attachment = await dbContext.MaintenanceRequestAttachments.SingleAsync();
            Assert.False(string.IsNullOrWhiteSpace(attachment.StorageKey));
            Assert.StartsWith("future-s3/demo-maintenance/", attachment.StorageKey);
            Assert.EndsWith("/kitchen-sink-leak-demo.jpg", attachment.StorageKey);
        }
    }

    [Fact]
    public async Task SeedDemoDataAsync_DoesNotDuplicateExistingSeedData()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"propcare-seed-repeat-{Guid.NewGuid()}")
            .Options;

        await using (var dbContext = new AppDbContext(options))
        {
            var service = new SeedDataService(dbContext);
            await service.SeedDemoDataAsync();
        }

        await using (var dbContext = new AppDbContext(options))
        {
            var service = new SeedDataService(dbContext);

            var result = await service.SeedDemoDataAsync();

            Assert.True(result.Success);
            Assert.True(result.SkippedBecauseAlreadySeeded);
            Assert.Equal(0, result.UsersCreated);
            Assert.Equal(0, result.PropertiesCreated);
            Assert.Equal(0, result.UnitsCreated);
            Assert.Equal(0, result.RequestsCreated);
            Assert.Equal(0, result.CommentsCreated);
            Assert.Equal(0, result.AttachmentsCreated);
            Assert.Equal(6, await dbContext.UserProfiles.CountAsync());
            Assert.Equal(3, await dbContext.TenantUnitAssignments.CountAsync());
            Assert.Equal(4, await dbContext.MaintenanceRequests.CountAsync());
        }
    }
}
