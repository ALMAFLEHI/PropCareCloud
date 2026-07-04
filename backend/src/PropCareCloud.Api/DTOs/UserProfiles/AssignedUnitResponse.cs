using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.UserProfiles;

public sealed record AssignedUnitResponse(
    Guid Id,
    Guid RentalUnitId,
    Guid PropertyId,
    string PropertyName,
    string UnitNumber,
    string? Floor,
    int? Bedrooms,
    UnitStatus Status,
    DateTime LeaseStartDateUtc);
