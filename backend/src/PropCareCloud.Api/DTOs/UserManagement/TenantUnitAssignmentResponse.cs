namespace PropCareCloud.Api.DTOs.UserManagement;

public sealed record TenantUnitAssignmentResponse(
    Guid AssignmentId,
    Guid TenantProfileId,
    string TenantName,
    Guid RentalUnitId,
    string UnitNumber,
    string PropertyName,
    bool IsActive,
    DateTime LeaseStartDateUtc,
    DateTime? LeaseEndDateUtc);
