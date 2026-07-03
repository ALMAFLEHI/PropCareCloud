using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.DTOs.Properties;

namespace PropCareCloud.Api.Services;

public interface IPropertyService
{
    Task<List<PropertyResponse>> GetPropertiesAsync();
    Task<PropertyResponse?> GetPropertyByIdAsync(Guid id);
    Task<PropertyResponse> CreatePropertyAsync(PropertyCreateRequest request);
    Task<PropertyResponse?> UpdatePropertyAsync(Guid id, PropertyUpdateRequest request);
    Task<bool> DeletePropertyAsync(Guid id);
    Task<List<RentalUnitResponse>> GetUnitsByPropertyAsync(Guid propertyId);
    Task<RentalUnitResponse?> GetUnitByIdAsync(Guid propertyId, Guid unitId);
    Task<RentalUnitResponse?> CreateUnitAsync(Guid propertyId, RentalUnitCreateRequest request);
    Task<RentalUnitResponse?> UpdateUnitAsync(Guid propertyId, Guid unitId, RentalUnitUpdateRequest request);
    Task<bool> DeleteUnitAsync(Guid propertyId, Guid unitId);
}

public sealed class PropertyService(AppDbContext dbContext) : IPropertyService
{
    public async Task<List<PropertyResponse>> GetPropertiesAsync()
    {
        return await dbContext.Properties
            .AsNoTracking()
            .OrderBy(property => property.Name)
            .Select(property => new PropertyResponse(
                property.Id,
                property.Name,
                property.AddressLine1,
                property.AddressLine2,
                property.City,
                property.Country,
                property.Status,
                property.CreatedAtUtc,
                property.Units.Count))
            .ToListAsync();
    }

    public async Task<PropertyResponse?> GetPropertyByIdAsync(Guid id)
    {
        return await dbContext.Properties
            .AsNoTracking()
            .Where(property => property.Id == id)
            .Select(property => new PropertyResponse(
                property.Id,
                property.Name,
                property.AddressLine1,
                property.AddressLine2,
                property.City,
                property.Country,
                property.Status,
                property.CreatedAtUtc,
                property.Units.Count))
            .SingleOrDefaultAsync();
    }

    public async Task<PropertyResponse> CreatePropertyAsync(PropertyCreateRequest request)
    {
        var property = new Property
        {
            Name = request.Name.Trim(),
            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(request.AddressLine2) ? null : request.AddressLine2.Trim(),
            City = request.City.Trim(),
            Country = request.Country.Trim(),
            Status = request.Status,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync();

        return MapProperty(property, unitCount: 0);
    }

    public async Task<PropertyResponse?> UpdatePropertyAsync(Guid id, PropertyUpdateRequest request)
    {
        var property = await dbContext.Properties.FindAsync(id);
        if (property is null)
        {
            return null;
        }

        property.Name = request.Name.Trim();
        property.AddressLine1 = request.AddressLine1.Trim();
        property.AddressLine2 = string.IsNullOrWhiteSpace(request.AddressLine2) ? null : request.AddressLine2.Trim();
        property.City = request.City.Trim();
        property.Country = request.Country.Trim();
        property.Status = request.Status;

        await dbContext.SaveChangesAsync();

        var unitCount = await dbContext.RentalUnits.CountAsync(unit => unit.PropertyId == property.Id);

        return MapProperty(property, unitCount);
    }

    public async Task<bool> DeletePropertyAsync(Guid id)
    {
        var property = await dbContext.Properties.FindAsync(id);
        if (property is null)
        {
            return false;
        }

        var hasUnits = await dbContext.RentalUnits.AnyAsync(unit => unit.PropertyId == id);
        if (hasUnits)
        {
            return false;
        }

        dbContext.Properties.Remove(property);
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<List<RentalUnitResponse>> GetUnitsByPropertyAsync(Guid propertyId)
    {
        return await dbContext.RentalUnits
            .AsNoTracking()
            .Where(unit => unit.PropertyId == propertyId)
            .OrderBy(unit => unit.UnitNumber)
            .Select(unit => new RentalUnitResponse(
                unit.Id,
                unit.PropertyId,
                unit.UnitNumber,
                unit.Floor,
                unit.Bedrooms,
                unit.Status,
                unit.CreatedAtUtc))
            .ToListAsync();
    }

    public async Task<RentalUnitResponse?> GetUnitByIdAsync(Guid propertyId, Guid unitId)
    {
        return await dbContext.RentalUnits
            .AsNoTracking()
            .Where(unit => unit.PropertyId == propertyId && unit.Id == unitId)
            .Select(unit => new RentalUnitResponse(
                unit.Id,
                unit.PropertyId,
                unit.UnitNumber,
                unit.Floor,
                unit.Bedrooms,
                unit.Status,
                unit.CreatedAtUtc))
            .SingleOrDefaultAsync();
    }

    public async Task<RentalUnitResponse?> CreateUnitAsync(Guid propertyId, RentalUnitCreateRequest request)
    {
        var propertyExists = await dbContext.Properties.AnyAsync(property => property.Id == propertyId);
        if (!propertyExists)
        {
            return null;
        }

        var unit = new RentalUnit
        {
            PropertyId = propertyId,
            UnitNumber = request.UnitNumber.Trim(),
            Floor = string.IsNullOrWhiteSpace(request.Floor) ? null : request.Floor.Trim(),
            Bedrooms = request.Bedrooms,
            Status = request.Status,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.RentalUnits.Add(unit);
        await dbContext.SaveChangesAsync();

        return MapUnit(unit);
    }

    public async Task<RentalUnitResponse?> UpdateUnitAsync(
        Guid propertyId,
        Guid unitId,
        RentalUnitUpdateRequest request)
    {
        var unit = await dbContext.RentalUnits
            .SingleOrDefaultAsync(rentalUnit => rentalUnit.PropertyId == propertyId && rentalUnit.Id == unitId);
        if (unit is null)
        {
            return null;
        }

        unit.UnitNumber = request.UnitNumber.Trim();
        unit.Floor = string.IsNullOrWhiteSpace(request.Floor) ? null : request.Floor.Trim();
        unit.Bedrooms = request.Bedrooms;
        unit.Status = request.Status;

        await dbContext.SaveChangesAsync();

        return MapUnit(unit);
    }

    public async Task<bool> DeleteUnitAsync(Guid propertyId, Guid unitId)
    {
        var unit = await dbContext.RentalUnits
            .SingleOrDefaultAsync(rentalUnit => rentalUnit.PropertyId == propertyId && rentalUnit.Id == unitId);
        if (unit is null)
        {
            return false;
        }

        var hasRequests = await dbContext.MaintenanceRequests
            .AnyAsync(request => request.RentalUnitId == unitId);
        if (hasRequests)
        {
            return false;
        }

        dbContext.RentalUnits.Remove(unit);
        await dbContext.SaveChangesAsync();

        return true;
    }

    private static PropertyResponse MapProperty(Property property, int unitCount)
    {
        return new PropertyResponse(
            property.Id,
            property.Name,
            property.AddressLine1,
            property.AddressLine2,
            property.City,
            property.Country,
            property.Status,
            property.CreatedAtUtc,
            unitCount);
    }

    private static RentalUnitResponse MapUnit(RentalUnit unit)
    {
        return new RentalUnitResponse(
            unit.Id,
            unit.PropertyId,
            unit.UnitNumber,
            unit.Floor,
            unit.Bedrooms,
            unit.Status,
            unit.CreatedAtUtc);
    }
}
