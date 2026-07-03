using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.Properties;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class PropertyServiceTests
{
    [Fact]
    public async Task GetPropertiesAsync_ReturnsSeededProperties()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        await SeedPropertyAsync(dbContext);
        var service = new PropertyService(dbContext);

        var properties = await service.GetPropertiesAsync();

        Assert.Single(properties);
        Assert.Equal("Cloud Residence", properties[0].Name);
        Assert.Equal(1, properties[0].UnitCount);
    }

    [Fact]
    public async Task CreatePropertyAsync_CreatesProperty()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var service = new PropertyService(dbContext);

        var created = await service.CreatePropertyAsync(new PropertyCreateRequest
        {
            Name = "Harbor Heights",
            AddressLine1 = "88 Marina Link",
            City = "Penang",
            Country = "Malaysia",
            Status = PropertyStatus.Active
        });

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Harbor Heights", created.Name);
        Assert.Equal(1, await dbContext.Properties.CountAsync());
    }

    [Fact]
    public async Task UpdatePropertyAsync_UpdatesPropertyData()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var property = await SeedPropertyAsync(dbContext);
        var service = new PropertyService(dbContext);

        var updated = await service.UpdatePropertyAsync(property.Id, new PropertyUpdateRequest
        {
            Name = "Updated Residence",
            AddressLine1 = "22 Updated Road",
            City = "Kuala Lumpur",
            Country = "Malaysia",
            Status = PropertyStatus.UnderMaintenance
        });

        Assert.NotNull(updated);
        Assert.Equal("Updated Residence", updated.Name);
        Assert.Equal(PropertyStatus.UnderMaintenance, updated.Status);
    }

    [Fact]
    public async Task DeletePropertyAsync_BlocksDeletionWhenUnitsExist()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var property = await SeedPropertyAsync(dbContext);
        var service = new PropertyService(dbContext);

        var deleted = await service.DeletePropertyAsync(property.Id);

        Assert.False(deleted);
        Assert.Equal(1, await dbContext.Properties.CountAsync());
    }

    [Fact]
    public async Task CreateUnitAsync_CreatesUnitUnderProperty()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var property = new Property
        {
            Name = "City Point",
            AddressLine1 = "1 City Road",
            City = "Kuala Lumpur",
            Country = "Malaysia",
            Status = PropertyStatus.Active
        };
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync();
        var service = new PropertyService(dbContext);

        var unit = await service.CreateUnitAsync(property.Id, new RentalUnitCreateRequest
        {
            UnitNumber = "A-0101",
            Floor = "1",
            Bedrooms = 2,
            Status = UnitStatus.Occupied
        });

        Assert.NotNull(unit);
        Assert.Equal(property.Id, unit.PropertyId);
        Assert.Equal("A-0101", unit.UnitNumber);
        Assert.Equal(1, await dbContext.RentalUnits.CountAsync());
    }

    [Fact]
    public async Task DeleteUnitAsync_BlocksDeletionWhenMaintenanceRequestsExist()
    {
        var options = CreateOptions();
        await using var dbContext = new AppDbContext(options);
        var (_, unit, tenant) = await SeedPropertyUnitAndTenantAsync(dbContext);
        dbContext.MaintenanceRequests.Add(new MaintenanceRequest
        {
            RentalUnitId = unit.Id,
            TenantProfileId = tenant.Id,
            Title = "Sink leak",
            Description = "Water leaking under sink.",
            Category = MaintenanceCategory.Plumbing,
            Priority = MaintenancePriority.High,
            Status = MaintenanceStatus.Submitted
        });
        await dbContext.SaveChangesAsync();
        var service = new PropertyService(dbContext);

        var deleted = await service.DeleteUnitAsync(unit.PropertyId, unit.Id);

        Assert.False(deleted);
        Assert.Equal(1, await dbContext.RentalUnits.CountAsync());
    }

    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"propcare-property-{Guid.NewGuid()}")
            .Options;
    }

    private static async Task<Property> SeedPropertyAsync(AppDbContext dbContext)
    {
        var (property, _, _) = await SeedPropertyUnitAndTenantAsync(dbContext);

        return property;
    }

    private static async Task<(Property Property, RentalUnit Unit, UserProfile Tenant)> SeedPropertyUnitAndTenantAsync(
        AppDbContext dbContext)
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
            FullName = "Test Tenant",
            Email = "tenant.test@example.com",
            Role = UserRole.Tenant
        };

        dbContext.Properties.Add(property);
        dbContext.RentalUnits.Add(unit);
        dbContext.UserProfiles.Add(tenant);
        await dbContext.SaveChangesAsync();

        return (property, unit, tenant);
    }
}
