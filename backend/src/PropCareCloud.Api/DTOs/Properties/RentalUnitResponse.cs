using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.Properties;

public sealed record RentalUnitResponse(
    Guid Id,
    Guid PropertyId,
    string UnitNumber,
    string? Floor,
    int? Bedrooms,
    UnitStatus Status,
    DateTime CreatedAtUtc);
