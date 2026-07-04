using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.UserManagement;

public sealed record AvailableUnitResponse(
    Guid RentalUnitId,
    Guid PropertyId,
    string PropertyName,
    string UnitNumber,
    string? Floor,
    int? Bedrooms,
    UnitStatus Status);
