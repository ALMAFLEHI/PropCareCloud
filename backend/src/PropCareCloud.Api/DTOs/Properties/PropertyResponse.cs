using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.Properties;

public sealed record PropertyResponse(
    Guid Id,
    string Name,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Country,
    PropertyStatus Status,
    DateTime CreatedAtUtc,
    int UnitCount);
