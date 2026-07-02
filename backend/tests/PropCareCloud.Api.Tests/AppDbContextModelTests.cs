using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.Tests;

public sealed class AppDbContextModelTests
{
    [Fact]
    public async Task AppDbContext_CanSaveAndReadMaintenanceRequestGraph()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"propcare-domain-{Guid.NewGuid()}")
            .Options;

        var tenant = new UserProfile
        {
            FullName = "Aisha Tenant",
            Email = "aisha.tenant@example.com",
            Role = UserRole.Tenant
        };
        var staff = new UserProfile
        {
            FullName = "Omar Staff",
            Email = "omar.staff@example.com",
            Role = UserRole.MaintenanceStaff
        };
        var property = new Property
        {
            Name = "City Heights",
            AddressLine1 = "100 Cloud Avenue",
            City = "Kuala Lumpur",
            Country = "Malaysia",
            Status = PropertyStatus.Active
        };
        var unit = new RentalUnit
        {
            PropertyId = property.Id,
            Property = property,
            UnitNumber = "B-08-02",
            Bedrooms = 2,
            Status = UnitStatus.Occupied
        };
        var request = new MaintenanceRequest
        {
            RentalUnitId = unit.Id,
            RentalUnit = unit,
            TenantProfileId = tenant.Id,
            TenantProfile = tenant,
            AssignedStaffProfileId = staff.Id,
            AssignedStaffProfile = staff,
            Title = "Air conditioning not cooling",
            Description = "The living room air conditioning unit is running but not cooling.",
            Category = MaintenanceCategory.HVAC,
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceStatus.Assigned
        };

        await using (var dbContext = new AppDbContext(options))
        {
            dbContext.UserProfiles.AddRange(tenant, staff);
            dbContext.Properties.Add(property);
            dbContext.RentalUnits.Add(unit);
            dbContext.MaintenanceRequests.Add(request);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = new AppDbContext(options))
        {
            var savedRequest = await dbContext.MaintenanceRequests
                .Include(maintenanceRequest => maintenanceRequest.RentalUnit)
                .Include(maintenanceRequest => maintenanceRequest.TenantProfile)
                .SingleAsync(maintenanceRequest => maintenanceRequest.Id == request.Id);

            Assert.Equal("Air conditioning not cooling", savedRequest.Title);
            Assert.Equal("B-08-02", savedRequest.RentalUnit?.UnitNumber);
            Assert.Equal("Aisha Tenant", savedRequest.TenantProfile?.FullName);
            Assert.Equal(MaintenanceStatus.Assigned, savedRequest.Status);
        }
    }
}
